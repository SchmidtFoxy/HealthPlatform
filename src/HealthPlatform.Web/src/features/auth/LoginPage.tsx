import { AnimatePresence, motion } from 'framer-motion';
import clsx from 'clsx';
import { ArrowLeft, ArrowRight, Check, Dumbbell, Eye, EyeOff, Lock, Mail, MailCheck, Salad, Sparkles } from 'lucide-react';
import { FormEvent, InputHTMLAttributes, ReactNode, useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import simbolo from '@/assets/aesyn-symbol.webp';
import { Button } from '@/components/ui';
import { api, entrar, pedirNovaSenha } from '@/lib/api';

type Etapa = 'entrar' | 'esqueci' | 'enviado';

const CHAVE_EMAIL = 'aesyn_email_lembrado';
const ease = [0.22, 1, 0.36, 1] as const;

function emailLembrado() {
  try {
    return localStorage.getItem(CHAVE_EMAIL) ?? '';
  } catch {
    return '';
  }
}

export function LoginPage() {
  const nav = useNavigate();
  const [params] = useSearchParams();
  const [etapa, setEtapa] = useState<Etapa>('entrar');
  const [email, setEmail] = useState(() => params.get('email') ?? emailLembrado());
  const [senha, setSenha] = useState('');
  const [lembrar, setLembrar] = useState(() => emailLembrado() !== '');
  const [ver, setVer] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [carregando, setCarregando] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      const s = await entrar(email.trim(), senha);
      try {
        if (lembrar) localStorage.setItem(CHAVE_EMAIL, email.trim());
        else localStorage.removeItem(CHAVE_EMAIL);
      } catch {
        /* sem armazenamento: so nao lembra o e-mail */
      }
      nav(s.tipoUsuario === 'Paciente' ? '/hoje' : '/pro', { replace: true });
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Não foi possível entrar.');
    } finally {
      setCarregando(false);
    }
  }

  async function recuperar(e: FormEvent) {
    e.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      await pedirNovaSenha(email.trim());
      setEtapa('enviado');
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Não foi possível enviar agora.');
    } finally {
      setCarregando(false);
    }
  }

  function irPara(e: Etapa) {
    setErro(null);
    setEtapa(e);
  }

  return (
    <div className="min-h-full bg-bg lg:grid lg:grid-cols-[1.1fr_1fr]">
      <PainelMarca />

      {/* Formulario: no celular sobe como um cartao sobre a marca; no PC ocupa a coluna da direita. */}
      <main className="relative z-10 -mt-8 flex flex-col rounded-t-[32px] bg-bg px-gutter pb-safe pt-8 shadow-[0_-24px_60px_-30px_rgb(0_0_0/0.6)] lg:mt-0 lg:items-center lg:justify-center lg:rounded-none lg:px-16 lg:shadow-none">
        <div className="mx-auto w-full max-w-[400px] pb-8">
          <AnimatePresence mode="wait" initial={false}>
            {etapa === 'entrar' && (
              <Passo key="entrar">
                <h2 className="font-display text-[28px] font-semibold tracking-tight">Bem-vindo de volta</h2>
                <p className="mt-1.5 text-[15px] text-muted">Entre para ver seu plano de hoje.</p>

                <form onSubmit={submit} className="mt-7 flex flex-col gap-4">
                  <Campo
                    label="E-mail"
                    icon={<Mail className="h-[18px] w-[18px]" />}
                    type="email"
                    autoComplete="email"
                    inputMode="email"
                    autoCapitalize="none"
                    placeholder="voce@email.com"
                    required
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                  <Campo
                    label="Senha"
                    icon={<Lock className="h-[18px] w-[18px]" />}
                    type={ver ? 'text' : 'password'}
                    autoComplete="current-password"
                    placeholder="Sua senha"
                    autoFocus={!!params.get('email')}
                    required
                    value={senha}
                    onChange={(e) => setSenha(e.target.value)}
                    trailing={
                      <button
                        type="button"
                        aria-label={ver ? 'Ocultar senha' : 'Mostrar senha'}
                        onClick={() => setVer((v) => !v)}
                        className="grid h-10 w-10 place-items-center rounded-full text-muted transition hover:bg-raised hover:text-fg"
                      >
                        {ver ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
                      </button>
                    }
                  />

                  <div className="flex items-center justify-between gap-3">
                    <label className="flex cursor-pointer select-none items-center gap-2.5 text-sm text-muted">
                      <input type="checkbox" checked={lembrar} onChange={(e) => setLembrar(e.target.checked)} className="peer sr-only" />
                      <span className="grid h-5 w-5 place-items-center rounded-md border border-line bg-surface text-accent-ink transition peer-checked:border-accent peer-checked:bg-accent peer-focus-visible:ring-2 peer-focus-visible:ring-accent/40">
                        {lembrar && <Check className="h-3.5 w-3.5" strokeWidth={3} />}
                      </span>
                      Lembrar e-mail
                    </label>
                    <button type="button" onClick={() => irPara('esqueci')} className="text-sm font-semibold text-accent hover:underline">
                      Esqueci a senha
                    </button>
                  </div>

                  <Erro texto={erro} />

                  <Button type="submit" size="lg" loading={carregando} className="mt-1 w-full">
                    Entrar <ArrowRight className="h-5 w-5" />
                  </Button>
                </form>

                <div className="mt-8 rounded-2xl border border-line/70 bg-surface p-4">
                  <p className="text-sm font-semibold">Primeiro acesso?</p>
                  <p className="mt-1 text-sm text-muted">Seu profissional envia um convite por e-mail. Abra o link do convite para criar sua senha.</p>
                </div>

                <p className="mt-6 text-center text-xs text-faint">
                  Prefere o visual antigo?{' '}
                  <a href="/?classico=1" className="font-semibold text-muted hover:text-fg">
                    Abrir versão clássica
                  </a>
                </p>
              </Passo>
            )}

            {etapa === 'esqueci' && (
              <Passo key="esqueci">
                <Voltar onClick={() => irPara('entrar')} />
                <h2 className="mt-4 font-display text-[28px] font-semibold tracking-tight">Recuperar senha</h2>
                <p className="mt-1.5 text-[15px] text-muted">Informe seu e-mail e enviaremos um link para criar uma nova senha.</p>
                <form onSubmit={recuperar} className="mt-7 flex flex-col gap-4">
                  <Campo
                    label="E-mail"
                    icon={<Mail className="h-[18px] w-[18px]" />}
                    type="email"
                    autoComplete="email"
                    inputMode="email"
                    autoCapitalize="none"
                    placeholder="voce@email.com"
                    required
                    autoFocus
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                  <Erro texto={erro} />
                  <Button type="submit" size="lg" loading={carregando} className="mt-1 w-full">
                    Enviar link
                  </Button>
                </form>
              </Passo>
            )}

            {etapa === 'enviado' && (
              <Passo key="enviado">
                <div className="flex flex-col items-center text-center">
                  <motion.div
                    initial={{ scale: 0.6, opacity: 0 }}
                    animate={{ scale: 1, opacity: 1 }}
                    transition={{ type: 'spring', stiffness: 220, damping: 16 }}
                    className="grid h-16 w-16 place-items-center rounded-full bg-accent/15 text-accent"
                  >
                    <MailCheck className="h-8 w-8" />
                  </motion.div>
                  <h2 className="mt-5 font-display text-[26px] font-semibold tracking-tight">Confira seu e-mail</h2>
                  <p className="mt-2 max-w-xs text-[15px] text-muted">
                    Se existir uma conta ativa para <span className="font-semibold text-fg">{email.trim()}</span>, o link para criar uma nova senha chega em alguns minutos.
                  </p>
                  <p className="mt-2 text-sm text-faint">Não chegou? Olhe também a caixa de spam.</p>
                  <Button variant="outline" size="lg" className="mt-8 w-full" onClick={() => irPara('entrar')}>
                    Voltar para entrar
                  </Button>
                </div>
              </Passo>
            )}
          </AnimatePresence>
        </div>
      </main>
    </div>
  );
}

