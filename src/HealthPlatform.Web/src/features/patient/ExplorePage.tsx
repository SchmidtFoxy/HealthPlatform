import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ChevronDown, ChevronRight, Compass, Heart, Info } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, ErrorBox, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';

// Explorar: conteudo educativo de modalidades e contextos. A API devolve formatos diferentes por caminho,
// entao um renderizador generico mostra filtros, listas e notas a partir das chaves conhecidas.

type Json = string | number | boolean | null | Json[] | { [k: string]: Json };
type Obj = { [k: string]: Json };

interface Caminho {
  codigo: string;
  titulo: string;
  descricao: string;
  contextos: string[];
  modalidades: string[];
  tipo: string;
}
interface Interesse {
  codigo: string;
  nome: string;
  intencao: string;
}
interface Hub {
  planoAtual: string | null;
  caminhos: Caminho[];
  interessesDeclarados: Interesse[];
  regraDeAutonomia: string | null;
}

/** Codigo do caminho (ou da modalidade) -> endpoint de detalhe. */
const ENDPOINT: Record<string, string> = {
  'start-a-sport': 'start-a-sport',
  'home-movement': 'home-workout',
  'quick-movement': 'quick-movement',
  'travel-mode': 'travel-mode',
  outdoor: 'outdoor-mode',
  'learn-fundamentals': 'learn-fundamentals',
  'sports-starter-packs': 'starter-packs',
  'sports-expansion-i': 'sports-expansion-i',
  'sports-expansion-ii': 'sports-expansion-ii',
  'beginner-journeys': 'beginner-journeys',
  'football-futsal': 'football-futsal',
  'basketball-volleyball': 'basketball-volleyball',
  'tennis-beach-tennis': 'tennis-beach-tennis',
  swimming: 'swimming',
  triathlon: 'triathlon',
  'martial-arts': 'martial-arts',
  rowing: 'rowing',
  trekking: 'trekking',
  'recreational-sports': 'recreational-sports',
};

const TITULOS: Record<string, string> = {
  'beginner-journeys': 'Jornada iniciante',
  'football-futsal': 'Futebol e futsal',
  'basketball-volleyball': 'Basquete e vôlei',
  'tennis-beach-tennis': 'Tênis e beach tennis',
  swimming: 'Natação',
  triathlon: 'Triathlon',
  'martial-arts': 'Artes marciais',
  rowing: 'Remo',
  trekking: 'Trekking',
  'recreational-sports': 'Esportes recreativos',
};

/** Modalidade dos esportes de quadra/campo e da expansao II -> pagina de detalhe. */
const DETALHE_MODALIDADE: Record<string, string> = {
  futebol: 'football-futsal',
  futsal: 'football-futsal',
  basquete: 'basketball-volleyball',
  volei: 'basketball-volleyball',
  tenis: 'tennis-beach-tennis',
  'beach-tennis': 'tennis-beach-tennis',
  natacao: 'swimming',
  triathlon: 'triathlon',
  'artes-marciais': 'martial-arts',
  remo: 'rowing',
  trekking: 'trekking',
  recreativos: 'recreational-sports',
};

const FILTROS: Record<string, { param: string; rotulo: string }> = {
  espacosDisponiveis: { param: 'espaco', rotulo: 'Espaço' },
  recursosDisponiveis: { param: 'recurso', rotulo: 'Recurso' },
  preferenciasDisponiveis: { param: 'preferencia', rotulo: 'Foco' },
  janelasDisponiveis: { param: 'janela', rotulo: 'Tempo' },
  contextosDisponiveis: { param: 'contexto', rotulo: 'Situação' },
  hospedagensDisponiveis: { param: 'hospedagem', rotulo: 'Hospedagem' },
  rotinasDisponiveis: { param: 'rotina', rotulo: 'Rotina' },
  ambientesDisponiveis: { param: 'ambiente', rotulo: 'Ambiente' },
  interessesDisponiveis: { param: 'interesse', rotulo: 'Interesse' },
  modalidadesDisponiveis: { param: 'modalidade', rotulo: 'Modalidade' },
  capacidadesDisponiveis: { param: 'capacidade', rotulo: 'Capacidade' },
};

