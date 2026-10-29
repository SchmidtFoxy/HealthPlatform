import clsx from 'clsx';
import { ChevronRight, LogOut, Plus, Repeat, X } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ContaLembrada, contasLembradas, encerrarSimulacao, esquecerConta, lerSessao, sair, TipoUsuario } from '@/lib/api';
import { iniciais } from '@/lib/format';
import { useSessao } from '@/lib/session';
import { Sheet } from './ui';

export const PAPEL: Record<TipoUsuario, string> = {
  Admin: 'Administrador',
  Medico: 'Médico',
  Nutricionista: 'Nutricionista',
  Personal: 'Personal trainer',
  Secretaria: 'Secretaria',
  Paciente: 'Paciente',
};

const COR_PAPEL: Record<TipoUsuario, string> = {
  Admin: 'bg-info/15 text-info',
  Medico: 'bg-accent/15 text-accent',
  Nutricionista: 'bg-carb/15 text-carb',
  Personal: 'bg-fat/15 text-fat',
  Secretaria: 'bg-raised text-muted',
  Paciente: 'bg-prot/15 text-prot',
};

export function Avatar({ nome, tipo, size = 'md' }: { nome?: string | null; tipo?: TipoUsuario; size?: 'sm' | 'md' | 'lg' }) {
  return (
    <span
      className={clsx(
        'grid shrink-0 place-items-center rounded-full font-display font-semibold',
        tipo ? COR_PAPEL[tipo] : 'bg-accent/15 text-accent',
        size === 'sm' && 'h-9 w-9 text-xs',
        size === 'md' && 'h-11 w-11 text-sm',
        size === 'lg' && 'h-16 w-16 text-xl',
      )}
    >
      {iniciais(nome ?? undefined)}
    </span>
  );
}

/** Sair e trocar de conta. Trocar encerra a sessao atual e abre o login ja com o e-mail escolhido. */
export function useConta() {
  const nav = useNavigate();
  return {
    async sair() {
      // Na simulacao, "Sair" volta para a conta do admin.
      if (lerSessao()?.impersonating) {
        await encerrarSimulacao();
        return nav('/admin/usuarios?aba=equipe', { replace: true });
      }
      await sair();
      nav('/entrar', { replace: true });
    },
    async trocarPara(email?: string) {
      await sair();
      nav(email ? `/entrar?email=${encodeURIComponent(email)}` : '/entrar', { replace: true });
    },
  };
}

export function TrocarConta() {
  const s = useSessao();
  const conta = useConta();
  const [lista, setLista] = useState(contasLembradas);
  const outras = lista.filter((c) => !(s && c.nome === s.nome && c.tipoUsuario === s.tipoUsuario));

  return (
    <div className="overflow-hidden rounded-xl2 border border-line/70 bg-surface">
      {outras.map((c: ContaLembrada) => (
        <div key={c.email} className="flex items-center gap-3 border-b border-line/70 px-4 py-3">
          <button onClick={() => conta.trocarPara(c.email)} className="flex min-w-0 flex-1 items-center gap-3 text-left">
            <Avatar nome={c.nome} tipo={c.tipoUsuario} size="sm" />
            <span className="min-w-0 flex-1">
              <span className="block truncate font-semibold">{c.nome}</span>
              <span className="block truncate text-[13px] text-muted">
                {PAPEL[c.tipoUsuario] ?? c.tipoUsuario} · {c.email}
              </span>
            </span>
            <Repeat className="h-4 w-4 shrink-0 text-muted" />
          </button>
          <button
            aria-label={`Esquecer ${c.nome} neste aparelho`}
            onClick={() => {
              esquecerConta(c.email);
              setLista(contasLembradas());
            }}
            className="grid h-8 w-8 shrink-0 place-items-center rounded-full text-faint hover:bg-raised hover:text-fg"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      ))}
      <button onClick={() => conta.trocarPara()} className="flex w-full items-center gap-3 px-4 py-3.5 text-left font-semibold text-accent hover:bg-raised/60">
        <span className="grid h-9 w-9 place-items-center rounded-full border border-dashed border-accent/50">
          <Plus className="h-4 w-4" />
        </span>
        Entrar com outra conta
      </button>
    </div>
  );
}

/** Cartao de conta no rodape do menu lateral (PC): abre uma folha com trocar conta e sair. */
export function ContaLateral() {
  const s = useSessao();
  const conta = useConta();
  const [aberto, setAberto] = useState(false);
  if (!s) return null;
  return (
    <>
      <button onClick={() => setAberto(true)} className="flex w-full items-center gap-3 rounded-2xl p-2 text-left transition hover:bg-raised">
        <Avatar nome={s.nome} tipo={s.tipoUsuario} size="sm" />
        <span className="min-w-0 flex-1">
          <span className="block truncate text-sm font-semibold">{s.nome}</span>
          <span className="block text-xs text-muted">{PAPEL[s.tipoUsuario]}</span>
        </span>
        <ChevronRight className="h-4 w-4 text-muted" />
      </button>
      <Sheet open={aberto} onClose={() => setAberto(false)} title="Sua conta">
        <div className="flex items-center gap-4 pb-5">
          <Avatar nome={s.nome} tipo={s.tipoUsuario} size="lg" />
          <div className="min-w-0">
            <p className="truncate font-display text-lg font-semibold">{s.nome}</p>
            <p className="text-sm text-muted">{PAPEL[s.tipoUsuario]}</p>
          </div>
        </div>
        <p className="eyebrow mb-2">Trocar de conta</p>
        <TrocarConta />
        <button onClick={conta.sair} className="mt-5 flex h-12 w-full items-center justify-center gap-2 rounded-full bg-bad/10 font-semibold text-bad hover:bg-bad/20">
          <LogOut className="h-5 w-5" /> Sair da conta
        </button>
      </Sheet>
    </>
  );
}
