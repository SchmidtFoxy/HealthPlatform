import clsx from 'clsx';
import { ArrowLeftRight, Check, Shuffle, X } from 'lucide-react';
import { useState } from 'react';
import { Pill, Sheet, toast } from '@/components/ui';
import { hora, n0 } from '@/lib/format';
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
        </>
      )}
    </Sheet>
  );
}
