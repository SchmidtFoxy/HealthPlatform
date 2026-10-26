import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ChevronRight, Plus, Search, SlidersHorizontal, UserPlus } from 'lucide-react';
import { FormEvent, useDeferredValue, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Empty, ErrorBox, Field, inputCls, Pill, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { idade, iniciais, tempoRelativo } from '@/lib/format';
import type { Paciente } from '@/lib/types';
import { STATUS_ACOMPANHAMENTO } from './FichaAcoes';
import { usePacientes, useSinais } from './queries';

const NIVEL: Record<string, { label: string; tone: 'bad' | 'warn' | 'neutral' | 'ok' }> = {
  RevisarHoje: { label: 'Revisar hoje', tone: 'bad' },
  Observar: { label: 'Observar', tone: 'warn' },
  ContextoPendente: { label: 'Sem check-in', tone: 'neutral' },
  Estavel: { label: 'Estável', tone: 'ok' },
};

// ---------------------------------------------------------------- filtros avancados

interface Filtros {
  status: string;
  responsavelId: string;
  aderencia: string;
  ultimaInteracao: string;
  proximaRevisao: string;
  marcador: string;
  ordenar: string;
}

const PADRAO: Filtros = { status: 'Ativos', responsavelId: '', aderencia: '', ultimaInteracao: '', proximaRevisao: '', marcador: '', ordenar: 'nome' };

interface ItemAvancado extends Omit<Paciente, 'dataNascimento' | 'sexo' | 'createdAtUtc'> {
  responsavelNome: string | null;
  adesaoMediaPercentual: number | null;
  ultimaInteracaoUtc: string | null;
  proximaRevisaoUtc: string | null;
}

interface PesquisaAvancada {
  total: number;
  itens: ItemAvancado[];
  responsaveis: { id: string; nome: string }[];
  marcadores: string[];
}

const MARCADOR: Record<string, string> = {
  SemInteracaoRecente: 'Sem interação recente',
  SemRevisaoAgendada: 'Sem revisão agendada',
  BaixaAdesao: 'Baixa adesão',
  SemDadosAdesao: 'Sem dados de adesão',
  Inativo: 'Inativo',
};

const OPCOES: { k: keyof Filtros; l: string; o: [string, string][] }[] = [
  { k: 'status', l: 'Status', o: [['Ativos', 'Em acompanhamento'], ['Ativo', 'Ativo'], ['Pausado', 'Pausado'], ['AguardandoAvaliacao', 'Aguardando avaliação'], ['Encerrado', 'Encerrado'], ['Todos', 'Todos']] },
  { k: 'aderencia', l: 'Adesão', o: [['', 'Todas'], ['Alta', 'Alta (80% ou mais)'], ['Media', 'Média (60 a 79%)'], ['Baixa', 'Baixa (menos de 60%)'], ['SemDados', 'Sem dados']] },
  { k: 'ultimaInteracao', l: 'Última interação', o: [['', 'Qualquer'], ['7d', 'Últimos 7 dias'], ['30d', 'Últimos 30 dias'], ['Antiga', 'Há mais de 30 dias ou nunca']] },
  { k: 'proximaRevisao', l: 'Próxima revisão', o: [['', 'Qualquer'], ['7d', 'Próximos 7 dias'], ['30d', 'Próximos 30 dias'], ['SemData', 'Sem data']] },
  { k: 'ordenar', l: 'Ordenar por', o: [['nome', 'Nome'], ['aderencia', 'Menor adesão'], ['interacao', 'Interação mais antiga'], ['revisao', 'Próxima revisão']] },
];

const ativos = (f: Filtros) => (Object.keys(PADRAO) as (keyof Filtros)[]).filter((k) => f[k] !== PADRAO[k]).length;

function usePesquisa(busca: string, f: Filtros, ligado: boolean) {
  return useQuery({
    queryKey: ['pro', 'pacientes', 'avancada', busca, f],
    queryFn: () => api<PesquisaAvancada>(`/api/pacientes/pesquisa-avancada?${new URLSearchParams({ busca, ...f })}`),
    enabled: ligado,
    placeholderData: keepPreviousData,
  });
}

function FiltrosSheet({ open, valor, responsaveis, marcadores, onAplicar, onClose }: { open: boolean; valor: Filtros; responsaveis: { id: string; nome: string }[]; marcadores: string[]; onAplicar: (f: Filtros) => void; onClose: () => void }) {
  const [f, setF] = useState(valor);
  const [aberto, setAberto] = useState(open);
  if (open !== aberto) {
    setAberto(open);
    if (open) setF(valor);
  }
  const campo = (k: keyof Filtros, l: string, o: [string, string][]) => (
    <Field key={k} label={l}>
      <select className={inputCls} value={f[k]} onChange={(e) => setF((x) => ({ ...x, [k]: e.target.value }))}>
        {o.map(([v, t]) => (
          <option key={v} value={v}>
            {t}
          </option>
        ))}
      </select>
    </Field>
  );
  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Filtrar pacientes"
      footer={
        <div className="flex gap-2">
          <Button variant="ghost" size="lg" onClick={() => setF(PADRAO)}>
            Limpar
          </Button>
          <Button size="lg" className="flex-1" onClick={() => onAplicar(f)}>
            Aplicar
          </Button>
        </div>
      }
    >
      <div className="grid gap-4 sm:grid-cols-2">
        {campo(OPCOES[0].k, OPCOES[0].l, OPCOES[0].o)}
        {responsaveis.length > 0 && campo('responsavelId', 'Responsável', [['', 'Todos'], ...responsaveis.map((r): [string, string] => [r.id, r.nome])])}
        {OPCOES.slice(1, 4).map((x) => campo(x.k, x.l, x.o))}
        {marcadores.length > 0 && campo('marcador', 'Sinal de atenção', [['', 'Todos'], ...marcadores.map((m): [string, string] => [m, MARCADOR[m] ?? m])])}
        {campo(OPCOES[4].k, OPCOES[4].l, OPCOES[4].o)}
      </div>
      <p className="mt-4 text-xs text-faint">O status de acompanhamento é administrativo e preserva todo o histórico clínico.</p>
    </Sheet>
  );
}

