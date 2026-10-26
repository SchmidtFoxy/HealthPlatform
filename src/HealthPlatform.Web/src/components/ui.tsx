import { AnimatePresence, motion } from 'framer-motion';
import clsx from 'clsx';
import { Loader2, Minus, Plus, X } from 'lucide-react';
import { ButtonHTMLAttributes, ReactNode, forwardRef, useEffect, useState } from 'react';

// ---------------------------------------------------------------- Botao

type Variante = 'primary' | 'ghost' | 'soft' | 'danger' | 'outline';

export const Button = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variante; size?: 'sm' | 'md' | 'lg'; loading?: boolean; icon?: ReactNode }
>(function Button({ variant = 'primary', size = 'md', loading, icon, className, children, disabled, ...rest }, ref) {
  return (
    <button
      ref={ref}
      disabled={disabled || loading}
      className={clsx(
        'inline-flex select-none items-center justify-center gap-2 rounded-full font-semibold transition active:scale-[0.97] disabled:cursor-not-allowed disabled:opacity-50',
        size === 'sm' && 'h-9 px-3.5 text-sm',
        size === 'md' && 'h-11 px-5 text-[15px]',
        size === 'lg' && 'h-14 px-6 text-base',
        variant === 'primary' && 'bg-accent text-accent-ink shadow-glow hover:brightness-110',
        variant === 'soft' && 'bg-accent/10 text-accent hover:bg-accent/20',
        variant === 'ghost' && 'text-fg hover:bg-raised',
        variant === 'outline' && 'border border-line bg-surface text-fg hover:bg-raised',
        variant === 'danger' && 'bg-bad/10 text-bad hover:bg-bad/20',
        className,
      )}
      {...rest}
    >
      {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : icon}
      {children}
    </button>
  );
});

export function IconButton({ label, className, children, ...rest }: ButtonHTMLAttributes<HTMLButtonElement> & { label: string }) {
  return (
    <button
      aria-label={label}
      title={label}
      className={clsx('grid h-10 w-10 place-items-center rounded-full text-fg transition hover:bg-raised active:scale-95', className)}
      {...rest}
    >
      {children}
    </button>
  );
}

// ---------------------------------------------------------------- Superficies

export function Card({ className, children, onClick }: { className?: string; children: ReactNode; onClick?: () => void }) {
  const Tag = onClick ? 'button' : 'div';
  return (
    <Tag
      onClick={onClick}
      className={clsx(
        'block w-full rounded-xl2 border border-line/70 bg-surface text-left',
        onClick && 'transition active:scale-[0.99] hover:border-accent/40',
        className,
      )}
    >
      {children}
    </Tag>
  );
}

export function SectionTitle({ children, action }: { children: ReactNode; action?: ReactNode }) {
  return (
    <div className="mb-3 mt-7 flex items-end justify-between gap-3">
      <h2 className="font-display text-[17px] font-semibold tracking-tight">{children}</h2>
      {action}
    </div>
  );
}

export function Pill({ tone = 'neutral', children, className }: { tone?: 'neutral' | 'accent' | 'ok' | 'warn' | 'bad' | 'info'; children: ReactNode; className?: string }) {
  return (
    <span
      className={clsx(
        'inline-flex items-center gap-1 whitespace-nowrap rounded-full px-2.5 py-0.5 text-[11px] font-semibold',
        tone === 'neutral' && 'bg-raised text-muted',
        tone === 'accent' && 'bg-accent/15 text-accent',
        tone === 'ok' && 'bg-ok/15 text-ok',
        tone === 'warn' && 'bg-warn/15 text-warn',
        tone === 'bad' && 'bg-bad/15 text-bad',
        tone === 'info' && 'bg-info/15 text-info',
        className,
      )}
    >
      {children}
    </span>
  );
}

export function Skeleton({ className }: { className?: string }) {
  return <div className={clsx('skeleton', className)} />;
}

