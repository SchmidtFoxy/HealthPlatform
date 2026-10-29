import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { AlertTriangle, CalendarCheck, ChevronDown, Download, Eye, FileText, Plus, Search, ShieldCheck, UserPlus, Users } from 'lucide-react';
import { ReactNode, useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Field, inputCls, Pill, Segmented, Sheet, Skeleton, toast } from '@/components/ui';
import { api, ApiError, iniciarSimulacao, lerSessao } from '@/lib/api';
import { n0, tempoRelativo } from '@/lib/format';
import { REGRAS_SENHA } from '../auth/LoginPage';
import { Chips } from './CuidadosPro';

const erroToast = (e: unknown) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad');
const dia = (iso: string | null | undefined) => (iso ? new Date(iso).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }) : '—');

type Aba = 'indicadores' | 'carteira' | 'equipe' | 'clinica';

/** Gestao da clinica: indicadores do periodo, carteira por prioridade, equipe e dados da organizacao. */
export function GestaoPage() {
  const [params, setParams] = useSearchParams();
  const aba = (params.get('aba') as Aba) || 'indicadores';
  return (
    <Page>
      <PageHeader back="/pro/perfil" title="Gestão" subtitle="Indicadores, carteira, equipe e dados da clínica." />
      <Segmented<Aba>
        compact
        value={aba}
        onChange={(v) => setParams({ aba: v }, { replace: true })}
        options={[
          { value: 'indicadores', label: 'Indicadores' },
          { value: 'carteira', label: 'Carteira' },
          { value: 'equipe', label: 'Equipe' },
          { value: 'clinica', label: 'Clínica' },
        ]}
      />
      <div className="mt-4">
        {aba === 'indicadores' && <Indicadores />}
        {aba === 'carteira' && <Carteira />}
        {aba === 'equipe' && <Equipe />}
        {aba === 'clinica' && <Clinica />}
      </div>
    </Page>
  );
}

// ---------------------------------------------------------------- Indicadores

interface Serie {
  rotulo: string;
  valor: number;
}

interface ResumoGestao {
  dias: number;
  pacientesAtivos: number;
  pacientesNovos: number;
  consultasTotal: number;
  consultasRealizadas: number;
  consultasAgendadas: number;
  consultasCanceladas: number;
  faltas: number;
  taxaComparecimentoPct: number | null;
  followUpsRealizados: number;
  followUpsVencidos: number;
  pendenciasAbertas: number;
  pendenciasResolvidasPeriodo: number;
  treinosRegistrados: number;
  registrosDiario: number;
  registrosMetas: number;
  consultasPorStatus: Serie[];
  atividadePorSemana: Serie[];
  pacientesAtencao: { pacienteId: string; nome: string; pendenciasAbertas: number; insightsEstimados: number; semRetornoFuturo: boolean; ultimaConsultaUtc: string | null; proximaConsultaUtc: string | null }[];
}

async function baixar(path: string, nome: string, abrir = false) {
  const aba = abrir ? window.open('', '_blank') : null;
  const s = lerSessao();
  const r = await fetch(path, { headers: s ? { Authorization: `Bearer ${s.token}` } : {} });
  if (!r.ok) {
    aba?.close();
    throw new Error('Não foi possível gerar o arquivo.');
  }
  const url = URL.createObjectURL(await r.blob());
  if (aba) aba.location.href = url;
  else {
    const a = document.createElement('a');
    a.href = url;
    a.download = nome;
    a.click();
  }
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}

export function Numero({ v, l, tone, icon }: { v: number | null | undefined; l: string; tone?: string; icon?: ReactNode }) {
  return (
    <Card className="p-4">
      {icon}
      <p className={clsx('num mt-2 text-[28px] font-semibold leading-none', tone)}>{v == null ? '—' : n0(v)}</p>
      <p className="mt-1.5 text-[13px] text-muted">{l}</p>
    </Card>
  );
}

