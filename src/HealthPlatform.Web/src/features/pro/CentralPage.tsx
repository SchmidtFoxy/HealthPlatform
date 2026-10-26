import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { CalendarPlus, Check, ChevronDown, ClipboardCheck, Clock, ExternalLink, Eye, MessageCircle, Phone, PhoneCall, X } from 'lucide-react';
import { ReactNode, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Field, inputCls, Pill, Segmented, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta, tempoRelativo } from '@/lib/format';

// ---------------------------------------------------------------- Tipos

interface Pendencia {
  id: string;
  pacienteId: string;
  pacienteNome: string;
  profissionalNome: string;
  categoria: string;
  severidade: 'Alta' | 'Media' | 'Baixa' | string;
  titulo: string;
  descricao: string | null;
  valorReferencia: string | null;
  acaoSugerida: string | null;
  status: 'Nova' | 'Vista' | 'Adiada' | 'Resolvida' | string;
  vencimentoUtc: string | null;
  adiadaAteUtc: string | null;
  resolvidaEmUtc: string | null;
  resolucao: string | null;
  consultaRetornoId: string | null;
  createdAtUtc: string;
}

interface ContatoFila {
  pacienteId: string;
  pacienteNome: string;
  telefone: string | null;
  email: string | null;
  ultimoContatoUtc: string;
  ultimoCanal: string;
  ultimoResultado: string;
  proximoContatoUtc: string;
  diasAtraso: number;
  faixa: 'Vencido' | 'Hoje' | 'Proximos7Dias' | 'Futuro' | string;
  prioridade: string;
  contatosUltimos30Dias: number;
}

interface Pedido {
  id: string;
  pacienteId: string;
  pacienteNome: string;
  tipo: string;
  titulo: string;
  descricao: string | null;
  dataLimiteUtc: string | null;
  status: 'Pendente' | 'Enviada' | 'Revisada' | 'Cancelada' | string;
  respostaPaciente: string | null;
  linkResposta: string | null;
  respondidaEmUtc: string | null;
  observacaoRevisao: string | null;
  createdAtUtc: string;
}

type Aba = 'pendencias' | 'contatos' | 'pedidos';

// ---------------------------------------------------------------- Utilidades

const pad = (n: number) => String(n).padStart(2, '0');
export const inputLocal = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
const daquiA = (dias: number, h = 9) => {
  const d = new Date();
  d.setDate(d.getDate() + dias);
  d.setHours(h, 0, 0, 0);
  return inputLocal(d);
};
const quando = (iso: string) => new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
const erro = (e: unknown, padrao: string) => toast(e instanceof Error ? e.message : padrao, 'bad');

const SEVERIDADE: Record<string, { l: string; tone: 'bad' | 'warn' | 'neutral' }> = {
  Alta: { l: 'Alta', tone: 'bad' },
  Media: { l: 'Média', tone: 'warn' },
  Baixa: { l: 'Baixa', tone: 'neutral' },
};

const FAIXA: Record<string, { l: string; tone: 'bad' | 'warn' | 'info' | 'neutral' }> = {
  Vencido: { l: 'Atrasado', tone: 'bad' },
  Hoje: { l: 'Hoje', tone: 'warn' },
  Proximos7Dias: { l: 'Esta semana', tone: 'info' },
  Futuro: { l: 'Mais adiante', tone: 'neutral' },
};

const whatsapp = (tel: string | null) => {
  const n = (tel ?? '').replace(/\D/g, '');
  if (n.length < 10) return null;
  return `https://wa.me/${n.length <= 11 ? `55${n}` : n}`;
};

// Mensagens do chat tambem entram no historico de contatos, com canal "Chat:<contexto>".
const ultimoContato = (c: ContatoFila) =>
  c.ultimoCanal.startsWith('Chat:') ? (c.ultimoResultado === 'Paciente' ? 'mensagem do paciente no chat' : 'mensagem sua no chat') : `${c.ultimoCanal}: ${c.ultimoResultado}`;

function invalidar(qc: ReturnType<typeof useQueryClient>) {
  qc.invalidateQueries({ queryKey: ['pro', 'central'] });
  qc.invalidateQueries({ queryKey: ['pro', 'pendencias'] });
  qc.invalidateQueries({ queryKey: ['pro', 'contatos'] });
  qc.invalidateQueries({ queryKey: ['pro', 'pedidos'] });
  qc.invalidateQueries({ queryKey: ['pro', 'resumo'] });
}

