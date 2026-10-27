import clsx from 'clsx';
import { motion } from 'framer-motion';
import { Activity, Clock, Flame, Moon, Trophy, TrendingDown, TrendingUp } from 'lucide-react';
import { useMemo } from 'react';
import { Page, PageHeader } from '@/components/shell';
import { Card, ErrorBox, Skeleton, Sparkline } from '@/components/ui';
import { dataCurta, diaSemanaCurto, n0, n1 } from '@/lib/format';
import { CheckInsSecao } from '@/features/checkins';
import { RecordesCard } from '@/features/recordes';
import { CorpoSecao, ExamesSecao, SinaisSecao } from './BodyAndExams';
import { useHistoricoDiario, useHistoricoTreinos } from './queries';

const DIA = 86400000;
const chave = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

export function ProgressPage() {
  const diario = useHistoricoDiario(30);
  const hist = useHistoricoTreinos(84);

  const dias = useMemo(() => [...(diario.data?.itens ?? [])].sort((a, b) => a.data.localeCompare(b.data)), [diario.data]);
  const comScore = dias.map((d) => d.prontidaoScore).filter((s): s is number => s != null);
  const media = comScore.length ? comScore.reduce((a, b) => a + b, 0) / comScore.length : null;
  const ultimo = comScore.length ? comScore[comScore.length - 1] : null;
  const sono = dias.slice(-14);
  const sonoMedio = sono.filter((d) => d.sonoHoras != null).reduce((a, d, _, l) => a + (d.sonoHoras ?? 0) / l.length, 0);

  // 12 semanas (segunda a domingo) com os treinos de cada dia, no fuso do aparelho.
  const { semanas, total12, sequencia } = useMemo(() => {
    const porDia = new Map<string, number>();
    for (const e of hist.data?.execucoes ?? []) {
      const k = chave(new Date(e.dataHoraInicioUtc));
      porDia.set(k, (porDia.get(k) ?? 0) + 1);
    }
    const hoje = new Date();
    hoje.setHours(12, 0, 0, 0);
    const offset = (hoje.getDay() + 6) % 7;
    const inicio = new Date(hoje.getTime() - (offset + 7 * 11) * DIA);
    const semanas = Array.from({ length: 12 }, (_, w) =>
      Array.from({ length: 7 }, (_, d) => {
        const dt = new Date(inicio.getTime() + (w * 7 + d) * DIA);
        const k = chave(dt);
        return { k, n: porDia.get(k) ?? 0, futuro: dt > hoje };
      }),
    );
    // Semanas seguidas com pelo menos um treino, contando a atual.
    let sequencia = 0;
    for (let w = 11; w >= 0; w--) {
      if (semanas[w].some((d) => d.n > 0)) sequencia++;
      else if (w !== 11) break;
    }
    return { semanas, total12: [...porDia.values()].reduce((a, b) => a + b, 0), sequencia };
  }, [hist.data]);

  // Evolucao de carga por exercicio (pelo nome, para atravessar versoes do plano).
  const exercicios = useMemo(() => {
    const m = new Map<string, { data: string; carga: number }[]>();
    for (const e of [...(hist.data?.execucoes ?? [])].sort((a, b) => a.dataHoraInicioUtc.localeCompare(b.dataHoraInicioUtc)))
      for (const i of e.itens) if (i.cargaRealizada != null) m.set(i.exercicio, [...(m.get(i.exercicio) ?? []), { data: e.dataHoraInicioUtc, carga: i.cargaRealizada }]);
    return [...m.entries()]
      .filter(([, v]) => v.length >= 2)
      .map(([nome, v]) => ({ nome, v, delta: v[v.length - 1].carga - v[0].carga, pct: v[0].carga ? ((v[v.length - 1].carga - v[0].carga) / v[0].carga) * 100 : 0 }))
      .sort((a, b) => b.pct - a.pct);
  }, [hist.data]);

  const execs = hist.data?.execucoes ?? [];
  const ult30 = execs.filter((e) => Date.now() - new Date(e.dataHoraInicioUtc).getTime() < 30 * DIA);
  const minutos = ult30.reduce((a, e) => a + (e.duracaoMinutos ?? 0), 0);
  const subindo = exercicios.filter((e) => e.delta > 0).length;

  const kpis = [
    { icon: Flame, v: String(ult30.length), l: 'treinos em 30 dias', c: 'text-warn' },
    { icon: Clock, v: minutos >= 60 ? `${n0(minutos / 60)}h` : `${minutos}min`, l: 'de treino em 30 dias', c: 'text-info' },
    { icon: Activity, v: media != null ? n0(media) : '—', l: 'prontidão média', c: 'text-accent' },
    { icon: Trophy, v: String(subindo), l: subindo === 1 ? 'exercício com carga subindo' : 'exercícios com carga subindo', c: 'text-ok' },
  ];

  return (
    <Page wide>
      <PageHeader eyebrow="Progresso" title="Sua evolução" subtitle="O que os seus registros mostram nas últimas semanas." />
      {(diario.isError || hist.isError) && <ErrorBox error={diario.error ?? hist.error} />}

      <div className="grid grid-cols-2 gap-2 sm:gap-3 lg:grid-cols-4">
        {kpis.map((k, i) => (
          <motion.div key={k.l} initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.05 }}>
            <Card className="h-full p-4">
              <k.icon className={clsx('h-4 w-4', k.c)} />
              <p className="num mt-2.5 text-[28px] font-semibold leading-none">{k.v}</p>
              <p className="mt-1.5 text-[12px] leading-tight text-muted">{k.l}</p>
            </Card>
          </motion.div>
        ))}
      </div>

      <div className="mt-6 grid gap-4 lg:grid-cols-2">
        {/* Prontidao e sono */}
        <Card className="p-5">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="eyebrow">Prontidão</p>
              <p className="num mt-1.5 font-display text-[40px] font-semibold leading-none">{ultimo ?? '—'}</p>
              {ultimo != null && media != null && (
                <p className={clsx('num mt-1.5 text-[13px] font-semibold', ultimo >= media ? 'text-ok' : 'text-warn')}>
                  {ultimo >= media ? '+' : ''}
                  {n0(ultimo - media)} vs. sua média
                </p>
              )}
            </div>
            <span className="rounded-full bg-raised px-3 py-1 text-[12px] text-muted">últimos {dias.length} dias</span>
          </div>
          {diario.isLoading ? <Skeleton className="mt-4 h-20" /> : <Sparkline points={comScore} min={0} max={100} height={72} className="mt-4" />}

          <div className="mt-6 flex items-center justify-between">
            <p className="flex items-center gap-1.5 text-sm font-semibold">
              <Moon className="h-4 w-4 text-info" /> Sono
            </p>
            {sonoMedio > 0 && <p className="num text-[13px] text-muted">média {n1(sonoMedio)}h por noite</p>}
          </div>
          <div className="mt-3 flex h-20 items-end gap-1">
            {sono.map((d) => {
              const h = d.sonoHoras ?? 0;
              return (
                <div key={d.data} className="flex h-full flex-1 flex-col items-center justify-end gap-1" title={`${dataCurta(d.data)}: ${h}h`}>
                  <motion.div
                    className={clsx('w-full rounded-[5px]', h === 0 ? 'bg-raised' : h < 6 ? 'bg-warn/70' : 'bg-info/70')}
                    initial={{ height: 0 }}
                    animate={{ height: `${Math.max(6, Math.min(100, (h / 10) * 100))}%` }}
                    transition={{ duration: 0.6 }}
                  />
                </div>
              );
            })}
          </div>
          <div className="mt-1 flex gap-1">
            {sono.map((d) => (
              <span key={d.data} className="flex-1 text-center text-[10px] uppercase text-faint">
                {diaSemanaCurto(d.data)}
              </span>
            ))}
          </div>
        </Card>

        {/* Consistencia */}
        <Card className="p-5">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="eyebrow">Consistência</p>
              <p className="num mt-1.5 font-display text-[40px] font-semibold leading-none">{total12}</p>
              <p className="mt-1.5 text-[13px] text-muted">treinos nas últimas 12 semanas</p>
            </div>
            {sequencia > 0 && (
              <span className="flex items-center gap-1 rounded-full bg-warn/15 px-3 py-1 text-[12px] font-semibold text-warn">
                <Flame className="h-3.5 w-3.5" />
                {sequencia} {sequencia > 1 ? 'semanas seguidas' : 'semana'}
              </span>
            )}
          </div>
          {hist.isLoading ? (
            <Skeleton className="mt-5 h-36" />
          ) : (
            <>
              <div className="mt-5 flex gap-2">
                <div className="flex flex-col justify-between py-0.5 text-[10px] text-faint">
                  <span>Seg</span>
                  <span>Qui</span>
                  <span>Dom</span>
                </div>
                <div className="flex flex-1 justify-between gap-1">
                  {semanas.map((s, w) => (
                    <div key={w} className="flex flex-1 flex-col gap-1">
                      {s.map((d) => (
                        <div
                          key={d.k}
                          title={`${dataCurta(d.k)}: ${d.n} treino(s)`}
                          className={clsx('h-[15px] rounded-[4px] sm:h-[18px]', d.futuro ? 'bg-transparent' : d.n === 0 ? 'bg-raised' : d.n === 1 ? 'bg-accent/75' : 'bg-accent')}
                        />
                      ))}
                    </div>
                  ))}
                </div>
              </div>
              <div className="mt-3 flex items-center justify-end gap-1.5 text-[11px] text-faint">
                sem treino <i className="h-3 w-3 rounded-[3px] bg-raised" /> <i className="h-3 w-3 rounded-[3px] bg-accent/75" /> <i className="h-3 w-3 rounded-[3px] bg-accent" /> mais treinos
              </div>
            </>
          )}
        </Card>
      </div>

      <div className="mb-3 mt-8 flex items-end justify-between">
        <h2 className="font-display text-[17px] font-semibold tracking-tight">Evolução de carga</h2>
        {!!exercicios.length && <span className="text-sm text-muted">{exercicios.length} exercícios</span>}
      </div>
      {hist.isLoading ? (
        <Skeleton className="h-40" />
      ) : !exercicios.length ? (
        <Card className="p-5 text-sm text-muted">Registre o mesmo exercício em dois treinos para ver a evolução da carga.</Card>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {exercicios.slice(0, 9).map((e, i) => (
            <motion.div key={e.nome} initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: Math.min(i, 6) * 0.04 }}>
              <Card className="flex h-full flex-col p-4">
                <div className="flex items-start justify-between gap-3">
                  <p className="min-w-0 font-semibold leading-snug">{e.nome}</p>
                  <span
                    className={clsx(
                      'num flex shrink-0 items-center gap-1 rounded-full px-2 py-0.5 text-[13px] font-semibold',
                      e.delta > 0 ? 'bg-ok/15 text-ok' : e.delta < 0 ? 'bg-bad/15 text-bad' : 'bg-raised text-muted',
                    )}
                  >
                    {e.delta > 0 ? <TrendingUp className="h-3.5 w-3.5" /> : e.delta < 0 ? <TrendingDown className="h-3.5 w-3.5" /> : null}
                    {e.delta > 0 ? '+' : ''}
                    {n0(e.pct)}%
                  </span>
                </div>
                <div className="mt-auto pt-3">
                  <Sparkline points={e.v.map((x) => x.carga)} height={40} color={e.delta < 0 ? 'rgb(var(--bad))' : undefined} />
                  <p className="num mt-2 flex justify-between text-xs text-muted">
                    <span>
                      {n1(e.v[0].carga)} → <span className="font-semibold text-fg">{n1(e.v[e.v.length - 1].carga)} kg</span>
                    </span>
                    <span>{e.v.length} treinos</span>
                  </p>
                </div>
              </Card>
            </motion.div>
          ))}
        </div>
      )}
      <div className="mt-8">
        <RecordesCard />
      </div>
      <CorpoSecao />
      <div className="mt-8">
        <CheckInsSecao paciente={null} titulo="Check-ins de evolução" />
      </div>
      <SinaisSecao />
      <ExamesSecao />
    </Page>
  );
}