function Barras({ itens }: { itens: Serie[] }) {
  const max = Math.max(1, ...itens.map((x) => x.valor));
  return (
    <div className="flex flex-col gap-2.5">
      {itens.map((x) => (
        <div key={x.rotulo} className="grid grid-cols-[96px_1fr_32px] items-center gap-3 text-[13px]">
          <span className="text-muted">{x.rotulo}</span>
          <span className="h-2 overflow-hidden rounded-full bg-raised">
            <span className="block h-full rounded-full bg-accent" style={{ width: `${Math.max(x.valor ? 4 : 0, (x.valor / max) * 100)}%` }} />
          </span>
          <span className="num text-right font-semibold">{x.valor}</span>
        </div>
      ))}
    </div>
  );
}

function Colunas({ itens }: { itens: Serie[] }) {
  const max = Math.max(1, ...itens.map((x) => x.valor));
  return (
    <div className="flex h-32 items-end gap-2">
      {itens.map((x) => (
        <div key={x.rotulo} className="flex h-full flex-1 flex-col items-center justify-end gap-1.5" title={`${x.rotulo}: ${x.valor}`}>
          <span className="num text-[12px] font-semibold">{x.valor}</span>
          <span className="w-full max-w-[40px] rounded-t-lg bg-accent/80" style={{ height: `${Math.max(4, (x.valor / max) * 100)}%` }} />
          <span className="text-[12px] text-muted">{x.rotulo}</span>
        </div>
      ))}
    </div>
  );
}

export function Indicadores() {
  const [dias, setDias] = useState('30');
  const q = useQuery({ queryKey: ['pro', 'gestao', dias], queryFn: () => api<ResumoGestao>(`/api/gestao/resumo?dias=${dias}`), placeholderData: (p) => p });
  const [baixando, setBaixando] = useState<'csv' | 'html' | null>(null);
  const exportar = async (tipo: 'csv' | 'html') => {
    setBaixando(tipo);
    try {
      await baixar(`/api/gestao/export/${tipo}?dias=${dias}`, `aesyn-gestao-${dias}d.${tipo}`, tipo === 'html');
    } catch (e) {
      erroToast(e);
    } finally {
      setBaixando(null);
    }
  };
  const d = q.data;
  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Chips
          value={dias}
          onChange={setDias}
          options={[
            { value: '7', label: '7 dias' },
            { value: '30', label: '30 dias' },
            { value: '90', label: '90 dias' },
            { value: '365', label: '1 ano' },
          ]}
        />
      </div>
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {!d ? (
        <Skeleton className="mt-4 h-64" />
      ) : (
        <div className="mt-4 flex flex-col gap-4">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Numero v={d.pacientesAtivos} l="pacientes ativos" icon={<Users className="h-4 w-4 text-accent" />} />
            <Numero v={d.pacientesNovos} l="novos no período" icon={<UserPlus className="h-4 w-4 text-info" />} />
            <Numero v={d.consultasRealizadas} l={`consultas realizadas de ${d.consultasTotal}`} icon={<CalendarCheck className="h-4 w-4 text-ok" />} />
            <Numero v={d.taxaComparecimentoPct} l="% de comparecimento" icon={<ShieldCheck className="h-4 w-4 text-ok" />} />
          </div>
          <Card className="p-4">
            <p className="mb-3 font-semibold">Consultas por status</p>
            <Barras itens={d.consultasPorStatus} />
          </Card>
          <Card className="p-4">
            <p className="mb-3 font-semibold">Atividade dos pacientes por semana</p>
            <Colunas itens={d.atividadePorSemana} />
          </Card>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            <Numero v={d.followUpsRealizados} l="contatos feitos" />
            <Numero v={d.followUpsVencidos} l="contatos atrasados" tone={d.followUpsVencidos ? 'text-warn' : undefined} />
            <Numero v={d.pendenciasAbertas} l="pendências abertas" tone={d.pendenciasAbertas ? 'text-warn' : undefined} />
            <Numero v={d.pendenciasResolvidasPeriodo} l="pendências resolvidas" />
            <Numero v={d.treinosRegistrados} l="treinos registrados" />
            <Numero v={d.registrosDiario} l="registros no diário" />
          </div>
          {!!d.pacientesAtencao.length && (
            <Card className="divide-y divide-line/60">
              <p className="p-4 pb-3 font-semibold">Quem pede atenção</p>
              {d.pacientesAtencao.map((x) => (
                <Link key={x.pacienteId} to={`/pro/pacientes/${x.pacienteId}`} className="flex items-center gap-3 p-3.5 transition hover:bg-raised/40">
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-semibold">{x.nome}</span>
                    <span className="block text-[13px] text-muted">
                      {[x.pendenciasAbertas ? `${x.pendenciasAbertas} pendência${x.pendenciasAbertas > 1 ? 's' : ''}` : null, x.insightsEstimados ? `${x.insightsEstimados} sina${x.insightsEstimados > 1 ? 'is' : 'l'}` : null, x.semRetornoFuturo ? 'sem retorno marcado' : null].filter(Boolean).join(' · ')}
                    </span>
                  </span>
                  <AlertTriangle className="h-4 w-4 shrink-0 text-warn" />
                </Link>
              ))}
            </Card>
          )}
          <div className="grid grid-cols-2 gap-2">
            <Button variant="soft" icon={<Download className="h-4 w-4" />} loading={baixando === 'csv'} onClick={() => exportar('csv')}>
              Exportar CSV
            </Button>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} loading={baixando === 'html'} onClick={() => exportar('html')}>
              Relatório
            </Button>
          </div>
        </div>
      )}
    </>
  );
}

