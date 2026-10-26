import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Apple, BookCopy, ChevronRight, Dumbbell, Library, Pill as PillIcon, Plus, Search } from 'lucide-react';
import { ReactNode, useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Field, inputCls, Pill, Segmented, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { hojeIso, n0, n1 } from '@/lib/format';
import { Chips } from './CuidadosPro';
import { usePacientes } from './queries';
import { ProgramasLista } from './Programas';
import { CATEGORIAS_SUPLEMENTO, porcaoSuplemento, type Suplemento, useSuplementos } from './suplementos';

// ---------------------------------------------------------------- Tipos

interface Exercicio {
  id: string;
  nome: string;
  grupoMuscular: string | null;
  equipamento: string | null;
  descricao: string | null;
  videoUrl: string | null;
  ativo: boolean;
}

interface Alimento {
  id: string;
  nome: string;
  categoria: string | null;
  caloriasPor100g: number;
  proteinasPor100g: number;
  carboidratosPor100g: number;
  gordurasPor100g: number;
  fibrasPor100g: number;
  ativo: boolean;
}

/** Os quatro tipos de modelo da biblioteca. Treino e dieta viram plano do paciente; sessao e refeicao entram num plano. */
export type TipoModelo = 'treino' | 'sessao' | 'dieta' | 'refeicao';

interface Modelo {
  id: string;
  nome: string;
  descricao: string | null;
  categoria?: string | null;
  ativo: boolean;
  profissionalNome: string | null;
  createdAtUtc: string;
  // treino
  sessoes?: number;
  exercicios?: number;
  objetivo?: string | null;
  // dieta
  refeicoes?: number;
  itens?: number;
  metaCalorias?: number | null;
  // sessao
  diasSemana?: string | null;
  // refeicao
  horario?: string | null;
  substituicoes?: number;
}

const ROTA: Record<TipoModelo, string> = {
  treino: '/api/modelos-planos-treino',
  sessao: '/api/modelos-sessoes-treino',
  dieta: '/api/modelos-planos-alimentares',
  refeicao: '/api/modelos-refeicoes',
};

const NOME: Record<TipoModelo, { um: string; varios: string }> = {
  treino: { um: 'Treino-modelo', varios: 'Treinos-modelo' },
  sessao: { um: 'Sessão-modelo', varios: 'Sessões-modelo' },
  dieta: { um: 'Dieta-modelo', varios: 'Dietas-modelo' },
  refeicao: { um: 'Refeição-modelo', varios: 'Refeições-modelo' },
};

const comCategoria = (t: TipoModelo) => t === 'sessao' || t === 'refeicao';

export const useModelos = (tipo: TipoModelo, incluirInativos = false) =>
  useQuery({ queryKey: ['pro', 'modelos', tipo, incluirInativos], queryFn: () => api<Modelo[]>(`${ROTA[tipo]}?incluirInativos=${incluirInativos}`) });

function resumoModelo(tipo: TipoModelo, m: Modelo) {
  const plural = (n: number | undefined, um: string, varios: string) => `${n ?? 0} ${n === 1 ? um : varios}`;
  if (tipo === 'treino') return [plural(m.sessoes, 'sessão', 'sessões'), plural(m.exercicios, 'exercício', 'exercícios'), m.objetivo].filter(Boolean).join(' · ');
  if (tipo === 'dieta') return [plural(m.refeicoes, 'refeição', 'refeições'), plural(m.itens, 'item', 'itens'), m.metaCalorias ? `${n0(m.metaCalorias)} kcal` : null].filter(Boolean).join(' · ');
  if (tipo === 'sessao') return [m.categoria, plural(m.exercicios, 'exercício', 'exercícios'), m.diasSemana].filter(Boolean).join(' · ');
  return [m.categoria, m.horario?.slice(0, 5), plural(m.itens, 'item', 'itens')].filter(Boolean).join(' · ');
}

const erroToast = (e: unknown) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad');

export function Busca({ value, onChange, placeholder }: { value: string; onChange: (v: string) => void; placeholder: string }) {
  return (
    <label className="relative block">
      <Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-faint" />
      <input className={clsx(inputCls, 'pl-10')} value={value} placeholder={placeholder} aria-label={placeholder} onChange={(e) => onChange(e.target.value)} />
    </label>
  );
}

export function Linha({ titulo, sub, extra, onClick }: { titulo: ReactNode; sub?: ReactNode; extra?: ReactNode; onClick: () => void }) {
  return (
    <button onClick={onClick} className="flex w-full items-center gap-3 p-3.5 text-left transition hover:bg-raised/40">
      <span className="min-w-0 flex-1 [overflow-wrap:anywhere]">
        <span className="block font-semibold">{titulo}</span>
        {sub && <span className="block text-[13px] text-muted">{sub}</span>}
      </span>
      {extra}
      <ChevronRight className="h-4 w-4 shrink-0 text-faint" />
    </button>
  );
}

function Inativos({ value, onChange }: { value: boolean; onChange: (v: boolean) => void }) {
  return (
    <label className="flex shrink-0 items-center gap-2 text-[13px] text-muted">
      <input type="checkbox" checked={value} onChange={(e) => onChange(e.target.checked)} className="h-4 w-4 accent-[rgb(var(--accent))]" />
      Mostrar inativos
    </label>
  );
}

// ---------------------------------------------------------------- Pagina

type Aba = 'exercicios' | 'alimentos' | 'suplementos' | 'modelos';

/** Biblioteca do profissional: catalogo de exercicios, tabela de alimentos e modelos reutilizaveis. */
export function BibliotecaPage() {
  const [params, setParams] = useSearchParams();
  const aba = (params.get('aba') as Aba) || 'modelos';
  return (
    <Page>
      <PageHeader back="/pro/perfil" title="Biblioteca" subtitle="Exercícios, alimentos, suplementos e modelos prontos para usar com qualquer paciente." />
      <Segmented<Aba>
        value={aba}
        onChange={(v) => setParams({ aba: v }, { replace: true })}
        options={[
          { value: 'modelos', label: 'Modelos' },
          { value: 'exercicios', label: 'Exercícios' },
          { value: 'alimentos', label: 'Alimentos' },
          { value: 'suplementos', label: 'Suplementos' },
        ]}
      />
      <div className="mt-4">
        {aba === 'modelos' && <Modelos />}
        {aba === 'exercicios' && <Exercicios />}
        {aba === 'alimentos' && <Alimentos />}
        {aba === 'suplementos' && <Suplementos />}
      </div>
    </Page>
  );
}

// ---------------------------------------------------------------- Modelos

function Modelos() {
  const [params, setParams] = useSearchParams();
  const tipoUrl = params.get('tipo');
  const tipo = (['treino', 'dieta', 'sessao', 'refeicao', 'programa'].includes(tipoUrl ?? '') ? tipoUrl : 'treino') as TipoModelo | 'programa';
  const setTipo = (t: TipoModelo | 'programa') => setParams({ aba: 'modelos', tipo: t }, { replace: true });
  const [inativos, setInativos] = useState(false);
  return (
    <>
      <div className="-mx-gutter mb-3 overflow-x-auto px-gutter pb-1">
        <Chips<TipoModelo | 'programa'>
          value={tipo}
          onChange={setTipo}
          options={[
            { value: 'treino', label: 'Treinos' },
            { value: 'programa', label: 'Programas' },
            { value: 'dieta', label: 'Dietas' },
            { value: 'sessao', label: 'Sessões' },
            { value: 'refeicao', label: 'Refeições' },
          ]}
        />
      </div>
      {tipo === 'programa' ? (
        <>
          <div className="mb-3 flex justify-end">
            <Inativos value={inativos} onChange={setInativos} />
          </div>
          <ProgramasLista inativos={inativos} />
        </>
      ) : (
        <ModelosDoTipo key={tipo} tipo={tipo} inativos={inativos} setInativos={setInativos} />
      )}
    </>
  );
}

function ModelosDoTipo({ tipo, inativos, setInativos }: { tipo: TipoModelo; inativos: boolean; setInativos: (v: boolean) => void }) {
  const [aberto, setAberto] = useState<Modelo | null>(null);
  const q = useModelos(tipo, inativos);
  const lista = q.data ?? [];
  const vazio: Record<TipoModelo, string> = {
    treino: 'Abra um treino de paciente e use "Salvar como modelo". Depois é só aplicar uma cópia em outro paciente.',
    dieta: 'Abra uma dieta de paciente e use "Salvar como modelo". Depois é só aplicar uma cópia em outro paciente.',
    sessao: 'Sessões-modelo nascem de uma sessão de treino salva e podem ser inseridas em qualquer plano.',
    refeicao: 'Refeições-modelo nascem de uma refeição de dieta salva e podem ser inseridas em qualquer plano.',
  };
  return (
    <>
      <div className="mb-3 flex items-center justify-between gap-3">
        <p className="text-sm text-muted">
          {lista.length} {lista.length === 1 ? NOME[tipo].um.toLowerCase() : NOME[tipo].varios.toLowerCase()}
        </p>
        <Inativos value={inativos} onChange={setInativos} />
      </div>
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : !lista.length ? (
        <Empty icon={<BookCopy className="h-5 w-5" />} title={`Nenhum ${NOME[tipo].um.toLowerCase()}`}>
          {vazio[tipo]}
        </Empty>
      ) : (
        <Card className="divide-y divide-line/60">
          {lista.map((m) => (
            <Linha key={m.id} titulo={m.nome} sub={resumoModelo(tipo, m)} extra={!m.ativo && <Pill>Inativo</Pill>} onClick={() => setAberto(m)} />
          ))}
        </Card>
      )}
      <ModeloSheet tipo={tipo} modelo={aberto} onClose={() => setAberto(null)} />
    </>
  );
}

