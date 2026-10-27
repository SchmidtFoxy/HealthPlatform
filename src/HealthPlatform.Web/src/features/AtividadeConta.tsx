import { useQuery } from '@tanstack/react-query';
import { History } from 'lucide-react';
import { useState } from 'react';
import { Card, Skeleton } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta } from '@/lib/format';

// Segurança da conta: a sessao atual e o que foi feito com ela (registro de auditoria).

interface Registro {
  id: string;
  acao: string;
  entidade: string;
  createdAtUtc: string;
  ipAddress: string | null;
}

const VERBO: [RegExp, string][] = [
  [/DOWNLOAD/, 'Abriu'],
  [/UPLOAD/, 'Enviou'],
  [/IMPERSONATION_START/, 'Começou a ver o app como'],
  [/IMPERSONATION_END/, 'Saiu da visão de'],
  [/PASSWORD/, 'Redefiniu a senha de'],
  [/LOGIN/, 'Entrou'],
  [/LOGOUT/, 'Saiu'],
  [/REORDER/, 'Reordenou'],
  [/ASSIGN/, 'Aplicou'],
  [/REVIEW/, 'Revisou'],
  [/DEACTIVATE|ARCHIV/, 'Desativou'],
  [/REACTIVATE|ACTIVATE/, 'Ativou'],
  [/DELETE|REMOVE/, 'Removeu'],
  [/CREATE/, 'Criou'],
  [/UPDATE|STATUS|CHANGE/, 'Alterou'],
  [/EXPORT/, 'Exportou'],
];

// Nomes que pedem traducao; o resto so separa as palavras ("PerguntaAnamnese" -> "pergunta anamnese").
const ENTIDADE: Record<string, string> = {
  ArquivoPaciente: 'arquivo de paciente',
  ImpersonacaoProfissional: 'profissional',
  UsuarioEquipe: 'membro da equipe',
  Organizacao: 'dados da clínica',
  CheckInAcompanhamento: 'check-in',
  PlanoAlimentar: 'dieta',
  PlanoTreino: 'treino',
  RevisaoFase: 'fase',
};
const entidade = (e: string) => ENTIDADE[e] ?? e.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase();
const verbo = (a: string) => VERBO.find(([r]) => r.test(a))?.[1] ?? 'Registrou';

export function AtividadeConta() {
  const [todos, setTodos] = useState(false);
  const sessao = useQuery({
    queryKey: ['conta', 'sessao-atual'],
    queryFn: () => api<{ ip: string | null; userAgent: string | null; expiraEmUtc: string | null }>('/api/configuracoes/minha-conta/sessao-atual'),
  });
  const q = useQuery({ queryKey: ['conta', 'auditoria'], queryFn: () => api<Registro[]>('/api/configuracoes/minha-conta/auditoria') });
  const lista = q.data ?? [];
  const visiveis = todos ? lista : lista.slice(0, 6);
  const s = sessao.data;
  return (
    <Card className="divide-y divide-line/60">
      {s && (
        <div className="px-4 py-3 text-[13px]">
          <p className="font-semibold text-fg">Esta sessão</p>
          <p className="text-muted">
            {[s.ip ? `IP ${s.ip}` : null, s.expiraEmUtc ? `válida até ${new Date(s.expiraEmUtc).toLocaleString('pt-BR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}` : null]
              .filter(Boolean)
              .join(' · ')}
          </p>
          <p className="mt-0.5 text-faint">Sair da conta encerra também as sessões abertas em outros aparelhos.</p>
        </div>
      )}
      {q.isLoading ? (
        <Skeleton className="m-4 h-24" />
      ) : !lista.length ? (
        <p className="px-4 py-3 text-[13px] text-muted">Nenhuma ação registrada ainda.</p>
      ) : (
        <>
          {visiveis.map((r) => (
            <div key={r.id} className="flex items-center gap-3 px-4 py-2.5">
              <History className="h-4 w-4 shrink-0 text-faint" />
              <p className="min-w-0 flex-1 truncate text-[14px]">
                {verbo(r.acao)} {entidade(r.entidade)}
              </p>
              <p className="num shrink-0 text-[12px] text-muted">{dataCurta(r.createdAtUtc)} {new Date(r.createdAtUtc).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</p>
            </div>
          ))}
          {lista.length > 6 && (
            <button onClick={() => setTodos((x) => !x)} className="w-full px-4 py-3 text-center text-[14px] font-semibold text-accent">
              {todos ? 'Mostrar menos' : `Ver as ${lista.length} últimas ações`}
            </button>
          )}
        </>
      )}
    </Card>
  );
}
