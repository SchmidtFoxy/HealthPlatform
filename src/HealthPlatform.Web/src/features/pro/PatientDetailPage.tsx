import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { motion } from 'framer-motion';
import { ChevronLeft, Copy, Dumbbell, ExternalLink, Flame, Mail, MessageCircle, Pencil, Phone, Plus, Salad, TrendingUp } from 'lucide-react';
import { ReactNode, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { Page } from '@/components/shell';
import { Button, Card, Empty, ErrorBox, MacroBar, Pill, Ring, SectionTitle, Segmented, Skeleton, Sparkline, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta, hora, idade, iniciais, n0, n1, tempoRelativo } from '@/lib/format';
import type { HomePaciente, PlanoAlimentarPro, PlanoTreinoPro } from '@/lib/types';
import { useHistoricoPaciente, usePaciente, usePlanosAlimentares, usePlanosTreino, useResumoClinico } from './queries';

type Aba = 'geral' | 'dieta' | 'treino';

interface EvolucaoCarga {
  exercicioId: string;
  exercicio: string;
  ultimaCarga: number | null;
  maiorCarga: number | null;
  registros: { dataHoraInicioUtc: string; cargaRealizada: number | null }[];
}

interface HistoricoPro {
  totalTreinos: number;
  minutosTotais: number;
  esforcoMedio: number | null;
  evolucaoCarga: EvolucaoCarga[];
  execucoes: { id: string; dataHoraInicioUtc: string; duracaoMinutos: number | null; esforcoPercebido: number | null; sessao: string; itens: unknown[] }[];
}

const STATUS_TONE: Record<string, 'ok' | 'neutral' | 'warn'> = { Ativo: 'ok', Inativo: 'neutral', Concluido: 'neutral', Rascunho: 'warn' };

function Stat({ value, label, tone }: { value: ReactNode; label: string; tone?: string }) {
  return (
    <div className="min-w-0">
      <p className={clsx('num text-2xl font-semibold leading-none', tone)}>{value}</p>
      <p className="mt-1.5 text-[12px] leading-tight text-muted">{label}</p>
    </div>
  );
}

function VisaoGeral({ id }: { id: string }) {
  const home = useQuery({ queryKey: ['pro', 'home', id], queryFn: () => api<HomePaciente>(`/api/pacientes/${id}/portal/home`) });
  const resumo = useResumoClinico(id);
  const hist = useHistoricoPaciente(id);
  const h = hist.data as unknown as HistoricoPro | undefined;
  const p = home.data?.prontidaoDiaria;
  const score = p?.score ?? p?.prontidaoScore;
  const ad = home.data?.adesaoNutricional;
  const progressoes = (h?.evolucaoCarga ?? [])
    .filter((e) => e.registros.length >= 2)
    .map((e) => {
      const primeiro = e.registros[0].cargaRealizada ?? 0;
      const ultimo = e.registros[e.registros.length - 1].cargaRealizada ?? 0;
      return { ...e, delta: ultimo - primeiro, pct: primeiro ? (ultimo - primeiro) / primeiro : 0 };
    })
    .sort((a, b) => b.pct - a.pct);

  if (home.isError) return <ErrorBox error={home.error} onRetry={() => home.refetch()} />;

  return (
    <div className="grid grid-cols-1 gap-x-6 lg:grid-cols-2">
      <section className="min-w-0">
        <SectionTitle>Hoje</SectionTitle>
        <Card className="flex items-center gap-5 p-5">
          {home.isLoading ? (
            <Skeleton className="h-24 w-full" />
          ) : (
            <>
              <Ring value={score ?? 0} size={96} stroke={9} color={score == null ? 'rgb(var(--line))' : score >= 75 ? 'rgb(var(--ok))' : score >= 55 ? 'rgb(var(--warn))' : 'rgb(var(--bad))'}>
                <span className="num text-2xl font-semibold">{score ?? '—'}</span>
              </Ring>
              <div className="min-w-0 flex-1">
                <p className="eyebrow">Prontidão</p>
                <p className="mt-1 font-semibold">{p ? p.recomendacaoTreino ?? 'Check-in feito' : 'Ainda sem check-in hoje'}</p>
                {p && (
                  <p className="mt-1 text-[13px] text-muted">
                    Sono {n1(p.sonoHoras)}h · energia {p.energiaNivel ?? '—'} · dor {p.dorNivel ?? '—'}
                  </p>
                )}
                {!p && home.data && <p className="mt-1 text-[13px] text-muted">{home.data.tendenciaRecuperacao.mensagem}</p>}
              </div>
            </>
          )}
        </Card>

        {ad && ad.refeicoesPlanejadas > 0 && (
          <Card className="mt-3 p-4">
            <div className="flex items-baseline justify-between">
              <p className="font-semibold">Refeições de hoje</p>
              <p className="num text-sm text-muted">
                {ad.realizadas + ad.adaptadas}/{ad.refeicoesPlanejadas}
              </p>
            </div>
            <div className="mt-3 flex gap-1.5">
              {ad.refeicoes.map((r) => (
                <div
                  key={r.refeicaoId}
                  title={`${r.nome}: ${r.status}`}
                  className={clsx(
                    'h-2 flex-1 rounded-full',
                    r.status === 'Realizada' ? 'bg-ok' : r.status === 'Adaptada' ? 'bg-warn' : r.status === 'NaoRealizada' ? 'bg-bad' : 'bg-line',
                  )}
                />
              ))}
            </div>
          </Card>
        )}

        <SectionTitle>Últimos 90 dias</SectionTitle>
        <Card className="grid grid-cols-3 gap-4 p-5">
          {hist.isLoading ? (
            <Skeleton className="col-span-3 h-12" />
          ) : (
            <>
              <Stat value={h?.totalTreinos ?? 0} label="treinos" tone="text-accent" />
              <Stat value={n0((h?.minutosTotais ?? 0) / 60)} label="horas treinando" />
              <Stat value={n1(h?.esforcoMedio)} label="esforço médio (RPE)" />
            </>
          )}
        </Card>

        {resumo.data && (
          <Card className="mt-3 divide-y divide-line/60 text-sm">
            <div className="flex justify-between p-4">
              <span className="text-muted">Última consulta</span>
              <span className="font-semibold">{resumo.data.ultimaConsulta?.dataHoraInicioUtc ? dataCurta(resumo.data.ultimaConsulta.dataHoraInicioUtc) : '—'}</span>
            </div>
            <div className="flex justify-between p-4">
              <span className="text-muted">Próxima consulta</span>
              <span className="font-semibold">{resumo.data.proximaConsulta?.dataHoraInicioUtc ? dataCurta(resumo.data.proximaConsulta.dataHoraInicioUtc) : 'Não marcada'}</span>
            </div>
            <div className="flex justify-between p-4">
              <span className="text-muted">Pendências abertas</span>
              <span className={clsx('font-semibold', resumo.data.pendenciasAbertas > 0 && 'text-warn')}>{resumo.data.pendenciasAbertas}</span>
            </div>
          </Card>
        )}
      </section>

      <section className="min-w-0">
        <SectionTitle>Evolução de carga</SectionTitle>
        {hist.isLoading ? (
          <Skeleton className="h-48" />
        ) : progressoes.length ? (
          <Card className="divide-y divide-line/60">
            {progressoes.slice(0, 6).map((e) => (
              <div key={e.exercicioId} className="flex items-center gap-3 px-4 py-3">
                <div className="min-w-0 flex-1">
                  <p className="truncate text-[15px] font-semibold">{e.exercicio}</p>
                  <p className="num text-[13px] text-muted">
                    {n1(e.registros[0].cargaRealizada)} → {n1(e.ultimaCarga)} kg
                  </p>
                </div>
                <div className="w-16 shrink-0">
                  <Sparkline points={e.registros.map((r) => r.cargaRealizada)} height={28} />
                </div>
                <Pill tone={e.delta > 0 ? 'ok' : e.delta < 0 ? 'bad' : 'neutral'}>
                  {e.delta > 0 ? '+' : ''}
                  {n1(e.delta)} kg
                </Pill>
              </div>
            ))}
          </Card>
        ) : (
          <Empty icon={<TrendingUp className="h-5 w-5" />} title="Sem progressão ainda">
            Aparece quando o paciente registrar o mesmo exercício pelo menos duas vezes.
          </Empty>
        )}

        {h && h.execucoes.length > 0 && (
          <>
            <SectionTitle>Treinos recentes</SectionTitle>
            <Card className="divide-y divide-line/60">
              {h.execucoes.slice(0, 5).map((x) => (
                <div key={x.id} className="flex items-center gap-3 px-4 py-3">
                  <div className="grid h-9 w-9 place-items-center rounded-full bg-accent/10 text-accent">
                    <Dumbbell className="h-4 w-4" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-semibold">{x.sessao}</p>
                    <p className="text-[13px] text-muted first-letter:uppercase">
                      {tempoRelativo(x.dataHoraInicioUtc)} · {x.duracaoMinutos ?? '—'} min
                    </p>
                  </div>
                  {x.esforcoPercebido != null && (
                    <span className="num flex items-center gap-1 text-sm text-muted">
                      <Flame className="h-4 w-4 text-warn" />
                      {x.esforcoPercebido}
                    </span>
                  )}
                </div>
              ))}
            </Card>
          </>
        )}
      </section>
    </div>
  );
}

function useAtivar(tipo: 'dieta' | 'treino', pacienteId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      api(tipo === 'dieta' ? `/api/planos-alimentares/${id}/status/${status}` : `/api/treinos/${id}/status/${status}`, { method: 'POST' }),
    onSuccess: (_, v) => {
      qc.invalidateQueries({ queryKey: ['pro', tipo === 'dieta' ? 'dietas' : 'treinos', pacienteId] });
      toast(v.status === 'Ativo' ? 'Plano liberado para o paciente' : 'Plano arquivado');
    },
    onError: (e) => toast(e.message, 'bad'),
  });
}

