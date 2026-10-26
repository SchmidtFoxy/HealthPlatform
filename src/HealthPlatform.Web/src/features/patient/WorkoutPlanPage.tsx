import clsx from 'clsx';
import { motion } from 'framer-motion';
import { Check, ChevronDown, ChevronLeft, ChevronRight, Clock, Coffee, Dumbbell, Flame, Play, Plus, Timer } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Page, PageHeader } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, Pill, SectionTitle, Skeleton } from '@/components/ui';
import { dataCurta, n0, n1, sessaoEhDoDia } from '@/lib/format';
import type { ExecucaoHistorico, SessaoTreinoPortal as SessaoTreino } from '@/lib/types';
import { useHistoricoTreinos, useMeusTreinos } from './queries';
import { mesmoDia, treinoDoDia } from './treino-dia';

const DIAS = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

const letra = (nome: string) => nome.trim()[0]?.toUpperCase() ?? '?';
const minutos = (s: SessaoTreino) => Math.round(s.itens.reduce((a, i) => a + i.series * ((i.descansoSegundos ?? 60) + 45), 0) / 60);

export function WorkoutPlanPage() {
  const nav = useNavigate();
  const q = useMeusTreinos();
  const hist = useHistoricoTreinos(90);
  const plano = q.data?.planos[0];
  const hoje = new Date();
  const dia = treinoDoDia(plano, hist.data?.execucoes);
  const sessaoHoje = dia.hoje;
  // Depois de treinar, o destaque passa para o proximo treino.
  const destaque = sessaoHoje ?? (dia.feitosHoje.length ? dia.proxima?.sessao : undefined);
  const [aberta, setAberta] = useState<string | null>(null);
  const abertaId = aberta ?? destaque?.id ?? null;
  const [offset, setOffset] = useState(0);
  const [sel, setSel] = useState(() => hoje.toDateString());

  // Semana de domingo a sabado (offset em semanas a partir da atual). Plano com dias: a sessao prevista em cada dia.
  // Plano em rodizio: o que foi feito em cada dia e, hoje, o proximo do rodizio.
  const semana = DIAS.map((rotulo, i) => {
    const d = new Date(hoje);
    d.setDate(hoje.getDate() - hoje.getDay() + i + offset * 7);
    const execs = (hist.data?.execucoes ?? []).filter((e) => mesmoDia(new Date(e.dataHoraInicioUtc), d));
    const ehHoje = mesmoDia(d, hoje);
    const sessao = dia.rodizio
      ? (execs[0] && plano?.sessoes.find((s) => s.nome === execs[0].sessao)) || (ehHoje ? sessaoHoje : undefined) || undefined
      : plano?.sessoes.find((s) => !!s.diasSemana && sessaoEhDoDia(s.diasSemana, d));
    return { rotulo, d, sessao, execs, feito: execs.length > 0, ehHoje, futuro: !ehHoje && d > hoje };
  });
  const diaSel = semana.find((s) => s.d.toDateString() === sel);
  const tituloSemana =
    offset === 0 ? 'Esta semana' : offset === -1 ? 'Semana passada' : offset === 1 ? 'Próxima semana' : `${semana[0].d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })} a ${semana[6].d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })}`;
  const mudarSemana = (n: number) => {
    const novo = offset + n;
    setOffset(novo);
    const d = new Date(hoje);
    d.setDate(hoje.getDate() + novo * 7);
    setSel((novo === 0 ? hoje : d).toDateString());
  };
  const verSessao = (id: string) => {
    setAberta(id);
    setTimeout(() => document.getElementById(`sessao-${id}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 60);
  };

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
          <div className="rounded-xl3 border border-line bg-surface p-2 sm:p-3">
            <div className="mb-1.5 flex items-center justify-between px-1">
              <button aria-label="Semana anterior" disabled={offset <= -12} onClick={() => mudarSemana(-1)} className="grid h-8 w-8 place-items-center rounded-full text-muted transition hover:bg-raised disabled:opacity-30">
                <ChevronLeft className="h-4 w-4" />
              </button>
              <button className="text-[13px] font-semibold text-muted" onClick={() => offset !== 0 && mudarSemana(-offset)}>
                {tituloSemana}
                {offset !== 0 && <span className="ml-1.5 text-accent">· voltar a hoje</span>}
              </button>
              <button aria-label="Próxima semana" disabled={offset >= 4} onClick={() => mudarSemana(1)} className="grid h-8 w-8 place-items-center rounded-full text-muted transition hover:bg-raised disabled:opacity-30">
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
            <div className="grid grid-cols-7 gap-1.5 sm:gap-2">
              {semana.map((s) => {
                const ativo = s.d.toDateString() === sel;
                return (
                  <button
                    key={s.rotulo}
                    onClick={() => setSel(s.d.toDateString())}
                    aria-pressed={ativo}
                    aria-label={`${s.d.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric' })}${s.feito ? ', treino feito' : s.sessao ? `, ${s.sessao.nome}` : ''}`}
                    className={clsx(
                      'flex flex-col items-center gap-1 rounded-2xl py-2.5 outline-none transition active:scale-95',
                      s.ehHoje ? 'bg-accent text-accent-ink' : s.sessao || s.feito ? 'bg-raised/70' : '',
                      ativo && (s.ehHoje ? 'ring-2 ring-accent/40 ring-offset-2 ring-offset-surface' : 'ring-2 ring-accent'),
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
                      {s.feito ? <Check className="h-3.5 w-3.5" strokeWidth={3} /> : s.sessao ? letra(s.sessao.nome) : '·'}
                    </span>
                  </button>
                );
              })}
            </div>
          </div>

          {diaSel && <DiaDetalhe dia={diaSel} rodizio={dia.rodizio} onVer={verSessao} onComecar={(id) => nav(`/treino/${id}`)} />}

          {plano.observacoes && <p className="mt-3 rounded-xl2 border border-accent/30 bg-accent/10 px-4 py-3 text-sm">{plano.observacoes}</p>}

          <SectionTitle>Sessões do plano</SectionTitle>
          <div className="grid gap-3 lg:grid-cols-2 xl:grid-cols-3">
            {plano.sessoes.map((s, idx) => {
              const ehHoje = s.id === sessaoHoje?.id;
              const feitaHoje = dia.feitasHojeNomes.has(s.nome);
              const ehProxima = !sessaoHoje && s.id === destaque?.id;
              const open = abertaId === s.id;
              const grupos = [...new Set(s.itens.map((i) => i.grupoMuscular).filter(Boolean))];
              return (
                <motion.div key={s.id} id={`sessao-${s.id}`} className="scroll-mt-20" initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: idx * 0.05 }}>
                  <Card className={clsx('flex h-full flex-col overflow-hidden', (ehHoje || ehProxima) && 'border-accent/50 shadow-glow', feitaHoje && 'border-ok/40')}>
                    <button className="flex w-full items-center gap-4 p-4 text-left lg:cursor-default" onClick={() => setAberta(open ? '' : s.id)} aria-expanded={open}>
                      <div className={clsx('grid h-12 w-12 shrink-0 place-items-center rounded-2xl font-display text-lg font-semibold', feitaHoje ? 'bg-ok/20 text-ok' : ehHoje || ehProxima ? 'bg-accent text-accent-ink' : 'bg-raised text-muted')}>
                        {feitaHoje ? <Check className="h-6 w-6" strokeWidth={3} /> : letra(s.nome)}
                      </div>
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2">
                          <p className="truncate font-semibold">{s.nome}</p>
                          {feitaHoje ? <Pill tone="ok">Feito hoje</Pill> : ehHoje ? <Pill tone="accent">Hoje</Pill> : ehProxima ? <Pill tone="accent">Próximo · {dia.proxima?.quando}</Pill> : null}
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
                      <Button variant={ehHoje && !feitaHoje ? 'primary' : 'soft'} className="mt-3 w-full" icon={<Play className="h-4 w-4 fill-current" />} onClick={() => nav(`/treino/${s.id}`)}>
                        {feitaHoje ? 'Treinar de novo' : `Começar ${s.nome.split('—')[0].trim()}`}
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

// ---------------------------------------------------------------- Dia escolhido no calendario

interface DiaSemana {
  d: Date;
  sessao?: SessaoTreino;
  execs: ExecucaoHistorico[];
  ehHoje: boolean;
  futuro: boolean;
}

function DiaDetalhe({ dia, rodizio, onVer, onComecar }: { dia: DiaSemana; rodizio: boolean; onVer: (id: string) => void; onComecar: (id: string) => void }) {
  const nome = dia.ehHoje ? 'Hoje' : dia.d.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' });
  const titulo = nome[0].toUpperCase() + nome.slice(1);
  const passado = !dia.ehHoje && !dia.futuro;
  return (
    <motion.div key={dia.d.toDateString()} initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.25 }}>
      <Card className="mt-3 p-4">
        <p className="eyebrow">{titulo}</p>
        {dia.execs.map((e) => (
          <ExecucaoResumo key={e.id} e={e} />
        ))}
        {dia.sessao && !dia.execs.length ? (
          <div className="mt-2">
            <p className="font-semibold">
              {dia.futuro ? 'Previsto: ' : dia.ehHoje ? 'Treino do dia: ' : 'Estava previsto: '}
              {dia.sessao.nome}
            </p>
            <p className="text-[13px] text-muted">
              {dia.sessao.itens.length} exercícios · ~{minutos(dia.sessao)} min{passado ? ' · não registrado' : ''}
            </p>
            <div className="mt-3 flex gap-2">
              <Button variant="soft" size="sm" onClick={() => onVer(dia.sessao!.id)}>
                Ver exercícios
              </Button>
              {!dia.futuro && (
                <Button size="sm" icon={<Play className="h-3.5 w-3.5 fill-current" />} onClick={() => onComecar(dia.sessao!.id)}>
                  {dia.ehHoje ? 'Começar' : 'Fazer agora'}
                </Button>
              )}
            </div>
          </div>
        ) : !dia.execs.length ? (
          <div className="mt-2 flex items-start gap-3">
            <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-raised text-muted">
              <Coffee className="h-5 w-5" />
            </span>
            <div className="min-w-0">
              <p className="font-semibold">{dia.futuro ? (rodizio ? 'Seu plano segue em rodízio' : 'Dia sem treino previsto') : passado ? 'Nenhum treino registrado' : 'Dia livre no plano'}</p>
              <p className="text-[13px] text-muted">
                {dia.futuro
                  ? rodizio
                    ? 'O próximo treino é sempre o que vem depois do último que você fez.'
                    : 'Aproveite para descansar e recuperar.'
                  : 'Se bateu vontade, escolha uma sessão do plano abaixo e mande ver.'}
              </p>
            </div>
          </div>
        ) : null}
      </Card>
    </motion.div>
  );
}

function ExecucaoResumo({ e }: { e: ExecucaoHistorico }) {
  const [aberto, setAberto] = useState(false);
  const feitos = e.itens.filter((i) => i.concluido).length;
  const extras = e.extras ?? [];
  const linhas = [
    ...e.itens.map((i) => ({ k: i.itemTreinoId, nome: i.exercicio, s: i.seriesRealizadas, r: i.repeticoesRealizadas, c: i.cargaRealizada, u: i.unidadeCarga, ok: i.concluido, extra: false })),
    ...extras.map((x, idx) => ({ k: `x${idx}`, nome: x.exercicio, s: x.seriesRealizadas, r: x.repeticoesRealizadas, c: x.cargaRealizada, u: x.unidadeCarga, ok: true, extra: true })),
  ];
  return (
    <div className="mt-2 border-b border-line/60 pb-3 last:border-0 last:pb-0">
      <button className="flex w-full items-center gap-3 text-left" onClick={() => setAberto(!aberto)} aria-expanded={aberto}>
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-ok/15 text-ok">
          <Check className="h-5 w-5" strokeWidth={3} />
        </span>
        <span className="min-w-0 flex-1">
          <span className="block truncate font-semibold">{e.sessao}</span>
          <span className="block text-[13px] text-muted">
            {new Date(e.dataHoraInicioUtc).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })} · {e.duracaoMinutos ?? '—'} min · {feitos}/{e.itens.length} exercícios
            {extras.length > 0 && ` + ${extras.length} extra${extras.length > 1 ? 's' : ''}`}
          </span>
        </span>
        {e.esforcoPercebido != null && <Pill tone={e.esforcoPercebido >= 9 ? 'bad' : e.esforcoPercebido >= 7 ? 'warn' : 'ok'}>RPE {e.esforcoPercebido}</Pill>}
        <ChevronDown className={clsx('h-4 w-4 shrink-0 text-muted transition', aberto && 'rotate-180')} />
      </button>
      {aberto && (
        <ul className="mt-2 divide-y divide-line/50 pl-[52px]">
          {linhas.map((l) => (
            <li key={l.k} className="flex items-center gap-2 py-2 text-sm">
              <span className={clsx('min-w-0 flex-1 truncate', !l.ok && 'text-muted line-through')}>
                {l.extra && <Plus className="mr-1 inline h-3.5 w-3.5 text-accent" />}
                {l.nome}
              </span>
              <span className="num shrink-0 text-[13px] text-muted">
                {l.s != null ? `${l.s}×${l.r ?? '—'}` : ''}
                {l.c != null ? ` · ${n1(l.c)} ${l.u ?? 'kg'}` : ''}
              </span>
            </li>
          ))}
          {e.observacoes && <li className="py-2 text-[13px] text-muted">{e.observacoes}</li>}
        </ul>
      )}
    </div>
  );
}
