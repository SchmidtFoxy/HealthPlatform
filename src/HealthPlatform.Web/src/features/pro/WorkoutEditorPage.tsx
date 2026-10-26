import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { AnimatePresence, motion } from 'framer-motion';
import { ArrowDown, ArrowUp, BarChart3, ChevronLeft, Copy, Plus, Replace, Search, Timer, Trash2, X } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button, Card, ErrorBox, Field, IconButton, inputCls, Pill, Sheet, Skeleton, Stepper, toast } from '@/components/ui';
import { api, ApiError } from '@/lib/api';
import { hojeIso, n0 } from '@/lib/format';
import { aplicarSeries, obsLimpa, SerieAlvo, seriesDoItem } from '@/lib/series';
import type { Exercicio, PlanoTreinoPro } from '@/lib/types';
import { useExercicios, usePaciente, usePlanosTreino } from './queries';

// ------------------------------------------------------------------ Modelo local

interface Item {
  k: string;
  id?: string;
  exercicioId: string;
  series: number;
  repeticoes: string;
  carga: number | null;
  unidadeCarga: string | null;
  descansoSegundos: number | null;
  tempoSegundos: number | null;
  rirAlvo: number | null;
  cadencia: string | null;
  tecnicaAvancada: string | null;
  tecnicaAvancadaCodigo: string | null;
  tecnicaAvancadaParametros: string | null;
  observacoes: string | null;
}
interface Sessao {
  k: string;
  id?: string;
  nome: string;
  dias: number[];
  observacoes: string | null;
  itens: Item[];
}
interface Rascunho {
  nome: string;
  objetivo: string | null;
  dataInicio: string;
  dataFim: string | null;
  status: string;
  observacoes: string | null;
  sessoes: Sessao[];
}

const uid = () => Math.random().toString(36).slice(2, 10);
const DIAS = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];
const TOKENS = [['seg'], ['ter'], ['qua'], ['qui'], ['sex'], ['sab', 'sáb'], ['dom']];

const lerDias = (s: string | null) => {
  const t = (s ?? '').toLowerCase();
  return TOKENS.map((tk, i) => (tk.some((x) => t.includes(x)) ? i : -1)).filter((i) => i >= 0);
};
const escreverDias = (d: number[]) => [...d].sort().map((i) => DIAS[i]).join(', ') || null;
const letra = (i: number) => String.fromCharCode(65 + i);

function deServidor(p: PlanoTreinoPro): Rascunho {
  return {
    nome: p.nome,
    objetivo: p.objetivo,
    dataInicio: p.dataInicio,
    dataFim: p.dataFim,
    status: p.status,
    observacoes: p.observacoes,
    sessoes: [...p.sessoes]
      .sort((a, b) => a.ordem - b.ordem)
      .map((s) => ({
        k: uid(),
        id: s.id,
        nome: s.nome,
        dias: lerDias(s.diasSemana),
        observacoes: s.observacoes,
        itens: [...s.itens]
          .sort((a, b) => a.ordem - b.ordem)
          .map((i) => ({
            k: uid(),
            id: i.id,
            exercicioId: i.exercicioId,
            series: i.series,
            repeticoes: i.repeticoes,
            carga: i.carga,
            unidadeCarga: i.unidadeCarga,
            descansoSegundos: i.descansoSegundos,
            tempoSegundos: i.tempoSegundos,
            rirAlvo: i.rirAlvo,
            cadencia: i.cadencia,
            tecnicaAvancada: i.tecnicaAvancada,
            tecnicaAvancadaCodigo: i.tecnicaAvancadaCodigo,
            tecnicaAvancadaParametros: i.tecnicaAvancadaParametros,
            observacoes: i.observacoes,
          })),
      })),
  };
}

function novo(): Rascunho {
  return {
    nome: 'Plano de treino',
    objetivo: null,
    dataInicio: hojeIso(),
    dataFim: null,
    status: 'Inativo',
    observacoes: null,
    sessoes: [
      { k: uid(), nome: 'A', dias: [0, 3], observacoes: null, itens: [] },
      { k: uid(), nome: 'B', dias: [1, 4], observacoes: null, itens: [] },
    ],
  };
}

