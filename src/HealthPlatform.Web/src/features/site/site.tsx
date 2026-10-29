import { AnimatePresence, motion } from 'framer-motion';
import clsx from 'clsx';
import {
  Activity,
  ArrowRight,
  BellRing,
  ClipboardList,
  Dumbbell,
  FileHeart,
  History,
  LucideIcon,
  Menu,
  MessageCircle,
  ShieldCheck,
  Smartphone,
  Sparkles,
  Stethoscope,
  Salad,
  Target,
  UserRoundCheck,
  Users,
  X,
} from 'lucide-react';
import { ReactNode, useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import simbolo from '@/assets/aesyn-symbol.webp';

const ease = [0.22, 1, 0.36, 1] as const;

const links = [
  { to: '/', label: 'Início', end: true },
  { to: '/recursos', label: 'Recursos' },
  { to: '/vantagens', label: 'Vantagens' },
  { to: '/sobre', label: 'Quem somos' },
];

// ---------------------------------------------------------------- Moldura

/** Moldura das paginas publicas: sempre escura, com a identidade da marca. */
export function SiteLayout() {
  const loc = useLocation();
  const [menu, setMenu] = useState(false);
  const [rolou, setRolou] = useState(false);

  useEffect(() => {
    setMenu(false);
    window.scrollTo({ top: 0 });
  }, [loc.pathname]);

  useEffect(() => {
    const fn = () => setRolou(window.scrollY > 12);
    fn();
    window.addEventListener('scroll', fn, { passive: true });
    return () => window.removeEventListener('scroll', fn);
  }, []);

  return (
    <div data-theme="dark" className="min-h-full bg-[rgb(4_14_12)] text-fg">
      <header className={clsx('sticky top-0 z-50 pt-safe transition', rolou || menu ? 'border-b border-white/[0.06] bg-[rgb(4_14_12/0.82)] backdrop-blur-xl' : 'bg-transparent')}>
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-gutter">
          <Link to="/" className="flex items-center gap-2.5" aria-label="AESYN, início">
            <img src={simbolo} alt="" className="h-9 w-9 rounded-xl object-cover" />
            <span className="font-display text-[15px] font-semibold tracking-[0.42em]">AESYN</span>
          </Link>

          <nav className="hidden items-center gap-1 md:flex">
            {links.map((l) => (
              <NavLink
                key={l.to}
                to={l.to}
                end={l.end}
                className={({ isActive }) => clsx('rounded-full px-4 py-2 text-sm font-semibold transition', isActive ? 'bg-white/[0.08] text-fg' : 'text-muted hover:text-fg')}
              >
                {l.label}
              </NavLink>
            ))}
          </nav>

          <div className="flex items-center gap-2">
            <Link to="/entrar" className="inline-flex h-10 items-center gap-1.5 rounded-full bg-accent px-5 text-sm font-semibold text-accent-ink shadow-glow transition hover:brightness-110">
              Entrar
            </Link>
            <button aria-label={menu ? 'Fechar menu' : 'Abrir menu'} onClick={() => setMenu((v) => !v)} className="grid h-10 w-10 place-items-center rounded-full text-fg hover:bg-white/[0.06] md:hidden">
              {menu ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
            </button>
          </div>
        </div>

        <AnimatePresence>
          {menu && (
            <motion.nav initial={{ height: 0, opacity: 0 }} animate={{ height: 'auto', opacity: 1 }} exit={{ height: 0, opacity: 0 }} transition={{ duration: 0.25, ease }} className="overflow-hidden md:hidden">
              <div className="flex flex-col gap-1 px-gutter pb-5 pt-1">
                {links.map((l) => (
                  <NavLink
                    key={l.to}
                    to={l.to}
                    end={l.end}
                    className={({ isActive }) => clsx('rounded-2xl px-4 py-3.5 text-[17px] font-semibold', isActive ? 'bg-white/[0.08] text-fg' : 'text-muted')}
                  >
                    {l.label}
                  </NavLink>
                ))}
              </div>
            </motion.nav>
          )}
        </AnimatePresence>
      </header>

      <main>
        <Outlet />
      </main>

      <footer className="border-t border-white/[0.06] pb-safe">
        <div className="mx-auto grid max-w-6xl gap-8 px-gutter py-12 md:grid-cols-[1.4fr_1fr_1fr]">
          <div>
            <div className="flex items-center gap-2.5">
              <img src={simbolo} alt="" className="h-9 w-9 rounded-xl object-cover" />
              <span className="font-display text-[15px] font-semibold tracking-[0.42em]">AESYN</span>
            </div>
            <p className="mt-4 max-w-xs text-sm text-muted">Saúde, performance e longevidade, com o seu profissional no centro de cada decisão.</p>
          </div>
          <div>
            <p className="eyebrow">Navegação</p>
            <ul className="mt-3 space-y-2 text-sm">
              {links.map((l) => (
                <li key={l.to}>
                  <Link to={l.to} className="text-muted hover:text-fg">
                    {l.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
          <div>
            <p className="eyebrow">Acesso</p>
            <ul className="mt-3 space-y-2 text-sm">
              <li>
                <Link to="/entrar" className="text-muted hover:text-fg">
                  Entrar
                </Link>
              </li>
            </ul>
          </div>
        </div>
        <p className="mx-auto max-w-6xl px-gutter pb-8 text-xs text-faint">© {new Date().getFullYear()} AESYN Performance</p>
      </footer>
    </div>
  );
}

// ---------------------------------------------------------------- Pecas

function Revela({ children, className, delay = 0 }: { children: ReactNode; className?: string; delay?: number }) {
  return (
    <motion.div initial={{ opacity: 0, y: 24 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true, margin: '-60px' }} transition={{ duration: 0.7, delay, ease }} className={className}>
      {children}
    </motion.div>
  );
}

function Halo() {
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0 -z-10 overflow-hidden [mask-image:linear-gradient(to_bottom,black_60%,transparent)]">
      <div className="absolute -left-[20%] -top-[20%] h-[70%] w-[80%] rounded-full bg-[rgb(94_240_200/0.12)] blur-[110px]" />
      <div className="absolute -right-[20%] top-[30%] h-[70%] w-[70%] rounded-full bg-[rgb(15_90_80/0.5)] blur-[120px]" />
      <div className="absolute inset-0 bg-[radial-gradient(rgb(255_255_255/0.05)_1px,transparent_1px)] [background-size:22px_22px] [mask-image:radial-gradient(60%_50%_at_50%_30%,black,transparent)]" />
    </div>
  );
}

function Cabecalho({ eyebrow, titulo, texto }: { eyebrow: string; titulo: ReactNode; texto?: ReactNode }) {
  return (
    <section className="relative isolate">
      <Halo />
      <div className="mx-auto max-w-6xl px-gutter pb-12 pt-14 md:pb-16 md:pt-24">
        <Revela>
          <p className="text-[11px] font-semibold uppercase tracking-[0.32em] text-accent">{eyebrow}</p>
          <h1 className="mt-4 max-w-3xl font-display text-[38px] font-semibold leading-[1.05] tracking-tight md:text-[60px]">{titulo}</h1>
          {texto && <p className="mt-5 max-w-2xl text-[17px] leading-relaxed text-muted md:text-lg">{texto}</p>}
        </Revela>
      </div>
    </section>
  );
}

function Gradiente({ children }: { children: ReactNode }) {
  return <span className="bg-gradient-to-r from-[rgb(94_240_200)] to-[rgb(150_220_255)] bg-clip-text text-transparent">{children}</span>;
}

interface Item {
  icon: LucideIcon;
  titulo: string;
  texto: string;
  cor?: string;
}

function GradeCartoes({ itens, colunas = 3 }: { itens: Item[]; colunas?: 2 | 3 }) {
  return (
    <div className={clsx('grid gap-3 sm:grid-cols-2', colunas === 3 && 'lg:grid-cols-3')}>
      {itens.map((it, i) => (
        <Revela key={it.titulo} delay={0.05 * (i % 3)}>
          <div className="group h-full rounded-3xl border border-white/[0.08] bg-white/[0.03] p-6 transition hover:border-accent/30 hover:bg-white/[0.05]">
            <div className="grid h-11 w-11 place-items-center rounded-2xl bg-white/[0.06]" style={{ color: it.cor ?? 'rgb(var(--accent))' }}>
              <it.icon className="h-5 w-5" />
            </div>
            <p className="mt-5 font-display text-lg font-semibold">{it.titulo}</p>
            <p className="mt-2 text-[15px] leading-relaxed text-muted">{it.texto}</p>
          </div>
        </Revela>
      ))}
    </div>
  );
}

function ChamadaFinal() {
  return (
    <section className="mx-auto max-w-6xl px-gutter pb-20">
      <Revela>
        <div className="relative isolate overflow-hidden rounded-[32px] border border-accent/20 px-6 py-12 text-center md:px-16 md:py-16">
          <div aria-hidden className="absolute inset-0 -z-10 bg-[radial-gradient(80%_120%_at_50%_0%,rgb(94_240_200/0.18),transparent_70%)]" />
          <img src={simbolo} alt="" className="mx-auto h-16 w-16 object-cover [mask-image:radial-gradient(closest-side,black_62%,transparent)]" />
          <h2 className="mx-auto mt-4 max-w-xl font-display text-[30px] font-semibold leading-tight tracking-tight md:text-[40px]">Seu acompanhamento começa com um convite.</h2>
          <p className="mx-auto mt-3 max-w-lg text-muted">Quem acompanha você cria seu acesso. Já tem uma conta? Entre e veja o seu plano de hoje.</p>
          <Link to="/entrar" className="mt-8 inline-flex h-14 items-center gap-2 rounded-full bg-accent px-8 text-base font-semibold text-accent-ink shadow-glow transition hover:brightness-110">
            Entrar no AESYN <ArrowRight className="h-5 w-5" />
          </Link>
        </div>
      </Revela>
    </section>
  );
}

// ---------------------------------------------------------------- Inicio

export function HomeSitePage() {
  return (
    <>
      <section className="relative isolate">
        <Halo />
        <div className="mx-auto grid max-w-6xl items-center gap-10 px-gutter pb-16 pt-10 md:pt-16 lg:grid-cols-[1.1fr_1fr] lg:pb-24">
          <motion.div initial={{ opacity: 0, y: 24 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.8, ease }}>
            <p className="text-[11px] font-semibold uppercase tracking-[0.32em] text-accent">Saúde · Performance · Longevidade</p>
            <h1 className="mt-4 font-display text-[42px] font-semibold leading-[1.02] tracking-tight md:text-[68px]">
              Saúde e performance <Gradiente>em sincronia.</Gradiente>
            </h1>
            <p className="mt-5 max-w-xl text-[17px] leading-relaxed text-muted md:text-lg">
              Treino, alimentação, check-ins e o acompanhamento do seu médico num só app. Você sabe o que fazer hoje; seu profissional enxerga como você está evoluindo.
            </p>
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <Link to="/entrar" className="inline-flex h-14 items-center justify-center gap-2 rounded-full bg-accent px-8 text-base font-semibold text-accent-ink shadow-glow transition hover:brightness-110">
                Entrar <ArrowRight className="h-5 w-5" />
              </Link>
              <Link to="/recursos" className="inline-flex h-14 items-center justify-center rounded-full border border-white/10 px-8 text-base font-semibold transition hover:bg-white/[0.05]">
                Conhecer os recursos
              </Link>
            </div>
          </motion.div>

          <motion.div initial={{ opacity: 0, scale: 0.94 }} animate={{ opacity: 1, scale: 1 }} transition={{ duration: 1, delay: 0.1, ease }} className="relative mx-auto w-full max-w-[420px]">
            <MockCelular />
          </motion.div>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-gutter pb-20">
        <Revela>
          <p className="eyebrow">Para quem é</p>
          <h2 className="mt-3 max-w-2xl font-display text-[30px] font-semibold leading-tight tracking-tight md:text-[40px]">Dois lados do mesmo cuidado.</h2>
        </Revela>
        <div className="mt-8 grid gap-3 md:grid-cols-2">
          <Revela>
            <Publico
              icon={UserRoundCheck}
              titulo="Para quem é acompanhado"
              itens={['Seu dia em um olhar: treino, refeições e check-in', 'Treino guiado com descanso e registro de cargas', 'Dieta com macros, horários e substituições', 'Sua evolução em gráficos, semana a semana']}
            />
          </Revela>
          <Revela delay={0.08}>
            <Publico
              icon={Stethoscope}
              titulo="Para quem acompanha"
              itens={['Quem precisa de atenção hoje, e por quê', 'Editor de dieta com cálculo ao vivo de energia e macros', 'Editor de treino com volume semanal por grupo', 'Histórico preservado a cada nova versão do plano']}
            />
          </Revela>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-gutter pb-20">
        <Revela>
          <p className="eyebrow">Como funciona</p>
          <h2 className="mt-3 font-display text-[30px] font-semibold leading-tight tracking-tight md:text-[40px]">Do consultório para o seu dia.</h2>
        </Revela>
        <ol className="mt-8 grid gap-3 md:grid-cols-3">
          {[
            ['Seu profissional monta o plano', 'Dieta e treino feitos para você, com metas de energia, macros e volume.'],
            ['Você executa no dia a dia', 'Check-in rápido de manhã, marca refeições com um toque e registra o treino.'],
            ['Os dois acompanham a evolução', 'Adesão, cargas e sinais do corpo viram contexto para a próxima consulta.'],
          ].map(([t, d], i) => (
            <Revela key={t} delay={0.06 * i}>
              <li className="h-full rounded-3xl border border-white/[0.08] bg-white/[0.03] p-6">
                <span className="num text-[13px] font-semibold text-accent">0{i + 1}</span>
                <p className="mt-3 font-display text-lg font-semibold">{t}</p>
                <p className="mt-2 text-[15px] leading-relaxed text-muted">{d}</p>
              </li>
            </Revela>
          ))}
        </ol>
      </section>

      <ChamadaFinal />
    </>
  );
}

function Publico({ icon: Icon, titulo, itens }: { icon: LucideIcon; titulo: string; itens: string[] }) {
  return (
    <div className="h-full rounded-3xl border border-white/[0.08] bg-white/[0.03] p-6 md:p-8">
      <div className="grid h-12 w-12 place-items-center rounded-2xl bg-accent/15 text-accent">
        <Icon className="h-6 w-6" />
      </div>
      <p className="mt-5 font-display text-xl font-semibold">{titulo}</p>
      <ul className="mt-4 space-y-3">
        {itens.map((t) => (
          <li key={t} className="flex gap-3 text-[15px] text-muted">
            <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-accent" />
            {t}
          </li>
        ))}
      </ul>
    </div>
  );
}

/** Ilustracao do app no celular, feita em HTML para ficar nitida em qualquer tela. */
function MockCelular() {
  const C = 2 * Math.PI * 42;
  return (
    <div aria-hidden className="relative">
      <div className="absolute -inset-10 -z-10 rounded-full bg-[rgb(94_240_200/0.15)] blur-[80px]" />
      <div className="mx-auto w-[300px] rounded-[44px] border border-white/10 bg-[rgb(8_22_20)] p-3 shadow-[0_40px_80px_-30px_rgb(0_0_0/0.9)]">
        <div className="rounded-[34px] bg-[rgb(5_16_14)] px-4 pb-5 pt-6">
          <p className="text-[11px] text-white/45">Segunda-feira, 26 de outubro</p>
          <p className="font-display text-[20px] font-semibold">Bom dia, Marina</p>

          <div className="mt-4 flex items-center gap-4 rounded-3xl border border-white/[0.08] bg-[radial-gradient(120%_80%_at_10%_0%,rgb(94_240_200/0.18),transparent_60%)] p-4">
            <div className="relative h-[92px] w-[92px] shrink-0">
              <svg viewBox="0 0 100 100" className="h-full w-full -rotate-90">
                <circle cx="50" cy="50" r="42" fill="none" stroke="rgb(255 255 255 / 0.1)" strokeWidth="9" />
                <motion.circle
                  cx="50"
                  cy="50"
                  r="42"
                  fill="none"
                  stroke="rgb(92 220 140)"
                  strokeWidth="9"
                  strokeLinecap="round"
                  strokeDasharray={C}
                  initial={{ strokeDashoffset: C }}
                  animate={{ strokeDashoffset: C * 0.18 }}
                  transition={{ delay: 0.6, duration: 1.4, ease }}
                />
              </svg>
              <span className="num absolute inset-0 grid place-items-center text-[26px] font-semibold">82</span>
            </div>
            <div>
              <p className="text-[13px] font-semibold leading-snug">Pronto para treinar forte</p>
              <p className="mt-1 text-[11px] text-white/45">7h30 de sono · energia 8</p>
            </div>
          </div>

          <div className="mt-3 rounded-3xl border border-white/[0.08] bg-white/[0.03] p-4">
            <p className="text-[10px] font-semibold uppercase tracking-wider text-white/45">Treino de hoje</p>
            <p className="mt-1 font-display text-[16px] font-semibold">A · Inferiores</p>
            <div className="mt-3 flex h-10 items-center justify-center rounded-full bg-accent text-[13px] font-semibold text-accent-ink">Começar treino</div>
          </div>

          <div className="mt-3 space-y-2">
            {[
              ['07:00', 'Café da manhã', 'ok'],
              ['12:30', 'Almoço', 'next'],
              ['16:30', 'Pré-treino', ''],
            ].map(([h, n, s]) => (
              <div key={h} className={clsx('flex items-center gap-3 rounded-2xl px-2 py-1.5', s === 'next' && 'bg-accent/[0.08]')}>
                <span className={clsx('num grid h-9 w-12 place-items-center rounded-xl text-[11px] font-semibold', s === 'ok' ? 'bg-[rgb(92_220_140/0.15)] text-[rgb(92_220_140)]' : 'border border-white/10')}>{h}</span>
                <span className="text-[13px] font-semibold">{n}</span>
              </div>
            ))}
          </div>

          <div className="mt-4 flex justify-around border-t border-white/[0.06] pt-3 text-white/40">
            {[Sparkles, Dumbbell, Salad, Activity, MessageCircle].map((I, i) => (
              <I key={i} className={clsx('h-5 w-5', i === 0 && 'text-accent')} />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------- Recursos

export function RecursosPage() {
  const paciente: Item[] = [
    { icon: Sparkles, titulo: 'Check-in de prontidão', texto: 'Sono, energia, dor e recuperação em poucos toques. O app traduz em uma nota do dia e ajusta a orientação.' },
    { icon: Dumbbell, titulo: 'Treino ao vivo', texto: 'Uma série de cada vez, com carga, repetições, técnica e contagem do descanso. Funciona bem com uma mão só.', cor: 'rgb(130 160 255)' },
    { icon: Salad, titulo: 'Plano alimentar', texto: 'Refeições por horário, macros e quantidades. Marque o que fez, o que adaptou ou pulou com um toque.', cor: 'rgb(255 200 90)' },
    { icon: Activity, titulo: 'Progresso', texto: 'Peso, adesão e cargas ao longo das semanas, em gráficos que você entende de primeira.', cor: 'rgb(255 120 160)' },
    { icon: MessageCircle, titulo: 'Conversa com o profissional', texto: 'Dúvidas e ajustes direto com quem acompanha você, sem se perder no meio de outros aplicativos.' },
    { icon: Target, titulo: 'Metas e consistência', texto: 'Sequência de dias e evolução de nível para reconhecer constância, não excesso.', cor: 'rgb(255 186 80)' },
  ];
  const profissional: Item[] = [
    { icon: Users, titulo: 'Fila de atenção', texto: 'Quem não fez check-in, quem caiu de adesão e quem relatou dor, com o motivo de cada alerta.' },
    { icon: ClipboardList, titulo: 'Editor de dieta', texto: 'Monte refeições com busca de alimentos, veja energia e macros fecharem ao vivo contra a meta e gere substituições equivalentes.', cor: 'rgb(255 200 90)' },
    { icon: Dumbbell, titulo: 'Editor de treino', texto: 'Sessões por dia da semana, técnicas avançadas e volume semanal por grupo muscular enquanto você monta.', cor: 'rgb(130 160 255)' },
    { icon: History, titulo: 'Versões sem perder histórico', texto: 'Ao mudar um plano que já tem execuções, o AESYN publica uma nova versão e preserva tudo o que foi feito.', cor: 'rgb(150 220 255)' },
    { icon: FileHeart, titulo: 'Ficha completa', texto: 'Anamnese, exames, fotos de evolução e arquivos do paciente no mesmo lugar.', cor: 'rgb(255 120 160)' },
    { icon: BellRing, titulo: 'Sinais do dia', texto: 'Um resumo do que mudou desde ontem para começar o dia sabendo onde agir.' },
  ];
  return (
    <>
      <Cabecalho
        eyebrow="Recursos"
        titulo={
          <>
            Tudo o que o acompanhamento precisa, <Gradiente>nada que atrapalhe.</Gradiente>
          </>
        }
        texto="O AESYN junta o que o paciente faz no dia a dia com o que o profissional precisa ver para decidir."
      />
      <section className="mx-auto max-w-6xl px-gutter pb-16">
        <Revela>
          <h2 className="font-display text-2xl font-semibold tracking-tight">Para quem é acompanhado</h2>
        </Revela>
        <div className="mt-6">
          <GradeCartoes itens={paciente} />
        </div>
      </section>
      <section className="mx-auto max-w-6xl px-gutter pb-20">
        <Revela>
          <h2 className="font-display text-2xl font-semibold tracking-tight">Para quem acompanha</h2>
        </Revela>
        <div className="mt-6">
          <GradeCartoes itens={profissional} />
        </div>
      </section>
      <ChamadaFinal />
    </>
  );
}

// ---------------------------------------------------------------- Vantagens

export function VantagensPage() {
  const itens: Item[] = [
    { icon: Stethoscope, titulo: 'O profissional decide', texto: 'O app organiza dados e sugere atenção, mas quem prescreve e ajusta é sempre o seu médico ou profissional.' },
    { icon: History, titulo: 'Nada se perde', texto: 'Planos ganham versões em vez de serem sobrescritos. O histórico de treinos, refeições e check-ins fica guardado.', cor: 'rgb(150 220 255)' },
    { icon: ShieldCheck, titulo: 'Privacidade de verdade', texto: 'Cada paciente só vê os próprios dados, cada profissional só vê quem acompanha, e ações sensíveis ficam registradas.', cor: 'rgb(92 220 140)' },
    { icon: Smartphone, titulo: 'Um app no seu celular', texto: 'Instale pelo navegador, sem loja de aplicativos. Abre rápido, com tema claro e escuro.', cor: 'rgb(130 160 255)' },
    { icon: Sparkles, titulo: 'Feito para o dia a dia', texto: 'Check-in em segundos, refeição marcada com um toque e treino guiado. Menos formulário, mais constância.', cor: 'rgb(255 200 90)' },
    { icon: Activity, titulo: 'Contexto na consulta', texto: 'Semanas de adesão, cargas e sinais do corpo organizados para a consulta render mais.', cor: 'rgb(255 120 160)' },
  ];
  return (
    <>
      <Cabecalho
        eyebrow="Vantagens"
        titulo={
          <>
            Mais constância para você. <Gradiente>Mais contexto para quem cuida.</Gradiente>
          </>
        }
        texto="O que muda quando o plano sai do papel e passa a acompanhar o seu dia."
      />
      <section className="mx-auto max-w-6xl px-gutter pb-20">
        <GradeCartoes itens={itens} />
      </section>
      <ChamadaFinal />
    </>
  );
}

// ---------------------------------------------------------------- Quem somos

export function SobrePage() {
  return (
    <>
      <Cabecalho
        eyebrow="Quem somos"
        titulo={
          <>
            Conhecer você melhor <Gradiente>a cada dia.</Gradiente>
          </>
        }
        texto="O AESYN nasceu da medicina do esporte: acompanhar saúde e performance de perto, com consentimento, contexto e utilidade."
      />
      <section className="mx-auto grid max-w-6xl gap-10 px-gutter pb-20 lg:grid-cols-[1fr_1fr]">
        <Revela>
          <div className="space-y-5 text-[17px] leading-relaxed text-muted">
            <p>
              Consulta é um momento. A evolução acontece nos dias entre uma consulta e outra: no treino que foi ou não foi feito, na refeição adaptada, na noite mal dormida, na dor que apareceu.
            </p>
            <p>
              O AESYN existe para ligar esses dias ao cuidado. O profissional prescreve; você executa com clareza; e os dois acompanham o que realmente aconteceu, sem planilhas, mensagens soltas ou papel perdido.
            </p>
            <p className="text-fg">Tecnologia a serviço do julgamento clínico, nunca no lugar dele.</p>
          </div>
        </Revela>
        <Revela delay={0.08}>
          <div className="grid gap-3">
            {[
              ['Missão', 'Transformar orientação profissional em evolução real, um dia de cada vez.'],
              ['Como pensamos', 'Celular primeiro, histórico preservado, alertas que explicam o porquê e gamificação que premia constância, não excesso.'],
              ['O que nos guia', 'O profissional no centro da decisão, privacidade em cada tela e respeito ao tempo de quem usa.'],
            ].map(([t, d]) => (
              <div key={t} className="rounded-3xl border border-white/[0.08] bg-white/[0.03] p-6">
                <p className="eyebrow text-accent">{t}</p>
                <p className="mt-2 text-[15px] leading-relaxed text-fg/90">{d}</p>
              </div>
            ))}
          </div>
        </Revela>
      </section>
      <ChamadaFinal />
    </>
  );
}
