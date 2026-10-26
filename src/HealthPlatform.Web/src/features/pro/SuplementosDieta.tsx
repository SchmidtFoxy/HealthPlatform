import clsx from 'clsx';
import { Pill as PillIcon, Plus } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Card, Field, inputCls, Sheet } from '@/components/ui';
import { n0, n1 } from '@/lib/format';
import { type Suplemento, useSuplementos } from './suplementos';
import { usePlanosTreino } from './queries';

// Cronograma de suplementos dentro do editor de dieta: item do catalogo, porcoes, contexto,
// horario e vinculo opcional com uma refeicao ou sessao de treino. Macros entram no total do plano.

export interface SuplementoDieta {
  suplementoId: string;
  refeicaoK: string | null;
  sessaoTreinoId: string | null;
  quantidadePorcoes: number;
  horario: string | null;
  contexto: string;
  observacoes: string | null;
}

export const CONTEXTOS = ['Com refeição', 'Pré-treino', 'Pós-treino', 'Ao acordar', 'Antes de dormir', 'Entre refeições', 'Outro'];

export function macrosSuplementos(lista: SuplementoDieta[], catalogo: Map<string, Suplemento>) {
  return lista.reduce(
    (s, x) => {
      const c = catalogo.get(x.suplementoId);
      if (!c) return s;
      const q = x.quantidadePorcoes;
      return { kcal: s.kcal + c.caloriasPorPorcao * q, p: s.p + c.proteinasGPorPorcao * q, c: s.c + c.carboidratosGPorPorcao * q, g: s.g + c.gordurasGPorPorcao * q, f: s.f + c.fibrasGPorPorcao * q };
    },
    { kcal: 0, p: 0, c: 0, g: 0, f: 0 },
  );
}

export function SuplementosSecao({
  pacienteId,
  itens,
  refeicoes,
  catalogo,
  onChange,
}: {
  pacienteId: string;
  itens: SuplementoDieta[];
  refeicoes: { k: string; nome: string }[];
  catalogo: Map<string, Suplemento>;
  onChange: (l: SuplementoDieta[]) => void;
}) {
  const [aberto, setAberto] = useState<number | 'novo' | null>(null);
  const treinos = usePlanosTreino(pacienteId);
  const sessoes = (treinos.data ?? []).filter((p) => p.status !== 'Arquivado').flatMap((p) => p.sessoes.map((s) => ({ id: s.id, nome: `${p.nome} · ${s.nome}` })));
  const nomeSessao = (id: string | null) => sessoes.find((s) => s.id === id)?.nome;

  return (
    <section>
      <div className="mb-2 flex items-center justify-between gap-3">
        <div>
          <p className="font-semibold">Suplementos</p>
          <p className="text-[13px] text-muted">Entram no total do plano e aparecem no cronograma do paciente.</p>
        </div>
        <Button size="sm" variant="soft" icon={<Plus className="h-4 w-4" />} onClick={() => setAberto('novo')}>
          Suplemento
        </Button>
      </div>
      {itens.length > 0 && (
        <Card className="divide-y divide-line/60">
          {itens.map((x, i) => {
            const c = catalogo.get(x.suplementoId);
            const ref = refeicoes.find((r) => r.k === x.refeicaoK)?.nome;
            return (
              <button key={i} onClick={() => setAberto(i)} className="flex w-full items-center gap-3 p-3.5 text-left transition hover:bg-raised/40">
                <span className="grid h-9 w-9 shrink-0 place-items-center rounded-xl bg-accent/10 text-accent">
                  <PillIcon className="h-4 w-4" />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate font-semibold">{c?.nome ?? 'Suplemento'}</span>
                  <span className="block truncate text-[13px] text-muted">
                    {[
                      `${n1(x.quantidadePorcoes)} × ${c ? `${n1(c.porcaoQuantidade)} ${c.porcaoUnidade}` : 'porção'}`,
                      x.contexto,
                      x.horario?.slice(0, 5),
                      ref,
                      nomeSessao(x.sessaoTreinoId),
                    ]
                      .filter(Boolean)
                      .join(' · ')}
                  </span>
                </span>
                {c && <span className="num shrink-0 text-[13px] text-muted">{n0(c.caloriasPorPorcao * x.quantidadePorcoes)} kcal</span>}
              </button>
            );
          })}
        </Card>
      )}
      <SuplementoDietaSheet
        open={aberto !== null}
        item={typeof aberto === 'number' ? itens[aberto] : null}
        refeicoes={refeicoes}
        sessoes={sessoes}
        onClose={() => setAberto(null)}
        onSalvar={(x) => {
          onChange(typeof aberto === 'number' ? itens.map((o, i) => (i === aberto ? x : o)) : [...itens, x]);
          setAberto(null);
        }}
        onRemover={typeof aberto === 'number' ? () => (onChange(itens.filter((_, i) => i !== aberto)), setAberto(null)) : undefined}
      />
    </section>
  );
}