// ---------------------------------------------------------------- Carteira

interface ItemCarteira {
  pacienteId: string;
  nome: string;
  ultimaConsultaUtc: string | null;
  proximaConsultaUtc: string | null;
  pesoAtualKg: number | null;
  insights: number;
  pendenciasAbertas: number;
  treinosUltimos30Dias: number;
  registrosDiarioUltimos14Dias: number;
  ultimoContatoUtc: string | null;
  proximoContatoUtc: string | null;
  prioridade: 'Alta' | 'Media' | 'Baixa' | 'SemSinais' | string;
  score: number;
  motivoPrioridade: string | null;
}

interface ResumoCarteira {
  totalPacientes: number;
  alta: number;
  media: number;
  baixa: number;
  semSinais: number;
  semRetornoFuturo: number;
  pacientes: ItemCarteira[];
}

interface InsightsDashboard {
  pacientes: { pacienteId: string; insights: { codigo: string; categoria: string; severidade: string; titulo: string; descricao: string; acaoSugerida: string | null }[] }[];
}

const PRIORIDADE: Record<string, { l: string; tone: 'bad' | 'warn' | 'info' | 'neutral' }> = {
  Alta: { l: 'Alta', tone: 'bad' },
  Media: { l: 'Média', tone: 'warn' },
  Baixa: { l: 'Baixa', tone: 'info' },
  SemSinais: { l: 'Sem sinais', tone: 'neutral' },
};

