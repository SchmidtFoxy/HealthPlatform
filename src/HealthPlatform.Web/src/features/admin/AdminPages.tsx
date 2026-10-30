import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Activity, AlertTriangle, Bell, CalendarCheck, ChevronRight, ClipboardList, KeyRound, LogIn, MailWarning, Pencil, Search, Stethoscope, UserCheck, UserPlus, Users, UserX } from 'lucide-react';
import { ReactNode, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { PAPEL } from '@/components/account';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Field, inputCls, Pill, SectionTitle, Segmented, Sheet, Skeleton, toast } from '@/components/ui';
import { api, TipoUsuario } from '@/lib/api';
import { tempoRelativo } from '@/lib/format';
import type { Paciente } from '@/lib/types';
import { useSessao } from '@/lib/session';
import { Chips } from '../pro/CuidadosPro';
import { AcessoSheet, EditarDadosSheet, STATUS_ACOMPANHAMENTO } from '../pro/FichaAcoes';
import { Clinica, Equipe, Indicadores, Numero } from '../pro/GestaoPage';
import { VinculoPacientePainel } from './VinculoPaciente';

// ---------------------------------------------------------------- Tipos

interface Contagem {
  rotulo: string;
  valor: number;
}

interface ItemAtencao {
  tipo: 'convite' | 'sem-profissional' | 'equipe-sem-acesso' | string;
  titulo: string;
  detalhe: string;
  pacienteId: string | null;
  usuarioId: string | null;
}

interface VisaoGeral {
  dias: number;
  equipeAtiva: number;
  equipeInativa: number;
  equipePorFuncao: Contagem[];
  pacientesAtivos: number;
  pacientesNovos: number;
  pacientesComAcesso: number;
  convitesPendentes: number;
  pacientesSemAcesso: number;
  pacientesSemProfissional: number;
  pessoasQueAcessaram: number;
  consultasPeriodo: number;
  consultasRealizadas: number;
  pendenciasAbertas: number;
  acoesRegistradas: number;
  atencao: ItemAtencao[];
}

type Acesso = 'ativo' | 'convite' | 'bloqueado' | 'sem-acesso';

interface PacienteAdmin {
  id: string;
  nome: string;
  email: string | null;
  telefone: string | null;
  ativo: boolean;
  statusAcompanhamento: string;
  acesso: Acesso;
  ultimoAcessoUtc: string | null;
  profissionalId: string | null;
  profissional: string | null;
  origemResponsavel: string;
  origemResponsavelRotulo: string;
  equipe: { id: string; nome: string; tipo: string }[];
  createdAtUtc: string;
}

interface PacientesAdmin {
  itens: PacienteAdmin[];
  total: number;
  pagina: number;
  totalPaginas: number;
}

interface ItemAtividade {
  id: string;
  quandoUtc: string;
  usuarioId: string | null;
  usuario: string | null;
  funcaoUsuario: TipoUsuario | null;
  acao: string;
  entidade: string;
  entidadeId: string | null;
  alvo: string | null;
}

const ACESSO: Record<Acesso, { l: string; tone: 'ok' | 'warn' | 'bad' | 'neutral' }> = {
  ativo: { l: 'Usa o app', tone: 'ok' },
  convite: { l: 'Convite pendente', tone: 'warn' },
  bloqueado: { l: 'Acesso revogado', tone: 'bad' },
  'sem-acesso': { l: 'Sem acesso', tone: 'neutral' },
};

const ICONE_ATENCAO: Record<string, ReactNode> = {
  convite: <MailWarning className="h-4 w-4 text-warn" />,
  'sem-profissional': <Stethoscope className="h-4 w-4 text-info" />,
  'equipe-sem-acesso': <UserX className="h-4 w-4 text-muted" />,
};

const dataHora = (iso: string) => new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });

// ---------------------------------------------------------------- Visao geral

type AbaInicio = 'plataforma' | 'clinica';

/** Tela inicial do admin: saude da plataforma e o que pede acao de gestao. */
export function AdminInicioPage() {
  const s = useSessao();
  const [params, setParams] = useSearchParams();
  const aba = (params.get('aba') as AbaInicio) || 'plataforma';
  return (
    <Page wide>
      <PageHeader eyebrow="Painel do administrador" title={`Olá, ${s?.nome.split(' ')[0] ?? ''}`} subtitle="Como a clínica está usando a AESYN." />
      <Segmented<AbaInicio>
        compact
        value={aba}
        onChange={(v) => setParams({ aba: v }, { replace: true })}
        options={[
          { value: 'plataforma', label: 'Plataforma' },
          { value: 'clinica', label: 'Indicadores clínicos' },
        ]}
      />
      <div className="mt-4">{aba === 'plataforma' ? <Plataforma /> : <Indicadores />}</div>
    </Page>
  );
}