function SuplementoDietaSheet({
  open,
  item,
  refeicoes,
  sessoes,
  onClose,
  onSalvar,
  onRemover,
}: {
  open: boolean;
  item: SuplementoDieta | null;
  refeicoes: { k: string; nome: string }[];
  sessoes: { id: string; nome: string }[];
  onClose: () => void;
  onSalvar: (x: SuplementoDieta) => void;
  onRemover?: () => void;
}) {
  const cat = useSuplementos(true);
  const [f, setF] = useState<SuplementoDieta & { porcoes: string }>({ suplementoId: '', refeicaoK: null, sessaoTreinoId: null, quantidadePorcoes: 1, horario: null, contexto: 'Com refeição', observacoes: null, porcoes: '1' });
  useEffect(() => {
    if (open) setF(item ? { ...item, porcoes: String(item.quantidadePorcoes).replace('.', ',') } : { suplementoId: '', refeicaoK: null, sessaoTreinoId: null, quantidadePorcoes: 1, horario: null, contexto: 'Com refeição', observacoes: null, porcoes: '1' });
  }, [open, item]);
  const lista = (cat.data ?? []).filter((x) => x.ativo || x.id === f.suplementoId);
  const sel = lista.find((x) => x.id === f.suplementoId);
  const porcoes = Number(f.porcoes.replace(',', '.'));
  const valido = !!f.suplementoId && porcoes > 0;

  return (
    <Sheet
      open={open}
      onClose={onClose}
      title={item ? 'Suplemento no plano' : 'Adicionar suplemento'}
      footer={
        <Button size="lg" className="w-full" disabled={!valido} onClick={() => onSalvar({ suplementoId: f.suplementoId, refeicaoK: f.refeicaoK, sessaoTreinoId: f.sessaoTreinoId, quantidadePorcoes: porcoes, horario: f.horario, contexto: f.contexto, observacoes: f.observacoes })}>
          {item ? 'Salvar' : 'Adicionar'}
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        {cat.isSuccess && !lista.length ? (
          <p className="rounded-xl bg-raised p-4 text-sm text-muted">
            Nenhum suplemento no catálogo.{' '}
            <Link to="/pro/biblioteca?aba=suplementos" className="font-semibold text-accent">
              Cadastrar na Biblioteca
            </Link>
          </p>
        ) : (
          <Field label="Suplemento">
            <select className={inputCls} value={f.suplementoId} onChange={(e) => setF((x) => ({ ...x, suplementoId: e.target.value }))}>
              <option value="">Selecione…</option>
              {lista.map((x) => (
                <option key={x.id} value={x.id}>
                  {x.nome}
                  {x.marca ? ` · ${x.marca}` : ''}
                </option>
              ))}
            </select>
          </Field>
        )}
        {sel && (
          <p className="-mt-2 text-[13px] text-muted">
            Porção: {n1(sel.porcaoQuantidade)} {sel.porcaoUnidade} · {n0(sel.caloriasPorPorcao)} kcal · P {n1(sel.proteinasGPorPorcao)} g
            {sel.instrucoesUso ? ` · ${sel.instrucoesUso}` : ''}
          </p>
        )}
        <div className="grid grid-cols-2 gap-3">
          <Field label="Porções">
            <input className={inputCls} inputMode="decimal" value={f.porcoes} onChange={(e) => setF((x) => ({ ...x, porcoes: e.target.value }))} />
          </Field>
          <Field label="Horário">
            <input className={inputCls} type="time" value={f.horario?.slice(0, 5) ?? ''} onChange={(e) => setF((x) => ({ ...x, horario: e.target.value ? `${e.target.value}:00` : null }))} />
          </Field>
        </div>
        <div>
          <p className="mb-2 text-[13px] font-semibold text-muted">Quando</p>
          <div className="flex flex-wrap gap-2">
            {CONTEXTOS.map((c) => (
              <button
                key={c}
                type="button"
                aria-pressed={f.contexto === c}
                onClick={() => setF((x) => ({ ...x, contexto: c }))}
                className={clsx('rounded-full border px-3 py-1.5 text-[13px] font-semibold transition', f.contexto === c ? 'border-accent bg-accent/10 text-accent' : 'border-line text-muted')}
              >
                {c}
              </button>
            ))}
          </div>
        </div>
        <Field label="Junto com a refeição">
          <select className={inputCls} value={f.refeicaoK ?? ''} onChange={(e) => setF((x) => ({ ...x, refeicaoK: e.target.value || null }))}>
            <option value="">Sem vínculo</option>
            {refeicoes.map((r) => (
              <option key={r.k} value={r.k}>
                {r.nome}
              </option>
            ))}
          </select>
        </Field>
        {sessoes.length > 0 && (
          <Field label="Sessão de treino">
            <select className={inputCls} value={f.sessaoTreinoId ?? ''} onChange={(e) => setF((x) => ({ ...x, sessaoTreinoId: e.target.value || null }))}>
              <option value="">Sem vínculo</option>
              {sessoes.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.nome}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label="Orientação">
          <input className={inputCls} value={f.observacoes ?? ''} maxLength={500} placeholder="Ex.: junto com água" onChange={(e) => setF((x) => ({ ...x, observacoes: e.target.value || null }))} />
        </Field>
        <p className="text-[12px] text-faint">O AESYN organiza o cronograma; dose e indicação são decisão profissional.</p>
        {onRemover && (
          <Button variant="ghost" className="text-bad" onClick={onRemover}>
            Tirar do plano
          </Button>
        )}
      </div>
    </Sheet>
  );
}