function useDuplicar(tipo: 'dieta' | 'treino', pacienteId: string) {
  const qc = useQueryClient();
  const nav = useNavigate();
  return useMutation({
    mutationFn: (plano: { id: string; nome: string }) =>
      api<{ id: string }>(tipo === 'dieta' ? `/api/planos-alimentares/${plano.id}/duplicar` : `/api/treinos/${plano.id}/duplicar`, {
        method: 'POST',
        json: { nome: `${plano.nome} (cópia)`, dataInicio: new Date().toISOString().slice(0, 10), concluirPlanoAnterior: false },
      }),
    onSuccess: (novo) => {
      qc.invalidateQueries({ queryKey: ['pro', tipo === 'dieta' ? 'dietas' : 'treinos', pacienteId] });
      nav(`/pro/pacientes/${pacienteId}/${tipo}/${novo.id}`);
    },
    onError: (e) => toast(e.message, 'bad'),
  });
}

function AcoesPlano({ tipo, pacienteId, plano }: { tipo: 'dieta' | 'treino'; pacienteId: string; plano: { id: string; nome: string; status: string } }) {
  const ativar = useAtivar(tipo, pacienteId);
  const duplicar = useDuplicar(tipo, pacienteId);
  return (
    <div className="flex flex-wrap gap-2 border-t border-line/60 p-3">
      <Link to={`/pro/pacientes/${pacienteId}/${tipo}/${plano.id}`}>
        <Button size="sm" variant="soft" icon={<Pencil className="h-4 w-4" />}>
          Editar
        </Button>
      </Link>
      <Button size="sm" variant="ghost" icon={<Copy className="h-4 w-4" />} loading={duplicar.isPending} onClick={() => duplicar.mutate(plano)}>
        Duplicar
      </Button>
      {plano.status === 'Ativo' ? (
        <Button size="sm" variant="ghost" className="ml-auto text-muted" loading={ativar.isPending} onClick={() => ativar.mutate({ id: plano.id, status: 'Concluido' })}>
          Arquivar
        </Button>
      ) : (
        <Button size="sm" variant="ghost" className="ml-auto text-accent" loading={ativar.isPending} onClick={() => ativar.mutate({ id: plano.id, status: 'Ativo' })}>
          Liberar ao paciente
        </Button>
      )}
    </div>
  );
}

