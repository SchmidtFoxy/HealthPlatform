import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { AnimatePresence, motion } from 'framer-motion';
import { Bell, BellRing, Check, Droplet, Flame, GlassWater, Milk, Plus, Undo2 } from 'lucide-react';
import { forwardRef, useEffect, useId, useRef, useState } from 'react';
import { toast } from '@/components/ui';
import { api } from '@/lib/api';
import { diaSemanaCurto, hojeIso, n0 } from '@/lib/format';
import { ativarPush, inscricaoAtual, pushSuportado } from '@/lib/push';

// Agua da tela Hoje: copo que enche, botoes rapidos, ultimos dias, sequencia e lembrete por push.
// Com meta hidrica do profissional, soma no registro da meta do dia (o mesmo que o coach e o profissional leem).
// Sem meta, cada copo vira um registro de Hidratacao no diario e a referencia vem do peso (35 ml/kg).
// As contas ficam no servidor (/api/portal/me/agua), que tambem usa o mesmo resumo para o lembrete.

interface DiaAgua {
  data: string;
  ml: number;
  bateu: boolean;
}

interface ResumoAgua {
  hojeMl: number;
  metaMl: number;
  origemMeta: 'Profissional' | 'Peso' | 'Padrao';
  metaId: string | null;
  pesoKg: number | null;
  sequencia: number;
  melhorSequencia: number;
  ultimosDias: DiaAgua[];
  lembreteAtivo: boolean;
}

const fuso = () => new Date().getTimezoneOffset();
const textoMl = (ml: number) => (ml >= 1000 ? `${(ml / 1000).toLocaleString('pt-BR', { maximumFractionDigits: 2 })} L` : `${n0(ml)} ml`);
const CHAVE = ['p', 'agua'] as const;

/** Quanto ja deveria ter bebido a esta hora: a meta distribuida das 7h as 21h (igual ao lembrete). */
const ritmo = (metaMl: number, d = new Date()) => Math.round(metaMl * Math.min(1, Math.max(0, (d.getHours() + d.getMinutes() / 60 - 7) / 14)));

const RAPIDOS = [
  { ml: 250, rotulo: 'Copo', Icone: GlassWater },
  { ml: 500, rotulo: 'Garrafa', Icone: Milk },
  { ml: 1000, rotulo: 'Garrafão', Icone: Milk },
];
const OUTROS = [100, 150, 200, 300, 350, 750];