function NovoPaciente({ open, onClose }: { open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const nav = useNavigate();
  const [f, setF] = useState({ nome: '', dataNascimento: '', sexo: '', telefone: '', email: '' });
  const criar = useMutation({
    mutationFn: () =>
      api<Paciente>('/api/pacientes', {
        method: 'POST',
        json: { nome: f.nome.trim(), cpf: null, dataNascimento: f.dataNascimento || null, sexo: f.sexo || null, telefone: f.telefone || null, email: f.email || null, profissao: null },
      }),
    onSuccess: (p) => {
      qc.invalidateQueries({ queryKey: ['pro'] });
      toast('Paciente cadastrado');
      onClose();
      nav(`/pro/pacientes/${p.id}`);
    },
    onError: (e) => toast(e.message, 'bad'),
  });
  const submit = (e: FormEvent) => {
    e.preventDefault();
    criar.mutate();
  };
  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Novo paciente"
      footer={
        <Button size="lg" className="w-full" form="novo-paciente" type="submit" loading={criar.isPending} disabled={f.nome.trim().length < 3}>
          Cadastrar
        </Button>
      }
    >
      <form id="novo-paciente" onSubmit={submit} className="flex flex-col gap-4">
        <Field label="Nome completo">
          <input id="np-nome" className={inputCls} value={f.nome} onChange={(e) => setF({ ...f, nome: e.target.value })} autoFocus required />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Nascimento">
            <input id="np-nasc" type="date" className={inputCls} value={f.dataNascimento} onChange={(e) => setF({ ...f, dataNascimento: e.target.value })} />
          </Field>
          <Field label="Sexo">
            <select id="np-sexo" className={inputCls} value={f.sexo} onChange={(e) => setF({ ...f, sexo: e.target.value })}>
              <option value="">—</option>
              <option>Feminino</option>
              <option>Masculino</option>
              <option>Outro</option>
            </select>
          </Field>
        </div>
        <Field label="E-mail" hint="Usado para liberar o acesso do paciente ao app.">
          <input id="np-email" type="email" className={inputCls} value={f.email} onChange={(e) => setF({ ...f, email: e.target.value })} />
        </Field>
        <Field label="Telefone">
          <input id="np-tel" inputMode="tel" className={inputCls} value={f.telefone} onChange={(e) => setF({ ...f, telefone: e.target.value })} />
        </Field>
      </form>
    </Sheet>
  );
}

