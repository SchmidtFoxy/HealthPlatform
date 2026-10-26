import clsx from 'clsx';
import { motion } from 'framer-motion';
import { ArrowLeftRight, Check, ChevronRight, Pill as PillIcon, Salad } from 'lucide-react';
import { MouseEvent, useState } from 'react';
import { Page, PageHeader } from '@/components/shell';
import { Empty, ErrorBox, Pill, Ring, Skeleton, toast } from '@/components/ui';
import { hora, n0, n1 } from '@/lib/format';
import type { Nutrientes, RefeicaoPortal, StatusRefeicao } from '@/lib/types';
import { MacroDots, MealSheet, STATUS_INFO, somaNutrientes } from './meals';
import { useAdesaoRefeicao, useHome, usePlanoAlimentar } from './queries';

const MACROS = [
  { k: 'proteinas', label: 'Proteína', cor: 'bg-prot', kcal: 4 },
  { k: 'carboidratos', label: 'Carboidrato', cor: 'bg-carb', kcal: 4 },
  { k: 'gorduras', label: 'Gordura', cor: 'bg-fat', kcal: 9 },
] as const;

export function DietPage() {
  const q = usePlanoAlimentar();
  const home = useHome();
  const [aberta, setAberta] = useState<RefeicaoPortal | null>(null);
  const plano = q.data?.plano;
  const status = new Map<string, StatusRefeicao>((home.data?.adesaoNutricional.refeicoes ?? []).map((r) => [r.refeicaoId, r.status]));
  const tot = plano ? somaNutrientes(plano.refeicoes.flatMap((r) => r.itens)) : null;
  const consumido = plano ? somaNutrientes(plano.refeicoes.filter((r) => ['Realizada', 'Adaptada'].includes(status.get(r.id) ?? '')).flatMap((r) => r.itens)) : null;
  const proxima = plano?.refeicoes.find((r) => (status.get(r.id) ?? 'Pendente') === 'Pendente');

  return (
    <Page wide>
      <PageHeader eyebrow="Alimentação" title={plano?.nome ?? 'Seu plano'} subtitle={plano ? `por ${plano.profissional}` : undefined} />
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && <Skeleton className="h-48" />}
      {q.data && !plano && <Empty icon={<Salad className="h-5 w-5" />} title="Plano ainda não liberado">Quando seu profissional publicar a dieta, ela aparece aqui.</Empty>}

      {plano && tot && consumido && (
        <div className="lg:grid lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)] lg:items-start lg:gap-8">
          {/* Resumo do dia: no PC fica fixo ao lado da lista */}
          <aside className="lg:sticky lg:top-6">
            <Resumo tot={tot} consumido={consumido} />
            {plano.observacoes && (
              <div className="mt-3 rounded-xl2 border border-line bg-surface p-4">
                <p className="eyebrow">Orientação do profissional</p>
                <p className="mt-1.5 text-[15px] leading-relaxed">{plano.observacoes}</p>
              </div>
            )}
          </aside>

          <section className="mt-6 lg:mt-0">
            <div className="mb-3 flex items-end justify-between">
              <h2 className="font-display text-[17px] font-semibold tracking-tight">Refeições</h2>
              <span className="num text-sm text-muted">
                {plano.refeicoes.filter((r) => (status.get(r.id) ?? 'Pendente') !== 'Pendente').length}/{plano.refeicoes.length} registradas
              </span>
            </div>
            <ol className="relative flex flex-col gap-3">
              <span aria-hidden className="absolute bottom-8 left-[43px] top-8 w-px bg-line" />
              {plano.refeicoes.map((r, idx) => (
                <motion.li key={r.id} initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: idx * 0.04 }}>
                  <Refeicao refeicao={r} status={status.get(r.id) ?? 'Pendente'} destaque={proxima?.id === r.id} onAbrir={() => setAberta(r)} />
                </motion.li>
              ))}
            </ol>
            {!!plano.suplementos?.length && (
              <>
                <h2 className="mb-3 mt-7 font-display text-[17px] font-semibold tracking-tight">Suplementos</h2>
                <ul className="flex flex-col gap-2">
                  {plano.suplementos.map((x) => (
                    <li key={x.id} className="flex items-start gap-3 rounded-2xl border border-line bg-surface p-3.5">
                      <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-accent/10 text-accent">
                        <PillIcon className="h-5 w-5" />
                      </span>
                      <div className="min-w-0 flex-1">
                        <p className="font-semibold">
                          {x.nome}
                          {x.marca && <span className="font-normal text-muted"> · {x.marca}</span>}
                        </p>
                        <p className="text-[13px] text-muted">
                          {[`${n1(x.quantidadePorcoes * x.porcaoQuantidade)} ${x.porcaoUnidade}`, x.contexto, x.horario && hora(x.horario), x.refeicao && `com ${x.refeicao}`, x.sessaoTreino && `treino ${x.sessaoTreino}`]
                            .filter(Boolean)
                            .join(' · ')}
                        </p>
                        {x.observacoes && <p className="mt-1 text-[13px]">{x.observacoes}</p>}
                      </div>
                      {x.calorias > 0 && <span className="num shrink-0 text-[13px] text-muted">{n0(x.calorias)} kcal</span>}
                    </li>
                  ))}
                </ul>
              </>
            )}
          </section>
        </div>
      )}
      <MealSheet refeicao={aberta} status={aberta ? (status.get(aberta.id) ?? 'Pendente') : 'Pendente'} onClose={() => setAberta(null)} />
    </Page>
  );
}

