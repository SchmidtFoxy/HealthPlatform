import { ArrowLeftRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useSessao } from '@/lib/session';

function Atalho({ to, titulo, sub }: { to: string; titulo: string; sub: string }) {
  return (
    <Link to={to} className="mb-3 flex items-center gap-3 rounded-2xl border border-line/70 p-3 transition hover:border-accent/40 hover:bg-raised">
      <ArrowLeftRight className="h-4 w-4 shrink-0 text-accent" />
      <span className="min-w-0">
        <span className="block text-sm font-semibold">{titulo}</span>
        <span className="block text-xs text-muted">{sub}</span>
      </span>
    </Link>
  );
}

/** No menu da rotina clinica, so o admin (fora da simulacao) ve o caminho de volta para o painel. */
export function AtalhoAdmin() {
  const s = useSessao();
  if (s?.tipoUsuario !== 'Admin' || s.impersonating) return null;
  return <Atalho to="/admin" titulo="Painel do administrador" sub="Equipe, pacientes e atividade" />;
}

export function AtalhoClinico() {
  return <Atalho to="/pro" titulo="Rotina clínica" sub="Agenda, pacientes e mensagens" />;
}
