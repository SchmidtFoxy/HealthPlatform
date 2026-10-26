import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { CalendarDays, ChevronDown, FileText, FlaskConical, NotebookPen, Plus, Scale, Stethoscope, Trash2 } from 'lucide-react';
import { ReactNode, useEffect, useState } from 'react';
import { Button, Card, Field, inputCls, Pill, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta, hojeIso, n1 } from '@/lib/format';
import { inputLocal } from './CentralPage';
import { Chips } from './CuidadosPro';
import { CheckInsSecao } from '@/features/checkins';
import { Fotos, LinhaDoTempo } from './FotosLinha';

// ---------------------------------------------------------------- Tipos

interface Consulta {
  id: string;
  profissionalNome: string;
  dataHoraUtc: string;
  motivo: string | null;
  queixaPrincipal: string | null;
  evolucao: string | null;
  conduta: string | null;
  orientacoes: string | null;
  status: string;
  possuiAvaliacao: boolean;
}

interface Evolucao {
  id: string;
  profissionalNome: string;
  consultaId: string | null;
  dataHoraUtc: string;
  subjetivo: string | null;
  objetivo: string | null;
  avaliacao: string | null;
  plano: string | null;
  observacoes: string | null;
}

interface Anamnese {
  id: string;
  profissionalNome: string;
  dataUtc: string;
  objetivoAcompanhamento: string | null;
  historicoDoencas: string | null;
  historicoFamiliar: string | null;
  cirurgias: string | null;
  alergias: string | null;
  medicamentos: string | null;
  suplementos: string | null;
  tabagismo: string | null;
  etilismo: string | null;
  sonoHorasMedia: number | null;
  sonoQualidade: string | null;
  despertaDuranteNoite: boolean | null;
  estresseNivel: number | null;
  atividadeFisica: string | null;
  atividadeFisicaDiasSemana: number | null;
  habitoIntestinal: string | null;
  aguaLitrosDia: number | null;
  observacoes: string | null;
  respostasPersonalizadas: { perguntaId: string; pergunta: string; tipoResposta: string; resposta: string | null }[];
}

interface Avaliacao {
  id: string;
  dataUtc: string;
  pesoKg: number | null;
  alturaM: number | null;
  imc: number | null;
  percentualGordura: number | null;
  massaMagraKg: number | null;
  massaGordaKg: number | null;
  cinturaCm: number | null;
  abdomenCm: number | null;
  quadrilCm: number | null;
  pressaoSistolica: number | null;
  pressaoDiastolica: number | null;
  frequenciaCardiaca: number | null;
}

interface Resultado {
  id: string;
  marcadorId: string;
  marcadorNome: string;
  valorNumerico: number | null;
  valorTexto: string | null;
  unidade: string | null;
  referenciaMinima: number | null;
  referenciaMaxima: number | null;
  referenciaTexto: string | null;
  situacao: string | null;
}

interface Exame {
  id: string;
  profissionalNome: string;
  dataColetaUtc: string;
  laboratorio: string | null;
  observacoes: string | null;
  resultados: Resultado[];
}

interface Marcador {
  id: string;
  nome: string;
  categoria: string | null;
  unidadePadrao: string | null;
}

interface Relatorio {
  id: string;
  titulo: string;
  profissionalNome: string;
  dataInicioUtc: string | null;
  dataFimUtc: string | null;
  dataGeracaoUtc: string;
  conclusaoMedica: string | null;
}

type Secao = 'linha' | 'checkins' | 'consultas' | 'evolucoes' | 'anamnese' | 'avaliacoes' | 'fotos' | 'exames' | 'relatorios';

// ---------------------------------------------------------------- Utilidades

const erro = (e: unknown, padrao: string) => toast(e instanceof Error ? e.message : padrao, 'bad');
const dec = (s: string) => (s.trim() ? Number(s.replace(',', '.')) : null);
const int = (s: string) => (s.trim() ? Math.round(Number(s.replace(',', '.'))) : null);
const txt = (s: string) => s.trim() || null;
const str = (v: number | null | undefined) => (v == null ? '' : String(v));
const quando = (iso: string) => new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

const STATUS_CONSULTA: Record<string, 'neutral' | 'info' | 'ok' | 'warn' | 'bad'> = { Agendada: 'neutral', Confirmada: 'info', Realizada: 'ok', Cancelada: 'warn', Faltou: 'bad' };

function Texto({ label, value, onChange, rows = 3, placeholder }: { label: string; value: string; onChange: (v: string) => void; rows?: number; placeholder?: string }) {
  return (
    <Field label={label}>
      <textarea className={clsx(inputCls, 'h-auto py-2.5')} rows={rows} value={value} placeholder={placeholder} onChange={(e) => onChange(e.target.value)} />
    </Field>
  );
}

function Num({ label, value, onChange, step = 'any' }: { label: string; value: string; onChange: (v: string) => void; step?: string }) {
  return (
    <Field label={label}>
      <input type="number" inputMode="decimal" step={step} min="0" className={inputCls} value={value} onChange={(e) => onChange(e.target.value)} />
    </Field>
  );
}

function Linha({ l, v }: { l: string; v: ReactNode }) {
  if (v == null || v === '') return null;
  return (
    <div className="py-1.5">
      <p className="text-[12px] font-semibold uppercase tracking-wide text-faint">{l}</p>
      <p className="whitespace-pre-line text-[14px]">{v}</p>
    </div>
  );
}

function Topo({ titulo, acao, onAcao }: { titulo: string; acao: string; onAcao: () => void }) {
  return (
    <div className="mb-3 flex items-center justify-between gap-3">
      <p className="text-sm text-muted">{titulo}</p>
      <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={onAcao}>
        {acao}
      </Button>
    </div>
  );
}

function Vazio({ icon, children }: { icon: ReactNode; children: ReactNode }) {
  return (
    <Card className="flex items-start gap-3 p-4 text-sm text-muted">
      <span className="text-faint">{icon}</span>
      <span>{children}</span>
    </Card>
  );
}

/** Item expansivel de uma lista do prontuario. */
function Item({ icon, titulo, sub, extra, children, aberto, onToggle }: { icon: ReactNode; titulo: ReactNode; sub?: ReactNode; extra?: ReactNode; children: ReactNode; aberto: boolean; onToggle: () => void }) {
  return (
    <div className="[overflow-wrap:anywhere]">
      <button onClick={onToggle} aria-expanded={aberto} className="flex w-full items-start gap-3 p-3.5 text-left transition hover:bg-raised/40">
        <span className="mt-0.5 shrink-0 text-accent">{icon}</span>
        <span className="min-w-0 flex-1">
          <span className="block font-semibold">{titulo}</span>
          {sub && <span className="block text-[13px] text-muted">{sub}</span>}
        </span>
        {extra}
        <ChevronDown className={clsx('mt-0.5 h-5 w-5 shrink-0 text-faint transition', aberto && 'rotate-180')} />
      </button>
      {aberto && <div className="px-3.5 pb-3.5 pl-11">{children}</div>}
    </div>
  );
}