function Plataforma() {
  const [dias, setDias] = useState('30');
  const q = useQuery({ queryKey: ['admin', 'visao-geral', dias], queryFn: () => api<VisaoGeral>(`/api/admin/visao-geral?dias=${dias}`), placeholderData: (p) => p });
  const d = q.data;
  return (
    <>
      <Chips
        value={dias}
        onChange={setDias}
        options={[
          { value: '7', label: '7 dias' },
          { value: '30', label: '30 dias' },
          { value: '90', label: '90 dias' },
        ]}
      />
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {!d ? (
        <Skeleton className="mt-4 h-64" />
      ) : (
        <div className="mt-4 flex flex-col gap-4">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Numero v={d.equipeAtiva} l="pessoas na equipe" icon={<Users className="h-4 w-4 text-accent" />} />
            <Numero v={d.pacientesAtivos} l="pacientes ativos" icon={<UserCheck className="h-4 w-4 text-ok" />} />
            <Numero v={d.pacientesNovos} l="pacientes novos" icon={<UserPlus className="h-4 w-4 text-info" />} />
            <Numero v={d.pessoasQueAcessaram} l="pessoas entraram no app" icon={<LogIn className="h-4 w-4 text-accent" />} />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <Card className="p-4">
              <div className="mb-3 flex items-center justify-between">
                <p className="font-semibold">Equipe por função</p>
                <Link to="/admin/usuarios?aba=equipe" className="text-sm font-semibold text-accent">
                  Gerenciar
                </Link>
              </div>
              {d.equipePorFuncao.length ? (
                <div className="flex flex-col gap-2.5">
                  {d.equipePorFuncao.map((x) => (
                    <div key={x.rotulo} className="flex items-center justify-between text-[14px]">
                      <span className="text-muted">{PAPEL[x.rotulo as TipoUsuario] ?? x.rotulo}</span>
                      <span className="num font-semibold">{x.valor}</span>
                    </div>
                  ))}
                  {d.equipeInativa > 0 && <p className="text-[12px] text-faint">{d.equipeInativa} com acesso desligado</p>}
                </div>
              ) : (
                <p className="text-sm text-muted">Ninguém cadastrado ainda.</p>
              )}
            </Card>
            <Card className="p-4">
              <div className="mb-3 flex items-center justify-between">
                <p className="font-semibold">Acesso dos pacientes ao app</p>
                <Link to="/admin/usuarios?aba=pacientes" className="text-sm font-semibold text-accent">
                  Ver pacientes
                </Link>
              </div>
              <Proporcao
                total={d.pacientesAtivos}
                partes={[
                  { l: 'Usam o app', v: d.pacientesComAcesso, cor: 'bg-ok', to: '/admin/usuarios?aba=pacientes&acesso=ativo' },
                  { l: 'Convite pendente', v: d.convitesPendentes, cor: 'bg-warn', to: '/admin/usuarios?aba=pacientes&acesso=convite' },
                  { l: 'Sem acesso', v: d.pacientesSemAcesso, cor: 'bg-line', to: '/admin/usuarios?aba=pacientes&acesso=sem-acesso' },
                ]}
              />
            </Card>
          </div>

          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Numero v={d.consultasRealizadas} l={`consultas realizadas de ${d.consultasPeriodo}`} icon={<CalendarCheck className="h-4 w-4 text-ok" />} />
            <Numero v={d.pendenciasAbertas} l="pendências clínicas abertas" tone={d.pendenciasAbertas ? 'text-warn' : undefined} icon={<ClipboardList className="h-4 w-4 text-warn" />} />
            <Numero v={d.pacientesSemProfissional} l="pacientes sem equipe" tone={d.pacientesSemProfissional ? 'text-info' : undefined} icon={<Stethoscope className="h-4 w-4 text-info" />} />
            <Numero v={d.acoesRegistradas} l="ações registradas" icon={<Activity className="h-4 w-4 text-accent" />} />
          </div>

          <Card className="divide-y divide-line/60">
            <p className="p-4 pb-3 font-semibold">Precisa da sua atenção</p>
            {d.atencao.length ? (
              d.atencao.map((x, i) => (
                <Link
                  key={`${x.tipo}-${x.pacienteId ?? x.usuarioId ?? i}`}
                  to={x.tipo === 'equipe-sem-acesso' ? '/admin/usuarios?aba=equipe' : `/admin/usuarios?aba=pacientes&paciente=${x.pacienteId}`}
                  className="flex items-center gap-3 p-3.5 transition hover:bg-raised/40"
                >
                  <span className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-raised">{ICONE_ATENCAO[x.tipo] ?? <AlertTriangle className="h-4 w-4 text-warn" />}</span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-semibold">{x.titulo}</span>
                    <span className="block text-[13px] text-muted">{x.detalhe}</span>
                  </span>
                  <ChevronRight className="h-4 w-4 shrink-0 text-faint" />
                </Link>
              ))
            ) : (
              <p className="p-4 pt-0 text-sm text-muted">Nada pendente. Convites, pacientes sem profissional e pessoas da equipe sem entrar aparecem aqui.</p>
            )}
          </Card>

          <AtividadeRecente />
        </div>
      )}
    </>
  );
}

function Proporcao({ total, partes }: { total: number; partes: { l: string; v: number; cor: string; to: string }[] }) {
  return (
    <>
      <div className="flex h-2.5 overflow-hidden rounded-full bg-raised">
        {partes.map((p) => (
          <span key={p.l} className={p.cor} style={{ width: `${total ? (p.v / total) * 100 : 0}%` }} />
        ))}
      </div>
      <div className="mt-3 flex flex-col gap-2">
        {partes.map((p) => (
          <Link key={p.l} to={p.to} className="flex items-center gap-2 text-[14px] hover:text-accent">
            <span className={clsx('h-2.5 w-2.5 rounded-full', p.cor)} />
            <span className="flex-1 text-muted">{p.l}</span>
            <span className="num font-semibold">{p.v}</span>
          </Link>
        ))}
      </div>
    </>
  );
}

