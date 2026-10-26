import clsx from 'clsx';
import { ChevronDown, Dumbbell, Play } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Pill, SectionTitle, Skeleton } from '@/components/ui';
import { dataCurta, n1, sessaoEhDoDia } from '@/lib/format';
import { useHistoricoTreinos, useMeusTreinos } from './queries';

export function WorkoutPlanPage() {
  const nav = useNavigate();
  const q = useMeusTreinos();
  const hist = useHistoricoTreinos(60);
  const plano = q.data?.planos[0];
  const [aberta, setAberta] = useState<string | null>(null);

  return (
    <Page>
      <PageHeader eyebrow="Treino" title={plano?.nome ?? 'Seu treino'} subtitle={plano ? `${plano.objetivo ?? 'Plano ativo'} · por ${plano.profissional}` : undefined} />
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-28" />
          <Skeleton className="h-28" />
          <Skeleton className="h-28" />
        </div>
      )}
      {q.data && !plano && <Empty icon={<Dumbbell className="h-5 w-5" />} title="Nenhum plano liberado">Quando seu profissional publicar o treino, ele aparece aqui.</Empty>}
      {plano?.observacoes && <p className="mb-4 rounded-xl2 border border-accent/30 bg-accent/10 p-3 text-sm">{plano.observacoes}</p>}

      <div className="flex flex-col gap-3">
        {plano?.sessoes.map((s) => {
          const hoje = sessaoEhDoDia(s.diasSemana) && !!s.diasSemana;
          const open = aberta === s.id;
          return (
            <Card key={s.id} className={clsx('overflow-hidden', hoje && 'border-accent/50')}>
              <button className="flex w-full items-center gap-4 p-4 text-left" onClick={() => setAberta(open ? null : s.id)} aria-expanded={open}>
                <div className={clsx('grid h-12 w-12 shrink-0 place-items-center rounded-2xl font-display text-lg font-semibold', hoje ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}>{s.nome.trim()[0]}</div>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-semibold">{s.nome}</p>
                  <p className="text-[13px] text-muted">
                    {s.itens.length} exercícios {s.diasSemana && `· ${s.diasSemana}`}
                  </p>
                </div>
                {hoje && <Pill tone="accent">Hoje</Pill>}
                <ChevronDown className={clsx('h-5 w-5 shrink-0 text-muted transition', open && 'rotate-180')} />
              </button>
              {open && (
                <div className="border-t border-line px-4 pb-4">
                  <ol className="divide-y divide-line/60">
                    {s.itens.map((it, i) => (
                      <li key={it.id} className="flex items-center gap-3 py-3">
                        <span className="num w-5 text-sm text-faint">{i + 1}</span>
                        <div className="min-w-0 flex-1">
                          <p className="truncate font-semibold">{it.exercicio}</p>
                          <p className="text-[13px] text-muted">{it.grupoMuscular}</p>
                        </div>
                        <p className="num shrink-0 text-right text-sm font-semibold">
                          {it.series}×{it.repeticoes}
                          {it.carga != null && <span className="block text-xs font-normal text-muted">{n1(it.carga)} {it.unidadeCarga ?? 'kg'}</span>}
                        </p>
                      </li>
                    ))}
                  </ol>
                  <Button className="mt-2 w-full" icon={<Play className="h-4 w-4 fill-current" />} onClick={() => nav(`/treino/${s.id}`)}>
                    Começar {s.nome.split('—')[0].trim()}
                  </Button>
                </div>
              )}
            </Card>
          );
        })}
      </div>

      {!!hist.data?.execucoes.length && (
        <>
          <SectionTitle>Últimos treinos</SectionTitle>
          <ul className="flex flex-col gap-2">
            {hist.data.execucoes.slice(0, 8).map((e) => (
              <li key={e.id} className="flex items-center gap-3 rounded-2xl border border-line bg-surface p-3">
                <div className="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-ok/15 text-center leading-none text-ok">
                  <span className="num text-[13px] font-semibold">{dataCurta(e.dataHoraInicioUtc).split(' ')[0]}</span>
                  <span className="text-[10px] uppercase">{dataCurta(e.dataHoraInicioUtc).split(' ')[1]}</span>
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-semibold">{e.sessao}</p>
                  <p className="text-[13px] text-muted">
                    {e.duracaoMinutos ?? '—'} min · {e.itens.filter((i) => i.concluido).length}/{e.itens.length} exercícios
                  </p>
                </div>
                {e.esforcoPercebido != null && <Pill tone={e.esforcoPercebido >= 9 ? 'bad' : e.esforcoPercebido >= 7 ? 'warn' : 'ok'}>RPE {e.esforcoPercebido}</Pill>}
              </li>
            ))}
          </ul>
        </>
      )}
    </Page>
  );
}
