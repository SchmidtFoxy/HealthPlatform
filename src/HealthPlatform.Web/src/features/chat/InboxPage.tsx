import { useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { MessageCircle, Search } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Card, Empty, ErrorBox, inputCls, Segmented, Skeleton } from '@/components/ui';
import { api } from '@/lib/api';
import { iniciais } from '@/lib/format';

interface ConversaInbox {
  pacienteId: string;
  pacienteNome: string;
  ultimaMensagem: string;
  ultimaMensagemEmUtc: string;
  ultimoAutorTipo: string;
  contextoRotulo: string;
  naoLidas: number;
  aguardandoResposta: boolean;
  minutosAguardando: number;
  proximoDoSla: boolean;
  slaExcedido: boolean;
}

interface Inbox {
  totalNaoLidas: number;
  totalAguardandoResposta: number;
  totalSlaExcedido: number;
  conversas: ConversaInbox[];
}

type Filtro = 'todas' | 'aguardando' | 'naoLidas';

export const useInbox = () =>
  useQuery({ queryKey: ['pro', 'inbox'], queryFn: () => api<Inbox>('/api/chat/inbox'), refetchInterval: 30_000, refetchIntervalInBackground: false });

const espera = (min: number) => (min < 60 ? `${min} min` : min < 24 * 60 ? `${Math.floor(min / 60)} h` : `${Math.floor(min / 1440)} d`);
const quando = (iso: string) => {
  const d = new Date(iso);
  return d.toDateString() === new Date().toDateString()
    ? d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
    : d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' });
};

export function InboxPage() {
  const q = useInbox();
  const [filtro, setFiltro] = useState<Filtro>('todas');
  const [busca, setBusca] = useState('');
  const d = q.data;
  const termo = busca.trim().toLowerCase();
  const lista = (d?.conversas ?? [])
    .filter((c) => (filtro === 'aguardando' ? c.aguardandoResposta : filtro === 'naoLidas' ? c.naoLidas > 0 : true))
    .filter((c) => !termo || c.pacienteNome.toLowerCase().includes(termo) || c.ultimaMensagem.toLowerCase().includes(termo));

  return (
    <Page>
      <PageHeader eyebrow="Mensagens" title="Conversas" subtitle={d ? `${d.totalAguardandoResposta} aguardando resposta${d.totalSlaExcedido ? ` · ${d.totalSlaExcedido} passaram de 24h` : ''}` : undefined} />
      <div className="relative mb-3">
        <Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-faint" />
        <input className={clsx(inputCls, 'pl-10')} value={busca} placeholder="Buscar paciente ou mensagem" onChange={(e) => setBusca(e.target.value)} />
      </div>
      <Segmented<Filtro>
        value={filtro}
        onChange={setFiltro}
        options={[
          { value: 'todas', label: 'Todas' },
          { value: 'aguardando', label: `A responder${d?.totalAguardandoResposta ? ` ${d.totalAguardandoResposta}` : ''}` },
          { value: 'naoLidas', label: `Não lidas${d?.totalNaoLidas ? ` ${d.totalNaoLidas}` : ''}` },
        ]}
      />
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && <Skeleton className="mt-4 h-64" />}
      {d && !lista.length && (
        <div className="mt-4">
          <Empty icon={<MessageCircle className="h-5 w-5" />} title={d.conversas.length ? 'Nada neste filtro' : 'Nenhuma conversa ainda'}>
            {d.conversas.length ? 'Tudo respondido por aqui.' : 'Quando um paciente mandar mensagem, ela aparece aqui.'}
          </Empty>
        </div>
      )}
      {!!lista.length && (
        <Card className="mt-4 divide-y divide-line/60">
          {lista.map((c) => (
            <Link key={c.pacienteId} to={`/pro/mensagens/${c.pacienteId}`} className="flex items-center gap-3 p-3.5 transition hover:bg-raised/40">
              <span className="relative grid h-11 w-11 shrink-0 place-items-center rounded-full bg-accent/15 font-display text-sm font-semibold text-accent">
                {iniciais(c.pacienteNome)}
                {c.naoLidas > 0 && <span className="num absolute -right-1 -top-1 grid h-5 min-w-5 place-items-center rounded-full bg-bad px-1 text-[11px] font-bold text-white">{c.naoLidas}</span>}
              </span>
              <span className="min-w-0 flex-1">
                <span className="flex items-baseline justify-between gap-2">
                  <span className={clsx('truncate', c.naoLidas ? 'font-bold' : 'font-semibold')}>{c.pacienteNome}</span>
                  <span className="shrink-0 text-[12px] text-faint">{quando(c.ultimaMensagemEmUtc)}</span>
                </span>
                <span className={clsx('block truncate text-[14px]', c.naoLidas ? 'text-fg' : 'text-muted')}>
                  {c.ultimoAutorTipo === 'Profissional' && 'Você: '}
                  {c.ultimaMensagem}
                </span>
                {c.aguardandoResposta && (
                  <span
                    className={clsx(
                      'mt-1 inline-block rounded-full px-2 py-0.5 text-[11px] font-semibold',
                      c.slaExcedido ? 'bg-bad/15 text-bad' : c.proximoDoSla ? 'bg-warn/15 text-warn' : 'bg-raised text-muted',
                    )}
                  >
                    Aguardando há {espera(c.minutosAguardando)}
                    {c.contextoRotulo && c.contextoRotulo !== 'Geral' ? ` · ${c.contextoRotulo}` : ''}
                  </span>
                )}
              </span>
            </Link>
          ))}
        </Card>
      )}
    </Page>
  );
}
