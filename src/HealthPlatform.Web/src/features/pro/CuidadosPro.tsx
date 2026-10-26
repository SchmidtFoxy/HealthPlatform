import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Activity, ClipboardList, MoreHorizontal, Pencil, Pill as PillIcon, Plus, Target } from 'lucide-react';
import { ReactNode, useEffect, useState } from 'react';
import { Button, Card, Field, inputCls, Pill, SectionTitle, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta, hojeIso, n1, tempoRelativo } from '@/lib/format';
import { FREQUENCIAS_PROTOCOLO, ItemProtocolo, Protocolo, quandoProtocolo, TIPOS_PROTOCOLO } from '../patient/Protocolo';
import { Pedido, RevisarPedidoSheet } from './CentralPage';

// ---------------------------------------------------------------- Tipos

interface MetaPro {
  id: string;
  nome: string;
  tipo: string;
  valorObjetivo: number | null;
  unidade: string | null;
  frequencia: 'Diaria' | 'Semanal' | 'Mensal' | string;
  dataInicio: string;
  dataFim: string | null;
  status: 'Ativa' | 'Pausada' | 'Concluida' | 'Cancelada' | string;
  observacoes: string | null;
  progressoHojePercentual: number | null;
  registroHoje: { valor: number | null; concluida: boolean | null } | null;
}

interface MedicamentoPro {
  id: string;
  nome: string;
  dose: number | null;
  unidade: string | null;
  via: string | null;
  frequencia: string | null;
  horariosLocais: string | null;
  dataInicio: string;
  dataFim: string | null;
  orientacao: string | null;
  ativo: boolean;
}

interface Medicacao {
  medicamentos: MedicamentoPro[];
  aderencia: { dias: number; total: number; tomados: number; percentual: number | null };
}

const erro = (e: unknown, padrao: string) => toast(e instanceof Error ? e.message : padrao, 'bad');
const offset = () => -new Date().getTimezoneOffset();

const FREQ_META: Record<string, string> = { Diaria: 'Todo dia', Semanal: 'Toda semana', Mensal: 'Todo mês' };
const STATUS_META: Record<string, { l: string; tone: 'ok' | 'warn' | 'neutral' | 'info' }> = {
  Ativa: { l: 'Ativa', tone: 'ok' },
  Pausada: { l: 'Pausada', tone: 'warn' },
  Concluida: { l: 'Concluída', tone: 'info' },
  Cancelada: { l: 'Cancelada', tone: 'neutral' },
};
const TIPOS_PEDIDO = ['Acompanhamento', 'Exame', 'Documento', 'Medição', 'Questionário', 'Retorno', 'Outro'];

export function Chips<T extends string>({ value, onChange, options }: { value: T; onChange: (v: T) => void; options: { value: T; label: string }[] }) {
  return (
    <div className="flex flex-wrap gap-2">
      {options.map((o) => (
        <button
          key={o.value}
          type="button"
          onClick={() => onChange(o.value)}
          className={clsx('h-9 rounded-full border px-3.5 text-sm font-semibold transition', value === o.value ? 'border-accent bg-accent/10 text-accent' : 'border-line text-muted hover:bg-raised')}
        >
          {o.label}
        </button>
      ))}
    </div>
  );
}

function Novo({ onClick, children }: { onClick: () => void; children: ReactNode }) {
  return (
    <button onClick={onClick} className="inline-flex items-center gap-1 text-sm font-semibold text-accent">
      <Plus className="h-4 w-4" /> {children}
    </button>
  );
}

function Vazio({ children }: { children: ReactNode }) {
  return <Card className="p-4 text-sm text-muted">{children}</Card>;
}

/** Aba "Cuidados" da ficha: pedidos, metas, remedios/suplementos e monitoramento do paciente. */
export function AbaCuidados({ id }: { id: string }) {
  return (
    <div className="grid gap-x-6 lg:grid-cols-2">
      <div className="lg:col-span-2">
        <PedidosPaciente id={id} />
      </div>
      <MetasPaciente id={id} />
      <MedicamentosPaciente id={id} />
      <div className="lg:col-span-2">
        <ProtocoloPaciente id={id} />
      </div>
    </div>
  );
}

// ---------------------------------------------------------------- Pedidos