export function PatientsPage() {
  const [busca, setBusca] = useState('');
  const termo = useDeferredValue(busca);
  const [tag, setTag] = useState<string | null>(null);
  const [novo, setNovo] = useState(false);
  const [filtros, setFiltros] = useState<Filtros>(PADRAO);
  const [abrirFiltros, setAbrirFiltros] = useState(false);
  const nFiltros = ativos(filtros);
  const q = usePacientes(termo);
  // A pesquisa avancada so roda com filtro ativo ou com o painel aberto (para listar responsaveis e marcadores).
  const adv = usePesquisa(termo, filtros, nFiltros > 0 || abrirFiltros);
  const sinaisQ = useSinais();
  const sinais = useMemo(() => new Map((sinaisQ.data?.sinais ?? []).map((x) => [x.pacienteId, x])), [sinaisQ.data]);
  const base = q.data?.itens ?? [];
  const lista: (Paciente & { extra?: ItemAvancado })[] = useMemo(() => {
    if (!nFiltros) return base;
    const porId = new Map(base.map((p) => [p.id, p]));
    return (adv.data?.itens ?? []).map((x) => ({ dataNascimento: null, sexo: null, createdAtUtc: '', ...porId.get(x.id), ...x, extra: x }));
  }, [nFiltros, base, adv.data]);
  const tags = useMemo(() => [...new Set(lista.flatMap((p) => p.tags))].sort(), [lista]);
  const filtrados = tag ? lista.filter((p) => p.tags.includes(tag)) : lista;
  const carregando = nFiltros ? adv.isLoading : q.isLoading;

  return (
    <Page wide>
      <PageHeader
        title="Pacientes"
        subtitle={nFiltros ? (adv.data ? `${adv.data.total} com os filtros atuais` : undefined) : q.data ? `${q.data.total ?? lista.length} em acompanhamento` : undefined}
        action={
          <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={() => setNovo(true)}>
            Novo
          </Button>
        }
      />
      <div className="sticky top-0 z-20 -mx-gutter bg-bg/90 px-gutter pb-3 pt-safe backdrop-blur-xl">
        <div className="flex gap-2">
          <label className="relative block min-w-0 flex-1">
            <Search className="pointer-events-none absolute left-3.5 top-3.5 h-5 w-5 text-faint" />
            <input id="busca-pacientes" type="search" placeholder="Buscar por nome, e-mail ou CPF" value={busca} onChange={(e) => setBusca(e.target.value)} className={inputCls + ' h-12 rounded-2xl pl-11'} />
          </label>
          <button
            onClick={() => setAbrirFiltros(true)}
            aria-label={nFiltros ? `Filtros (${nFiltros} ativos)` : 'Filtros'}
            className={clsx('relative grid h-12 w-12 shrink-0 place-items-center rounded-2xl border transition', nFiltros ? 'border-accent bg-accent/10 text-accent' : 'border-line bg-surface text-muted hover:text-fg')}
          >
            <SlidersHorizontal className="h-5 w-5" />
            {nFiltros > 0 && <span className="num absolute -right-1 -top-1 grid h-5 min-w-5 place-items-center rounded-full bg-accent px-1 text-[11px] font-bold text-accent-ink">{nFiltros}</span>}
          </button>
        </div>
        {tags.length > 0 && (
          <div className="no-scrollbar -mx-gutter mt-3 flex gap-2 overflow-x-auto px-gutter">
            {[null, ...tags].map((t) => (
              <button
                key={t ?? 'todos'}
                onClick={() => setTag(t)}
                className={clsx('h-8 shrink-0 rounded-full border px-3 text-[13px] font-semibold transition', tag === t ? 'border-accent bg-accent text-accent-ink' : 'border-line text-muted')}
              >
                {t ?? 'Todos'}
              </button>
            ))}
          </div>
        )}
      </div>

      {(nFiltros ? adv : q).isError && <ErrorBox error={(nFiltros ? adv : q).error} onRetry={() => (nFiltros ? adv : q).refetch()} />}
      {carregando ? (
        <div className="flex flex-col gap-2">
          {[0, 1, 2, 3, 4].map((i) => (
            <Skeleton key={i} className="h-16" />
          ))}
        </div>
      ) : filtrados.length === 0 ? (
        nFiltros ? (
          <Empty icon={<SlidersHorizontal className="h-5 w-5" />} title="Ninguém com esses filtros" action={<Button variant="outline" onClick={() => setFiltros(PADRAO)}>Limpar filtros</Button>} />
        ) : (
          <Empty icon={<UserPlus className="h-5 w-5" />} title={busca ? 'Ninguém com esse nome' : 'Nenhum paciente ainda'} action={<Button onClick={() => setNovo(true)}>Cadastrar paciente</Button>} />
        )
      ) : (
        <div className="grid grid-cols-1 gap-2 md:grid-cols-2 md:gap-3 xl:grid-cols-3">
          {filtrados.map((p) => {
            const a = idade(p.dataNascimento);
            const sinal = sinais.get(p.id);
            const nivel = sinal ? (NIVEL[sinal.nivel] ?? NIVEL.Estavel) : NIVEL.Estavel;
            return (
              <Link key={p.id} to={`/pro/pacientes/${p.id}`} className="group flex flex-col gap-2.5 rounded-xl2 border border-line bg-surface p-4 transition hover:border-accent/40">
                <div className="flex items-center gap-3">
                  <div className="grid h-12 w-12 shrink-0 place-items-center rounded-full bg-accent/15 font-display text-[15px] font-semibold text-accent">{iniciais(p.nome)}</div>
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-semibold">{p.nome}</p>
                    <p className="truncate text-[13px] text-muted">{[a != null && `${a} anos`, p.profissao].filter(Boolean).join(' · ') || p.email}</p>
                  </div>
                  <ChevronRight className="h-5 w-5 shrink-0 text-faint transition group-hover:translate-x-0.5" />
                </div>
                <div className="flex flex-wrap items-center gap-1.5">
                  <Pill tone={nivel.tone}>{nivel.label}</Pill>
                  {p.statusAcompanhamento !== 'Ativo' && <Pill tone="warn">{STATUS_ACOMPANHAMENTO[p.statusAcompanhamento] ?? p.statusAcompanhamento}</Pill>}
                  {p.tags.slice(0, 2).map((t) => (
                    <Pill key={t}>{t}</Pill>
                  ))}
                </div>
                <p className="num flex flex-wrap gap-x-3 text-[12px] text-muted">
                  {sinal ? (
                    <>
                      <span>
                        Prontidão{' '}
                        <b className={clsx(sinal.prontidaoScore == null ? 'text-faint' : sinal.prontidaoScore >= 75 ? 'text-ok' : sinal.prontidaoScore >= 55 ? 'text-warn' : 'text-bad')}>{sinal.prontidaoScore ?? '—'}</b>
                      </span>
                      <span>
                        <b className="text-fg">{sinal.treinosUltimos7Dias}</b> {sinal.treinosUltimos7Dias === 1 ? 'treino' : 'treinos'} em 7 dias
                      </span>
                      <span>Check-in {tempoRelativo(sinal.ultimoCheckInData)}</span>
                    </>
                  ) : (
                    <span>Sem alertas nos últimos 7 dias</span>
                  )}
                </p>
                {p.extra && (
                  <p className="num flex flex-wrap gap-x-3 text-[12px] text-muted">
                    <span>Adesão {p.extra.adesaoMediaPercentual == null ? '—' : `${Math.round(p.extra.adesaoMediaPercentual)}%`}</span>
                    <span>Interação {p.extra.ultimaInteracaoUtc ? tempoRelativo(p.extra.ultimaInteracaoUtc) : 'nunca'}</span>
                    {p.extra.responsavelNome && <span>{p.extra.responsavelNome}</span>}
                  </p>
                )}
              </Link>
            );
          })}
        </div>
      )}
      <NovoPaciente open={novo} onClose={() => setNovo(false)} />
      <FiltrosSheet
        open={abrirFiltros}
        valor={filtros}
        responsaveis={adv.data?.responsaveis ?? []}
        marcadores={adv.data?.marcadores ?? []}
        onClose={() => setAbrirFiltros(false)}
        onAplicar={(f) => {
          setFiltros(f);
          setAbrirFiltros(false);
        }}
      />
    </Page>
  );
}
