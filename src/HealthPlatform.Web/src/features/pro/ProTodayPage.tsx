import clsx from 'clsx';
import { motion } from 'framer-motion';
import { CalendarClock, ChevronRight, Dumbbell, Moon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Page } from '@/components/shell';
import { Card, Empty, Pill, SectionTitle, Skeleton } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { dataLonga, iniciais, n0, primeiroNome, saudacao, tempoRelativo } from '@/lib/format';
import { useSessao } from '@/lib/session';
import type { SinalDiario } from '@/lib/types';
import { useDashboard, useSinais } from './queries';

const NIVEL: Record<string, { label: string; tone: 'bad' | 'warn' | 'neutral' | 'ok'; ordem: number }> = {
  RevisarHoje: { label: 'Revisar hoje', tone: 'bad', ordem: 0 },
  Observar: { label: 'Observar', tone: 'warn', ordem: 1 },
  ContextoPendente: { label: 'Sem check-in', tone: 'neutral', ordem: 2 },
  Estavel: { label: 'Estável', tone: 'ok', ordem: 3 },
};

const corScore = (s: number | null) => (s == null ? 'text-faint' : s >= 75 ? 'text-ok' : s >= 55 ? 'text-warn' : 'text-bad');

function LinhaSinal({ s }: { s: SinalDiario }) {
  const n = NIVEL[s.nivel] ?? { label: s.nivel, tone: 'neutral' as const, ordem: 9 };
  return (
    <Link to={`/pro/pacientes/${s.pacienteId}`} className="flex items-center gap-3 rounded-2xl px-2 py-3 transition hover:bg-raised/60">
      <div className="relative grid h-11 w-11 shrink-0 place-items-center rounded-full bg-raised font-display text-sm font-semibold">
        {iniciais(s.pacienteNome)}
        {n.tone !== 'ok' && n.tone !== 'neutral' && <span className={clsx('absolute -right-0.5 -top-0.5 h-3 w-3 rounded-full border-2 border-surface', n.tone === 'bad' ? 'bg-bad' : 'bg-warn')} />}
      </div>
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <p className="truncate font-semibold">{s.pacienteNome}</p>
          <Pill tone={n.tone}>{n.label}</Pill>
        </div>
        <p className="mt-0.5 truncate text-[13px] text-muted">
          {s.razoes[0] ?? `${s.treinosUltimos7Dias} treinos na semana · último ${tempoRelativo(s.ultimoTreinoUtc)}`}
        </p>
      </div>
      <div className="w-12 shrink-0 text-right">
        <p className={clsx('num text-xl font-semibold leading-none', corScore(s.prontidaoScore))}>{s.prontidaoScore ?? '—'}</p>
        <p className="mt-1 text-[10px] uppercase tracking-wider text-faint">prontidão</p>
      </div>
    </Link>
  );
}

