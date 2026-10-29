import { Eye } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { encerrarSimulacao } from '@/lib/api';
import { useSessao } from '@/lib/session';

/** Faixa no topo enquanto o admin ve o app como um profissional (somente leitura). */
export function FaixaSimulacao() {
  const s = useSessao();
  const nav = useNavigate();
  const [saindo, setSaindo] = useState(false);
  if (!s?.impersonating) return null;
  const voltar = async () => {
    setSaindo(true);
    await encerrarSimulacao();
    setSaindo(false);
    nav('/admin/equipe', { replace: true });
  };
  return (
    <div className="bg-warn/15 pt-safe text-warn">
      <div className="mx-auto flex max-w-5xl items-center gap-3 px-gutter py-2 text-[13px]">
        <Eye className="h-4 w-4 shrink-0" />
        <p className="min-w-0 flex-1 leading-snug">
          Vendo como <b>{s.nome}</b>. Somente leitura, nada é salvo.
        </p>
        <button onClick={voltar} disabled={saindo} className="shrink-0 rounded-full bg-warn/20 px-3 py-1.5 font-semibold transition hover:bg-warn/30 disabled:opacity-60">
          {saindo ? 'Voltando…' : 'Voltar à minha conta'}
        </button>
      </div>
    </div>
  );
}