// ---------------------------------------------------------------- Marca

function PainelMarca() {
  return (
    <section
      data-theme="dark"
      className="relative isolate flex min-h-[44vh] flex-col overflow-hidden bg-[rgb(4_14_12)] px-gutter pb-14 pt-safe text-[rgb(230_243_240)] lg:min-h-full lg:px-14 lg:pb-14"
    >
      {/* Halos de luz atras do simbolo */}
      <div aria-hidden className="pointer-events-none absolute inset-0 -z-10">
        <div className="absolute -left-[20%] -top-[30%] h-[80%] w-[90%] rounded-full bg-[rgb(94_240_200/0.14)] blur-[100px]" />
        <div className="absolute -bottom-[30%] right-[-20%] h-[80%] w-[80%] rounded-full bg-[rgb(15_90_80/0.55)] blur-[120px]" />
        <div className="absolute inset-0 bg-[radial-gradient(rgb(255_255_255/0.05)_1px,transparent_1px)] [background-size:22px_22px] [mask-image:radial-gradient(70%_60%_at_50%_40%,black,transparent)]" />
      </div>

      <header className="flex items-center justify-between gap-3 pt-5 lg:pt-10">
        <Link to="/" className="flex items-center gap-3" aria-label="Conhecer o AESYN">
          <img src={simbolo} alt="" className="h-9 w-9 rounded-xl object-cover" />
          <span className="font-display text-[15px] font-semibold tracking-[0.42em]">AESYN</span>
        </Link>
        <Link to="/recursos" className="rounded-full border border-white/10 px-4 py-2 text-sm font-semibold text-[rgb(200_220_215)] transition hover:bg-white/[0.06]">
          Conheça o AESYN
        </Link>
      </header>

      <div className="flex flex-1 flex-col items-center justify-center pt-4 lg:items-start lg:pt-0">
        <motion.img
          src={simbolo}
          alt="AESYN"
          initial={{ opacity: 0, scale: 0.85, y: 10 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          transition={{ duration: 1, ease }}
          className="h-40 w-40 object-cover [mask-image:radial-gradient(closest-side,black_62%,transparent)] lg:h-64 lg:w-64 lg:-ml-6"
        />
        <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.2, duration: 0.8, ease }} className="text-center lg:text-left">
          <p className="mt-2 text-[11px] font-semibold uppercase tracking-[0.32em] text-[rgb(94_240_200)] lg:mt-4">Saúde · Performance · Longevidade</p>
          <h1 className="mt-3 hidden font-display text-[56px] font-semibold leading-[1.02] tracking-tight lg:block">
            Saúde e performance
            <br />
            <span className="bg-gradient-to-r from-[rgb(94_240_200)] to-[rgb(150_220_255)] bg-clip-text text-transparent">em sincronia.</span>
          </h1>
          <p className="mt-5 hidden max-w-md text-[17px] leading-relaxed text-[rgb(140_168_162)] lg:block">
            Treino, alimentação e o acompanhamento do seu profissional num só lugar, no ritmo do seu dia.
          </p>
        </motion.div>

        <Vitrine />
      </div>
    </section>
  );
}

