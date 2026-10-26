import { useMutation, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { AnimatePresence, motion } from 'framer-motion';
import { Check, ChevronLeft, ChevronRight, Info, Minus, Pause, Plus, RotateCcw, Timer, Trophy, X } from 'lucide-react';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button, ErrorBox, Ring, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { mmss, n0, n1 } from '@/lib/format';
import type { ItemTreinoPortal, SessaoTreinoPortal } from '@/lib/types';
import { useHistoricoTreinos, useMeusTreinos } from './queries';
import { novoAndamento, salvarAndamento, SerieRegistro, treinoEmAndamento, TreinoAndamento } from './workout-store';

// ------------------------------------------------------------ utilidades de feedback

function bipe() {
  try {
    const Ctx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    const ctx = new Ctx();
    [0, 0.18, 0.36].forEach((t, i) => {
      const o = ctx.createOscillator();
      const g = ctx.createGain();
      o.frequency.value = i === 2 ? 1320 : 880;
      g.gain.setValueAtTime(0.0001, ctx.currentTime + t);
      g.gain.exponentialRampToValueAtTime(0.25, ctx.currentTime + t + 0.01);
      g.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + t + 0.14);
      o.connect(g).connect(ctx.destination);
      o.start(ctx.currentTime + t);
      o.stop(ctx.currentTime + t + 0.15);
    });
    setTimeout(() => ctx.close(), 800);
  } catch {
    /* sem audio */
  }
}

function useWakeLock(ativo: boolean) {
  useEffect(() => {
    if (!ativo || !('wakeLock' in navigator)) return;
    let lock: WakeLockSentinel | null = null;
    const pedir = () =>
      navigator.wakeLock
        .request('screen')
        .then((l) => (lock = l))
        .catch(() => {});
    pedir();
    const vis = () => document.visibilityState === 'visible' && pedir();
    document.addEventListener('visibilitychange', vis);
    return () => {
      document.removeEventListener('visibilitychange', vis);
      lock?.release().catch(() => {});
    };
  }, [ativo]);
}

function useAgora(intervalo = 250) {
  const [agora, setAgora] = useState(Date.now());
  useEffect(() => {
    const id = setInterval(() => setAgora(Date.now()), intervalo);
    return () => clearInterval(id);
  }, [intervalo]);
  return agora;
}

// ------------------------------------------------------------ descanso