export function Empty({ icon, title, children, action }: { icon?: ReactNode; title: string; children?: ReactNode; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center gap-2 rounded-xl2 border border-dashed border-line px-6 py-10 text-center">
      {icon && <div className="mb-1 grid h-12 w-12 place-items-center rounded-full bg-raised text-muted">{icon}</div>}
      <p className="font-display font-semibold">{title}</p>
      {children && <p className="max-w-xs text-sm text-muted">{children}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}

export function ErrorBox({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div className="rounded-xl2 border border-bad/30 bg-bad/10 p-4 text-sm text-bad">
      <p className="font-semibold">Não deu para carregar.</p>
      <p className="mt-1 opacity-90">{error instanceof Error ? error.message : 'Verifique a conexão e tente de novo.'}</p>
      {onRetry && (
        <Button variant="danger" size="sm" className="mt-3" onClick={onRetry}>
          Tentar de novo
        </Button>
      )}
    </div>
  );
}

// ---------------------------------------------------------------- Graficos simples

/** Anel de progresso. value e max na mesma unidade. */
export function Ring({
  value,
  max = 100,
  size = 120,
  stroke = 10,
  color = 'rgb(var(--accent))',
  track = 'rgb(var(--line) / 0.7)',
  children,
}: {
  value: number;
  max?: number;
  size?: number;
  stroke?: number;
  color?: string;
  track?: string;
  children?: ReactNode;
}) {
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const pct = Math.max(0, Math.min(1, max ? value / max : 0));
  return (
    <div className="relative grid place-items-center" style={{ width: size, height: size }}>
      <svg width={size} height={size} className="-rotate-90">
        <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke={track} strokeWidth={stroke} />
        <motion.circle
          cx={size / 2}
          cy={size / 2}
          r={r}
          fill="none"
          stroke={color}
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={c}
          initial={{ strokeDashoffset: c }}
          animate={{ strokeDashoffset: c * (1 - pct) }}
          transition={{ duration: 0.9, ease: [0.22, 1, 0.36, 1] }}
        />
      </svg>
      <div className="absolute inset-0 grid place-items-center text-center">{children}</div>
    </div>
  );
}

/** Barra horizontal de macro com meta. */
export function MacroBar({ label, value, goal, color, unit = 'g' }: { label: string; value: number; goal?: number | null; color: string; unit?: string }) {
  const pct = goal ? Math.min(1.15, value / goal) : 0;
  const passou = goal ? value > goal * 1.1 : false;
  return (
    <div className="min-w-0">
      <div className="mb-1.5 flex items-baseline justify-between gap-2 text-[13px]">
        <span className="text-muted">{label}</span>
        <span className="num font-semibold">
          {Math.round(value)}
          <span className="text-faint">{goal ? ` / ${Math.round(goal)}${unit}` : unit}</span>
        </span>
      </div>
      <div className="h-2 overflow-hidden rounded-full bg-line/60">
        <motion.div
          className="h-full rounded-full"
          style={{ background: passou ? 'rgb(var(--warn))' : color }}
          initial={{ width: 0 }}
          animate={{ width: `${Math.min(100, (goal ? pct : 0.0) * 100)}%` }}
          transition={{ duration: 0.8, ease: [0.22, 1, 0.36, 1] }}
        />
      </div>
    </div>
  );
}

/** Linha com area para series curtas (7 a 30 pontos). Pontos nulos viram lacunas. */
export function Sparkline({
  points,
  height = 56,
  min,
  max,
  color = 'rgb(var(--accent))',
  className,
}: {
  points: (number | null)[];
  height?: number;
  min?: number;
  max?: number;
  color?: string;
  className?: string;
}) {
  const w = 300;
  const vals = points.filter((p): p is number => p != null);
  if (vals.length < 2) return <div className={clsx('grid place-items-center text-xs text-faint', className)} style={{ height }}>Poucos dados ainda</div>;
  const lo = min ?? Math.min(...vals);
  const hi = max ?? Math.max(...vals);
  const span = hi - lo || 1;
  const step = w / Math.max(1, points.length - 1);
  const xy = points.map((p, i) => (p == null ? null : [i * step, height - 4 - ((p - lo) / span) * (height - 10)]));
  const segs: number[][][] = [];
  let cur: number[][] = [];
  xy.forEach((p) => {
    if (p) cur.push(p);
    else if (cur.length) {
      segs.push(cur);
      cur = [];
    }
  });
  if (cur.length) segs.push(cur);
  const last = [...xy].reverse().find(Boolean)!;
  const id = `g${Math.round(Math.random() * 1e9)}`;
  return (
    <svg viewBox={`0 0 ${w} ${height}`} preserveAspectRatio="none" className={clsx('w-full overflow-visible', className)} style={{ height }}>
      <defs>
        <linearGradient id={id} x1="0" x2="0" y1="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity="0.28" />
          <stop offset="100%" stopColor={color} stopOpacity="0" />
        </linearGradient>
      </defs>
      {segs.map((s, i) => (
        <g key={i}>
          {s.length > 1 && (
            <path d={`M${s[0][0]},${height} ` + s.map((p) => `L${p[0]},${p[1]}`).join(' ') + ` L${s[s.length - 1][0]},${height} Z`} fill={`url(#${id})`} />
          )}
          <path d={s.map((p, j) => `${j ? 'L' : 'M'}${p[0]},${p[1]}`).join(' ')} fill="none" stroke={color} strokeWidth="2.5" strokeLinejoin="round" strokeLinecap="round" vectorEffect="non-scaling-stroke" />
        </g>
      ))}
      <circle cx={last[0]} cy={last[1]} r="4" fill={color} stroke="rgb(var(--surface))" strokeWidth="2" vectorEffect="non-scaling-stroke" />
    </svg>
  );
}

// ---------------------------------------------------------------- Entradas

export function Stepper({
  value,
  onChange,
  step = 1,
  min = 0,
  max = 9999,
  suffix,
  label,
  big,
}: {
  value: number | null;
  onChange: (v: number) => void;
  step?: number;
  min?: number;
  max?: number;
  suffix?: string;
  label?: string;
  big?: boolean;
}) {
  const v = value ?? 0;
  const set = (n: number) => onChange(Math.max(min, Math.min(max, Math.round(n * 100) / 100)));
  return (
    <div className="flex min-w-0 flex-col items-center gap-1">
      {label && <span className="eyebrow">{label}</span>}
      <div className={clsx('flex items-center rounded-full border border-line bg-raised/60', big ? 'h-14' : 'h-11')}>
        <button type="button" aria-label={`Diminuir ${label ?? ''}`} onClick={() => set(v - step)} className={clsx('grid place-items-center rounded-full text-muted active:bg-line', big ? 'h-14 w-12' : 'h-11 w-10')}>
          <Minus className="h-4 w-4" />
        </button>
        <label className="flex items-baseline">
          <input
            inputMode="decimal"
            aria-label={label}
            className={clsx('num w-[3.2ch] bg-transparent text-center font-semibold outline-none', big ? 'text-2xl' : 'text-lg')}
            value={value ?? ''}
            onChange={(e) => {
              const n = Number(e.target.value.replace(',', '.'));
              if (!Number.isNaN(n)) onChange(n);
            }}
          />
          {suffix && <span className="text-xs text-muted">{suffix}</span>}
        </label>
        <button type="button" aria-label={`Aumentar ${label ?? ''}`} onClick={() => set(v + step)} className={clsx('grid place-items-center rounded-full text-muted active:bg-line', big ? 'h-14 w-12' : 'h-11 w-10')}>
          <Plus className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}

export function Segmented<T extends string>({ options, value, onChange, className }: { options: { value: T; label: ReactNode }[]; value: T; onChange: (v: T) => void; className?: string }) {
  return (
    <div className={clsx('relative flex rounded-full bg-raised p-1', className)} role="tablist">
      {options.map((o) => (
        <button
          key={o.value}
          role="tab"
          aria-selected={o.value === value}
          onClick={() => onChange(o.value)}
          className={clsx('relative z-10 flex-1 rounded-full px-3 py-2 text-sm font-semibold transition', o.value === value ? 'text-fg' : 'text-muted')}
        >
          {o.value === value && <motion.span layoutId={`seg-${options.map((x) => x.value).join()}`} className="absolute inset-0 -z-10 rounded-full bg-surface shadow-card" transition={{ type: 'spring', stiffness: 500, damping: 40 }} />}
          {o.label}
        </button>
      ))}
    </div>
  );
}

export function Field({ label, children, hint }: { label: string; children: ReactNode; hint?: string }) {
  return (
    <label className="flex min-w-0 flex-col gap-1.5">
      <span className="text-[13px] font-semibold text-muted">{label}</span>
      {children}
      {hint && <span className="text-xs text-faint">{hint}</span>}
    </label>
  );
}

export const inputCls =
  'h-11 w-full min-w-0 rounded-xl border border-line bg-surface px-3.5 text-base text-fg sm:text-[15px] outline-none transition placeholder:text-faint focus:border-accent focus:ring-2 focus:ring-accent/25';

// ---------------------------------------------------------------- Bottom sheet

export function Sheet({ open, onClose, title, children, footer, tall }: { open: boolean; onClose: () => void; title?: ReactNode; children: ReactNode; footer?: ReactNode; tall?: boolean }) {
  useEffect(() => {
    if (!open) return;
    const k = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', k);
    const prev = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      window.removeEventListener('keydown', k);
      document.body.style.overflow = prev;
    };
  }, [open, onClose]);
  return (
    <AnimatePresence>
      {open && (
        <div className="fixed inset-0 z-50 flex items-end justify-center sm:items-center">
          <motion.div className="absolute inset-0 bg-black/55 backdrop-blur-[2px]" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} onClick={onClose} />
          <motion.div
            role="dialog"
            aria-modal="true"
            className={clsx(
              'relative flex w-full max-w-lg flex-col rounded-t-xl3 border border-line bg-surface shadow-2xl sm:rounded-xl3',
              tall ? 'h-[92dvh] sm:h-[85dvh]' : 'max-h-[92dvh]',
            )}
            initial={{ y: '100%' }}
            animate={{ y: 0 }}
            exit={{ y: '100%' }}
            transition={{ type: 'spring', stiffness: 420, damping: 42 }}
            drag="y"
            dragConstraints={{ top: 0, bottom: 0 }}
            dragElastic={{ top: 0, bottom: 0.6 }}
            onDragEnd={(_, i) => (i.offset.y > 120 || i.velocity.y > 600) && onClose()}
          >
            <div className="mx-auto mt-2.5 h-1.5 w-10 shrink-0 rounded-full bg-line" />
            <div className="flex shrink-0 items-center justify-between gap-3 px-5 pb-2 pt-3">
              <div className="min-w-0 font-display text-lg font-semibold">{title}</div>
              <IconButton label="Fechar" onClick={onClose} className="-mr-2">
                <X className="h-5 w-5" />
              </IconButton>
            </div>
            <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain px-5 pb-5">{children}</div>
            {footer && <div className="shrink-0 border-t border-line px-5 pb-safe pt-3"><div className="pb-3">{footer}</div></div>}
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
}

// ---------------------------------------------------------------- Toast

let pushToast: ((t: { msg: string; tone?: 'ok' | 'bad' }) => void) | null = null;
export const toast = (msg: string, tone: 'ok' | 'bad' = 'ok') => pushToast?.({ msg, tone });


export function Toaster() {
  const [items, setItems] = useState<{ id: number; msg: string; tone?: 'ok' | 'bad' }[]>([]);
  useEffect(() => {
    pushToast = (t) => {
      const id = Date.now() + Math.random();
      setItems((x) => [...x, { id, ...t }]);
      setTimeout(() => setItems((x) => x.filter((i) => i.id !== id)), 3200);
    };
    return () => {
      pushToast = null;
    };
  }, []);
  return (
    <div className="pointer-events-none fixed inset-x-0 top-0 z-[60] flex flex-col items-center gap-2 px-gutter pt-safe">
      <AnimatePresence>
        {items.map((t) => (
          <motion.div
            key={t.id}
            initial={{ y: -40, opacity: 0 }}
            animate={{ y: 12, opacity: 1 }}
            exit={{ y: -40, opacity: 0 }}
            className={clsx(
              'pointer-events-auto rounded-full border px-4 py-2.5 text-sm font-semibold shadow-xl backdrop-blur',
              t.tone === 'bad' ? 'border-bad/40 bg-bad/90 text-white' : 'border-line bg-surface/95 text-fg',
            )}
          >
            {t.msg}
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  );
}
