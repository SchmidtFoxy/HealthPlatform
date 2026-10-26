import clsx from 'clsx';
import { motion } from 'framer-motion';
import { ChevronDown, Clock, Dumbbell, Flame, Play, Timer } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Pill, SectionTitle, Skeleton } from '@/components/ui';
import { dataCurta, n0, n1, sessaoEhDoDia } from '@/lib/format';
import type { SessaoTreinoPortal as SessaoTreino } from '@/lib/types';
import { useHistoricoTreinos, useMeusTreinos } from './queries';

const DIAS = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

const letra = (nome: string) => nome.trim()[0]?.toUpperCase() ?? '?';
const minutos = (s: SessaoTreino) => Math.round(s.itens.reduce((a, i) => a + i.series * ((i.descansoSegundos ?? 60) + 45), 0) / 60);

export function WorkoutPlanPage() {
  const nav = useNavigate();
  const q = useMeusTreinos();
  const hist = useHistoricoTreinos(60);
  const plano = q.data?.planos[0];
  const hoje = new Date();
  const sessaoHoje = plano?.sessoes.find((s) => !!s.diasSemana && sessaoEhDoDia(s.diasSemana));
  const [aberta, setAberta] = useState<string | null>(null);
  const abertaId = aberta ?? sessaoHoje?.id ?? null;

  // Semana atual, de domingo a sabado, com a sessao prevista em cada dia.
  const semana = DIAS.map((rotulo, dia) => {
    const d = new Date(hoje);
    d.setDate(hoje.getDate() - hoje.getDay() + dia);
    const sessao = plano?.sessoes.find((s) => !!s.diasSemana && sessaoEhDoDia(s.diasSemana, d));
    const feito = hist.data?.execucoes.some((e) => new Date(e.dataHoraInicioUtc).toDateString() === d.toDateString());
    return { rotulo, d, sessao, feito, ehHoje: dia === hoje.getDay() };
  });

  const ultimos30 = (hist.data?.execucoes ?? []).filter((e) => Date.now() - new Date(e.dataHoraInicioUtc).getTime() < 30 * 864e5);

  return (
    <Page wide>
      <PageHeader eyebrow="Treino" title={plano?.nome ?? 'Seu treino'} subtitle={plano ? `${plano.objetivo ?? 'Plano ativo'} · por ${plano.profissional}` : undefined} />
      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-20" />
          <Skeleton className="h-28" />
          <Skeleton className="h-28" />
        </div>
      )}
      {q.data && !plano && <Empty icon={<Dumbbell className="h-5 w-5" />} title="Nenhum plano liberado">Quando seu profissional publicar o treino, ele aparece aqui.</Empty>}

      {plano && (
        <>
          {/* Semana */}
          <div className="grid grid-cols-7 gap-1.5 rounded-xl3 border border-line bg-surface p-2 sm:gap-2 sm:p-3">
            {semana.map((s) => (
              <div
                key={s.rotulo}
                className={clsx(
                  'flex flex-col items-center gap-1 rounded-2xl py-2.5',
                  s.ehHoje ? 'bg-accent text-accent-ink' : s.sessao ? 'bg-raised/70' : '',
                )}
              >
                <span className={clsx('text-[11px] font-semibold', !s.ehHoje && 'text-muted')}>{s.rotulo}</span>
                <span className="num text-[15px] font-semibold">{s.d.getDate()}</span>
                <span
                  className={clsx(
                    'grid h-6 w-6 place-items-center rounded-full font-display text-[11px] font-bold',
                    s.feito ? (s.ehHoje ? 'bg-accent-ink/20' : 'bg-ok/20 text-ok') : s.sessao ? (s.ehHoje ? 'bg-accent-ink/15' : 'text-fg') : 'text-faint',
                  )}
                >
                  {s.sessao ? letra(s.sessao.nome) : '·'}
                </span>
              </div>
            ))}
          </div>

          {plano.observacoes && <p className="mt-3 rounded-xl2 border border-accent/30 bg-accent/10 px-4 py-3 text-sm">{plano.observacoes}</p>}

          <SectionTitle>Sessões do plano</SectionTitle>
          <div className="grid gap-3 lg:grid-cols-2 xl:grid-cols-3">
            {plano.sessoes.map((s, idx) => {
              const ehHoje = s.id === sessaoHoje?.id;
              const open = abertaId === s.id;
              const grupos = [...new Set(s.itens.map((i) => i.grupoMuscular).filter(Boolean))];
              return (
                <motion.div key={s.id} initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: idx * 0.05 }}>
                  <Card className={clsx('flex h-full flex-col overflow-hidden', ehHoje && 'border-accent/50 shadow-glow')}>
                    <button className="flex w-full items-center gap-4 p-4 text-left lg:cursor-default" onClick={() => setAberta(open ? '' : s.id)} aria-expanded={open}>
                      <div className={clsx('grid h-12 w-12 shrink-0 place-items-center rounded-2xl font-display text-lg font-semibold', ehHoje ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}>
                        {letra(s.nome)}
                      </div>
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2">
                          <p className="truncate font-semibold">{s.nome}</p>
                          {ehHoje && <Pill tone="accent">Hoje</Pill>}
                        </div>
                        <p className="mt-0.5 flex flex-wrap items-center gap-x-3 text-[13px] text-muted">
                          <span>{s.itens.length} exercícios</span>
                          <span className="inline-flex items-center gap-1">
                            <Clock className="h-3.5 w-3.5" />~{minutos(s)} min
                          </span>
                          {s.diasSemana && <span>{s.diasSemana}</span>}
                        </p>
                      </div>
                      <ChevronDown className={clsx('h-5 w-5 shrink-0 text-muted transition lg:hidden', open && 'rotate-180')} />
                    </button>

                    <div className={clsx('flex-1 flex-col border-t border-line px-4 pb-4', open ? 'flex' : 'hidden lg:flex')}>
                      {!!grupos.length && (
                        <div className="flex flex-wrap gap-1.5 pt-3">
                          {grupos.map((g) => (
                            <Pill key={g}>{g}</Pill>
                          ))}
                        </div>
                      )}
                      <ol className="mt-1 flex-1 divide-y divide-line/60">
                        {s.itens.map((it, i) => (
                          <li key={it.id} className="flex items-center gap-3 py-2.5">
                            <span className="num w-5 text-sm text-faint">{i + 1}</span>
                            <div className="min-w-0 flex-1">
                              <p className="truncate text-[15px] font-semibold">{it.exercicio}</p>
                              {it.tecnicaAvancada && <p className="truncate text-[12px] font-semibold text-accent">{it.tecnicaAvancada}</p>}
                            </div>
                            <p className="num shrink-0 text-right text-sm font-semibold">
                              {it.series}×{it.repeticoes}
                              {it.carga != null && <span className="block text-xs font-normal text-muted">{n1(it.carga)} {it.unidadeCarga ?? 'kg'}</span>}
                            </p>
                          </li>
                        ))}
                      </ol>
                      <Button variant={ehHoje ? 'primary' : 'soft'} className="mt-3 w-full" icon={<Play className="h-4 w-4 fill-current" />} onClick={() => nav(`/treino/${s.id}`)}>
                        Começar {s.nome.split('—')[0].trim()}
                      </Button>
                    </div>
                  </Card>
                </motion.div>
              );
            })}
          </div>
        </>
      )}

      {!!hist.data?.execucoes.length && (
        <>
          <SectionTitle>Últimos treinos</SectionTitle>
          <div className="mb-3 grid grid-cols-3 gap-2">
            {[
              { icon: Flame, v: ultimos30.length, l: 'treinos em 30 dias', c: 'text-warn' },
              { icon: Timer, v: n0(ultimos30.reduce((a, e) => a + (e.duracaoMinutos ?? 0), 0)), l: 'minutos em 30 dias', c: 'text-info' },
              {
                icon: Dumbbell,
                v: ultimos30.length ? n1(ultimos30.reduce((a, e) => a + (e.esforcoPercebido ?? 0), 0) / Math.max(1, ultimos30.filter((e) => e.esforcoPercebido != null).length)) : '—',
                l: 'esforço médio (RPE)',
                c: 'text-accent',
              },
            ].map((k) => (
              <Card key={k.l} className="p-3.5">
                <k.icon className={clsx('h-4 w-4', k.c)} />
                <p className="num mt-2 text-2xl font-semibold leading-none">{k.v}</p>
                <p className="mt-1 text-[12px] leading-tight text-muted">{k.l}</p>
              </Card>
            ))}
          </div>
          <ul className="grid grid-cols-1 gap-2 lg:grid-cols-2">
            {hist.data.execucoes.slice(0, 8).map((e) => {
              const [dia, mes] = dataCurta(e.dataHoraInicioUtc).split(' ');
              return (
                <li key={e.id} className="flex items-center gap-3 rounded-2xl border border-line bg-surface p-3">
                  <div className="grid h-12 w-12 shrink-0 place-items-center rounded-xl bg-ok/15 text-center leading-none text-ok">
                    <span className="num text-[15px] font-semibold">{dia}</span>
                    <span className="text-[10px] font-semibold uppercase">{mes}</span>
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-semibold">{e.sessao}</p>
                    <p className="text-[13px] text-muted">
                      {e.duracaoMinutos ?? '—'} min · {e.itens.filter((i) => i.concluido).length}/{e.itens.length} exercícios
                    </p>
                  </div>
                  {e.esforcoPercebido != null && <Pill tone={e.esforcoPercebido >= 9 ? 'bad' : e.esforcoPercebido >= 7 ? 'warn' : 'ok'}>RPE {e.esforcoPercebido}</Pill>}
                </li>
              );
            })}
          </ul>
        </>
      )}
    </Page>
  );
}