function paraServidor(d: Rascunho, status: string, comIds: boolean) {
  return {
    nome: d.nome.trim() || 'Plano de treino',
    objetivo: d.objetivo,
    dataInicio: d.dataInicio,
    dataFim: d.dataFim,
    status,
    observacoes: d.observacoes,
    sessoes: d.sessoes.map((s, si) => ({
      id: comIds ? s.id ?? null : null,
      nome: s.nome.trim() || `Treino ${letra(si)}`,
      diasSemana: escreverDias(s.dias),
      ordem: si + 1,
      observacoes: s.observacoes,
      itens: s.itens.map((i, ii) => {
        const { k: _k, id, ...resto } = i;
        return { ...resto, id: comIds ? id ?? null : null, ordem: ii + 1, repeticoes: i.repeticoes.trim() || '10' };
      }),
    })),
  };
}

// ------------------------------------------------------------------ Seletor de exercicio

function SeletorExercicio({ open, onClose, onPick, titulo, grupoInicial }: { open: boolean; onClose: () => void; onPick: (e: Exercicio) => void; titulo: string; grupoInicial?: string | null }) {
  const ex = useExercicios();
  const [busca, setBusca] = useState('');
  const [grupo, setGrupo] = useState<string | null>(null);
  useEffect(() => {
    if (open) {
      setBusca('');
      setGrupo(grupoInicial ?? null);
    }
  }, [open, grupoInicial]);
  const ativos = (ex.data ?? []).filter((e) => e.ativo);
  const grupos = useMemo(() => [...new Set(ativos.map((e) => e.grupoMuscular).filter(Boolean) as string[])].sort(), [ativos]);
  const norm = (s: string) => s.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();
  const termo = norm(busca);
  const lista = ativos.filter((e) => (!grupo || e.grupoMuscular === grupo) && (!termo || norm(e.nome).includes(termo) || norm(e.equipamento ?? '').includes(termo)));
  return (
    <Sheet open={open} onClose={onClose} title={titulo} tall>
      <label className="relative mb-3 block">
        <Search className="pointer-events-none absolute left-3.5 top-3 h-5 w-5 text-faint" />
        <input id="busca-exercicio" type="search" placeholder="Buscar exercício ou equipamento" value={busca} onChange={(e) => setBusca(e.target.value)} className={inputCls + ' pl-11'} />
      </label>
      <div className="no-scrollbar -mx-5 mb-3 flex gap-2 overflow-x-auto px-5">
        {[null, ...grupos].map((g) => (
          <button key={g ?? 'todos'} onClick={() => setGrupo(g)} className={clsx('h-8 shrink-0 rounded-full border px-3 text-[13px] font-semibold', grupo === g ? 'border-accent bg-accent text-accent-ink' : 'border-line text-muted')}>
            {g ?? 'Todos'}
          </button>
        ))}
      </div>
      {ex.isLoading && <Skeleton className="h-60" />}
      <div className="flex flex-col">
        {lista.map((e) => (
          <button key={e.id} onClick={() => onPick(e)} className="flex items-center gap-3 rounded-xl px-2 py-3 text-left transition hover:bg-raised active:bg-raised">
            <div className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-accent/10 font-display text-xs font-semibold text-accent">{(e.grupoMuscular ?? '?').slice(0, 3)}</div>
            <div className="min-w-0 flex-1">
              <p className="truncate font-semibold">{e.nome}</p>
              <p className="truncate text-[12px] text-muted">{[e.grupoMuscular, e.equipamento].filter(Boolean).join(' · ')}</p>
            </div>
          </button>
        ))}
        {!ex.isLoading && lista.length === 0 && <p className="py-10 text-center text-sm text-muted">Nenhum exercício encontrado.</p>}
      </div>
    </Sheet>
  );
}

// ------------------------------------------------------------------ Item

interface Tecnica {
  codigo: string;
  nome: string;
}

// Texto local para aceitar "27," ou "27." enquanto digita; o numero vai para o item a cada tecla valida.
function CargaInput({ label, value, onChange }: { label: string; value: number | null; onChange: (v: number | null) => void }) {
  const [txt, setTxt] = useState(value == null ? '' : String(value).replace('.', ','));
  useEffect(() => {
    const atual = txt.trim() === '' ? null : Number(txt.replace(',', '.'));
    if (atual !== value) setTxt(value == null ? '' : String(value).replace('.', ','));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value]);
  return (
    <input
      aria-label={label}
      inputMode="decimal"
      className={inputCls + ' num h-10 text-center font-semibold'}
      value={txt}
      placeholder="—"
      onChange={(e) => {
        const t = e.target.value.replace(/[^\d.,]/g, '');
        setTxt(t);
        const n = Number(t.replace(',', '.'));
        if (t === '') onChange(null);
        else if (!Number.isNaN(n)) onChange(n);
      }}
    />
  );
}