export function ProTodayPage() {
  const sessao = useSessao();
  const dash = useDashboard();
  const sinais = useSinais();
  const semPerfil = dash.error instanceof ApiError && dash.error.status === 409;
  const lista = [...(sinais.data?.sinais ?? [])].sort((a, b) => (NIVEL[a.nivel]?.ordem ?? 9) - (NIVEL[b.nivel]?.ordem ?? 9));
  const comCheckin = (sinais.data?.sinais ?? []).filter((s) => s.prontidaoScore != null);

  return (
    <Page wide>
      <header className="pb-2 pt-6">
        <p className="text-sm capitalize text-muted">{dataLonga()}</p>
        <h1 className="font-display text-[28px] font-semibold leading-tight tracking-tight">
          {saudacao()}, {primeiroNome(dash.data?.profissionalNome ?? sessao?.nome)}
        </h1>
      </header>

      {semPerfil && (
        <div className="mt-3 rounded-xl2 border border-warn/40 bg-warn/10 p-4 text-sm">
          Complete seu perfil profissional na <a href="/" className="font-semibold underline">versão clássica</a> para ver a agenda.
        </div>
      )}

      <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="mt-4 grid grid-cols-2 gap-3 md:grid-cols-4">
        {[
          { v: sinais.data?.totalPacientesAtivos ?? dash.data?.pacientesAtivos, l: 'pacientes ativos' },
          { v: dash.data?.consultasHoje, l: 'consultas hoje' },
          { v: sinais.data ? sinais.data.revisarHoje + sinais.data.observar : undefined, l: 'pedem atenção', tone: 'text-warn' },
          { v: sinais.data ? sinais.data.totalPacientesAtivos - sinais.data.contextoPendente : undefined, l: 'com check-in na semana', tone: 'text-accent' },
        ].map((x) => (
          <Card key={x.l} className="p-4">
            {x.v === undefined ? <Skeleton className="h-8 w-12" /> : <p className={clsx('num text-[30px] font-semibold leading-none', x.tone)}>{n0(x.v)}</p>}
            <p className="mt-2 text-[13px] text-muted">{x.l}</p>
          </Card>
        ))}
      </motion.div>

      <div className="grid gap-x-6 lg:grid-cols-[1.4fr_1fr]">
        <section>
          <SectionTitle action={<Link to="/pro/pacientes" className="text-sm font-semibold text-accent">Todos</Link>}>Quem pede atenção</SectionTitle>
          <Card className="p-2">
            {sinais.isLoading ? (
              <div className="flex flex-col gap-2 p-2">
                {[0, 1, 2, 3].map((i) => (
                  <Skeleton key={i} className="h-14" />
                ))}
              </div>
            ) : lista.length ? (
              <>
                <div className="divide-y divide-line/60">{lista.map((s) => <LinhaSinal key={s.pacienteId} s={s} />)}</div>
                {sinais.data && sinais.data.totalPacientesAtivos > lista.length && (
                  <p className="px-2 pb-2 pt-3 text-[13px] text-muted">
                    Os outros {sinais.data.totalPacientesAtivos - lista.length} pacientes estão sem alertas nos últimos {7} dias.
                  </p>
                )}
              </>
            ) : (
              <p className="p-4 text-sm text-muted">Nenhum sinal novo. Todos os pacientes estão com check-in e treino em dia.</p>
            )}
          </Card>
        </section>

        <section>
          <SectionTitle>Agenda</SectionTitle>
          {dash.isLoading ? (
            <Skeleton className="h-40" />
          ) : dash.data?.agendaHoje.length ? (
            <Card className="divide-y divide-line/60">
              {dash.data.agendaHoje.map((c) => (
                <Link key={c.id} to={`/pro/pacientes/${c.pacienteId}`} className="flex items-center gap-3 p-4">
                  <p className="num w-12 font-semibold">{new Date(c.dataHoraUtc).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</p>
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-semibold">{c.pacienteNome}</p>
                    <p className="truncate text-[13px] text-muted">{c.motivo ?? 'Consulta'}</p>
                  </div>
                  <Pill tone={c.status === 'Confirmada' ? 'ok' : c.status === 'Cancelada' ? 'bad' : 'neutral'}>{c.status}</Pill>
                </Link>
              ))}
            </Card>
          ) : (
            <Empty icon={<CalendarClock className="h-5 w-5" />} title="Agenda livre hoje">
              {dash.data?.proximasConsultas[0] ? `Próxima: ${dash.data.proximasConsultas[0].pacienteNome}, ${new Date(dash.data.proximasConsultas[0].dataHoraUtc).toLocaleString('pt-BR', { weekday: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit' })}` : 'Nenhuma consulta marcada.'}
            </Empty>
          )}

          {comCheckin.length > 0 && (
            <>
              <SectionTitle>Destaques da semana</SectionTitle>
              <Card className="flex flex-col gap-3 p-4 text-sm">
                {(() => {
                  const maisTreinos = [...comCheckin].sort((a, b) => b.treinosUltimos7Dias - a.treinosUltimos7Dias)[0];
                  const menosSono = [...comCheckin].filter((s) => s.sonoHoras != null).sort((a, b) => (a.sonoHoras ?? 0) - (b.sonoHoras ?? 0))[0];
                  return (
                    <>
                      {maisTreinos && (
                        <Link to={`/pro/pacientes/${maisTreinos.pacienteId}`} className="flex items-center gap-3">
                          <Dumbbell className="h-5 w-5 text-accent" />
                          <span className="flex-1">
                            <b>{primeiroNome(maisTreinos.pacienteNome)}</b> fez {maisTreinos.treinosUltimos7Dias} treinos em 7 dias
                          </span>
                          <ChevronRight className="h-4 w-4 text-faint" />
                        </Link>
                      )}
                      {menosSono && (menosSono.sonoHoras ?? 9) < 7 && (
                        <Link to={`/pro/pacientes/${menosSono.pacienteId}`} className="flex items-center gap-3">
                          <Moon className="h-5 w-5 text-info" />
                          <span className="flex-1">
                            <b>{primeiroNome(menosSono.pacienteNome)}</b> dormiu {n0(menosSono.sonoHoras)}h no último check-in
                          </span>
                          <ChevronRight className="h-4 w-4 text-faint" />
                        </Link>
                      )}
                    </>
                  );
                })()}
              </Card>
            </>
          )}
        </section>
      </div>
    </Page>
  );
}