/** Amostra decorativa do produto, so no PC. */
function Vitrine() {
  const cartao = 'rounded-2xl border border-white/10 bg-white/[0.06] p-4 backdrop-blur-xl shadow-[0_20px_50px_-20px_rgb(0_0_0/0.7)]';
  return (
    <div aria-hidden className="mt-12 hidden w-full max-w-xl grid-cols-3 gap-3 lg:grid">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.45, duration: 0.8, ease }} className={cartao}>
        <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-white/50">
          <Sparkles className="h-3.5 w-3.5 text-[rgb(94_240_200)]" /> Prontidão
        </div>
        <div className="relative mx-auto mt-3 h-20 w-20">
          <svg viewBox="0 0 80 80" className="h-20 w-20 -rotate-90">
            <circle cx="40" cy="40" r="33" fill="none" stroke="rgb(255 255 255 / 0.1)" strokeWidth="7" />
            <motion.circle
              cx="40"
              cy="40"
              r="33"
              fill="none"
              stroke="rgb(94 240 200)"
              strokeWidth="7"
              strokeLinecap="round"
              strokeDasharray={2 * Math.PI * 33}
              initial={{ strokeDashoffset: 2 * Math.PI * 33 }}
              animate={{ strokeDashoffset: 2 * Math.PI * 33 * 0.18 }}
              transition={{ delay: 0.8, duration: 1.4, ease }}
            />
          </svg>
          <span className="num absolute inset-0 grid place-items-center text-2xl font-semibold">82</span>
        </div>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.55, duration: 0.8, ease }} className={cartao + ' translate-y-6'}>
        <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-white/50">
          <Salad className="h-3.5 w-3.5 text-[rgb(255_200_90)]" /> Hoje
        </div>
        <p className="num mt-3 text-[26px] font-semibold">
          1.840<span className="ml-1 text-sm font-normal text-white/50">kcal</span>
        </p>
        {[
          ['P', 'rgb(255 120 160)', 0.8],
          ['C', 'rgb(255 200 90)', 0.62],
          ['G', 'rgb(130 160 255)', 0.45],
        ].map(([l, c, v]) => (
          <div key={l as string} className="mt-2 flex items-center gap-2">
            <span className="w-3 text-[10px] font-bold text-white/50">{l}</span>
            <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-white/10">
              <motion.div className="h-full rounded-full" style={{ background: c as string }} initial={{ width: 0 }} animate={{ width: `${(v as number) * 100}%` }} transition={{ delay: 0.9, duration: 1.2, ease }} />
            </div>
          </div>
        ))}
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.65, duration: 0.8, ease }} className={cartao}>
        <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-white/50">
          <Dumbbell className="h-3.5 w-3.5 text-[rgb(130_160_255)]" /> Treino A
        </div>
        <p className="mt-3 text-sm font-semibold">Agachamento livre</p>
        <p className="num mt-1 text-[26px] font-semibold">
          4×8<span className="ml-1.5 text-sm font-normal text-white/50">80 kg</span>
        </p>
        <div className="mt-2 flex gap-1">
          {[1, 1, 1, 0].map((f, i) => (
            <span key={i} className={clsx('h-1.5 flex-1 rounded-full', f ? 'bg-[rgb(94_240_200)]' : 'bg-white/10')} />
          ))}
        </div>
      </motion.div>
    </div>
  );
}

