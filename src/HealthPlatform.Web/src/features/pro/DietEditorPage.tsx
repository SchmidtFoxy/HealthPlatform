import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { AnimatePresence, motion } from 'framer-motion';
import { ArrowLeftRight, BookCopy, BookmarkPlus, ChevronDown, ChevronLeft, Clock, MoreHorizontal, Plus, Search, Target, Trash2 } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button, Card, ErrorBox, Field, IconButton, inputCls, Pill, Ring, Sheet, Skeleton, Stepper, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { hojeIso, n0 } from '@/lib/format';
import type { Alimento, PlanoAlimentarPro } from '@/lib/types';
import { InserirModeloSheet, SalvarParteComoModeloSheet } from './ModelosNoPlano';
import { useAlimentos, usePaciente, usePlanosAlimentares } from './queries';
import { useSuplementos } from './suplementos';
import { macrosSuplementos, SuplementosSecao } from './SuplementosDieta';

// ------------------------------------------------------------------ Modelo local

interface Sub {
  k: string;
  alimentoId: string;
  quantidade: number;
  unidade: string;
  gramas: number;
  observacao: string | null;
}
interface Item extends Sub {
  subs: Sub[];
}
interface Refeicao {
  k: string;
  id?: string;
  nome: string;
  horario: string;
  observacoes: string | null;
  metas: { calorias: number | null; proteinasG: number | null; carboidratosG: number | null; gordurasG: number | null; fibrasG: number | null };
  itens: Item[];
}
interface Metas {
  calorias: number | null;
  proteinasG: number | null;
  carboidratosG: number | null;
  gordurasG: number | null;
  fibrasG: number | null;
}
interface Rascunho {
  nome: string;
  dataInicio: string;
  dataFim: string | null;
  status: string;
  observacoes: string | null;
  metas: Metas;
  refeicoes: Refeicao[];
  suplementos: { suplementoId: string; refeicaoK: string | null; sessaoTreinoId: string | null; quantidadePorcoes: number; horario: string | null; contexto: string; observacoes: string | null }[];
}

const uid = () => Math.random().toString(36).slice(2, 10);
const semMetas = { calorias: null, proteinasG: null, carboidratosG: null, gordurasG: null, fibrasG: null };

const MODELO_REFEICOES: [string, string][] = [
  ['Café da manhã', '07:00'],
  ['Lanche da manhã', '10:00'],
  ['Almoço', '12:30'],
  ['Lanche da tarde', '16:00'],
  ['Jantar', '19:30'],
  ['Ceia', '22:00'],
];

function deServidor(p: PlanoAlimentarPro): Rascunho {
  const chaves = new Map<string, string>();
  const refeicoes = [...p.refeicoes]
    .sort((a, b) => a.ordem - b.ordem)
    .map((r) => {
      const k = uid();
      chaves.set(r.id, k);
      return {
        k,
        id: r.id,
        nome: r.nome,
        horario: (r.horario ?? '').slice(0, 5),
        observacoes: r.observacoes,
        metas: r.metas,
        itens: r.itens.map((i) => ({
          k: uid(),
          alimentoId: i.alimentoId,
          quantidade: i.quantidade,
          unidade: i.unidade,
          gramas: i.quantidadeGramas,
          observacao: i.observacao,
          subs: i.substituicoes.map((s) => ({ k: uid(), alimentoId: s.alimentoId, quantidade: s.quantidade, unidade: s.unidade, gramas: s.quantidadeGramas, observacao: s.observacao })),
        })),
      };
    });
  return {
    nome: p.nome,
    dataInicio: p.dataInicio,
    dataFim: p.dataFim,
    status: p.status,
    observacoes: p.observacoes,
    metas: { calorias: p.metaCalorias, proteinasG: p.metaProteinasG, carboidratosG: p.metaCarboidratosG, gordurasG: p.metaGordurasG, fibrasG: p.metaFibrasG },
    refeicoes,
    suplementos: p.suplementos.map((s) => ({ ...s, refeicaoK: s.refeicaoId ? chaves.get(s.refeicaoId) ?? null : null })),
  };
}

function novo(): Rascunho {
  return {
    nome: 'Plano alimentar',
    dataInicio: hojeIso(),
    dataFim: null,
    status: 'Inativo',
    observacoes: null,
    metas: { ...semMetas },
    refeicoes: MODELO_REFEICOES.slice(0, 5).map(([nome, horario]) => ({ k: uid(), nome, horario, observacoes: null, metas: { ...semMetas }, itens: [] })),
    suplementos: [],
  };
}

