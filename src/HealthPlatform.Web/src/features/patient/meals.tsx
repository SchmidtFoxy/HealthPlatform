import clsx from 'clsx';
import { ArrowLeftRight, Check, Shuffle, X } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { Button, ErrorBox, Pill, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { hora, n0, n1 } from '@/lib/format';
import type { ItemRefeicaoPortal, Nutrientes, RefeicaoPortal, StatusRefeicao } from '@/lib/types';
import { useAdesaoRefeicao } from './queries';

export const somaNutrientes = (itens: { nutrientes: Nutrientes }[]): Nutrientes =>
  itens.reduce(
    (a, i) => ({
      calorias: a.calorias + i.nutrientes.calorias,
      proteinas: a.proteinas + i.nutrientes.proteinas,
      carboidratos: a.carboidratos + i.nutrientes.carboidratos,
      gorduras: a.gorduras + i.nutrientes.gorduras,
      fibras: a.fibras + i.nutrientes.fibras,
    }),
    { calorias: 0, proteinas: 0, carboidratos: 0, gorduras: 0, fibras: 0 },
  );

export function MacroDots({ n, className }: { n: Nutrientes; className?: string }) {
  return (
    <div className={clsx('num flex items-center gap-3 text-[12px] text-muted', className)}>
      <span className="flex items-center gap-1">
        <i className="h-1.5 w-1.5 rounded-full bg-prot" />P {n0(n.proteinas)}
      </span>
      <span className="flex items-center gap-1">
        <i className="h-1.5 w-1.5 rounded-full bg-carb" />C {n0(n.carboidratos)}
      </span>
      <span className="flex items-center gap-1">
        <i className="h-1.5 w-1.5 rounded-full bg-fat" />G {n0(n.gorduras)}
      </span>
    </div>
  );
}

const quantidade = (i: { quantidade: number; unidade: string; quantidadeGramas: number }) =>
  i.unidade === 'g' || i.unidade === 'ml' ? `${n0(i.quantidade)} ${i.unidade}` : `${n0(i.quantidade)} ${i.unidade}${i.quantidade > 1 && !i.unidade.endsWith('s') ? 's' : ''} · ${n0(i.quantidadeGramas)} g`;

function ItemLinha({ item }: { item: ItemRefeicaoPortal }) {
  const [aberto, setAberto] = useState(false);
  const temSubs = item.substituicoes.length > 0;
  return (
    <li className="border-b border-line/60 py-3 last:border-0">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="font-semibold leading-snug">{item.alimento}</p>
          <p className="mt-0.5 text-sm text-muted">{quantidade(item)}</p>
        </div>
        <div className="text-right">
          <p className="num font-semibold">{n0(item.nutrientes.calorias)}</p>
          <p className="text-[11px] text-faint">kcal</p>
        </div>
      </div>
      {temSubs && (
        <>
          <button onClick={() => setAberto((a) => !a)} className="mt-2 inline-flex items-center gap-1.5 rounded-full bg-accent/10 px-3 py-1 text-[12px] font-semibold text-accent">
            <ArrowLeftRight className="h-3.5 w-3.5" />
            {aberto ? 'Esconder trocas' : `${item.substituicoes.length} ${item.substituicoes.length > 1 ? 'trocas possíveis' : 'troca possível'}`}
          </button>
          {aberto && (
            <ul className="mt-2 flex flex-col gap-1.5 rounded-xl bg-raised/70 p-3">
              {item.substituicoes.map((s) => (
                <li key={s.id} className="flex items-center justify-between gap-3 text-sm">
                  <span className="min-w-0">
                    <span className="font-semibold">{s.alimento}</span> <span className="text-muted">· {quantidade(s)}</span>
                  </span>
                  <span className="num shrink-0 text-muted">{n0(s.nutrientes.calorias)} kcal</span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </li>
  );
}

export const STATUS_INFO: Record<StatusRefeicao, { label: string; tone: 'ok' | 'warn' | 'bad' | 'neutral' }> = {
  Pendente: { label: 'Pendente', tone: 'neutral' },
  Realizada: { label: 'Feita', tone: 'ok' },
  Adaptada: { label: 'Adaptei', tone: 'warn' },
  NaoRealizada: { label: 'Pulei', tone: 'bad' },
};

export function StatusButtons({ refeicaoId, status, compact }: { refeicaoId: string; status: StatusRefeicao; compact?: boolean }) {
  const m = useAdesaoRefeicao();
  const opcoes: { s: Exclude<StatusRefeicao, 'Pendente'>; icon: typeof Check; label: string; cls: string }[] = [
    { s: 'Realizada', icon: Check, label: 'Feita', cls: 'bg-ok text-white' },
    { s: 'Adaptada', icon: Shuffle, label: 'Adaptei', cls: 'bg-warn text-black' },
    { s: 'NaoRealizada', icon: X, label: 'Pulei', cls: 'bg-bad text-white' },
  ];
  return (
    <div className="flex gap-1.5">
      {opcoes.map((o) => {
        const ativo = status === o.s;
        return (
          <button
            key={o.s}
            aria-pressed={ativo}
            aria-label={o.label}
            onClick={(e) => {
              e.stopPropagation();
              if (navigator.vibrate) navigator.vibrate(12);
              m.mutate({ refeicaoId, status: o.s }, { onError: (err) => toast(err.message, 'bad') });
            }}
            className={clsx(
              'flex items-center justify-center gap-1.5 rounded-full font-semibold transition active:scale-90',
              compact ? 'h-9 w-9' : 'h-10 flex-1 px-3 text-sm',
              ativo ? o.cls : !compact && o.s === 'Realizada' ? 'bg-ok/15 text-ok' : 'bg-raised text-muted',
            )}
          >
            <o.icon className="h-4 w-4" strokeWidth={2.6} />
            {!compact && o.label}
          </button>
        );
      })}
    </div>
  );
}

export function MealSheet({ refeicao, status, onClose }: { refeicao: RefeicaoPortal | null; status: StatusRefeicao; onClose: () => void }) {
  const tot = refeicao ? somaNutrientes(refeicao.itens) : null;
  const [trocar, setTrocar] = useState(false);
  useEffect(() => setTrocar(false), [refeicao?.id]);
  if (refeicao && trocar) return <TrocarRefeicao refeicao={refeicao} onVoltar={() => setTrocar(false)} onClose={onClose} />;
  return (
    <Sheet
      open={!!refeicao}
      onClose={onClose}
      title={
        refeicao && (
          <span className="flex items-center gap-2">
            {refeicao.nome}
            <Pill tone={STATUS_INFO[status].tone}>{STATUS_INFO[status].label}</Pill>
          </span>
        )
      }
      footer={refeicao && <StatusButtons refeicaoId={refeicao.id} status={status} />}
    >
      {refeicao && tot && (
        <>
          <div className="flex items-end justify-between gap-3 rounded-xl2 bg-raised/70 p-4">
            <div>
              <p className="eyebrow">{hora(refeicao.horario) || 'Sem horário'}</p>
              <p className="num mt-1 text-3xl font-semibold">
                {n0(tot.calorias)} <span className="text-base font-normal text-muted">kcal</span>
              </p>
            </div>
            <MacroDots n={tot} />
          </div>
          {refeicao.observacoes && <p className="mt-3 rounded-xl border border-accent/30 bg-accent/10 px-3 py-2 text-sm">{refeicao.observacoes}</p>}
          <ul className="mt-2">
            {refeicao.itens.map((i) => (
              <ItemLinha key={i.id} item={i} />
            ))}
          </ul>
          <button onClick={() => setTrocar(true)} className="mt-3 flex w-full items-center justify-center gap-2 rounded-xl border border-dashed border-line py-3 text-sm font-semibold text-muted transition hover:text-fg">
            <ArrowLeftRight className="h-4 w-4" /> Trocar por outra refeição do plano
          </button>
        </>
      )}
    </Sheet>
  );
}

interface AlternativasRefeicao {
  refeicaoOriginalNome: string;
  caloriasOriginais: number;
  alternativas: { refeicaoId: string; planoNome: string; nome: string; horario: string | null; calorias: number; proteinasG: number; carboidratosG: number; gordurasG: number; similaridadePercentual: number; itens: string[] }[];
}

const MOTIVOS_TROCA = ['Não gostei desta refeição', 'Não tenho os alimentos agora', 'Estou fora de casa', 'Quero algo mais prático', 'Outro motivo'];

/** Troca a refeicao de hoje por outra ja prescrita, ordenada pela proximidade de calorias e macros. Fica registrada como adaptada. */
function TrocarRefeicao({ refeicao, onVoltar, onClose }: { refeicao: RefeicaoPortal; onVoltar: () => void; onClose: () => void }) {
  const q = useQuery({ queryKey: ['p', 'refeicao-alternativas', refeicao.id], queryFn: () => api<AlternativasRefeicao>(`/api/portal/me/refeicoes/${refeicao.id}/alternativas`) });
  const m = useAdesaoRefeicao();
  const [sel, setSel] = useState<string | null>(null);
  const [motivo, setMotivo] = useState(MOTIVOS_TROCA[0]);
  const alts = q.data?.alternativas ?? [];
  const escolhida = alts.find((a) => a.refeicaoId === sel) ?? alts[0];
  return (
    <Sheet
      open
      onClose={onClose}
      title={`Trocar ${refeicao.nome.toLowerCase()}`}
      footer={
        <div className="flex gap-2">
          <Button variant="ghost" size="lg" onClick={onVoltar}>
            Voltar
          </Button>
          <Button
            size="lg"
            className="flex-1"
            disabled={!escolhida}
            loading={m.isPending}
            onClick={() =>
              escolhida &&
              m.mutate(
                { refeicaoId: refeicao.id, status: 'Adaptada', observacao: `Troca AESYN: ${escolhida.nome} | ${motivo}` },
                {
                  onSuccess: () => {
                    toast(`Troca registrada: ${escolhida.nome}`);
                    onClose();
                  },
                  onError: (e) => toast(e.message, 'bad'),
                },
              )
            }
          >
            Usar esta hoje
          </Button>
        </div>
      }
    >
      <p className="-mt-1 mb-4 text-sm text-muted">Opções que já estão no seu plano, das mais parecidas para as menos. A troca fica registrada como adaptada para seu profissional ver.</p>
      {q.isLoading ? (
        <Skeleton className="h-48" />
      ) : q.error ? (
        <ErrorBox error={q.error} onRetry={() => q.refetch()} />
      ) : !alts.length ? (
        <p className="rounded-xl bg-raised p-4 text-sm text-muted">Ainda não há outra refeição prescrita para trocar hoje. Se precisar, marque como adaptada e conte no chat o que comeu.</p>
      ) : (
        <>
          <div className="flex flex-col gap-2" role="radiogroup" aria-label="Alternativas">
            {alts.map((a) => {
              const ativo = escolhida?.refeicaoId === a.refeicaoId;
              return (
                <button
                  key={a.refeicaoId}
                  role="radio"
                  aria-checked={ativo}
                  onClick={() => setSel(a.refeicaoId)}
                  className={clsx('rounded-xl border p-3.5 text-left transition', ativo ? 'border-accent bg-accent/10' : 'border-line hover:bg-raised/50')}
                >
                  <span className="flex items-baseline justify-between gap-2">
                    <span className="font-semibold">{a.nome}</span>
                    <span className="num shrink-0 text-sm font-semibold text-accent">{n0(a.similaridadePercentual)}%</span>
                  </span>
                  <span className="num mt-0.5 block text-sm text-muted">
                    {n0(a.calorias)} kcal · P {n1(a.proteinasG)} · C {n1(a.carboidratosG)} · G {n1(a.gordurasG)}
                    {a.horario ? ` · ${hora(a.horario)}` : ''}
                  </span>
                  {a.itens.length > 0 && <span className="mt-1 block text-[13px] text-faint">{a.itens.join(' · ')}</span>}
                </button>
              );
            })}
          </div>
          <p className="mb-2 mt-5 text-[13px] font-semibold text-muted">Por que trocar?</p>
          <div className="flex flex-wrap gap-2" role="group" aria-label="Motivo da troca">
            {MOTIVOS_TROCA.map((x) => (
              <button
                key={x}
                type="button"
                aria-pressed={motivo === x}
                onClick={() => setMotivo(x)}
                className={clsx('rounded-full border px-3.5 py-1.5 text-sm font-semibold transition', motivo === x ? 'border-accent bg-accent/10 text-accent' : 'border-line text-muted hover:text-fg')}
              >
                {x}
              </button>
            ))}
          </div>
        </>
      )}
    </Sheet>
  );
}