function SeriesEditor({ item, onChange }: { item: Item; onChange: (i: Item) => void }) {
  const lista = seriesDoItem(item);
  const unidade = item.unidadeCarga ?? 'kg';
  const salvar = (nova: SerieAlvo[]) => onChange(aplicarSeries({ ...item, unidadeCarga: unidade }, nova));
  const mudar = (idx: number, s: Partial<SerieAlvo>) => salvar(lista.map((x, i) => (i === idx ? { ...x, ...s } : x)));
  const variando = lista.some((s) => s.reps !== lista[0].reps || s.carga !== lista[0].carga);
  return (
    <section>
      <div className="mb-2 flex items-end justify-between gap-3">
        <div>
          <p className="text-[13px] font-semibold">
            {lista.length} {lista.length === 1 ? 'série' : 'séries'}
          </p>
          <p className="text-[12px] text-muted">Cada linha é uma série. Mude reps e carga de uma só para montar pirâmide ou top set.</p>
        </div>
        {variando && (
          <button onClick={() => salvar(lista.map(() => ({ ...lista[0] })))} className="shrink-0 text-[13px] font-semibold text-accent">
            Igualar à 1ª
          </button>
        )}
      </div>
      <div className="grid grid-cols-[28px_minmax(0,1fr)_minmax(0,1fr)_36px] items-center gap-x-2 px-1 pb-1 text-[11px] font-semibold uppercase tracking-wider text-faint">
        <span />
        <span className="text-center">Reps</span>
        <span className="text-center">Carga ({unidade})</span>
        <span />
      </div>
      <ol className="flex flex-col gap-1.5">
        {lista.map((s, i) => (
          <li key={i} className="grid grid-cols-[28px_minmax(0,1fr)_minmax(0,1fr)_36px] items-center gap-x-2 rounded-2xl bg-raised/50 p-1.5">
            <span className="num grid h-7 w-7 place-items-center rounded-full bg-accent/15 text-[13px] font-semibold text-accent">{i + 1}</span>
            <input aria-label={`Repetições da série ${i + 1}`} className={inputCls + ' num h-10 text-center font-semibold'} value={s.reps} placeholder="10" onChange={(e) => mudar(i, { reps: e.target.value })} />
            <CargaInput label={`Carga da série ${i + 1}`} value={s.carga} onChange={(carga) => mudar(i, { carga })} />
            <button
              aria-label={`Remover série ${i + 1}`}
              disabled={lista.length <= 1}
              onClick={() => salvar(lista.filter((_, j) => j !== i))}
              className="grid h-9 w-9 place-items-center rounded-full text-muted transition hover:bg-bad/10 hover:text-bad disabled:opacity-30"
            >
              <X className="h-4 w-4" />
            </button>
          </li>
        ))}
      </ol>
      <button
        onClick={() => lista.length < 20 && salvar([...lista, { ...lista[lista.length - 1] }])}
        className="mt-2 flex h-11 w-full items-center justify-center gap-1.5 rounded-2xl border border-dashed border-line text-sm font-semibold text-muted transition hover:border-accent/50 hover:text-accent"
      >
        <Plus className="h-4 w-4" /> Adicionar série
      </button>
    </section>
  );
}