// ---------------------------------------------------------------- Pecas

function Passo({ children }: { children: ReactNode }) {
  return (
    <motion.div initial={{ opacity: 0, x: 24 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -24 }} transition={{ duration: 0.28, ease }}>
      {children}
    </motion.div>
  );
}

function Campo({ label, icon, trailing, className, ...rest }: InputHTMLAttributes<HTMLInputElement> & { label: string; icon: ReactNode; trailing?: ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-[13px] font-semibold text-muted">{label}</span>
      <span className="group flex h-14 items-center gap-3 rounded-2xl border border-line bg-surface pl-4 pr-2 transition focus-within:border-accent focus-within:ring-4 focus-within:ring-accent/15">
        <span className="text-faint transition group-focus-within:text-accent">{icon}</span>
        <input {...rest} className={clsx('h-full min-w-0 flex-1 bg-transparent text-base text-fg outline-none placeholder:text-faint', className)} />
        {trailing}
      </span>
    </label>
  );
}

function Erro({ texto }: { texto: string | null }) {
  return (
    <AnimatePresence>
      {texto && (
        <motion.p
          role="alert"
          initial={{ opacity: 0, height: 0 }}
          animate={{ opacity: 1, height: 'auto' }}
          exit={{ opacity: 0, height: 0 }}
          className="overflow-hidden rounded-xl bg-bad/10 px-4 py-3 text-sm text-bad"
        >
          {texto}
        </motion.p>
      )}
    </AnimatePresence>
  );
}