function RestTimer({ fimEm, total, onAjustar, onFim }: { fimEm: number; total: number; onAjustar: (s: number) => void; onFim: () => void }) {
  const agora = useAgora(200);
  const restante = Math.max(0, Math.ceil((fimEm - agora) / 1000));
  const avisou = useRef(false);
  useEffect(() => {
    if (restante === 0 && !avisou.current) {
      avisou.current = true;
      bipe();
      navigator.vibrate?.([200, 100, 200]);
      const t = setTimeout(onFim, 900);
      return () => clearTimeout(t);
    }
  }, [restante, onFim]);
  return (
    <motion.div
      initial={{ y: 140, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      exit={{ y: 140, opacity: 0 }}
      transition={{ type: 'spring', stiffness: 380, damping: 34 }}
      className="fixed inset-x-0 bottom-0 z-40 px-gutter pb-[calc(12px+env(safe-area-inset-bottom,0px))]"
    >
      <div className="mx-auto flex max-w-lg items-center gap-4 rounded-xl3 border border-accent/40 bg-surface/95 p-3 pr-4 shadow-glow backdrop-blur-xl">
        <Ring value={restante} max={total} size={64} stroke={6}>
          <Timer className="h-5 w-5 text-accent" />
        </Ring>
        <div className="min-w-0 flex-1">
          <p className="eyebrow">Descanso</p>
          <p className={clsx('num text-[34px] font-semibold leading-none', restante <= 5 && 'text-accent')}>{mmss(restante)}</p>
        </div>
        <button onClick={() => onAjustar(15)} className="num h-11 rounded-full bg-raised px-3 text-sm font-semibold">
          +15s
        </button>
        <button onClick={onFim} className="h-11 rounded-full bg-accent px-4 text-sm font-semibold text-accent-ink">
          Pular
        </button>
      </div>
    </motion.div>
  );
}

// ------------------------------------------------------------ cartao do exercicio

function linhaPrescricao(it: ItemTreinoPortal) {
  const partes = [`${it.series} × ${it.repeticoes}`];
  if (it.carga != null) partes.push(`${n1(it.carga)} ${it.unidadeCarga ?? 'kg'}`);
  if (it.rirAlvo != null) partes.push(`RIR ${it.rirAlvo}`);
  if (it.cadencia) partes.push(`cadência ${it.cadencia}`);
  return partes.join(' · ');
}

function Ajuste({ label, value, onChange, step, max, sufixo }: { label: string; value: number | null; onChange: (v: number) => void; step: number; max: number; sufixo: string }) {
  const v = value ?? 0;
  const set = (n: number) => {
    navigator.vibrate?.(8);
    onChange(Math.max(0, Math.min(max, Math.round(n * 100) / 100)));
  };
  const botao = 'grid h-14 w-14 shrink-0 place-items-center rounded-2xl border border-line bg-surface text-fg transition active:scale-90 active:bg-raised';
  return (
    <div className="flex items-center gap-2 rounded-2xl bg-raised/60 p-2">
      <button type="button" aria-label={`Diminuir ${label}`} onClick={() => set(v - step)} className={botao}>
        <Minus className="h-5 w-5" strokeWidth={2.6} />
      </button>
      <label className="flex min-w-0 flex-1 flex-col items-center">
        <span className="text-[11px] font-semibold uppercase tracking-widest text-muted">{label}</span>
        <span className="flex items-baseline gap-1">
          <input
            inputMode="decimal"
            aria-label={label}
            className="num min-w-0 bg-transparent text-center text-[34px] font-semibold leading-tight outline-none"
            style={{ width: `${Math.max(1, String(value ?? '').length) + 0.6}ch` }}
            value={value ?? ''}
            placeholder="0"
            onChange={(e) => {
              const n = Number(e.target.value.replace(',', '.'));
              if (!Number.isNaN(n)) onChange(n);
            }}
          />
          <span className="text-sm text-muted">{sufixo}</span>
        </span>
      </label>
      <button type="button" aria-label={`Aumentar ${label}`} onClick={() => set(v + step)} className={botao}>
        <Plus className="h-5 w-5" strokeWidth={2.6} />
      </button>
    </div>
  );
}

function SerieLinha({ i, s, foco, unidade, onClick }: { i: number; s: SerieRegistro; foco: boolean; unidade: string; onClick: () => void }) {
  return (
    <li>
      <button
        onClick={onClick}
        className={clsx(
          'flex w-full items-center gap-3 rounded-2xl border px-3 py-2.5 text-left transition',
          foco ? 'border-accent bg-accent/[0.08]' : s.feita ? 'border-ok/25 bg-ok/[0.07]' : 'border-line bg-surface hover:border-accent/40',
        )}
      >
        <span className={clsx('num grid h-8 w-8 shrink-0 place-items-center rounded-full text-sm font-semibold', s.feita ? 'bg-ok text-white' : foco ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}>
          {s.feita ? <Check className="h-4 w-4" strokeWidth={3} /> : i + 1}
        </span>
        <span className="num flex-1 text-[15px] font-semibold">
          {s.reps ?? '—'} <span className="font-normal text-muted">reps</span> × {s.carga != null ? n1(s.carga) : '—'} <span className="font-normal text-muted">{unidade}</span>
        </span>
        <span className={clsx('text-[12px] font-semibold', s.feita ? 'text-ok' : foco ? 'text-accent' : 'text-faint')}>{s.feita ? 'Feita' : foco ? 'Agora' : 'A fazer'}</span>
      </button>
    </li>
  );
}

// ------------------------------------------------------------ pagina

interface Conclusao {
  duracao: number;
  volume: number;
  series: number;
  recordes: { nome: string; carga: number; anterior: number }[];
}

export function LiveWorkoutPage() {
  const { sessaoId } = useParams();
  const nav = useNavigate();
  const qc = useQueryClient();
  const treinos = useMeusTreinos();
  const historico = useHistoricoTreinos(180);
  const sessao: SessaoTreinoPortal | undefined = useMemo(() => treinos.data?.planos.flatMap((p) => p.sessoes).find((s) => s.id === sessaoId), [treinos.data, sessaoId]);

  // Ultima carga e melhor carga por item da prescricao.
  const { ultima, melhor, ultimaTexto } = useMemo(() => {
    const ultima = new Map<string, number>();
    const melhor = new Map<string, number>();
    const ultimaTexto = new Map<string, string>();
    const execs = [...(historico.data?.execucoes ?? [])].sort((a, b) => a.dataHoraInicioUtc.localeCompare(b.dataHoraInicioUtc));
    for (const e of execs)
      for (const it of e.itens) {
        if (it.cargaRealizada != null) {
          ultima.set(it.itemTreinoId, it.cargaRealizada);
          melhor.set(it.itemTreinoId, Math.max(melhor.get(it.itemTreinoId) ?? 0, it.cargaRealizada));
        }
        ultimaTexto.set(
          it.itemTreinoId,
          [it.cargaRealizada != null && `${n1(it.cargaRealizada)} ${it.unidadeCarga ?? 'kg'}`, it.repeticoesRealizadas && `${it.repeticoesRealizadas} reps`].filter(Boolean).join(' × '),
        );
      }
    return { ultima, melhor, ultimaTexto };
  }, [historico.data]);

  const [t, setT] = useState<TreinoAndamento | null>(null);
  const [descanso, setDescanso] = useState<{ fimEm: number; total: number } | null>(null);
  const [finalizar, setFinalizar] = useState(false);
  const [sair, setSair] = useState(false);
  const [detalhe, setDetalhe] = useState(false);
  const [rpe, setRpe] = useState(7);
  const [conclusao, setConclusao] = useState<Conclusao | null>(null);
  const [focoSel, setFocoSel] = useState<number | null>(null);
  const agora = useAgora(1000);
  useWakeLock(!!t && !conclusao);

  useEffect(() => {
    if (!sessao || t || historico.isLoading) return;
    const salvo = treinoEmAndamento();
    setT(salvo && salvo.sessaoId === sessao.id ? salvo : novoAndamento(sessao, ultima));
  }, [sessao, t, historico.isLoading, ultima]);

  useEffect(() => {
    if (t && !conclusao) salvarAndamento(t);
  }, [t, conclusao]);

  // Ao trocar de exercicio, o foco volta para a primeira serie pendente.
  useEffect(() => setFocoSel(null), [t?.atual]);

  const registrar = useMutation({
    mutationFn: (body: unknown) => api('/api/portal/me/treinos/execucoes', { method: 'POST', json: body }),
  });

  const fimDescanso = useCallback(() => setDescanso(null), []);

  if (treinos.isError) return <div className="p-4"><ErrorBox error={treinos.error} onRetry={() => treinos.refetch()} /></div>;
  if (!sessao || !t)
    return (
      <div className="mx-auto max-w-xl p-4 pt-safe">
        {treinos.isLoading || historico.isLoading ? (
          <div className="mt-6 flex flex-col gap-3">
            <Skeleton className="h-10 w-2/3" />
            <Skeleton className="h-64" />
            <Skeleton className="h-40" />
          </div>
        ) : (
          <ErrorBox error={new Error('Sessão não encontrada no seu plano atual.')} />
        )}
      </div>
    );

  const itens = sessao.itens;
  const atual = Math.min(t.atual, itens.length - 1);
  const it = itens[atual];
  const series = t.series[it.id] ?? [];
  const todas = Object.values(t.series).flat();
  const feitas = todas.filter((s) => s.feita).length;
  const decorrido = Math.floor((agora - new Date(t.inicio).getTime()) / 1000);
  const indiceAtiva = series.findIndex((s) => !s.feita);
  const foco = focoSel != null && focoSel < series.length ? focoSel : indiceAtiva;
  const sFoco = foco >= 0 ? series[foco] : undefined;
  const unidade = it.unidadeCarga ?? 'kg';

  const atualizarSerie = (idx: number, s: SerieRegistro) => setT((x) => x && { ...x, series: { ...x.series, [it.id]: x.series[it.id].map((o, i) => (i === idx ? s : o)) } });

  const alternar = (idx: number) => {
    const s = series[idx];
    const feita = !s.feita;
    // Propaga carga e reps para as series seguintes ainda nao feitas (padrao de academia).
    setT((x) => {
      if (!x) return x;
      const lista = x.series[it.id].map((o, i) => (i === idx ? { ...o, feita } : feita && i > idx && !o.feita ? { ...o, carga: s.carga, reps: o.reps ?? s.reps } : o));
      return { ...x, series: { ...x.series, [it.id]: lista } };
    });
    setFocoSel(null);
    if (feita) {
      navigator.vibrate?.(25);
      const ultimaDoExercicio = series.filter((o, i) => i !== idx && !o.feita).length === 0;
      const desc = it.descansoSegundos ?? 60;
      if (desc > 0) setDescanso({ fimEm: Date.now() + desc * 1000, total: desc });
      if (ultimaDoExercicio && atual < itens.length - 1) setTimeout(() => setT((x) => x && { ...x, atual: atual + 1 }), 450);
    }
  };

  const ir = (n: number) => setT((x) => x && { ...x, atual: Math.max(0, Math.min(itens.length - 1, n)) });

  const adicionarSerie = () =>
    setT((x) => {
      if (!x) return x;
      const l = x.series[it.id];
      const base = l[l.length - 1] ?? { reps: null, carga: null };
      return { ...x, series: { ...x.series, [it.id]: [...l, { reps: base.reps, carga: base.carga, feita: false }] } };
    });

  async function concluir() {
    if (!t || !sessao) return;
    const fim = new Date();
    const duracao = Math.max(1, Math.round((fim.getTime() - new Date(t.inicio).getTime()) / 60000));
    const recordes: Conclusao['recordes'] = [];
    let volume = 0;
    const itensBody = sessao.itens.map((item) => {
      const ss = (t.series[item.id] ?? []).filter((s) => s.feita);
      const cargaMax = ss.reduce<number | null>((m, s) => (s.carga != null && (m == null || s.carga > m) ? s.carga : m), null);
      ss.forEach((s) => (volume += (s.carga ?? 0) * (s.reps ?? 0)));
      const ant = melhor.get(item.id);
      if (cargaMax != null && ant != null && cargaMax > ant) recordes.push({ nome: item.exercicio, carga: cargaMax, anterior: ant });
      return {
        itemTreinoId: item.id,
        seriesRealizadas: ss.length,
        repeticoesRealizadas: ss.length ? ss.map((s) => s.reps ?? '-').join('/') : null,
        cargaRealizada: cargaMax,
        unidadeCarga: item.unidadeCarga,
        esforcoPercebido: rpe,
        concluido: ss.length >= item.series,
        observacoes: ss.length ? 'Séries: ' + ss.map((s) => `${s.reps ?? '-'}×${s.carga != null ? n1(s.carga) : '-'}`).join(' · ') : null,
        exercicioAlternativoId: null,
        motivoAlternativa: null,
        rirRealizado: null,
      };
    });
    try {
      await registrar.mutateAsync({
        sessaoTreinoId: sessao.id,
        dataHoraInicioUtc: t.inicio,
        dataHoraFimUtc: fim.toISOString(),
        duracaoMinutos: duracao,
        esforcoPercebido: rpe,
        observacoes: null,
        itens: itensBody,
      });
      salvarAndamento(null);
      qc.invalidateQueries({ queryKey: ['p'] });
      setFinalizar(false);
      setConclusao({ duracao, volume, series: itensBody.reduce((a, i) => a + i.seriesRealizadas, 0), recordes });
    } catch (e) {
      toast(e instanceof Error ? e.message : 'Não foi possível salvar. Seu treino continua salvo no aparelho.', 'bad');
    }
  }

  if (conclusao) return <Celebracao c={conclusao} nome={sessao.nome} onFechar={() => nav('/hoje', { replace: true })} />;

  return (
    <div className="min-h-full pb-40">
      {/* Barra superior */}
      <div className="sticky top-0 z-30 border-b border-line/70 bg-bg/90 pt-safe backdrop-blur-xl">
        <div className="mx-auto flex max-w-xl items-center gap-2 px-gutter py-2.5 lg:max-w-5xl">
          <button aria-label="Sair do treino" onClick={() => setSair(true)} className="-ml-2 grid h-10 w-10 place-items-center rounded-full hover:bg-raised">
            <X className="h-5 w-5" />
          </button>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-semibold">{sessao.nome}</p>
            <p className="num text-xs text-muted">
              {feitas}/{todas.length} séries
            </p>
          </div>
          <span className="num mr-1 rounded-full bg-raised px-3 py-1.5 text-sm font-semibold tabular-nums">{mmss(decorrido)}</span>
          <Button size="sm" onClick={() => setFinalizar(true)} variant={feitas === todas.length ? 'primary' : 'soft'}>
            Finalizar
          </Button>
        </div>
        <div className="h-[3px] bg-line/50">
          <motion.div className="h-full bg-accent" animate={{ width: `${(feitas / Math.max(1, todas.length)) * 100}%` }} />
        </div>
      </div>

      <div className="mx-auto max-w-xl px-gutter lg:grid lg:max-w-5xl lg:grid-cols-[280px_minmax(0,1fr)] lg:items-start lg:gap-8">
        {/* Lista de exercicios: trilha no celular, coluna no PC */}
        <nav className="lg:sticky lg:top-24 lg:mt-6">
          <p className="eyebrow mb-3 hidden lg:block">Sessão de hoje</p>
          <ol className="no-scrollbar -mx-gutter mt-3 flex gap-2 overflow-x-auto px-gutter pb-1 lg:mx-0 lg:mt-0 lg:flex-col lg:overflow-visible lg:px-0">
            {itens.map((x, i) => {
              const ss = t.series[x.id] ?? [];
              const nFeitas = ss.filter((s) => s.feita).length;
              const ok = ss.length > 0 && nFeitas === ss.length;
              return (
                <li key={x.id} className="shrink-0">
                  <button
                    onClick={() => ir(i)}
                    className={clsx(
                      'flex h-9 items-center gap-1.5 rounded-full border px-3 text-[13px] font-semibold transition lg:h-auto lg:w-full lg:gap-3 lg:rounded-2xl lg:p-3 lg:text-left',
                      i === atual ? 'border-accent bg-accent text-accent-ink lg:bg-accent/10 lg:text-fg' : ok ? 'border-ok/40 bg-ok/10 text-ok lg:text-fg' : nFeitas ? 'border-accent/40 text-accent lg:text-fg' : 'border-line text-muted lg:bg-surface lg:text-fg',
                    )}
                  >
                    <span
                      className={clsx(
                        'num lg:grid lg:h-8 lg:w-8 lg:shrink-0 lg:place-items-center lg:rounded-full lg:text-sm',
                        ok ? 'lg:bg-ok lg:text-white' : i === atual ? 'lg:bg-accent lg:text-accent-ink' : 'lg:bg-raised lg:text-muted',
                      )}
                    >
                      {ok ? <Check className="h-3.5 w-3.5" strokeWidth={3} /> : i + 1}
                    </span>
                    <span className="max-w-[9rem] truncate lg:min-w-0 lg:max-w-none lg:flex-1">
                      {x.exercicio}
                      <span className="num hidden text-[12px] font-normal text-muted lg:block">
                        {nFeitas}/{ss.length} séries{x.grupoMuscular ? ` · ${x.grupoMuscular}` : ''}
                      </span>
                    </span>
                  </button>
                </li>
              );
            })}
          </ol>
        </nav>

        <AnimatePresence mode="wait">
          <motion.section key={it.id} initial={{ opacity: 0, x: 30 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -30 }} transition={{ duration: 0.25 }} className="lg:mt-6">
            <div className="mt-5 flex items-start justify-between gap-3 lg:mt-0">
              <div className="min-w-0">
                <p className="eyebrow">
                  Exercício {atual + 1} de {itens.length}
                  {it.grupoMuscular && ` · ${it.grupoMuscular}`}
                </p>
                <h1 className="mt-1.5 font-display text-[28px] font-semibold leading-[1.1] tracking-tight lg:text-[34px]">{it.exercicio}</h1>
              </div>
              {(it.descricao || it.observacoes || it.videoUrl) && (
                <button aria-label="Como executar" onClick={() => setDetalhe(true)} className="mt-1 grid h-10 w-10 shrink-0 place-items-center rounded-full bg-raised text-muted">
                  <Info className="h-5 w-5" />
                </button>
              )}
            </div>

            <div className="mt-3 flex flex-wrap gap-2 text-[13px]">
              <span className="rounded-full bg-accent/10 px-3 py-1 font-semibold text-accent">{linhaPrescricao(it)}</span>
              {it.descansoSegundos != null && (
                <span className="flex items-center gap-1 rounded-full bg-raised px-3 py-1 text-muted">
                  <Pause className="h-3 w-3" /> {mmss(it.descansoSegundos)}
                </span>
              )}
              {it.tecnicaAvancada && <span className="rounded-full bg-warn/15 px-3 py-1 font-semibold text-warn">{it.tecnicaAvancada}</span>}
              {ultimaTexto.get(it.id) && (
                <span className="rounded-full bg-raised px-3 py-1 text-muted">
                  Última vez <span className="num font-semibold text-fg">{ultimaTexto.get(it.id)}</span>
                </span>
              )}
            </div>
            {it.observacoes && <p className="mt-3 rounded-xl border border-line bg-surface px-3 py-2 text-sm">{it.observacoes}</p>}

            {/* Serie em foco */}
            {sFoco ? (
              <div className="mt-5 rounded-xl3 border border-accent/40 bg-surface p-4 shadow-glow">
                <div className="flex items-baseline justify-between gap-3">
                  <p className="font-display text-lg font-semibold">
                    Série {foco + 1} <span className="text-muted">de {series.length}</span>
                  </p>
                  <p className="num text-[13px] text-muted">
                    Meta {it.repeticoes} reps{it.carga != null ? ` · ${n1(it.carga)} ${unidade}` : ''}
                  </p>
                </div>
                <div className="mt-3 grid gap-2 sm:grid-cols-2">
                  <Ajuste label="Repetições" value={sFoco.reps} onChange={(reps) => atualizarSerie(foco, { ...sFoco, reps })} step={1} max={100} sufixo="reps" />
                  <Ajuste label="Carga" value={sFoco.carga} onChange={(carga) => atualizarSerie(foco, { ...sFoco, carga })} step={unidade === 'kg' ? 2.5 : 1} max={999} sufixo={unidade} />
                </div>
                {sFoco.feita ? (
                  <button onClick={() => alternar(foco)} className="mt-3 flex h-14 w-full items-center justify-center gap-2 rounded-2xl border border-line text-[15px] font-semibold text-muted transition active:scale-[0.98]">
                    <RotateCcw className="h-4 w-4" /> Desmarcar série {foco + 1}
                  </button>
                ) : (
                  <button
                    onClick={() => alternar(foco)}
                    className="mt-3 flex h-16 w-full items-center justify-center gap-2 rounded-2xl bg-accent text-[17px] font-semibold text-accent-ink shadow-glow transition hover:brightness-110 active:scale-[0.98]"
                  >
                    <Check className="h-6 w-6" strokeWidth={3} /> Concluir série {foco + 1}
                  </button>
                )}
              </div>
            ) : (
              <div className="mt-5 flex flex-col items-center rounded-xl3 border border-ok/40 bg-ok/[0.08] p-5 text-center">
                <span className="grid h-12 w-12 place-items-center rounded-full bg-ok text-white">
                  <Check className="h-6 w-6" strokeWidth={3} />
                </span>
                <p className="mt-3 font-display text-lg font-semibold">Exercício concluído</p>
                <p className="text-sm text-muted">Todas as séries foram registradas.</p>
                {atual < itens.length - 1 ? (
                  <Button size="lg" className="mt-4 w-full" onClick={() => ir(atual + 1)}>
                    Próximo: {itens[atual + 1].exercicio}
                  </Button>
                ) : (
                  <Button size="lg" className="mt-4 w-full" onClick={() => setFinalizar(true)}>
                    Finalizar treino
                  </Button>
                )}
              </div>
            )}

            <ul className="mt-4 flex flex-col gap-2">
              {series.map((s, i) => (
                <SerieLinha key={i} i={i} s={s} foco={i === foco} unidade={unidade} onClick={() => setFocoSel(i)} />
              ))}
            </ul>
            <button onClick={adicionarSerie} className="mt-2 flex h-11 w-full items-center justify-center gap-1.5 rounded-2xl border border-dashed border-line text-sm font-semibold text-muted hover:text-fg">
              <Plus className="h-4 w-4" /> Série extra
            </button>

            <div className="mt-6 flex gap-3">
              <Button variant="outline" className="flex-1" disabled={atual === 0} onClick={() => ir(atual - 1)} icon={<ChevronLeft className="h-4 w-4" />}>
                Anterior
              </Button>
              {atual < itens.length - 1 ? (
                <Button variant="soft" className="flex-1" onClick={() => ir(atual + 1)}>
                  Próximo <ChevronRight className="h-4 w-4" />
                </Button>
              ) : (
                <Button variant="soft" className="flex-1" onClick={() => setFinalizar(true)}>
                  Finalizar treino
                </Button>
              )}
            </div>
          </motion.section>
        </AnimatePresence>
      </div>
      <AnimatePresence>
        {descanso && (
          <RestTimer
            key={descanso.total + '-' + descanso.fimEm}
            fimEm={descanso.fimEm}
            total={descanso.total}
            onAjustar={(s) => setDescanso((d) => d && { fimEm: d.fimEm + s * 1000, total: d.total + s })}
            onFim={fimDescanso}
          />
        )}
      </AnimatePresence>

      <Sheet open={detalhe} onClose={() => setDetalhe(false)} title={it.exercicio}>
        <div className="flex flex-col gap-3 text-[15px]">
          {it.equipamento && <p className="text-muted">Equipamento: {it.equipamento}</p>}
          {it.descricao && <p>{it.descricao}</p>}
          {it.observacoes && <p className="rounded-xl bg-raised p-3">{it.observacoes}</p>}
          {it.videoUrl && (
            <a href={it.videoUrl} target="_blank" rel="noreferrer" className="font-semibold text-accent">
              Ver vídeo de execução
            </a>
          )}
        </div>
      </Sheet>

      <Sheet
        open={finalizar}
        onClose={() => setFinalizar(false)}
        title="Como foi o treino?"
        footer={
          <Button size="lg" className="w-full" loading={registrar.isPending} onClick={concluir}>
            Salvar treino
          </Button>
        }
      >
        <p className="text-sm text-muted">
          {feitas} de {todas.length} séries em {Math.max(1, Math.round(decorrido / 60))} min.
          {feitas < todas.length && ' As séries não marcadas ficam registradas como não feitas.'}
        </p>
        <p className="mt-5 font-semibold">Esforço percebido (RPE)</p>
        <p className="mb-3 text-sm text-muted">0 é repouso, 10 é o máximo que você aguentaria.</p>
        <div className="grid grid-cols-11 gap-1">
          {Array.from({ length: 11 }, (_, i) => (
            <button
              key={i}
              onClick={() => setRpe(i)}
              className={clsx('num h-11 rounded-lg text-sm font-semibold', rpe === i ? (i >= 9 ? 'bg-bad text-white' : i >= 7 ? 'bg-warn text-black' : 'bg-ok text-white') : 'bg-raised text-muted')}
            >
              {i}
            </button>
          ))}
        </div>
        <p className="mt-3 text-center text-sm font-semibold">
          {['Repouso', 'Muito leve', 'Muito leve', 'Leve', 'Moderado', 'Moderado', 'Um pouco pesado', 'Pesado', 'Muito pesado', 'Quase no limite', 'Máximo'][rpe]}
        </p>
      </Sheet>

      <Sheet open={sair} onClose={() => setSair(false)} title="Sair do treino?">
        <p className="text-muted">Seu progresso fica salvo neste aparelho. Você pode continuar depois pela tela Hoje.</p>
        <div className="mt-5 flex flex-col gap-2">
          <Button size="lg" onClick={() => nav('/hoje')}>
            Sair e continuar depois
          </Button>
          <Button
            variant="danger"
            size="lg"
            onClick={() => {
              salvarAndamento(null);
              nav('/hoje');
            }}
          >
            Descartar treino
          </Button>
        </div>
      </Sheet>
    </div>
  );
}

function Celebracao({ c, nome, onFechar }: { c: Conclusao; nome: string; onFechar: () => void }) {
  const pecas = useMemo(() => Array.from({ length: 28 }, (_, i) => ({ i, x: Math.random() * 100, d: Math.random() * 0.6, r: Math.random() * 360, s: 6 + Math.random() * 8 })), []);
  return (
    <div className="relative flex min-h-full flex-col items-center overflow-hidden px-gutter pb-10 pt-safe text-center">
      <div className="pointer-events-none absolute inset-0">
        {pecas.map((p) => (
          <motion.span
            key={p.i}
            className="absolute top-0 block rounded-sm"
            style={{ left: `${p.x}%`, width: p.s, height: p.s * 0.45, background: p.i % 3 === 0 ? 'rgb(var(--accent))' : p.i % 3 === 1 ? 'rgb(var(--carb))' : 'rgb(var(--prot))' }}
            initial={{ y: -20, rotate: p.r, opacity: 1 }}
            animate={{ y: '105vh', rotate: p.r + 540, opacity: 0.9 }}
            transition={{ duration: 2.4 + p.d * 2, delay: p.d, ease: 'easeIn' }}
          />
        ))}
      </div>
      <motion.div initial={{ scale: 0.4, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} transition={{ type: 'spring', stiffness: 200, damping: 12 }} className="mt-20 grid h-28 w-28 place-items-center rounded-full bg-accent text-accent-ink shadow-glow">
        <Trophy className="h-14 w-14" />
      </motion.div>
      <motion.h1 initial={{ y: 20, opacity: 0 }} animate={{ y: 0, opacity: 1 }} transition={{ delay: 0.2 }} className="mt-8 font-display text-[32px] font-semibold leading-tight">
        Treino concluído
      </motion.h1>
      <p className="mt-1 text-muted">{nome}</p>
      <motion.div initial={{ y: 20, opacity: 0 }} animate={{ y: 0, opacity: 1 }} transition={{ delay: 0.35 }} className="mt-8 grid w-full max-w-sm grid-cols-3 gap-2">
        {[
          [String(c.duracao), 'min'],
          [String(c.series), 'séries'],
          [c.volume >= 1000 ? `${n1(c.volume / 1000)}t` : n0(c.volume), 'kg movidos'],
        ].map(([v, l]) => (
          <div key={l} className="rounded-2xl border border-line bg-surface p-3">
            <p className="num text-2xl font-semibold">{v}</p>
            <p className="text-xs text-muted">{l}</p>
          </div>
        ))}
      </motion.div>
      {c.recordes.length > 0 && (
        <motion.div initial={{ y: 20, opacity: 0 }} animate={{ y: 0, opacity: 1 }} transition={{ delay: 0.5 }} className="mt-4 w-full max-w-sm rounded-2xl border border-warn/40 bg-warn/10 p-4 text-left">
          <p className="eyebrow !text-warn">Recordes pessoais</p>
          <ul className="mt-2 flex flex-col gap-1.5">
            {c.recordes.map((r) => (
              <li key={r.nome} className="flex justify-between gap-3 text-sm">
                <span className="font-semibold">{r.nome}</span>
                <span className="num text-muted">
                  {n1(r.anterior)} → <span className="font-semibold text-fg">{n1(r.carga)} kg</span>
                </span>
              </li>
            ))}
          </ul>
        </motion.div>
      )}
      <Button size="lg" className="mt-10 w-full max-w-sm" onClick={onFechar}>
        Voltar para hoje
      </Button>
    </div>
  );
}