function ItemSheet({
  item,
  exercicio,
  tecnicas,
  pos,
  onClose,
  onChange,
  onRemove,
  onMove,
  onSwap,
}: {
  item: Item | null;
  exercicio?: Exercicio;
  tecnicas: Tecnica[];
  pos: { i: number; n: number };
  onClose: () => void;
  onChange: (i: Item) => void;
  onRemove: () => void;
  onMove: (d: number) => void;
  onSwap: () => void;
}) {
  return (
    <Sheet
      open={!!item}
      onClose={onClose}
      title={exercicio?.nome ?? 'Exercício'}
      footer={
        <div className="flex gap-2">
          <Button variant="danger" icon={<Trash2 className="h-4 w-4" />} onClick={onRemove}>
            Remover
          </Button>
          <Button className="flex-1" onClick={onClose}>
            Pronto
          </Button>
        </div>
      }
    >
      {item && (
        <div className="flex flex-col gap-5 pt-1">
          <div className="flex items-center gap-2">
            {exercicio?.grupoMuscular && <Pill tone="accent">{exercicio.grupoMuscular}</Pill>}
            {exercicio?.equipamento && <Pill>{exercicio.equipamento}</Pill>}
            <div className="ml-auto flex">
              <IconButton label="Trocar exercício" onClick={onSwap}>
                <Replace className="h-4 w-4" />
              </IconButton>
              <IconButton label="Subir" disabled={pos.i === 0} onClick={() => onMove(-1)} className="disabled:opacity-30">
                <ArrowUp className="h-4 w-4" />
              </IconButton>
              <IconButton label="Descer" disabled={pos.i === pos.n - 1} onClick={() => onMove(1)} className="disabled:opacity-30">
                <ArrowDown className="h-4 w-4" />
              </IconButton>
            </div>
          </div>

          <SeriesEditor item={item} onChange={onChange} />

          <div className="grid grid-cols-2 gap-x-3 gap-y-4">
            <Stepper label="Descanso" value={item.descansoSegundos} step={15} max={600} suffix="s" onChange={(v) => onChange({ ...item, descansoSegundos: v || null })} />
            <Stepper label="RIR alvo" value={item.rirAlvo} max={6} onChange={(v) => onChange({ ...item, rirAlvo: v })} />
            <div className="flex min-w-0 flex-col items-center gap-1">
              <span className="eyebrow">Cadência</span>
              <input aria-label="Cadência" className={inputCls + ' num h-11 rounded-full text-center'} value={item.cadencia ?? ''} onChange={(e) => onChange({ ...item, cadencia: e.target.value || null })} placeholder="2-0-1-0" />
            </div>
          </div>

          {tecnicas.length > 0 && (
            <div>
              <p className="mb-2 text-[13px] font-semibold text-muted">Técnica avançada</p>
              <div className="flex flex-wrap gap-2">
                {[{ codigo: '', nome: 'Nenhuma' }, ...tecnicas].map((t) => {
                  const sel = (item.tecnicaAvancadaCodigo ?? '') === t.codigo;
                  return (
                    <button
                      key={t.codigo || 'nenhuma'}
                      onClick={() => onChange({ ...item, tecnicaAvancadaCodigo: t.codigo || null, tecnicaAvancada: t.codigo ? t.nome : null, tecnicaAvancadaParametros: t.codigo ? item.tecnicaAvancadaParametros : null })}
                      className={clsx('h-8 rounded-full border px-3 text-[13px] font-semibold', sel ? 'border-accent bg-accent text-accent-ink' : 'border-line text-muted')}
                    >
                      {t.nome}
                    </button>
                  );
                })}
              </div>
              {item.tecnicaAvancadaCodigo && (
                <input className={inputCls + ' mt-2'} placeholder="Como aplicar (ex.: 2 drops de 20% na última série)" value={item.tecnicaAvancadaParametros ?? ''} onChange={(e) => onChange({ ...item, tecnicaAvancadaParametros: e.target.value || null })} />
              )}
            </div>
          )}

          <Field label="Orientação para o paciente">
            <textarea
              className={inputCls + ' h-20 py-3'}
              placeholder="Amplitude, postura, cuidados…"
              value={obsLimpa(item.observacoes) ?? ''}
              onChange={(e) => onChange(aplicarSeries({ ...item, observacoes: e.target.value || null }, seriesDoItem(item)))}
            />
          </Field>
        </div>
      )}
    </Sheet>
  );
}

// ------------------------------------------------------------------ Volume

function VolumeSheet({ open, onClose, sessoes, mapa }: { open: boolean; onClose: () => void; sessoes: Sessao[]; mapa: Map<string, Exercicio> }) {
  const porGrupo = new Map<string, number>();
  sessoes.forEach((s) => {
    const vezes = Math.max(1, s.dias.length);
    s.itens.forEach((i) => {
      const g = mapa.get(i.exercicioId)?.grupoMuscular ?? 'Outros';
      porGrupo.set(g, (porGrupo.get(g) ?? 0) + i.series * vezes);
    });
  });
  const linhas = [...porGrupo.entries()].sort((a, b) => b[1] - a[1]);
  const max = Math.max(22, ...linhas.map((l) => l[1]));
  return (
    <Sheet open={open} onClose={onClose} title="Volume semanal">
      <p className="mb-4 text-[13px] text-muted">Séries por grupo muscular na semana, considerando os dias marcados em cada sessão. A faixa destacada vai de 10 a 20 séries.</p>
      {linhas.length === 0 && <p className="py-6 text-center text-sm text-muted">Adicione exercícios para ver o volume.</p>}
      <div className="flex flex-col gap-3">
        {linhas.map(([g, v]) => (
          <div key={g}>
            <div className="mb-1 flex justify-between text-[13px]">
              <span className="font-semibold">{g}</span>
              <span className={clsx('num font-semibold', v < 10 ? 'text-muted' : v > 20 ? 'text-warn' : 'text-ok')}>{v} séries</span>
            </div>
            <div className="relative h-2.5 overflow-hidden rounded-full bg-line/50">
              <div className="absolute inset-y-0 bg-ok/15" style={{ left: `${(10 / max) * 100}%`, width: `${(10 / max) * 100}%` }} />
              <motion.div className={clsx('relative h-full rounded-full', v > 20 ? 'bg-warn' : 'bg-accent')} initial={{ width: 0 }} animate={{ width: `${(v / max) * 100}%` }} />
            </div>
          </div>
        ))}
      </div>
    </Sheet>
  );
}

