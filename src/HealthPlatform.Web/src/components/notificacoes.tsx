import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Bell, BellOff } from 'lucide-react';
import { Button, Card, toast } from '@/components/ui';
import { ativarPush, desativarPush, inscricaoAtual, pushSuportado, statusPush, testarPush } from '@/lib/push';

/** Liga e desliga as notificacoes push neste aparelho. */
export function NotificacoesCard() {
  const qc = useQueryClient();
  const suportado = pushSuportado();
  const st = useQuery({ queryKey: ['push', 'status'], queryFn: statusPush, staleTime: 60_000 });
  const sub = useQuery({ queryKey: ['push', 'sub'], queryFn: inscricaoAtual, enabled: suportado });
  const ativo = !!sub.data;
  const negado = suportado && Notification.permission === 'denied';
  const servidorOff = st.data && !st.data.habilitadoNoServidor;

  const alternar = useMutation({
    mutationFn: async () => {
      if (ativo) await desativarPush();
      else await ativarPush();
    },
    onSuccess: () => {
      toast(ativo ? 'Notificações desligadas neste aparelho.' : 'Notificações ligadas neste aparelho.');
      qc.invalidateQueries({ queryKey: ['push'] });
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível alterar.', 'bad'),
  });
  const teste = useMutation({
    mutationFn: testarPush,
    onSuccess: (r) => toast(r.enviadas > 0 ? 'Notificação de teste enviada.' : r.motivo || 'Teste enviado.'),
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível testar.', 'bad'),
  });

  const aviso = !suportado
    ? 'Este navegador não recebe notificações. No iPhone, adicione o AESYN à tela de início e abra por lá.'
    : servidorOff
      ? 'As notificações ainda não estão ligadas no servidor.'
      : negado
        ? 'Você bloqueou as notificações. Libere nas configurações do navegador para este site.'
        : ativo
          ? 'Você recebe mensagens e atualizações mesmo com o app fechado.'
          : 'Receba mensagens e atualizações do acompanhamento mesmo com o app fechado.';

  return (
    <Card className="p-4">
      <div className="flex items-center gap-3">
        <span className={clsx('grid h-11 w-11 shrink-0 place-items-center rounded-xl', ativo ? 'bg-ok/15 text-ok' : 'bg-raised text-muted')}>
          {ativo ? <Bell className="h-5 w-5" /> : <BellOff className="h-5 w-5" />}
        </span>
        <div className="min-w-0 flex-1">
          <p className="font-semibold">{ativo ? 'Ligadas neste aparelho' : 'Desligadas neste aparelho'}</p>
          <p className="text-sm text-muted">{aviso}</p>
        </div>
      </div>
      {suportado && !servidorOff && !negado && (
        <div className="mt-3 flex gap-2">
          <Button variant={ativo ? 'outline' : 'primary'} className="flex-1" loading={alternar.isPending} onClick={() => alternar.mutate()}>
            {ativo ? 'Desligar' : 'Ligar notificações'}
          </Button>
          {ativo && (
            <Button variant="soft" loading={teste.isPending} onClick={() => teste.mutate()}>
              Testar
            </Button>
          )}
        </div>
      )}
    </Card>
  );
}
