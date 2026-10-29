import clsx from 'clsx';
import { motion } from 'framer-motion';
import { ChevronLeft, LucideIcon } from 'lucide-react';
import { ReactNode } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { ContaLateral } from './account';
import simbolo from '@/assets/aesyn-symbol.webp';

export interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  end?: boolean;
  /** Aparece so no menu lateral (PC) ou so na barra de baixo (celular). */
  only?: 'desktop' | 'mobile';
}

export function Logo({ className }: { className?: string }) {
  return (
    <div className={clsx('flex items-center gap-2.5', className)}>
      <img src={simbolo} alt="" className="h-8 w-8 rounded-[10px] object-cover" />
      <span className="font-display text-[15px] font-semibold tracking-[0.32em]">AESYN</span>
    </div>
  );
}

/** Navegacao inferior no celular e trilho lateral a partir de 1024px. */
export function AppShell({ items, footer, atalho }: { items: NavItem[]; footer?: ReactNode; atalho?: ReactNode }) {
  return (
    <div className="min-h-full lg:flex">
      <aside className="sticky top-0 hidden h-dvh w-60 shrink-0 flex-col border-r border-line bg-surface px-4 py-6 lg:flex">
        <Logo className="px-2" />
        <nav className="mt-8 flex flex-col gap-1">
          {items.filter((it) => it.only !== 'mobile').map((it) => (
            <NavLink
              key={it.to}
              to={it.to}
              end={it.end}
              className={({ isActive }) =>
                clsx('flex items-center gap-3 rounded-xl px-3 py-2.5 text-[15px] font-semibold transition', isActive ? 'bg-accent/15 text-accent' : 'text-muted hover:bg-raised hover:text-fg')
              }
            >
              <it.icon className="h-5 w-5" />
              {it.label}
            </NavLink>
          ))}
        </nav>
        <div className="mt-auto">
          {atalho}
          {footer ?? <ContaLateral />}
        </div>
      </aside>

      <main className="min-w-0 flex-1 pb-[calc(84px+env(safe-area-inset-bottom,0px))] lg:pb-10">
        <Outlet />
      </main>

      <nav className="fixed inset-x-0 bottom-0 z-40 border-t border-line/80 bg-surface/85 pb-safe backdrop-blur-xl lg:hidden">
        <div className="mx-auto flex max-w-lg px-gutter">
          {items.filter((it) => it.only !== 'desktop').map((it) => (
            <NavLink key={it.to} to={it.to} end={it.end} className="relative flex flex-1 flex-col items-center gap-1 pb-2 pt-2.5">
              {({ isActive }) => (
                <>
                  {isActive && <motion.span layoutId="tab-dot" className="absolute top-0 h-[3px] w-8 rounded-full bg-accent" transition={{ type: 'spring', stiffness: 500, damping: 38 }} />}
                  <it.icon className={clsx('h-[22px] w-[22px] transition', isActive ? 'text-accent' : 'text-muted')} strokeWidth={isActive ? 2.4 : 1.9} />
                  <span className={clsx('text-[11px] font-semibold', isActive ? 'text-fg' : 'text-muted')}>{it.label}</span>
                </>
              )}
            </NavLink>
          ))}
        </div>
      </nav>
    </div>
  );
}

/** Container de pagina com largura de leitura e respiro lateral. */
export function Page({ children, className, wide }: { children: ReactNode; className?: string; wide?: boolean }) {
  return <div className={clsx('mx-auto w-full px-gutter pb-8 pt-safe', wide ? 'max-w-5xl' : 'max-w-xl', className)}>{children}</div>;
}

export function PageHeader({ title, subtitle, back, action, eyebrow }: { title: ReactNode; subtitle?: ReactNode; back?: string | true; action?: ReactNode; eyebrow?: ReactNode }) {
  const nav = useNavigate();
  return (
    <header className="flex items-start justify-between gap-3 pb-4 pt-5">
      <div className="flex min-w-0 items-start gap-1">
        {back && (
          <button aria-label="Voltar" onClick={() => (back === true ? nav(-1) : nav(back))} className="-ml-2 mt-0.5 grid h-9 w-9 shrink-0 place-items-center rounded-full hover:bg-raised">
            <ChevronLeft className="h-6 w-6" />
          </button>
        )}
        <div className="min-w-0">
          {eyebrow && <div className="eyebrow mb-1">{eyebrow}</div>}
          <h1 className="font-display text-[26px] font-semibold leading-tight tracking-tight">{title}</h1>
          {subtitle && <p className="mt-1 text-[15px] text-muted">{subtitle}</p>}
        </div>
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </header>
  );
}