const SECOES: Record<string, string> = {
  modalidades: 'Modalidades',
  movimentos: 'Movimentos que cabem aqui',
  possibilidades: 'Possibilidades',
  fundamentos: 'Fundamentos',
  jornadas: 'Jornadas',
  packs: 'Starter packs',
  estilos: 'Estilos',
  disciplinas: 'Disciplinas',
  transicoes: 'Transições',
  fases: 'Fases do movimento',
  contextos: 'Contextos',
  capacidades: 'Capacidades',
  sessoesReferencia: 'Sessões de referência',
  etapas: 'Etapas',
  objetivos: 'Objetivos',
  sessoes: 'Sessões',
};

const TEXTOS: [string, string | null][] = [
  ['descricao', null],
  ['caracteristica', null],
  ['contexto', null],
  ['explicacao', null],
  ['dinamica', 'Dinâmica'],
  ['objetivoEducacional', 'Objetivo'],
  ['focoTecnico', 'Foco técnico'],
  ['oQueObservar', 'O que observar'],
  ['erroComum', 'Erro comum'],
  ['oQueOrganizar', 'O que organizar'],
  ['diferencaChave', 'Diferença-chave'],
  ['diferencaDaOutraModalidade', 'Comparando'],
  ['exemplo', 'Exemplo'],
  ['primeiroMarco', 'Primeiro marco'],
  ['evidenciaDeFamiliaridade', 'Sinal de familiaridade'],
  ['proximoQuando', 'Próximo passo quando'],
  ['proximoPasso', 'Próximo passo'],
  ['regraDeProgressao', 'Progressão'],
  ['regraDeUso', null],
  ['estadoEditorial', null],
  ['motivoDaCompatibilidade', 'Por que aparece'],
];
const CHIPS: Record<string, string> = { ambientes: 'Ambientes', recursos: 'Recursos', capacidades: 'Capacidades', contextos: 'Contextos', modalidades: 'Modalidades' };
const NOTAS = ['mensagemDeContexto', 'cadeiaEstrutural', 'regraDeAutonomia', 'regraGeral', 'regraDeUso'];

const isObj = (v: Json): v is Obj => !!v && typeof v === 'object' && !Array.isArray(v);
const tituloDe = (o: Obj) => String(o.nome ?? o.titulo ?? o.modalidadeNome ?? o.codigo ?? '');

const useHub = () => useQuery({ queryKey: ['p', 'explore'], queryFn: () => api<Hub>('/api/portal/me/explore') });

export function ExplorePage() {
  const q = useHub();
  const [editar, setEditar] = useState(false);
  const hub = q.data;
  return (
    <Page>
      <PageHeader back="/perfil" eyebrow="Explorar" title="Novos caminhos" subtitle="Conheça modalidades e jeitos de se mover. Complementa o seu plano, não substitui." />
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && <Skeleton className="h-64" />}
      {hub && (
        <>
          <Card className="mb-5 p-4">
            <div className="flex items-center justify-between gap-3">
              <p className="flex items-center gap-2 font-semibold">
                <Heart className="h-4 w-4 text-accent" /> Seus interesses
              </p>
              <Button size="sm" variant="soft" onClick={() => setEditar(true)}>
                {hub.interessesDeclarados.length ? 'Editar' : 'Escolher'}
              </Button>
            </div>
            {hub.interessesDeclarados.length ? (
              <div className="mt-3 flex flex-wrap gap-1.5">
                {hub.interessesDeclarados.map((i) => (
                  <span key={i.codigo} className="rounded-full bg-accent/10 px-3 py-1 text-[13px] font-semibold text-accent">
                    {i.nome} · {INTENCOES[i.intencao] ?? i.intencao}
                  </span>
                ))}
              </div>
            ) : (
              <p className="mt-1.5 text-sm text-muted">Conte o que você tem vontade de experimentar. Seu profissional vê.</p>
            )}
          </Card>

          <div className="flex flex-col gap-2.5">
            {hub.caminhos.map((c) => (
              <Link key={c.codigo} to={`/explorar/${c.codigo}`} className="block">
                <Card className="flex items-center gap-3 p-4 transition active:scale-[0.99]">
                  <span className="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-accent/10 text-accent">
                    <Compass className="h-5 w-5" />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block font-semibold">{c.titulo}</span>
                    <span className="block text-[13px] leading-snug text-muted">{c.descricao}</span>
                    <span className="mt-1.5 block truncate text-[12px] font-semibold text-faint">{c.modalidades.join(' · ')}</span>
                  </span>
                  <ChevronRight className="h-5 w-5 shrink-0 text-faint" />
                </Card>
              </Link>
            ))}
          </div>
          {hub.regraDeAutonomia && <Nota texto={hub.regraDeAutonomia} />}
          <InteressesSheet open={editar} onClose={() => setEditar(false)} atuais={hub.interessesDeclarados} />
        </>
      )}
    </Page>
  );
}