function PedidosPaciente({ id }: { id: string }) {
  const q = useQuery({ queryKey: ['pro', 'pedidos', 'paciente', id], queryFn: () => api<Pedido[]>(`/api/pacientes/${id}/solicitacoes`) });
  const [novo, setNovo] = useState(false);
  const [revisar, setRevisar] = useState<Pedido | null>(null);
  const qc = useQueryClient();
  const cancelar = useMutation({
    mutationFn: (pid: string) => api(`/api/solicitacoes/${pid}/cancelar`, { method: 'PUT' }),
    onSuccess: () => {
      toast('Pedido cancelado.');
      qc.invalidateQueries({ queryKey: ['pro', 'pedidos'] });
      qc.invalidateQueries({ queryKey: ['pro', 'central'] });
    },
    onError: (e) => erro(e, 'Não foi possível cancelar.'),
  });
  const [verTodos, setVerTodos] = useState(false);
  const todos = q.data ?? [];
  const abertos = todos.filter((p) => p.status === 'Pendente' || p.status === 'Enviada');
  const lista = verTodos ? todos : abertos;

  return (
    <section>
      <SectionTitle action={<Novo onClick={() => setNovo(true)}>Novo pedido</Novo>}>Pedidos ao paciente</SectionTitle>
      {q.isLoading ? (
        <Skeleton className="h-24" />
      ) : !lista.length ? (
        <Vazio>{todos.length ? 'Nenhum pedido em aberto.' : 'Peça um exame, uma foto, uma medida ou um documento. O paciente responde pelo app.'}</Vazio>
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((p) => {
            const vencido = p.status === 'Pendente' && p.dataLimiteUtc && new Date(p.dataLimiteUtc) < new Date();
            return (
              <div key={p.id} className="p-3.5 [overflow-wrap:anywhere]">
                <div className="flex items-start gap-3">
                  <ClipboardList className="mt-0.5 h-5 w-5 shrink-0 text-accent" />
                  <div className="min-w-0 flex-1">
                    <p className="font-semibold">{p.titulo}</p>
                    <p className="text-[13px] text-muted">
                      {p.tipo}
                      {p.dataLimiteUtc && ` · até ${dataCurta(p.dataLimiteUtc)}`}
                    </p>
                  </div>
                  <Pill tone={p.status === 'Enviada' ? 'accent' : vencido ? 'bad' : p.status === 'Revisada' ? 'ok' : 'neutral'}>
                    {p.status === 'Enviada' ? 'Respondido' : vencido ? 'Vencido' : p.status === 'Pendente' ? 'Aguardando' : p.status === 'Revisada' ? 'Revisado' : 'Cancelado'}
                  </Pill>
                </div>
                {p.respostaPaciente && (
                  <p className="ml-8 mt-2 rounded-xl bg-raised px-3 py-2 text-[14px]">
                    <span className="block text-[12px] font-semibold text-faint">Resposta {p.respondidaEmUtc && tempoRelativo(p.respondidaEmUtc)}</span>
                    {p.respostaPaciente}
                  </p>
                )}
                {(p.status === 'Enviada' || p.status === 'Pendente') && (
                  <div className="ml-8 mt-2 flex flex-wrap gap-2">
                    {p.status === 'Enviada' && (
                      <Button size="sm" onClick={() => setRevisar(p)}>
                        Revisar
                      </Button>
                    )}
                    <Button size="sm" variant="ghost" className="text-muted" onClick={() => window.confirm('Cancelar este pedido?') && cancelar.mutate(p.id)}>
                      Cancelar
                    </Button>
                  </div>
                )}
              </div>
            );
          })}
        </Card>
      )}
      {todos.length > abertos.length && (
        <button onClick={() => setVerTodos(!verTodos)} className="mt-2 text-sm font-semibold text-muted hover:text-fg">
          {verTodos ? 'Mostrar só os abertos' : `Ver histórico (${todos.length - abertos.length})`}
        </button>
      )}
      <NovoPedidoSheet id={id} open={novo} onClose={() => setNovo(false)} />
      <RevisarPedidoSheet pedido={revisar} onClose={() => setRevisar(null)} />
    </section>
  );
}