function AtividadeRecente() {
  const q = useQuery({ queryKey: ['admin', 'atividade', 'recente'], queryFn: () => api<{ itens: ItemAtividade[] }>('/api/admin/atividade') });
  const itens = (q.data?.itens ?? []).slice(0, 6);
  return (
    <div>
      <SectionTitle
        action={
          <Link to="/admin/atividade" className="text-sm font-semibold text-accent">
            Ver tudo
          </Link>
        }
      >
        Atividade recente
      </SectionTitle>
      {q.isLoading ? <Skeleton className="h-32" /> : itens.length ? <ListaAtividade itens={itens} /> : <p className="text-sm text-muted">Nenhuma ação registrada ainda.</p>}
    </div>
  );
}

// ---------------------------------------------------------------- Usuarios

interface ProfissionalAdmin {
  id: string;
  usuarioId: string;
  nome: string;
  tipo: string;
  especialidade: string | null;
  contaAtiva: boolean;
  pacientes: number;
  pacientesDefinidos: number;
}

const useProfissionais = () => useQuery({ queryKey: ['admin', 'profissionais'], queryFn: () => api<ProfissionalAdmin[]>('/api/admin/profissionais') });

type AbaUsuarios = 'equipe' | 'pacientes';

/** Todas as contas da clinica: equipe e pacientes, com quem atende quem. */
export function AdminUsuariosPage() {
  const [params, setParams] = useSearchParams();
  const aba = (params.get('aba') as AbaUsuarios) || 'equipe';
  return (
    <Page wide>
      <PageHeader title="Usuários" subtitle="Equipe e pacientes: contas, acesso e quem atende quem." />
      <Segmented<AbaUsuarios>
        compact
        value={aba}
        onChange={(v) => setParams({ aba: v }, { replace: true })}
        options={[
          { value: 'equipe', label: 'Equipe' },
          { value: 'pacientes', label: 'Pacientes' },
        ]}
      />
      <div className="mt-4">{aba === 'equipe' ? <EquipeAdmin /> : <PacientesAdmin />}</div>
    </Page>
  );
}

function EquipeAdmin() {
  const acessos = useQuery({ queryKey: ['admin', 'equipe-acessos'], queryFn: () => api<{ usuarioId: string; ultimoAcessoUtc: string | null }[]>('/api/admin/equipe/acessos') });
  const profs = useProfissionais();
  const mapa = Object.fromEntries((acessos.data ?? []).map((x) => [x.usuarioId, x.ultimoAcessoUtc]));
  const extra = Object.fromEntries(
    (profs.data ?? []).map((p) => [
      p.usuarioId,
      <Link key={p.id} to={`/admin/usuarios?aba=pacientes&profissional=${p.id}`} onClick={(e) => e.stopPropagation()} className="text-[12px] font-semibold text-accent">
        {p.pacientes} {p.pacientes === 1 ? 'paciente' : 'pacientes'}
      </Link>,
    ]),
  );
  return (
    <>
      <Equipe acessos={mapa} extra={extra} />
      <p className="mt-4 text-[13px] text-muted">
        Médicos, nutricionistas e personais só veem os pacientes da própria equipe. Toque na quantidade de pacientes para ver, atribuir ou transferir. Abra um profissional para ver o app como ele vê, sem alterar nada.
      </p>
    </>
  );
}