function Resumo({ tot, consumido }: { tot: Nutrientes; consumido: Nutrientes }) {
  const kcalMacros = tot.proteinas * 4 + tot.carboidratos * 4 + tot.gorduras * 9 || 1;
  const restante = Math.max(0, tot.calorias - consumido.calorias);
  return (
    <div className="aurora rounded-xl3 border border-line p-5">
      <div className="flex items-center gap-5">
        <Ring value={consumido.calorias} max={Math.max(1, tot.calorias)} size={128} stroke={11}>
          <div>
            <p className="num text-[28px] font-semibold leading-none">{n0(consumido.calorias)}</p>
            <p className="num mt-1 text-xs text-muted">de {n0(tot.calorias)} kcal</p>
          </div>
        </Ring>
        <div className="min-w-0 flex-1">
          <p className="eyebrow">Ainda hoje</p>
          <p className="num mt-1 font-display text-[26px] font-semibold leading-none">
            {n0(restante)}
            <span className="ml-1 text-sm font-normal text-muted">kcal</span>
          </p>
          <p className="mt-2 text-[13px] text-muted">Conta o que você marcou como feito ou adaptado.</p>
        </div>
      </div>

      <div className="mt-5 grid grid-cols-3 gap-2">
        {MACROS.map((m) => {
          const v = consumido[m.k];
          const meta = tot[m.k];
          return (
            <div key={m.k} className="rounded-2xl bg-surface/70 p-3">
              <div className="flex items-center gap-1.5 text-[12px] text-muted">
                <i className={clsx('h-2 w-2 rounded-full', m.cor)} />
                {m.label}
              </div>
              <p className="num mt-1.5 text-lg font-semibold leading-none">
                {n0(v)}
                <span className="text-xs font-normal text-faint">/{n0(meta)}g</span>
              </p>
              <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-line/60">
                <motion.div className={clsx('h-full rounded-full', m.cor)} initial={{ width: 0 }} animate={{ width: `${Math.min(100, (v / Math.max(1, meta)) * 100)}%` }} transition={{ duration: 0.8 }} />
              </div>
            </div>
          );
        })}
      </div>

      <div className="mt-5">
        <div className="flex h-2 overflow-hidden rounded-full">
          {MACROS.map((m) => (
            <div key={m.k} className={m.cor} style={{ width: `${((tot[m.k] * m.kcal) / kcalMacros) * 100}%` }} />
          ))}
        </div>
        <div className="num mt-2 flex justify-between text-xs text-muted">
          {MACROS.map((m) => (
            <span key={m.k}>
              {m.label} {n0(((tot[m.k] * m.kcal) / kcalMacros) * 100)}%
            </span>
          ))}
        </div>
      </div>
    </div>
  );
}

