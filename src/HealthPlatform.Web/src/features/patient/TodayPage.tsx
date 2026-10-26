import clsx from 'clsx';
import { motion } from 'framer-motion';
import { ChevronRight, Dumbbell, Flame, Moon, Play, Sparkles, Zap } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Page } from '@/components/shell';
import { Button, Card, ErrorBox, Pill, Ring, SectionTitle, Skeleton } from '@/components/ui';
import { dataLonga, hora, n0, primeiroNome, saudacao, sessaoEhDoDia } from '@/lib/format';
import { useSessao } from '@/lib/session';
import { Avatar } from '@/components/account';
import type { RefeicaoPortal, StatusRefeicao } from '@/lib/types';
import { CheckInSheet } from './CheckInSheet';
import { MealSheet, STATUS_INFO, StatusButtons, somaNutrientes } from './meals';
import { useHome, useMeusTreinos, usePlanoAlimentar } from './queries';
import { treinoEmAndamento } from './workout-store';

const fade = (i: number) => ({ initial: { opacity: 0, y: 14 }, animate: { opacity: 1, y: 0 }, transition: { delay: 0.04 * i, duration: 0.45, ease: [0.22, 1, 0.36, 1] } });

function corProntidao(score: number) {
  if (score >= 75) return 'rgb(var(--ok))';
  if (score >= 55) return 'rgb(var(--warn))';
  return 'rgb(var(--bad))';
}

function rotuloProntidao(score: number) {
  if (score >= 80) return 'Pronto para treinar forte';
  if (score >= 65) return 'Bom para o treino do plano';
  if (score >= 50) return 'Vá com calma hoje';
  return 'Priorize recuperação';
}

