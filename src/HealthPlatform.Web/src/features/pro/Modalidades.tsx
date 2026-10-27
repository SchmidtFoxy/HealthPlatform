import { useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { ChevronDown, Compass } from 'lucide-react';
import { useState } from 'react';
import { Card, ErrorBox, Pill, Sheet, Skeleton } from '@/components/ui';
import { api } from '@/lib/api';

// Biblioteca de movimento: o catalogo de exercicios organizado por modalidade, objetivo e capacidade,
// com progressao e regressao sugeridas. So leitura: os exercicios vem do catalogo da clinica.

interface ExercicioRef {
  id: string;
  nome: string;
  grupoMuscular: string | null;
  equipamento: string | null;
}
interface Capacidade {
  codigo: string;
  nome: string;
  sessoes: { id: string; nome: string; categoria: string | null }[];
  exercicios: ExercicioRef[];
  progressaoRegressao: { eixo: string; progressao: string; regressao: string; criterioDeUso: string }[];
}
interface Modalidade {
  codigo: string;
  nome: string;
  descricao: string;
  ambientes: string[];
  equipamentosComuns: string[];
  objetivos: { codigo: string; nome: string; capacidades: Capacidade[] }[];
}
interface Biblioteca {
  modalidades: Modalidade[];
  totalExerciciosAtivos: number;
  regraDeSeguranca: string;
}

export function ModalidadesSheet({ open, onClose, onExercicio }: { open: boolean; onClose: () => void; onExercicio: (id: string) => void }) {
  const q = useQuery({ queryKey: ['pro', 'biblioteca-movimento'], queryFn: () => api<Biblioteca>('/api/biblioteca-movimento'), enabled: open, staleTime: 5 * 60_000 });
  const [codigo, setCodigo] = useState<string | null>(null);
  const modalidades = q.data?.modalidades ?? [];
  const atual = modalidades.find((m) => m.codigo === codigo) ?? modalidades[0];
  const total = (m: Modalidade) => new Set(m.objetivos.flatMap((o) => o.capacidades.flatMap((c) => c.exercicios.map((e) => e.id)))).size;

  return (
    <Sheet open={open} onClose={onClose} title="Explorar por modalidade">
      <div className="flex flex-col gap-3">
        {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
        {q.isLoading ? (
          <Skeleton className="h-64" />
        ) : (
          atual && (
            <>
              <div className="-mx-1 overflow-x-auto px-1 pb-1">
                <div className="flex w-max gap-2">
                  {modalidades.map((m) => (
                    <button
                      key={m.codigo}
                      type="button"
                      onClick={() => setCodigo(m.codigo)}
                      className={clsx('h-9 shrink-0 rounded-full border px-3.5 text-sm font-semibold transition', atual.codigo === m.codigo ? 'border-accent bg-accent/10 text-accent' : 'border-line text-muted hover:bg-raised')}
                    >
                      {m.nome}
                    </button>
                  ))}
                </div>
              </div>
              <div>
                <p className="text-[14px] text-muted">{atual.descricao}</p>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  {atual.ambientes.map((a) => (
                    <Pill key={a} tone="info">
                      {a}
                    </Pill>
                  ))}
                  {atual.equipamentosComuns.slice(0, 5).map((a) => (
                    <Pill key={a}>{a}</Pill>
                  ))}
                </div>
                <p className="mt-2 text-[12px] text-faint">
                  {total(atual)} {total(atual) === 1 ? 'exercício do seu catálogo encaixa' : 'exercícios do seu catálogo encaixam'} aqui.
                </p>
              </div>
              {atual.objetivos.map((o) => (
                <section key={o.codigo}>
                  <p className="mb-1.5 text-[12px] font-semibold uppercase tracking-wide text-faint">{o.nome}</p>
                  <Card className="divide-y divide-line/60">
                    {o.capacidades.map((c) => (
                      <CapacidadeLinha key={c.codigo} c={c} onExercicio={onExercicio} />
                    ))}
                  </Card>
                </section>
              ))}
              <p className="text-[12px] text-faint">{q.data?.regraDeSeguranca}</p>
            </>
          )
        )}
      </div>
    </Sheet>
  );
}

function CapacidadeLinha({ c, onExercicio }: { c: Capacidade; onExercicio: (id: string) => void }) {
  const [aberto, setAberto] = useState(false);
  return (
    <div>
      <button onClick={() => setAberto((x) => !x)} className="flex w-full items-center gap-3 p-3.5 text-left transition hover:bg-raised/40" aria-expanded={aberto}>
        <Compass className="h-4 w-4 shrink-0 text-accent" />
        <span className="min-w-0 flex-1">
          <span className="block font-semibold">{c.nome}</span>
          <span className="block text-[13px] text-muted">
            {c.exercicios.length ? `${c.exercicios.length} ${c.exercicios.length === 1 ? 'exercício' : 'exercícios'}` : 'Nenhum exercício do catálogo ainda'}
            {c.sessoes.length ? ` · ${c.sessoes.length} ${c.sessoes.length === 1 ? 'sessão-modelo' : 'sessões-modelo'}` : ''}
          </span>
        </span>
        <ChevronDown className={clsx('h-4 w-4 shrink-0 text-faint transition', aberto && 'rotate-180')} />
      </button>
      {aberto && (
        <div className="flex flex-col gap-3 px-3.5 pb-3.5">
          {!!c.exercicios.length && (
            <div className="flex flex-wrap gap-1.5">
              {c.exercicios.map((e) => (
                <button key={e.id} onClick={() => onExercicio(e.id)} className="rounded-full bg-raised px-3 py-1.5 text-[13px] font-semibold transition hover:bg-accent/10 hover:text-accent">
                  {e.nome}
                </button>
              ))}
            </div>
          )}
          {!!c.sessoes.length && <p className="text-[13px] text-muted">Sessões-modelo: {c.sessoes.map((s) => s.nome).join(', ')}</p>}
          {c.progressaoRegressao.map((p) => (
            <div key={p.eixo} className="rounded-xl bg-raised/60 p-3 text-[13px]">
              <p className="font-semibold">{p.eixo}</p>
              <p className="mt-1">
                <span className="text-ok">Progredir:</span> {p.progressao}
              </p>
              <p>
                <span className="text-warn">Regredir:</span> {p.regressao}
              </p>
              <p className="mt-1 text-faint">{p.criterioDeUso}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