function PacientesAdmin() {
  const [params, setParams] = useSearchParams();
  const acesso = params.get('acesso') ?? '';
  const profissional = params.get('profissional') ?? '';
  const aberto = params.get('paciente');
  const [busca, setBusca] = useState(params.get('busca') ?? '');
  const [pagina, setPagina] = useState(1);
  const [convite, setConvite] = useState<PacienteAdmin | null>(null);
  const [marcados, setMarcados] = useState<Set<string>>(new Set());
  const [atribuir, setAtribuir] = useState(false);
  const profs = useProfissionais();
  const termo = busca.trim();
  const filtroProf = profissional === 'sem' ? '&equipe=sem' : profissional ? `&profissionalId=${profissional}` : '';
  const q = useQuery({
    queryKey: ['admin', 'pacientes', termo, acesso, profissional, pagina],
    queryFn: () => api<PacientesAdmin>(`/api/admin/pacientes?pagina=${pagina}&busca=${encodeURIComponent(termo)}&acesso=${acesso}${filtroProf}`),
    placeholderData: (p) => p,
  });
  const mudar = (k: string, v: string) => {
    setPagina(1);
    setMarcados(new Set());
    const p = new URLSearchParams(params);
    if (v) p.set(k, v);
    else p.delete(k);
    setParams(p, { replace: true });
  };
  const marcar = (id: string) =>
    setMarcados((s) => {
      const n = new Set(s);
      if (n.has(id)) n.delete(id);
      else n.add(id);
      return n;
    });
  const d = q.data;
  const todosMarcados = !!d?.itens.length && d.itens.every((x) => marcados.has(x.id));
  return (
    <>
      <div className="relative">
        <Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted" />
        <input
          className={clsx(inputCls, 'pl-10')}
          placeholder="Buscar por nome ou e-mail"
          value={busca}
          onChange={(e) => {
            setBusca(e.target.value);
            setPagina(1);
          }}
        />
      </div>
      <div className="mt-3 grid gap-3 sm:grid-cols-[1fr_auto] sm:items-center">
        <Chips
          value={acesso}
          onChange={(v) => mudar('acesso', v)}
          options={[
            { value: '', label: 'Todos' },
            { value: 'ativo', label: 'Usam o app' },
            { value: 'convite', label: 'Convite pendente' },
            { value: 'sem-acesso', label: 'Sem acesso' },
            { value: 'bloqueado', label: 'Revogados' },
          ]}
        />
        <select className={clsx(inputCls, 'sm:w-64')} value={profissional} onChange={(e) => mudar('profissional', e.target.value)} aria-label="Filtrar por profissional">
          <option value="">Todos os profissionais</option>
          <option value="sem">Sem equipe (ninguém vê)</option>
          {(profs.data ?? []).map((p) => (
            <option key={p.id} value={p.id}>
              {p.nome} ({p.pacientes})
            </option>
          ))}
        </select>
      </div>
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {!d ? (
        <Skeleton className="mt-4 h-64" />
      ) : d.itens.length === 0 ? (
        <div className="mt-4">
          <Empty icon={<Users className="h-5 w-5" />} title="Nenhum paciente aqui">
            {termo || acesso || profissional ? 'Mude a busca ou os filtros.' : 'Os pacientes cadastrados aparecem aqui.'}
          </Empty>
        </div>
      ) : (
        <>
          <div className="mb-2 mt-4 flex items-center justify-between gap-3 text-sm text-muted">
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                className="h-4 w-4 accent-[rgb(var(--accent))]"
                checked={todosMarcados}
                onChange={() => setMarcados(todosMarcados ? new Set() : new Set(d.itens.map((x) => x.id)))}
              />
              {d.total} {d.total === 1 ? 'paciente' : 'pacientes'}
            </label>
            {marcados.size > 0 && (
              <Button size="sm" icon={<UserCheck className="h-4 w-4" />} onClick={() => setAtribuir(true)}>
                Atribuir {marcados.size}
              </Button>
            )}
          </div>
          <Card className="divide-y divide-line/60">
            {d.itens.map((p) => (
              <div key={p.id} className="flex items-center gap-3 p-3.5">
                <input type="checkbox" aria-label={`Selecionar ${p.nome}`} className="h-4 w-4 shrink-0 accent-[rgb(var(--accent))]" checked={marcados.has(p.id)} onChange={() => marcar(p.id)} />
                <button onClick={() => mudar('paciente', p.id)} className="flex min-w-0 flex-1 items-center gap-3 text-left">
                  <span className="min-w-0 flex-1 [overflow-wrap:anywhere]">
                    <span className="block font-semibold">
                      {p.nome}
                      {!p.ativo && <span className="font-normal text-muted"> (inativo)</span>}
                    </span>
                    <span className={clsx('block text-[13px]', p.equipe.length ? 'text-muted' : 'text-warn')}>
                      {p.equipe.length ? p.equipe.map((m) => m.nome).join(', ') : 'Sem equipe: nenhum profissional vê'}
                    </span>
                    <span className="block text-[12px] text-faint">
                      {[STATUS_ACOMPANHAMENTO[p.statusAcompanhamento] ?? p.statusAcompanhamento, p.acesso === 'ativo' ? (p.ultimoAcessoUtc ? `último acesso ${tempoRelativo(p.ultimoAcessoUtc)}` : 'sem acesso registrado') : `cadastrado ${tempoRelativo(p.createdAtUtc)}`].join(' · ')}
                    </span>
                  </span>
                  <Pill tone={ACESSO[p.acesso].tone}>{ACESSO[p.acesso].l}</Pill>
                  <ChevronRight className="h-4 w-4 shrink-0 text-faint" />
                </button>
              </div>
            ))}
          </Card>
          {d.totalPaginas > 1 && (
            <div className="mt-3 flex items-center justify-between">
              <Button size="sm" variant="ghost" disabled={pagina <= 1} onClick={() => setPagina((x) => x - 1)}>
                Anterior
              </Button>
              <span className="text-sm text-muted">
                {pagina} de {d.totalPaginas}
              </span>
              <Button size="sm" variant="ghost" disabled={pagina >= d.totalPaginas} onClick={() => setPagina((x) => x + 1)}>
                Próxima
              </Button>
            </div>
          )}
        </>
      )}
      <PacienteAdminSheet
        pacienteId={aberto}
        onClose={() => mudar('paciente', '')}
        onAcesso={(p) => {
          mudar('paciente', '');
          setTimeout(() => setConvite(p), 180);
        }}
      />
      <AtribuirSheet
        aberto={atribuir}
        ids={[...marcados]}
        deProfissional={profissional && profissional !== 'sem' ? profissional : ''}
        onClose={(feito) => {
          setAtribuir(false);
          if (feito) setMarcados(new Set());
        }}
      />
      <AcessoSheet
        paciente={convite && { id: convite.id, nome: convite.nome, email: convite.email, telefone: convite.telefone }}
        onClose={() => {
          setConvite(null);
          q.refetch();
        }}
      />
    </>
  );
}