// ---------------------------------------------------------------- Consultas

export interface CentralDia {
  consultasHoje: number;
  followUpsVencidos: number;
  followUpsHoje: number;
  pendenciasPrioritarias: number;
  solicitacoesParaRevisao: number;
  solicitacoesVencidas: number;
  pacientesEmAtencao: number;
}

export const useCentralDia = () =>
  useQuery({ queryKey: ['pro', 'central'], queryFn: () => api<CentralDia>(`/api/central-dia?offsetMinutos=${-new Date().getTimezoneOffset()}`) });

// ---------------------------------------------------------------- Pagina

export function CentralPage() {
  const [params, setParams] = useSearchParams();
  const aba = (['pendencias', 'contatos', 'pedidos'].includes(params.get('aba') ?? '') ? params.get('aba') : 'pendencias') as Aba;
  const central = useCentralDia();
  const c = central.data;
  const contatos = c ? c.followUpsVencidos + c.followUpsHoje : 0;

  return (
    <Page>
      <PageHeader back="/pro" eyebrow="Central do dia" title="Para resolver" subtitle="Pendências clínicas, contatos de acompanhamento e pedidos respondidos pelos pacientes." />
      <Segmented<Aba>
        value={aba}
        onChange={(v) => setParams({ aba: v }, { replace: true })}
        options={[
          { value: 'pendencias', label: `Pendências${c?.pendenciasPrioritarias ? ` ${c.pendenciasPrioritarias}` : ''}` },
          { value: 'contatos', label: `Contatos${contatos ? ` ${contatos}` : ''}` },
          { value: 'pedidos', label: `Pedidos${c?.solicitacoesParaRevisao ? ` ${c.solicitacoesParaRevisao}` : ''}` },
        ]}
      />
      <div className="mt-4">
        {aba === 'pendencias' && <PendenciasAba />}
        {aba === 'contatos' && <ContatosAba />}
        {aba === 'pedidos' && <PedidosAba />}
      </div>
    </Page>
  );
}

function Filtros<T extends string>({ value, onChange, options }: { value: T; onChange: (v: T) => void; options: { value: T; label: string }[] }) {
  return (
    <div className="mb-3 flex gap-2 overflow-x-auto pb-1">
      {options.map((o) => (
        <button
          key={o.value}
          onClick={() => onChange(o.value)}
          className={clsx('h-9 shrink-0 rounded-full border px-3.5 text-sm font-semibold transition', value === o.value ? 'border-accent bg-accent/10 text-accent' : 'border-line text-muted hover:bg-raised')}
        >
          {o.label}
        </button>
      ))}
    </div>
  );
}

function Lista({ q, vazio, children }: { q: { isLoading: boolean; isError: boolean; error: unknown; refetch: () => unknown }; vazio: ReactNode; children: ReactNode }) {
  if (q.isError) return <ErrorBox error={q.error} onRetry={() => q.refetch()} />;
  if (q.isLoading) return <Skeleton className="h-56" />;
  return <>{children || vazio}</>;
}

// ---------------------------------------------------------------- Pendencias

type FiltroPendencia = 'abertas' | 'Adiada' | 'Resolvida';