function Carteira() {
  const [prioridade, setPrioridade] = useState('Todas');
  const [busca, setBusca] = useState('');
  const [aberto, setAberto] = useState<string | null>(null);
  const q = useQuery({ queryKey: ['pro', 'carteira'], queryFn: () => api<ResumoCarteira>('/api/carteira?ordenar=score') });
  const ins = useQuery({ queryKey: ['pro', 'insights-dashboard'], queryFn: () => api<InsightsDashboard>('/api/insights/dashboard') });
  const sinais = new Map((ins.data?.pacientes ?? []).map((x) => [x.pacienteId, x.insights]));
  const d = q.data;
  const termo = busca.trim().toLowerCase();
  const lista = (d?.pacientes ?? []).filter((x) => (prioridade === 'Todas' || x.prioridade === prioridade) && (!termo || x.nome.toLowerCase().includes(termo)));

  return (
    <>
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {!d ? (
        <Skeleton className="h-64" />
      ) : (
        <div className="flex flex-col gap-3">
          <div className="grid grid-cols-4 gap-2">
            {(
              [
                ['Alta', d.alta],
                ['Media', d.media],
                ['Baixa', d.baixa],
                ['SemSinais', d.semSinais],
              ] as const
            ).map(([k, v]) => (
              <button
                key={k}
                onClick={() => setPrioridade(prioridade === k ? 'Todas' : k)}
                aria-pressed={prioridade === k}
                className={clsx('rounded-xl2 border p-3 text-left transition', prioridade === k ? 'border-accent bg-accent/10' : 'border-line bg-surface hover:bg-raised/40')}
              >
                <span className={clsx('num block text-[22px] font-semibold leading-none', k === 'Alta' && v ? 'text-bad' : k === 'Media' && v ? 'text-warn' : '')}>{v}</span>
                <span className="mt-1 block text-[12px] text-muted">{PRIORIDADE[k].l}</span>
              </button>
            ))}
          </div>
          <label className="relative block">
            <Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-faint" />
            <input className={clsx(inputCls, 'pl-10')} value={busca} placeholder="Buscar paciente" aria-label="Buscar paciente" onChange={(e) => setBusca(e.target.value)} />
          </label>
          <p className="text-sm text-muted">
            {lista.length} de {d.totalPacientes} pacientes ativos{d.semRetornoFuturo ? ` · ${d.semRetornoFuturo} sem retorno marcado` : ''}
          </p>
          {!lista.length ? (
            <Empty icon={<Users className="h-5 w-5" />} title="Ninguém aqui">
              Nenhum paciente com esse filtro.
            </Empty>
          ) : (
            <Card className="divide-y divide-line/60">
              {lista.map((x) => {
                const p = PRIORIDADE[x.prioridade] ?? PRIORIDADE.SemSinais;
                const s = sinais.get(x.pacienteId) ?? [];
                const exp = aberto === x.pacienteId;
                return (
                  <div key={x.pacienteId}>
                    <button onClick={() => setAberto(exp ? null : x.pacienteId)} aria-expanded={exp} className="flex w-full items-start gap-3 p-3.5 text-left transition hover:bg-raised/40">
                      <span className="min-w-0 flex-1">
                        <span className="flex items-center gap-2">
                          <span className="truncate font-semibold">{x.nome}</span>
                          <Pill tone={p.tone}>{p.l}</Pill>
                        </span>
                        <span className="mt-0.5 block text-[13px] text-muted">
                          {x.motivoPrioridade ?? 'Sem sinais'} · último contato {x.ultimoContatoUtc ? tempoRelativo(x.ultimoContatoUtc) : 'nunca'}
                        </span>
                      </span>
                      <ChevronDown className={clsx('mt-0.5 h-5 w-5 shrink-0 text-faint transition', exp && 'rotate-180')} />
                    </button>
                    {exp && (
                      <div className="flex flex-col gap-3 px-3.5 pb-3.5">
                        <div className="grid grid-cols-3 gap-2 text-[13px]">
                          <Mini l="Última consulta" v={dia(x.ultimaConsultaUtc)} />
                          <Mini l="Próxima consulta" v={dia(x.proximaConsultaUtc)} />
                          <Mini l="Próximo contato" v={dia(x.proximoContatoUtc)} />
                          <Mini l="Treinos (30 dias)" v={String(x.treinosUltimos30Dias)} />
                          <Mini l="Diário (14 dias)" v={String(x.registrosDiarioUltimos14Dias)} />
                          <Mini l="Pendências" v={String(x.pendenciasAbertas)} />
                        </div>
                        {s.map((i) => (
                          <div key={i.codigo + i.titulo} className="rounded-xl bg-raised/60 p-3 [overflow-wrap:anywhere]">
                            <div className="flex items-center gap-2">
                              <Pill tone={i.severidade === 'Alta' ? 'bad' : i.severidade === 'Media' ? 'warn' : 'info'}>{i.categoria}</Pill>
                              <span className="text-[14px] font-semibold">{i.titulo}</span>
                            </div>
                            <p className="mt-1 text-[13px] text-muted">{i.descricao}</p>
                            {i.acaoSugerida && <p className="mt-1 text-[13px]">{i.acaoSugerida}</p>}
                          </div>
                        ))}
                        <Link to={`/pro/pacientes/${x.pacienteId}`}>
                          <Button size="sm" variant="soft">
                            Abrir ficha
                          </Button>
                        </Link>
                      </div>
                    )}
                  </div>
                );
              })}
            </Card>
          )}
          <p className="text-[12px] text-faint">A prioridade soma sinais de exames, evolução, pendências e falta de contato. É apoio à organização, não diagnóstico.</p>
        </div>
      )}
    </>
  );
}

function Mini({ l, v }: { l: string; v: string }) {
  return (
    <div className="rounded-xl bg-raised/60 px-2.5 py-2">
      <p className="text-[11px] text-muted">{l}</p>
      <p className="num font-semibold">{v}</p>
    </div>
  );
}

