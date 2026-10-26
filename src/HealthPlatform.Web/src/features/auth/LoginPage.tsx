import { motion } from 'framer-motion';
import { ArrowRight, Eye, EyeOff } from 'lucide-react';
import { FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, inputCls } from '@/components/ui';
import { entrar } from '@/lib/api';

export function LoginPage() {
  const nav = useNavigate();
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [ver, setVer] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [carregando, setCarregando] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      const s = await entrar(email.trim(), senha);
      nav(s.tipoUsuario === 'Paciente' ? '/hoje' : '/pro', { replace: true });
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Não foi possível entrar.');
    } finally {
      setCarregando(false);
    }
  }

  return (
    <div className="relative flex min-h-full flex-col overflow-hidden bg-[rgb(5_16_14)] text-[rgb(230_243_240)]" data-theme="dark">
      <div className="pointer-events-none absolute inset-0">
        <div className="absolute -left-1/3 -top-1/4 h-[70vh] w-[90vw] rounded-full bg-[rgb(94_240_200/0.16)] blur-[90px]" />
        <div className="absolute -right-1/4 top-1/3 h-[50vh] w-[70vw] rounded-full bg-[rgb(15_90_80/0.5)] blur-[100px]" />
      </div>

      <div className="relative mx-auto flex w-full max-w-md flex-1 flex-col px-gutter pb-10 pt-safe">
        <motion.div initial={{ opacity: 0, y: 24 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.7, ease: [0.22, 1, 0.36, 1] }} className="flex flex-1 flex-col justify-center pt-12">
          <motion.img
            src="/icons/icon-512.png"
            alt="AESYN"
            className="h-24 w-24 rounded-[28px] shadow-[0_0_60px_-10px_rgb(94_240_200/0.6)]"
            initial={{ scale: 0.8, rotate: -6 }}
            animate={{ scale: 1, rotate: 0 }}
            transition={{ type: 'spring', stiffness: 160, damping: 14 }}
          />
          <p className="mt-8 text-[12px] font-semibold uppercase tracking-[0.3em] text-[rgb(94_240_200)]">Saúde · Performance · Longevidade</p>
          <h1 className="mt-3 font-display text-[40px] font-semibold leading-[1.05] tracking-tight">
            Seu plano,
            <br />
            seu ritmo.
          </h1>
          <p className="mt-3 max-w-xs text-[15px] text-[rgb(140_168_162)]">Treino, alimentação e acompanhamento do seu profissional no mesmo lugar.</p>
        </motion.div>

        <motion.form
          onSubmit={submit}
          initial={{ opacity: 0, y: 30 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.15, duration: 0.6, ease: [0.22, 1, 0.36, 1] }}
          className="mt-10 flex flex-col gap-3"
        >
          <input
            id="email"
            type="email"
            autoComplete="email"
            inputMode="email"
            placeholder="seu@email.com"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className={inputCls + ' h-14 rounded-2xl border-white/10 bg-white/5 text-white placeholder:text-white/35'}
          />
          <div className="relative">
            <input
              id="senha"
              type={ver ? 'text' : 'password'}
              autoComplete="current-password"
              placeholder="Senha"
              required
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              className={inputCls + ' h-14 rounded-2xl border-white/10 bg-white/5 pr-12 text-white placeholder:text-white/35'}
            />
            <button type="button" aria-label={ver ? 'Ocultar senha' : 'Mostrar senha'} onClick={() => setVer((v) => !v)} className="absolute right-2 top-2 grid h-10 w-10 place-items-center rounded-full text-white/60">
              {ver ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
            </button>
          </div>
          {erro && <p className="rounded-xl bg-[rgb(255_110_110/0.12)] px-4 py-3 text-sm text-[rgb(255_150_150)]">{erro}</p>}
          <Button type="submit" size="lg" loading={carregando} className="mt-2 bg-[rgb(94_240_200)] text-[rgb(4_30_25)]">
            Entrar <ArrowRight className="h-5 w-5" />
          </Button>
          <div className="mt-3 flex justify-between text-sm text-white/55">
            <a href="/?recuperar=1" className="hover:text-white">Esqueci a senha</a>
            <a href="/" className="hover:text-white">Versão clássica</a>
          </div>
        </motion.form>
      </div>
    </div>
  );
}