function NovoPedidoSheet({ id, open, onClose }: { id: string; open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const [tipo, setTipo] = useState('Exame');
  const [titulo, setTitulo] = useState('');
  const [descricao, setDescricao] = useState('');
  const [prazo, setPrazo] = useState('');
  useEffect(() => {
    if (!open) return;
    setTipo('Exame');
    setTitulo('');
    setDescricao('');
    const d = new Date();
    d.setDate(d.getDate() + 7);
    setPrazo(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`);
  }, [open]);
  const salvar = useMutation({
    mutationFn: () =>
      api(`/api/pacientes/${id}/solicitacoes`, {
        method: 'POST',
        json: { tipo, titulo: titulo.trim(), descricao: descricao.trim() || null, dataLimiteUtc: prazo ? new Date(`${prazo}T23:59:00`).toISOString() : null },
      }),
    onSuccess: () => {
      toast('Pedido enviado ao paciente.');
      qc.invalidateQueries({ queryKey: ['pro', 'pedidos'] });
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível criar o pedido.'),
  });
  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Novo pedido"
      footer={
        <Button size="lg" className="w-full" disabled={!titulo.trim()} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Enviar ao paciente
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Tipo" group>
          <Chips value={tipo} onChange={setTipo} options={TIPOS_PEDIDO.map((t) => ({ value: t, label: t }))} />
        </Field>
        <Field label="O que você precisa">
          <input className={inputCls} value={titulo} maxLength={300} placeholder="Ex.: Enviar hemograma completo" onChange={(e) => setTitulo(e.target.value)} />
        </Field>
        <Field label="Detalhes (opcional)">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={descricao} maxLength={3000} placeholder="Ex.: pode ser foto ou PDF do laudo" onChange={(e) => setDescricao(e.target.value)} />
        </Field>
        <Field label="Prazo (opcional)">
          <input type="date" className={inputCls} value={prazo} min={hojeIso()} onChange={(e) => setPrazo(e.target.value)} />
        </Field>
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Metas

function MetasPaciente({ id }: { id: string }) {
  const q = useQuery({ queryKey: ['pro', 'cuidados', 'metas', id], queryFn: () => api<MetaPro[]>(`/api/pacientes/${id}/metas?incluirEncerradas=true`) });
  const [editar, setEditar] = useState<MetaPro | 'nova' | null>(null);
  const [menu, setMenu] = useState<string | null>(null);
  const qc = useQueryClient();
  const status = useMutation({
    mutationFn: ({ mid, s }: { mid: string; s: string }) => api(`/api/metas/${mid}/status/${s}`, { method: 'POST' }),
    onSuccess: () => {
      setMenu(null);
      qc.invalidateQueries({ queryKey: ['pro', 'cuidados', 'metas', id] });
    },
    onError: (e) => erro(e, 'Não foi possível mudar o status.'),
  });
  const metas = [...(q.data ?? [])].sort((a, b) => Number(b.status === 'Ativa') - Number(a.status === 'Ativa'));
  const [verEncerradas, setVerEncerradas] = useState(false);
  const ativas = metas.filter((m) => m.status === 'Ativa' || m.status === 'Pausada');
  const lista = verEncerradas ? metas : ativas;

  return (
    <section>
      <SectionTitle action={<Novo onClick={() => setEditar('nova')}>Nova meta</Novo>}>Metas</SectionTitle>
      {q.isLoading ? (
        <Skeleton className="h-24" />
      ) : !lista.length ? (
        <Vazio>{metas.length ? 'Nenhuma meta ativa.' : 'Crie metas como beber 3 L de água por dia ou dormir antes das 23h. O paciente marca no app.'}</Vazio>
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((m) => {
            const st = STATUS_META[m.status] ?? { l: m.status, tone: 'neutral' as const };
            const feitoHoje = m.registroHoje?.concluida || (m.progressoHojePercentual ?? 0) >= 100;
            return (
              <div key={m.id} className="relative p-3.5 [overflow-wrap:anywhere]">
                <div className="flex items-start gap-3">
                  <Target className={clsx('mt-0.5 h-5 w-5 shrink-0', m.status === 'Ativa' ? 'text-accent' : 'text-faint')} />
                  <div className="min-w-0 flex-1">
                    <p className="font-semibold">{m.nome}</p>
                    <p className="text-[13px] text-muted">
                      {[m.valorObjetivo != null ? `${n1(m.valorObjetivo)} ${m.unidade ?? ''}`.trim() : null, FREQ_META[m.frequencia] ?? m.frequencia, m.dataFim ? `até ${dataCurta(m.dataFim)}` : null].filter(Boolean).join(' · ')}
                    </p>
                    {m.status === 'Ativa' && (
                      <p className={clsx('mt-0.5 text-[13px] font-semibold', feitoHoje ? 'text-ok' : 'text-faint')}>
                        {feitoHoje
                          ? 'Cumprida hoje'
                          : m.registroHoje?.valor != null
                            ? `Hoje: ${n1(m.registroHoje.valor)} ${m.unidade ?? ''} (${n1(m.progressoHojePercentual)}%)`
                            : 'Ainda não marcada hoje'}
                      </p>
                    )}
                  </div>
                  {m.status !== 'Ativa' && <Pill tone={st.tone}>{st.l}</Pill>}
                  <button aria-label="Opções da meta" onClick={() => setMenu(menu === m.id ? null : m.id)} className="grid h-8 w-8 shrink-0 place-items-center rounded-full text-muted hover:bg-raised">
                    <MoreHorizontal className="h-4 w-4" />
                  </button>
                </div>
                {menu === m.id && (
                  <div className="ml-8 mt-2 flex flex-wrap gap-2">
                    <Button size="sm" variant="outline" icon={<Pencil className="h-3.5 w-3.5" />} onClick={() => (setMenu(null), setEditar(m))}>
                      Editar
                    </Button>
                    {m.status === 'Ativa' && (
                      <Button size="sm" variant="outline" onClick={() => status.mutate({ mid: m.id, s: 'Pausada' })}>
                        Pausar
                      </Button>
                    )}
                    {m.status !== 'Ativa' && (
                      <Button size="sm" variant="outline" onClick={() => status.mutate({ mid: m.id, s: 'Ativa' })}>
                        Reativar
                      </Button>
                    )}
                    {m.status !== 'Concluida' && (
                      <Button size="sm" variant="outline" onClick={() => status.mutate({ mid: m.id, s: 'Concluida' })}>
                        Concluir
                      </Button>
                    )}
                    {m.status !== 'Cancelada' && (
                      <Button size="sm" variant="ghost" className="text-muted" onClick={() => status.mutate({ mid: m.id, s: 'Cancelada' })}>
                        Cancelar meta
                      </Button>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </Card>
      )}
      {metas.length > ativas.length && (
        <button onClick={() => setVerEncerradas(!verEncerradas)} className="mt-2 text-sm font-semibold text-muted hover:text-fg">
          {verEncerradas ? 'Esconder encerradas' : `Ver encerradas (${metas.length - ativas.length})`}
        </button>
      )}
      <MetaSheet id={id} meta={editar} onClose={() => setEditar(null)} />
    </section>
  );
}

function MetaSheet({ id, meta, onClose }: { id: string; meta: MetaPro | 'nova' | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [nome, setNome] = useState('');
  const [comNumero, setComNumero] = useState<'sim' | 'nao'>('nao');
  const [valor, setValor] = useState('');
  const [unidade, setUnidade] = useState('');
  const [freq, setFreq] = useState('Diaria');
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const [obs, setObs] = useState('');
  useEffect(() => {
    if (!meta) return;
    const m = meta === 'nova' ? null : meta;
    setNome(m?.nome ?? '');
    setComNumero(m?.valorObjetivo != null ? 'sim' : 'nao');
    setValor(m?.valorObjetivo != null ? String(m.valorObjetivo) : '');
    setUnidade(m?.unidade ?? '');
    setFreq(m?.frequencia ?? 'Diaria');
    setInicio(m?.dataInicio ?? hojeIso());
    setFim(m?.dataFim ?? '');
    setObs(m?.observacoes ?? '');
  }, [meta]);
  const numero = Number(valor.replace(',', '.'));
  const valido = !!nome.trim() && !!inicio && (comNumero === 'nao' || numero > 0) && (!fim || fim >= inicio);
  const salvar = useMutation({
    mutationFn: () => {
      const json = {
        nome: nome.trim(),
        tipo: comNumero === 'sim' ? 'Quantitativa' : 'Binaria',
        valorObjetivo: comNumero === 'sim' ? numero : null,
        unidade: comNumero === 'sim' ? unidade.trim() || null : null,
        frequencia: freq,
        dataInicio: inicio,
        dataFim: fim || null,
        observacoes: obs.trim() || null,
      };
      return meta === 'nova' ? api(`/api/pacientes/${id}/metas`, { method: 'POST', json }) : api(`/api/metas/${(meta as MetaPro).id}`, { method: 'PUT', json });
    },
    onSuccess: () => {
      toast(meta === 'nova' ? 'Meta criada.' : 'Meta atualizada.');
      qc.invalidateQueries({ queryKey: ['pro', 'cuidados', 'metas', id] });
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível salvar a meta.'),
  });
  return (
    <Sheet
      open={!!meta}
      onClose={onClose}
      title={meta === 'nova' ? 'Nova meta' : 'Editar meta'}
      footer={
        <Button size="lg" className="w-full" disabled={!valido} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          {meta === 'nova' ? 'Criar meta' : 'Salvar'}
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Meta">
          <input className={inputCls} value={nome} maxLength={200} placeholder="Ex.: Beber 3 L de água" onChange={(e) => setNome(e.target.value)} />
        </Field>
        <Field label="Como o paciente marca" group>
          <Chips
            value={comNumero}
            onChange={setComNumero}
            options={[
              { value: 'nao', label: 'Fiz / não fiz' },
              { value: 'sim', label: 'Com um número' },
            ]}
          />
        </Field>
        {comNumero === 'sim' && (
          <div className="grid grid-cols-2 gap-3">
            <Field label="Alvo">
              <input type="number" inputMode="decimal" min="0" step="any" className={inputCls} value={valor} placeholder="3" onChange={(e) => setValor(e.target.value)} />
            </Field>
            <Field label="Unidade">
              <input className={inputCls} value={unidade} maxLength={30} placeholder="L, h, passos" onChange={(e) => setUnidade(e.target.value)} />
            </Field>
          </div>
        )}
        <Field label="Frequência" group>
          <Chips value={freq} onChange={setFreq} options={Object.entries(FREQ_META).map(([value, label]) => ({ value, label }))} />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Início">
            <input type="date" className={inputCls} value={inicio} onChange={(e) => setInicio(e.target.value)} />
          </Field>
          <Field label="Fim (opcional)">
            <input type="date" className={inputCls} value={fim} min={inicio} onChange={(e) => setFim(e.target.value)} />
          </Field>
        </div>
        <Field label="Orientação ao paciente (opcional)">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={obs} maxLength={2000} onChange={(e) => setObs(e.target.value)} />
        </Field>
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Remedios e suplementos

function MedicamentosPaciente({ id }: { id: string }) {
  const q = useQuery({ queryKey: ['pro', 'cuidados', 'meds', id], queryFn: () => api<Medicacao>(`/api/pacientes/${id}/medicamentos?dias=30`) });
  const [editar, setEditar] = useState<MedicamentoPro | 'novo' | null>(null);
  const [verInativos, setVerInativos] = useState(false);
  const d = q.data;
  const todos = d?.medicamentos ?? [];
  const ativos = todos.filter((m) => m.ativo);
  const lista = verInativos ? todos : ativos;

  return (
    <section>
      <SectionTitle action={<Novo onClick={() => setEditar('novo')}>Novo item</Novo>}>Remédios e suplementos</SectionTitle>
      {q.isLoading ? (
        <Skeleton className="h-24" />
      ) : !lista.length ? (
        <Vazio>{todos.length ? 'Nenhum item ativo.' : 'Cadastre remédios e suplementos com horários. O paciente marca cada dose no app.'}</Vazio>
      ) : (
        <>
          {d?.aderencia.total ? (
            <p className="mb-2 text-sm text-muted">
              Adesão: <b className={clsx((d.aderencia.percentual ?? 0) >= 80 ? 'text-ok' : 'text-warn')}>{d.aderencia.percentual}%</b> ({d.aderencia.tomados} de {d.aderencia.total} doses em {d.aderencia.dias} dias)
            </p>
          ) : null}
          <Card className="divide-y divide-line/60">
            {lista.map((m) => (
              <button key={m.id} onClick={() => setEditar(m)} className="flex w-full items-start gap-3 p-3.5 text-left transition hover:bg-raised/40 [overflow-wrap:anywhere]">
                <PillIcon className={clsx('mt-0.5 h-5 w-5 shrink-0', m.ativo ? 'text-accent' : 'text-faint')} />
                <span className="min-w-0 flex-1">
                  <span className="block font-semibold">{m.nome}</span>
                  <span className="block text-[13px] text-muted">
                    {[m.dose != null ? `${n1(m.dose)} ${m.unidade ?? ''}`.trim() : null, m.via, m.horariosLocais?.replaceAll(',', ', ')].filter(Boolean).join(' · ') || 'Sem horário definido'}
                  </span>
                </span>
                {!m.ativo && <Pill>Inativo</Pill>}
                <Pencil className="mt-1 h-4 w-4 shrink-0 text-faint" />
              </button>
            ))}
          </Card>
        </>
      )}
      {todos.length > ativos.length && (
        <button onClick={() => setVerInativos(!verInativos)} className="mt-2 text-sm font-semibold text-muted hover:text-fg">
          {verInativos ? 'Esconder inativos' : `Ver inativos (${todos.length - ativos.length})`}
        </button>
      )}
      <p className="mt-2 text-[12px] text-faint">Para acompanhamento e adesão. Não substitui a prescrição.</p>
      <MedicamentoSheet id={id} med={editar} onClose={() => setEditar(null)} />
    </section>
  );
}

const HORARIOS_RAPIDOS = ['07:00', '08:00', '12:00', '18:00', '20:00', '22:00'];

function MedicamentoSheet({ id, med, onClose }: { id: string; med: MedicamentoPro | 'novo' | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [nome, setNome] = useState('');
  const [dose, setDose] = useState('');
  const [unidade, setUnidade] = useState('');
  const [via, setVia] = useState('Oral');
  const [horarios, setHorarios] = useState<string[]>([]);
  const [hora, setHora] = useState('');
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const [orientacao, setOrientacao] = useState('');
  useEffect(() => {
    if (!med) return;
    const m = med === 'novo' ? null : med;
    setNome(m?.nome ?? '');
    setDose(m?.dose != null ? String(m.dose) : '');
    setUnidade(m?.unidade ?? '');
    setVia(m?.via ?? 'Oral');
    setHorarios(m?.horariosLocais ? m.horariosLocais.split(',').map((h) => h.trim().slice(0, 5)) : ['08:00']);
    setHora('');
    setInicio(m?.dataInicio ?? hojeIso());
    setFim(m?.dataFim ?? '');
    setOrientacao(m?.orientacao ?? '');
  }, [med]);
  const alternar = (h: string) => setHorarios((x) => (x.includes(h) ? x.filter((y) => y !== h) : [...x, h].sort()));
  const json = (ativo: boolean) => ({
    nome: nome.trim(),
    dose: dose.trim() ? Number(dose.replace(',', '.')) : null,
    unidade: unidade.trim() || null,
    via: via.trim() || null,
    frequencia: 'Diario',
    horariosLocais: horarios.join(','),
    dataInicio: inicio,
    dataFim: fim || null,
    orientacao: orientacao.trim() || null,
    ativo,
  });
  const invalidar = () => qc.invalidateQueries({ queryKey: ['pro', 'cuidados', 'meds', id] });
  const salvar = useMutation({
    mutationFn: () => (med === 'novo' ? api(`/api/pacientes/${id}/medicamentos`, { method: 'POST', json: json(true) }) : api(`/api/medicamentos/${(med as MedicamentoPro).id}`, { method: 'PUT', json: json((med as MedicamentoPro).ativo) })),
    onSuccess: () => {
      toast(med === 'novo' ? 'Adicionado.' : 'Atualizado.');
      invalidar();
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível salvar.'),
  });
  const ativar = useMutation({
    mutationFn: (ativo: boolean) => (ativo ? api(`/api/medicamentos/${(med as MedicamentoPro).id}`, { method: 'PUT', json: json(true) }) : api(`/api/medicamentos/${(med as MedicamentoPro).id}`, { method: 'DELETE' })),
    onSuccess: (_, ativo) => {
      toast(ativo ? 'Reativado.' : 'Desativado. O paciente deixa de ver este item.');
      invalidar();
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível alterar.'),
  });
  const existente = med && med !== 'novo' ? med : null;
  return (
    <Sheet
      open={!!med}
      onClose={onClose}
      title={existente ? existente.nome : 'Novo remédio ou suplemento'}
      footer={
        <div className="flex flex-col gap-2">
          <Button size="lg" className="w-full" disabled={!nome.trim() || !inicio} loading={salvar.isPending} onClick={() => salvar.mutate()}>
            {existente ? 'Salvar' : 'Adicionar'}
          </Button>
          {existente && (
            <Button variant={existente.ativo ? 'danger' : 'outline'} className="w-full" loading={ativar.isPending} onClick={() => ativar.mutate(!existente.ativo)}>
              {existente.ativo ? 'Desativar' : 'Reativar'}
            </Button>
          )}
        </div>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome">
          <input className={inputCls} value={nome} maxLength={220} placeholder="Ex.: Vitamina D3" onChange={(e) => setNome(e.target.value)} />
        </Field>
        <div className="grid grid-cols-3 gap-3">
          <Field label="Dose">
            <input type="number" inputMode="decimal" min="0" step="any" className={inputCls} value={dose} onChange={(e) => setDose(e.target.value)} />
          </Field>
          <Field label="Unidade">
            <input className={inputCls} value={unidade} maxLength={30} placeholder="mg, UI, g" onChange={(e) => setUnidade(e.target.value)} />
          </Field>
          <Field label="Via">
            <input className={inputCls} value={via} maxLength={40} onChange={(e) => setVia(e.target.value)} />
          </Field>
        </div>
        <Field group label="Horários" hint="O paciente recebe cada horário como uma dose para marcar.">
          <div className="flex flex-wrap gap-2">
            {[...new Set([...HORARIOS_RAPIDOS, ...horarios])].sort().map((h) => (
              <button
                key={h}
                type="button"
                onClick={() => alternar(h)}
                className={clsx('num h-9 rounded-full border px-3 text-sm font-semibold transition', horarios.includes(h) ? 'border-accent bg-accent/10 text-accent' : 'border-line text-muted hover:bg-raised')}
              >
                {h}
              </button>
            ))}
          </div>
          <div className="mt-2 flex gap-2">
            <input type="time" className={inputCls} value={hora} onChange={(e) => setHora(e.target.value)} />
            <Button variant="outline" disabled={!hora} onClick={() => (alternar(hora), setHora(''))}>
              Incluir
            </Button>
          </div>
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Início">
            <input type="date" className={inputCls} value={inicio} onChange={(e) => setInicio(e.target.value)} />
          </Field>
          <Field label="Fim (opcional)">
            <input type="date" className={inputCls} value={fim} min={inicio} onChange={(e) => setFim(e.target.value)} />
          </Field>
        </div>
        <Field label="Orientação ao paciente (opcional)">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={orientacao} maxLength={1600} placeholder="Ex.: junto de uma refeição com gordura" onChange={(e) => setOrientacao(e.target.value)} />
        </Field>
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Monitoramento (protocolo)

function ProtocoloPaciente({ id }: { id: string }) {
  const q = useQuery({ queryKey: ['pro', 'cuidados', 'protocolo', id], queryFn: () => api<Protocolo>(`/api/pacientes/${id}/protocolo-acompanhamento?offsetMinutos=${offset()}`) });
  const [editar, setEditar] = useState<ItemProtocolo | 'novo' | null>(null);
  const d = q.data;
  const ativos = (d?.itens ?? []).filter((x) => x.ativo);
  const hoje = new Map((d?.aderencia.hoje.itens ?? []).map((x) => [x.id, x.concluido]));

  return (
    <section>
      <SectionTitle action={<Novo onClick={() => setEditar('novo')}>Nova medição</Novo>}>Monitoramento</SectionTitle>
      {q.isLoading ? (
        <Skeleton className="h-24" />
      ) : !ativos.length ? (
        <Vazio>Defina o que o paciente deve medir e registrar: peso, pressão, glicemia, sono, dor, sintomas. Aparece em Cuidados no app dele.</Vazio>
      ) : (
        <>
          {d?.aderencia.previstos ? (
            <p className="mb-2 text-sm text-muted">
              Adesão: <b className={clsx((d.aderencia.percentual ?? 0) >= 70 ? 'text-ok' : 'text-warn')}>{d.aderencia.percentual}%</b> ({d.aderencia.concluidos} de {d.aderencia.previstos} registros em {d.aderencia.dias} dias)
            </p>
          ) : null}
          <Card className="divide-y divide-line/60">
            {ativos.map((x) => (
              <button key={x.id} onClick={() => setEditar(x)} className="flex w-full items-start gap-3 p-3.5 text-left transition hover:bg-raised/40 [overflow-wrap:anywhere]">
                <Activity className="mt-0.5 h-5 w-5 shrink-0 text-accent" />
                <span className="min-w-0 flex-1">
                  <span className="block font-semibold">{x.titulo}</span>
                  <span className="block text-[13px] text-muted">{[quandoProtocolo(x), x.unidade].filter(Boolean).join(' · ')}</span>
                </span>
                {hoje.has(x.id) && <Pill tone={hoje.get(x.id) ? 'ok' : 'neutral'}>{hoje.get(x.id) ? 'Feito hoje' : 'Pendente hoje'}</Pill>}
                <Pencil className="mt-1 h-4 w-4 shrink-0 text-faint" />
              </button>
            ))}
          </Card>
        </>
      )}
      <ProtocoloSheet id={id} item={editar} onClose={() => setEditar(null)} />
    </section>
  );
}

const DIAS = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sab', 'Dom'];

function ProtocoloSheet({ id, item, onClose }: { id: string; item: ItemProtocolo | 'novo' | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [tipo, setTipo] = useState('Peso');
  const [titulo, setTitulo] = useState('');
  const [unidade, setUnidade] = useState('');
  const [freq, setFreq] = useState('Diario');
  const [horario, setHorario] = useState('');
  const [dias, setDias] = useState<string[]>([]);
  const [instrucoes, setInstrucoes] = useState('');
  useEffect(() => {
    if (!item) return;
    const x = item === 'novo' ? null : item;
    setTipo(x?.tipo ?? 'Peso');
    setTitulo(x?.titulo ?? TIPOS_PROTOCOLO.Peso.l);
    setUnidade(x?.unidade ?? TIPOS_PROTOCOLO.Peso.u);
    setFreq(x?.frequencia ?? 'Diario');
    setHorario(x?.horarioLocal ?? '');
    setDias(x?.diasSemana ? x.diasSemana.split(',').map((d) => d.trim()) : []);
    setInstrucoes(x?.instrucoes ?? '');
  }, [item]);
  const escolherTipo = (t: string) => {
    // Troca titulo e unidade padrao se ainda nao foram personalizados.
    if (!titulo.trim() || titulo === TIPOS_PROTOCOLO[tipo]?.l) setTitulo(TIPOS_PROTOCOLO[t].l);
    if (!unidade.trim() || unidade === TIPOS_PROTOCOLO[tipo]?.u) setUnidade(TIPOS_PROTOCOLO[t].u);
    setTipo(t);
  };
  const existente = item && item !== 'novo' ? item : null;
  const json = (ativo: boolean) => ({
    tipo,
    titulo: titulo.trim(),
    unidade: unidade.trim() || null,
    frequencia: freq,
    horarioLocal: horario || null,
    diasSemana: freq === 'DiasSemana' ? DIAS.filter((d) => dias.includes(d)).join(',') : null,
    instrucoes: instrucoes.trim() || null,
    ativo,
  });
  const invalidar = () => qc.invalidateQueries({ queryKey: ['pro', 'cuidados', 'protocolo', id] });
  const salvar = useMutation({
    mutationFn: () => (existente ? api(`/api/protocolos-acompanhamento/${existente.id}`, { method: 'PUT', json: json(true) }) : api(`/api/pacientes/${id}/protocolo-acompanhamento`, { method: 'POST', json: json(true) })),
    onSuccess: () => {
      toast(existente ? 'Atualizado.' : 'Adicionado ao monitoramento.');
      invalidar();
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível salvar.'),
  });
  const remover = useMutation({
    mutationFn: () => api(`/api/protocolos-acompanhamento/${existente!.id}`, { method: 'DELETE' }),
    onSuccess: () => {
      toast('Removido do monitoramento.');
      invalidar();
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível remover.'),
  });
  const valido = !!titulo.trim() && (freq !== 'DiasSemana' || dias.length > 0);
  return (
    <Sheet
      open={!!item}
      onClose={onClose}
      title={existente ? 'Editar monitoramento' : 'O que o paciente deve registrar'}
      footer={
        <div className="flex flex-col gap-2">
          <Button size="lg" className="w-full" disabled={!valido} loading={salvar.isPending} onClick={() => salvar.mutate()}>
            {existente ? 'Salvar' : 'Adicionar'}
          </Button>
          {existente && (
            <Button variant="danger" className="w-full" loading={remover.isPending} onClick={() => remover.mutate()}>
              Remover do monitoramento
            </Button>
          )}
        </div>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="O que medir" group>
          <Chips value={tipo} onChange={escolherTipo} options={Object.entries(TIPOS_PROTOCOLO).map(([value, t]) => ({ value, label: t.l }))} />
        </Field>
        <div className="grid grid-cols-[1fr_7rem] gap-3">
          <Field label="Nome que o paciente vê">
            <input className={inputCls} value={titulo} maxLength={180} onChange={(e) => setTitulo(e.target.value)} />
          </Field>
          <Field label="Unidade">
            <input className={inputCls} value={unidade} maxLength={30} onChange={(e) => setUnidade(e.target.value)} />
          </Field>
        </div>
        <Field label="Frequência" group>
          <Chips value={freq} onChange={setFreq} options={Object.entries(FREQUENCIAS_PROTOCOLO).map(([value, label]) => ({ value, label }))} />
        </Field>
        {freq === 'DiasSemana' && (
          <div className="grid grid-cols-7 gap-1">
            {DIAS.map((d) => (
              <button
                key={d}
                type="button"
                onClick={() => setDias((x) => (x.includes(d) ? x.filter((y) => y !== d) : [...x, d]))}
                className={clsx('h-10 rounded-lg text-[13px] font-semibold transition', dias.includes(d) ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}
              >
                {d === 'Sab' ? 'Sáb' : d}
              </button>
            ))}
          </div>
        )}
        {freq !== 'SobDemanda' && (
          <Field label="Horário sugerido (opcional)">
            <input type="time" className={inputCls} value={horario} onChange={(e) => setHorario(e.target.value)} />
          </Field>
        )}
        <Field label="Instruções ao paciente (opcional)">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={instrucoes} maxLength={1200} placeholder="Ex.: em jejum, logo ao acordar" onChange={(e) => setInstrucoes(e.target.value)} />
        </Field>
      </div>
    </Sheet>
  );
}

