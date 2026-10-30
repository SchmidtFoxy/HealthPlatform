import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ArrowDownRight, ArrowUpRight, Camera, ImagePlus, Minus, Table2, Trash2 } from 'lucide-react';
import { ReactNode, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { Button, Card, Field, inputCls, Segmented, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta, n0, n1 } from '@/lib/format';
import { Foto } from '@/lib/imagemProtegida';

// Painel de evolucao por periodo. O paciente ve em Progresso; a equipe ve na aba Evolucao da ficha.
// Os dados vem de /evolucao-periodo, que ja compara o periodo escolhido com o anterior de mesmo tamanho.

export type Periodo = '30' | '90' | '180' | '365';
export const PERIODOS: { value: Periodo; label: string }[] = [
  { value: '30', label: '30 dias' },
  { value: '90', label: '90 dias' },
  { value: '180', label: '6 meses' },
  { value: '365', label: '1 ano' },
];

interface Resumo {
  inicio: string;
  fim: string;
  treinos: number;
  minutos: number;
  series: number;
  volumeKg: number;
  esforcoMedio: number | null;
  prontidaoMedia: number | null;
  sonoMedio: number | null;
  checkIns: number;
  adesaoDieta: number | null;
  refeicoesRegistradas: number;
  pesoKg: number | null;
  percentualGordura: number | null;
  cinturaCm: number | null;
}

type Semana = Omit<Resumo, 'fim' | 'pesoKg' | 'percentualGordura' | 'cinturaCm'>;

export interface AvaliacaoEvolucao {
  id: string;
  dataUtc: string;
  pesoKg: number | null;
  imc: number | null;
  percentualGordura: number | null;
  massaMagraKg: number | null;
  massaGordaKg: number | null;
  cinturaCm: number | null;
  abdomenCm: number | null;
  quadrilCm: number | null;
}

export interface FotoEvolucao {
  id: string;
  dataHoraUtc: string;
  pose: string;
  anotada: boolean;
  descricao: string | null;
  url: string;
}

export interface EvolucaoPeriodo {
  dias: number;
  atual: Resumo;
  anterior: Resumo;
  semanas: Semana[];
  gruposMusculares: { grupo: string; seriesAtual: number; seriesAnterior: number }[];
  avaliacoes: AvaliacaoEvolucao[];
  fotos: FotoEvolucao[];
}

/** pacienteId null = o proprio paciente logado. */
export function useEvolucaoPeriodo(pacienteId: string | null, periodo: Periodo) {
  return useQuery({
    queryKey: pacienteId ? ['pro', 'evolucao-periodo', pacienteId, periodo] : ['p', 'evolucao-periodo', periodo],
    queryFn: () => api<EvolucaoPeriodo>(`${pacienteId ? `/api/pacientes/${pacienteId}` : '/api/portal/me'}/evolucao-periodo?dias=${periodo}`),
    placeholderData: (anterior) => anterior,
  });
}

const rotuloPeriodo = (p: Periodo) => PERIODOS.find((x) => x.value === p)!.label;
const toneladas = (kg: number) => (kg >= 1000 ? `${n1(kg / 1000)} t` : `${n0(kg)} kg`);
const horas = (min: number) => (min >= 60 ? `${n1(min / 60)}h` : `${min}min`);

// ---------------------------------------------------------------- Resumo do periodo vs. anterior

interface Indicador {
  rotulo: string;
  atual: number | null;
  anterior: number | null;
  formato: (v: number) => string;
  /** true: subir e bom; false: descer e bom; null: neutro (so mostra a direcao). */
  subirBom: boolean | null;
  /** Diferencas abaixo disso contam como estavel. */
  limiar?: number;
  dica?: string;
}

function Variacao({ i }: { i: Indicador }) {
  if (i.atual == null || i.anterior == null) return <p className="mt-1.5 text-[12px] text-faint">{i.atual == null ? 'sem registros' : 'sem base anterior'}</p>;
  const d = i.atual - i.anterior;
  const estavel = Math.abs(d) < (i.limiar ?? 0.05);
  const bom = estavel || i.subirBom == null ? null : d > 0 === i.subirBom;
  const Icone = estavel ? Minus : d > 0 ? ArrowUpRight : ArrowDownRight;
  return (
    <p className={clsx('num mt-1.5 flex items-center gap-0.5 text-[12px] font-semibold', bom == null ? 'text-muted' : bom ? 'text-ok' : 'text-warn')}>
      <Icone className="h-3.5 w-3.5 shrink-0" aria-hidden />
      {estavel ? 'estável' : `${d > 0 ? '+' : '−'}${i.formato(Math.abs(d))}`}
      <span className="ml-1 font-normal text-faint">vs. antes</span>
    </p>
  );
}

export function ResumoPeriodo({ dados, periodo }: { dados: EvolucaoPeriodo; periodo: Periodo }) {
  const { atual: a, anterior: b } = dados;
  const indicadores: Indicador[] = [
    { rotulo: 'Treinos', atual: a.treinos, anterior: b.treinos, formato: n0, subirBom: true, limiar: 1 },
    { rotulo: 'Tempo treinando', atual: a.minutos, anterior: b.minutos, formato: horas, subirBom: true, limiar: 1 },
    { rotulo: 'Volume levantado', atual: a.volumeKg, anterior: b.volumeKg, formato: toneladas, subirBom: true, limiar: 1, dica: 'Carga × repetições de todas as séries registradas.' },
    { rotulo: 'Adesão à dieta', atual: a.adesaoDieta, anterior: b.adesaoDieta, formato: (v) => `${n0(v)}%`, subirBom: true, limiar: 1, dica: `${a.refeicoesRegistradas} refeições registradas no período.` },
    { rotulo: 'Prontidão média', atual: a.prontidaoMedia, anterior: b.prontidaoMedia, formato: n0, subirBom: true, limiar: 1 },
    { rotulo: 'Sono médio', atual: a.sonoMedio, anterior: b.sonoMedio, formato: (v) => `${n1(v)}h`, subirBom: true, limiar: 0.1 },
    { rotulo: 'Peso', atual: a.pesoKg, anterior: b.pesoKg, formato: (v) => `${n1(v)} kg`, subirBom: null, limiar: 0.1 },
    { rotulo: 'Gordura corporal', atual: a.percentualGordura, anterior: b.percentualGordura, formato: (v) => `${n1(v)}%`, subirBom: false, limiar: 0.1 },
  ];
  const vazio = (i: Indicador) => i.atual == null || (i.atual === 0 && !i.anterior);
  const visiveis = indicadores.filter((i) => !vazio(i) || ['Treinos', 'Adesão à dieta'].includes(i.rotulo));
  return (
    <section>
      <p className="mb-2 text-[13px] text-muted">
        Últimos {rotuloPeriodo(periodo)} ({dataCurta(a.inicio)} a {dataCurta(a.fim)}) comparados com os {rotuloPeriodo(periodo)} anteriores.
      </p>
      <div className="grid grid-cols-2 gap-2 sm:gap-3 lg:grid-cols-4">
        {visiveis.map((i) => (
          <Card key={i.rotulo} className="flex flex-col p-4">
            <p className="text-[13px] text-muted" title={i.dica}>
              {i.rotulo}
            </p>
            <p className="num mt-1 text-[24px] font-semibold leading-none">{i.atual == null ? '—' : i.formato(i.atual)}</p>
            <Variacao i={i} />
          </Card>
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------- Graficos

function useLargura<T extends HTMLElement>() {
  const ref = useRef<T>(null);
  const [w, setW] = useState(0);
  useLayoutEffect(() => {
    const el = ref.current;
    if (!el) return;
    setW(el.clientWidth);
    const ro = new ResizeObserver(() => setW(el.clientWidth));
    ro.observe(el);
    return () => ro.disconnect();
  }, []);
  return [ref, w] as const;
}

function Dica({ x, children, largura }: { x: number; children: ReactNode; largura: number }) {
  // Mantem a caixa dentro do grafico nas pontas.
  const lado = x < 70 ? 'left' : x > largura - 70 ? 'right' : 'center';
  return (
    <div
      role="status"
      className="pointer-events-none absolute top-0 z-10 whitespace-nowrap rounded-xl border border-line bg-surface px-2.5 py-1.5 text-[12px] shadow-card"
      style={lado === 'left' ? { left: 0 } : lado === 'right' ? { right: 0 } : { left: x, transform: 'translateX(-50%)' }}
    >
      {children}
    </div>
  );
}

/** Barras por semana com dica ao passar o dedo/mouse. Semanas sem registro aparecem como lacuna. */
export function BarrasSemanais({ semanas, valor, formato, rotulo }: { semanas: Semana[]; valor: (s: Semana) => number | null; formato: (v: number) => string; rotulo: string }) {
  const [ref, largura] = useLargura<HTMLDivElement>();
  const [foco, setFoco] = useState<number | null>(null);
  const vals = semanas.map(valor);
  const max = Math.max(1, ...vals.map((v) => v ?? 0));
  const com = vals.filter((v): v is number => v != null && v > 0);
  const media = com.length ? com.reduce((x, y) => x + y, 0) / com.length : null;
  const altura = 132;
  const passo = largura / Math.max(1, semanas.length);
  const marcas = [0, Math.floor((semanas.length - 1) / 2), semanas.length - 1].filter((v, i, l) => l.indexOf(v) === i);
  return (
    <div>
      <div className="mb-2 flex items-baseline justify-between text-[12px] text-muted">
        <span>
          máx. <span className="num font-semibold text-fg">{formato(max)}</span>
        </span>
        {media != null && (
          <span>
            média nas semanas com registro <span className="num font-semibold text-fg">{formato(media)}</span>
          </span>
        )}
      </div>
      <div ref={ref} className="relative" style={{ height: altura + 34 }} onMouseLeave={() => setFoco(null)}>
        {foco != null && largura > 0 && (
          <Dica x={(foco + 0.5) * passo} largura={largura}>
            <span className="text-muted">Semana de {dataCurta(semanas[foco].inicio)}: </span>
            <span className="num font-semibold">{vals[foco] == null ? 'sem registro' : formato(vals[foco]!)}</span>
          </Dica>
        )}
        <div className="absolute inset-x-0 border-t border-dashed border-line" style={{ top: 34 }} aria-hidden />
        {media != null && <div className="absolute inset-x-0 border-t border-accent/40" style={{ top: 34 + altura - (media / max) * altura }} aria-hidden />}
        <div className="absolute inset-x-0 bottom-0 flex items-end border-b border-line" style={{ height: altura, gap: semanas.length > 30 ? 1 : 3 }} role="img" aria-label={`${rotulo} por semana`}>
          {semanas.map((s, i) => {
            const v = vals[i];
            return (
              <button
                key={s.inicio}
                type="button"
                className="flex h-full min-w-0 flex-1 items-end focus:outline-none"
                onMouseEnter={() => setFoco(i)}
                onFocus={() => setFoco(i)}
                onClick={() => setFoco(i)}
                aria-label={`Semana de ${dataCurta(s.inicio)}: ${v == null ? 'sem registro' : formato(v)}`}
              >
                <span
                  className={clsx('block w-full rounded-t-[4px] transition-colors', v == null || v === 0 ? 'bg-raised' : foco === i ? 'bg-accent' : 'bg-accent/70')}
                  style={{ height: v == null || v === 0 ? 3 : Math.max(4, (v / max) * altura) }}
                />
              </button>
            );
          })}
        </div>
      </div>
      <div className="relative mt-1 h-4 text-[11px] text-faint">
        {marcas.map((i) => (
          <span key={i} className="absolute" style={i === 0 ? { left: 0 } : i === semanas.length - 1 ? { right: 0 } : { left: (i + 0.5) * passo, transform: 'translateX(-50%)' }}>
            {dataCurta(semanas[i].inicio)}
          </span>
        ))}
      </div>
    </div>
  );
}

/** Linha no tempo (eixo proporcional as datas) com grade, pontos e dica no ponto mais proximo. */
export function LinhaTempo({ pontos, formato, rotulo, subirBom }: { pontos: { data: string; valor: number }[]; formato: (v: number) => string; rotulo: string; subirBom: boolean | null }) {
  const [ref, largura] = useLargura<HTMLDivElement>();
  const [foco, setFoco] = useState<number | null>(null);
  const altura = 180;
  const esq = 44;
  const dir = 12;
  const topo = 40;
  const base = 22;
  if (pontos.length === 0) return null;
  const ts = pontos.map((p) => new Date(p.data).getTime());
  const t0 = ts[0];
  const t1 = ts[ts.length - 1];
  const vs = pontos.map((p) => p.valor);
  let lo = Math.min(...vs);
  let hi = Math.max(...vs);
  const folga = Math.max((hi - lo) * 0.15, Math.abs(hi) * 0.01, 0.5);
  lo -= folga;
  hi += folga;
  const w = Math.max(0, largura - esq - dir);
  const h = altura - topo - base;
  const x = (t: number) => esq + (t1 === t0 ? w / 2 : ((t - t0) / (t1 - t0)) * w);
  const y = (v: number) => topo + h - ((v - lo) / (hi - lo)) * h;
  const grade = [0, 0.5, 1].map((f) => lo + (hi - lo) * f);
  const caminho = pontos.map((p, i) => `${i ? 'L' : 'M'}${x(ts[i]).toFixed(1)},${y(p.valor).toFixed(1)}`).join(' ');
  const delta = vs[vs.length - 1] - vs[0];
  const cor = subirBom == null || Math.abs(delta) < 0.05 || delta > 0 === subirBom ? 'rgb(var(--accent))' : 'rgb(var(--warn))';
  const perto = (px: number) => {
    let melhor = 0;
    ts.forEach((t, i) => Math.abs(x(t) - px) < Math.abs(x(ts[melhor]) - px) && (melhor = i));
    return melhor;
  };
  return (
    <div
      ref={ref}
      className="relative touch-pan-y select-none"
      style={{ height: altura }}
      onPointerMove={(e) => setFoco(perto(e.clientX - e.currentTarget.getBoundingClientRect().left))}
      onPointerDown={(e) => setFoco(perto(e.clientX - e.currentTarget.getBoundingClientRect().left))}
      onMouseLeave={() => setFoco(null)}
    >
      {largura > 0 && (
        <svg width={largura} height={altura} role="img" aria-label={`${rotulo}: de ${formato(vs[0])} em ${dataCurta(pontos[0].data)} para ${formato(vs[vs.length - 1])} em ${dataCurta(pontos[pontos.length - 1].data)}`}>
          {grade.map((g) => (
            <g key={g}>
              <line x1={esq} x2={largura - dir} y1={y(g)} y2={y(g)} stroke="rgb(var(--line))" strokeDasharray="3 4" />
              <text x={esq - 8} y={y(g) + 4} textAnchor="end" className="fill-faint text-[10px]">
                {n1(g)}
              </text>
            </g>
          ))}
          <text x={esq} y={altura - 4} className="fill-faint text-[11px]">
            {dataCurta(pontos[0].data)}
          </text>
          {pontos.length > 1 && (
            <text x={largura - dir} y={altura - 4} textAnchor="end" className="fill-faint text-[11px]">
              {dataCurta(pontos[pontos.length - 1].data)}
            </text>
          )}
          <path d={caminho} fill="none" stroke={cor} strokeWidth={2} strokeLinejoin="round" strokeLinecap="round" />
          {foco != null && <line x1={x(ts[foco])} x2={x(ts[foco])} y1={topo - 4} y2={topo + h} stroke="rgb(var(--line))" />}
          {pontos.map((p, i) => (
            <circle key={p.data + i} cx={x(ts[i])} cy={y(p.valor)} r={foco === i ? 5.5 : 4} fill={cor} stroke="rgb(var(--surface))" strokeWidth={2} />
          ))}
        </svg>
      )}
      {foco != null && largura > 0 && (
        <Dica x={x(ts[foco])} largura={largura}>
          <span className="text-muted">{dataCurta(pontos[foco].data)}: </span>
          <span className="num font-semibold">{formato(pontos[foco].valor)}</span>
          {foco > 0 && (
            <span className="num ml-1 text-muted">
              ({pontos[foco].valor - pontos[foco - 1].valor >= 0 ? '+' : '−'}
              {formato(Math.abs(pontos[foco].valor - pontos[foco - 1].valor))})
            </span>
          )}
        </Dica>
      )}
    </div>
  );
}

// ---------------------------------------------------------------- Semana a semana

type MetricaSemana = 'treinos' | 'volume' | 'dieta' | 'prontidao' | 'sono';
const METRICAS: { value: MetricaSemana; label: string; valor: (s: Semana) => number | null; formato: (v: number) => string; vazio: string }[] = [
  { value: 'treinos', label: 'Treinos', valor: (s) => s.treinos, formato: n0, vazio: 'Nenhum treino concluído no período.' },
  { value: 'volume', label: 'Volume', valor: (s) => s.volumeKg, formato: toneladas, vazio: 'Registre carga e repetições nos treinos para ver o volume.' },
  { value: 'dieta', label: 'Dieta', valor: (s) => s.adesaoDieta, formato: (v) => `${n0(v)}%`, vazio: 'Marque as refeições como feitas, adaptadas ou puladas para ver a adesão.' },
  { value: 'prontidao', label: 'Prontidão', valor: (s) => s.prontidaoMedia, formato: n0, vazio: 'Faça o check-in do dia para acompanhar a prontidão.' },
  { value: 'sono', label: 'Sono', valor: (s) => s.sonoMedio, formato: (v) => `${n1(v)}h`, vazio: 'O sono vem do check-in do dia.' },
];

export function SemanaASemana({ dados }: { dados: EvolucaoPeriodo }) {
  const [m, setM] = useState<MetricaSemana>('treinos');
  const metrica = METRICAS.find((x) => x.value === m)!;
  const temDados = dados.semanas.some((s) => (metrica.valor(s) ?? 0) > 0);
  return (
    <Card className="p-4 sm:p-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="font-semibold">Semana a semana</p>
        <Segmented<MetricaSemana> compact value={m} onChange={setM} options={METRICAS.map(({ value, label }) => ({ value, label }))} className="sm:w-[420px]" />
      </div>
      <div className="mt-4">
        {temDados ? <BarrasSemanais semanas={dados.semanas} valor={metrica.valor} formato={metrica.formato} rotulo={metrica.label} /> : <p className="py-10 text-center text-sm text-muted">{metrica.vazio}</p>}
      </div>
    </Card>
  );
}

// ---------------------------------------------------------------- Corpo

const MEDIDAS: { k: keyof AvaliacaoEvolucao; l: string; u: string; subirBom: boolean | null }[] = [
  { k: 'pesoKg', l: 'Peso', u: 'kg', subirBom: null },
  { k: 'percentualGordura', l: 'Gordura', u: '%', subirBom: false },
  { k: 'massaMagraKg', l: 'Massa magra', u: 'kg', subirBom: true },
  { k: 'massaGordaKg', l: 'Massa gorda', u: 'kg', subirBom: false },
  { k: 'cinturaCm', l: 'Cintura', u: 'cm', subirBom: false },
  { k: 'abdomenCm', l: 'Abdômen', u: 'cm', subirBom: false },
  { k: 'quadrilCm', l: 'Quadril', u: 'cm', subirBom: null },
  { k: 'imc', l: 'IMC', u: '', subirBom: null },
];

export function CorpoEvolucao({ avaliacoes, vazio }: { avaliacoes: AvaliacaoEvolucao[]; vazio: ReactNode }) {
  const medidas = useMemo(
    () =>
      MEDIDAS.map((m) => ({ ...m, pontos: avaliacoes.filter((a) => a[m.k] != null).map((a) => ({ data: a.dataUtc, valor: a[m.k] as number })) })).filter((m) => m.pontos.length > 0),
    [avaliacoes],
  );
  const [sel, setSel] = useState<string | null>(null);
  const [tabela, setTabela] = useState(false);
  const atual = medidas.find((m) => m.k === sel) ?? medidas[0];
  if (!atual) return <Card className="p-5 text-sm text-muted">{vazio}</Card>;
  const fmt = (v: number) => `${n1(v)}${atual.u ? ` ${atual.u}` : ''}`;
  const p0 = atual.pontos[0];
  const p1 = atual.pontos[atual.pontos.length - 1];
  const d = p1.valor - p0.valor;
  const bom = atual.subirBom == null || Math.abs(d) < 0.05 ? null : d > 0 === atual.subirBom;
  return (
    <Card className="p-4 sm:p-5">
      <div className="no-scrollbar -mx-1 flex gap-1.5 overflow-x-auto px-1 pb-1">
        {medidas.map((m) => (
          <button
            key={m.k}
            onClick={() => setSel(m.k)}
            aria-pressed={m.k === atual.k}
            className={clsx('shrink-0 rounded-full border px-3 py-1.5 text-[13px] font-semibold transition', m.k === atual.k ? 'border-accent bg-accent/10 text-accent' : 'border-line text-muted hover:text-fg')}
          >
            {m.l}
          </button>
        ))}
      </div>
      <div className="mt-4 flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="num text-[32px] font-semibold leading-none">
            {n1(p1.valor)}
            {atual.u && <span className="ml-1 text-[15px] font-normal text-muted">{atual.u}</span>}
          </p>
          <p className="mt-1 text-[13px] text-muted">última avaliação, {dataCurta(p1.data)}</p>
        </div>
        {atual.pontos.length > 1 && (
          <div className="text-right">
            <p className={clsx('num text-[17px] font-semibold', bom == null ? 'text-fg' : bom ? 'text-ok' : 'text-warn')}>
              {Math.abs(d) < 0.05 ? 'sem mudança' : `${d > 0 ? '+' : '−'}${fmt(Math.abs(d))}`}
            </p>
            <p className="text-[13px] text-muted">
              desde {dataCurta(p0.data)} ({fmt(p0.valor)})
            </p>
          </div>
        )}
      </div>
      <div className="mt-3">
        {atual.pontos.length > 1 ? (
          <LinhaTempo pontos={atual.pontos} formato={fmt} rotulo={atual.l} subirBom={atual.subirBom} />
        ) : (
          <p className="py-6 text-center text-sm text-muted">Com a próxima avaliação aparece a linha de evolução.</p>
        )}
      </div>
      <button onClick={() => setTabela((t) => !t)} className="mt-2 inline-flex items-center gap-1.5 text-[13px] font-semibold text-accent" aria-expanded={tabela}>
        <Table2 className="h-4 w-4" /> {tabela ? 'Esconder tabela' : 'Ver todas as avaliações'}
      </button>
      {tabela && (
        <div className="mt-3 overflow-x-auto rounded-2xl border border-line/70">
          <table className="w-full min-w-[520px] text-[13px]">
            <thead className="bg-raised/60 text-left text-muted">
              <tr>
                <th className="px-3 py-2 font-semibold">Data</th>
                {medidas.map((m) => (
                  <th key={m.k} className="px-3 py-2 text-right font-semibold">
                    {m.l}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {[...avaliacoes].reverse().map((a) => (
                <tr key={a.id} className="border-t border-line/50">
                  <td className="px-3 py-2">{dataCurta(a.dataUtc)}</td>
                  {medidas.map((m) => (
                    <td key={m.k} className="num px-3 py-2 text-right">
                      {a[m.k] == null ? '—' : n1(a[m.k] as number)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
}

// ---------------------------------------------------------------- Grupos musculares

export function GruposMusculares({ grupos }: { grupos: EvolucaoPeriodo['gruposMusculares'] }) {
  const lista = grupos.filter((g) => g.seriesAtual + g.seriesAnterior > 0).slice(0, 10);
  if (!lista.length) return null;
  const max = Math.max(...lista.map((g) => Math.max(g.seriesAtual, g.seriesAnterior)));
  return (
    <Card className="p-4 sm:p-5">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <p className="font-semibold">Séries por grupo muscular</p>
        <p className="flex items-center gap-3 text-[12px] text-muted">
          <span className="flex items-center gap-1.5">
            <i className="h-2.5 w-2.5 rounded-[3px] bg-accent" /> período atual
          </span>
          <span className="flex items-center gap-1.5">
            <i className="h-2.5 w-2.5 rounded-[3px] bg-faint/50" /> período anterior
          </span>
        </p>
      </div>
      <div className="mt-4 flex flex-col gap-3">
        {lista.map((g) => {
          const d = g.seriesAtual - g.seriesAnterior;
          return (
            <div key={g.grupo} className="grid grid-cols-[minmax(0,7.5rem)_1fr_auto] items-center gap-3">
              <p className="truncate text-[13px] font-semibold" title={g.grupo}>
                {g.grupo}
              </p>
              <div className="flex flex-col gap-[2px]" aria-hidden>
                <span className="h-2.5 rounded-r-[4px] bg-accent" style={{ width: `${Math.max(1, (g.seriesAtual / max) * 100)}%` }} />
                <span className="h-1.5 rounded-r-[4px] bg-faint/50" style={{ width: `${Math.max(1, (g.seriesAnterior / max) * 100)}%` }} />
              </div>
              <p className="num w-[5.5rem] text-right text-[12px] text-muted">
                <span className="font-semibold text-fg">{g.seriesAtual}</span> séries
                {d !== 0 && (
                  <span className={clsx('block', d > 0 ? 'text-ok' : 'text-warn')}>
                    {d > 0 ? '+' : '−'}
                    {Math.abs(d)} vs. antes
                  </span>
                )}
              </p>
            </div>
          );
        })}
      </div>
    </Card>
  );
}

// ---------------------------------------------------------------- Fotos

const POSES = [
  { value: 'Frente', label: 'Frente' },
  { value: 'Costas', label: 'Costas' },
  { value: 'LadoDireito', label: 'Direito' },
  { value: 'LadoEsquerdo', label: 'Esquerdo' },
] as const;
type Pose = (typeof POSES)[number]['value'];
const rotuloPose = (p: string) => POSES.find((x) => x.value === p)?.label ?? p;
const diaFoto = (iso: string) => new Date(iso).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });

/** Antes e depois por posicao, e a galeria. O paciente pode enviar e apagar as proprias fotos. */
export function FotosEvolucao({ fotos, podeEnviar, rodape }: { fotos: FotoEvolucao[]; podeEnviar: boolean; rodape?: ReactNode }) {
  const originais = fotos.filter((f) => !f.anotada);
  const poses = POSES.filter((p) => originais.some((f) => f.pose === p.value));
  const [pose, setPose] = useState<Pose | null>(null);
  const [enviar, setEnviar] = useState(false);
  const [aberta, setAberta] = useState<FotoEvolucao | null>(null);
  const poseAtual = (poses.find((p) => p.value === pose) ?? poses.find((p) => originais.filter((f) => f.pose === p.value).length > 1) ?? poses[0])?.value;
  const daPose = originais.filter((f) => f.pose === poseAtual).sort((a, b) => a.dataHoraUtc.localeCompare(b.dataHoraUtc));
  const antes = daPose[0];
  const depois = daPose.length > 1 ? daPose[daPose.length - 1] : null;
  const dias = antes && depois ? Math.round((new Date(depois.dataHoraUtc).getTime() - new Date(antes.dataHoraUtc).getTime()) / 86400000) : 0;

  return (
    <Card className="p-4 sm:p-5">
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="font-semibold">Fotos</p>
          <p className="text-[13px] text-muted">{fotos.length ? `${fotos.length} ${fotos.length === 1 ? 'foto' : 'fotos'}` : 'Nenhuma foto ainda'}</p>
        </div>
        {podeEnviar && (
          <Button size="sm" icon={<ImagePlus className="h-4 w-4" />} onClick={() => setEnviar(true)}>
            Nova foto
          </Button>
        )}
      </div>

      {!fotos.length ? (
        <div className="mt-4 flex items-start gap-3 rounded-2xl bg-raised/50 p-4 text-sm text-muted">
          <Camera className="h-5 w-5 shrink-0 text-faint" />
          <span>
            {podeEnviar
              ? 'Tire uma foto de frente, de costas e de lado. A cada poucas semanas, repita no mesmo lugar e com a mesma luz para comparar.'
              : 'O paciente ainda não tem fotos de evolução.'}
          </span>
        </div>
      ) : (
        <>
          {poses.length > 1 && (
            <div className="mt-4">
              <Segmented<Pose> compact value={poseAtual as Pose} onChange={setPose} options={poses.map((p) => ({ value: p.value, label: p.label }))} />
            </div>
          )}
          {antes && (
            <div className="mt-4 grid grid-cols-2 gap-3">
              {[
                { f: antes, r: depois ? 'Antes' : 'Primeira' },
                ...(depois ? [{ f: depois, r: 'Agora' }] : []),
              ].map(({ f, r }) => (
                <button key={f.id} onClick={() => setAberta(f)} className="min-w-0 text-left">
                  <Foto path={f.url} alt={`${r}: ${rotuloPose(f.pose)}, ${diaFoto(f.dataHoraUtc)}`} className="aspect-[3/4] w-full rounded-2xl" />
                  <p className="mt-1.5 text-[13px] font-semibold">{r}</p>
                  <p className="text-[12px] text-muted">{diaFoto(f.dataHoraUtc)}</p>
                </button>
              ))}
              {!depois && (
                <div className="grid aspect-[3/4] place-items-center rounded-2xl border border-dashed border-line p-4 text-center text-[13px] text-muted">
                  A próxima foto nesta posição aparece aqui, lado a lado.
                </div>
              )}
            </div>
          )}
          {depois && dias > 0 && <p className="mt-2 text-[13px] text-muted">{dias} dias entre as duas fotos.</p>}
          {fotos.length > 2 && (
            <div className="no-scrollbar -mx-1 mt-4 flex gap-2 overflow-x-auto px-1 pb-1">
              {fotos.map((f) => (
                <button key={f.id} onClick={() => setAberta(f)} className="w-[72px] shrink-0 text-left" aria-label={`Abrir foto ${rotuloPose(f.pose)} de ${diaFoto(f.dataHoraUtc)}`}>
                  <Foto path={f.url} alt="" className="aspect-[3/4] w-full rounded-xl" />
                  <p className="mt-1 truncate text-[11px] text-muted">{dataCurta(f.dataHoraUtc)}</p>
                </button>
              ))}
            </div>
          )}
        </>
      )}
      {rodape && <div className="mt-3 text-[12px] text-faint">{rodape}</div>}
      {podeEnviar && <EnviarFotoSheet open={enviar} onClose={() => setEnviar(false)} />}
      <VerFotoSheet foto={aberta} podeApagar={podeEnviar} onClose={() => setAberta(null)} />
    </Card>
  );
}

function EnviarFotoSheet({ open, onClose }: { open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [pose, setPose] = useState<Pose>('Frente');
  const [nota, setNota] = useState('');
  useEffect(() => {
    if (!open) {
      setFile(null);
      setNota('');
      setPose('Frente');
    }
  }, [open]);
  useEffect(() => {
    if (!file) return setPreview(null);
    const u = URL.createObjectURL(file);
    setPreview(u);
    return () => URL.revokeObjectURL(u);
  }, [file]);
  const salvar = useMutation({
    mutationFn: () => {
      const fd = new FormData();
      fd.append('file', file!);
      fd.append('pose', pose);
      if (nota.trim()) fd.append('descricao', nota.trim());
      return api('/api/portal/me/fotos-evolucao', { method: 'POST', body: fd });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['p', 'evolucao-periodo'] });
      toast('Foto salva. Seu profissional também vê.');
      onClose();
    },
    onError: (e) => toast(e.message, 'bad'),
  });
  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Nova foto de evolução"
      footer={
        <Button className="w-full" disabled={!file} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar foto
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <label className="flex cursor-pointer flex-col items-center justify-center gap-2 overflow-hidden rounded-2xl border border-dashed border-line bg-raised/40 p-4 text-center text-sm text-muted">
          {preview ? <img src={preview} alt="Pré-visualização" className="max-h-64 rounded-xl object-contain" /> : <ImagePlus className="h-7 w-7 text-accent" />}
          <span>{file ? file.name : 'Tirar ou escolher foto (JPEG, PNG ou WebP, até 8 MB)'}</span>
          <input
            type="file"
            accept="image/jpeg,image/png,image/webp"
            className="sr-only"
            aria-label="Imagem"
            onChange={(e) => {
              const f = e.target.files?.[0];
              if (f && f.size > 8 * 1024 * 1024) return toast('A imagem deve ter no máximo 8 MB.', 'bad');
              setFile(f ?? null);
            }}
          />
        </label>
        <Field label="Posição" group>
          <Segmented<Pose> compact value={pose} onChange={setPose} options={POSES.map((p) => ({ value: p.value, label: p.label }))} />
        </Field>
        <Field label="Observação (opcional)">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={nota} maxLength={1000} placeholder="Ex.: em jejum, depois do treino..." onChange={(e) => setNota(e.target.value)} />
        </Field>
        <p className="text-[13px] text-muted">Mesmo lugar, distância e luz deixam a comparação justa. Evite mostrar o rosto. Só você e sua equipe veem.</p>
      </div>
    </Sheet>
  );
}

function VerFotoSheet({ foto, podeApagar, onClose }: { foto: FotoEvolucao | null; podeApagar: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const apagar = useMutation({
    mutationFn: (id: string) => api(`/api/portal/me/fotos-evolucao/${id}`, { method: 'DELETE' }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['p', 'evolucao-periodo'] });
      toast('Foto apagada.');
      onClose();
    },
    onError: (e) => toast(e.message, 'bad'),
  });
  return (
    <Sheet
      open={!!foto}
      onClose={onClose}
      title={foto ? `${rotuloPose(foto.pose)} · ${diaFoto(foto.dataHoraUtc)}` : ''}
      footer={
        podeApagar && foto && !foto.anotada ? (
          <Button
            variant="ghost"
            className="w-full text-bad"
            icon={<Trash2 className="h-4 w-4" />}
            loading={apagar.isPending}
            onClick={() => window.confirm('Apagar esta foto? Ela some também para a sua equipe.') && apagar.mutate(foto.id)}
          >
            Apagar foto
          </Button>
        ) : undefined
      }
    >
      {foto && (
        <div className="flex flex-col gap-3">
          <Foto path={foto.url} alt={`${rotuloPose(foto.pose)}, ${diaFoto(foto.dataHoraUtc)}`} className="max-h-[70vh] w-full rounded-2xl object-contain" />
          {foto.anotada && <p className="text-[13px] font-semibold text-accent">Cópia anotada pela sua equipe</p>}
          {foto.descricao && <p className="text-sm text-muted [overflow-wrap:anywhere]">{foto.descricao}</p>}
        </div>
      )}
    </Sheet>
  );
}

// ---------------------------------------------------------------- Seletor de periodo

export function SeletorPeriodo({ value, onChange }: { value: Periodo; onChange: (p: Periodo) => void }) {
  return <Segmented<Periodo> compact value={value} onChange={onChange} options={PERIODOS} className="w-full sm:w-[360px]" />;
}

export function EvolucaoCarregando() {
  return (
    <div className="flex flex-col gap-3">
      <div className="grid grid-cols-2 gap-2 sm:gap-3 lg:grid-cols-4">
        {Array.from({ length: 4 }, (_, i) => (
          <Skeleton key={i} className="h-[104px]" />
        ))}
      </div>
      <Skeleton className="h-64" />
    </div>
  );
}
