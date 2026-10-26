import clsx from 'clsx';
import { TrendingDown, TrendingUp } from 'lucide-react';
import { useMemo } from 'react';
import { Page, PageHeader } from '@/components/shell';
import { Card, ErrorBox, SectionTitle, Skeleton, Sparkline } from '@/components/ui';
import { dataCurta, diaSemanaCurto, n0, n1 } from '@/lib/format';
import { useHistoricoDiario, useHistoricoTreinos } from './queries';

const DIA = 86400000;

export function ProgressPage() {
  const diario = useHistoricoDiario(30);
  const hist = useHistoricoTreinos(84);

  const dias = useMemo(() => [...(diario.data?.itens ?? [])].sort((a, b) => a.data.localeCompare(b.data)), [diario.data]);
  const scores = dias.map((d) => d.prontidaoScore);
  const comScore = scores.filter((s): s is number => s != null);
  const media = comScore.length ? comScore.reduce((a, b) => a + b, 0) / comScore.length : null;

  // Mapa de 12 semanas (segunda a domingo) com treinos concluidos.
  const semanas = useMemo(() => {
    const porDia = new Map<string, number>();
    for (const e of hist.data?.execucoes ?? []) {
      const k = new Date(e.dataHoraInicioUtc).toISOString().slice(0, 10);
      porDia.set(k, (porDia.get(k) ?? 0) + 1);
    }
    const hoje = new Date();
    hoje.setHours(12, 0, 0, 0);
    const offset = (hoje.getDay() + 6) % 7;
    const inicio = new Date(hoje.getTime() - (offset + 7 * 11) * DIA);
    return Array.from({ length: 12 }, (_, w) =>
      Array.from({ length: 7 }, (_, d) => {
        const dt = new Date(inicio.getTime() + (w * 7 + d) * DIA);
        const k = dt.toISOString().slice(0, 10);
        return { k, n: porDia.get(k) ?? 0, futuro: dt > hoje };
      }),
    );
  }, [hist.data]);

  // Evolucao de carga por exercicio (pelo nome, para atravessar versoes do plano).
  const exercicios = useMemo(() => {
    const m = new Map<string, { data: string; carga: number }[]>();
    for (const e of [...(hist.data?.execucoes ?? [])].sort((a, b) => a.dataHoraInicioUtc.localeCompare(b.dataHoraInicioUtc)))
      for (const i of e.itens) if (i.cargaRealizada != null) m.set(i.exercicio, [...(m.get(i.exercicio) ?? []), { data: e.dataHoraInicioUtc, carga: i.cargaRealizada }]);
    return [...m.entries()]
      .filter(([, v]) => v.length >= 2)
      .map(([nome, v]) => ({ nome, v, delta: v[v.length - 1].carga - v[0].carga }))
      .sort((a, b) => b.delta - a.delta);
  }, [hist.data]);

  const execs = hist.data?.execucoes ?? [];
  const ult30 = execs.filter((e) => Date.now() - new Date(e.dataHoraInicioUtc).getTime() < 30 * DIA);
  const minutos = ult30.reduce((a, e) => a + (e.duracaoMinutos ?? 0), 0);

  return (
    <Page>
      <PageHeader eyebrow="Progresso" title="Sua evolução" subtitle="O que os seus registros mostram nas últimas semanas." />
      {(diario.isError || hist.isError) && <ErrorBox error={diario.error ?? hist.error} />}

      <div className="grid grid-cols-3 gap-2">
        {[
          [String(ult30.length), 'treinos em 30 dias'],
          [n0(minutos / 60) + 'h', 'treinando'],
          [media != null ? n0(media) : '—', 'prontidão média'],
        ].map(([v, l]) => (
          <Card key={l} className="p-3">
            <p className="num text-[26px] font-semibold leading-tight">{v}</p>
            <p className="text-[12px] leading-tight text-muted">{l}</p>
          </Card>
        ))}
      </div>

      <SectionTitle>Prontidão</SectionTitle>
      <Card className="p-4">
        {diario.isLoading ? (
          <Skeleton className="h-24" />
        ) : (
          <>
            <div className="flex items-baseline justify-between">
              <p className="num text-3xl font-semibold">{comScore.length ? comScore[comScore.length - 1] : '—'}</p>
              <p className="text-sm text-muted">últimos {dias.length} dias</p>
            </div>
            <Sparkline points={scores} min={0} max={100} height={80} className="mt-3" />
            <div className="mt-4 grid grid-cols-7 gap-1.5">
              {dias.slice(-14).map((d) => (
                <div key={d.data} className="flex flex-col items-center gap-1">
                  <div className="flex h-14 w-full items-end overflow-hidden rounded-md bg-raised">
                    <div className="w-full rounded-md bg-info/70" style={{ height: `${Math.min(100, ((d.sonoHoras ?? 0) / 10) * 100)}%` }} title={`${d.sonoHoras ?? 0}h de sono`} />
                  </div>
                  <span className="text-[10px] uppercase text-faint">{diaSemanaCurto(d.data)}</span>
                </div>
              ))}
            </div>
            <p className="mt-2 text-xs text-muted">Barras: horas de sono por noite.</p>
          </>
        )}
      </Card>

      <SectionTitle>Consistência</SectionTitle>
      <Card className="p-4">
        {hist.isLoading ? (
          <Skeleton className="h-28" />
        ) : (
          <>
            <div className="flex gap-1.5">
              <div className="flex flex-col justify-between py-0.5 text-[10px] text-faint">
                <span>S</span>
                <span>Q</span>
                <span>D</span>
              </div>
              <div className="grid flex-1 grid-cols-12 gap-1.5">
                {semanas.map((s, w) => (
                  <div key={w} className="grid grid-rows-7 gap-1.5">
                    {s.map((d) => (
                      <div
                        key={d.k}
                        title={`${dataCurta(d.k)}: ${d.n} treino(s)`}
                        className={clsx('aspect-square rounded-[4px]', d.futuro ? 'bg-transparent' : d.n === 0 ? 'bg-raised' : d.n === 1 ? 'bg-accent/70' : 'bg-accent')}
                      />
                    ))}
                  </div>
                ))}
              </div>
            </div>
            <p className="mt-3 text-xs text-muted">Cada quadrado é um dia das últimas 12 semanas.</p>
          </>
        )}
      </Card>

      <SectionTitle>Evolução de carga</SectionTitle>
      {hist.isLoading ? (
        <Skeleton className="h-40" />
      ) : !exercicios.length ? (
        <Card className="p-5 text-sm text-muted">Registre o mesmo exercício em dois treinos para ver a evolução da carga.</Card>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2">
          {exercicios.slice(0, 8).map((e) => (
            <Card key={e.nome} className="p-4">
              <div className="flex items-start justify-between gap-3">
                <p className="min-w-0 font-semibold leading-snug">{e.nome}</p>
                <span className={clsx('num flex shrink-0 items-center gap-1 text-sm font-semibold', e.delta > 0 ? 'text-ok' : e.delta < 0 ? 'text-bad' : 'text-muted')}>
                  {e.delta > 0 ? <TrendingUp className="h-4 w-4" /> : e.delta < 0 ? <TrendingDown className="h-4 w-4" /> : null}
                  {e.delta > 0 ? '+' : ''}
                  {n1(e.delta)} kg
                </span>
              </div>
              <Sparkline points={e.v.map((x) => x.carga)} height={48} className="mt-2" />
              <p className="num mt-1 text-xs text-muted">
                {n1(e.v[0].carga)} → {n1(e.v[e.v.length - 1].carga)} kg em {e.v.length} treinos
              </p>
            </Card>
          ))}
        </div>
      )}
    </Page>
  );
}
