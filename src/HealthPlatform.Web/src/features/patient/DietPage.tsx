import clsx from 'clsx';
import { motion } from 'framer-motion';
import { ChevronRight, Salad } from 'lucide-react';
import { useState } from 'react';
import { Page, PageHeader } from '@/components/shell';
import { Empty, ErrorBox, Pill, Ring, Skeleton } from '@/components/ui';
import { hora, n0 } from '@/lib/format';
import type { RefeicaoPortal, StatusRefeicao } from '@/lib/types';
import { MacroDots, MealSheet, STATUS_INFO, somaNutrientes } from './meals';
import { useHome, usePlanoAlimentar } from './queries';

export function DietPage() {
  const q = usePlanoAlimentar();
  const home = useHome();
  const [aberta, setAberta] = useState<RefeicaoPortal | null>(null);
  const plano = q.data?.plano;
  const status = new Map<string, StatusRefeicao>((home.data?.adesaoNutricional.refeicoes ?? []).map((r) => [r.refeicaoId, r.status]));
  const tot = plano ? somaNutrientes(plano.refeicoes.flatMap((r) => r.itens)) : null;
  const consumido = plano ? somaNutrientes(plano.refeicoes.filter((r) => ['Realizada', 'Adaptada'].includes(status.get(r.id) ?? '')).flatMap((r) => r.itens)) : null;
  const kcalMacros = tot ? tot.proteinas * 4 + tot.carboidratos * 4 + tot.gorduras * 9 || 1 : 1;

  return (
    <Page>
      <PageHeader eyebrow="Alimentação" title={plano?.nome ?? 'Seu plano'} subtitle={plano ? `por ${plano.profissional}` : undefined} />
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && <Skeleton className="h-48" />}
      {q.data && !plano && <Empty icon={<Salad className="h-5 w-5" />} title="Plano ainda não liberado">Quando seu profissional publicar a dieta, ela aparece aqui.</Empty>}

      {plano && tot && consumido && (
        <>
          <div className="aurora rounded-xl3 border border-line p-5">
            <div className="flex items-center gap-5">
              <Ring value={consumido.calorias} max={tot.calorias} size={124} stroke={11}>
                <div>
                  <p className="num text-[26px] font-semibold leading-none">{n0(consumido.calorias)}</p>
                  <p className="num mt-1 text-xs text-muted">de {n0(tot.calorias)} kcal</p>
                </div>
              </Ring>
              <div className="flex min-w-0 flex-1 flex-col gap-3">
                {[
                  ['Proteína', consumido.proteinas, tot.proteinas, 'bg-prot'],
                  ['Carboidrato', consumido.carboidratos, tot.carboidratos, 'bg-carb'],
                  ['Gordura', consumido.gorduras, tot.gorduras, 'bg-fat'],
                ].map(([l, v, m, c]) => (
                  <div key={l as string}>
                    <div className="flex justify-between text-[13px]">
                      <span className="text-muted">{l}</span>
                      <span className="num font-semibold">
                        {n0(v as number)}
                        <span className="font-normal text-faint">/{n0(m as number)}g</span>
                      </span>
                    </div>
                    <div className="mt-1 h-1.5 overflow-hidden rounded-full bg-line/60">
                      <motion.div className={clsx('h-full rounded-full', c)} initial={{ width: 0 }} animate={{ width: `${Math.min(100, ((v as number) / Math.max(1, m as number)) * 100)}%` }} transition={{ duration: 0.8 }} />
                    </div>
                  </div>
                ))}
              </div>
            </div>
            <div className="mt-5">
              <p className="eyebrow mb-2">Distribuição do plano</p>
              <div className="flex h-2.5 overflow-hidden rounded-full">
                <div className="bg-prot" style={{ width: `${((tot.proteinas * 4) / kcalMacros) * 100}%` }} />
                <div className="bg-carb" style={{ width: `${((tot.carboidratos * 4) / kcalMacros) * 100}%` }} />
                <div className="bg-fat" style={{ width: `${((tot.gorduras * 9) / kcalMacros) * 100}%` }} />
              </div>
              <div className="num mt-2 flex justify-between text-xs text-muted">
                <span>P {n0(((tot.proteinas * 4) / kcalMacros) * 100)}%</span>
                <span>C {n0(((tot.carboidratos * 4) / kcalMacros) * 100)}%</span>
                <span>G {n0(((tot.gorduras * 9) / kcalMacros) * 100)}%</span>
              </div>
            </div>
          </div>

          {plano.observacoes && (
            <div className="mt-4 rounded-xl2 border border-line bg-surface p-4">
              <p className="eyebrow">Orientação do profissional</p>
              <p className="mt-1.5 text-[15px]">{plano.observacoes}</p>
            </div>
          )}

          <div className="mt-6 flex flex-col gap-3">
            {plano.refeicoes.map((r, idx) => {
              const n = somaNutrientes(r.itens);
              const st = status.get(r.id) ?? 'Pendente';
              const trocas = r.itens.reduce((a, i) => a + i.substituicoes.length, 0);
              return (
                <motion.button
                  key={r.id}
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: idx * 0.04 }}
                  onClick={() => setAberta(r)}
                  className="flex w-full items-center gap-4 rounded-xl2 border border-line bg-surface p-4 text-left transition active:scale-[0.99]"
                >
                  <div className="w-12 shrink-0 text-center">
                    <p className="num text-[15px] font-semibold">{hora(r.horario) || '—'}</p>
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <p className="truncate font-semibold">{r.nome}</p>
                      {st !== 'Pendente' && <Pill tone={STATUS_INFO[st].tone}>{STATUS_INFO[st].label}</Pill>}
                    </div>
                    <p className="mt-0.5 truncate text-[13px] text-muted">{r.itens.map((i) => i.alimento).join(', ')}</p>
                    <div className="mt-1.5 flex items-center gap-3">
                      <span className="num text-[13px] font-semibold">{n0(n.calorias)} kcal</span>
                      <MacroDots n={n} />
                    </div>
                    {trocas > 0 && <p className="mt-1 text-[12px] font-semibold text-accent">{trocas} opções de troca</p>}
                  </div>
                  <ChevronRight className="h-5 w-5 shrink-0 text-faint" />
                </motion.button>
              );
            })}
          </div>
        </>
      )}
      <MealSheet refeicao={aberta} status={aberta ? (status.get(aberta.id) ?? 'Pendente') : 'Pendente'} onClose={() => setAberta(null)} />
    </Page>
  );
}