function Voltar({ onClick }: { onClick: () => void }) {
  return (
    <button type="button" onClick={onClick} className="-ml-2 inline-flex items-center gap-1.5 rounded-full px-2 py-1.5 text-sm font-semibold text-muted transition hover:bg-raised hover:text-fg">
      <ArrowLeft className="h-4 w-4" /> Voltar
    </button>
  );
}

// ---------------------------------------------------------------- Ativacao do convite

/** Regras de senha do servidor (Identity): tamanho, maiuscula, minuscula, numero e simbolo. */
export const REGRAS_SENHA: { l: string; ok: (s: string) => boolean }[] = [
  { l: '10 caracteres ou mais', ok: (s) => s.length >= 10 },
  { l: 'Letra maiúscula', ok: (s) => /[A-Z]/.test(s) },
  { l: 'Letra minúscula', ok: (s) => /[a-z]/.test(s) },
  { l: 'Número', ok: (s) => /\d/.test(s) },
  { l: 'Símbolo, como ! ou @', ok: (s) => /[^A-Za-z0-9]/.test(s) },
];

/** Link do convite do profissional: o paciente cria a senha e ja entra no app. */
export function AtivarPage() {
  const nav = useNavigate();
  const [params] = useSearchParams();
  const email = params.get('email') ?? '';
  const token = params.get('token') ?? '';
  const [senha, setSenha] = useState('');
  const [senha2, setSenha2] = useState('');
  const [ver, setVer] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [carregando, setCarregando] = useState(false);
  const valida = REGRAS_SENHA.every((r) => r.ok(senha));

  async function submit(e: FormEvent) {
    e.preventDefault();
    setErro(null);
    if (!valida) return setErro('A senha ainda não atende a todos os requisitos.');
    if (senha !== senha2) return setErro('As senhas não conferem.');
    setCarregando(true);
    try {
      await api('/api/auth/paciente/ativar', { method: 'POST', json: { email, token, senha } });
      try {
        await entrar(email, senha);
        nav('/hoje', { replace: true });
      } catch {
        nav(`/entrar?email=${encodeURIComponent(email)}`, { replace: true });
      }
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Não foi possível ativar agora.');
    } finally {
      setCarregando(false);
    }
  }

  const olho = (
    <button type="button" aria-label={ver ? 'Ocultar senha' : 'Mostrar senha'} onClick={() => setVer((v) => !v)} className="grid h-10 w-10 place-items-center rounded-full text-muted transition hover:bg-raised hover:text-fg">
      {ver ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
    </button>
  );

  return (
    <div className="min-h-full bg-bg lg:grid lg:grid-cols-[1.1fr_1fr]">
      <PainelMarca />
      <main className="relative z-10 -mt-8 flex flex-col rounded-t-[32px] bg-bg px-gutter pb-safe pt-8 shadow-[0_-24px_60px_-30px_rgb(0_0_0/0.6)] lg:mt-0 lg:items-center lg:justify-center lg:rounded-none lg:px-16 lg:shadow-none">
        <div className="mx-auto w-full max-w-[400px] pb-8">
          {!email || !token ? (
            <Passo>
              <h2 className="font-display text-[28px] font-semibold tracking-tight">Link incompleto</h2>
              <p className="mt-1.5 text-[15px] text-muted">Abra o link do convite exatamente como chegou, ou peça ao seu profissional um novo convite.</p>
              <Link to="/entrar">
                <Button variant="outline" size="lg" className="mt-8 w-full">
                  Ir para entrar
                </Button>
              </Link>
            </Passo>
          ) : (
            <Passo>
              <h2 className="font-display text-[28px] font-semibold tracking-tight">Crie sua senha</h2>
              <p className="mt-1.5 text-[15px] text-muted">
                Seu acompanhamento está pronto. Você vai entrar com <span className="font-semibold text-fg">{email}</span>.
              </p>
              <form onSubmit={submit} className="mt-7 flex flex-col gap-4">
                <Campo label="Nova senha" icon={<Lock className="h-[18px] w-[18px]" />} type={ver ? 'text' : 'password'} autoComplete="new-password" required autoFocus value={senha} onChange={(e) => setSenha(e.target.value)} trailing={olho} />
                <ul className="grid grid-cols-2 gap-x-3 gap-y-1 text-[13px]">
                  {REGRAS_SENHA.map((r) => (
                    <li key={r.l} className={clsx('flex items-center gap-1.5', r.ok(senha) ? 'text-ok' : 'text-faint')}>
                      <Check className="h-3.5 w-3.5 shrink-0" strokeWidth={3} /> {r.l}
                    </li>
                  ))}
                </ul>
                <Campo label="Repita a senha" icon={<Lock className="h-[18px] w-[18px]" />} type={ver ? 'text' : 'password'} autoComplete="new-password" required value={senha2} onChange={(e) => setSenha2(e.target.value)} />
                <Erro texto={erro} />
                <Button type="submit" size="lg" loading={carregando} className="mt-1 w-full">
                  Ativar e entrar <ArrowRight className="h-5 w-5" />
                </Button>
              </form>
            </Passo>
          )}
        </div>
      </main>
    </div>
  );
}

// ---------------------------------------------------------------- Links publicos de e-mail

function TelaPublica({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-full bg-bg lg:grid lg:grid-cols-[1.1fr_1fr]">
      <PainelMarca />
      <main className="relative z-10 -mt-8 flex flex-col rounded-t-[32px] bg-bg px-gutter pb-safe pt-8 shadow-[0_-24px_60px_-30px_rgb(0_0_0/0.6)] lg:mt-0 lg:items-center lg:justify-center lg:rounded-none lg:px-16 lg:shadow-none">
        <div className="mx-auto w-full max-w-[400px] pb-8">
          <Passo>{children}</Passo>
        </div>
      </main>
    </div>
  );
}

function Resultado({ ok, titulo, texto, email }: { ok: boolean; titulo: string; texto: string; email?: string }) {
  return (
    <div className="flex flex-col items-center text-center">
      <motion.div
        initial={{ scale: 0.6, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        transition={{ type: 'spring', stiffness: 220, damping: 16 }}
        className={clsx('grid h-16 w-16 place-items-center rounded-full', ok ? 'bg-accent/15 text-accent' : 'bg-bad/10 text-bad')}
      >
        {ok ? <Check className="h-8 w-8" strokeWidth={3} /> : <Lock className="h-8 w-8" />}
      </motion.div>
      <h2 className="mt-5 font-display text-[26px] font-semibold tracking-tight">{titulo}</h2>
      <p className="mt-2 max-w-xs text-[15px] text-muted">{texto}</p>
      <Link to={email ? `/entrar?email=${encodeURIComponent(email)}` : '/entrar'} className="w-full">
        <Button size="lg" className="mt-8 w-full">
          Ir para entrar
        </Button>
      </Link>
    </div>
  );
}

/** Link "esqueci a senha" do e-mail: define a nova senha. */
export function RedefinirSenhaPage() {
  const [params] = useSearchParams();
  const email = params.get('email') ?? '';
  const token = params.get('token') ?? '';
  const [senha, setSenha] = useState('');
  const [senha2, setSenha2] = useState('');
  const [ver, setVer] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [carregando, setCarregando] = useState(false);
  const [pronto, setPronto] = useState(false);
  const valida = REGRAS_SENHA.every((r) => r.ok(senha));

  async function submit(e: FormEvent) {
    e.preventDefault();
    setErro(null);
    if (!valida) return setErro('A senha ainda não atende a todos os requisitos.');
    if (senha !== senha2) return setErro('As senhas não conferem.');
    setCarregando(true);
    try {
      await api('/api/auth/password/redefinir', { method: 'POST', json: { email, token, novaSenha: senha, confirmacaoNovaSenha: senha2 } });
      setPronto(true);
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Não foi possível redefinir agora.');
    } finally {
      setCarregando(false);
    }
  }

  if (!email || !token) return <TelaPublica><Resultado ok={false} titulo="Link incompleto" texto="Abra o link do e-mail exatamente como chegou, ou peça um novo em Esqueci a senha." /></TelaPublica>;
  if (pronto) return <TelaPublica><Resultado ok titulo="Senha atualizada" texto="Pronto. Entre com a nova senha." email={email} /></TelaPublica>;
  const olho = (
    <button type="button" aria-label={ver ? 'Ocultar senha' : 'Mostrar senha'} onClick={() => setVer((v) => !v)} className="grid h-10 w-10 place-items-center rounded-full text-muted transition hover:bg-raised hover:text-fg">
      {ver ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
    </button>
  );
  return (
    <TelaPublica>
      <h2 className="font-display text-[28px] font-semibold tracking-tight">Nova senha</h2>
      <p className="mt-1.5 text-[15px] text-muted">
        Para a conta <span className="font-semibold text-fg">{email}</span>.
      </p>
      <form onSubmit={submit} className="mt-7 flex flex-col gap-4">
        <Campo label="Nova senha" icon={<Lock className="h-[18px] w-[18px]" />} type={ver ? 'text' : 'password'} autoComplete="new-password" required autoFocus value={senha} onChange={(e) => setSenha(e.target.value)} trailing={olho} />
        <ul className="grid grid-cols-2 gap-x-3 gap-y-1 text-[13px]">
          {REGRAS_SENHA.map((r) => (
            <li key={r.l} className={clsx('flex items-center gap-1.5', r.ok(senha) ? 'text-ok' : 'text-faint')}>
              <Check className="h-3.5 w-3.5 shrink-0" strokeWidth={3} /> {r.l}
            </li>
          ))}
        </ul>
        <Campo label="Repita a senha" icon={<Lock className="h-[18px] w-[18px]" />} type={ver ? 'text' : 'password'} autoComplete="new-password" required value={senha2} onChange={(e) => setSenha2(e.target.value)} />
        <Erro texto={erro} />
        <Button type="submit" size="lg" loading={carregando} className="mt-1 w-full">
          Salvar nova senha
        </Button>
      </form>
    </TelaPublica>
  );
}

/** Links de confirmacao de e-mail e de troca de e-mail. Confirma sozinho ao abrir. */
export function ConfirmarEmailPage({ troca }: { troca?: boolean }) {
  const [params] = useSearchParams();
  const usuarioId = params.get('userId') ?? '';
  const token = params.get('token') ?? '';
  const novoEmail = params.get('email') ?? '';
  const [estado, setEstado] = useState<'carregando' | 'ok' | 'erro'>('carregando');
  const [msg, setMsg] = useState('');
  // O token so vale uma vez: o ref evita confirmar de novo quando o efeito roda duas vezes.
  const enviado = useRef(false);
  useEffect(() => {
    if (enviado.current) return;
    enviado.current = true;
    if (!usuarioId || !token || (troca && !novoEmail)) {
      setEstado('erro');
      setMsg('O link está incompleto. Abra o link do e-mail exatamente como chegou.');
      return;
    }
    api(troca ? '/api/conta/email/troca/confirmar' : '/api/conta/email/confirmacao/confirmar', { method: 'POST', json: troca ? { usuarioId, novoEmail, token } : { usuarioId, token } }).then(
      () => setEstado('ok'),
      (e) => {
        setEstado('erro');
        setMsg(e instanceof Error ? e.message : 'Não foi possível confirmar agora.');
      },
    );
  }, [usuarioId, token, novoEmail, troca]);

  return (
    <TelaPublica>
      {estado === 'carregando' ? (
        <p className="text-center text-muted">Confirmando seu e-mail...</p>
      ) : estado === 'ok' ? (
        <Resultado ok titulo={troca ? 'E-mail alterado' : 'E-mail confirmado'} texto={troca ? `A partir de agora você entra com ${novoEmail}.` : 'Tudo certo com seu e-mail.'} email={troca ? novoEmail : undefined} />
      ) : (
        <Resultado ok={false} titulo="Não deu para confirmar" texto={msg || 'O link pode ter expirado. Peça um novo pela sua conta.'} />
      )}
    </TelaPublica>
  );
}
