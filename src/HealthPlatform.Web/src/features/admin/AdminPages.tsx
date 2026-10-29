import { useInfiniteQuery, useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { Activity, AlertTriangle, CalendarCheck, ChevronRight, ClipboardList, KeyRound, LogIn, MailWarning, Search, Stethoscope, UserCheck, UserPlus, Users, UserX } from 'lucide-react';
import { ReactNode, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { PAPEL } from '@/components/account';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, inputCls, Pill, SectionTitle, Segmented, Sheet, Skeleton } from '@/components/ui';
import { api, TipoUsuario } from '@/lib/api';
import { tempoRelativo } from '@/lib/format';
import { useSessao } from '@/lib/session';
import { Chips } from '../pro/CuidadosPro';
import { AcessoSheet, STATUS_ACOMPANHAMENTO } from '../pro/FichaAcoes';
import { Clinica, Equipe, Indicadores, Numero } from '../pro/GestaoPage';

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
  ativo: boolean;
  statusAcompanhamento: string;
  acesso: Acesso;
  ultimoAcessoUtc: string | null;
  profissional: string | null;
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
                <Link to="/admin/equipe" className="text-sm font-semibold text-accent">
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
                <Link to="/admin/pacientes" className="text-sm font-semibold text-accent">
                  Ver pacientes
                </Link>
              </div>
              <Proporcao
                total={d.pacientesAtivos}
                partes={[
                  { l: 'Usam o app', v: d.pacientesComAcesso, cor: 'bg-ok', to: '/admin/pacientes?acesso=ativo' },
                  { l: 'Convite pendente', v: d.convitesPendentes, cor: 'bg-warn', to: '/admin/pacientes?acesso=convite' },
                  { l: 'Sem acesso', v: d.pacientesSemAcesso, cor: 'bg-line', to: '/admin/pacientes?acesso=sem-acesso' },
                ]}
              />
            </Card>
          </div>

          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Numero v={d.consultasRealizadas} l={`consultas realizadas de ${d.consultasPeriodo}`} icon={<CalendarCheck className="h-4 w-4 text-ok" />} />
            <Numero v={d.pendenciasAbertas} l="pendências clínicas abertas" tone={d.pendenciasAbertas ? 'text-warn' : undefined} icon={<ClipboardList className="h-4 w-4 text-warn" />} />
            <Numero v={d.pacientesSemProfissional} l="pacientes sem profissional" tone={d.pacientesSemProfissional ? 'text-info' : undefined} icon={<Stethoscope className="h-4 w-4 text-info" />} />
            <Numero v={d.acoesRegistradas} l="ações registradas" icon={<Activity className="h-4 w-4 text-accent" />} />
          </div>

          <Card className="divide-y divide-line/60">
            <p className="p-4 pb-3 font-semibold">Precisa da sua atenção</p>
            {d.atencao.length ? (
              d.atencao.map((x, i) => (
                <Link
                  key={`${x.tipo}-${x.pacienteId ?? x.usuarioId ?? i}`}
                  to={x.tipo === 'equipe-sem-acesso' ? '/admin/equipe' : `/admin/pacientes?busca=${encodeURIComponent(x.titulo)}`}
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

// ---------------------------------------------------------------- Equipe

export function AdminEquipePage() {
  const acessos = useQuery({ queryKey: ['admin', 'equipe-acessos'], queryFn: () => api<{ usuarioId: string; ultimoAcessoUtc: string | null }[]>('/api/admin/equipe/acessos') });
  const mapa = Object.fromEntries((acessos.data ?? []).map((x) => [x.usuarioId, x.ultimoAcessoUtc]));
  return (
    <Page>
      <PageHeader title="Equipe" subtitle="Quem trabalha na clínica, função, acesso e senha." />
      <Equipe acessos={mapa} />
      <p className="mt-4 text-[13px] text-muted">Abra um profissional para ver o app como ele vê, sem alterar nada. Cada visualização fica registrada em Atividade.</p>
    </Page>
  );
}

// ---------------------------------------------------------------- Pacientes

export function AdminPacientesPage() {
  const [params, setParams] = useSearchParams();
  const acesso = params.get('acesso') ?? '';
  const [busca, setBusca] = useState(params.get('busca') ?? '');
  const [pagina, setPagina] = useState(1);
  const [aberto, setAberto] = useState<PacienteAdmin | null>(null);
  const [convite, setConvite] = useState<PacienteAdmin | null>(null);
  const termo = busca.trim();
  const q = useQuery({
    queryKey: ['admin', 'pacientes', termo, acesso, pagina],
    queryFn: () => api<PacientesAdmin>(`/api/admin/pacientes?pagina=${pagina}&busca=${encodeURIComponent(termo)}&acesso=${acesso}`),
    placeholderData: (p) => p,
  });
  const filtrar = (v: string) => {
    setPagina(1);
    const p = new URLSearchParams(params);
    if (v) p.set('acesso', v);
    else p.delete('acesso');
    setParams(p, { replace: true });
  };
  const d = q.data;
  return (
    <Page wide>
      <PageHeader title="Pacientes" subtitle="Cadastro e acesso ao app. O prontuário fica com os profissionais." />
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
      <div className="mt-3">
        <Chips
          value={acesso}
          onChange={filtrar}
          options={[
            { value: '', label: 'Todos' },
            { value: 'ativo', label: 'Usam o app' },
            { value: 'convite', label: 'Convite pendente' },
            { value: 'sem-acesso', label: 'Sem acesso' },
            { value: 'bloqueado', label: 'Revogados' },
          ]}
        />
      </div>
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {!d ? (
        <Skeleton className="mt-4 h-64" />
      ) : d.itens.length === 0 ? (
        <div className="mt-4">
          <Empty icon={<Users className="h-5 w-5" />} title="Nenhum paciente aqui">
            {termo || acesso ? 'Mude a busca ou o filtro.' : 'Os profissionais cadastram pacientes pela rotina clínica.'}
          </Empty>
        </div>
      ) : (
        <>
          <p className="mb-2 mt-4 text-sm text-muted">
            {d.total} {d.total === 1 ? 'paciente' : 'pacientes'}
          </p>
          <Card className="divide-y divide-line/60">
            {d.itens.map((p) => (
              <button key={p.id} onClick={() => setAberto(p)} className="flex w-full items-center gap-3 p-3.5 text-left transition hover:bg-raised/40">
                <span className="min-w-0 flex-1 [overflow-wrap:anywhere]">
                  <span className="block font-semibold">{p.nome}</span>
                  <span className="block text-[13px] text-muted">
                    {[p.profissional ? `Com ${p.profissional}` : 'Sem profissional', STATUS_ACOMPANHAMENTO[p.statusAcompanhamento] ?? p.statusAcompanhamento].join(' · ')}
                  </span>
                  <span className="block text-[12px] text-faint">
                    {p.acesso === 'ativo' ? (p.ultimoAcessoUtc ? `Último acesso ${tempoRelativo(p.ultimoAcessoUtc)}` : 'Sem acesso registrado') : `Cadastrado ${tempoRelativo(p.createdAtUtc)}`}
                  </span>
                </span>
                <Pill tone={ACESSO[p.acesso].tone}>{ACESSO[p.acesso].l}</Pill>
                <ChevronRight className="h-4 w-4 shrink-0 text-faint" />
              </button>
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
        paciente={aberto}
        onClose={() => setAberto(null)}
        onAcesso={(p) => {
          setAberto(null);
          setTimeout(() => setConvite(p), 180);
        }}
      />
      <AcessoSheet
        paciente={convite && { id: convite.id, nome: convite.nome, email: convite.email, telefone: null }}
        onClose={() => {
          setConvite(null);
          q.refetch();
        }}
      />
    </Page>
  );
}

/** Detalhe administrativo do paciente: cadastro, vinculo e acesso ao app, sem dados clinicos. */
function PacienteAdminSheet({ paciente: p, onClose, onAcesso }: { paciente: PacienteAdmin | null; onClose: () => void; onAcesso: (p: PacienteAdmin) => void }) {
  return (
    <Sheet
      open={!!p}
      onClose={onClose}
      title={p?.nome}
      footer={
        p && (
          <Button size="lg" className="w-full" icon={<KeyRound className="h-5 w-5" />} onClick={() => onAcesso(p)}>
            {p.acesso === 'ativo' || p.acesso === 'convite' ? 'Gerenciar acesso ao app' : 'Liberar acesso ao app'}
          </Button>
        )
      }
    >
      {p && (
        <div className="flex flex-col gap-3">
          <div className="flex items-center justify-between gap-3 rounded-xl bg-raised/60 p-3 text-[14px]">
            <span>Acesso ao app</span>
            <Pill tone={ACESSO[p.acesso].tone}>{ACESSO[p.acesso].l}</Pill>
          </div>
          <Card className="divide-y divide-line/60 text-[14px]">
            <Linha l="E-mail" v={p.email ?? 'Não informado'} />
            <Linha l="Profissional" v={p.profissional ?? 'Sem profissional'} />
            <Linha l="Acompanhamento" v={STATUS_ACOMPANHAMENTO[p.statusAcompanhamento] ?? p.statusAcompanhamento} />
            <Linha l="Último acesso" v={p.ultimoAcessoUtc ? dataHora(p.ultimoAcessoUtc) : 'Sem acesso registrado'} />
            <Linha l="Cadastrado" v={new Date(p.createdAtUtc).toLocaleDateString('pt-BR')} />
          </Card>
          <p className="text-[13px] text-muted">Prontuário, planos e conversas ficam com os profissionais, na rotina clínica.</p>
        </div>
      )}
    </Sheet>
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
    </Page>
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