function AbaDieta({ id }: { id: string }) {
  const q = usePlanosAlimentares(id);
  const planos = [...(q.data ?? [])].sort((a, b) => Number(b.status === 'Ativo') - Number(a.status === 'Ativo') || b.createdAtUtc.localeCompare(a.createdAtUtc));
  if (q.isLoading) return <Skeleton className="mt-6 h-64" />;
  if (q.isError) return <ErrorBox error={q.error} onRetry={() => q.refetch()} />;
  return (
    <div className="mt-6 flex flex-col gap-4">
      <Link to={`/pro/pacientes/${id}/dieta/novo`}>
        <Button className="w-full" variant="outline" icon={<Plus className="h-4 w-4" />}>
          Montar nova dieta
        </Button>
      </Link>
      {planos.length === 0 && <Empty icon={<Salad className="h-5 w-5" />} title="Nenhum plano alimentar">Monte a primeira dieta com metas, refeições e substituições.</Empty>}
      {planos.map((p: PlanoAlimentarPro) => {
        const t = p.totaisDiarios;
        return (
          <Card key={p.id} className="overflow-hidden">
            <div className="p-4">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="font-display text-[17px] font-semibold leading-snug">{p.nome}</p>
                  <p className="mt-0.5 text-[13px] text-muted">
                    Desde {dataCurta(p.dataInicio)} · {p.refeicoes.length} refeições · v{p.versao}
                  </p>
                </div>
                <Pill tone={STATUS_TONE[p.status] ?? 'neutral'}>{p.status === 'Concluido' ? 'Arquivado' : p.status === 'Inativo' ? 'Não liberado' : p.status}</Pill>
              </div>
              <div className="mt-4 flex items-baseline gap-1">
                <span className="num text-3xl font-semibold">{n0(t.calorias)}</span>
                <span className="text-sm text-muted">kcal{p.metaCalorias ? ` de ${n0(p.metaCalorias)}` : ''}</span>
              </div>
              <div className="mt-3 grid grid-cols-3 gap-3">
                <MacroBar label="Proteína" value={t.proteinasG} goal={p.metaProteinasG} color="rgb(var(--prot))" />
                <MacroBar label="Carbo" value={t.carboidratosG} goal={p.metaCarboidratosG} color="rgb(var(--carb))" />
                <MacroBar label="Gordura" value={t.gordurasG} goal={p.metaGordurasG} color="rgb(var(--fat))" />
              </div>
              <div className="no-scrollbar -mx-4 mt-4 flex gap-2 overflow-x-auto px-4">
                {p.refeicoes.map((r) => (
                  <div key={r.id} className="shrink-0 rounded-xl bg-raised/70 px-3 py-2">
                    <p className="num text-[11px] text-muted">{hora(r.horario) || '—'}</p>
                    <p className="text-[13px] font-semibold">{r.nome}</p>
                  </div>
                ))}
              </div>
            </div>
            <AcoesPlano tipo="dieta" pacienteId={id} plano={p} />
          </Card>
        );
      })}
    </div>
  );
}