// ---------------------------------------------------------------- Equipe

interface Membro {
  usuarioId: string;
  nome: string;
  email: string;
  tipoUsuario: string;
  ativo: boolean;
  createdAtUtc: string;
  profissionalId: string | null;
  registroProfissional: string | null;
  especialidade: string | null;
  ehUsuarioAtual: boolean;
}

const TIPOS: Record<string, string> = { Admin: 'Administrador', Medico: 'Médico', Nutricionista: 'Nutricionista', Personal: 'Personal', Secretaria: 'Secretaria' };
const exigeRegistro = (t: string) => t === 'Medico' || t === 'Nutricionista' || t === 'Personal';

export function Equipe({ acessos }: { acessos?: Record<string, string | null> } = {}) {
  const q = useQuery({ queryKey: ['pro', 'equipe'], queryFn: () => api<Membro[]>('/api/equipe'), retry: (n, e) => !(e instanceof ApiError && e.status === 403) && n < 2 });
  const [aberto, setAberto] = useState<Membro | 'novo' | null>(null);
  const [senha, setSenha] = useState<Membro | null>(null);
  if (q.error instanceof ApiError && q.error.status === 403)
    return (
      <Empty icon={<ShieldCheck className="h-5 w-5" />} title="Só para administradores">
        Peça a um administrador da clínica para cadastrar ou alterar membros da equipe.
      </Empty>
    );
  const lista = q.data ?? [];
  return (
    <>
      <div className="mb-3 flex items-center justify-between gap-3">
        <p className="text-sm text-muted">
          {lista.filter((x) => x.ativo).length} {lista.filter((x) => x.ativo).length === 1 ? 'pessoa ativa' : 'pessoas ativas'}
        </p>
        <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={() => setAberto('novo')}>
          Novo membro
        </Button>
      </div>
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((m) => (
            <button key={m.usuarioId} onClick={() => setAberto(m)} className="flex w-full items-center gap-3 p-3.5 text-left transition hover:bg-raised/40">
              <span className="min-w-0 flex-1 [overflow-wrap:anywhere]">
                <span className="block font-semibold">
                  {m.nome}
                  {m.ehUsuarioAtual && <span className="font-normal text-muted"> (você)</span>}
                </span>
                <span className="block text-[13px] text-muted">{[TIPOS[m.tipoUsuario] ?? m.tipoUsuario, m.especialidade, m.email].filter(Boolean).join(' · ')}</span>
                {acessos && m.ativo && <span className="block text-[12px] text-faint">{acessos[m.usuarioId] ? `Último acesso ${tempoRelativo(acessos[m.usuarioId])}` : 'Sem acesso registrado'}</span>}
              </span>
              {!m.ativo && <Pill>Inativo</Pill>}
            </button>
          ))}
        </Card>
      )}
      <MembroSheet
        item={aberto}
        onClose={() => setAberto(null)}
        onSenha={(m) => {
          setAberto(null);
          setTimeout(() => setSenha(m), 180);
        }}
      />
      <SenhaSheet membro={senha} onClose={() => setSenha(null)} />
    </>
  );
}

function SenhaRegras({ senha }: { senha: string }) {
  return (
    <ul className="grid grid-cols-2 gap-x-3 gap-y-1 text-[12px]">
      {REGRAS_SENHA.map((r) => (
        <li key={r.l} className={r.ok(senha) ? 'text-ok' : 'text-faint'}>
          {r.ok(senha) ? '✓' : '·'} {r.l}
        </li>
      ))}
    </ul>
  );
}