// ------------------------------------------------------------------ Pagina

export function WorkoutEditorPage() {
  const { id = '', planoId = 'novo' } = useParams();
  const ehNovo = planoId === 'novo';
  const nav = useNavigate();
  const qc = useQueryClient();
  const paciente = usePaciente(id);
  const exercicios = useExercicios();
  const outros = usePlanosTreino(id);
  const tecnicas = useQuery({ queryKey: ['pro', 'tecnicas'], queryFn: () => api<{ itens: Tecnica[] }>('/api/treinos/tecnicas-avancadas'), staleTime: Infinity });
  const plano = useQuery({ queryKey: ['pro', 'treino', planoId], queryFn: () => api<PlanoTreinoPro>(`/api/treinos/${planoId}`), enabled: !ehNovo });
  const [d, setD] = useState<Rascunho | null>(ehNovo ? novo() : null);
  const [ativa, setAtiva] = useState(0);
  const [sujo, setSujo] = useState(false);
  const [aberto, setAberto] = useState<string | null>(null);
  const [picker, setPicker] = useState<'add' | 'swap' | null>(null);
  const [volume, setVolume] = useState(false);
  const [conflito, setConflito] = useState<string | null>(null);

  useEffect(() => {
    if (plano.data && !d) setD(deServidor(plano.data));
  }, [plano.data, d]);

  useEffect(() => {
    if (!sujo) return;
    const h = (e: BeforeUnloadEvent) => e.preventDefault();
    window.addEventListener('beforeunload', h);
    return () => window.removeEventListener('beforeunload', h);
  }, [sujo]);

  const mapa = useMemo(() => new Map((exercicios.data ?? []).map((e) => [e.id, e])), [exercicios.data]);
  const up = (fn: (x: Rascunho) => Rascunho) => {
    setD((x) => (x ? fn(x) : x));
    setSujo(true);
  };
  const sessao = d?.sessoes[Math.min(ativa, (d?.sessoes.length ?? 1) - 1)];
  const upSessao = (fn: (s: Sessao) => Sessao) => sessao && up((x) => ({ ...x, sessoes: x.sessoes.map((s) => (s.k === sessao.k ? fn(s) : s)) }));
  const upItem = (it: Item) => upSessao((s) => ({ ...s, itens: s.itens.map((i) => (i.k === it.k ? it : i)) }));

  const desativarOutros = async (manter: string) => {
    const ativos = (outros.data ?? []).filter((p) => p.status === 'Ativo' && p.id !== manter);
    await Promise.all(ativos.map((p) => api(`/api/treinos/${p.id}/status/Inativo`, { method: 'POST' })));
  };

  const salvar = useMutation({
    mutationFn: async ({ liberar, novaVersao }: { liberar: boolean; novaVersao?: boolean }) => {
      if (!d) throw new Error('Nada para salvar');
      const status = liberar || novaVersao ? 'Ativo' : ehNovo ? 'Inativo' : d.status;
      if (ehNovo || novaVersao) {
        const criado = await api<PlanoTreinoPro>(`/api/pacientes/${id}/treinos`, { method: 'POST', json: paraServidor(d, status, false) });
        if (novaVersao) await api(`/api/treinos/${planoId}/status/Concluido`, { method: 'POST' });
        if (status === 'Ativo') await desativarOutros(criado.id);
        return criado;
      }
      const salvo = await api<PlanoTreinoPro>(`/api/treinos/${planoId}`, { method: 'PUT', json: paraServidor(d, status, true) });
      if (liberar) {
        if (salvo.status !== 'Ativo') await api(`/api/treinos/${planoId}/status/Ativo`, { method: 'POST' });
        await desativarOutros(planoId);
      }
      return salvo;
    },
    onSuccess: (salvo, v) => {
      setSujo(false);
      setConflito(null);
      qc.invalidateQueries({ queryKey: ['pro'] });
      toast(v.novaVersao ? 'Nova versão publicada; o histórico ficou na anterior' : v.liberar ? 'Treino liberado para o paciente' : 'Treino salvo');
      if (ehNovo || v.novaVersao) nav(`/pro/pacientes/${id}/treino/${salvo.id}`, { replace: true });
      else setD(deServidor(salvo));
    },
    onError: (e) => {
      if (e instanceof ApiError && e.status === 409) setConflito(e.message);
      else toast(e.message, 'bad');
    },
  });

  const voltar = () => {
    if (sujo && !confirm('Sair sem salvar as alterações?')) return;
    nav(`/pro/pacientes/${id}?aba=treino`);
  };

  if (plano.isError) return <div className="p-gutter"><ErrorBox error={plano.error} onRetry={() => plano.refetch()} /></div>;

  const itemAberto = sessao?.itens.find((i) => i.k === aberto) ?? null;
  const posAberto = sessao && itemAberto ? { i: sessao.itens.indexOf(itemAberto), n: sessao.itens.length } : { i: 0, n: 0 };
  const vazio = !d || d.sessoes.every((s) => s.itens.length === 0);
  const seriesSessao = sessao?.itens.reduce((s, i) => s + i.series, 0) ?? 0;
  const minutosSessao = sessao ? Math.round(sessao.itens.reduce((s, i) => s + i.series * (40 + (i.descansoSegundos ?? 60)), 0) / 60) : 0;

  return (
    <div className="min-h-dvh bg-bg">
      <div className="sticky top-0 z-30 border-b border-line/60 bg-bg/90 pt-safe backdrop-blur-xl">
        <div className="mx-auto flex max-w-3xl items-center gap-2 px-gutter pt-2">
          <IconButton label="Voltar" onClick={voltar} className="-ml-2">
            <ChevronLeft className="h-6 w-6" />
          </IconButton>
          <div className="min-w-0 flex-1">
            <p className="truncate text-[12px] text-muted">{paciente.data?.nome ?? ' '}</p>
            {d ? (
              <input aria-label="Nome do plano" className="w-full truncate bg-transparent font-display text-lg font-semibold outline-none" value={d.nome} onChange={(e) => up((x) => ({ ...x, nome: e.target.value }))} />
            ) : (
              <Skeleton className="h-6 w-40" />
            )}
          </div>
          <IconButton label="Volume semanal" onClick={() => setVolume(true)}>
            <BarChart3 className="h-5 w-5" />
          </IconButton>
        </div>
        {d && (
          <div className="no-scrollbar mx-auto flex max-w-3xl gap-2 overflow-x-auto px-gutter pb-3 pt-2">
            {d.sessoes.map((s, i) => (
              <button
                key={s.k}
                onClick={() => setAtiva(i)}
                className={clsx('flex h-10 shrink-0 items-center gap-2 rounded-full border pl-1.5 pr-3.5 text-sm font-semibold transition', i === ativa ? 'border-accent bg-accent text-accent-ink' : 'border-line text-muted')}
              >
                <span className={clsx('grid h-7 w-7 place-items-center rounded-full font-display text-[13px]', i === ativa ? 'bg-accent-ink/15' : 'bg-raised')}>{letra(i)}</span>
                <span className="max-w-[9rem] truncate">{s.nome}</span>
              </button>
            ))}
            <button
              aria-label="Nova sessão"
              onClick={() => {
                up((x) => ({ ...x, sessoes: [...x.sessoes, { k: uid(), nome: letra(x.sessoes.length), dias: [], observacoes: null, itens: [] }] }));
                setAtiva(d.sessoes.length);
              }}
              className="grid h-10 w-10 shrink-0 place-items-center rounded-full border border-dashed border-line text-muted"
            >
              <Plus className="h-4 w-4" />
            </button>
          </div>
        )}
      </div>

      <div className="mx-auto max-w-3xl px-gutter pb-40 pt-4">
        {conflito && (
          <div className="mb-4 rounded-xl2 border border-warn/40 bg-warn/10 p-4 text-sm">
            <p>{conflito}</p>
            <Button size="sm" className="mt-3" loading={salvar.isPending} onClick={() => salvar.mutate({ liberar: true, novaVersao: true })}>
              Publicar como nova versão
            </Button>
          </div>
        )}
        {!d || !sessao || exercicios.isLoading ? (
          <div className="flex flex-col gap-3">
            <Skeleton className="h-24" />
            <Skeleton className="h-40" />
          </div>
        ) : (
          <AnimatePresence mode="wait">
            <motion.div key={sessao.k} initial={{ opacity: 0, x: 12 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -12 }} transition={{ duration: 0.18 }}>
              <Card className="p-4">
                <input aria-label="Nome da sessão" className="w-full bg-transparent font-display text-xl font-semibold outline-none" value={sessao.nome} onChange={(e) => upSessao((s) => ({ ...s, nome: e.target.value }))} />
                <div className="mt-3 grid grid-cols-7 gap-1.5">
                  {DIAS.map((dia, i) => {
                    const on = sessao.dias.includes(i);
                    return (
                      <button
                        key={dia}
                        aria-pressed={on}
                        onClick={() => upSessao((s) => ({ ...s, dias: on ? s.dias.filter((x) => x !== i) : [...s.dias, i] }))}
                        className={clsx('h-10 rounded-xl text-[13px] font-semibold transition', on ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}
                      >
                        {dia}
                      </button>
                    );
                  })}
                </div>
                <p className="num mt-3 flex items-center gap-3 text-[13px] text-muted">
                  <span>{sessao.itens.length} exercícios</span>
                  <span>{seriesSessao} séries</span>
                  <span className="flex items-center gap-1">
                    <Timer className="h-3.5 w-3.5" />~{minutosSessao} min
                  </span>
                </p>
              </Card>

              <div className="mt-4 flex flex-col gap-2">
                <AnimatePresence initial={false}>
                  {sessao.itens.map((i, idx) => {
                    const e = mapa.get(i.exercicioId);
                    return (
                      <motion.button
                        layout
                        key={i.k}
                        initial={{ opacity: 0, scale: 0.98 }}
                        animate={{ opacity: 1, scale: 1 }}
                        exit={{ opacity: 0, height: 0 }}
                        onClick={() => setAberto(i.k)}
                        className="flex w-full items-center gap-3 rounded-xl2 border border-line/70 bg-surface p-3 text-left transition active:scale-[0.99]"
                      >
                        <span className="num grid h-9 w-9 shrink-0 place-items-center rounded-full bg-raised text-sm font-semibold text-muted">{idx + 1}</span>
                        <div className="min-w-0 flex-1">
                          <p className="truncate font-semibold">{e?.nome ?? 'Exercício'}</p>
                          <p className="num truncate text-[13px] text-muted">
                            {i.series}×{i.repeticoes}
                            {i.carga ? ` · ${n0(i.carga)}${i.unidadeCarga ?? 'kg'}` : ''}
                            {i.descansoSegundos ? ` · ${i.descansoSegundos}s` : ''}
                            {i.rirAlvo != null ? ` · RIR ${i.rirAlvo}` : ''}
                          </p>
                        </div>
                        {i.tecnicaAvancada && <Pill tone="info">{i.tecnicaAvancada}</Pill>}
                        {e?.grupoMuscular && <span className="hidden text-[12px] text-faint sm:block">{e.grupoMuscular}</span>}
                      </motion.button>
                    );
                  })}
                </AnimatePresence>
                <Button variant="outline" icon={<Plus className="h-4 w-4" />} onClick={() => setPicker('add')}>
                  Adicionar exercício
                </Button>
              </div>

              <div className="mt-5">
              <Field label="Orientações da sessão">
                <textarea className={inputCls + ' h-20 py-3'} placeholder="Aquecimento, ordem, intervalo entre blocos…" value={sessao.observacoes ?? ''} onChange={(e) => upSessao((s) => ({ ...s, observacoes: e.target.value || null }))} />
              </Field>
              </div>

              <div className="mt-4 flex gap-2">
                <Button
                  size="sm"
                  variant="ghost"
                  icon={<Copy className="h-4 w-4" />}
                  onClick={() => {
                    up((x) => ({ ...x, sessoes: [...x.sessoes, { ...sessao, k: uid(), id: undefined, nome: `${sessao.nome} (cópia)`, dias: [], itens: sessao.itens.map((i) => ({ ...i, k: uid(), id: undefined })) }] }));
                    setAtiva(d.sessoes.length);
                  }}
                >
                  Duplicar sessão
                </Button>
                {d.sessoes.length > 1 && (
                  <Button
                    size="sm"
                    variant="ghost"
                    className="ml-auto text-bad"
                    icon={<Trash2 className="h-4 w-4" />}
                    onClick={() => {
                      if (!confirm(`Excluir a sessão ${sessao.nome}?`)) return;
                      up((x) => ({ ...x, sessoes: x.sessoes.filter((s) => s.k !== sessao.k) }));
                      setAtiva(0);
                    }}
                  >
                    Excluir sessão
                  </Button>
                )}
              </div>

              <Card className="mt-6 p-4">
                <p className="mb-3 font-display font-semibold">Plano</p>
                <div className="grid grid-cols-2 gap-3">
                  <Field label="Objetivo">
                    <input className={inputCls} value={d.objetivo ?? ''} placeholder="Hipertrofia, força…" onChange={(e) => up((x) => ({ ...x, objetivo: e.target.value || null }))} />
                  </Field>
                  <Field label="Início">
                    <input type="date" className={inputCls} value={d.dataInicio} onChange={(e) => up((x) => ({ ...x, dataInicio: e.target.value }))} />
                  </Field>
                </div>
              </Card>
            </motion.div>
          </AnimatePresence>
        )}
      </div>

      <div className="fixed inset-x-0 bottom-0 z-30 border-t border-line/60 bg-bg/90 pb-safe backdrop-blur-xl">
        <div className="mx-auto flex max-w-3xl gap-2 px-gutter py-3">
          <Button variant="outline" className="flex-1" disabled={vazio || (!sujo && !ehNovo)} loading={salvar.isPending && !salvar.variables?.liberar} onClick={() => salvar.mutate({ liberar: false })}>
            {sujo || ehNovo ? 'Salvar' : 'Salvo'}
          </Button>
          {(d?.status !== 'Ativo' || sujo) && (
            <Button className="flex-[1.4]" disabled={vazio} loading={salvar.isPending && !!salvar.variables?.liberar} onClick={() => salvar.mutate({ liberar: true })}>
              {d?.status === 'Ativo' ? 'Salvar e notificar' : 'Liberar ao paciente'}
            </Button>
          )}
        </div>
      </div>

      <SeletorExercicio
        open={!!picker}
        onClose={() => setPicker(null)}
        titulo={picker === 'swap' ? 'Trocar por' : 'Adicionar exercício'}
        grupoInicial={picker === 'swap' && itemAberto ? mapa.get(itemAberto.exercicioId)?.grupoMuscular : null}
        onPick={(e) => {
          if (picker === 'swap' && itemAberto) {
            upItem({ ...itemAberto, exercicioId: e.id });
          } else {
            const ultimo = sessao?.itens[sessao.itens.length - 1];
            const novoItem: Item = {
              k: uid(),
              exercicioId: e.id,
              series: ultimo?.series ?? 3,
              repeticoes: ultimo?.repeticoes ?? '10-12',
              carga: null,
              unidadeCarga: 'kg',
              descansoSegundos: ultimo?.descansoSegundos ?? 90,
              tempoSegundos: null,
              rirAlvo: ultimo?.rirAlvo ?? 2,
              cadencia: null,
              tecnicaAvancada: null,
              tecnicaAvancadaCodigo: null,
              tecnicaAvancadaParametros: null,
              observacoes: null,
            };
            upSessao((s) => ({ ...s, itens: [...s.itens, novoItem] }));
          }
          setPicker(null);
        }}
      />

      <ItemSheet
        item={picker ? null : itemAberto}
        exercicio={itemAberto ? mapa.get(itemAberto.exercicioId) : undefined}
        tecnicas={tecnicas.data?.itens ?? []}
        pos={posAberto}
        onClose={() => setAberto(null)}
        onChange={upItem}
        onRemove={() => {
          upSessao((s) => ({ ...s, itens: s.itens.filter((i) => i.k !== aberto) }));
          setAberto(null);
        }}
        onMove={(delta) =>
          upSessao((s) => {
            const arr = [...s.itens];
            const idx = arr.findIndex((i) => i.k === aberto);
            const [it] = arr.splice(idx, 1);
            arr.splice(idx + delta, 0, it);
            return { ...s, itens: arr };
          })
        }
        onSwap={() => setPicker('swap')}
      />

      {d && <VolumeSheet open={volume} onClose={() => setVolume(false)} sessoes={d.sessoes} mapa={mapa} />}
    </div>
  );
}
