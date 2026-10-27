import { useQuery } from '@tanstack/react-query';
import { Trophy } from 'lucide-react';
import { useState } from 'react';
import { Card, Pill } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta, n1 } from '@/lib/format';

// Recordes do passaporte de performance: a maior carga registrada em cada exercicio (180 dias).
// Paciente usa /api/portal/me, equipe usa a rota do paciente.

interface Recorde {
  exercicioId: string;
  exercicio: string;
  grupoMuscular: string | null;
  tipo: string;
  valor: number;
  unidade: string | null;
  dataUtc: string;
  recente: boolean;
  registrosComparaveis: number;
  evolucaoDesdePrimeiroPercentual: number | null;
}

export function RecordesCard({ pacienteId }: { pacienteId?: string }) {
  const [todos, setTodos] = useState(false);
  const base = pacienteId ? `/api/pacientes/${pacienteId}` : '/api/portal/me';
  const q = useQuery({
    queryKey: [pacienteId ? 'pro' : 'p', 'recordes', pacienteId ?? 'me'],
    queryFn: () => api<Recorde[]>(`${base}/performance/passaporte/recordes`),
  });
  const lista = [...(q.data ?? [])].filter((r) => r.tipo === 'MelhorCarga').sort((a, b) => b.dataUtc.localeCompare(a.dataUtc));
  if (!lista.length) return null;
  const visiveis = todos ? lista : lista.slice(0, 6);
  const recentes = lista.filter((r) => r.recente).length;
  return (
    <section>
      <div className="mb-2 flex items-end justify-between gap-3">
        <div>
          <p className="font-semibold">{pacienteId ? 'Recordes' : 'Seus recordes'}</p>
          <p className="text-[13px] text-muted">Maior carga em cada exercício nos últimos 6 meses.</p>
        </div>
        {recentes > 0 && <Pill tone="ok">{recentes} {recentes === 1 ? 'recente' : 'recentes'}</Pill>}
      </div>
      <Card className="divide-y divide-line/60">
        {visiveis.map((r) => (
          <div key={r.exercicioId} className="flex items-center gap-3 px-4 py-3">
            <Trophy className={r.recente ? 'h-4 w-4 shrink-0 text-warn' : 'h-4 w-4 shrink-0 text-faint'} />
            <span className="min-w-0 flex-1">
              <span className="block truncate font-semibold">{r.exercicio}</span>
              <span className="block truncate text-[13px] text-muted">
                {[dataCurta(r.dataUtc), r.evolucaoDesdePrimeiroPercentual ? `+${n1(r.evolucaoDesdePrimeiroPercentual)}% desde o início` : null].filter(Boolean).join(' · ')}
              </span>
            </span>
            <span className="num shrink-0 text-lg font-semibold">
              {n1(r.valor)}
              <span className="ml-0.5 text-[12px] font-normal text-muted">{r.unidade ?? ''}</span>
            </span>
          </div>
        ))}
        {lista.length > 6 && (
          <button onClick={() => setTodos((x) => !x)} className="w-full px-4 py-3 text-center text-[14px] font-semibold text-accent">
            {todos ? 'Mostrar menos' : `Ver todos (${lista.length})`}
          </button>
        )}
      </Card>
    </section>
  );
}
