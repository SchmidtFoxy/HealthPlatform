import { useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { ChevronDown, FlaskConical, TrendingDown, TrendingUp } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Card, Pill, Skeleton, Sparkline } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta, n1 } from '@/lib/format';

interface Avaliacao {
  id: string;
  dataUtc: string;
  pesoKg: number | null;
  imc: number | null;
  percentualGordura: number | null;
  massaMagraKg: number | null;
  cinturaCm: number | null;
  abdomenCm: number | null;
  quadrilCm: number | null;
}

interface Resultado {
  id: string;
  marcador: string;
  valorNumerico: number | null;
  valorTexto: string | null;
  unidade: string | null;
  referenciaMinima: number | null;
  referenciaMaxima: number | null;
  referenciaTexto: string | null;
  classificacao: 'Baixo' | 'Alto' | 'DentroDaReferencia' | 'SemReferenciaNumerica' | string;
}

interface Exame {
  id: string;
  dataColetaUtc: string;
  laboratorio: string | null;
  observacoes: string | null;
  profissional: string;
  resultados: Resultado[];
}

const useEvolucao = () => useQuery({ queryKey: ['p', 'evolucao'], queryFn: () => api<{ itens: Avaliacao[] }>('/api/portal/me/evolucao?limite=24') });
const useExames = () => useQuery({ queryKey: ['p', 'exames'], queryFn: () => api<{ exames: Exame[] }>('/api/portal/me/exames?limite=20') });

// Para cada medida, se subir e bom ou ruim (null: neutro).
const MEDIDAS: { k: keyof Avaliacao; l: string; u: string; subirBom: boolean | null }[] = [
  { k: 'pesoKg', l: 'Peso', u: 'kg', subirBom: null },
  { k: 'percentualGordura', l: 'Gordura', u: '%', subirBom: false },
  { k: 'massaMagraKg', l: 'Massa magra', u: 'kg', subirBom: true },
  { k: 'cinturaCm', l: 'Cintura', u: 'cm', subirBom: false },
  { k: 'abdomenCm', l: 'Abdômen', u: 'cm', subirBom: false },
  { k: 'quadrilCm', l: 'Quadril', u: 'cm', subirBom: null },
  { k: 'imc', l: 'IMC', u: '', subirBom: null },
];

/** Avaliacoes fisicas registradas pelo profissional. */
export function CorpoSecao() {
  const q = useEvolucao();
  const itens = q.data?.itens ?? [];
  const medidas = MEDIDAS.map((m) => {
    const serie = itens.map((a) => a[m.k] as number | null).filter((v): v is number => v != null);
    return { ...m, serie, delta: serie.length > 1 ? serie[serie.length - 1] - serie[0] : 0 };
  }).filter((m) => m.serie.length);

  if (q.isLoading) return <Skeleton className="mt-8 h-40" />;
  if (!itens.length) return null;
  const ultima = itens[itens.length - 1];

  return (
    <section className="mt-8">
      <div className="mb-3 flex items-end justify-between">
        <h2 className="font-display text-[17px] font-semibold tracking-tight">Corpo</h2>
        <span className="text-sm text-muted">
          {itens.length} {itens.length > 1 ? 'avaliações' : 'avaliação'} · última {dataCurta(ultima.dataUtc)}
        </span>
      </div>
      <div className="grid grid-cols-2 gap-2 sm:gap-3 lg:grid-cols-4">
        {medidas.map((m) => {
          const bom = m.subirBom == null || m.delta === 0 ? null : m.delta > 0 === m.subirBom;
          return (
            <Card key={m.k} className="flex flex-col p-4">
              <p className="text-[13px] text-muted">{m.l}</p>
              <p className="num mt-1 text-[24px] font-semibold leading-none">
                {n1(m.serie[m.serie.length - 1])}
                {m.u && <span className="ml-0.5 text-[13px] font-normal text-muted">{m.u}</span>}
              </p>
              {m.serie.length > 1 && (
                <>
                  <p className={clsx('num mt-1.5 flex items-center gap-1 text-[12px] font-semibold', bom == null ? 'text-muted' : bom ? 'text-ok' : 'text-warn')}>
                    {m.delta > 0 ? <TrendingUp className="h-3.5 w-3.5" /> : m.delta < 0 ? <TrendingDown className="h-3.5 w-3.5" /> : null}
                    {m.delta > 0 ? '+' : ''}
                    {n1(m.delta)} {m.u} desde {dataCurta(itens[0].dataUtc)}
                  </p>
                  <Sparkline points={m.serie} height={34} className="mt-auto pt-2" color={bom === false ? 'rgb(var(--warn))' : undefined} />
                </>
              )}
            </Card>
          );
        })}
      </div>
    </section>
  );
}