/** Atribui varios pacientes a um profissional; com "tirar de", vira transferencia. */
function AtribuirSheet({ aberto, ids, deProfissional, onClose }: { aberto: boolean; ids: string[]; deProfissional: string; onClose: (feito: boolean) => void }) {
  const qc = useQueryClient();
  const profs = useProfissionais().data ?? [];
  const [para, setPara] = useState('');
  const [de, setDe] = useState('');
  const [responsavel, setResponsavel] = useState(true);
  useEffect(() => {
    if (!aberto) return;
    setPara('');
    setDe(deProfissional);
    setResponsavel(true);
  }, [aberto]);
  const salvar = useMutation({
    mutationFn: () => api<{ pacientes: number; adicionados: number }>('/api/admin/pacientes/atribuir', { method: 'POST', json: { pacienteIds: ids, profissionalId: para, removerProfissionalId: de || null, tornarResponsavel: responsavel } }),
    onSuccess: (r) => {
      const nome = profs.find((x) => x.id === para)?.nome;
      toast(de ? `${r.pacientes} paciente(s) transferido(s) para ${nome}.` : `${r.pacientes} paciente(s) com ${nome}.`);
      qc.invalidateQueries({ queryKey: ['admin'] });
      onClose(true);
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });
  return (
    <Sheet
      open={aberto}
      onClose={() => onClose(false)}
      title={`Atribuir ${ids.length} ${ids.length === 1 ? 'paciente' : 'pacientes'}`}
      footer={
        <Button size="lg" className="w-full" disabled={!para || para === de} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          {de ? 'Transferir' : 'Atribuir'}
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Para quem">
          <select className={inputCls} value={para} onChange={(e) => setPara(e.target.value)}>
            <option value="">Escolha um profissional</option>
            {profs.map((p) => (
              <option key={p.id} value={p.id}>
                {p.nome} · {PAPEL[p.tipo as TipoUsuario] ?? p.tipo}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Tirar de (opcional)" hint="Use para transferir: quem sair deixa de ver estes pacientes. O histórico fica salvo.">
          <select className={inputCls} value={de} onChange={(e) => setDe(e.target.value)}>
            <option value="">Ninguém, só adicionar</option>
            {profs.map((p) => (
              <option key={p.id} value={p.id}>
                {p.nome}
              </option>
            ))}
          </select>
        </Field>
        <label className="flex items-start gap-2 text-[14px]">
          <input type="checkbox" className="mt-1 h-4 w-4 accent-[rgb(var(--accent))]" checked={responsavel} onChange={(e) => setResponsavel(e.target.checked)} />
          <span>
            Tornar responsável
            <span className="block text-[13px] text-muted">Passa a receber as mensagens do chat destes pacientes.</span>
          </span>
        </label>
      </div>
    </Sheet>
  );
}

/** Detalhe administrativo do paciente: equipe, acesso ao app, cadastro e mensagens, sem prontuario. */
function PacienteAdminSheet({ pacienteId, onClose, onAcesso }: { pacienteId: string | null; onClose: () => void; onAcesso: (p: PacienteAdmin) => void }) {
  const qc = useQueryClient();
  const q = useQuery({
    queryKey: ['admin', 'paciente', pacienteId],
    queryFn: () => api<PacientesAdmin>(`/api/admin/pacientes?id=${pacienteId}`).then((r) => r.itens[0] ?? null),
    enabled: !!pacienteId,
  });
  const ficha = useQuery({ queryKey: ['pro', 'paciente', pacienteId], queryFn: () => api<Paciente>(`/api/pacientes/${pacienteId}`), enabled: !!pacienteId });
  const profs = useProfissionais().data ?? [];
  const p = q.data;
  const [equipe, setEquipe] = useState<Set<string>>(new Set());
  const [editar, setEditar] = useState(false);
  const [mensagens, setMensagens] = useState(false);
  useEffect(() => {
    if (p) setEquipe(new Set(p.equipe.map((x) => x.id)));
  }, [p?.id, p?.equipe.map((x) => x.id).join()]);
  useEffect(() => setMensagens(false), [pacienteId]);
  const atualizar = () => {
    qc.invalidateQueries({ queryKey: ['admin'] });
    qc.invalidateQueries({ queryKey: ['pro', 'paciente', pacienteId] });
  };
  const erro = (e: unknown) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad');
  const salvarEquipe = useMutation({
    mutationFn: () => api(`/api/admin/pacientes/${pacienteId}/equipe`, { method: 'PUT', json: { profissionalIds: [...equipe] } }),
    onSuccess: () => {
      toast('Equipe salva.');
      atualizar();
    },
    onError: erro,
  });
  const cadastro = useMutation({
    mutationFn: (ativar: boolean) => api(`/api/pacientes/${pacienteId}${ativar ? '/ativar' : ''}`, { method: ativar ? 'PATCH' : 'DELETE' }),
    onSuccess: (_, ativar) => {
      toast(ativar ? 'Cadastro reativado.' : 'Cadastro desativado. O histórico fica salvo.');
      atualizar();
    },
    onError: erro,
  });
  const mudouEquipe = !!p && [...equipe].sort().join() !== p.equipe.map((x) => x.id).sort().join();
  return (
    <>
      <Sheet open={!!pacienteId && !editar} onClose={onClose} title={p?.nome ?? 'Paciente'} tall>
        {q.isError ? (
          <ErrorBox error={q.error} onRetry={() => q.refetch()} />
        ) : !p ? (
          <Skeleton className="h-64" />
        ) : (
          <div className="flex flex-col gap-5">
            <section>
              <p className="eyebrow mb-2">Equipe do paciente</p>
              <p className="mb-2 text-[13px] text-muted">Só os profissionais marcados veem este paciente e os dados dele.</p>
              <Card className="divide-y divide-line/60">
                {profs.map((x) => (
                  <label key={x.id} className="flex items-center gap-3 px-4 py-3 text-[14px]">
                    <input
                      type="checkbox"
                      className="h-4 w-4 accent-[rgb(var(--accent))]"
                      checked={equipe.has(x.id)}
                      onChange={() =>
                        setEquipe((s) => {
                          const n = new Set(s);
                          if (n.has(x.id)) n.delete(x.id);
                          else n.add(x.id);
                          return n;
                        })
                      }
                    />
                    <span className="min-w-0 flex-1">
                      <span className="font-semibold">{x.nome}</span>
                      <span className="block text-[12px] text-muted">{[PAPEL[x.tipo as TipoUsuario] ?? x.tipo, x.especialidade].filter(Boolean).join(' · ')}</span>
                    </span>
                    {p.profissionalId === x.id && <Pill tone="accent">Responsável</Pill>}
                  </label>
                ))}
              </Card>
              {mudouEquipe && (
                <Button className="mt-3 w-full" loading={salvarEquipe.isPending} onClick={() => salvarEquipe.mutate()}>
                  Salvar equipe
                </Button>
              )}
            </section>

            <section>
              <p className="eyebrow mb-2">Mensagens</p>
              <Card className="p-4 text-[14px]">
                <p>
                  Recebe as mensagens: <b>{p.profissional ?? 'ninguém'}</b>
                </p>
                <p className="text-[13px] text-muted">{p.origemResponsavelRotulo}</p>
                <Button size="sm" variant="soft" className="mt-3" onClick={() => setMensagens((x) => !x)}>
                  {mensagens ? 'Fechar detalhes' : 'Trocar responsável e ver conversa'}
                </Button>
              </Card>
              {mensagens && (
                <div className="mt-3">
                  <VinculoPacientePainel pacienteId={p.id} />
                </div>
              )}
            </section>

            <section>
              <p className="eyebrow mb-2">Conta e cadastro</p>
              <Card className="divide-y divide-line/60 text-[14px]">
                <div className="flex items-center justify-between gap-3 px-4 py-3">
                  <span className="text-muted">Acesso ao app</span>
                  <Pill tone={ACESSO[p.acesso].tone}>{ACESSO[p.acesso].l}</Pill>
                </div>
                <Linha l="E-mail" v={p.email ?? 'Não informado'} />
                <Linha l="Telefone" v={p.telefone ?? 'Não informado'} />
                <Linha l="Acompanhamento" v={STATUS_ACOMPANHAMENTO[p.statusAcompanhamento] ?? p.statusAcompanhamento} />
                <Linha l="Último acesso" v={p.ultimoAcessoUtc ? dataHora(p.ultimoAcessoUtc) : 'Sem acesso registrado'} />
                <Linha l="Cadastrado" v={new Date(p.createdAtUtc).toLocaleDateString('pt-BR')} />
              </Card>
              <div className="mt-3 grid grid-cols-2 gap-2">
                <Button variant="soft" icon={<KeyRound className="h-4 w-4" />} onClick={() => onAcesso(p)}>
                  Acesso ao app
                </Button>
                <Button variant="soft" icon={<Pencil className="h-4 w-4" />} disabled={!ficha.data} onClick={() => setEditar(true)}>
                  Editar dados
                </Button>
              </div>
              <Button variant="ghost" className={clsx('mt-2 w-full', p.ativo && 'text-bad')} loading={cadastro.isPending} onClick={() => cadastro.mutate(!p.ativo)}>
                {p.ativo ? 'Desativar cadastro' : 'Reativar cadastro'}
              </Button>
              <p className="mt-1 text-center text-[12px] text-muted">Prontuário, planos e consultas ficam com a equipe, na rotina clínica.</p>
            </section>
          </div>
        )}
      </Sheet>
      <EditarDadosSheet
        paciente={editar ? (ficha.data ?? null) : null}
        onClose={() => {
          setEditar(false);
          atualizar();
        }}
      />
    </>
  );
}

function Linha({ l, v }: { l: string; v: string }) {
  return (
    <div className="flex items-start justify-between gap-3 px-4 py-3">
      <span className="text-muted">{l}</span>
      <span className="text-right font-semibold [overflow-wrap:anywhere]">{v}</span>
    </div>
  );
}

// ---------------------------------------------------------------- Atividade

const ACOES: Record<string, string> = {
  LOGIN: 'entrou no app',
  ACCESS: 'usou o app',
  CREATE: 'criou',
  UPDATE: 'alterou',
  DELETE: 'excluiu',
  PASSWORD_RESET: 'redefiniu a senha de',
  PASSWORD_CHANGE: 'trocou a própria senha',
  IMPERSONATION_START: 'abriu o app como',
  IMPERSONATION_END: 'saiu da visualização de',
  CREATE_ACCESS: 'liberou o acesso de',
  RENEW_ACCESS: 'gerou novo convite para',
  REVOKE_ACCESS: 'revogou o acesso de',
  ACTIVATE_ACCESS: 'ativou o próprio acesso',
  ACCOUNT_DEACTIVATE: 'desativou a própria conta',
  EMAIL_CHANGED: 'trocou o e-mail',
  LEGAL_ACCEPTANCE: 'aceitou os termos',
  DATA_DELETION_REQUEST: 'pediu exclusão de dados',
  PATIENT_TAGS_CHANGED: 'mudou as etiquetas de',
  ASSIGN_PROGRAM: 'atribuiu um programa a',
  SET_CARE_TEAM: 'mudou a equipe de',
  ADD_CARE_TEAM: 'atribuiu um profissional a',
  TRANSFER_CARE_TEAM: 'transferiu',
  DefinirProfissionalResponsavel: 'definiu quem recebe as mensagens de',
  RemoverProfissionalResponsavel: 'voltou ao automático o responsável de',
  ACTIVATE: 'reativou',
  DEACTIVATE: 'desativou',
  PATIENT_STATUS_CHANGED: 'mudou o acompanhamento de',
};

const ENTIDADES: Record<string, string> = {
  UsuarioEquipe: 'membro da equipe',
  Paciente: 'paciente',
  Consulta: 'consulta',
  PlanoAlimentar: 'plano alimentar',
  PlanoTreino: 'plano de treino',
  MetaPaciente: 'meta',
  RegistroMeta: 'registro de meta',
  ExameLaboratorial: 'exame',
  Anamnese: 'anamnese',
  PendenciaClinica: 'pendência',
};

function descreverAcao(x: ItemAtividade) {
  const verbo = ACOES[x.acao] ?? x.acao.toLowerCase().replace(/_/g, ' ');
  const proprio = x.alvo && x.alvo === x.usuario;
  if (proprio || ['LOGIN', 'ACCESS', 'PASSWORD_CHANGE', 'ACTIVATE_ACCESS', 'ACCOUNT_DEACTIVATE', 'EMAIL_CHANGED', 'LEGAL_ACCEPTANCE', 'DATA_DELETION_REQUEST'].includes(x.acao)) return verbo;
  const entidade = ENTIDADES[x.entidade] ?? x.entidade;
  if (x.acao === 'CREATE' || x.acao === 'UPDATE' || x.acao === 'DELETE') return `${verbo} ${entidade}${x.alvo ? ` · ${x.alvo}` : ''}`;
  return `${verbo} ${x.alvo ?? entidade}`;
}

function ListaAtividade({ itens }: { itens: ItemAtividade[] }) {
  return (
    <Card className="divide-y divide-line/60">
      {itens.map((x) => (
        <div key={x.id} className="flex items-start gap-3 p-3.5">
          <span className="min-w-0 flex-1 text-[14px] [overflow-wrap:anywhere]">
            <b>{x.usuario ?? 'Sistema'}</b> {descreverAcao(x)}
            <span className="block text-[12px] text-faint">
              {[x.funcaoUsuario ? PAPEL[x.funcaoUsuario] : null, dataHora(x.quandoUtc)].filter(Boolean).join(' · ')}
            </span>
          </span>
        </div>
      ))}
    </Card>
  );
}

type Area = '' | 'equipe' | 'pacientes' | 'contas';

export function AdminAtividadePage() {
  const [area, setArea] = useState<Area>('');
  const [acessos, setAcessos] = useState(false);
  const q = useInfiniteQuery({
    queryKey: ['admin', 'atividade', area, acessos],
    initialPageParam: '',
    queryFn: ({ pageParam }) => api<{ itens: ItemAtividade[]; temMais: boolean }>(`/api/admin/atividade?area=${area}&incluirAcessos=${acessos}${pageParam ? `&antesDeUtc=${encodeURIComponent(pageParam)}` : ''}`),
    getNextPageParam: (ultima) => (ultima.temMais ? ultima.itens[ultima.itens.length - 1]?.quandoUtc : undefined),
  });
  const itens = q.data?.pages.flatMap((p) => p.itens) ?? [];
  return (
    <Page>
      <PageHeader title="Atividade" subtitle="O que foi feito na plataforma, por quem e quando." />
      <Chips
        value={area}
        onChange={setArea}
        options={[
          { value: '', label: 'Tudo' },
          { value: 'equipe', label: 'Equipe' },
          { value: 'pacientes', label: 'Pacientes' },
          { value: 'contas', label: 'Contas' },
        ]}
      />
      <label className="mt-3 flex items-center gap-2 text-[14px] text-muted">
        <input type="checkbox" checked={acessos} onChange={(e) => setAcessos(e.target.checked)} className="h-4 w-4 accent-[rgb(var(--accent))]" />
        Mostrar entradas no app
      </label>
      <div className="mt-4">
        {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
        {q.isLoading ? (
          <Skeleton className="h-64" />
        ) : itens.length ? (
          <>
            <ListaAtividade itens={itens} />
            {q.hasNextPage && (
              <Button variant="ghost" className="mt-3 w-full" loading={q.isFetchingNextPage} onClick={() => q.fetchNextPage()}>
                Carregar mais
              </Button>
            )}
          </>
        ) : (
          <Empty icon={<Activity className="h-5 w-5" />} title="Nada registrado">
            As ações da equipe aparecem aqui conforme acontecem.
          </Empty>
        )}
      </div>
    </Page>
  );
}

// ---------------------------------------------------------------- Clinica

export function AdminClinicaPage() {
  const [aviso, setAviso] = useState(false);
  return (
    <Page>
      <PageHeader title="Clínica" subtitle="Dados da organização e configurações compartilhadas." />
      <Clinica />
      <p className="eyebrow mb-2 mt-7">Configurações da rotina clínica</p>
      <Card className="divide-y divide-line">
        <LinkLinha to="/pro/anamnese-exames" titulo="Anamnese e exames" sub="Perguntas de anamnese e marcadores de exame" />
        <LinkLinha to="/pro/biblioteca" titulo="Biblioteca" sub="Exercícios, alimentos, suplementos, modelos e programas" />
        <LinkLinha to="/pro/gestao?aba=carteira" titulo="Carteira de pacientes" sub="Pacientes por prioridade de acompanhamento" />
      </Card>
      <p className="eyebrow mb-2 mt-7">Comunicação</p>
      <Card>
        <button type="button" className="flex w-full items-center gap-3 p-4 text-left font-semibold" onClick={() => setAviso(true)}>
          <Bell className="h-5 w-5 text-accent" />
          <span className="min-w-0 flex-1">
            Enviar notificação
            <span className="block text-sm font-normal text-muted">Push para os aparelhos com notificação ligada</span>
          </span>
          <ChevronRight className="h-5 w-5 shrink-0 text-muted" />
        </button>
      </Card>
      <AvisoPushSheet aberto={aviso} onClose={() => setAviso(false)} />
    </Page>
  );
}

type PublicoAviso = 'todos' | 'equipe' | 'admins';

/** Push do admin para varias pessoas: mostra quantos aparelhos recebem antes de enviar. */
function AvisoPushSheet({ aberto, onClose }: { aberto: boolean; onClose: () => void }) {
  const [publico, setPublico] = useState<PublicoAviso>('admins');
  const [titulo, setTitulo] = useState('AESYN');
  const [mensagem, setMensagem] = useState('Notificação de teste. Se você recebeu, está tudo certo!');
  const previa = useQuery({
    queryKey: ['admin', 'push-previa', publico],
    queryFn: () => api<{ pessoas: number; aparelhos: number }>(`/api/admin/push/previa?publico=${publico}`),
    enabled: aberto,
  });
  const enviar = useMutation({
    mutationFn: () => api<{ pessoas: number; enviadas: number; falhas: number; habilitado: boolean }>('/api/admin/push/enviar', { method: 'POST', json: { publico, titulo, mensagem } }),
    onSuccess: (r) => {
      if (!r.habilitado) toast('Nada enviado: o push está desligado no servidor ou ninguém tem notificação ligada.', 'bad');
      else toast(`Enviada para ${r.enviadas} aparelho(s)${r.falhas ? `, ${r.falhas} falharam (aparelhos antigos são removidos)` : ''}.`);
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível enviar.', 'bad'),
  });
  const p = previa.data;
  const confirmar = () => {
    if (p && window.confirm(`Enviar "${titulo}" para ${p.aparelhos} aparelho(s) de ${p.pessoas} pessoa(s)?`)) enviar.mutate();
  };
  return (
    <Sheet
      open={aberto}
      onClose={onClose}
      title="Enviar notificação"
      footer={
        <Button size="lg" className="w-full" disabled={!p?.aparelhos || !titulo.trim() || !mensagem.trim()} loading={enviar.isPending} onClick={confirmar}>
          {p ? `Enviar para ${p.aparelhos} aparelho(s)` : 'Enviar'}
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Para quem">
          <Segmented<PublicoAviso>
            compact
            value={publico}
            onChange={setPublico}
            options={[
              { value: 'admins', label: 'Só admins' },
              { value: 'equipe', label: 'Equipe' },
              { value: 'todos', label: 'Todos' },
            ]}
          />
        </Field>
        <p className="text-[13px] text-muted">
          {p ? `${p.pessoas} pessoa(s) com ${p.aparelhos} aparelho(s) com notificação ligada.` : 'Contando aparelhos...'} Quem não ligou a notificação não recebe.
        </p>
        <Field label="Título">
          <input className={inputCls} maxLength={80} value={titulo} onChange={(e) => setTitulo(e.target.value)} />
        </Field>
        <Field label="Mensagem">
          <textarea className={inputCls} rows={3} maxLength={240} value={mensagem} onChange={(e) => setMensagem(e.target.value)} />
        </Field>
      </div>
    </Sheet>
  );
}

export function LinkLinha({ to, titulo, sub, icon }: { to: string; titulo: string; sub: string; icon?: ReactNode }) {
  return (
    <Link to={to} className="flex items-center gap-3 p-4 font-semibold">
      {icon}
      <span className="min-w-0 flex-1">
        {titulo}
        <span className="block text-sm font-normal text-muted">{sub}</span>
      </span>
      <ChevronRight className="h-5 w-5 shrink-0 text-muted" />
    </Link>
  );
}
