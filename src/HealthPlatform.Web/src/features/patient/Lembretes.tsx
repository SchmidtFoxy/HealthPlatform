import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ChevronRight, Droplet, Dumbbell, MessageCircle, UtensilsCrossed } from 'lucide-react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Toggle } from '@/features/AccountPage';
import { Card, Skeleton, inputCls, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { ativarPush, inscricaoAtual, pushSuportado } from '@/lib/push';
import { useChatNaoLidas } from './queries';

// Perfil › Lembretes: o paciente escolhe quais avisos quer receber por push.
// Refeicoes e treino sao novos (v0.80.0); agua e o mesmo lembrete do sino do card de agua;
// chat e a mesma preferencia "Mensagens" da conta.

export interface Lembretes {
  refeicoes: boolean;
  agua: boolean;
  treino: boolean;
  horaTreino: string;
  chat: boolean;
  aparelhos: number;
  refeicoesDoPlano: { nome: string; horario: string | null }[];
  treinoHoje: string | null;
}

type Preferencias = Pick<Lembretes, 'refeicoes' | 'agua' | 'treino' | 'horaTreino' | 'chat'>;

const CHAVE = ['p', 'lembretes'] as const;
const fuso = () => new Date().getTimezoneOffset();

export const useLembretes = () => useQuery({ queryKey: CHAVE, queryFn: () => api<Lembretes>('/api/portal/me/lembretes') });

function Linha({ icone, titulo, detalhe, on, onChange, children }: { icone: ReactNode; titulo: string; detalhe: ReactNode; on: boolean; onChange: (v: boolean) => void; children?: ReactNode }) {
  return (
    <div className="p-4">
      <div className="flex items-center gap-3">
        <span className={clsx('grid h-10 w-10 shrink-0 place-items-center rounded-xl', on ? 'bg-accent/15 text-accent' : 'bg-raised text-muted')}>{icone}</span>
        <span className="min-w-0 flex-1">
          <span className="block font-semibold">{titulo}</span>
          <span className="block text-sm text-muted">{detalhe}</span>
        </span>
        <Toggle label={titulo} on={on} onChange={onChange} />
      </div>
      {children}
    </div>
  );
}

export function LembretesCard() {
  const qc = useQueryClient();
  const q = useLembretes();
  const naoLidas = useChatNaoLidas().data?.total ?? 0;
  const l = q.data;

  const salvar = useMutation({
    mutationFn: async (p: Partial<Preferencias>) => {
      const atual = { ...l!, ...p };
      const ligando = (['refeicoes', 'agua', 'treino', 'chat'] as const).some((k) => p[k] === true);
      if (ligando) {
        if (!pushSuportado()) throw new Error('Este aparelho não recebe notificações. No iPhone, instale o AESYN na tela de início primeiro.');
        if (!(await inscricaoAtual())) await ativarPush();
      }
      return api<Lembretes>('/api/portal/me/lembretes', {
        method: 'PUT',
        json: { refeicoes: atual.refeicoes, agua: atual.agua, treino: atual.treino, horaTreino: atual.horaTreino, chat: atual.chat, fuso: fuso() },
      });
    },
    onMutate: (p) => {
      const antes = qc.getQueryData<Lembretes>(CHAVE);
      if (antes) qc.setQueryData<Lembretes>(CHAVE, { ...antes, ...p });
      return { antes };
    },
    onSuccess: (r) => {
      qc.setQueryData(CHAVE, r);
      qc.invalidateQueries({ queryKey: ['p', 'agua'] });
      qc.invalidateQueries({ queryKey: ['conta'] });
      qc.invalidateQueries({ queryKey: ['push'] });
    },
    onError: (e, _p, ctx) => {
      if (ctx?.antes) qc.setQueryData(CHAVE, ctx.antes);
      toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad');
    },
  });

  if (q.isLoading) return <Skeleton className="h-72" />;
  if (!l) return null;

  const comHorario = l.refeicoesDoPlano.filter((r) => r.horario);
  const refeicoesTexto = !l.refeicoesDoPlano.length
    ? 'Quando seu plano alimentar estiver ativo, avisamos na hora de cada refeição.'
    : comHorario.length
      ? 'Na hora de cada refeição do plano, se ainda não marcou.'
      : 'Seu plano ainda não tem horários nas refeições. Peça ao seu profissional para colocar.';

  return (
    <>
      <Card className="divide-y divide-line/60">
        <Linha icone={<UtensilsCrossed className="h-5 w-5" />} titulo="Refeições" detalhe={refeicoesTexto} on={l.refeicoes} onChange={(v) => salvar.mutate({ refeicoes: v })}>
          {l.refeicoes && comHorario.length > 0 && (
            <div className="mt-3 flex flex-wrap gap-1.5 pl-[52px]">
              {comHorario.map((r) => (
                <span key={r.nome + r.horario} className="rounded-full bg-raised px-2.5 py-1 text-xs text-muted">
                  <b className="font-semibold text-fg">{r.horario}</b> {r.nome}
                </span>
              ))}
            </div>
          )}
        </Linha>
        <Linha
          icone={<Droplet className="h-5 w-5" />}
          titulo="Água"
          detalhe="Às 10h, 13h, 16h e 19h, só se você estiver atrás do ritmo da meta."
          on={l.agua}
          onChange={(v) => salvar.mutate({ agua: v })}
        />
        <Linha
          icone={<Dumbbell className="h-5 w-5" />}
          titulo="Treino"
          detalhe={l.treinoHoje ? `Nos dias de treino, se ainda não registrou. Hoje: ${l.treinoHoje}.` : 'Nos dias de treino, se ainda não registrou.'}
          on={l.treino}
          onChange={(v) => salvar.mutate({ treino: v })}
        >
          {l.treino && (
            <label className="mt-3 flex items-center justify-between gap-3 pl-[52px] text-sm">
              <span className="whitespace-nowrap text-muted">Horário do aviso</span>
              <input
                type="time"
                className={clsx(inputCls, 'w-28')}
                value={l.horaTreino}
                onChange={(e) => e.target.value && salvar.mutate({ horaTreino: e.target.value })}
              />
            </label>
          )}
        </Linha>
        <Linha
          icone={<MessageCircle className="h-5 w-5" />}
          titulo="Mensagens do chat"
          detalhe="Avisa quando seu profissional responder."
          on={l.chat}
          onChange={(v) => salvar.mutate({ chat: v })}
        />
        <Link to="/chat" className="flex items-center justify-between gap-3 p-4">
          <span className="text-sm">
            <span className="font-semibold">{naoLidas > 0 ? `${naoLidas} mensagem${naoLidas > 1 ? 'ns' : ''} não lida${naoLidas > 1 ? 's' : ''}` : 'Nenhuma mensagem nova'}</span>
            <span className="block text-muted">Abrir o chat</span>
          </span>
          <ChevronRight className="h-5 w-5 shrink-0 text-muted" />
        </Link>
      </Card>
      {l.aparelhos === 0 && (l.refeicoes || l.agua || l.treino || l.chat) && (
        <p className="mt-2 px-1 text-sm text-warn">Nenhum aparelho recebendo notificações. Ligue as notificações acima para os lembretes chegarem.</p>
      )}
    </>
  );
}