function ModeloSheet({ tipo, modelo, onClose }: { tipo: TipoModelo; modelo: Modelo | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [nome, setNome] = useState('');
  const [descricao, setDescricao] = useState('');
  const [categoria, setCategoria] = useState('');
  const [usar, setUsar] = useState(false);
  useEffect(() => {
    if (!modelo) return;
    setNome(modelo.nome);
    setDescricao(modelo.descricao ?? '');
    setCategoria(modelo.categoria ?? '');
    setUsar(false);
  }, [modelo]);

  const salvar = useMutation({
    mutationFn: (ativo: boolean) =>
      api(`${ROTA[tipo]}/${modelo!.id}`, {
        method: 'PUT',
        json: { nome: nome.trim(), descricao: descricao.trim() || null, ...(comCategoria(tipo) ? { categoria: categoria.trim() || null } : {}), ativo },
      }),
    onSuccess: (_, ativo) => {
      toast(ativo === modelo!.ativo ? 'Modelo salvo.' : ativo ? 'Modelo reativado.' : 'Modelo desativado.');
      qc.invalidateQueries({ queryKey: ['pro', 'modelos', tipo] });
      onClose();
    },
    onError: erroToast,
  });

  const aplicavel = tipo === 'treino' || tipo === 'dieta';
  return (
    <>
      <Sheet
        open={!!modelo && !usar}
        onClose={onClose}
        title={NOME[tipo].um}
        footer={
          <div className="flex flex-col gap-2">
            {aplicavel && modelo?.ativo && (
              <Button size="lg" className="w-full" icon={<Plus className="h-5 w-5" />} onClick={() => setUsar(true)}>
                Usar com um paciente
              </Button>
            )}
            <Button size="lg" variant={aplicavel && modelo?.ativo ? 'soft' : 'primary'} className="w-full" disabled={!nome.trim()} loading={salvar.isPending && salvar.variables === modelo?.ativo} onClick={() => salvar.mutate(modelo!.ativo)}>
              Salvar alterações
            </Button>
          </div>
        }
      >
        {modelo && (
          <div className="flex flex-col gap-4">
            <p className="text-[13px] text-muted">
              {resumoModelo(tipo, modelo)}
              {modelo.profissionalNome && ` · por ${modelo.profissionalNome}`}
            </p>
            {tipo === 'treino' && <ConteudoTreino id={modelo.id} />}
            <Field label="Nome">
              <input className={inputCls} value={nome} maxLength={160} onChange={(e) => setNome(e.target.value)} />
            </Field>
            {comCategoria(tipo) && (
              <Field label="Categoria">
                <input className={inputCls} value={categoria} maxLength={80} placeholder={tipo === 'sessao' ? 'Ex.: Inferiores, Mobilidade' : 'Ex.: Café da manhã, Pré-treino'} onChange={(e) => setCategoria(e.target.value)} />
              </Field>
            )}
            <Field label="Descrição">
              <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={descricao} maxLength={1000} onChange={(e) => setDescricao(e.target.value)} />
            </Field>
            <Button variant="ghost" className={modelo.ativo ? 'text-muted' : 'text-accent'} loading={salvar.isPending && salvar.variables !== modelo.ativo} onClick={() => salvar.mutate(!modelo.ativo)}>
              {modelo.ativo ? 'Desativar modelo' : 'Reativar modelo'}
            </Button>
          </div>
        )}
      </Sheet>
      {aplicavel && <UsarModeloSheet tipo={tipo} modelo={usar ? modelo : null} onClose={() => (usar ? onClose() : undefined)} />}
    </>
  );
}

interface ConteudoTreinoModelo {
  conteudo: { sessoes: { nome: string; diasSemana: string | null; itens: { exercicioId: string; series: number; repeticoes: string }[] }[] } | null;
}

function ConteudoTreino({ id }: { id: string }) {
  const q = useQuery({ queryKey: ['pro', 'modelo-treino', id], queryFn: () => api<ConteudoTreinoModelo>(`/api/modelos-planos-treino/${id}`) });
  const ex = useExercicios(true);
  const nomes = new Map((ex.data ?? []).map((x) => [x.id, x.nome]));
  if (q.isLoading) return <Skeleton className="h-24" />;
  const sessoes = q.data?.conteudo?.sessoes ?? [];
  if (!sessoes.length) return null;
  return (
    <div className="flex flex-col gap-2">
      {sessoes.map((s, i) => (
        <div key={i} className="rounded-xl bg-raised/70 px-3 py-2.5">
          <p className="text-[14px] font-semibold">
            {s.nome}
            {s.diasSemana && <span className="font-normal text-muted"> · {s.diasSemana}</span>}
          </p>
          <p className="text-[12px] text-muted">{s.itens.map((it) => `${nomes.get(it.exercicioId) ?? 'Exercício'} ${it.series}x${it.repeticoes}`).join(' · ')}</p>
        </div>
      ))}
    </div>
  );
}

/**
 * Aplica um treino-modelo ou dieta-modelo num paciente: cria uma copia independente e abre no editor.
 * Sem pacienteId, pede o paciente; sem modelo, pede o modelo.
 */
export function UsarModeloSheet({ tipo, modelo, pacienteId, open, onClose }: { tipo: 'treino' | 'dieta'; modelo?: Modelo | null; pacienteId?: string; open?: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const nav = useNavigate();
  const aberto = open ?? !!modelo;
  const modelos = useModelos(tipo);
  const [escolhido, setEscolhido] = useState<Modelo | null>(null);
  const [paciente, setPaciente] = useState<{ id: string; nome: string } | null>(null);
  const [busca, setBusca] = useState('');
  const [nome, setNome] = useState('');
  const [inicio, setInicio] = useState(hojeIso());
  const pacientes = usePacientes(busca);
  const m = modelo ?? escolhido;
  const pid = pacienteId ?? paciente?.id;

  useEffect(() => {
    if (!aberto) return;
    setEscolhido(null);
    setPaciente(null);
    setBusca('');
    setInicio(hojeIso());
  }, [aberto]);
  useEffect(() => setNome(m?.nome ?? ''), [m]);

  const criar = useMutation({
    mutationFn: () =>
      api<{ id: string }>(tipo === 'treino' ? `/api/pacientes/${pid}/treinos/criar-de-modelo/${m!.id}` : `/api/pacientes/${pid}/planos-alimentares/criar-de-modelo/${m!.id}`, {
        method: 'POST',
        json: { nome: nome.trim(), dataInicio: inicio, dataFim: null, observacoes: null, ...(tipo === 'treino' ? { objetivo: m!.objetivo ?? null } : {}) },
      }),
    onSuccess: (novo) => {
      toast('Plano criado a partir do modelo. Revise e libere ao paciente.');
      qc.invalidateQueries({ queryKey: ['pro', tipo === 'dieta' ? 'dietas' : 'treinos', pid] });
      onClose();
      nav(`/pro/pacientes/${pid}/${tipo}/${novo.id}`);
    },
    onError: erroToast,
  });

  const disponiveis = modelos.data ?? [];
  return (
    <Sheet
      open={aberto}
      onClose={onClose}
      title={tipo === 'treino' ? 'Usar treino-modelo' : 'Usar dieta-modelo'}
      footer={
        <Button size="lg" className="w-full" disabled={!m || !pid || !nome.trim() || !inicio} loading={criar.isPending} onClick={() => criar.mutate()}>
          Criar plano e abrir
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        {!modelo &&
          (modelos.isLoading ? (
            <Skeleton className="h-24" />
          ) : !disponiveis.length ? (
            <p className="rounded-xl bg-raised/60 p-3 text-sm text-muted">
              Nenhum {tipo === 'treino' ? 'treino' : 'dieta'}-modelo ainda. Use "Salvar como modelo" num plano pronto para começar a biblioteca.
            </p>
          ) : (
            <Field label="Modelo" group>
              <div className="flex flex-col gap-2">
                {disponiveis.map((x) => (
                  <button
                    key={x.id}
                    type="button"
                    aria-pressed={escolhido?.id === x.id}
                    onClick={() => setEscolhido(x)}
                    className={clsx('rounded-xl border p-3 text-left transition', escolhido?.id === x.id ? 'border-accent bg-accent/10' : 'border-line hover:bg-raised')}
                  >
                    <span className="block font-semibold">{x.nome}</span>
                    <span className="block text-[13px] text-muted">{resumoModelo(tipo, x)}</span>
                  </button>
                ))}
              </div>
            </Field>
          ))}
        {!pacienteId && (
          <Field label="Paciente" group>
            {paciente ? (
              <div className="flex items-center justify-between rounded-xl border border-accent bg-accent/10 p-3">
                <span className="font-semibold">{paciente.nome}</span>
                <button type="button" className="text-sm font-semibold text-accent" onClick={() => setPaciente(null)}>
                  Trocar
                </button>
              </div>
            ) : (
              <div className="flex flex-col gap-2">
                <Busca value={busca} onChange={setBusca} placeholder="Buscar paciente" />
                <Card className="max-h-56 divide-y divide-line/60 overflow-y-auto">
                  {(pacientes.data?.itens ?? []).slice(0, 30).map((p) => (
                    <button key={p.id} type="button" onClick={() => setPaciente({ id: p.id, nome: p.nome })} className="block w-full p-3 text-left text-[15px] hover:bg-raised/40">
                      {p.nome}
                    </button>
                  ))}
                  {pacientes.data && !pacientes.data.itens.length && <p className="p-3 text-sm text-muted">Nenhum paciente encontrado.</p>}
                </Card>
              </div>
            )}
          </Field>
        )}
        {m && (
          <>
            <Field label="Nome do plano">
              <input className={inputCls} value={nome} maxLength={160} onChange={(e) => setNome(e.target.value)} />
            </Field>
            <Field label="Começa em">
              <input type="date" className={inputCls} value={inicio} onChange={(e) => setInicio(e.target.value)} />
            </Field>
            <p className="text-[13px] text-muted">O paciente recebe uma cópia. Mudanças nela não alteram o modelo, e o plano só aparece para o paciente quando você liberar.</p>
          </>
        )}
      </div>
    </Sheet>
  );
}

/** Guarda um plano do paciente como modelo da biblioteca. */
export function SalvarModeloSheet({ tipo, plano, onClose }: { tipo: 'treino' | 'dieta'; plano: { id: string; nome: string } | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [nome, setNome] = useState('');
  const [descricao, setDescricao] = useState('');
  useEffect(() => {
    setNome(plano?.nome.replace(/ \(cópia\)$/, '') ?? '');
    setDescricao('');
  }, [plano]);
  const salvar = useMutation({
    mutationFn: () =>
      api(tipo === 'treino' ? `/api/treinos/${plano!.id}/salvar-como-modelo` : `/api/planos-alimentares/${plano!.id}/salvar-como-modelo`, {
        method: 'POST',
        json: { nome: nome.trim(), descricao: descricao.trim() || null },
      }),
    onSuccess: () => {
      toast('Modelo salvo na biblioteca.');
      qc.invalidateQueries({ queryKey: ['pro', 'modelos', tipo] });
      onClose();
    },
    onError: erroToast,
  });
  return (
    <Sheet
      open={!!plano}
      onClose={onClose}
      title="Salvar como modelo"
      footer={
        <Button size="lg" className="w-full" disabled={!nome.trim()} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar na biblioteca
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <p className="text-[13px] text-muted">O modelo guarda a estrutura do plano, sem dados do paciente. Você pode aplicar uma cópia em qualquer paciente depois.</p>
        <Field label="Nome do modelo">
          <input className={inputCls} value={nome} maxLength={160} onChange={(e) => setNome(e.target.value)} />
        </Field>
        <Field label="Descrição (opcional)">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={descricao} maxLength={1000} placeholder="Para quem serve, fase, observações..." onChange={(e) => setDescricao(e.target.value)} />
        </Field>
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Exercicios

const useExercicios = (incluirInativos: boolean) =>
  useQuery({ queryKey: ['pro', 'exercicios', incluirInativos], queryFn: () => api<Exercicio[]>(`/api/exercicios?incluirInativos=${incluirInativos}`) });

const semAcento = (s: string) => s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();

function Exercicios() {
  const qc = useQueryClient();
  const [inativos, setInativos] = useState(false);
  const [busca, setBusca] = useState('');
  const [grupo, setGrupo] = useState('Todos');
  const [aberto, setAberto] = useState<Exercicio | 'novo' | null>(null);
  const q = useExercicios(inativos);
  const todos = q.data ?? [];
  const grupos = ['Todos', ...Array.from(new Set(todos.map((x) => x.grupoMuscular).filter((x): x is string => !!x))).sort()];
  const termo = semAcento(busca.trim());
  const lista = todos.filter(
    (x) => (grupo === 'Todos' || x.grupoMuscular === grupo) && (!termo || semAcento([x.nome, x.grupoMuscular, x.equipamento].filter(Boolean).join(' ')).includes(termo)),
  );

  const base = useMutation({
    mutationFn: () => api<{ criados: number; ignorados: number }>('/api/exercicios/catalogo-base', { method: 'POST' }),
    onSuccess: (r) => {
      toast(r.criados ? `${r.criados} exercícios adicionados ao catálogo.` : 'Seu catálogo já tem todos os exercícios da base.');
      qc.invalidateQueries({ queryKey: ['pro', 'exercicios'] });
    },
    onError: erroToast,
  });

  return (
    <>
      <div className="flex flex-col gap-3">
        <Busca value={busca} onChange={setBusca} placeholder="Buscar por nome, grupo ou equipamento" />
        {grupos.length > 2 && (
          <div className="-mx-gutter overflow-x-auto px-gutter pb-1">
            <div className="flex w-max gap-2">
              {grupos.map((g) => (
                <button
                  key={g}
                  type="button"
                  onClick={() => setGrupo(g)}
                  className={clsx('h-9 shrink-0 rounded-full border px-3.5 text-sm font-semibold transition', grupo === g ? 'border-accent bg-accent/10 text-accent' : 'border-line text-muted hover:bg-raised')}
                >
                  {g}
                </button>
              ))}
            </div>
          </div>
        )}
        <div className="flex items-center justify-between gap-3">
          <p className="text-sm text-muted">
            {lista.length} {lista.length === 1 ? 'exercício' : 'exercícios'}
          </p>
          <Inativos value={inativos} onChange={setInativos} />
        </div>
        <div className="flex gap-2">
          <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={() => setAberto('novo')}>
            Novo exercício
          </Button>
          <Button size="sm" variant="soft" icon={<Library className="h-4 w-4" />} loading={base.isPending} onClick={() => base.mutate()}>
            Carregar base inicial
          </Button>
        </div>
      </div>
      <div className="mt-3">
        {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
        {q.isLoading ? (
          <Skeleton className="h-64" />
        ) : !lista.length ? (
          <Empty icon={<Dumbbell className="h-5 w-5" />} title={todos.length ? 'Nada encontrado' : 'Catálogo vazio'}>
            {todos.length ? 'Tente outro termo ou grupo.' : 'Carregue a base inicial com os exercícios mais usados ou cadastre os seus.'}
          </Empty>
        ) : (
          <Card className="divide-y divide-line/60">
            {lista.map((x) => (
              <Linha key={x.id} titulo={x.nome} sub={[x.grupoMuscular, x.equipamento].filter(Boolean).join(' · ') || undefined} extra={!x.ativo && <Pill>Inativo</Pill>} onClick={() => setAberto(x)} />
            ))}
          </Card>
        )}
      </div>
      <ExercicioSheet item={aberto} todos={todos} onClose={() => setAberto(null)} />
    </>
  );
}

function ExercicioSheet({ item, todos, onClose }: { item: Exercicio | 'novo' | null; todos: Exercicio[]; onClose: () => void }) {
  const qc = useQueryClient();
  const novo = item === 'novo';
  const e = novo ? null : item;
  const [f, setF] = useState({ nome: '', grupoMuscular: '', equipamento: '', descricao: '', videoUrl: '' });
  useEffect(() => {
    if (!item) return;
    setF({ nome: e?.nome ?? '', grupoMuscular: e?.grupoMuscular ?? '', equipamento: e?.equipamento ?? '', descricao: e?.descricao ?? '', videoUrl: e?.videoUrl ?? '' });
  }, [item]);
  const set = (k: keyof typeof f) => (ev: { target: { value: string } }) => setF((x) => ({ ...x, [k]: ev.target.value }));
  const invalidar = () => qc.invalidateQueries({ queryKey: ['pro', 'exercicios'] });

  const salvar = useMutation({
    mutationFn: () => {
      const json = { nome: f.nome.trim(), grupoMuscular: f.grupoMuscular.trim() || null, equipamento: f.equipamento.trim() || null, descricao: f.descricao.trim() || null, videoUrl: f.videoUrl.trim() || null };
      return novo ? api('/api/exercicios', { method: 'POST', json }) : api(`/api/exercicios/${e!.id}`, { method: 'PUT', json });
    },
    onSuccess: () => {
      toast(novo ? 'Exercício adicionado.' : 'Exercício salvo.');
      invalidar();
      onClose();
    },
    onError: erroToast,
  });
  const status = useMutation({
    mutationFn: () => (e!.ativo ? api(`/api/exercicios/${e!.id}`, { method: 'DELETE' }) : api(`/api/exercicios/${e!.id}/reativar`, { method: 'POST' })),
    onSuccess: () => {
      toast(e!.ativo ? 'Exercício desativado. O histórico continua preservado.' : 'Exercício reativado.');
      invalidar();
      onClose();
    },
    onError: erroToast,
  });

  const opcoes = (k: 'grupoMuscular' | 'equipamento') => Array.from(new Set(todos.map((x) => x[k]).filter(Boolean))).sort() as string[];
  const videoInvalido = !!f.videoUrl.trim() && !/^https?:\/\//i.test(f.videoUrl.trim());
  return (
    <Sheet
      open={!!item}
      onClose={onClose}
      title={novo ? 'Novo exercício' : 'Exercício'}
      footer={
        <Button size="lg" className="w-full" disabled={!f.nome.trim() || videoInvalido} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome">
          <input className={inputCls} value={f.nome} maxLength={160} onChange={set('nome')} />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Grupo muscular">
            <input className={inputCls} list="grupos-exercicio" value={f.grupoMuscular} maxLength={80} onChange={set('grupoMuscular')} />
          </Field>
          <Field label="Equipamento">
            <input className={inputCls} list="equipamentos-exercicio" value={f.equipamento} maxLength={80} onChange={set('equipamento')} />
          </Field>
        </div>
        <datalist id="grupos-exercicio">{opcoes('grupoMuscular').map((x) => <option key={x} value={x} />)}</datalist>
        <datalist id="equipamentos-exercicio">{opcoes('equipamento').map((x) => <option key={x} value={x} />)}</datalist>
        <Field label="Descrição e execução">
          <textarea className={clsx(inputCls, 'h-24 py-2.5')} value={f.descricao} maxLength={2000} onChange={set('descricao')} />
        </Field>
        <Field label="Vídeo de referência (link)" hint={videoInvalido ? 'Use um link que comece com https://' : undefined}>
          <input className={inputCls} type="url" inputMode="url" value={f.videoUrl} placeholder="https://" maxLength={500} onChange={set('videoUrl')} />
        </Field>
        {e && (
          <Button variant="ghost" className={e.ativo ? 'text-muted' : 'text-accent'} loading={status.isPending} onClick={() => status.mutate()}>
            {e.ativo ? 'Desativar exercício' : 'Reativar exercício'}
          </Button>
        )}
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Alimentos

function Alimentos() {
  const [inativos, setInativos] = useState(false);
  const [busca, setBusca] = useState('');
  const [aberto, setAberto] = useState<Alimento | 'novo' | null>(null);
  const q = useQuery({ queryKey: ['pro', 'alimentos-lista', inativos], queryFn: () => api<Alimento[]>(`/api/alimentos?incluirInativos=${inativos}`) });
  const todos = q.data ?? [];
  const termo = semAcento(busca.trim());
  const lista = todos.filter((x) => !termo || semAcento(`${x.nome} ${x.categoria ?? ''}`).includes(termo));
  const categorias = Array.from(new Set(todos.map((x) => x.categoria).filter(Boolean))).sort() as string[];

  return (
    <>
      <div className="flex flex-col gap-3">
        <Busca value={busca} onChange={setBusca} placeholder="Buscar alimento ou categoria" />
        <div className="flex items-center justify-between gap-3">
          <p className="text-sm text-muted">
            {lista.length} {lista.length === 1 ? 'alimento' : 'alimentos'}
          </p>
          <Inativos value={inativos} onChange={setInativos} />
        </div>
        <div>
          <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={() => setAberto('novo')}>
            Novo alimento
          </Button>
        </div>
      </div>
      <div className="mt-3">
        {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
        {q.isLoading ? (
          <Skeleton className="h-64" />
        ) : !lista.length ? (
          <Empty icon={<Apple className="h-5 w-5" />} title={todos.length ? 'Nada encontrado' : 'Tabela vazia'}>
            {todos.length ? 'Tente outro termo.' : 'Cadastre os alimentos que você usa nas dietas, com valores por 100 g.'}
          </Empty>
        ) : (
          <Card className="divide-y divide-line/60">
            {lista.map((x) => (
              <Linha
                key={x.id}
                titulo={x.nome}
                sub={`${x.categoria ? `${x.categoria} · ` : ''}${n0(x.caloriasPor100g)} kcal · P ${n1(x.proteinasPor100g)} · C ${n1(x.carboidratosPor100g)} · G ${n1(x.gordurasPor100g)} por 100 g`}
                extra={!x.ativo && <Pill>Inativo</Pill>}
                onClick={() => setAberto(x)}
              />
            ))}
          </Card>
        )}
      </div>
      <AlimentoSheet item={aberto} categorias={categorias} onClose={() => setAberto(null)} />
    </>
  );
}

const CAMPOS_ALIMENTO = [
  ['caloriasPor100g', 'Calorias (kcal)'],
  ['proteinasPor100g', 'Proteínas (g)'],
  ['carboidratosPor100g', 'Carboidratos (g)'],
  ['gordurasPor100g', 'Gorduras (g)'],
  ['fibrasPor100g', 'Fibras (g)'],
] as const;

function AlimentoSheet({ item, categorias, onClose }: { item: Alimento | 'novo' | null; categorias: string[]; onClose: () => void }) {
  const qc = useQueryClient();
  const novo = item === 'novo';
  const a = novo ? null : item;
  const [nome, setNome] = useState('');
  const [categoria, setCategoria] = useState('');
  const [v, setV] = useState<Record<string, string>>({});
  useEffect(() => {
    if (!item) return;
    setNome(a?.nome ?? '');
    setCategoria(a?.categoria ?? '');
    setV(Object.fromEntries(CAMPOS_ALIMENTO.map(([k]) => [k, a ? String(a[k]).replace('.', ',') : ''])));
  }, [item]);
  const num = (s: string | undefined) => (s?.trim() ? Number(s.replace(',', '.')) : 0);
  const valido = !!nome.trim() && CAMPOS_ALIMENTO.every(([k]) => num(v[k]) >= 0 && !Number.isNaN(num(v[k])));
  const invalidar = () => {
    qc.invalidateQueries({ queryKey: ['pro', 'alimentos-lista'] });
    qc.invalidateQueries({ queryKey: ['pro', 'alimentos'] });
  };

  const salvar = useMutation({
    mutationFn: () => {
      const json = { nome: nome.trim(), categoria: categoria.trim() || null, ...Object.fromEntries(CAMPOS_ALIMENTO.map(([k]) => [k, num(v[k])])) };
      return novo ? api('/api/alimentos', { method: 'POST', json }) : api(`/api/alimentos/${a!.id}`, { method: 'PUT', json });
    },
    onSuccess: () => {
      toast(novo ? 'Alimento adicionado.' : 'Alimento salvo.');
      invalidar();
      onClose();
    },
    onError: erroToast,
  });
  const status = useMutation({
    mutationFn: () => (a!.ativo ? api(`/api/alimentos/${a!.id}`, { method: 'DELETE' }) : api(`/api/alimentos/${a!.id}/reativar`, { method: 'POST' })),
    onSuccess: () => {
      toast(a!.ativo ? 'Alimento desativado. Dietas existentes não mudam.' : 'Alimento reativado.');
      invalidar();
      onClose();
    },
    onError: erroToast,
  });

  return (
    <Sheet
      open={!!item}
      onClose={onClose}
      title={novo ? 'Novo alimento' : 'Alimento'}
      footer={
        <Button size="lg" className="w-full" disabled={!valido} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome">
          <input className={inputCls} value={nome} maxLength={160} onChange={(e) => setNome(e.target.value)} />
        </Field>
        <Field label="Categoria">
          <input className={inputCls} list="categorias-alimento" value={categoria} maxLength={80} placeholder="Ex.: Carnes, Frutas, Cereais" onChange={(e) => setCategoria(e.target.value)} />
        </Field>
        <datalist id="categorias-alimento">{categorias.map((x) => <option key={x} value={x} />)}</datalist>
        <p className="-mb-2 text-[13px] font-semibold text-muted">Valores por 100 g</p>
        <div className="grid grid-cols-2 gap-3">
          {CAMPOS_ALIMENTO.map(([k, l]) => (
            <Field key={k} label={l}>
              <input className={inputCls} inputMode="decimal" value={v[k] ?? ''} placeholder="0" onChange={(e) => setV((x) => ({ ...x, [k]: e.target.value }))} />
            </Field>
          ))}
        </div>
        {a && (
          <Button variant="ghost" className={a.ativo ? 'text-muted' : 'text-accent'} loading={status.isPending} onClick={() => status.mutate()}>
            {a.ativo ? 'Desativar alimento' : 'Reativar alimento'}
          </Button>
        )}
      </div>
    </Sheet>
  );
}

// ---------------------------------------------------------------- Suplementos

function Suplementos() {
  const [inativos, setInativos] = useState(false);
  const [busca, setBusca] = useState('');
  const [cat, setCat] = useState<string>('Todos');
  const [aberto, setAberto] = useState<Suplemento | 'novo' | null>(null);
  const q = useSuplementos(inativos);
  const todos = q.data ?? [];
  const termo = semAcento(busca.trim());
  const lista = todos.filter((x) => (cat === 'Todos' || x.categoria === cat) && (!termo || semAcento(`${x.nome} ${x.marca ?? ''} ${x.categoria} ${x.composicao ?? ''}`).includes(termo)));
  const usadas = ['Todos', ...CATEGORIAS_SUPLEMENTO.filter((c) => todos.some((x) => x.categoria === c))];

  return (
    <>
      <div className="flex flex-col gap-3">
        <Busca value={busca} onChange={setBusca} placeholder="Buscar suplemento, marca ou composição" />
        {usadas.length > 2 && (
          <div className="-mx-gutter overflow-x-auto px-gutter pb-1">
            <Chips<string> value={cat} onChange={setCat} options={usadas.map((c) => ({ value: c, label: c }))} />
          </div>
        )}
        <div className="flex items-center justify-between gap-3">
          <p className="text-sm text-muted">
            {lista.length} {lista.length === 1 ? 'suplemento' : 'suplementos'}
          </p>
          <Inativos value={inativos} onChange={setInativos} />
        </div>
        <div>
          <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={() => setAberto('novo')}>
            Novo suplemento
          </Button>
        </div>
      </div>
      <div className="mt-3">
        {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
        {q.isLoading ? (
          <Skeleton className="h-64" />
        ) : !lista.length ? (
          <Empty icon={<PillIcon className="h-5 w-5" />} title={todos.length ? 'Nada encontrado' : 'Catálogo vazio'}>
            {todos.length ? 'Tente outro termo ou categoria.' : 'Cadastre whey, creatina, pré-treino e o que mais você usa. Depois é só incluir no cronograma da dieta.'}
          </Empty>
        ) : (
          <Card className="divide-y divide-line/60">
            {lista.map((x) => (
              <Linha
                key={x.id}
                titulo={x.nome}
                sub={`${x.categoria}${x.marca ? ` · ${x.marca}` : ''} · ${porcaoSuplemento(x)}`}
                extra={!x.ativo && <Pill>Inativo</Pill>}
                onClick={() => setAberto(x)}
              />
            ))}
          </Card>
        )}
      </div>
      <SuplementoSheet item={aberto} onClose={() => setAberto(null)} />
    </>
  );
}

const CAMPOS_SUPLEMENTO = [
  ['caloriasPorPorcao', 'Calorias (kcal)'],
  ['proteinasGPorPorcao', 'Proteínas (g)'],
  ['carboidratosGPorPorcao', 'Carboidratos (g)'],
  ['gordurasGPorPorcao', 'Gorduras (g)'],
  ['fibrasGPorPorcao', 'Fibras (g)'],
  ['cafeinaMgPorPorcao', 'Cafeína (mg)'],
] as const;

function SuplementoSheet({ item, onClose }: { item: Suplemento | 'novo' | null; onClose: () => void }) {
  const qc = useQueryClient();
  const novo = item === 'novo';
  const a = novo ? null : item;
  const [f, setF] = useState<Record<string, string>>({});
  useEffect(() => {
    if (!item) return;
    const txt = (v: number | null | undefined) => (v == null ? '' : String(v).replace('.', ','));
    setF({
      nome: a?.nome ?? '',
      marca: a?.marca ?? '',
      categoria: a?.categoria ?? 'Proteína',
      forma: a?.forma ?? '',
      porcaoQuantidade: a ? txt(a.porcaoQuantidade) : '1',
      porcaoUnidade: a?.porcaoUnidade ?? 'scoop',
      composicao: a?.composicao ?? '',
      instrucoesUso: a?.instrucoesUso ?? '',
      observacoes: a?.observacoes ?? '',
      ...Object.fromEntries(CAMPOS_SUPLEMENTO.map(([k]) => [k, a ? txt(a[k]) : ''])),
    });
  }, [item]);
  const set = (k: string) => (e: { target: { value: string } }) => setF((x) => ({ ...x, [k]: e.target.value }));
  const num = (s: string | undefined) => (s?.trim() ? Number(s.replace(',', '.')) : 0);
  const porcao = num(f.porcaoQuantidade);
  const valido = !!f.nome?.trim() && porcao > 0 && CAMPOS_SUPLEMENTO.every(([k]) => !Number.isNaN(num(f[k])) && num(f[k]) >= 0);
  const invalidar = () => qc.invalidateQueries({ queryKey: ['pro', 'suplementos'] });
  const salvar = useMutation({
    mutationFn: () => {
      const json = {
        nome: f.nome.trim(),
        marca: f.marca.trim() || null,
        categoria: f.categoria || 'Outros',
        forma: f.forma.trim() || null,
        porcaoQuantidade: porcao,
        porcaoUnidade: f.porcaoUnidade.trim() || 'porção',
        caloriasPorPorcao: num(f.caloriasPorPorcao),
        proteinasGPorPorcao: num(f.proteinasGPorPorcao),
        carboidratosGPorPorcao: num(f.carboidratosGPorPorcao),
        gordurasGPorPorcao: num(f.gordurasGPorPorcao),
        fibrasGPorPorcao: num(f.fibrasGPorPorcao),
        cafeinaMgPorPorcao: f.cafeinaMgPorPorcao?.trim() ? num(f.cafeinaMgPorPorcao) : null,
        composicao: f.composicao.trim() || null,
        instrucoesUso: f.instrucoesUso.trim() || null,
        observacoes: f.observacoes.trim() || null,
      };
      return novo ? api('/api/suplementos', { method: 'POST', json }) : api(`/api/suplementos/${a!.id}`, { method: 'PUT', json });
    },
    onSuccess: () => {
      toast(novo ? 'Suplemento cadastrado.' : 'Suplemento salvo.');
      invalidar();
      onClose();
    },
    onError: erroToast,
  });
  const status = useMutation({
    mutationFn: () => (a!.ativo ? api(`/api/suplementos/${a!.id}`, { method: 'DELETE' }) : api(`/api/suplementos/${a!.id}/reativar`, { method: 'POST' })),
    onSuccess: () => {
      toast(a!.ativo ? 'Suplemento inativado. Dietas existentes não mudam.' : 'Suplemento reativado.');
      invalidar();
      onClose();
    },
    onError: erroToast,
  });

  return (
    <Sheet
      open={!!item}
      onClose={onClose}
      title={novo ? 'Novo suplemento' : 'Suplemento'}
      footer={
        <Button size="lg" className="w-full" disabled={!valido} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome">
          <input className={inputCls} value={f.nome ?? ''} maxLength={160} placeholder="Ex.: Whey protein isolado" onChange={set('nome')} />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Marca">
            <input className={inputCls} value={f.marca ?? ''} maxLength={120} onChange={set('marca')} />
          </Field>
          <Field label="Forma">
            <input className={inputCls} value={f.forma ?? ''} maxLength={60} placeholder="Pó, cápsula…" onChange={set('forma')} />
          </Field>
        </div>
        <Field label="Categoria">
          <select className={inputCls} value={f.categoria ?? 'Outros'} onChange={set('categoria')}>
            {CATEGORIAS_SUPLEMENTO.map((c) => (
              <option key={c}>{c}</option>
            ))}
          </select>
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Porção">
            <input className={inputCls} inputMode="decimal" value={f.porcaoQuantidade ?? ''} onChange={set('porcaoQuantidade')} />
          </Field>
          <Field label="Unidade da porção">
            <input className={inputCls} value={f.porcaoUnidade ?? ''} maxLength={40} placeholder="scoop, g, cápsula" onChange={set('porcaoUnidade')} />
          </Field>
        </div>
        <p className="-mb-2 text-[13px] font-semibold text-muted">Valores por porção (do rótulo)</p>
        <div className="grid grid-cols-2 gap-3">
          {CAMPOS_SUPLEMENTO.map(([k, l]) => (
            <Field key={k} label={l}>
              <input className={inputCls} inputMode="decimal" value={f[k] ?? ''} placeholder={k === 'cafeinaMgPorPorcao' ? '—' : '0'} onChange={set(k)} />
            </Field>
          ))}
        </div>
        <Field label="Composição">
          <textarea className={inputCls + ' h-20 py-3'} value={f.composicao ?? ''} maxLength={1000} onChange={set('composicao')} />
        </Field>
        <Field label="Como usar">
          <textarea className={inputCls + ' h-20 py-3'} value={f.instrucoesUso ?? ''} maxLength={1000} placeholder="Ex.: diluir em 200 ml de água" onChange={set('instrucoesUso')} />
        </Field>
        <Field label="Observações">
          <textarea className={inputCls + ' h-20 py-3'} value={f.observacoes ?? ''} maxLength={1000} onChange={set('observacoes')} />
        </Field>
        <p className="text-[12px] text-faint">Dados de rótulo para organizar o cronograma. Dose e indicação continuam sendo decisão profissional.</p>
        {a && (
          <Button variant="ghost" className={a.ativo ? 'text-muted' : 'text-accent'} loading={status.isPending} onClick={() => status.mutate()}>
            {a.ativo ? 'Inativar suplemento' : 'Reativar suplemento'}
          </Button>
        )}
      </div>
    </Sheet>
  );
}