function paraServidor(d: Rascunho, status: string) {
  const ordem = new Map(d.refeicoes.map((r, i) => [r.k, i + 1]));
  const item = (i: Sub) => ({
    alimentoId: i.alimentoId,
    quantidade: i.unidade === 'g' ? i.gramas : i.quantidade,
    unidade: i.unidade || 'g',
    quantidadeGramas: i.gramas,
    observacao: i.observacao,
  });
  return {
    nome: d.nome.trim() || 'Plano alimentar',
    dataInicio: d.dataInicio,
    dataFim: d.dataFim,
    status,
    observacoes: d.observacoes,
    metaCalorias: d.metas.calorias,
    metaProteinasG: d.metas.proteinasG,
    metaCarboidratosG: d.metas.carboidratosG,
    metaGordurasG: d.metas.gordurasG,
    metaFibrasG: d.metas.fibrasG,
    refeicoes: d.refeicoes.map((r, idx) => ({
      nome: r.nome.trim() || `Refeição ${idx + 1}`,
      horario: r.horario ? `${r.horario}:00`.slice(0, 8) : null,
      ordem: idx + 1,
      observacoes: r.observacoes,
      metaCalorias: r.metas.calorias,
      metaProteinasG: r.metas.proteinasG,
      metaCarboidratosG: r.metas.carboidratosG,
      metaGordurasG: r.metas.gordurasG,
      metaFibrasG: r.metas.fibrasG,
      itens: r.itens.map((i) => ({ ...item(i), substituicoes: i.subs.map(item) })),
    })),
    suplementos: d.suplementos.map((s) => ({
      suplementoId: s.suplementoId,
      refeicaoOrdem: s.refeicaoK ? ordem.get(s.refeicaoK) ?? null : null,
      sessaoTreinoId: s.sessaoTreinoId,
      quantidadePorcoes: s.quantidadePorcoes,
      horario: s.horario,
      contexto: s.contexto,
      observacoes: s.observacoes,
    })),
  };
}

// ------------------------------------------------------------------ Calculo

type Macros = { kcal: number; p: number; c: number; g: number; f: number };
const zero: Macros = { kcal: 0, p: 0, c: 0, g: 0, f: 0 };
const soma = (a: Macros, b: Macros): Macros => ({ kcal: a.kcal + b.kcal, p: a.p + b.p, c: a.c + b.c, g: a.g + b.g, f: a.f + b.f });

function macrosDe(al: Alimento | undefined, gramas: number): Macros {
  if (!al) return zero;
  const k = gramas / 100;
  return { kcal: al.caloriasPor100g * k, p: al.proteinasPor100g * k, c: al.carboidratosPor100g * k, g: al.gordurasPor100g * k, f: al.fibrasPor100g * k };
}

/** Gramas do substituto que entregam as mesmas calorias do alimento original, arredondado a 5 g. */
function gramasEquivalentes(orig: Alimento | undefined, gramas: number, sub: Alimento) {
  if (!orig || !sub.caloriasPor100g) return gramas;
  const kcal = (orig.caloriasPor100g * gramas) / 100;
  return Math.max(5, Math.round(((kcal / sub.caloriasPor100g) * 100) / 5) * 5);
}

// ------------------------------------------------------------------ Seletor de alimento