function AbaTreino({ id }: { id: string }) {
  const q = usePlanosTreino(id);
  const planos = [...(q.data ?? [])].sort((a, b) => Number(b.status === 'Ativo') - Number(a.status === 'Ativo') || b.createdAtUtc.localeCompare(a.createdAtUtc));
  if (q.isLoading) return <Skeleton className="mt-6 h-64" />;
  if (q.isError) return <ErrorBox error={q.error} onRetry={() => q.refetch()} />;
  return (
    <div className="mt-6 flex flex-col gap-4">
      <Link to={`/pro/pacientes/${id}/treino/novo`}>
        <Button className="w-full" variant="outline" icon={<Plus className="h-4 w-4" />}>
          Montar novo treino
        </Button>
      </Link>
      {planos.length === 0 && <Empty icon={<Dumbbell className="h-5 w-5" />} title="Nenhum plano de treino">Monte a primeira divisão com séries, cargas e descanso.</Empty>}
      {planos.map((p: PlanoTreinoPro) => {
        const series = p.sessoes.reduce((s, x) => s + x.itens.reduce((a, i) => a + i.series, 0), 0);
        return (
          <Card key={p.id} className="overflow-hidden">
            <div className="p-4">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="font-display text-[17px] font-semibold leading-snug">{p.nome}</p>
                  <p className="mt-0.5 text-[13px] text-muted">{p.objetivo ?? `Desde ${dataCurta(p.dataInicio)}`}</p>
                </div>
                <Pill tone={STATUS_TONE[p.status] ?? 'neutral'}>{p.status === 'Concluido' ? 'Arquivado' : p.status === 'Inativo' ? 'Não liberado' : p.status}</Pill>
              </div>
              <div className="mt-4 flex flex-col gap-2">
                {p.sessoes.map((s) => (
                  <div key={s.id} className="flex items-center gap-3 rounded-xl bg-raised/70 px-3 py-2.5">
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-[15px] font-semibold">{s.nome}</p>
                      <p className="truncate text-[12px] text-muted">{s.itens.map((i) => i.exercicioNome).join(' · ')}</p>
                    </div>
                    {s.diasSemana && <Pill>{s.diasSemana}</Pill>}
                  </div>
                ))}
              </div>
              <p className="num mt-3 text-[13px] text-muted">
                {p.sessoes.length} sessões · {series} séries no total
              </p>
            </div>
            <AcoesPlano tipo="treino" pacienteId={id} plano={p} />
          </Card>
        );
      })}
    </div>
  );
}

