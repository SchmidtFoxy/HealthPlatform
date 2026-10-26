import clsx from 'clsx';
import { motion } from 'framer-motion';
import { AlertTriangle, Bell, CalendarClock, ChevronRight, ClipboardCheck, Dumbbell, FileCheck2, HeartPulse, Moon, PhoneCall, Users } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Page } from '@/components/shell';
import { Card, Empty, Pill, SectionTitle, Skeleton } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { dataLonga, iniciais, n0, primeiroNome, saudacao, tempoRelativo } from '@/lib/format';
import { useSessao } from '@/lib/session';
import type { SinalDiario } from '@/lib/types';
import { useCentralDia } from './CentralPage';
import { useAvisosNaoLidos, useDashboard, useSinais } from './queries';

const NIVEL: Record<string, { label: string; tone: 'bad' | 'warn' | 'neutral' | 'ok'; ordem: number }> = {
  RevisarHoje: { label: 'Revisar hoje', tone: 'bad', ordem: 0 },
  Observar: { label: 'Observar', tone: 'warn', ordem: 1 },
  ContextoPendente: { label: 'Sem check-in', tone: 'neutral', ordem: 2 },
  Estavel: { label: 'Estável', tone: 'ok', ordem: 3 },
};

const corScore = (s: number | null) => (s == null ? 'text-faint' : s >= 75 ? 'text-ok' : s >= 55 ? 'text-warn' : 'text-bad');

function LinhaSinal({ s }: { s: SinalDiario }) {
  const n = NIVEL[s.nivel] ?? { label: s.nivel, tone: 'neutral' as const, ordem: 9 };
  const anel = n.tone === 'bad' ? 'ring-bad' : n.tone === 'warn' ? 'ring-warn' : n.tone === 'ok' ? 'ring-ok' : 'ring-line';
  return (
    <Link to={`/pro/pacientes/${s.pacienteId}`} className="group flex items-center gap-3 rounded-2xl px-2 py-3 transition hover:bg-raised/60">
      <div className={clsx('grid h-11 w-11 shrink-0 place-items-center rounded-full bg-raised font-display text-sm font-semibold ring-2 ring-offset-2 ring-offset-surface', anel)}>{iniciais(s.pacienteNome)}</div>
      <div className="min-w-0 flex-1">
        <div className="flex min-w-0 items-center gap-2">
          <p className="truncate font-semibold">{s.pacienteNome}</p>
          <Pill tone={n.tone}>{n.label}</Pill>
        </div>
        <p className="mt-0.5 truncate text-[13px] text-muted">
          {s.razoes[0] ?? `${s.treinosUltimos7Dias} treinos na semana · último ${tempoRelativo(s.ultimoTreinoUtc)}`}
        </p>
      </div>
      {s.prontidaoScore != null ? (
        <div className={clsx('num grid h-11 w-11 shrink-0 place-items-center rounded-full bg-raised text-[15px] font-semibold', corScore(s.prontidaoScore))} title="Prontidão">
          {s.prontidaoScore}
        </div>
      ) : (
        <ChevronRight className="h-5 w-5 shrink-0 text-faint transition group-hover:translate-x-0.5" />
      )}
    </Link>
  );
}