function SeletorAlimento({ open, onClose, onPick, titulo, referencia }: { open: boolean; onClose: () => void; onPick: (a: Alimento) => void; titulo: string; referencia?: { alimento?: Alimento; gramas: number } }) {
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

// ------------------------------------------------------------------ Item

function LinhaItem({ item, al, onOpen }: { item: Item; al?: Alimento; onOpen: () => void }) {
  const m = macrosDe(al, item.gramas);
  return (
    <button onClick={onOpen} className="flex w-full items-center gap-3 px-4 py-3 text-left transition hover:bg-raised/50 active:bg-raised/70">
      <div className="min-w-0 flex-1">
        <p className="truncate font-semibold">{al?.nome ?? 'Alimento indisponível'}</p>
        <p className="num text-[12px] text-muted">
          {item.unidade !== 'g' && item.quantidade ? `${n0(item.quantidade)} ${item.unidade} · ` : ''}
          {n0(item.gramas)} g · P {n0(m.p)} · C {n0(m.c)} · G {n0(m.g)}
        </p>
      </div>
      {item.subs.length > 0 && (
        <span className="flex items-center gap-1 text-[12px] font-semibold text-info">
          <ArrowLeftRight className="h-3.5 w-3.5" />
          {item.subs.length}
        </span>
      )}
      <span className="num w-14 shrink-0 text-right font-semibold">{n0(m.kcal)}</span>
    </button>
  );
}

function ItemSheet({
  item,
  mapa,
  onClose,
  onChange,
  onRemove,
}: {
  item: Item | null;
  mapa: Map<string, Alimento>;
  onClose: () => void;
  onChange: (i: Item) => void;
  onRemove: () => void;
}) {
  const [picker, setPicker] = useState(false);
  if (!item) return <Sheet open={false} onClose={onClose}>{null}</Sheet>;
  const al = mapa.get(item.alimentoId);
  const m = macrosDe(al, item.gramas);
  const setGramas = (g: number) => {
    const fator = item.gramas ? g / item.gramas : 1;
    onChange({ ...item, gramas: g, quantidade: item.unidade === 'g' ? g : Math.round(item.quantidade * fator * 100) / 100 });
  };
  return (
    <>
      <Sheet
        open={!!item && !picker}
        onClose={onClose}
        title={al?.nome ?? 'Alimento'}
        footer={
          <div className="flex gap-2">
            <Button variant="danger" icon={<Trash2 className="h-4 w-4" />} onClick={onRemove}>
              Remover
            </Button>
            <Button className="flex-1" onClick={onClose}>
              Pronto
            </Button>
          </div>
        }
      >
        <div className="flex flex-col items-center gap-4 py-2">
          <Stepper big value={item.gramas} onChange={setGramas} step={al && al.caloriasPor100g > 300 ? 5 : 10} min={1} suffix="g" label="Quantidade" />
          <div className="grid w-full grid-cols-4 gap-2 text-center">
            {[
              ['kcal', m.kcal, 'text-fg'],
              ['prot', m.p, 'text-prot'],
              ['carb', m.c, 'text-carb'],
              ['gord', m.g, 'text-fat'],
            ].map(([l, v, c]) => (
              <div key={l as string} className="rounded-xl bg-raised/70 py-2">
                <p className={clsx('num text-lg font-semibold', c as string)}>{n0(v as number)}</p>
                <p className="text-[11px] text-muted">{l as string}</p>
              </div>
            ))}
          </div>
          {item.unidade !== 'g' && (
            <div className="grid w-full grid-cols-2 gap-3">
              <Field label="Medida caseira">
                <input className={inputCls} inputMode="decimal" value={item.quantidade} onChange={(e) => onChange({ ...item, quantidade: Number(e.target.value.replace(',', '.')) || 0 })} />
              </Field>
              <Field label="Unidade">
                <input className={inputCls} value={item.unidade} onChange={(e) => onChange({ ...item, unidade: e.target.value })} />
              </Field>
            </div>
          )}
          <div className="w-full">
          <Field label="Orientação para o paciente">
            <input className={inputCls} placeholder="Ex.: grelhado, sem óleo" value={item.observacao ?? ''} onChange={(e) => onChange({ ...item, observacao: e.target.value || null })} />
          </Field>
          </div>
        </div>

        <div className="mt-5 flex items-center justify-between">
          <p className="font-display font-semibold">Substituições</p>
          <Button size="sm" variant="soft" icon={<Plus className="h-4 w-4" />} onClick={() => setPicker(true)}>
            Adicionar
          </Button>
        </div>
        <p className="mt-1 text-[13px] text-muted">A quantidade sugerida mantém as mesmas calorias do alimento original.</p>
        <div className="mt-3 flex flex-col gap-2">
          {item.subs.map((s) => {
            const sa = mapa.get(s.alimentoId);
            const sm = macrosDe(sa, s.gramas);
            return (
              <div key={s.k} className="flex items-center gap-2 rounded-xl border border-line/70 p-2 pl-3">
                <div className="min-w-0 flex-1">
                  <p className="truncate text-[15px] font-semibold">{sa?.nome ?? 'Alimento'}</p>
                  <p className="num text-[12px] text-muted">
                    {n0(sm.kcal)} kcal · P {n0(sm.p)} · C {n0(sm.c)} · G {n0(sm.g)}
                  </p>
                </div>
                <input
                  aria-label="Gramas"
                  inputMode="decimal"
                  className="num h-10 w-16 rounded-lg border border-line bg-surface text-center font-semibold"
                  value={s.gramas}
                  onChange={(e) => {
                    const g = Number(e.target.value.replace(',', '.')) || 0;
                    onChange({ ...item, subs: item.subs.map((x) => (x.k === s.k ? { ...x, gramas: g, quantidade: x.unidade === 'g' ? g : x.quantidade } : x)) });
                  }}
                />
                <span className="text-xs text-muted">g</span>
                <IconButton label="Remover substituição" onClick={() => onChange({ ...item, subs: item.subs.filter((x) => x.k !== s.k) })}>
                  <Trash2 className="h-4 w-4 text-faint" />
                </IconButton>
              </div>
            );
          })}
        </div>
      </Sheet>
      <SeletorAlimento
        open={picker}
        onClose={() => setPicker(false)}
        titulo="Substituir por"
        referencia={{ alimento: al, gramas: item.gramas }}
        onPick={(a) => {
          const g = gramasEquivalentes(al, item.gramas, a);
          onChange({ ...item, subs: [...item.subs, { k: uid(), alimentoId: a.id, quantidade: g, unidade: 'g', gramas: g, observacao: null }] });
          setPicker(false);
        }}
      />
    </>
  );
}

// ------------------------------------------------------------------ Metas

/** Percentual de cada refeicao nas metas do dia: o que ja estava salvo ou, sem isso, partes iguais. */
function percentuaisIniciais(refeicoes: Refeicao[], metas: Metas): number[] {
  const total = metas.calorias ?? 0;
  const salvos = refeicoes.map((r) => (total && r.metas.calorias != null ? Math.round((r.metas.calorias / total) * 100) : null));
  if (salvos.every((x) => x != null) && salvos.reduce((a, b) => a + (b ?? 0), 0) === 100) return salvos as number[];
  const base = Math.floor(100 / Math.max(1, refeicoes.length));
  return refeicoes.map((_, i) => (i === 0 ? 100 - base * (refeicoes.length - 1) : base));
}

const parte = (v: number | null, pct: number) => (v == null ? null : Math.round((v * pct) / 100));

function MetasSheet({
  open,
  onClose,
  metas,
  onChange,
  atual,
  refeicoes,
  onDistribuir,
}: {
  open: boolean;
  onClose: () => void;
  metas: Metas;
  onChange: (m: Metas) => void;
  atual: Macros;
  refeicoes: Refeicao[];
  onDistribuir: (pcts: number[]) => void;
}) {
  const [dividir, setDividir] = useState(false);
  const [pcts, setPcts] = useState<number[]>([]);
  useEffect(() => {
    if (open) {
      setDividir(false);
      setPcts(percentuaisIniciais(refeicoes, metas));
    }
    // So ao abrir: depois o profissional ajusta a vontade.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);
  const somaPct = pcts.reduce((a, b) => a + b, 0);
  const kcalMacros = (metas.proteinasG ?? 0) * 4 + (metas.carboidratosG ?? 0) * 4 + (metas.gordurasG ?? 0) * 9;
  const campo = (k: keyof Metas, label: string, step: number, cor: string) => (
    <div className="flex items-center justify-between gap-3 py-2">
      <div>
        <p className={clsx('font-semibold', cor)}>{label}</p>
        {k !== 'calorias' && k !== 'fibrasG' && metas[k] != null && kcalMacros > 0 && (
          <p className="num text-[12px] text-muted">{n0((((metas[k] ?? 0) * (k === 'gordurasG' ? 9 : 4)) / kcalMacros) * 100)}% das calorias</p>
        )}
      </div>
      <Stepper value={metas[k]} onChange={(v) => onChange({ ...metas, [k]: v || null })} step={step} suffix={k === 'calorias' ? '' : 'g'} />
    </div>
  );
  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Metas do dia"
      footer={
        <Button className="w-full" size="lg" onClick={onClose}>
          Aplicar
        </Button>
      }
    >
      {campo('calorias', 'Calorias', 50, 'text-fg')}
      {kcalMacros > 0 && Math.abs(kcalMacros - (metas.calorias ?? 0)) > 30 && (
        <button className="mb-2 w-full rounded-xl bg-accent/10 px-3 py-2 text-left text-[13px] text-accent" onClick={() => onChange({ ...metas, calorias: Math.round(kcalMacros) })}>
          Os macros somam <b className="num">{n0(kcalMacros)} kcal</b>. Toque para usar esse valor.
        </button>
      )}
      {campo('proteinasG', 'Proteína', 5, 'text-prot')}
      {campo('carboidratosG', 'Carboidrato', 5, 'text-carb')}
      {campo('gordurasG', 'Gordura', 5, 'text-fat')}
      {campo('fibrasG', 'Fibras', 1, 'text-muted')}
      <button
        className="mt-3 w-full rounded-xl border border-dashed border-line px-3 py-3 text-[13px] text-muted"
        onClick={() => onChange({ calorias: Math.round(atual.kcal), proteinasG: Math.round(atual.p), carboidratosG: Math.round(atual.c), gordurasG: Math.round(atual.g), fibrasG: Math.round(atual.f) })}
      >
        Usar o que já está montado ({n0(atual.kcal)} kcal) como meta
      </button>
      {metas.calorias != null && refeicoes.length > 1 && (
        <div className="mt-4 border-t border-line/60 pt-3">
          <button className="flex w-full items-center justify-between text-left" onClick={() => setDividir((x) => !x)} aria-expanded={dividir}>
            <span>
              <span className="block font-semibold">Dividir entre as refeições</span>
              <span className="block text-[13px] text-muted">Cada refeição ganha a sua meta, em % do dia.</span>
            </span>
            <ChevronDown className={clsx('h-4 w-4 shrink-0 text-faint transition', dividir && 'rotate-180')} />
          </button>
          {dividir && (
            <div className="mt-2">
              {refeicoes.map((r, i) => (
                <div key={r.k} className="flex items-center justify-between gap-3 py-1.5">
                  <div className="min-w-0">
                    <p className="truncate font-semibold">{r.nome || 'Refeição'}</p>
                    <p className="num text-[12px] text-muted">
                      {n0(parte(metas.calorias, pcts[i] ?? 0) ?? 0)} kcal
                      {metas.proteinasG != null ? ` · P ${n0(parte(metas.proteinasG, pcts[i] ?? 0) ?? 0)}g` : ''}
                    </p>
                  </div>
                  <Stepper value={pcts[i] ?? 0} onChange={(v) => setPcts((x) => x.map((p, j) => (j === i ? Math.max(0, Math.min(100, v || 0)) : p)))} step={5} suffix="%" />
                </div>
              ))}
              <Button
                className="mt-2 w-full"
                variant="soft"
                disabled={somaPct !== 100}
                onClick={() => {
                  onDistribuir(pcts);
                  toast('Metas divididas entre as refeições.');
                }}
              >
                {somaPct === 100 ? 'Aplicar nas refeições' : `A soma está em ${somaPct}%, precisa dar 100%`}
              </Button>
            </div>
          )}
        </div>
      )}
    </Sheet>
  );
}

function MiniMacro({ label, value, goal, cor }: { label: string; value: number; goal?: number | null; cor: 'prot' | 'carb' | 'fat' }) {
  const pct = goal ? Math.min(1, value / goal) : 0;
  const fora = goal ? Math.abs(value - goal) / goal > 0.1 : false;
  return (
    <div className="min-w-0">
      <p className="whitespace-nowrap text-[11px] text-muted">{label}</p>
      <p className="num whitespace-nowrap leading-tight">
        <span className={clsx('text-[15px] font-semibold', fora && 'text-warn')}>{n0(value)}</span>
        <span className="text-[11px] text-faint">{goal ? `/${n0(goal)}` : 'g'}</span>
      </p>
      <div className="mt-1 h-1.5 overflow-hidden rounded-full bg-line/60">
        <div className="h-full rounded-full transition-all" style={{ width: `${pct * 100}%`, background: `rgb(var(--${cor}))` }} />
      </div>
    </div>
  );
}

// ------------------------------------------------------------------ Pagina

export function DietEditorPage() {
  const { id = '', planoId = 'novo' } = useParams();
  const ehNovo = planoId === 'novo';
  const nav = useNavigate();
  const qc = useQueryClient();
  const paciente = usePaciente(id);
  const alimentos = useAlimentos();
  const outros = usePlanosAlimentares(id);
  const plano = useQuery({ queryKey: ['pro', 'dieta', planoId], queryFn: () => api<PlanoAlimentarPro>(`/api/planos-alimentares/${planoId}`), enabled: !ehNovo });
  const [d, setD] = useState<Rascunho | null>(ehNovo ? novo() : null);
  const [sujo, setSujo] = useState(false);
  const [aberto, setAberto] = useState<{ r: string; i: string } | null>(null);
  const [addEm, setAddEm] = useState<string | null>(null);
  const [metasOpen, setMetasOpen] = useState(false);
  const [menuRef, setMenuRef] = useState<string | null>(null);
  const [inserir, setInserir] = useState(false);
  const [salvarModelo, setSalvarModelo] = useState<{ id: string; nome: string } | null>(null);

  useEffect(() => {
    if (plano.data && !d) setD(deServidor(plano.data));
  }, [plano.data, d]);

  useEffect(() => {
    if (!sujo) return;
    const h = (e: BeforeUnloadEvent) => e.preventDefault();
    window.addEventListener('beforeunload', h);
    return () => window.removeEventListener('beforeunload', h);
  }, [sujo]);

  const mapa = useMemo(() => new Map((alimentos.data ?? []).map((a) => [a.id, a])), [alimentos.data]);
  const up = (fn: (x: Rascunho) => Rascunho) => {
    setD((x) => (x ? fn(x) : x));
    setSujo(true);
  };
  const upRef = (k: string, fn: (r: Refeicao) => Refeicao) => up((x) => ({ ...x, refeicoes: x.refeicoes.map((r) => (r.k === k ? fn(r) : r)) }));

  const totaisRef = (r: Refeicao) => r.itens.reduce((s, i) => soma(s, macrosDe(mapa.get(i.alimentoId), i.gramas)), zero);
  const catSup = useSuplementos(true);
  const mapaSup = useMemo(() => new Map((catSup.data ?? []).map((x) => [x.id, x])), [catSup.data]);
  const total = d ? soma(d.refeicoes.reduce((s, r) => soma(s, totaisRef(r)), zero), macrosSuplementos(d.suplementos, mapaSup)) : zero;

  const salvar = useMutation({
    mutationFn: async (liberar: boolean) => {
      if (!d) throw new Error('Nada para salvar');
      const status = liberar ? 'Ativo' : ehNovo ? 'Inativo' : d.status;
      const body = paraServidor(d, status);
      const salvo = ehNovo
        ? await api<PlanoAlimentarPro>(`/api/pacientes/${id}/planos-alimentares`, { method: 'POST', json: body })
        : await api<PlanoAlimentarPro>(`/api/planos-alimentares/${planoId}`, { method: 'PUT', json: body });
      if (liberar) {
        // O paciente enxerga um plano ativo por vez: os demais ativos saem de cena.
        const ativos = (outros.data ?? []).filter((p) => p.status === 'Ativo' && p.id !== salvo.id);
        await Promise.all(ativos.map((p) => api(`/api/planos-alimentares/${p.id}/status/Inativo`, { method: 'POST' })));
      }
      return salvo;
    },
    onSuccess: (salvo, liberar) => {
      setSujo(false);
      qc.invalidateQueries({ queryKey: ['pro'] });
      toast(liberar ? 'Dieta liberada para o paciente' : 'Dieta salva');
      if (ehNovo) nav(`/pro/pacientes/${id}/dieta/${salvo.id}`, { replace: true });
      else setD(deServidor(salvo));
    },
    onError: (e) => toast(e.message, 'bad'),
  });

  const voltar = () => {
    if (sujo && !confirm('Sair sem salvar as alterações?')) return;
    nav(`/pro/pacientes/${id}?aba=dieta`);
  };

  if (plano.isError) return <div className="p-gutter"><ErrorBox error={plano.error} onRetry={() => plano.refetch()} /></div>;

  const itemAberto = aberto && d ? d.refeicoes.find((r) => r.k === aberto.r)?.itens.find((i) => i.k === aberto.i) ?? null : null;
  const metaKcal = d?.metas.calorias ?? null;
  const vazio = d ? d.refeicoes.every((r) => r.itens.length === 0) : true;

  return (
    <div className="min-h-dvh bg-bg">
      {/* Cabecalho fixo com o placar do dia */}
      <div className="sticky top-0 z-30 border-b border-line/60 bg-bg/90 pt-safe backdrop-blur-xl">
        <div className="mx-auto flex max-w-3xl items-center gap-2 px-gutter pt-2">
          <IconButton label="Voltar" onClick={voltar} className="-ml-2">
            <ChevronLeft className="h-6 w-6" />
          </IconButton>
          <div className="min-w-0 flex-1">
            <p className="truncate text-[12px] text-muted">{paciente.data?.nome ?? ' '}</p>
            {d ? (
              <input aria-label="Nome do plano" className="w-full truncate bg-transparent font-display text-lg font-semibold outline-none" value={d.nome} onChange={(e) => up((x) => ({ ...x, nome: e.target.value }))} />
            ) : (
              <Skeleton className="h-6 w-40" />
            )}
          </div>
          {d && <Pill tone={d.status === 'Ativo' ? 'ok' : 'warn'}>{d.status === 'Ativo' ? 'Liberada' : 'Não liberada'}</Pill>}
        </div>
        <button onClick={() => setMetasOpen(true)} className="mx-auto flex w-full max-w-3xl items-center gap-4 px-gutter pb-3 pt-2 text-left">
          <Ring value={total.kcal} max={metaKcal || Math.max(total.kcal, 1)} size={64} stroke={7} color={metaKcal && total.kcal > metaKcal * 1.05 ? 'rgb(var(--warn))' : 'rgb(var(--accent))'}>
            <span className="num text-[13px] font-semibold leading-none">{n0(total.kcal)}</span>
          </Ring>
          <div className="grid min-w-0 flex-1 grid-cols-3 gap-3">
            <MiniMacro label="Prot" value={total.p} goal={d?.metas.proteinasG} cor="prot" />
            <MiniMacro label="Carb" value={total.c} goal={d?.metas.carboidratosG} cor="carb" />
            <MiniMacro label="Gord" value={total.g} goal={d?.metas.gordurasG} cor="fat" />
          </div>
          <Target className="h-5 w-5 shrink-0 text-faint" />
        </button>
      </div>

      <div className="mx-auto max-w-3xl px-gutter pb-40 pt-4">
        {!d || alimentos.isLoading ? (
          <div className="flex flex-col gap-3">
            <Skeleton className="h-40" />
            <Skeleton className="h-40" />
          </div>
        ) : (
          <>
            {metaKcal == null && (
              <button onClick={() => setMetasOpen(true)} className="mb-4 flex w-full items-center gap-3 rounded-xl2 border border-accent/30 bg-accent/10 p-4 text-left text-sm">
                <Target className="h-5 w-5 shrink-0 text-accent" />
                <span>
                  <b>Defina as metas do dia</b> para acompanhar calorias e macros enquanto monta.
                </span>
              </button>
            )}
            <div className="flex flex-col gap-4">
              <AnimatePresence initial={false}>
                {d.refeicoes.map((r) => {
                  const t = totaisRef(r);
                  return (
                    <motion.div key={r.k} layout initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, height: 0 }}>
                      <Card className="overflow-hidden">
                        <div className="flex items-start gap-2 px-4 pb-2 pt-3">
                          <div className="min-w-0 flex-1">
                            <input aria-label="Nome da refeição" className="w-full bg-transparent font-display text-[17px] font-semibold outline-none" value={r.nome} onChange={(e) => upRef(r.k, (x) => ({ ...x, nome: e.target.value }))} />
                            <label className="mt-0.5 inline-flex items-center gap-1.5 text-muted">
                              <Clock className="h-3.5 w-3.5" />
                              <input aria-label="Horário" type="time" className="num w-[5.5rem] bg-transparent text-base sm:w-[4.75rem] sm:text-[13px] font-semibold text-muted outline-none [&::-webkit-calendar-picker-indicator]:hidden" value={r.horario} onChange={(e) => upRef(r.k, (x) => ({ ...x, horario: e.target.value }))} />
                            </label>
                          </div>
                          <div className="pt-1 text-right">
                            <p className={clsx('num font-semibold leading-none', r.metas.calorias && Math.abs(t.kcal - r.metas.calorias) > r.metas.calorias * 0.1 && 'text-warn')}>{n0(t.kcal)}</p>
                            <p className="num text-[11px] text-muted">{r.metas.calorias ? `de ${n0(r.metas.calorias)} kcal` : 'kcal'}</p>
                          </div>
                          <IconButton label="Opções da refeição" onClick={() => setMenuRef(r.k)} className="-mr-2">
                            <MoreHorizontal className="h-5 w-5" />
                          </IconButton>
                        </div>
                        {r.itens.length > 0 && (
                          <div className="divide-y divide-line/50 border-t border-line/50">
                            {r.itens.map((i) => (
                              <LinhaItem key={i.k} item={i} al={mapa.get(i.alimentoId)} onOpen={() => setAberto({ r: r.k, i: i.k })} />
                            ))}
                          </div>
                        )}
                        <button onClick={() => setAddEm(r.k)} className="flex w-full items-center gap-2 border-t border-line/50 px-4 py-3 text-sm font-semibold text-accent">
                          <Plus className="h-4 w-4" /> Adicionar alimento
                        </button>
                      </Card>
                    </motion.div>
                  );
                })}
              </AnimatePresence>
              <Button
                variant="outline"
                icon={<Plus className="h-4 w-4" />}
                onClick={() => {
                  const usados = new Set(d.refeicoes.map((r) => r.nome));
                  const prox = MODELO_REFEICOES.find(([n]) => !usados.has(n)) ?? [`Refeição ${d.refeicoes.length + 1}`, ''];
                  up((x) => ({ ...x, refeicoes: [...x.refeicoes, { k: uid(), nome: prox[0], horario: prox[1], observacoes: null, metas: { ...semMetas }, itens: [] }] }));
                }}
              >
                Nova refeição
              </Button>
              <Button variant="ghost" icon={<BookCopy className="h-4 w-4" />} onClick={() => setInserir(true)}>
                Inserir refeição-modelo
              </Button>
              <Field label="Orientações gerais do plano">
                <textarea className={inputCls + ' h-24 py-3'} placeholder="Hidratação, temperos liberados, combinações…" value={d.observacoes ?? ''} onChange={(e) => up((x) => ({ ...x, observacoes: e.target.value || null }))} />
              </Field>
              <SuplementosSecao pacienteId={id} itens={d.suplementos} refeicoes={d.refeicoes.map((r, i) => ({ k: r.k, nome: r.nome || `Refeição ${i + 1}` }))} catalogo={mapaSup} onChange={(l) => up((x) => ({ ...x, suplementos: l }))} />
            </div>
          </>
        )}
      </div>

      {/* Barra de acao */}
      <div className="fixed inset-x-0 bottom-0 z-30 border-t border-line/60 bg-bg/90 pb-safe backdrop-blur-xl">
        <div className="mx-auto flex max-w-3xl gap-2 px-gutter py-3">
          <Button variant="outline" className="flex-1" disabled={!d || vazio || (!sujo && !ehNovo)} loading={salvar.isPending && salvar.variables === false} onClick={() => salvar.mutate(false)}>
            {sujo || ehNovo ? 'Salvar' : 'Salvo'}
          </Button>
          {(d?.status !== 'Ativo' || sujo) && (
            <Button className="flex-[1.4]" disabled={!d || vazio} loading={salvar.isPending && salvar.variables === true} onClick={() => salvar.mutate(true)}>
              {d?.status === 'Ativo' ? 'Salvar e notificar' : 'Liberar ao paciente'}
            </Button>
          )}
        </div>
      </div>

      <SeletorAlimento
        open={!!addEm}
        onClose={() => setAddEm(null)}
        titulo="Adicionar alimento"
        onPick={(a) => {
          const g = a.caloriasPor100g > 300 ? 30 : 100;
          const novoItem: Item = { k: uid(), alimentoId: a.id, quantidade: g, unidade: 'g', gramas: g, observacao: null, subs: [] };
          const r = addEm!;
          upRef(r, (x) => ({ ...x, itens: [...x.itens, novoItem] }));
          setAddEm(null);
          setAberto({ r, i: novoItem.k });
        }}
      />

      <ItemSheet
        item={itemAberto}
        mapa={mapa}
        onClose={() => setAberto(null)}
        onChange={(novoItem) => aberto && upRef(aberto.r, (x) => ({ ...x, itens: x.itens.map((i) => (i.k === novoItem.k ? novoItem : i)) }))}
        onRemove={() => {
          if (!aberto) return;
          upRef(aberto.r, (x) => ({ ...x, itens: x.itens.filter((i) => i.k !== aberto.i) }));
          setAberto(null);
        }}
      />

      {d && (
        <MetasSheet
          open={metasOpen}
          onClose={() => setMetasOpen(false)}
          metas={d.metas}
          onChange={(m) => up((x) => ({ ...x, metas: m }))}
          atual={total}
          refeicoes={d.refeicoes}
          onDistribuir={(pcts) =>
            up((x) => ({
              ...x,
              refeicoes: x.refeicoes.map((r, i) => ({
                ...r,
                metas: {
                  calorias: parte(x.metas.calorias, pcts[i]),
                  proteinasG: parte(x.metas.proteinasG, pcts[i]),
                  carboidratosG: parte(x.metas.carboidratosG, pcts[i]),
                  gordurasG: parte(x.metas.gordurasG, pcts[i]),
                  fibrasG: parte(x.metas.fibrasG, pcts[i]),
                },
              })),
            }))
          }
        />
      )}

      <InserirModeloSheet
        tipo="refeicao"
        open={inserir}
        onClose={() => setInserir(false)}
        onInserir={(c, nome) => {
          const ok = (a: string) => mapa.has(a);
          const faltando = c.itens.filter((i) => !ok(i.alimentoId)).length;
          const sub = (x: { alimentoId: string; quantidade: number; unidade: string; quantidadeGramas: number; observacao: string | null }) => ({
            k: uid(),
            alimentoId: x.alimentoId,
            quantidade: x.quantidade,
            unidade: x.unidade,
            gramas: x.quantidadeGramas,
            observacao: x.observacao,
          });
          const nova: Refeicao = {
            k: uid(),
            nome: c.nomeOriginal || nome,
            horario: (c.horarioOriginal ?? '').slice(0, 5),
            observacoes: c.observacoesOriginais,
            metas: { calorias: c.metaCalorias, proteinasG: c.metaProteinasG, carboidratosG: c.metaCarboidratosG, gordurasG: c.metaGordurasG, fibrasG: c.metaFibrasG },
            itens: c.itens.filter((i) => ok(i.alimentoId)).map((i) => ({ ...sub(i), subs: i.substituicoes.filter((x) => ok(x.alimentoId)).map(sub) })),
          };
          up((x) => ({ ...x, refeicoes: [...x.refeicoes, nova] }));
          toast(faltando ? `Refeição inserida. ${faltando} alimento(s) inativo(s) ficaram de fora.` : 'Refeição inserida no rascunho.');
        }}
      />
      <SalvarParteComoModeloSheet tipo="refeicao" alvo={salvarModelo} onClose={() => setSalvarModelo(null)} />

      <Sheet open={!!menuRef} onClose={() => setMenuRef(null)} title={d?.refeicoes.find((r) => r.k === menuRef)?.nome}>
        {menuRef && d && (
          <div className="flex flex-col gap-2 pb-2">
            {(() => {
              const idx = d.refeicoes.findIndex((r) => r.k === menuRef);
              const mover = (delta: number) =>
                up((x) => {
                  const arr = [...x.refeicoes];
                  const [r] = arr.splice(idx, 1);
                  arr.splice(idx + delta, 0, r);
                  return { ...x, refeicoes: arr };
                });
              return (
                <>
                  <Button variant="outline" disabled={idx === 0} onClick={() => mover(-1)}>
                    Mover para cima
                  </Button>
                  <Button variant="outline" disabled={idx === d.refeicoes.length - 1} onClick={() => mover(1)}>
                    Mover para baixo
                  </Button>
                  <Button
                    variant="outline"
                    onClick={() => {
                      const r = d.refeicoes[idx];
                      up((x) => {
                        const arr = [...x.refeicoes];
                        arr.splice(idx + 1, 0, { ...r, k: uid(), nome: `${r.nome} (cópia)`, itens: r.itens.map((i) => ({ ...i, k: uid(), subs: i.subs.map((s) => ({ ...s, k: uid() })) })) });
                        return { ...x, refeicoes: arr };
                      });
                      setMenuRef(null);
                    }}
                  >
                    Duplicar refeição
                  </Button>
                  <Button
                    variant="outline"
                    icon={<BookmarkPlus className="h-4 w-4" />}
                    disabled={!d.refeicoes[idx].itens.length}
                    onClick={() => {
                      const r = d.refeicoes[idx];
                      if (!r.id || sujo) return toast('Salve a dieta antes de guardar a refeição como modelo.', 'bad');
                      setMenuRef(null);
                      setSalvarModelo({ id: r.id, nome: r.nome });
                    }}
                  >
                    Salvar como modelo
                  </Button>
                  <Button
                    variant="danger"
                    icon={<Trash2 className="h-4 w-4" />}
                    onClick={() => {
                      up((x) => ({ ...x, refeicoes: x.refeicoes.filter((r) => r.k !== menuRef), suplementos: x.suplementos.map((s) => (s.refeicaoK === menuRef ? { ...s, refeicaoK: null } : s)) }));
                      setMenuRef(null);
                    }}
                  >
                    Excluir refeição
                  </Button>
                </>
              );
            })()}
          </div>
        )}
      </Sheet>
    </div>
  );
}