export const AguaCard = forwardRef<HTMLDivElement, { delay?: number }>(function AguaCard({ delay = 0 }, ref) {
  const qc = useQueryClient();
  const hoje = hojeIso();
  const resumo = useQuery({
    queryKey: [...CHAVE, hoje],
    queryFn: () => api<ResumoAgua>(`/api/portal/me/agua?data=${hoje}&fuso=${fuso()}`),
  });
  const [desfazer, setDesfazer] = useState<number[]>([]);
  const [outros, setOutros] = useState(false);
  const [festa, setFesta] = useState(0);
  const [respingo, setRespingo] = useState<{ id: number; ml: number } | null>(null);

  const somar = useMutation({
    // Toques seguidos vao um de cada vez, para o total no servidor nao se perder.
    scope: { id: 'agua' },
    mutationFn: (ml: number) => api<ResumoAgua>('/api/portal/me/agua', { method: 'POST', json: { data: hoje, ml, fuso: fuso() } }),
    onMutate: async (ml) => {
      await qc.cancelQueries({ queryKey: [...CHAVE, hoje] });
      const antes = qc.getQueryData<ResumoAgua>([...CHAVE, hoje]);
      if (antes) qc.setQueryData<ResumoAgua>([...CHAVE, hoje], { ...antes, hojeMl: Math.max(0, antes.hojeMl + ml) });
      if (ml > 0) {
        setRespingo({ id: Date.now(), ml });
        setDesfazer((d) => [...d.slice(-9), ml]);
      } else setDesfazer((d) => d.slice(0, -1));
      return { antes };
    },
    onSuccess: (novo, ml, ctx) => {
      qc.setQueryData([...CHAVE, hoje], novo);
      qc.invalidateQueries({ queryKey: ['p', 'metas'] });
      qc.invalidateQueries({ queryKey: ['p', 'diario-registros'] });
      qc.invalidateQueries({ queryKey: ['p', 'home'] });
      const antes = ctx?.antes?.hojeMl ?? 0;
      if (ml > 0 && antes < novo.metaMl && novo.hojeMl >= novo.metaMl) {
        setFesta(Date.now());
        toast(novo.sequencia > 1 ? `Meta de água batida. ${novo.sequencia} dias seguidos! 💧` : 'Meta de água do dia batida. 💧');
      }
    },
    onError: (e, _ml, ctx) => {
      if (ctx?.antes) qc.setQueryData([...CHAVE, hoje], ctx.antes);
      toast(e instanceof Error ? e.message : 'Não foi possível registrar.', 'bad');
    },
  });

  const lembrete = useMutation({
    mutationFn: async (ativo: boolean) => {
      if (ativo) {
        if (!pushSuportado()) throw new Error('Este aparelho não recebe notificações. No iPhone, instale o AESYN na tela de início primeiro.');
        if (!(await inscricaoAtual())) await ativarPush();
      }
      return api<{ ativo: boolean; aparelhos: number }>('/api/portal/me/agua/lembrete', { method: 'PUT', json: { ativo, fuso: fuso() } });
    },
    onSuccess: (r) => {
      qc.setQueryData<ResumoAgua>([...CHAVE, hoje], (a) => (a ? { ...a, lembreteAtivo: r.ativo } : a));
      qc.invalidateQueries({ queryKey: ['push'] });
      toast(r.ativo ? 'Lembrete ligado. Às 10h, 13h, 16h e 19h, só se você estiver atrás do ritmo.' : 'Lembrete de água desligado.');
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível alterar o lembrete.', 'bad'),
  });

  useEffect(() => {
    if (!respingo) return;
    const t = setTimeout(() => setRespingo(null), 900);
    return () => clearTimeout(t);
  }, [respingo]);

  if (!resumo.data) return null;
  const r = resumo.data;
  const pct = r.metaMl > 0 ? Math.min(100, (r.hojeMl / r.metaMl) * 100) : 0;
  const bateu = r.hojeMl >= r.metaMl;
  const falta = Math.max(0, r.metaMl - r.hojeMl);
  const esperado = ritmo(r.metaMl);
  const ultimoDesfazer = desfazer[desfazer.length - 1] ?? 250;
  const semana = r.ultimosDias.slice(-7);

  const status = bateu
    ? r.hojeMl > r.metaMl * 1.05
      ? `Meta batida, ${textoMl(r.hojeMl - r.metaMl)} além.`
      : 'Meta do dia batida. Mandou bem!'
    : r.hojeMl === 0
      ? 'Toque num copo quando beber.'
      : r.hojeMl >= esperado
        ? `No ritmo. Faltam ${textoMl(falta)}.`
        : `Um pouco atrás do ritmo. Faltam ${textoMl(falta)}.`;
  const origem =
    r.origemMeta === 'Profissional'
      ? 'meta do seu profissional'
      : r.origemMeta === 'Peso'
        ? `referência de 35 ml/kg (${n0(r.pesoKg)} kg)`
        : 'referência geral';

  return (
    <motion.div
      ref={ref}
      id="agua"
      initial={{ opacity: 0, y: 14 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay, duration: 0.45 }}
      className={clsx('relative mt-3 scroll-mt-24 overflow-hidden rounded-xl2 border bg-surface p-4 transition-colors', bateu ? 'border-info/40' : 'border-line')}
    >
      {bateu && <div aria-hidden className="pointer-events-none absolute inset-0 bg-gradient-to-br from-info/[0.10] via-transparent to-transparent" />}

      <div className="relative flex items-center gap-2">
        <Droplet className="h-4 w-4 text-info" />
        <p className="flex-1 font-semibold">Água hoje</p>
        {r.sequencia > 0 && (
          <span
            title={r.melhorSequencia > r.sequencia ? `Melhor sequência: ${r.melhorSequencia} dias` : undefined}
            className="num inline-flex items-center gap-1 rounded-full bg-warn/15 px-2.5 py-1 text-[12px] font-semibold text-warn"
          >
            <Flame className="h-3.5 w-3.5" />
            {r.sequencia} {r.sequencia === 1 ? 'dia' : 'dias'}
          </span>
        )}
        <button
          aria-label={r.lembreteAtivo ? 'Desligar lembrete de água' : 'Ligar lembrete de água'}
          aria-pressed={r.lembreteAtivo}
          disabled={lembrete.isPending}
          onClick={() => lembrete.mutate(!r.lembreteAtivo)}
          className={clsx(
            'grid h-9 w-9 place-items-center rounded-full transition active:scale-95 disabled:opacity-60',
            r.lembreteAtivo ? 'bg-info/15 text-info' : 'bg-raised text-muted hover:text-fg',
          )}
        >
          {r.lembreteAtivo ? <BellRing className="h-4 w-4" /> : <Bell className="h-4 w-4" />}
        </button>
      </div>

      <div className="relative mt-3 flex items-center gap-4">
        <div className="relative shrink-0">
          <Copo pct={pct} cheio={bateu} />
          <AnimatePresence>
            {respingo && (
              <motion.span
                key={respingo.id}
                initial={{ opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: -18 }}
                exit={{ opacity: 0, y: -30 }}
                transition={{ duration: 0.5 }}
                className="num pointer-events-none absolute inset-x-0 top-0 text-center text-[12px] font-bold text-info"
              >
                +{textoMl(respingo.ml)}
              </motion.span>
            )}
          </AnimatePresence>
          <Festa chave={festa} />
        </div>
        <div className="min-w-0 flex-1">
          <p className="num font-display text-[30px] font-semibold leading-none tracking-tight">
            <motion.span key={r.hojeMl} initial={{ opacity: 0.4, y: 4 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.25 }} className="inline-block">
              {textoMl(r.hojeMl)}
            </motion.span>
          </p>
          <p className="num mt-1 text-[13px] text-muted">
            de {textoMl(r.metaMl)} · {Math.round((r.hojeMl / Math.max(1, r.metaMl)) * 100)}%
          </p>
          <p className={clsx('mt-2 text-[13px] font-medium', bateu ? 'text-info' : r.hojeMl > 0 && r.hojeMl < esperado ? 'text-warn' : 'text-fg')}>{status}</p>
          <p className="mt-0.5 text-[11px] text-faint">{origem}</p>
        </div>
      </div>

      <div className="relative mt-4 grid grid-cols-7 gap-1.5" aria-label="Últimos 7 dias">
        {semana.map((d, i) => {
          const p = Math.min(100, (d.ml / Math.max(1, r.metaMl)) * 100);
          const eHoje = i === semana.length - 1;
          return (
            <div key={d.data} className="flex flex-col items-center gap-1" title={`${d.data.slice(8, 10)}/${d.data.slice(5, 7)}: ${textoMl(d.ml)}`}>
              <div className={clsx('relative h-9 w-full max-w-[30px] overflow-hidden rounded-lg bg-info/[0.08]', eHoje && 'ring-1 ring-info/50')}>
                <motion.div
                  className={clsx('absolute inset-x-0 bottom-0', d.bateu ? 'bg-info' : 'bg-info/45')}
                  initial={false}
                  animate={{ height: `${eHoje ? pct : p}%` }}
                  transition={{ duration: 0.6, ease: 'easeOut' }}
                />
                {d.bateu && <Check className="absolute inset-0 m-auto h-3.5 w-3.5 text-white" strokeWidth={3} />}
              </div>
              <span className={clsx('text-[10px] uppercase', eHoje ? 'font-bold text-info' : 'text-faint')}>{eHoje ? 'hoje' : diaSemanaCurto(d.data)}</span>
            </div>
          );
        })}
      </div>

      <div className="relative mt-4 grid grid-cols-4 gap-2">
        {RAPIDOS.map(({ ml, rotulo, Icone }) => (
          <button
            key={ml}
            onClick={() => somar.mutate(ml)}
            className="flex flex-col items-center gap-0.5 rounded-2xl bg-info/10 py-2.5 text-info transition hover:bg-info/15 active:scale-95"
          >
            <Icone className={clsx(ml === 1000 ? 'h-5 w-5' : 'h-4 w-4')} />
            <span className="num text-sm font-semibold">+ {textoMl(ml)}</span>
            <span className="text-[11px] text-info/80">{rotulo}</span>
          </button>
        ))}
        <button
          aria-expanded={outros}
          onClick={() => setOutros((v) => !v)}
          className={clsx('flex flex-col items-center justify-center gap-0.5 rounded-2xl py-2.5 transition active:scale-95', outros ? 'bg-info text-white' : 'bg-raised text-muted')}
        >
          <Plus className={clsx('h-4 w-4 transition', outros && 'rotate-45')} />
          <span className="text-sm font-semibold">Outro</span>
        </button>
      </div>

      <AnimatePresence initial={false}>
        {outros && (
          <motion.div initial={{ height: 0, opacity: 0 }} animate={{ height: 'auto', opacity: 1 }} exit={{ height: 0, opacity: 0 }} className="overflow-hidden">
            <div className="mt-2 flex flex-wrap gap-2">
              {OUTROS.map((ml) => (
                <button key={ml} onClick={() => somar.mutate(ml)} className="num rounded-full border border-info/30 px-3 py-1.5 text-[13px] font-semibold text-info transition active:scale-95">
                  + {textoMl(ml)}
                </button>
              ))}
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {r.hojeMl > 0 && (
        <button
          onClick={() => somar.mutate(-Math.min(ultimoDesfazer, r.hojeMl))}
          className="num relative mt-3 inline-flex items-center gap-1.5 text-[12px] font-medium text-muted transition hover:text-fg"
        >
          <Undo2 className="h-3.5 w-3.5" />
          Tirar {textoMl(Math.min(ultimoDesfazer, r.hojeMl))}
        </button>
      )}
    </motion.div>
  );
});

/** Copo em SVG: a agua sobe ate o percentual com uma onda correndo por cima. */
function Copo({ pct, cheio }: { pct: number; cheio: boolean }) {
  const id = useId().replace(/:/g, '');
  const topo = 14;
  const fundo = 122;
  const nivel = fundo - (pct / 100) * (fundo - topo);
  const contorno = 'M12 8 H88 L79.5 112 Q79 122 69 122 H31 Q21 122 20.5 112 Z';
  return (
    <svg viewBox="0 0 100 130" className="h-[118px] w-[92px]" role="img" aria-label={`Copo ${Math.round(pct)}% cheio`}>
      <defs>
        <clipPath id={`copo-${id}`}>
          <path d={contorno} />
        </clipPath>
        <linearGradient id={`agua-${id}`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="rgb(var(--info))" stopOpacity="0.95" />
          <stop offset="100%" stopColor="rgb(var(--info))" stopOpacity="0.65" />
        </linearGradient>
      </defs>
      <path d={contorno} fill="rgb(var(--info) / 0.07)" />
      <g clipPath={`url(#copo-${id})`}>
        <motion.g initial={{ y: fundo }} animate={{ y: nivel }} transition={{ type: 'spring', stiffness: 70, damping: 14 }}>
          <g className="agua-onda-lenta">
            <path d="M-100 -2 Q-75 -9 -50 -2 T0 -2 T50 -2 T100 -2 T150 -2 T200 -2 V140 H-100 Z" fill="rgb(var(--info) / 0.35)" />
          </g>
          <g className="agua-onda">
            <path d="M-100 0 Q-75 -6 -50 0 T0 0 T50 0 T100 0 T150 0 T200 0 V140 H-100 Z" fill={`url(#agua-${id})`} />
          </g>
          {pct > 8 &&
            [
              [32, 0.2, 2.2],
              [52, 1.1, 1.6],
              [68, 1.9, 2.6],
            ].map(([x, atraso, raio]) => (
              <circle
                key={x}
                className="agua-bolha"
                cx={x}
                cy={fundo - nivel - 4}
                r={raio}
                fill="white"
                opacity="0"
                style={{ animationDelay: `${atraso}s`, ['--sobe' as string]: `-${Math.max(0, fundo - nivel - 12)}px` }}
              />
            ))}
        </motion.g>
      </g>
      <path d={contorno} fill="none" stroke={cheio ? 'rgb(var(--info))' : 'rgb(var(--line))'} strokeWidth="3" strokeLinejoin="round" />
      <path d="M20 16 L26 100" stroke="white" strokeOpacity="0.25" strokeWidth="3" strokeLinecap="round" />
    </svg>
  );
}

/** Gotinhas que espirram do copo quando a meta e batida. */
function Festa({ chave }: { chave: number }) {
  const [ativa, setAtiva] = useState(false);
  const primeira = useRef(true);
  useEffect(() => {
    if (primeira.current) {
      primeira.current = false;
      return;
    }
    if (!chave) return;
    setAtiva(true);
    const t = setTimeout(() => setAtiva(false), 1400);
    return () => clearTimeout(t);
  }, [chave]);
  if (!ativa) return null;
  return (
    <div aria-hidden className="pointer-events-none absolute left-1/2 top-4">
      {Array.from({ length: 14 }, (_, i) => {
        const ang = (i / 14) * Math.PI * 2;
        return (
          <motion.span
            key={`${chave}-${i}`}
            className={clsx('absolute h-2.5 w-2.5 rounded-full', i % 3 ? 'bg-info' : 'bg-accent')}
            initial={{ x: 0, y: 0, opacity: 1, scale: 1.2 }}
            animate={{ x: Math.cos(ang) * 70, y: Math.sin(ang) * 54 - 10, opacity: 0, scale: 0.5 }}
            transition={{ duration: 1.1, ease: 'easeOut' }}
          />
        );
      })}
    </div>
  );
}