function Refeicao({ refeicao: r, status: st, destaque, onAbrir }: { refeicao: RefeicaoPortal; status: StatusRefeicao; destaque: boolean; onAbrir: () => void }) {
  const m = useAdesaoRefeicao();
  const n = somaNutrientes(r.itens);
  const trocas = r.itens.reduce((a, i) => a + i.substituicoes.length, 0);
  const feita = st === 'Realizada';

  function marcarFeita(e: MouseEvent) {
    e.stopPropagation();
    if (navigator.vibrate) navigator.vibrate(12);
    m.mutate({ refeicaoId: r.id, status: 'Realizada' }, { onError: (err) => toast(err.message, 'bad') });
  }

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onAbrir}
      onKeyDown={(e) => e.key === 'Enter' && onAbrir()}
      className={clsx(
        'relative flex cursor-pointer items-start gap-4 rounded-xl2 border bg-surface p-4 transition hover:border-accent/40 active:scale-[0.995]',
        destaque ? 'border-accent/50 shadow-glow' : 'border-line',
      )}
    >
      <span
        className={clsx(
          'num z-10 grid h-[54px] w-[54px] shrink-0 place-items-center rounded-2xl border text-[13px] font-semibold',
          st === 'Pendente' && (destaque ? 'border-accent bg-accent/10 text-accent' : 'border-line bg-surface'),
          st === 'Realizada' && 'border-transparent bg-ok/15 text-ok',
          st === 'Adaptada' && 'border-transparent bg-warn/15 text-warn',
          st === 'NaoRealizada' && 'border-transparent bg-bad/15 text-bad',
        )}
      >
        {hora(r.horario) || '—'}
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <p className={clsx('truncate font-semibold', st === 'NaoRealizada' && 'text-muted line-through')}>{r.nome}</p>
          {destaque && <Pill tone="accent">Próxima</Pill>}
          {st !== 'Pendente' && <Pill tone={STATUS_INFO[st].tone === 'neutral' ? 'neutral' : STATUS_INFO[st].tone}>{STATUS_INFO[st].label}</Pill>}
        </div>
        <p className="mt-0.5 line-clamp-2 text-[13px] text-muted">{r.itens.map((i) => i.alimento).join(' · ')}</p>
        <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1">
          <span className="num text-[13px] font-semibold">{n0(n.calorias)} kcal</span>
          <MacroDots n={n} />
        </div>
        {trocas > 0 && (
          <p className="mt-1.5 inline-flex items-center gap-1 text-[12px] font-semibold text-accent">
            <ArrowLeftRight className="h-3.5 w-3.5" />
            {trocas} {trocas > 1 ? 'opções de troca' : 'opção de troca'}
          </p>
        )}
      </div>

      {st === 'Pendente' ? (
        <button
          aria-label={`Marcar ${r.nome} como feita`}
          onClick={marcarFeita}
          className={clsx('grid h-11 w-11 shrink-0 place-items-center rounded-full transition active:scale-90', destaque ? 'bg-ok text-white' : 'bg-raised text-muted hover:bg-ok/15 hover:text-ok')}
        >
          <Check className="h-5 w-5" strokeWidth={2.6} />
        </button>
      ) : (
        <ChevronRight className={clsx('mt-4 h-5 w-5 shrink-0', feita ? 'text-ok' : 'text-faint')} />
      )}
    </div>
  );
}