function Rodape({ salvar, pendente, valido = true, rotulo = 'Salvar' }: { salvar: () => void; pendente: boolean; valido?: boolean; rotulo?: string }) {
  return (
    <Button size="lg" className="w-full" disabled={!valido} loading={pendente} onClick={salvar}>
      {rotulo}
    </Button>
  );
}

// ---------------------------------------------------------------- Aba

/** Aba "Prontuario" da ficha: consultas, evolucoes (SOAP), anamnese, avaliacoes, fotos, exames, relatorios e linha do tempo. */
export function AbaProntuario({ id }: { id: string }) {
  const [secao, setSecao] = useState<Secao>('consultas');
  return (
    <div className="mt-6">
      <div className="-mx-gutter mb-4 overflow-x-auto px-gutter pb-1">
        <Chips<Secao>
          value={secao}
          onChange={setSecao}
          options={[
            { value: 'consultas', label: 'Consultas' },
            { value: 'evolucoes', label: 'Evoluções' },
            { value: 'anamnese', label: 'Anamnese' },
            { value: 'avaliacoes', label: 'Avaliações' },
            { value: 'checkins', label: 'Check-ins' },
            { value: 'fotos', label: 'Fotos' },
            { value: 'exames', label: 'Exames' },
            { value: 'relatorios', label: 'Relatórios' },
            { value: 'linha', label: 'Linha do tempo' },
          ]}
        />
      </div>
      {secao === 'consultas' && <Consultas id={id} />}
      {secao === 'evolucoes' && <Evolucoes id={id} />}
      {secao === 'anamnese' && <Anamneses id={id} />}
      {secao === 'avaliacoes' && <Avaliacoes id={id} />}
      {secao === 'exames' && <Exames id={id} />}
      {secao === 'relatorios' && <Relatorios id={id} />}
      {secao === 'fotos' && <Fotos id={id} />}
      {secao === 'checkins' && <CheckInsSecao paciente={id} />}
      {secao === 'linha' && <LinhaDoTempo id={id} />}
    </div>
  );
}

const useConsultas = (id: string) => useQuery({ queryKey: ['pro', 'pront', 'consultas', id], queryFn: () => api<Consulta[]>(`/api/pacientes/${id}/consultas`) });

// ---------------------------------------------------------------- Consultas

function Consultas({ id }: { id: string }) {
  const q = useConsultas(id);
  const [aberta, setAberta] = useState<string | null>(null);
  const [editar, setEditar] = useState<Consulta | 'nova' | null>(null);
  const lista = [...(q.data ?? [])].sort((a, b) => b.dataHoraUtc.localeCompare(a.dataHoraUtc));
  return (
    <>
      <Topo titulo={`${lista.length} ${lista.length === 1 ? 'consulta' : 'consultas'}`} acao="Registrar atendimento" onAcao={() => setEditar('nova')} />
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : !lista.length ? (
        <Vazio icon={<Stethoscope className="h-5 w-5" />}>Nenhuma consulta ainda. Registre um atendimento com queixa, evolução, conduta e orientações.</Vazio>
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((c) => (
            <Item
              key={c.id}
              icon={<Stethoscope className="h-5 w-5" />}
              titulo={c.motivo ?? 'Consulta'}
              sub={`${quando(c.dataHoraUtc)} · ${c.profissionalNome}`}
              extra={<Pill tone={STATUS_CONSULTA[c.status] ?? 'neutral'}>{c.status}</Pill>}
              aberto={aberta === c.id}
              onToggle={() => setAberta(aberta === c.id ? null : c.id)}
            >
              <Linha l="Queixa principal" v={c.queixaPrincipal} />
              <Linha l="Evolução" v={c.evolucao} />
              <Linha l="Conduta" v={c.conduta} />
              <Linha l="Orientações" v={c.orientacoes} />
              {!c.queixaPrincipal && !c.evolucao && !c.conduta && !c.orientacoes && <p className="text-sm text-muted">Sem anotações clínicas.</p>}
              <Button size="sm" variant="outline" className="mt-2" onClick={() => setEditar(c)}>
                Editar atendimento
              </Button>
            </Item>
          ))}
        </Card>
      )}
      <ConsultaSheet id={id} item={editar} onClose={() => setEditar(null)} />
    </>
  );
}