export function TodayPage() {
  const sessao = useSessao();
  const nav = useNavigate();
  const home = useHome();
  const treinos = useMeusTreinos();
  const plano = usePlanoAlimentar();
  const [checkin, setCheckin] = useState(false);
  const [refeicaoAberta, setRefeicaoAberta] = useState<RefeicaoPortal | null>(null);

  const plano0 = treinos.data?.planos[0];
  const sessaoHoje = useMemo(() => {
    if (!plano0) return null;
    const doDia = plano0.sessoes.filter((s) => sessaoEhDoDia(s.diasSemana));
    return doDia[0] ?? null;
  }, [plano0]);
  const andamento = treinoEmAndamento();

  const statusPorRefeicao = new Map<string, StatusRefeicao>((home.data?.adesaoNutricional.refeicoes ?? []).map((r) => [r.refeicaoId, r.status]));
  const refeicoes = plano.data?.plano?.refeicoes ?? [];
  const feitas = refeicoes.filter((r) => statusPorRefeicao.get(r.id) && statusPorRefeicao.get(r.id) !== 'Pendente').length;
  const proxima = refeicoes.find((r) => (statusPorRefeicao.get(r.id) ?? 'Pendente') === 'Pendente');

  const pront = home.data?.prontidaoDiaria;
  const score = pront?.score ?? pront?.prontidaoScore ?? null;
  const g = home.data?.gamificacao;
  const coach = home.data?.coachDiario.prioridades.find((p) => p.codigo !== 'checkin-prontidao' && p.codigo !== 'proxima-acao');

  const kcalDia = refeicoes.reduce((a, r) => a + somaNutrientes(r.itens).calorias, 0);
  const kcalFeito = refeicoes.filter((r) => ['Realizada', 'Adaptada'].includes(statusPorRefeicao.get(r.id) ?? '')).reduce((a, r) => a + somaNutrientes(r.itens).calorias, 0);

  const blocoProntidao = (
    <motion.div {...fade(0)}>
      {home.isLoading ? (
        <Skeleton className="h-36" />
      ) : score == null ? (
        <button onClick={() => setCheckin(true)} className="aurora relative flex w-full items-center gap-4 overflow-hidden rounded-xl3 border border-accent/25 p-5 text-left shadow-glow transition active:scale-[0.99]">
          <div className="grid h-16 w-16 shrink-0 place-items-center rounded-full border-2 border-dashed border-accent/60 text-accent">
            <Sparkles className="h-7 w-7" />
          </div>
          <div className="min-w-0 flex-1">
            <p className="font-display text-lg font-semibold leading-snug">Como você acordou hoje?</p>
            <p className="mt-0.5 text-sm text-muted">Check-in de 30 segundos para liberar a estratégia do dia.</p>
          </div>
          <ChevronRight className="h-5 w-5 shrink-0 text-accent" />
        </button>
      ) : (
        <button onClick={() => setCheckin(true)} className="aurora flex w-full items-center gap-5 rounded-xl3 border border-line p-5 text-left transition active:scale-[0.99]">
          <Ring value={score} size={104} stroke={9} color={corProntidao(score)}>
            <div>
              <p className="num text-3xl font-semibold leading-none">{score}</p>
              <p className="mt-1 text-[10px] font-semibold uppercase tracking-widest text-muted">prontidão</p>
            </div>
          </Ring>
          <div className="min-w-0 flex-1">
            <p className="font-display text-[17px] font-semibold leading-snug">{rotuloProntidao(score)}</p>
            <div className="mt-2 flex flex-wrap gap-1.5">
              {pront?.sonoHoras != null && (
                <Pill>
                  <Moon className="h-3 w-3" /> {n0(pront.sonoHoras)}h sono
                </Pill>
              )}
              {pront?.energiaNivel != null && (
                <Pill>
                  <Zap className="h-3 w-3" /> energia {pront.energiaNivel}
                </Pill>
              )}
              {pront?.dorNivel != null && pront.dorNivel > 0 && <Pill tone={pront.dorNivel >= 6 ? 'bad' : 'warn'}>dor {pront.dorNivel}</Pill>}
            </div>
          </div>
        </button>
      )}
    </motion.div>
  );

  const blocoTreino = (
    <>
      <SectionTitle action={<Link to="/treino" className="text-sm font-semibold text-accent">Ver plano</Link>}>Treino de hoje</SectionTitle>
      <motion.div {...fade(2)}>
        {treinos.isLoading ? (
          <Skeleton className="h-40" />
        ) : !plano0 ? (
          <Card className="p-5 text-muted">Seu profissional ainda não liberou um plano de treino.</Card>
        ) : andamento ? (
          <Card onClick={() => nav(`/treino/${andamento.sessaoId}`)} className="border-accent/50 p-5">
            <Pill tone="accent">Em andamento</Pill>
            <p className="mt-2 font-display text-xl font-semibold">{andamento.sessaoNome}</p>
            <p className="mt-1 text-sm text-muted">Você parou no meio. Toque para continuar de onde estava.</p>
          </Card>
        ) : sessaoHoje ? (
          <div className="relative overflow-hidden rounded-xl3 border border-line bg-surface">
            <div className="absolute -right-10 -top-10 h-40 w-40 rounded-full bg-fat/10 blur-2xl" />
            <div className="relative p-5">
              <div className="flex items-center gap-2">
                <Pill tone="info">
                  <Dumbbell className="h-3 w-3" /> {sessaoHoje.itens.length} exercícios
                </Pill>
                <Pill>~{Math.round(sessaoHoje.itens.reduce((a, i) => a + i.series * ((i.descansoSegundos ?? 60) + 45), 0) / 60)} min</Pill>
              </div>
              <p className="mt-3 font-display text-[22px] font-semibold leading-tight">{sessaoHoje.nome}</p>
              <ul className="mt-3 space-y-1.5">
                {sessaoHoje.itens.slice(0, 4).map((i) => (
                  <li key={i.id} className="flex items-center justify-between gap-3 text-sm">
                    <span className="truncate">{i.exercicio}</span>
                    <span className="num shrink-0 text-muted">
                      {i.series}×{i.repeticoes}
                    </span>
                  </li>
                ))}
                {sessaoHoje.itens.length > 4 && <li className="text-sm text-faint">+ {sessaoHoje.itens.length - 4} exercícios</li>}
              </ul>
              <Button size="lg" className="mt-4 w-full" icon={<Play className="h-5 w-5 fill-current" />} onClick={() => nav(`/treino/${sessaoHoje.id}`)}>
                Começar treino
              </Button>
            </div>
          </div>
        ) : (
          <Card className="p-5">
            <p className="font-display text-lg font-semibold">Dia de descanso</p>
            <p className="mt-1 text-sm text-muted">Nenhuma sessão prevista para hoje. Recuperar também faz parte do plano.</p>
            <Link to="/treino" className="mt-3 inline-flex text-sm font-semibold text-accent">
              Treinar mesmo assim
            </Link>
          </Card>
        )}
      </motion.div>
    </>
  );

  const blocoRefeicoes = (
    <>
      <SectionTitle action={<Link to="/dieta" className="text-sm font-semibold text-accent">Ver dieta</Link>}>Refeições de hoje</SectionTitle>
      <motion.div {...fade(3)}>
        {plano.isLoading ? (
          <Skeleton className="h-64" />
        ) : !refeicoes.length ? (
          <Card className="p-5 text-muted">Seu plano alimentar ainda não foi liberado.</Card>
        ) : (
          <div className="overflow-hidden rounded-xl3 border border-line bg-surface">
            {/* Resumo do dia */}
            <div className="flex items-center gap-4 border-b border-line/70 p-4">
              <Ring value={feitas} max={Math.max(1, refeicoes.length)} size={56} stroke={6} color="rgb(var(--ok))">
                <span className="num text-sm font-semibold">
                  {feitas}/{refeicoes.length}
                </span>
              </Ring>
              <div className="min-w-0">
                <p className="font-semibold">{feitas === refeicoes.length ? 'Dia completo, mandou bem!' : `${refeicoes.length - feitas} refeições pela frente`}</p>
                <p className="num text-sm text-muted">
                  {n0(kcalFeito)} de {n0(kcalDia)} kcal
                </p>
              </div>
            </div>

            {/* Proxima refeicao em destaque */}
            {proxima && (
              <div className="border-b border-line/70 bg-accent/[0.05] p-4">
                <p className="eyebrow text-accent">Próxima · {hora(proxima.horario) || 'sem horário'}</p>
                <button onClick={() => setRefeicaoAberta(proxima)} className="mt-1 block w-full text-left">
                  <p className="font-display text-lg font-semibold">{proxima.nome}</p>
                  <p className="mt-0.5 line-clamp-1 text-sm text-muted">{proxima.itens.map((i) => i.alimento).join(' · ')}</p>
                </button>
                <div className="mt-3">
                  <StatusButtons refeicaoId={proxima.id} status="Pendente" />
                </div>
              </div>
            )}

            <ol>
              {refeicoes
                .filter((r) => r.id !== proxima?.id)
                .map((r) => {
                  const st = statusPorRefeicao.get(r.id) ?? 'Pendente';
                  const kcal = somaNutrientes(r.itens).calorias;
                  return (
                    <li key={r.id}>
                      <button onClick={() => setRefeicaoAberta(r)} className="flex w-full items-center gap-3 px-4 py-3 text-left transition hover:bg-raised/60">
                        <span
                          className={clsx(
                            'num grid h-10 w-14 shrink-0 place-items-center rounded-xl text-[12px] font-semibold',
                            st === 'Pendente' && 'border border-line text-muted',
                            st === 'Realizada' && 'bg-ok/15 text-ok',
                            st === 'Adaptada' && 'bg-warn/15 text-warn',
                            st === 'NaoRealizada' && 'bg-bad/15 text-bad',
                          )}
                        >
                          {hora(r.horario) || '—'}
                        </span>
                        <span className="min-w-0 flex-1">
                          <span className={clsx('block truncate font-semibold', st === 'NaoRealizada' && 'text-muted line-through')}>{r.nome}</span>
                          <span className="block text-[13px] text-muted">{n0(kcal)} kcal</span>
                        </span>
                        {st !== 'Pendente' ? (
                          <Pill tone={STATUS_INFO[st].tone === 'neutral' ? 'neutral' : STATUS_INFO[st].tone}>{STATUS_INFO[st].label}</Pill>
                        ) : (
                          <ChevronRight className="h-4 w-4 text-faint" />
                        )}
                      </button>
                    </li>
                  );
                })}
            </ol>
          </div>
        )}
      </motion.div>
    </>
  );

  const blocoConsistencia = (
    <>
      {g && (
        <motion.div {...fade(4)} className="mt-7 grid grid-cols-2 gap-3">
          <Card className="p-4">
            <Flame className="h-5 w-5 text-warn" />
            <p className="num mt-2 text-3xl font-semibold">{g.streakDias}</p>
            <p className="text-sm text-muted">dias seguidos</p>
          </Card>
          <Card className="p-4">
            <div className="flex items-center justify-between">
              <span className="eyebrow">Nível {g.nivel}</span>
              <span className="num text-xs text-faint">{g.xpTotal} XP</span>
            </div>
            <div className="mt-4 h-2 overflow-hidden rounded-full bg-line/60">
              <motion.div className="h-full rounded-full bg-accent" initial={{ width: 0 }} animate={{ width: `${Math.min(100, (g.xpNoNivel / Math.max(1, g.xpProximoNivel)) * 100)}%` }} transition={{ duration: 1 }} />
            </div>
            <p className="mt-2 text-sm text-muted">
              <span className="num">{Math.max(0, g.xpProximoNivel - g.xpNoNivel)}</span> XP para o próximo
            </p>
          </Card>
        </motion.div>
      )}
      {coach && (
        <motion.div {...fade(5)} className="mt-3 rounded-xl2 border border-line bg-surface p-4">
          <p className="eyebrow">Recado do dia</p>
          <p className="mt-1.5 font-semibold">{coach.titulo}</p>
          <p className="mt-1 text-sm text-muted">{coach.acao}</p>
        </motion.div>
      )}
    </>
  );

  return (
    <Page wide>
      <header className="flex items-center justify-between pb-2 pt-5">
        <div>
          <p className="text-sm text-muted">{dataLonga()}</p>
          <h1 className="font-display text-[28px] font-semibold leading-tight tracking-tight lg:text-[34px]">
            {saudacao()}, {primeiroNome(home.data?.paciente.nome ?? sessao?.nome)}
          </h1>
        </div>
        <Link to="/perfil" aria-label="Perfil" className="lg:hidden">
          <Avatar nome={sessao?.nome} tipo={sessao?.tipoUsuario} />
        </Link>
      </header>

      {home.isError && <ErrorBox error={home.error} onRetry={() => home.refetch()} />}

      {/* Celular: uma coluna na ordem do dia. PC: dia e treino a esquerda, refeicoes a direita. */}
      <div className="mt-4 lg:grid lg:grid-cols-[1.35fr_1fr] lg:items-start lg:gap-8">
        <div className="min-w-0">
          {blocoProntidao}
          {blocoTreino}
          <div className="hidden lg:block">{blocoConsistencia}</div>
        </div>
        <div className="min-w-0 lg:sticky lg:top-6">
          {blocoRefeicoes}
          <div className="lg:hidden">{blocoConsistencia}</div>
        </div>
      </div>

      <CheckInSheet open={checkin} onClose={() => setCheckin(false)} />
      <MealSheet refeicao={refeicaoAberta} status={refeicaoAberta ? (statusPorRefeicao.get(refeicaoAberta.id) ?? 'Pendente') : 'Pendente'} onClose={() => setRefeicaoAberta(null)} />
    </Page>
  );
}
