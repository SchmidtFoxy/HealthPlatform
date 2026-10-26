import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Bell, CalendarClock, CheckCheck, ClipboardList, MessageCircle, PhoneCall, TriangleAlert } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { tempoRelativo } from '@/lib/format';
import { destinoDoLink } from '@/lib/push';
import { Chips } from './CuidadosPro';

interface Aviso {
  id: string;
  tipo: string;
  prioridade: 'Alta' | 'Media' | 'Normal' | string;
  titulo: string;
  mensagem: string | null;
  dataEventoUtc: string | null;
  link: string | null;
  lida: boolean;
  createdAtUtc: string;
}

interface ListaAvisos {
  naoLidas: number;
  itens: Aviso[];
}

const ICONE: Record<string, typeof Bell> = { Chat: MessageCircle, FollowUp: PhoneCall, Solicitacao: ClipboardList, Agenda: CalendarClock, Pendencia: TriangleAlert };

/** Avisos da equipe: chat, contatos vencidos, respostas de pedidos, pendencias e agenda. */
export function AvisosPage() {
  const qc = useQueryClient();
  const nav = useNavigate();
  const [filtro, setFiltro] = useState<'nao-lidas' | 'todas'>('nao-lidas');
  const q = useQuery({ queryKey: ['pro', 'avisos', filtro], queryFn: () => api<ListaAvisos>(`/api/notificacoes?limite=100${filtro === 'nao-lidas' ? '&lida=false' : ''}`) });
  const invalidar = () => qc.invalidateQueries({ queryKey: ['pro', 'avisos'] });
  const lerTodas = useMutation({
    mutationFn: () => api('/api/notificacoes/ler-todas', { method: 'PUT' }),
    onSuccess: () => {
      toast('Tudo marcado como lido.');
      invalidar();
    },
    onError: (e) => toast(e.message, 'bad'),
  });
  const alternar = useMutation({
    mutationFn: (a: Aviso) => api(`/api/notificacoes/${a.id}/${a.lida ? 'nao-lida' : 'lida'}`, { method: 'PUT' }),
    onSuccess: invalidar,
  });

  const abrir = async (a: Aviso) => {
    if (!a.lida) await api(`/api/notificacoes/${a.id}/lida`, { method: 'PUT' }).catch(() => {});
    invalidar();
    if (!a.link) return;
    const destino = destinoDoLink(a.link, false);
    if (destino.startsWith('/app/')) nav(destino.slice(4));
    else location.assign(destino);
  };

  const itens = q.data?.itens ?? [];
  return (
    <Page>
      <PageHeader
        back="/pro"
        title="Avisos"
        subtitle={q.data ? (q.data.naoLidas ? `${q.data.naoLidas} não ${q.data.naoLidas === 1 ? 'lido' : 'lidos'}` : 'Tudo lido') : undefined}
        action={
          !!q.data?.naoLidas && (
            <Button size="sm" variant="soft" icon={<CheckCheck className="h-4 w-4" />} loading={lerTodas.isPending} onClick={() => lerTodas.mutate()}>
              Ler todos
            </Button>
          )
        }
      />
      <Chips
        value={filtro}
        onChange={setFiltro}
        options={[
          { value: 'nao-lidas', label: 'Não lidos' },
          { value: 'todas', label: 'Todos' },
        ]}
      />
      <div className="mt-4">
        {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
        {q.isLoading ? (
          <Skeleton className="h-64" />
        ) : !itens.length ? (
          <Empty icon={<Bell className="h-5 w-5" />} title={filtro === 'nao-lidas' ? 'Nada novo' : 'Sem avisos'}>
            Mensagens de pacientes, contatos atrasados, respostas de pedidos e consultas próximas aparecem aqui.
          </Empty>
        ) : (
          <Card className="divide-y divide-line/60 overflow-hidden">
            {itens.map((a) => {
              const Icone = ICONE[a.tipo] ?? Bell;
              return (
                <div key={a.id} className={clsx('flex items-start gap-3 p-3.5', !a.lida && 'bg-accent/[0.04]')}>
                  <button onClick={() => abrir(a)} className="flex min-w-0 flex-1 items-start gap-3 text-left">
                    <span className={clsx('mt-0.5 grid h-9 w-9 shrink-0 place-items-center rounded-xl', a.prioridade === 'Alta' ? 'bg-bad/15 text-bad' : a.prioridade === 'Media' ? 'bg-warn/15 text-warn' : 'bg-accent/10 text-accent')}>
                      <Icone className="h-4 w-4" />
                    </span>
                    <span className="min-w-0 flex-1 [overflow-wrap:anywhere]">
                      <span className={clsx('block text-[15px]', a.lida ? 'text-muted' : 'font-semibold')}>{a.titulo}</span>
                      {a.mensagem && <span className="line-clamp-2 block text-[13px] text-muted">{a.mensagem}</span>}
                      <span className="block text-[12px] text-faint">{tempoRelativo(a.dataEventoUtc ?? a.createdAtUtc)}</span>
                    </span>
                  </button>
                  <button
                    aria-label={a.lida ? 'Marcar como não lido' : 'Marcar como lido'}
                    title={a.lida ? 'Marcar como não lido' : 'Marcar como lido'}
                    onClick={() => alternar.mutate(a)}
                    className="mt-1 grid h-8 w-8 shrink-0 place-items-center rounded-full hover:bg-raised"
                  >
                    <span className={clsx('h-2.5 w-2.5 rounded-full', a.lida ? 'border border-line' : 'bg-accent')} />
                  </button>
                </div>
              );
            })}
          </Card>
        )}
      </div>
    </Page>
  );
}