export function ProTodayPage() {
  const sessao = useSessao();
  const dash = useDashboard();
  const avisos = useAvisosNaoLidos();
  const sinais = useSinais();
  const central = useCentralDia();
  const c = central.data;
  const semPerfil = dash.error instanceof ApiError && dash.error.status === 409;
  const lista = [...(sinais.data?.sinais ?? [])].sort((a, b) => (NIVEL[a.nivel]?.ordem ?? 9) - (NIVEL[b.nivel]?.ordem ?? 9));
  const comCheckin = (sinais.data?.sinais ?? []).filter((s) => s.prontidaoScore != null);
  const atencao = sinais.data ? sinais.data.revisarHoje + sinais.data.observar : undefined;
  const semCheckin = sinais.data?.contextoPendente ?? 0;

  return (
    <Page wide>
      <header className="flex items-start justify-between gap-3 pb-2 pt-6">
        <div className="min-w-0">
          <p className="text-sm text-muted">{dataLonga()}</p>
          <h1 className="font-display text-[28px] font-semibold leading-tight tracking-tight">
            {saudacao()}, {primeiroNome(dash.data?.profissionalNome ?? sessao?.nome)}
          </h1>
        </div>
        <Link to="/pro/avisos" aria-label={avisos.data ? `Avisos, ${avisos.data} não lidos` : 'Avisos'} className="relative mt-1 grid h-11 w-11 shrink-0 place-items-center rounded-full border border-line bg-surface transition hover:border-accent/40">
          <Bell className="h-5 w-5" />
          {!!avisos.data && <span className="num absolute -right-1 -top-1 grid h-5 min-w-5 place-items-center rounded-full bg-accent px-1 text-[11px] font-bold text-accent-ink">{avisos.data > 99 ? '99+' : avisos.data}</span>}
        </Link>
      </header>

      {sinais.data && (
        <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} className="aurora mt-3 flex flex-col gap-3 rounded-xl3 border border-line p-5 sm:flex-row sm:items-center">
          <div className="min-w-0 flex-1">
            <p className="eyebrow">Resumo do dia</p>
            <p className="mt-1.5 font-display text-[19px] font-semibold leading-snug">
              {atencao
                ? `${atencao} ${atencao > 1 ? 'pacientes pedem' : 'paciente pede'} atenção hoje.`
                : semCheckin
                  ? `Nenhum alerta. ${semCheckin} ${semCheckin > 1 ? 'pacientes estão' : 'paciente está'} sem check-in na semana.`
                  : 'Tudo em dia: todos os pacientes fizeram check-in na semana.'}
            </p>
          </div>
          <Link to="/pro/pacientes" className="inline-flex h-11 shrink-0 items-center justify-center gap-1.5 rounded-full bg-accent px-5 text-sm font-semibold text-accent-ink shadow-glow transition hover:brightness-110">
            Ver pacientes <ChevronRight className="h-4 w-4" />
          </Link>
        </motion.div>
      )}

      {semPerfil && (
        <div className="mt-3 rounded-xl2 border border-warn/40 bg-warn/10 p-4 text-sm">
          Complete seu perfil profissional em <Link to="/pro/conta" className="font-semibold underline">Minha conta</Link> para ver a agenda.
        </div>
      )}

      <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="mt-4 grid grid-cols-2 gap-3 md:grid-cols-4">
        {[
          { icon: Users, c: 'text-accent', v: sinais.data?.totalPacientesAtivos ?? dash.data?.pacientesAtivos, l: 'pacientes ativos' },
          { icon: CalendarClock, c: 'text-info', v: dash.data?.consultasHoje, l: 'consultas hoje' },
          { icon: AlertTriangle, c: 'text-warn', v: atencao, l: 'pedem atenção', tone: atencao ? 'text-warn' : undefined },
          { icon: HeartPulse, c: 'text-ok', v: sinais.data ? sinais.data.totalPacientesAtivos - sinais.data.contextoPendente : undefined, l: 'com check-in na semana' },
        ].map((x) => (
          <Card key={x.l} className="p-4">
            <x.icon className={clsx('h-4 w-4', x.c)} />
            {x.v === undefined ? <Skeleton className="mt-2.5 h-8 w-12" /> : <p className={clsx('num mt-2.5 text-[30px] font-semibold leading-none', x.tone)}>{n0(x.v)}</p>}
            <p className="mt-1.5 text-[13px] text-muted">{x.l}</p>
          </Card>
        ))}
      </motion.div>

      <SectionTitle action={<Link to="/pro/central" className="text-sm font-semibold text-accent">Abrir central</Link>}>Para resolver</SectionTitle>
      <div className="grid grid-cols-3 gap-2 sm:gap-3">
        {[
          { aba: 'pendencias', icon: ClipboardCheck, v: c?.pendenciasPrioritarias, l: c?.pendenciasPrioritarias === 1 ? 'pendência' : 'pendências', d: c?.pendenciasPrioritarias === 1 ? 'prioritária' : 'prioritárias' },
          { aba: 'contatos', icon: PhoneCall, v: c ? c.followUpsVencidos + c.followUpsHoje : undefined, l: c && c.followUpsVencidos + c.followUpsHoje === 1 ? 'contato' : 'contatos', d: c?.followUpsVencidos ? `${c.followUpsVencidos} atrasado${c.followUpsVencidos > 1 ? 's' : ''}` : 'para hoje' },
          { aba: 'pedidos', icon: FileCheck2, v: c?.solicitacoesParaRevisao, l: c?.solicitacoesParaRevisao === 1 ? 'resposta' : 'respostas', d: 'para revisar' },
        ].map((x) => (
          <Link key={x.aba} to={`/pro/central?aba=${x.aba}`} className="rounded-xl2 border border-line bg-surface p-3.5 transition hover:border-accent/40 sm:p-4">
            <x.icon className={clsx('h-4 w-4', x.v ? 'text-warn' : 'text-faint')} />
            {x.v === undefined ? <Skeleton className="mt-2 h-7 w-10" /> : <p className={clsx('num mt-2 text-[26px] font-semibold leading-none', !x.v && 'text-faint')}>{n0(x.v)}</p>}
            <p className="mt-1 truncate text-[13px] font-semibold">{x.l}</p>
            <p className="truncate text-[12px] text-muted">{x.d}</p>
          </Link>
        ))}
      </div>

      <div className="grid grid-cols-1 gap-x-6 lg:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
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
          <SectionTitle action={<Link to="/pro/agenda" className="text-sm font-semibold text-accent">Ver agenda</Link>}>Agenda</SectionTitle>
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