export function PatientDetailPage() {
  const { id = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const aba = (params.get('aba') as Aba) || 'geral';
  const [, force] = useState(0);
  const q = usePaciente(id);
  const p = q.data;
  const a = idade(p?.dataNascimento);

  return (
    <Page wide>
      <Link to="/pro/pacientes" className="-ml-1 mt-4 inline-flex h-9 items-center gap-1 rounded-full pr-3 text-sm font-semibold text-muted hover:text-fg">
        <ChevronLeft className="h-5 w-5" /> Pacientes
      </Link>

      {q.isError && <ErrorBox error={q.error} onRetry={() => q.refetch()} />}
      {q.isLoading && <Skeleton className="mt-3 h-36" />}

      {p && (
        <motion.section initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} className="aurora mt-3 flex flex-col gap-4 rounded-xl3 border border-line p-5 lg:flex-row lg:items-center">
          <div className="flex min-w-0 flex-1 items-center gap-4">
            <div className="grid h-14 w-14 shrink-0 place-items-center rounded-full bg-accent/15 font-display text-lg sm:h-16 sm:w-16 sm:text-xl font-semibold text-accent ring-2 ring-accent/30 ring-offset-2 ring-offset-bg">{iniciais(p.nome)}</div>
            <div className="min-w-0">
              <h1 className="font-display text-[22px] font-semibold leading-tight tracking-tight sm:text-[26px]">{p.nome}</h1>
              <p className="truncate text-sm text-muted">{[a != null && `${a} anos`, p.sexo, p.profissao].filter(Boolean).join(' · ') || p.email}</p>
              {(p.tags.length > 0 || p.statusAcompanhamento !== 'Ativo') && (
                <div className="mt-2 flex flex-wrap gap-1.5">
                  {p.statusAcompanhamento !== 'Ativo' && <Pill tone="warn">{p.statusAcompanhamento}</Pill>}
                  {p.tags.map((t) => (
                    <Pill key={t}>{t}</Pill>
                  ))}
                </div>
              )}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-2 sm:flex">
            <Link to={`/pro/mensagens/${p.id}`} className="inline-flex h-11 min-w-0 flex-1 items-center justify-center gap-1.5 whitespace-nowrap rounded-full border border-accent/40 bg-accent/10 px-2.5 text-[13px] font-semibold text-accent transition hover:bg-accent/20 sm:flex-none sm:px-4">
              <MessageCircle className="h-4 w-4 shrink-0" /> Chat
            </Link>
            {p.telefone && (
              <a href={`https://wa.me/55${p.telefone.replace(/\D/g, '')}`} target="_blank" rel="noreferrer" className="inline-flex h-11 min-w-0 flex-1 items-center justify-center gap-1.5 whitespace-nowrap rounded-full border border-line bg-surface/70 px-2.5 text-[13px] font-semibold transition hover:border-accent/40 sm:flex-none sm:px-4">
                <Phone className="h-4 w-4 shrink-0 text-ok" /> WhatsApp
              </a>
            )}
            {p.email && (
              <a href={`mailto:${p.email}`} className="inline-flex h-11 min-w-0 flex-1 items-center justify-center gap-1.5 whitespace-nowrap rounded-full border border-line bg-surface/70 px-2.5 text-[13px] font-semibold transition hover:border-accent/40 sm:flex-none sm:px-4">
                <Mail className="h-4 w-4 shrink-0 text-info" /> E-mail
              </a>
            )}
            <a href="/?classico=1#pacientes" className="inline-flex h-11 min-w-0 flex-1 items-center justify-center gap-1.5 whitespace-nowrap rounded-full border border-line bg-surface/70 px-2.5 text-[13px] font-semibold transition hover:border-accent/40 sm:flex-none sm:px-4" title="Abre o prontuário completo na versão clássica">
              <ExternalLink className="h-4 w-4 shrink-0 text-muted" /> Prontuário
            </a>
          </div>
        </motion.section>
      )}

      <div className="sticky top-0 z-20 -mx-gutter mt-4 bg-bg/90 px-gutter py-2 pt-safe backdrop-blur-xl">
        <Segmented<Aba>
          value={aba}
          onChange={(v) => {
            setParams(v === 'geral' ? {} : { aba: v }, { replace: true });
            force((x) => x + 1);
          }}
          options={[
            { value: 'geral', label: 'Visão geral' },
            { value: 'dieta', label: 'Dieta' },
            { value: 'treino', label: 'Treino' },
          ]}
        />
      </div>

      {aba === 'geral' && <VisaoGeral id={id} />}
      {aba === 'dieta' && <AbaDieta id={id} />}
      {aba === 'treino' && <AbaTreino id={id} />}
    </Page>
  );
}