const INTENCOES: Record<string, string> = { QueroExperimentar: 'Quero experimentar', QueroRetomar: 'Quero retomar', TenhoCuriosidade: 'Tenho curiosidade' };

function InteressesSheet({ open, onClose, atuais }: { open: boolean; onClose: () => void; atuais: Interesse[] }) {
  const qc = useQueryClient();
  const opcoes = useQuery({
    queryKey: ['p', 'explore-interesses'],
    queryFn: () => api<{ modalidadesDisponiveis: { codigo: string; nome: string }[] }>('/api/portal/me/explore/interesses'),
    enabled: open,
  });
  const [sel, setSel] = useState<Record<string, string>>({});
  useEffect(() => {
    if (open) setSel(Object.fromEntries(atuais.map((i) => [i.codigo, i.intencao])));
  }, [open, atuais]);

  const salvar = useMutation({
    mutationFn: () => api('/api/portal/me/explore/interesses', { method: 'PUT', json: { interesses: Object.entries(sel).map(([codigo, intencao]) => ({ codigo, intencao })) } }),
    onSuccess: () => {
      toast('Interesses salvos.');
      qc.invalidateQueries({ queryKey: ['p', 'explore'] });
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });

  const total = Object.keys(sel).length;
  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Seus interesses"
      tall
      footer={
        <Button size="lg" className="w-full" loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar {total ? `(${total})` : ''}
        </Button>
      }
    >
      <p className="mb-4 text-[15px] text-muted">Toque numa modalidade para marcar e de novo para trocar a intenção. Até 12.</p>
      {opcoes.isLoading && <Skeleton className="h-48" />}
      <div className="flex flex-col gap-1.5">
        {opcoes.data?.modalidadesDisponiveis.map((m) => {
          const atual = sel[m.codigo];
          const proxima = () =>
            setSel((s) => {
              const n = { ...s };
              const ordem = Object.keys(INTENCOES);
              const i = atual ? ordem.indexOf(atual) + 1 : 0;
              if (i >= ordem.length) delete n[m.codigo];
              else if (!atual && total >= 12) toast('Máximo de 12 interesses.', 'bad');
              else n[m.codigo] = ordem[i];
              return n;
            });
          return (
            <button
              key={m.codigo}
              onClick={proxima}
              className={clsx('flex min-h-12 items-center justify-between rounded-2xl px-4 text-left transition', atual ? 'bg-accent/10 ring-1 ring-accent/40' : 'bg-raised/60')}
            >
              <span className="font-semibold">{m.nome}</span>
              <span className={clsx('text-[13px] font-semibold', atual ? 'text-accent' : 'text-faint')}>{atual ? INTENCOES[atual] : 'Marcar'}</span>
            </button>
          );
        })}
      </div>
    </Sheet>
  );
}