const valor = (r: Resultado) => (r.valorNumerico != null ? `${n1(r.valorNumerico)}${r.unidade ? ` ${r.unidade}` : ''}` : r.valorTexto ?? '—');
const referencia = (r: Resultado) =>
  r.referenciaTexto ??
  (r.referenciaMinima != null && r.referenciaMaxima != null
    ? `${n1(r.referenciaMinima)} a ${n1(r.referenciaMaxima)}`
    : r.referenciaMaxima != null
      ? `até ${n1(r.referenciaMaxima)}`
      : r.referenciaMinima != null
        ? `a partir de ${n1(r.referenciaMinima)}`
        : null);

/** Exames laboratoriais lancados pelo profissional, com comparacao com a coleta anterior. */
export function ExamesSecao() {
  const q = useExames();
  const exames = q.data?.exames ?? [];
  const [aberto, setAberto] = useState<string | null>(null);
  const aberta = aberto ?? exames[0]?.id ?? null;

  // Valor anterior de cada marcador, para mostrar se subiu ou desceu.
  const anterior = useMemo(() => {
    const m = new Map<string, Map<string, number>>();
    exames.forEach((e, i) => {
      const prev = new Map<string, number>();
      for (const antigo of exames.slice(i + 1)) for (const r of antigo.resultados) if (r.valorNumerico != null && !prev.has(r.marcador)) prev.set(r.marcador, r.valorNumerico);
      m.set(e.id, prev);
    });
    return m;
  }, [exames]);

  if (q.isLoading) return <Skeleton className="mt-8 h-32" />;
  if (!exames.length) return null;

  return (
    <section className="mt-8">
      <div className="mb-3 flex items-end justify-between">
        <h2 className="font-display text-[17px] font-semibold tracking-tight">Exames</h2>
        <span className="text-sm text-muted">{exames.length} {exames.length > 1 ? 'coletas' : 'coleta'}</span>
      </div>
      <Card className="divide-y divide-line/60">
        {exames.map((e) => {
          const fora = e.resultados.filter((r) => r.classificacao === 'Alto' || r.classificacao === 'Baixo').length;
          const open = aberta === e.id;
          return (
            <div key={e.id}>
              <button onClick={() => setAberto(open ? '' : e.id)} className="flex w-full items-center gap-3 p-3.5 text-left" aria-expanded={open}>
                <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-accent/10 text-accent">
                  <FlaskConical className="h-5 w-5" />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate font-semibold">Coleta de {dataCurta(e.dataColetaUtc)}</span>
                  <span className="block truncate text-[13px] text-muted">
                    {[e.laboratorio, `${e.resultados.length} marcadores`].filter(Boolean).join(' · ')}
                  </span>
                </span>
                {fora ? <Pill tone="warn">{fora} fora</Pill> : <Pill tone="ok">Na referência</Pill>}
                <ChevronDown className={clsx('h-5 w-5 shrink-0 text-faint transition', open && 'rotate-180')} />
              </button>
              {open && (
                <div className="px-3.5 pb-3.5">
                  <div className="overflow-hidden rounded-2xl border border-line/70">
                    {e.resultados.map((r) => {
                      const prev = anterior.get(e.id)?.get(r.marcador);
                      const ref = referencia(r);
                      return (
                        <div key={r.id} className="flex items-center gap-3 border-b border-line/50 px-3 py-2.5 last:border-0">
                          <div className="min-w-0 flex-1">
                            <p className="truncate text-[14px] font-semibold">{r.marcador}</p>
                            <p className="truncate text-[12px] text-muted">
                              {ref ? `Ref. ${ref}` : 'Sem referência'}
                              {prev != null && r.valorNumerico != null && ` · antes ${n1(prev)}`}
                            </p>
                          </div>
                          <span
                            className={clsx(
                              'num shrink-0 text-[15px] font-semibold',
                              r.classificacao === 'Alto' || r.classificacao === 'Baixo' ? 'text-warn' : r.classificacao === 'DentroDaReferencia' ? 'text-ok' : '',
                            )}
                          >
                            {valor(r)}
                            {(r.classificacao === 'Alto' || r.classificacao === 'Baixo') && <span className="ml-1 text-[11px] uppercase">{r.classificacao}</span>}
                          </span>
                        </div>
                      );
                    })}
                  </div>
                  {e.observacoes && <p className="mt-2 text-[13px] text-muted">{e.observacoes}</p>}
                  <p className="mt-2 text-[12px] text-faint">Lançado por {e.profissional}. Interprete sempre com seu profissional.</p>
                </div>
              )}
            </div>
          );
        })}
      </Card>
    </section>
  );
}

