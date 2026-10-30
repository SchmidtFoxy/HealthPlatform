import clsx from 'clsx';
import { Search } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { inputCls, Sheet, Skeleton } from '@/components/ui';
import { n0 } from '@/lib/format';
import type { Alimento } from '@/lib/types';
import { useAlimentos } from './queries';

// Busca de alimento usada no editor de dieta e no "montar pela meta".

/** Gramas do substituto que entregam as mesmas calorias do alimento original, arredondado a 5 g. */
export function gramasEquivalentes(orig: Alimento | undefined, gramas: number, sub: Alimento) {
  if (!orig || !sub.caloriasPor100g) return gramas;
  const kcal = (orig.caloriasPor100g * gramas) / 100;
  return Math.max(5, Math.round(((kcal / sub.caloriasPor100g) * 100) / 5) * 5);
}

export function SeletorAlimento({ open, onClose, onPick, titulo, referencia }: { open: boolean; onClose: () => void; onPick: (a: Alimento) => void; titulo: string; referencia?: { alimento?: Alimento; gramas: number } }) {
  const alimentos = useAlimentos();
  const [busca, setBusca] = useState('');
  const [cat, setCat] = useState<string | null>(null);
  useEffect(() => {
    if (open) {
      setBusca('');
      setCat(referencia?.alimento?.categoria ?? null);
    }
  }, [open, referencia?.alimento?.categoria]);
  const ativos = (alimentos.data ?? []).filter((a) => a.ativo);
  const cats = useMemo(() => [...new Set(ativos.map((a) => a.categoria).filter(Boolean) as string[])].sort(), [ativos]);
  const termo = busca
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase();
  const lista = ativos
    .filter((a) => !cat || a.categoria === cat)
    .filter((a) => !termo || a.nome.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase().includes(termo))
    .slice(0, 80);
  return (
    <Sheet open={open} onClose={onClose} title={titulo} tall>
      <label className="relative mb-3 block">
        <Search className="pointer-events-none absolute left-3.5 top-3 h-5 w-5 text-faint" />
        <input id="busca-alimento" type="search" placeholder="Buscar alimento" value={busca} onChange={(e) => setBusca(e.target.value)} className={inputCls + ' pl-11'} />
      </label>
      {cats.length > 0 && (
        <div className="no-scrollbar -mx-5 mb-3 flex gap-2 overflow-x-auto px-5">
          {[null, ...cats].map((c) => (
            <button key={c ?? 'todas'} onClick={() => setCat(c)} className={clsx('h-8 shrink-0 rounded-full border px-3 text-[13px] font-semibold', cat === c ? 'border-accent bg-accent text-accent-ink' : 'border-line text-muted')}>
              {c ?? 'Todas'}
            </button>
          ))}
        </div>
      )}
      {alimentos.isLoading && <Skeleton className="h-60" />}
      <div className="flex flex-col">
        {lista.map((a) => (
          <button key={a.id} onClick={() => onPick(a)} className="flex items-center gap-3 rounded-xl px-2 py-3 text-left transition hover:bg-raised active:bg-raised">
            <div className="min-w-0 flex-1">
              <p className="truncate font-semibold">{a.nome}</p>
              <p className="num text-[12px] text-muted">
                <span className="text-prot">P {n0(a.proteinasPor100g)}</span> · <span className="text-carb">C {n0(a.carboidratosPor100g)}</span> · <span className="text-fat">G {n0(a.gordurasPor100g)}</span> por 100 g
              </p>
            </div>
            {referencia?.alimento ? (
              <span className="num shrink-0 text-right text-sm font-semibold">
                {gramasEquivalentes(referencia.alimento, referencia.gramas, a)} g<span className="block text-[11px] font-normal text-muted">equivalente</span>
              </span>
            ) : (
              <span className="num shrink-0 text-sm text-muted">{n0(a.caloriasPor100g)} kcal</span>
            )}
          </button>
        ))}
        {!alimentos.isLoading && lista.length === 0 && <p className="py-10 text-center text-sm text-muted">Nenhum alimento encontrado. Cadastre novos alimentos na Biblioteca.</p>}
      </div>
    </Sheet>
  );
}