function PendenciasAba() {
  const [filtro, setFiltro] = useState<FiltroPendencia>('abertas');
  const q = useQuery({ queryKey: ['pro', 'pendencias', filtro], queryFn: () => api<{ itens: Pendencia[] }>(`/api/pendencias?status=${filtro}&limite=200`), placeholderData: (p) => p });
  const [aberta, setAberta] = useState<string | null>(null);
  const [acao, setAcao] = useState<{ tipo: 'adiar' | 'resolver' | 'retorno'; p: Pendencia } | null>(null);
  const qc = useQueryClient();
  const vista = useMutation({
    mutationFn: (id: string) => api(`/api/pendencias/${id}/vista`, { method: 'PUT' }),
    onSuccess: () => invalidar(qc),
    onError: (e) => erro(e, 'Não foi possível marcar como vista.'),
  });
  const itens = q.data?.itens ?? [];

  return (
    <>
      <Filtros
        value={filtro}
        onChange={setFiltro}
        options={[
          { value: 'abertas', label: 'Abertas' },
          { value: 'Adiada', label: 'Adiadas' },
          { value: 'Resolvida', label: 'Resolvidas' },
        ]}
      />
      <Lista
        q={q}
        vazio={
          <Empty icon={<ClipboardCheck className="h-5 w-5" />} title={filtro === 'abertas' ? 'Nenhuma pendência aberta' : 'Nada por aqui'}>
            {filtro === 'abertas' ? 'Alertas de check-in, exames e adesão viram pendências aqui quando pedem uma ação sua.' : undefined}
          </Empty>
        }
      >
        {!!itens.length && (
          <Card className="divide-y divide-line/60">
            {itens.map((p) => {
              const s = SEVERIDADE[p.severidade] ?? { l: p.severidade, tone: 'neutral' as const };
              const open = aberta === p.id;
              const vencida = p.status !== 'Resolvida' && p.vencimentoUtc && new Date(p.vencimentoUtc) < new Date();
              return (
                <div key={p.id} className="[overflow-wrap:anywhere]">
                  <button
                    className="flex w-full items-start gap-3 p-3.5 text-left"
                    aria-expanded={open}
                    onClick={() => {
                      setAberta(open ? null : p.id);
                      if (!open && p.status === 'Nova') vista.mutate(p.id);
                    }}
                  >
                    <span className={clsx('mt-1.5 h-2.5 w-2.5 shrink-0 rounded-full', s.tone === 'bad' ? 'bg-bad' : s.tone === 'warn' ? 'bg-warn' : 'bg-line')} />
                    <span className="min-w-0 flex-1">
                      <span className={clsx('block', p.status === 'Nova' ? 'font-bold' : 'font-semibold')}>{p.titulo}</span>
                      <span className="block text-[13px] text-muted">
                        {p.pacienteNome} · {p.categoria}
                        {p.status === 'Adiada' && p.adiadaAteUtc && ` · adiada até ${dataCurta(p.adiadaAteUtc)}`}
                        {p.status === 'Resolvida' && p.resolvidaEmUtc && ` · resolvida ${tempoRelativo(p.resolvidaEmUtc)}`}
                      </span>
                      <span className="mt-1 flex flex-wrap gap-1.5">
                        <Pill tone={s.tone}>{s.l}</Pill>
                        {p.status === 'Nova' && <Pill tone="accent">Nova</Pill>}
                        {vencida && <Pill tone="bad">Venceu {dataCurta(p.vencimentoUtc!)}</Pill>}
                        {!vencida && p.vencimentoUtc && p.status !== 'Resolvida' && <Pill>Até {dataCurta(p.vencimentoUtc)}</Pill>}
                        {p.consultaRetornoId && <Pill tone="info">Retorno marcado</Pill>}
                      </span>
                    </span>
                    <ChevronDown className={clsx('mt-1 h-5 w-5 shrink-0 text-faint transition', open && 'rotate-180')} />
                  </button>
                  {open && (
                    <div className="px-3.5 pb-3.5 pl-9">
                      {p.descricao && <p className="text-[14px] leading-snug">{p.descricao}</p>}
                      {p.valorReferencia && <p className="mt-1.5 text-[13px] text-muted">Referência: {p.valorReferencia}</p>}
                      {p.acaoSugerida && (
                        <p className="mt-2 rounded-xl bg-accent/10 px-3 py-2 text-[13px]">
                          <b className="text-accent">Sugestão:</b> {p.acaoSugerida}
                        </p>
                      )}
                      {p.resolucao && <p className="mt-2 text-[13px] text-muted">Resolução: {p.resolucao}</p>}
                      <div className="mt-3 flex flex-wrap gap-2">
                        {p.status !== 'Resolvida' && (
                          <>
                            <Button size="sm" icon={<Check className="h-4 w-4" />} onClick={() => setAcao({ tipo: 'resolver', p })}>
                              Resolver
                            </Button>
                            <Button size="sm" variant="outline" icon={<Clock className="h-4 w-4" />} onClick={() => setAcao({ tipo: 'adiar', p })}>
                              Adiar
                            </Button>
                            {!p.consultaRetornoId && (
                              <Button size="sm" variant="outline" icon={<CalendarPlus className="h-4 w-4" />} onClick={() => setAcao({ tipo: 'retorno', p })}>
                                Marcar retorno
                              </Button>
                            )}
                          </>
                        )}
                        <Link to={`/pro/mensagens/${p.pacienteId}`} className="inline-flex h-9 items-center gap-1.5 rounded-full px-3 text-sm font-semibold text-accent hover:bg-accent/10">
                          <MessageCircle className="h-4 w-4" /> Conversar
                        </Link>
                        <Link to={`/pro/pacientes/${p.pacienteId}`} className="inline-flex h-9 items-center rounded-full px-3 text-sm font-semibold text-accent hover:bg-accent/10">
                          Abrir paciente
                        </Link>
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </Card>
        )}
      </Lista>
      <PendenciaSheet acao={acao} onClose={() => setAcao(null)} />
    </>
  );
}

function PendenciaSheet({ acao, onClose }: { acao: { tipo: 'adiar' | 'resolver' | 'retorno'; p: Pendencia } | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [texto, setTexto] = useState('');
  const [data, setData] = useState('');
  useEffect(() => {
    if (!acao) return;
    setTexto(acao.tipo === 'retorno' ? `Retorno: ${acao.p.titulo}`.slice(0, 200) : '');
    setData(acao.tipo === 'adiar' ? daquiA(3) : daquiA(7, 14));
  }, [acao]);

  const salvar = useMutation({
    mutationFn: () => {
      const id = acao!.p.id;
      if (acao!.tipo === 'resolver') return api(`/api/pendencias/${id}/resolver`, { method: 'PUT', json: { resolucao: texto.trim() || null } });
      if (acao!.tipo === 'adiar') return api(`/api/pendencias/${id}/adiar`, { method: 'PUT', json: { adiadaAteUtc: new Date(data).toISOString(), observacao: texto.trim() || null } });
      return api(`/api/pendencias/${id}/retorno`, { method: 'POST', json: { dataHoraUtc: new Date(data).toISOString(), motivo: texto.trim() || null } });
    },
    onSuccess: () => {
      toast(acao!.tipo === 'resolver' ? 'Pendência resolvida.' : acao!.tipo === 'adiar' ? 'Pendência adiada.' : 'Retorno marcado na agenda.');
      invalidar(qc);
      if (acao!.tipo === 'retorno') qc.invalidateQueries({ queryKey: ['pro', 'agenda'] });
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível salvar.'),
  });

  const t = acao?.tipo;
  const titulo = t === 'resolver' ? 'Resolver pendência' : t === 'adiar' ? 'Adiar pendência' : 'Marcar retorno';
  return (
    <Sheet
      open={!!acao}
      onClose={onClose}
      title={titulo}
      footer={
        <Button size="lg" className="w-full" disabled={t !== 'resolver' && !data} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          {t === 'resolver' ? 'Marcar como resolvida' : t === 'adiar' ? 'Adiar' : 'Agendar retorno'}
        </Button>
      }
    >
      {acao && (
        <div className="flex flex-col gap-4">
          <p className="text-sm text-muted">
            {acao.p.pacienteNome} · {acao.p.titulo}
          </p>
          {t !== 'resolver' && (
            <Field label={t === 'adiar' ? 'Voltar a mostrar em' : 'Dia e horário'}>
              <input type="datetime-local" className={inputCls} value={data} min={inputLocal(new Date())} onChange={(e) => setData(e.target.value)} />
            </Field>
          )}
          {t === 'adiar' && (
            <div className="flex flex-wrap gap-2">
              {[
                { l: 'Amanhã', d: 1 },
                { l: '3 dias', d: 3 },
                { l: '1 semana', d: 7 },
              ].map((x) => (
                <Button key={x.d} size="sm" variant="outline" onClick={() => setData(daquiA(x.d))}>
                  {x.l}
                </Button>
              ))}
            </div>
          )}
          <Field label={t === 'resolver' ? 'O que foi feito (opcional)' : t === 'adiar' ? 'Observação (opcional)' : 'Motivo'}>
            <textarea className={clsx(inputCls, 'h-24 py-2.5')} value={texto} maxLength={1000} onChange={(e) => setTexto(e.target.value)} />
          </Field>
        </div>
      )}
    </Sheet>
  );
}

// ---------------------------------------------------------------- Contatos (follow-ups)

type FiltroContato = 'abertos' | 'vencidos' | 'hoje' | '7dias' | 'futuro';

function ContatosAba() {
  const [filtro, setFiltro] = useState<FiltroContato>('abertos');
  const q = useQuery({ queryKey: ['pro', 'contatos', filtro], queryFn: () => api<{ itens: ContatoFila[] }>(`/api/followups/fila?faixa=${filtro}`), placeholderData: (p) => p });
  const [registrar, setRegistrar] = useState<{ id: string; nome: string } | null>(null);
  const itens = q.data?.itens ?? [];

  return (
    <>
      <Filtros
        value={filtro}
        onChange={setFiltro}
        options={[
          { value: 'abertos', label: 'Para agora' },
          { value: 'vencidos', label: 'Atrasados' },
          { value: 'hoje', label: 'Hoje' },
          { value: '7dias', label: 'Esta semana' },
          { value: 'futuro', label: 'Mais adiante' },
        ]}
      />
      <Lista
        q={q}
        vazio={
          <Empty icon={<PhoneCall className="h-5 w-5" />} title="Nenhum contato previsto">
            Ao registrar um contato com "próximo contato", ele entra nesta fila na data marcada.
          </Empty>
        }
      >
        {!!itens.length && (
          <Card className="divide-y divide-line/60">
            {itens.map((c) => {
              const f = FAIXA[c.faixa] ?? { l: c.faixa, tone: 'neutral' as const };
              const wa = whatsapp(c.telefone);
              return (
                <div key={c.pacienteId} className="p-3.5 [overflow-wrap:anywhere]">
                  <div className="flex items-start gap-3">
                    <div className="min-w-0 flex-1">
                      <Link to={`/pro/pacientes/${c.pacienteId}`} className="font-semibold hover:underline">
                        {c.pacienteNome}
                      </Link>
                      <p className="text-[13px] text-muted">
                        Previsto para {quando(c.proximoContatoUtc)}
                        {c.diasAtraso > 0 && ` · ${c.diasAtraso} ${c.diasAtraso > 1 ? 'dias' : 'dia'} de atraso`}
                      </p>
                      <p className="mt-1 text-[13px] text-faint">
                        Último contato {tempoRelativo(c.ultimoContatoUtc)} · {ultimoContato(c)}
                      </p>
                    </div>
                    <Pill tone={f.tone}>{f.l}</Pill>
                  </div>
                  <div className="mt-2.5 flex flex-wrap gap-2">
                    <Button size="sm" icon={<Check className="h-4 w-4" />} onClick={() => setRegistrar({ id: c.pacienteId, nome: c.pacienteNome })}>
                      Registrar contato
                    </Button>
                    {wa && (
                      <a href={wa} target="_blank" rel="noreferrer" className="inline-flex h-9 items-center gap-1.5 rounded-full border border-line px-3.5 text-sm font-semibold hover:bg-raised">
                        <Phone className="h-4 w-4" /> WhatsApp
                      </a>
                    )}
                    <Link to={`/pro/mensagens/${c.pacienteId}`} className="inline-flex h-9 items-center gap-1.5 rounded-full px-3 text-sm font-semibold text-accent hover:bg-accent/10">
                      <MessageCircle className="h-4 w-4" /> Chat
                    </Link>
                  </div>
                </div>
              );
            })}
          </Card>
        )}
      </Lista>
      <RegistrarContatoSheet paciente={registrar} onClose={() => setRegistrar(null)} />
    </>
  );
}

const CANAIS = ['WhatsApp', 'Telefone', 'Email', 'Presencial', 'Outro'];
const RESULTADOS = ['Paciente respondeu', 'Sem resposta', 'Ajuste combinado', 'Retorno confirmado'];

/** Registra um contato de acompanhamento (follow-up) e, opcionalmente, quando falar de novo. */
export function RegistrarContatoSheet({ paciente, onClose }: { paciente: { id: string; nome: string } | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [canal, setCanal] = useState('WhatsApp');
  const [resultado, setResultado] = useState('');
  const [obs, setObs] = useState('');
  const [proximo, setProximo] = useState('');
  useEffect(() => {
    if (!paciente) return;
    setCanal('WhatsApp');
    setResultado('');
    setObs('');
    setProximo(daquiA(7));
  }, [paciente]);

  const salvar = useMutation({
    mutationFn: () =>
      api(`/api/pacientes/${paciente!.id}/followups`, {
        method: 'POST',
        json: {
          dataHoraUtc: new Date().toISOString(),
          canal,
          resultado: resultado.trim(),
          observacoes: obs.trim() || null,
          proximoContatoUtc: proximo ? new Date(proximo).toISOString() : null,
        },
      }),
    onSuccess: () => {
      toast('Contato registrado.');
      invalidar(qc);
      qc.invalidateQueries({ queryKey: ['pro', 'followups', paciente!.id] });
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível registrar o contato.'),
  });

  return (
    <Sheet
      open={!!paciente}
      onClose={onClose}
      title={`Contato com ${paciente?.nome ?? ''}`}
      footer={
        <Button size="lg" className="w-full" disabled={!resultado.trim()} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Registrar contato
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Canal">
          <div className="flex flex-wrap gap-2">
            {CANAIS.map((c) => (
              <button
                key={c}
                type="button"
                onClick={() => setCanal(c)}
                className={clsx('h-9 rounded-full border px-3.5 text-sm font-semibold transition', canal === c ? 'border-accent bg-accent/10 text-accent' : 'border-line text-muted hover:bg-raised')}
              >
                {c}
              </button>
            ))}
          </div>
        </Field>
        <Field label="Como foi">
          <input className={inputCls} value={resultado} maxLength={300} placeholder="Ex.: respondeu e confirmou o retorno" onChange={(e) => setResultado(e.target.value)} />
        </Field>
        <div className="-mt-2 flex flex-wrap gap-2">
          {RESULTADOS.map((r) => (
            <button key={r} type="button" onClick={() => setResultado(r)} className="h-8 rounded-full bg-raised px-3 text-[13px] font-medium text-muted hover:text-fg">
              {r}
            </button>
          ))}
        </div>
        <Field label="Observações (opcional)">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={obs} maxLength={2000} onChange={(e) => setObs(e.target.value)} />
        </Field>
        <Field label="Próximo contato" hint="Deixe em branco se não precisar de novo contato.">
          <div className="flex gap-2">
            <input type="datetime-local" className={inputCls} value={proximo} min={inputLocal(new Date())} onChange={(e) => setProximo(e.target.value)} />
            {proximo && (
              <button type="button" aria-label="Sem próximo contato" onClick={() => setProximo('')} className="grid h-11 w-11 shrink-0 place-items-center rounded-xl border border-line text-muted hover:bg-raised">
                <X className="h-4 w-4" />
              </button>
            )}
          </div>
        </Field>
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Pedidos (solicitacoes clinicas)

type FiltroPedido = 'Enviada' | 'Pendente' | 'Revisada' | 'Todos';

function PedidosAba() {
  const [filtro, setFiltro] = useState<FiltroPedido>('Enviada');
  const q = useQuery({ queryKey: ['pro', 'pedidos', filtro], queryFn: () => api<{ itens: Pedido[] }>(`/api/solicitacoes?status=${filtro}`), placeholderData: (p) => p });
  const [revisar, setRevisar] = useState<Pedido | null>(null);
  const qc = useQueryClient();
  const cancelar = useMutation({
    mutationFn: (id: string) => api(`/api/solicitacoes/${id}/cancelar`, { method: 'PUT' }),
    onSuccess: () => {
      toast('Pedido cancelado.');
      invalidar(qc);
    },
    onError: (e) => erro(e, 'Não foi possível cancelar.'),
  });
  const itens = q.data?.itens ?? [];

  return (
    <>
      <Filtros
        value={filtro}
        onChange={setFiltro}
        options={[
          { value: 'Enviada', label: 'Para revisar' },
          { value: 'Pendente', label: 'Aguardando paciente' },
          { value: 'Revisada', label: 'Revisados' },
          { value: 'Todos', label: 'Todos' },
        ]}
      />
      <Lista
        q={q}
        vazio={
          <Empty icon={<ClipboardCheck className="h-5 w-5" />} title={filtro === 'Enviada' ? 'Nenhuma resposta para revisar' : 'Nada por aqui'}>
            {filtro === 'Enviada' ? 'Quando um paciente responder a um pedido seu (exame, foto, registro), ele aparece aqui.' : undefined}
          </Empty>
        }
      >
        {!!itens.length && (
          <Card className="divide-y divide-line/60">
            {itens.map((p) => {
              const vencido = p.status === 'Pendente' && p.dataLimiteUtc && new Date(p.dataLimiteUtc) < new Date();
              return (
                <div key={p.id} className="p-3.5 [overflow-wrap:anywhere]">
                  <div className="flex items-start gap-3">
                    <div className="min-w-0 flex-1">
                      <p className="font-semibold">{p.titulo}</p>
                      <p className="text-[13px] text-muted">
                        <Link to={`/pro/pacientes/${p.pacienteId}`} className="hover:underline">
                          {p.pacienteNome}
                        </Link>{' '}
                        · {p.tipo}
                        {p.dataLimiteUtc && ` · até ${dataCurta(p.dataLimiteUtc)}`}
                      </p>
                    </div>
                    <Pill tone={p.status === 'Enviada' ? 'accent' : vencido ? 'bad' : p.status === 'Revisada' ? 'ok' : 'neutral'}>
                      {p.status === 'Enviada' ? 'Respondido' : vencido ? 'Vencido' : p.status === 'Pendente' ? 'Aguardando' : p.status}
                    </Pill>
                  </div>
                  {p.descricao && <p className="mt-1.5 text-[13px] text-muted">{p.descricao}</p>}
                  {(p.respostaPaciente || p.linkResposta) && (
                    <div className="mt-2 rounded-xl bg-raised px-3 py-2 text-[14px]">
                      <p className="text-[12px] font-semibold text-faint">Resposta {p.respondidaEmUtc && tempoRelativo(p.respondidaEmUtc)}</p>
                      {p.respostaPaciente && <p className="mt-0.5">{p.respostaPaciente}</p>}
                      {p.linkResposta && (
                        <a href={p.linkResposta} target="_blank" rel="noreferrer" className="mt-1 inline-flex items-center gap-1 font-semibold text-accent">
                          Abrir anexo <ExternalLink className="h-3.5 w-3.5" />
                        </a>
                      )}
                    </div>
                  )}
                  {p.observacaoRevisao && <p className="mt-1.5 text-[13px] text-muted">Sua revisão: {p.observacaoRevisao}</p>}
                  {(p.status === 'Enviada' || p.status === 'Pendente') && (
                    <div className="mt-2.5 flex flex-wrap gap-2">
                      {p.status === 'Enviada' && (
                        <Button size="sm" icon={<Eye className="h-4 w-4" />} onClick={() => setRevisar(p)}>
                          Revisar
                        </Button>
                      )}
                      <Link to={`/pro/mensagens/${p.pacienteId}`} className="inline-flex h-9 items-center gap-1.5 rounded-full px-3 text-sm font-semibold text-accent hover:bg-accent/10">
                        <MessageCircle className="h-4 w-4" /> Conversar
                      </Link>
                      <Button
                        size="sm"
                        variant="ghost"
                        className="text-muted"
                        loading={cancelar.isPending && cancelar.variables === p.id}
                        onClick={() => window.confirm('Cancelar este pedido?') && cancelar.mutate(p.id)}
                      >
                        Cancelar pedido
                      </Button>
                    </div>
                  )}
                </div>
              );
            })}
          </Card>
        )}
      </Lista>
      <RevisarPedidoSheet pedido={revisar} onClose={() => setRevisar(null)} />
    </>
  );
}

function RevisarPedidoSheet({ pedido, onClose }: { pedido: Pedido | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [obs, setObs] = useState('');
  useEffect(() => setObs(''), [pedido]);
  const salvar = useMutation({
    mutationFn: () => api(`/api/solicitacoes/${pedido!.id}/revisar`, { method: 'PUT', json: { observacao: obs.trim() || null } }),
    onSuccess: () => {
      toast('Pedido revisado.');
      invalidar(qc);
      onClose();
    },
    onError: (e) => erro(e, 'Não foi possível revisar.'),
  });
  return (
    <Sheet
      open={!!pedido}
      onClose={onClose}
      title="Revisar resposta"
      footer={
        <Button size="lg" className="w-full" loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Marcar como revisado
        </Button>
      }
    >
      {pedido && (
        <div className="flex flex-col gap-4">
          <p className="text-sm text-muted">
            {pedido.pacienteNome} · {pedido.titulo}
          </p>
          <Field label="Observação para o registro (opcional)">
            <textarea className={clsx(inputCls, 'h-24 py-2.5')} value={obs} maxLength={2000} onChange={(e) => setObs(e.target.value)} />
          </Field>
        </div>
      )}
    </Sheet>
  );
}