interface Passivo {
  totalAmostras: number;
  fontes: { fonte: string; rotulo: string; amostras: number; ultimaSincronizacaoUtc: string | null; comDadosRecentes: boolean }[];
  metricas: { metrica: string; rotulo: string; unidade: string; ultimoValor: number | null; total: number | null; variacaoPercentual7Dias: number | null }[];
  mensagemSeguranca: string;
}

const SOMADAS = ['Passos', 'SonoMinutos', 'EnergiaAtiva', 'Distancia', 'MinutosAtivos'];

/** Sinais de relogios e apps de saude (Apple Health, Health Connect, Garmin). So aparece quando ha dados. */
export function SinaisSecao() {
  const q = useQuery({ queryKey: ['p', 'passivo'], queryFn: () => api<Passivo>('/api/portal/me/monitoramento-passivo?dias=14') });
  const d = q.data;
  if (!d?.totalAmostras) return null;
  const fontes = d.fontes.filter((f) => f.amostras > 0);
  return (
    <section className="mt-8">
      <div className="mb-3 flex items-end justify-between">
        <h2 className="font-display text-[17px] font-semibold tracking-tight">Sinais conectados</h2>
        <span className="text-sm text-muted">{fontes.map((f) => f.rotulo).join(' · ')}</span>
      </div>
      <div className="grid grid-cols-2 gap-2 sm:gap-3 lg:grid-cols-4">
        {d.metricas.slice(0, 8).map((m) => {
          const v = SOMADAS.includes(m.metrica) && m.total != null ? m.total : m.ultimoValor;
          return (
            <Card key={m.metrica} className="p-4">
              <p className="text-[13px] text-muted">{m.rotulo}</p>
              <p className="num mt-1 text-[22px] font-semibold leading-none">
                {v != null ? n1(v) : '—'}
                <span className="ml-1 text-[13px] font-normal text-muted">{m.unidade}</span>
              </p>
              {m.variacaoPercentual7Dias != null && (
                <p className="num mt-1.5 text-[12px] text-muted">
                  {m.variacaoPercentual7Dias > 0 ? '+' : ''}
                  {n1(m.variacaoPercentual7Dias)}% vs. 7 dias antes
                </p>
              )}
            </Card>
          );
        })}
      </div>
      <p className="mt-2 text-[12px] text-faint">{d.mensagemSeguranca}</p>
    </section>
  );
}