function MembroSheet({ item, onClose, onSenha }: { item: Membro | 'novo' | null; onClose: () => void; onSenha: (m: Membro) => void }) {
  const qc = useQueryClient();
  const novo = item === 'novo';
  const m = novo ? null : item;
  const [f, setF] = useState({ nome: '', email: '', tipo: 'Medico', senha: '', registro: '', especialidade: '', ativo: true });
  useEffect(() => {
    if (!item) return;
    setF({ nome: m?.nome ?? '', email: m?.email ?? '', tipo: m?.tipoUsuario ?? 'Medico', senha: '', registro: m?.registroProfissional ?? '', especialidade: m?.especialidade ?? '', ativo: m?.ativo ?? true });
  }, [item]);
  const set = (k: keyof typeof f) => (e: { target: { value: string } }) => setF((x) => ({ ...x, [k]: e.target.value }));
  const senhaOk = REGRAS_SENHA.every((r) => r.ok(f.senha));
  const valido = f.nome.trim() && (!novo || (/^\S+@\S+\.\S+$/.test(f.email.trim()) && senhaOk)) && (!exigeRegistro(f.tipo) || f.registro.trim());

  const salvar = useMutation({
    mutationFn: () =>
      novo
        ? api('/api/equipe', { method: 'POST', json: { nome: f.nome.trim(), email: f.email.trim(), tipoUsuario: f.tipo, senhaTemporaria: f.senha, registroProfissional: f.registro.trim() || null, especialidade: f.especialidade.trim() || null } })
        : api(`/api/equipe/${m!.usuarioId}`, { method: 'PUT', json: { nome: f.nome.trim(), tipoUsuario: f.tipo, ativo: f.ativo, registroProfissional: f.registro.trim() || null, especialidade: f.especialidade.trim() || null } }),
    onSuccess: () => {
      toast(novo ? 'Membro cadastrado. Passe a senha temporária com segurança.' : 'Membro atualizado.');
      qc.invalidateQueries({ queryKey: ['pro', 'equipe'] });
      onClose();
    },
    onError: erroToast,
  });

  return (
    <Sheet
      open={!!item}
      onClose={onClose}
      title={novo ? 'Novo membro da equipe' : 'Membro da equipe'}
      footer={
        <Button size="lg" className="w-full" disabled={!valido} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          {novo ? 'Cadastrar' : 'Salvar'}
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome">
          <input className={inputCls} value={f.nome} maxLength={160} onChange={set('nome')} />
        </Field>
        {novo ? (
          <Field label="E-mail de login">
            <input className={inputCls} type="email" inputMode="email" autoCapitalize="none" autoComplete="off" value={f.email} maxLength={200} onChange={set('email')} />
          </Field>
        ) : (
          <p className="text-[13px] text-muted">Login: {m?.email}</p>
        )}
        <Field label="Função" group>
          <Chips value={f.tipo} onChange={(v) => setF((x) => ({ ...x, tipo: v }))} options={Object.entries(TIPOS).map(([value, label]) => ({ value, label }))} />
        </Field>
        {(exigeRegistro(f.tipo) || f.tipo === 'Admin') && (
          <div className="grid grid-cols-2 gap-3">
            <Field label={exigeRegistro(f.tipo) ? 'Registro (CRM, CRN, CREF)' : 'Registro (opcional)'}>
              <input className={inputCls} value={f.registro} maxLength={40} onChange={set('registro')} />
            </Field>
            <Field label="Especialidade">
              <input className={inputCls} value={f.especialidade} maxLength={120} onChange={set('especialidade')} />
            </Field>
          </div>
        )}
        {novo && (
          <Field label="Senha temporária">
            <input className={inputCls} type="text" autoComplete="new-password" value={f.senha} maxLength={100} onChange={set('senha')} />
            <div className="mt-2">
              <SenhaRegras senha={f.senha} />
            </div>
          </Field>
        )}
        {m && !m.ehUsuarioAtual && (
          <>
            <label className="flex items-center justify-between gap-3 rounded-xl bg-raised/60 p-3 text-[14px]">
              <span>
                Acesso ativo
                <span className="block text-[12px] text-muted">Desligado, a pessoa não entra mais. O histórico fica salvo.</span>
              </span>
              <input type="checkbox" checked={f.ativo} onChange={(e) => setF((x) => ({ ...x, ativo: e.target.checked }))} className="h-5 w-5 accent-[rgb(var(--accent))]" />
            </label>
            {m.ativo && (
              <Button variant="ghost" onClick={() => onSenha(m)}>
                Redefinir senha
              </Button>
            )}
            {m.ativo && exigeRegistro(m.tipoUsuario) && lerSessao()?.tipoUsuario === 'Admin' && <VerComo membro={m} />}
          </>
        )}
      </div>
    </Sheet>
  );
}

/** Admin abre o app como o profissional ve, sem poder alterar nada. */
function VerComo({ membro }: { membro: Membro }) {
  const nav = useNavigate();
  const [abrindo, setAbrindo] = useState(false);
  const entrar = async () => {
    setAbrindo(true);
    try {
      await iniciarSimulacao(membro.usuarioId);
      nav('/pro', { replace: true });
    } catch (e) {
      toast(e instanceof Error ? e.message : 'Não foi possível abrir a visão.', 'bad');
      setAbrindo(false);
    }
  };
  return (
    <div className="rounded-xl bg-raised/60 p-3">
      <Button variant="ghost" className="w-full" icon={<Eye className="h-4 w-4" />} loading={abrindo} onClick={entrar}>
        Ver o app como {membro.nome.split(' ')[0]}
      </Button>
      <p className="mt-1 text-center text-[12px] text-muted">Somente leitura, para conferir o que a pessoa vê. Fica registrado na auditoria.</p>
    </div>
  );
}

function SenhaSheet({ membro, onClose }: { membro: Membro | null; onClose: () => void }) {
  const [senha, setSenha] = useState('');
  useEffect(() => setSenha(''), [membro]);
  const salvar = useMutation({
    mutationFn: () => api(`/api/equipe/${membro!.usuarioId}/redefinir-senha`, { method: 'POST', json: { novaSenhaTemporaria: senha } }),
    onSuccess: () => {
      toast('Senha redefinida. Passe a senha temporária com segurança.');
      onClose();
    },
    onError: erroToast,
  });
  return (
    <Sheet
      open={!!membro}
      onClose={onClose}
      title="Redefinir senha"
      footer={
        <Button size="lg" className="w-full" disabled={!REGRAS_SENHA.every((r) => r.ok(senha))} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Redefinir
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <p className="text-[14px] text-muted">Nova senha temporária para {membro?.nome}. A pessoa pode trocar depois em Minha conta.</p>
        <Field label="Senha temporária">
          <input className={inputCls} type="text" autoComplete="new-password" value={senha} maxLength={100} onChange={(e) => setSenha(e.target.value)} />
        </Field>
        <SenhaRegras senha={senha} />
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Clinica

interface ResumoConfig {
  organizacao: { id: string; nome: string; cnpj: string | null; ativa: boolean };
}

export function Clinica() {
  const qc = useQueryClient();
  const q = useQuery({ queryKey: ['pro', 'config'], queryFn: () => api<ResumoConfig>('/api/configuracoes/resumo') });
  const [nome, setNome] = useState('');
  const [cnpj, setCnpj] = useState('');
  useEffect(() => {
    if (!q.data) return;
    setNome(q.data.organizacao.nome);
    setCnpj(q.data.organizacao.cnpj ?? '');
  }, [q.data]);
  const salvar = useMutation({
    mutationFn: () => api('/api/configuracoes/organizacao', { method: 'PUT', json: { nome: nome.trim(), cnpj: cnpj.trim() || null } }),
    onSuccess: () => {
      toast('Dados da clínica salvos.');
      qc.invalidateQueries({ queryKey: ['pro', 'config'] });
    },
    onError: erroToast,
  });
  if (q.isError) return <ErrorBox error={q.error} onRetry={() => q.refetch()} />;
  if (!q.data) return <Skeleton className="h-40" />;
  const mudou = nome.trim() !== q.data.organizacao.nome || (cnpj.trim() || null) !== (q.data.organizacao.cnpj ?? null);
  return (
    <div className="flex flex-col gap-4">
      <Card className="flex flex-col gap-4 p-4">
        <Field label="Nome da clínica">
          <input className={inputCls} value={nome} maxLength={160} onChange={(e) => setNome(e.target.value)} />
        </Field>
        <Field label="CNPJ (opcional)">
          <input className={inputCls} value={cnpj} inputMode="numeric" maxLength={18} onChange={(e) => setCnpj(e.target.value)} />
        </Field>
        <Button disabled={!nome.trim() || !mudou} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar
        </Button>
      </Card>
      <p className="text-[13px] text-muted">O nome aparece nos relatórios e nos convites enviados aos pacientes.</p>
    </div>
  );
}
