import { useQuery } from '@tanstack/react-query';
import { Activity, CalendarDays, ClipboardList, Dumbbell, FlaskConical, HeartPulse, NotebookPen, Scale, Target } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Card, Empty, ErrorBox, Segmented, Skeleton } from '@/components/ui';
import { api } from '@/lib/api';

interface ItemJornada {
  tipo: string;
  id: string;
  dataUtc: string;
  titulo: string;
  resumo: string | null;
  complemento: string | null;
  destino: string;
}

const TIPOS: Record<string, { l: string; icon: typeof Activity }> = {
  consulta: { l: 'Consulta', icon: CalendarDays },
  avaliacao: { l: 'Avaliação corporal', icon: Scale },
  exame: { l: 'Exame', icon: FlaskConical },
  meta: { l: 'Meta', icon: Target },
  diario: { l: 'Diário', icon: NotebookPen },
  checkin: { l: 'Check-in', icon: HeartPulse },
  treino: { l: 'Treino', icon: Dumbbell },
  solicitacao: { l: 'Pedido do profissional', icon: ClipboardList },
};

// Titulos que a API manda sem acento (tipos do diario e rotulos fixos).
const TITULOS: Record<string, string> = {
  FechamentoDia: 'Fechamento do dia',
  Hidratacao: 'Hidratação',
  Alimentacao: 'Alimentação',
  Observacao: 'Observação',
  'Avaliacao corporal': 'Avaliação corporal',
  'Solicitacao clinica': 'Pedido do profissional',
};

const DESTINOS: Record<string, string> = { diario: '/diario', metas: '/cuidados', solicitacoes: '/cuidados', treino: '/treino', evolucao: '/progresso', exames: '/progresso' };

const quando = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: 'short', year: new Date(iso).getFullYear() === new Date().getFullYear() ? undefined : 'numeric', hour: '2-digit', minute: '2-digit' });
const mes = (iso: string) => new Date(iso).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' });

export function JourneyPage() {
  const [dias, setDias] = useState<'90' | '180' | '365'>('180');
  const q = useQuery({ queryKey: ['p', 'jornada', dias], queryFn: () => api<{ total: number; itens: ItemJornada[] }>(`/api/portal/me/jornada?dias=${dias}`), placeholderData: (p) => p });
  const itens = q.data?.itens ?? [];

  return (
    <Page>
      <PageHeader back="/perfil" title="Sua jornada" subtitle="Tudo o que aconteceu no seu acompanhamento, em ordem." />
      <Segmented
        value={dias}
        onChange={setDias}
        options={[
          { value: '90', label: '3 meses' },
          { value: '180', label: '6 meses' },
          { value: '365', label: '1 ano' },
        ]}
      />
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && <Skeleton className="mt-4 h-64" />}
      {q.data && !itens.length && (
        <div className="mt-4">
          <Empty icon={<Activity className="h-5 w-5" />} title="Sua jornada começa aqui">
            Consultas, check-ins, treinos, exames, metas e pedidos aparecem nesta linha do tempo.
          </Empty>
        </div>
      )}
      {!!itens.length && <p className="mt-4 text-sm text-muted">{itens.length} {itens.length === 1 ? 'evento' : 'eventos'} no período</p>}
      <div className="mt-2">
        {itens.map((x, i) => {
          const t = TIPOS[x.tipo] ?? { l: x.tipo, icon: Activity };
          const novoMes = i === 0 || mes(itens[i - 1].dataUtc) !== mes(x.dataUtc);
          const destino = DESTINOS[x.destino];
          const corpo = (
            <Card className="min-w-0 flex-1 p-3.5 [overflow-wrap:anywhere]">
              <p className="text-[12px] font-semibold uppercase tracking-wide text-faint">
                {t.l} · {quando(x.dataUtc)}
              </p>
              <p className="mt-0.5 font-semibold">{TITULOS[x.titulo] ?? x.titulo}</p>
              {x.resumo && <p className="text-[14px] leading-snug text-muted">{x.resumo.replaceAll(' • ', ' · ')}</p>}
              {x.complemento && <p className="mt-1 text-[12px] text-faint">{x.complemento}</p>}
            </Card>
          );
          return (
            <div key={`${x.tipo}-${x.id}`}>
              {novoMes && <p className="eyebrow mb-2 mt-5 capitalize">{mes(x.dataUtc)}</p>}
              <div className="flex gap-3 pb-2">
                <div className="flex flex-col items-center">
                  <span className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-accent/10 text-accent">
                    <t.icon className="h-4 w-4" />
                  </span>
                  <span className="mt-1 w-px flex-1 bg-line" />
                </div>
                {destino ? (
                  <Link to={destino} className="flex min-w-0 flex-1">
                    {corpo}
                  </Link>
                ) : (
                  corpo
                )}
              </div>
            </div>
          );
        })}
      </div>
    </Page>
  );
}