export function ExploreDetailPage() {
  const { codigo = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const endpoint = ENDPOINT[codigo];
  const qs = params.toString();
  const hub = useHub();
  const q = useQuery({
    queryKey: ['p', 'explore', endpoint, qs],
    queryFn: () => api<Obj>(`/api/portal/me/explore/${endpoint}${qs ? `?${qs}` : ''}`),
    enabled: !!endpoint,
    placeholderData: (p) => p,
  });
  const caminho = hub.data?.caminhos.find((c) => c.codigo === codigo);
  const d = q.data;

  const filtros = d ? Object.entries(FILTROS).filter(([k]) => Array.isArray(d[k]) && (d[k] as Json[]).every((x) => typeof x === 'string')) : [];
  const secoes = d ? Object.entries(d).filter(([k, v]) => SECOES[k] && Array.isArray(v) && v.length && v.every(isObj)) : [];

  return (
    <Page>
      <PageHeader back="/explorar" eyebrow="Explorar" title={caminho?.titulo ?? TITULOS[codigo] ?? 'Explorar'} subtitle={caminho?.descricao} />
      {!endpoint && <p className="text-muted">Caminho não encontrado.</p>}
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && <Skeleton className="h-64" />}
      {d && (
        <div className={clsx('transition-opacity', q.isFetching && 'opacity-60')}>
          {filtros.map(([k, f]) => {
            const atual = (d[f.param] as string | null) ?? params.get(f.param);
            return (
              <div key={k} className="mb-3">
                <p className="eyebrow mb-1.5">{f.rotulo}</p>
                <div className="-mx-gutter flex gap-1.5 overflow-x-auto px-gutter no-scrollbar">
                  {(d[k] as string[]).map((op) => (
                    <button
                      key={op}
                      onClick={() => {
                        const n = new URLSearchParams(params);
                        n.set(f.param, op);
                        setParams(n, { replace: true });
                      }}
                      className={clsx('h-9 shrink-0 rounded-full px-3.5 text-sm font-semibold transition', atual === op ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}
                    >
                      {op}
                    </button>
                  ))}
                </div>
              </div>
            );
          })}
          {typeof d.mensagemDeContexto === 'string' && <Nota texto={d.mensagemDeContexto} destaque />}
          {secoes.map(([k, v]) => (
            <section key={k} className="mt-6">
              <h2 className="mb-2.5 font-display text-[17px] font-semibold tracking-tight">{SECOES[k]}</h2>
              <div className="flex flex-col gap-2">
                {(v as Obj[]).map((o, i) => (
                  <Item key={String(o.codigo ?? o.id ?? i)} o={o} endpoint={endpoint} aberto={(v as Obj[]).length === 1} />
                ))}
              </div>
            </section>
          ))}
          {NOTAS.filter((k) => k !== 'mensagemDeContexto' && typeof d[k] === 'string').map((k) => (
            <Nota key={k} texto={d[k] as string} />
          ))}
        </div>
      )}
    </Page>
  );
}

function Item({ o, endpoint, aberto }: { o: Obj; endpoint: string; aberto?: boolean }) {
  const [open, setOpen] = useState(!!aberto);
  const titulo = tituloDe(o);
  const sub = [o.grupoMuscular, o.equipamento, o.ambientePrincipal].filter((x) => typeof x === 'string').join(' · ');
  const textos = TEXTOS.filter(([k]) => typeof o[k] === 'string' && o[k]);
  const resumo = textos[0];
  const resto = textos.slice(1);
  const chips = Object.entries(CHIPS).filter(([k]) => Array.isArray(o[k]) && (o[k] as Json[]).length && (o[k] as Json[]).every((x) => typeof x === 'string'));
  const filhos = Object.entries(o).filter(([k, v]) => SECOES[k] && Array.isArray(v) && v.length && v.every(isObj));
  const temMais = !!(resto.length || chips.length || filhos.length);
  const detalhe = typeof o.codigo === 'string' ? DETALHE_MODALIDADE[o.codigo] : undefined;
  const linkDetalhe = (endpoint === 'sports-expansion-i' || endpoint === 'sports-expansion-ii') && detalhe;
  const linkJornada = endpoint === 'start-a-sport' && typeof o.codigo === 'string';

  return (
    <Card className="p-4">
      <button className="flex w-full items-start gap-3 text-left" onClick={() => temMais && setOpen(!open)} aria-expanded={open}>
        <span className="min-w-0 flex-1">
          <span className="flex items-baseline gap-2">
            {typeof o.ordem === 'number' && <span className="num text-[13px] font-semibold text-accent">{o.ordem}.</span>}
            <span className="font-semibold">{titulo}</span>
          </span>
          {sub && <span className="block text-[12px] font-semibold uppercase tracking-wide text-faint">{sub}</span>}
          {resumo && <span className="mt-1 block text-[14px] leading-snug text-muted">{String(o[resumo[0]])}</span>}
        </span>
        {temMais && <ChevronDown className={clsx('mt-0.5 h-5 w-5 shrink-0 text-faint transition', open && 'rotate-180')} />}
      </button>
      {open && (
        <div className="mt-3 flex flex-col gap-2.5 border-t border-line/60 pt-3">
          {resto.map(([k, rotulo]) => (
            <p key={k} className="text-[14px] leading-snug">
              {rotulo && <b className="font-semibold">{rotulo}: </b>}
              <span className="text-muted">{String(o[k])}</span>
            </p>
          ))}
          {chips.map(([k, rotulo]) => (
            <div key={k}>
              <p className="mb-1 text-[12px] font-semibold uppercase tracking-wide text-faint">{rotulo}</p>
              <div className="flex flex-wrap gap-1.5">
                {(o[k] as string[]).map((c) => (
                  <span key={c} className="rounded-full bg-raised px-2.5 py-1 text-[13px]">
                    {c}
                  </span>
                ))}
              </div>
            </div>
          ))}
          {filhos.map(([k, v]) => (
            <div key={k}>
              <p className="mb-1 text-[12px] font-semibold uppercase tracking-wide text-faint">{SECOES[k]}</p>
              <ul className="flex flex-col gap-1.5">
                {(v as Obj[]).map((f, i) => {
                  const t = TEXTOS.find(([tk]) => typeof f[tk] === 'string' && f[tk]);
                  const netos = Object.entries(f).filter(([nk, nv]) => SECOES[nk] && Array.isArray(nv) && nv.length && nv.every(isObj));
                  return (
                    <li key={String(f.codigo ?? i)} className="rounded-xl bg-raised/50 px-3 py-2 text-[14px]">
                      <span className="font-semibold">
                        {typeof f.ordem === 'number' ? `${f.ordem}. ` : ''}
                        {tituloDe(f)}
                      </span>
                      {t && <span className="block text-[13px] leading-snug text-muted">{String(f[t[0]])}</span>}
                      {netos.map(([nk, nv]) => (
                        <span key={nk} className="block text-[13px] text-muted">
                          {(nv as Obj[]).map(tituloDe).join(' · ')}
                        </span>
                      ))}
                    </li>
                  );
                })}
              </ul>
            </div>
          ))}
        </div>
      )}
      {(linkDetalhe || linkJornada) && (
        <Link
          to={linkDetalhe ? `/explorar/${detalhe}` : `/explorar/beginner-journeys?modalidade=${encodeURIComponent(String(o.codigo))}`}
          className="mt-3 flex items-center justify-between rounded-xl bg-accent/10 px-3 py-2.5 text-sm font-semibold text-accent"
        >
          {linkDetalhe ? 'Ver fundamentos e capacidades' : 'Ver jornada iniciante'}
          <ChevronRight className="h-4 w-4" />
        </Link>
      )}
    </Card>
  );
}

function Nota({ texto, destaque }: { texto: string; destaque?: boolean }) {
  return (
    <p className={clsx('mt-5 flex gap-2 rounded-2xl p-3 text-[13px] leading-snug', destaque ? 'bg-accent/10 text-fg' : 'bg-raised/50 text-muted')}>
      <Info className="mt-0.5 h-4 w-4 shrink-0" />
      <span>{texto}</span>
    </p>
  );
}