function ConsultaSheet({ id, item, onClose }: { id: string; item: Consulta | 'nova' | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [quandoL, setQuando] = useState('');
  const [status, setStatus] = useState('Realizada');
  const [motivo, setMotivo] = useState('');
  const [queixa, setQueixa] = useState('');
  const [evolucao, setEvolucao] = useState('');
  const [conduta, setConduta] = useState('');
  const [orientacoes, setOrientacoes] = useState('');
  useEffect(() => {
    if (!item) return;
    const c = item === 'nova' ? null : item;
    setQuando(inputLocal(c ? new Date(c.dataHoraUtc) : new Date()));
    setStatus(c?.status ?? 'Realizada');
    setMotivo(c?.motivo ?? '');
    setQueixa(c?.queixaPrincipal ?? '');
    setEvolucao(c?.evolucao ?? '');
    setConduta(c?.conduta ?? '');
    setOrientacoes(c?.orientacoes ?? '');
  }, [item]);
  const salvar = useMutation({
    mutationFn: () => {
      const json = { dataHoraUtc: new Date(quandoL).toISOString(), motivo: txt(motivo), queixaPrincipal: txt(queixa), evolucao: txt(evolucao), conduta: txt(conduta), orientacoes: txt(orientacoes), status };
      return item === 'nova' ? api(`/api/pacientes/${id}/consultas`, { method: 'POST', json }) : api(`/api/consultas/${(item as Consulta).id}`, { method: 'PUT', json });
    },
    onSuccess: () => {
      toast('Atendimento salvo.');
      qc.invalidateQueries({ queryKey: ['pro', 'pront', 'consultas', id] });
      qc.invalidateQueries({ queryKey: ['pro', 'agenda'] });
      qc.invalidateQueries({ queryKey: ['pro', 'dashboard'] });
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível salvar. Só quem atendeu pode editar a consulta.'),
  });
  return (
    <Sheet open={!!item} onClose={onClose} tall title={item === 'nova' ? 'Registrar atendimento' : 'Editar atendimento'} footer={<Rodape salvar={() => salvar.mutate()} pendente={salvar.isPending} valido={!!quandoL} />}>
      <div className="flex flex-col gap-4">
        <Field label="Data e hora">
          <input type="datetime-local" className={inputCls} value={quandoL} onChange={(e) => setQuando(e.target.value)} />
        </Field>
        <Field label="Situação" group>
          <Chips value={status} onChange={setStatus} options={Object.keys(STATUS_CONSULTA).map((s) => ({ value: s, label: s }))} />
        </Field>
        <Field label="Motivo">
          <input className={inputCls} value={motivo} maxLength={200} placeholder="Ex.: Retorno de 30 dias" onChange={(e) => setMotivo(e.target.value)} />
        </Field>
        <Texto label="Queixa principal" value={queixa} onChange={setQueixa} />
        <Texto label="Evolução" value={evolucao} onChange={setEvolucao} rows={4} />
        <Texto label="Conduta" value={conduta} onChange={setConduta} rows={4} />
        <Texto label="Orientações ao paciente" value={orientacoes} onChange={setOrientacoes} />
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Evolucoes (SOAP)

function Evolucoes({ id }: { id: string }) {
  const q = useQuery({ queryKey: ['pro', 'pront', 'evolucoes', id], queryFn: () => api<Evolucao[]>(`/api/pacientes/${id}/evolucoes`) });
  const [aberta, setAberta] = useState<string | null>(null);
  const [editar, setEditar] = useState<Evolucao | 'nova' | null>(null);
  const lista = [...(q.data ?? [])].sort((a, b) => b.dataHoraUtc.localeCompare(a.dataHoraUtc));
  return (
    <>
      <Topo titulo={`${lista.length} ${lista.length === 1 ? 'evolução' : 'evoluções'}`} acao="Nova evolução" onAcao={() => setEditar('nova')} />
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : !lista.length ? (
        <Vazio icon={<NotebookPen className="h-5 w-5" />}>Nenhuma evolução registrada. Use o formato SOAP: subjetivo, objetivo, avaliação e plano.</Vazio>
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((x) => (
            <Item
              key={x.id}
              icon={<NotebookPen className="h-5 w-5" />}
              titulo={quando(x.dataHoraUtc)}
              sub={[x.profissionalNome, (x.avaliacao ?? x.subjetivo ?? '').slice(0, 80)].filter(Boolean).join(' · ')}
              aberto={aberta === x.id}
              onToggle={() => setAberta(aberta === x.id ? null : x.id)}
            >
              <Linha l="S · Subjetivo" v={x.subjetivo} />
              <Linha l="O · Objetivo" v={x.objetivo} />
              <Linha l="A · Avaliação" v={x.avaliacao} />
              <Linha l="P · Plano" v={x.plano} />
              <Linha l="Observações" v={x.observacoes} />
              <Button size="sm" variant="outline" className="mt-2" onClick={() => setEditar(x)}>
                Editar evolução
              </Button>
            </Item>
          ))}
        </Card>
      )}
      <EvolucaoSheet id={id} item={editar} onClose={() => setEditar(null)} />
    </>
  );
}

function EvolucaoSheet({ id, item, onClose }: { id: string; item: Evolucao | 'nova' | null; onClose: () => void }) {
  const qc = useQueryClient();
  const consultas = useConsultas(id).data ?? [];
  const [quandoL, setQuando] = useState('');
  const [consultaId, setConsultaId] = useState('');
  const [s, setS] = useState('');
  const [o, setO] = useState('');
  const [a, setA] = useState('');
  const [p, setP] = useState('');
  const [obs, setObs] = useState('');
  useEffect(() => {
    if (!item) return;
    const x = item === 'nova' ? null : item;
    setQuando(inputLocal(x ? new Date(x.dataHoraUtc) : new Date()));
    setConsultaId(x?.consultaId ?? '');
    setS(x?.subjetivo ?? '');
    setO(x?.objetivo ?? '');
    setA(x?.avaliacao ?? '');
    setP(x?.plano ?? '');
    setObs(x?.observacoes ?? '');
  }, [item]);
  const valido = !!quandoL && !!(s.trim() || o.trim() || a.trim() || p.trim() || obs.trim());
  const salvar = useMutation({
    mutationFn: () => {
      const json = { consultaId: consultaId || null, dataHoraUtc: new Date(quandoL).toISOString(), subjetivo: txt(s), objetivo: txt(o), avaliacao: txt(a), plano: txt(p), observacoes: txt(obs) };
      return item === 'nova' ? api(`/api/pacientes/${id}/evolucoes`, { method: 'POST', json }) : api(`/api/evolucoes/${(item as Evolucao).id}`, { method: 'PUT', json });
    },
    onSuccess: () => {
      toast('Evolução salva.');
      qc.invalidateQueries({ queryKey: ['pro', 'pront', 'evolucoes', id] });
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível salvar a evolução.'),
  });
  return (
    <Sheet open={!!item} onClose={onClose} tall title={item === 'nova' ? 'Nova evolução' : 'Editar evolução'} footer={<Rodape salvar={() => salvar.mutate()} pendente={salvar.isPending} valido={valido} />}>
      <div className="flex flex-col gap-4">
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Data e hora">
            <input type="datetime-local" className={inputCls} value={quandoL} onChange={(e) => setQuando(e.target.value)} />
          </Field>
          <Field label="Consulta (opcional)">
            <select className={inputCls} value={consultaId} onChange={(e) => setConsultaId(e.target.value)}>
              <option value="">Sem vínculo</option>
              {consultas.map((c) => (
                <option key={c.id} value={c.id}>
                  {dataCurta(c.dataHoraUtc)} · {c.motivo ?? 'Consulta'}
                </option>
              ))}
            </select>
          </Field>
        </div>
        <Texto label="S · Subjetivo (o que o paciente relata)" value={s} onChange={setS} />
        <Texto label="O · Objetivo (o que você observou e mediu)" value={o} onChange={setO} />
        <Texto label="A · Avaliação" value={a} onChange={setA} />
        <Texto label="P · Plano" value={p} onChange={setP} />
        <Texto label="Observações" value={obs} onChange={setObs} rows={2} />
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Anamnese

const CAMPOS_ANAMNESE: { k: keyof Anamnese; l: string }[] = [
  { k: 'objetivoAcompanhamento', l: 'Objetivo do acompanhamento' },
  { k: 'historicoDoencas', l: 'Histórico de doenças' },
  { k: 'historicoFamiliar', l: 'Histórico familiar' },
  { k: 'cirurgias', l: 'Cirurgias' },
  { k: 'alergias', l: 'Alergias' },
  { k: 'medicamentos', l: 'Medicamentos' },
  { k: 'suplementos', l: 'Suplementos' },
  { k: 'tabagismo', l: 'Tabagismo' },
  { k: 'etilismo', l: 'Etilismo' },
  { k: 'sonoQualidade', l: 'Qualidade do sono' },
  { k: 'atividadeFisica', l: 'Atividade física' },
  { k: 'habitoIntestinal', l: 'Hábito intestinal' },
  { k: 'observacoes', l: 'Observações' },
];

function Anamneses({ id }: { id: string }) {
  const q = useQuery({ queryKey: ['pro', 'pront', 'anamneses', id], queryFn: () => api<Anamnese[]>(`/api/pacientes/${id}/anamneses`) });
  const [editar, setEditar] = useState<{ base: Anamnese | null; editando: boolean } | null>(null);
  const [aberta, setAberta] = useState<string | null>(null);
  const lista = [...(q.data ?? [])].sort((a, b) => b.dataUtc.localeCompare(a.dataUtc));
  const atual = aberta ?? lista[0]?.id ?? null;
  return (
    <>
      <Topo titulo={lista.length ? `Última em ${dataCurta(lista[0].dataUtc)}` : 'Sem anamnese'} acao="Nova anamnese" onAcao={() => setEditar({ base: lista[0] ?? null, editando: false })} />
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : !lista.length ? (
        <Vazio icon={<FileText className="h-5 w-5" />}>Nenhuma anamnese. Registre histórico, hábitos, sono, estresse e o objetivo do acompanhamento.</Vazio>
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((x) => (
            <Item
              key={x.id}
              icon={<FileText className="h-5 w-5" />}
              titulo={`Anamnese de ${dataCurta(x.dataUtc)}`}
              sub={[x.profissionalNome, x.objetivoAcompanhamento].filter(Boolean).join(' · ')}
              aberto={atual === x.id}
              onToggle={() => setAberta(atual === x.id ? '' : x.id)}
            >
              <div className="grid gap-x-6 sm:grid-cols-2">
                {CAMPOS_ANAMNESE.map((c) => (
                  <Linha key={c.k} l={c.l} v={x[c.k] as string | null} />
                ))}
                <Linha l="Sono" v={x.sonoHorasMedia != null ? `${n1(x.sonoHorasMedia)} h por noite${x.despertaDuranteNoite ? ', desperta à noite' : ''}` : null} />
                <Linha l="Estresse" v={x.estresseNivel != null ? `${x.estresseNivel} de 10` : null} />
                <Linha l="Treinos por semana" v={x.atividadeFisicaDiasSemana != null ? `${x.atividadeFisicaDiasSemana} dias` : null} />
                <Linha l="Água" v={x.aguaLitrosDia != null ? `${n1(x.aguaLitrosDia)} L por dia` : null} />
                {x.respostasPersonalizadas.map((r) => (
                  <Linha key={r.perguntaId} l={r.pergunta} v={r.resposta} />
                ))}
              </div>
              <Button size="sm" variant="outline" className="mt-2" onClick={() => setEditar({ base: x, editando: true })}>
                Editar anamnese
              </Button>
            </Item>
          ))}
        </Card>
      )}
      <AnamneseSheet id={id} estado={editar} onClose={() => setEditar(null)} />
    </>
  );
}

type FormAnamnese = Record<string, string>;
const NUMERICOS_ANAMNESE = ['sonoHorasMedia', 'estresseNivel', 'atividadeFisicaDiasSemana', 'aguaLitrosDia'];

function AnamneseSheet({ id, estado, onClose }: { id: string; estado: { base: Anamnese | null; editando: boolean } | null; onClose: () => void }) {
  const qc = useQueryClient();
  const perguntas = useQuery({ queryKey: ['pro', 'perguntas-anamnese'], queryFn: () => api<{ id: string; texto: string; tipoResposta: string; opcoes: string[] }[]>('/api/anamnese/perguntas'), enabled: !!estado }).data ?? [];
  const [f, setF] = useState<FormAnamnese>({});
  const [desperta, setDesperta] = useState<'sim' | 'nao' | ''>('');
  const [respostas, setRespostas] = useState<Record<string, string>>({});
  useEffect(() => {
    if (!estado) return;
    // Nova anamnese parte da ultima, para o profissional so atualizar o que mudou.
    const b = estado.base;
    const novo: FormAnamnese = { data: b && estado.editando ? b.dataUtc.slice(0, 10) : hojeIso() };
    for (const c of CAMPOS_ANAMNESE) novo[c.k] = (b?.[c.k] as string | null) ?? '';
    for (const k of NUMERICOS_ANAMNESE) novo[k] = str(b?.[k as keyof Anamnese] as number | null);
    setF(novo);
    setDesperta(b?.despertaDuranteNoite == null ? '' : b.despertaDuranteNoite ? 'sim' : 'nao');
    setRespostas(Object.fromEntries((b?.respostasPersonalizadas ?? []).map((r) => [r.perguntaId, r.resposta ?? ''])));
  }, [estado]);
  const set = (k: string) => (v: string) => setF((x) => ({ ...x, [k]: v }));
  const salvar = useMutation({
    mutationFn: () => {
      const json: Record<string, unknown> = { consultaId: null, dataUtc: new Date(`${f.data}T12:00:00`).toISOString() };
      for (const c of CAMPOS_ANAMNESE) json[c.k] = txt(f[c.k] ?? '');
      json.sonoHorasMedia = dec(f.sonoHorasMedia ?? '');
      json.aguaLitrosDia = dec(f.aguaLitrosDia ?? '');
      json.estresseNivel = int(f.estresseNivel ?? '');
      json.atividadeFisicaDiasSemana = int(f.atividadeFisicaDiasSemana ?? '');
      json.despertaDuranteNoite = desperta === '' ? null : desperta === 'sim';
      json.respostasPersonalizadas = perguntas.length ? perguntas.map((p) => ({ perguntaId: p.id, resposta: txt(respostas[p.id] ?? '') })) : null;
      return estado!.editando ? api(`/api/anamneses/${estado!.base!.id}`, { method: 'PUT', json }) : api(`/api/pacientes/${id}/anamneses`, { method: 'POST', json });
    },
    onSuccess: () => {
      toast('Anamnese salva.');
      qc.invalidateQueries({ queryKey: ['pro', 'pront', 'anamneses', id] });
      qc.invalidateQueries({ queryKey: ['pro', 'resumo', id] });
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível salvar a anamnese.'),
  });
  return (
    <Sheet
      open={!!estado}
      onClose={onClose}
      tall
      title={estado?.editando ? 'Editar anamnese' : 'Nova anamnese'}
      footer={<Rodape salvar={() => salvar.mutate()} pendente={salvar.isPending} valido={!!f.data && (!f.estresseNivel || (Number(f.estresseNivel) >= 0 && Number(f.estresseNivel) <= 10))} />}
    >
      <div className="flex flex-col gap-4">
        {!estado?.editando && estado?.base && <p className="rounded-xl bg-accent/10 px-3 py-2 text-[13px]">Preenchida com a anamnese de {dataCurta(estado.base.dataUtc)}. Atualize só o que mudou.</p>}
        <Field label="Data">
          <input type="date" className={inputCls} value={f.data ?? ''} onChange={(e) => set('data')(e.target.value)} />
        </Field>
        <Texto label="Objetivo do acompanhamento" value={f.objetivoAcompanhamento ?? ''} onChange={set('objetivoAcompanhamento')} rows={2} />
        <Texto label="Histórico de doenças" value={f.historicoDoencas ?? ''} onChange={set('historicoDoencas')} rows={2} />
        <Texto label="Histórico familiar" value={f.historicoFamiliar ?? ''} onChange={set('historicoFamiliar')} rows={2} />
        <div className="grid gap-3 sm:grid-cols-2">
          <Texto label="Cirurgias" value={f.cirurgias ?? ''} onChange={set('cirurgias')} rows={2} />
          <Texto label="Alergias" value={f.alergias ?? ''} onChange={set('alergias')} rows={2} />
          <Texto label="Medicamentos" value={f.medicamentos ?? ''} onChange={set('medicamentos')} rows={2} />
          <Texto label="Suplementos" value={f.suplementos ?? ''} onChange={set('suplementos')} rows={2} />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Tabagismo">
            <input className={inputCls} value={f.tabagismo ?? ''} onChange={(e) => set('tabagismo')(e.target.value)} />
          </Field>
          <Field label="Etilismo">
            <input className={inputCls} value={f.etilismo ?? ''} onChange={(e) => set('etilismo')(e.target.value)} />
          </Field>
          <Num label="Sono (h por noite)" value={f.sonoHorasMedia ?? ''} onChange={set('sonoHorasMedia')} step="0.5" />
          <Field label="Qualidade do sono">
            <input className={inputCls} value={f.sonoQualidade ?? ''} onChange={(e) => set('sonoQualidade')(e.target.value)} />
          </Field>
        </div>
        <Field label="Desperta durante a noite?" group>
          <Chips
            value={desperta}
            onChange={setDesperta}
            options={[
              { value: 'sim', label: 'Sim' },
              { value: 'nao', label: 'Não' },
              { value: '', label: 'Não informado' },
            ]}
          />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Num label="Estresse (0 a 10)" value={f.estresseNivel ?? ''} onChange={set('estresseNivel')} step="1" />
          <Num label="Água (L por dia)" value={f.aguaLitrosDia ?? ''} onChange={set('aguaLitrosDia')} step="0.1" />
          <Field label="Atividade física">
            <input className={inputCls} value={f.atividadeFisica ?? ''} onChange={(e) => set('atividadeFisica')(e.target.value)} />
          </Field>
          <Num label="Dias por semana" value={f.atividadeFisicaDiasSemana ?? ''} onChange={set('atividadeFisicaDiasSemana')} step="1" />
        </div>
        <Texto label="Hábito intestinal" value={f.habitoIntestinal ?? ''} onChange={set('habitoIntestinal')} rows={2} />
        {perguntas.map((p) => (
          <Texto key={p.id} label={p.texto} value={respostas[p.id] ?? ''} onChange={(v) => setRespostas((r) => ({ ...r, [p.id]: v }))} rows={2} />
        ))}
        <Texto label="Observações" value={f.observacoes ?? ''} onChange={set('observacoes')} />
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Avaliacoes

const MEDIDAS: { k: keyof Avaliacao; l: string; u: string }[] = [
  { k: 'pesoKg', l: 'Peso', u: 'kg' },
  { k: 'imc', l: 'IMC', u: '' },
  { k: 'percentualGordura', l: 'Gordura', u: '%' },
  { k: 'massaMagraKg', l: 'Massa magra', u: 'kg' },
  { k: 'massaGordaKg', l: 'Massa gorda', u: 'kg' },
  { k: 'cinturaCm', l: 'Cintura', u: 'cm' },
  { k: 'abdomenCm', l: 'Abdômen', u: 'cm' },
  { k: 'quadrilCm', l: 'Quadril', u: 'cm' },
  { k: 'frequenciaCardiaca', l: 'FC repouso', u: 'bpm' },
];

function Avaliacoes({ id }: { id: string }) {
  const q = useQuery({ queryKey: ['pro', 'pront', 'avaliacoes', id], queryFn: () => api<Avaliacao[]>(`/api/pacientes/${id}/avaliacoes`) });
  const [editar, setEditar] = useState<Avaliacao | 'nova' | null>(null);
  const lista = [...(q.data ?? [])].sort((a, b) => b.dataUtc.localeCompare(a.dataUtc));
  return (
    <>
      <Topo titulo={`${lista.length} ${lista.length === 1 ? 'avaliação' : 'avaliações'}`} acao="Nova avaliação" onAcao={() => setEditar('nova')} />
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : !lista.length ? (
        <Vazio icon={<Scale className="h-5 w-5" />}>Nenhuma avaliação física. Registre peso, composição corporal, medidas e sinais vitais.</Vazio>
      ) : (
        <div className="flex flex-col gap-3">
          {lista.map((a, i) => {
            const ant = lista[i + 1];
            return (
              <Card key={a.id} className="p-4">
                <div className="mb-2 flex items-center justify-between gap-3">
                  <p className="font-semibold">{dataCurta(a.dataUtc)}</p>
                  <button onClick={() => setEditar(a)} className="text-sm font-semibold text-accent">
                    Editar
                  </button>
                </div>
                <div className="grid grid-cols-3 gap-x-3 gap-y-2 sm:grid-cols-5">
                  {MEDIDAS.filter((m) => a[m.k] != null).map((m) => {
                    const v = a[m.k] as number;
                    const d = ant && ant[m.k] != null ? v - (ant[m.k] as number) : null;
                    return (
                      <div key={m.k}>
                        <p className="text-[12px] text-muted">{m.l}</p>
                        <p className="num font-semibold">
                          {n1(v)}
                          <span className="ml-0.5 text-[12px] font-normal text-muted">{m.u}</span>
                        </p>
                        {d != null && d !== 0 && (
                          <p className="num text-[11px] text-faint">
                            {d > 0 ? '+' : ''}
                            {n1(d)}
                          </p>
                        )}
                      </div>
                    );
                  })}
                  {a.pressaoSistolica != null && (
                    <div>
                      <p className="text-[12px] text-muted">Pressão</p>
                      <p className="num font-semibold">
                        {a.pressaoSistolica}/{a.pressaoDiastolica ?? '—'}
                      </p>
                    </div>
                  )}
                </div>
              </Card>
            );
          })}
        </div>
      )}
      <AvaliacaoSheet id={id} item={editar} ultima={lista[0] ?? null} onClose={() => setEditar(null)} />
    </>
  );
}

const CAMPOS_AVALIACAO: { k: keyof Avaliacao; l: string; step?: string }[] = [
  { k: 'pesoKg', l: 'Peso (kg)', step: '0.1' },
  { k: 'alturaM', l: 'Altura (m)', step: '0.01' },
  { k: 'percentualGordura', l: 'Gordura (%)', step: '0.1' },
  { k: 'massaMagraKg', l: 'Massa magra (kg)', step: '0.1' },
  { k: 'massaGordaKg', l: 'Massa gorda (kg)', step: '0.1' },
  { k: 'cinturaCm', l: 'Cintura (cm)', step: '0.1' },
  { k: 'abdomenCm', l: 'Abdômen (cm)', step: '0.1' },
  { k: 'quadrilCm', l: 'Quadril (cm)', step: '0.1' },
  { k: 'pressaoSistolica', l: 'Pressão sistólica', step: '1' },
  { k: 'pressaoDiastolica', l: 'Pressão diastólica', step: '1' },
  { k: 'frequenciaCardiaca', l: 'FC de repouso (bpm)', step: '1' },
];
const INTEIROS_AVALIACAO = ['pressaoSistolica', 'pressaoDiastolica', 'frequenciaCardiaca'];

function AvaliacaoSheet({ id, item, ultima, onClose }: { id: string; item: Avaliacao | 'nova' | null; ultima: Avaliacao | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [data, setData] = useState('');
  const [f, setF] = useState<Record<string, string>>({});
  useEffect(() => {
    if (!item) return;
    const a = item === 'nova' ? null : item;
    setData(a ? a.dataUtc.slice(0, 10) : hojeIso());
    // Altura raramente muda: na nova avaliacao, vem da ultima.
    setF(Object.fromEntries(CAMPOS_AVALIACAO.map((c) => [c.k, a ? str(a[c.k] as number | null) : c.k === 'alturaM' ? str(ultima?.alturaM) : ''])));
  }, [item, ultima]);
  const algum = CAMPOS_AVALIACAO.some((c) => (f[c.k] ?? '').trim());
  const salvar = useMutation({
    mutationFn: () => {
      const json: Record<string, unknown> = { consultaId: null, dataUtc: new Date(`${data}T12:00:00`).toISOString() };
      for (const c of CAMPOS_AVALIACAO) json[c.k] = INTEIROS_AVALIACAO.includes(c.k) ? int(f[c.k] ?? '') : dec(f[c.k] ?? '');
      return item === 'nova' ? api(`/api/pacientes/${id}/avaliacoes`, { method: 'POST', json }) : api(`/api/avaliacoes/${(item as Avaliacao).id}`, { method: 'PUT', json });
    },
    onSuccess: () => {
      toast('Avaliação salva.');
      qc.invalidateQueries({ queryKey: ['pro', 'pront', 'avaliacoes', id] });
      qc.invalidateQueries({ queryKey: ['pro', 'resumo', id] });
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível salvar a avaliação.'),
  });
  return (
    <Sheet open={!!item} onClose={onClose} tall title={item === 'nova' ? 'Nova avaliação' : 'Editar avaliação'} footer={<Rodape salvar={() => salvar.mutate()} pendente={salvar.isPending} valido={!!data && algum} />}>
      <div className="flex flex-col gap-4">
        <Field label="Data">
          <input type="date" className={inputCls} value={data} onChange={(e) => setData(e.target.value)} />
        </Field>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          {CAMPOS_AVALIACAO.map((c) => (
            <Num key={c.k} label={c.l} value={f[c.k] ?? ''} step={c.step} onChange={(v) => setF((x) => ({ ...x, [c.k]: v }))} />
          ))}
        </div>
        <p className="text-[12px] text-faint">O IMC é calculado a partir do peso e da altura.</p>
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Exames

function Exames({ id }: { id: string }) {
  const q = useQuery({ queryKey: ['pro', 'pront', 'exames', id], queryFn: () => api<Exame[]>(`/api/pacientes/${id}/exames`) });
  const [aberto, setAberto] = useState<string | null>(null);
  const [editar, setEditar] = useState<Exame | 'novo' | null>(null);
  const lista = [...(q.data ?? [])].sort((a, b) => b.dataColetaUtc.localeCompare(a.dataColetaUtc));
  const atual = aberto ?? lista[0]?.id ?? null;
  return (
    <>
      <Topo titulo={`${lista.length} ${lista.length === 1 ? 'coleta' : 'coletas'}`} acao="Lançar exame" onAcao={() => setEditar('novo')} />
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : !lista.length ? (
        <Vazio icon={<FlaskConical className="h-5 w-5" />}>Nenhum exame lançado. Os resultados aparecem para o paciente em Progresso, com a referência de cada marcador.</Vazio>
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((e) => {
            const fora = e.resultados.filter((r) => r.situacao === 'Alto' || r.situacao === 'Baixo').length;
            return (
              <Item
                key={e.id}
                icon={<FlaskConical className="h-5 w-5" />}
                titulo={`Coleta de ${dataCurta(e.dataColetaUtc)}`}
                sub={[e.laboratorio, `${e.resultados.length} marcadores`].filter(Boolean).join(' · ')}
                extra={fora ? <Pill tone="warn">{fora} fora</Pill> : undefined}
                aberto={atual === e.id}
                onToggle={() => setAberto(atual === e.id ? '' : e.id)}
              >
                <div className="overflow-hidden rounded-xl border border-line/70">
                  {e.resultados.map((r) => (
                    <div key={r.id} className="flex items-center gap-3 border-b border-line/50 px-3 py-2 last:border-0">
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-[14px] font-semibold">{r.marcadorNome}</p>
                        <p className="truncate text-[12px] text-muted">
                          {r.referenciaTexto ?? (r.referenciaMinima != null || r.referenciaMaxima != null ? `Ref. ${r.referenciaMinima != null ? n1(r.referenciaMinima) : '…'} a ${r.referenciaMaxima != null ? n1(r.referenciaMaxima) : '…'}` : 'Sem referência')}
                        </p>
                      </div>
                      <span className={clsx('num shrink-0 font-semibold', r.situacao === 'Alto' || r.situacao === 'Baixo' ? 'text-warn' : '')}>
                        {r.valorNumerico != null ? n1(r.valorNumerico) : r.valorTexto} <span className="text-[12px] font-normal text-muted">{r.unidade}</span>
                      </span>
                    </div>
                  ))}
                </div>
                {e.observacoes && <p className="mt-2 text-[13px] text-muted">{e.observacoes}</p>}
                <Button size="sm" variant="outline" className="mt-2" onClick={() => setEditar(e)}>
                  Editar exame
                </Button>
              </Item>
            );
          })}
        </Card>
      )}
      <ExameSheet id={id} item={editar} onClose={() => setEditar(null)} />
    </>
  );
}

interface LinhaExame {
  chave: string;
  marcadorId: string;
  valor: string;
  unidade: string;
  min: string;
  max: string;
}

let seq = 0;
const novaLinha = (): LinhaExame => ({ chave: `l${++seq}`, marcadorId: '', valor: '', unidade: '', min: '', max: '' });

function ExameSheet({ id, item, onClose }: { id: string; item: Exame | 'novo' | null; onClose: () => void }) {
  const qc = useQueryClient();
  const marcadores = useQuery({ queryKey: ['pro', 'marcadores'], queryFn: () => api<Marcador[]>('/api/exames/marcadores'), enabled: !!item, staleTime: 5 * 60_000 });
  const [data, setData] = useState('');
  const [lab, setLab] = useState('');
  const [obs, setObs] = useState('');
  const [linhas, setLinhas] = useState<LinhaExame[]>([]);
  const [novoMarcador, setNovoMarcador] = useState<{ nome: string; unidade: string; categoria: string; linha: string } | null>(null);
  useEffect(() => {
    if (!item) return;
    const e = item === 'novo' ? null : item;
    setData(e ? e.dataColetaUtc.slice(0, 10) : hojeIso());
    setLab(e?.laboratorio ?? '');
    setObs(e?.observacoes ?? '');
    setLinhas(
      e?.resultados.length
        ? e.resultados.map((r) => ({ ...novaLinha(), marcadorId: r.marcadorId, valor: r.valorNumerico != null ? String(r.valorNumerico) : r.valorTexto ?? '', unidade: r.unidade ?? '', min: str(r.referenciaMinima), max: str(r.referenciaMaxima) }))
        : [novaLinha(), novaLinha(), novaLinha()],
    );
  }, [item]);
  const mudar = (chave: string, patch: Partial<LinhaExame>) => setLinhas((ls) => ls.map((l) => (l.chave === chave ? { ...l, ...patch } : l)));
  const escolher = (chave: string, marcadorId: string) => {
    if (marcadorId === '__novo') {
      setNovoMarcador({ nome: '', unidade: '', categoria: '', linha: chave });
      return;
    }
    const m = marcadores.data?.find((x) => x.id === marcadorId);
    setLinhas((ls) => ls.map((l) => (l.chave === chave ? { ...l, marcadorId, unidade: l.unidade || m?.unidadePadrao || '' } : l)));
  };
  const criarMarcador = useMutation({
    mutationFn: () => api<Marcador>('/api/exames/marcadores', { method: 'POST', json: { nome: novoMarcador!.nome.trim(), categoria: txt(novoMarcador!.categoria), unidadePadrao: txt(novoMarcador!.unidade) } }),
    onSuccess: async (m) => {
      await qc.invalidateQueries({ queryKey: ['pro', 'marcadores'] });
      mudar(novoMarcador!.linha, { marcadorId: m.id, unidade: m.unidadePadrao ?? '' });
      setNovoMarcador(null);
    },
    onError: (e) => erro(e, 'Não foi possível criar o marcador.'),
  });
  const preenchidas = linhas.filter((l) => l.marcadorId && l.valor.trim());
  const repetido = new Set(preenchidas.map((l) => l.marcadorId)).size !== preenchidas.length;
  const salvar = useMutation({
    mutationFn: () => {
      const json = {
        dataColetaUtc: new Date(`${data}T12:00:00`).toISOString(),
        laboratorio: txt(lab),
        observacoes: txt(obs),
        resultados: preenchidas.map((l) => {
          const n = Number(l.valor.replace(',', '.'));
          const numerico = l.valor.trim() !== '' && !Number.isNaN(n);
          return { marcadorId: l.marcadorId, valorNumerico: numerico ? n : null, valorTexto: numerico ? null : l.valor.trim(), unidade: txt(l.unidade), referenciaMinima: dec(l.min), referenciaMaxima: dec(l.max), referenciaTexto: null, observacao: null };
        }),
      };
      return item === 'novo' ? api(`/api/pacientes/${id}/exames`, { method: 'POST', json }) : api(`/api/exames/${(item as Exame).id}`, { method: 'PUT', json });
    },
    onSuccess: () => {
      toast('Exame salvo.');
      qc.invalidateQueries({ queryKey: ['pro', 'pront', 'exames', id] });
      qc.invalidateQueries({ queryKey: ['pro', 'resumo', id] });
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível salvar o exame.'),
  });
  const grupos = new Map<string, Marcador[]>();
  for (const m of marcadores.data ?? []) grupos.set(m.categoria ?? 'Outros', [...(grupos.get(m.categoria ?? 'Outros') ?? []), m]);
  return (
    <Sheet
      open={!!item}
      onClose={onClose}
      tall
      title={item === 'novo' ? 'Lançar exame' : 'Editar exame'}
      footer={<Rodape salvar={() => salvar.mutate()} pendente={salvar.isPending} valido={!!data && preenchidas.length > 0 && !repetido} />}
    >
      <div className="flex flex-col gap-4">
        <div className="grid grid-cols-2 gap-3">
          <Field label="Data da coleta">
            <input type="date" className={inputCls} value={data} onChange={(e) => setData(e.target.value)} />
          </Field>
          <Field label="Laboratório">
            <input className={inputCls} value={lab} maxLength={160} onChange={(e) => setLab(e.target.value)} />
          </Field>
        </div>
        <div>
          <p className="mb-1.5 text-[13px] font-semibold text-muted">Resultados</p>
          <div className="flex flex-col gap-2">
            {linhas.map((l) => (
              <div key={l.chave} className="rounded-xl border border-line/70 p-2.5">
                <div className="flex gap-2">
                  <select aria-label="Marcador" className={clsx(inputCls, 'flex-1')} value={l.marcadorId} onChange={(e) => escolher(l.chave, e.target.value)}>
                    <option value="">Escolha o marcador</option>
                    {[...grupos.entries()].map(([g, ms]) => (
                      <optgroup key={g} label={g}>
                        {ms.map((m) => (
                          <option key={m.id} value={m.id}>
                            {m.nome}
                          </option>
                        ))}
                      </optgroup>
                    ))}
                    <option value="__novo">+ Novo marcador…</option>
                  </select>
                  <button aria-label="Remover linha" onClick={() => setLinhas((ls) => ls.filter((x) => x.chave !== l.chave))} className="grid h-11 w-11 shrink-0 place-items-center rounded-xl text-muted hover:bg-raised">
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
                <div className="mt-2 grid grid-cols-4 gap-2">
                  <input aria-label="Valor" className={inputCls} placeholder="Valor" value={l.valor} onChange={(e) => mudar(l.chave, { valor: e.target.value })} />
                  <input aria-label="Unidade" className={inputCls} placeholder="Unid." value={l.unidade} onChange={(e) => mudar(l.chave, { unidade: e.target.value })} />
                  <input aria-label="Referência mínima" inputMode="decimal" className={inputCls} placeholder="Ref. mín" value={l.min} onChange={(e) => mudar(l.chave, { min: e.target.value })} />
                  <input aria-label="Referência máxima" inputMode="decimal" className={inputCls} placeholder="Ref. máx" value={l.max} onChange={(e) => mudar(l.chave, { max: e.target.value })} />
                </div>
              </div>
            ))}
          </div>
          {repetido && <p className="mt-1.5 text-[13px] text-bad">Há um marcador repetido.</p>}
          <Button variant="outline" size="sm" className="mt-2" icon={<Plus className="h-4 w-4" />} onClick={() => setLinhas((ls) => [...ls, novaLinha()])}>
            Mais um marcador
          </Button>
        </div>
        <Texto label="Observações" value={obs} onChange={setObs} rows={2} />
      </div>
      {novoMarcador && (
        <div className="mt-4 rounded-xl border border-accent/40 bg-accent/5 p-3">
          <p className="mb-2 text-sm font-semibold">Novo marcador</p>
          <div className="grid grid-cols-2 gap-2">
            <input aria-label="Nome do marcador" className={clsx(inputCls, 'col-span-2')} placeholder="Ex.: Vitamina B12" value={novoMarcador.nome} onChange={(e) => setNovoMarcador({ ...novoMarcador, nome: e.target.value })} />
            <input aria-label="Categoria" className={inputCls} placeholder="Categoria" value={novoMarcador.categoria} onChange={(e) => setNovoMarcador({ ...novoMarcador, categoria: e.target.value })} />
            <input aria-label="Unidade padrão" className={inputCls} placeholder="Unidade" value={novoMarcador.unidade} onChange={(e) => setNovoMarcador({ ...novoMarcador, unidade: e.target.value })} />
          </div>
          <div className="mt-2 flex gap-2">
            <Button size="sm" disabled={!novoMarcador.nome.trim()} loading={criarMarcador.isPending} onClick={() => criarMarcador.mutate()}>
              Criar
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setNovoMarcador(null)}>
              Cancelar
            </Button>
          </div>
        </div>
      )}
    </Sheet>
  );
}

// ---------------------------------------------------------------- Relatorios

async function abrirRelatorio(rid: string) {
  // O HTML exige o token, entao e baixado aqui e aberto como arquivo local.
  const janela = window.open('', '_blank');
  try {
    const html = await api<string>(`/api/relatorios/${rid}/html`);
    const url = URL.createObjectURL(new Blob([html], { type: 'text/html' }));
    if (janela) janela.location.href = url;
    else window.location.href = url;
  } catch (e) {
    janela?.close();
    erro(e, 'Não foi possível abrir o relatório.');
  }
}

function Relatorios({ id }: { id: string }) {
  const q = useQuery({ queryKey: ['pro', 'pront', 'relatorios', id], queryFn: () => api<Relatorio[]>(`/api/pacientes/${id}/relatorios`) });
  const [novo, setNovo] = useState(false);
  const lista = [...(q.data ?? [])].sort((a, b) => b.dataGeracaoUtc.localeCompare(a.dataGeracaoUtc));
  return (
    <>
      <Topo titulo={`${lista.length} ${lista.length === 1 ? 'relatório' : 'relatórios'}`} acao="Gerar relatório" onAcao={() => setNovo(true)} />
      {q.isLoading ? (
        <Skeleton className="h-32" />
      ) : !lista.length ? (
        <Vazio icon={<FileText className="h-5 w-5" />}>Gere um relatório clínico com evolução corporal, consultas, anamnese e exames do período. Dá para imprimir ou salvar em PDF.</Vazio>
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((r) => (
            <button key={r.id} onClick={() => abrirRelatorio(r.id)} className="flex w-full items-start gap-3 p-3.5 text-left transition hover:bg-raised/40 [overflow-wrap:anywhere]">
              <FileText className="mt-0.5 h-5 w-5 shrink-0 text-accent" />
              <span className="min-w-0 flex-1">
                <span className="block font-semibold">{r.titulo}</span>
                <span className="block text-[13px] text-muted">
                  {dataCurta(r.dataGeracaoUtc)} · {r.dataInicioUtc || r.dataFimUtc ? `${r.dataInicioUtc ? dataCurta(r.dataInicioUtc) : 'início'} a ${r.dataFimUtc ? dataCurta(r.dataFimUtc) : 'hoje'}` : 'todo o acompanhamento'}
                </span>
              </span>
              <span className="shrink-0 text-sm font-semibold text-accent">Abrir</span>
            </button>
          ))}
        </Card>
      )}
      <RelatorioSheet id={id} open={novo} onClose={() => setNovo(false)} />
    </>
  );
}

function RelatorioSheet({ id, open, onClose }: { id: string; open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const [titulo, setTitulo] = useState('');
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const [conclusao, setConclusao] = useState('');
  useEffect(() => {
    if (!open) return;
    setTitulo('');
    const d = new Date();
    d.setMonth(d.getMonth() - 3);
    setInicio(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`);
    setFim(hojeIso());
    setConclusao('');
  }, [open]);
  const salvar = useMutation({
    mutationFn: () =>
      api<Relatorio>(`/api/pacientes/${id}/relatorios`, {
        method: 'POST',
        json: { titulo: txt(titulo), dataInicioUtc: inicio ? new Date(`${inicio}T00:00:00`).toISOString() : null, dataFimUtc: fim ? new Date(`${fim}T23:59:59`).toISOString() : null, conclusaoMedica: txt(conclusao) },
      }),
    onSuccess: (r) => {
      toast('Relatório gerado.');
      qc.invalidateQueries({ queryKey: ['pro', 'pront', 'relatorios', id] });
      onClose();
      abrirRelatorio(r.id);
    },
    onError: (e) => erro(e, 'Não foi possível gerar o relatório.'),
  });
  return (
    <Sheet open={open} onClose={onClose} title="Gerar relatório" footer={<Rodape salvar={() => salvar.mutate()} pendente={salvar.isPending} valido={!inicio || !fim || inicio <= fim} rotulo="Gerar e abrir" />}>
      <div className="flex flex-col gap-4">
        <Field label="Título (opcional)">
          <input className={inputCls} value={titulo} maxLength={200} placeholder="Relatório clínico" onChange={(e) => setTitulo(e.target.value)} />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="De">
            <input type="date" className={inputCls} value={inicio} onChange={(e) => setInicio(e.target.value)} />
          </Field>
          <Field label="Até">
            <input type="date" className={inputCls} value={fim} onChange={(e) => setFim(e.target.value)} />
          </Field>
        </div>
        <Texto label="Conclusão (opcional)" value={conclusao} onChange={setConclusao} rows={4} />
        <p className="flex items-center gap-1.5 text-[12px] text-faint">
          <CalendarDays className="h-3.5 w-3.5" /> Deixe as datas vazias para incluir todo o acompanhamento.
        </p>
      </div>
    </Sheet>
  );
}
