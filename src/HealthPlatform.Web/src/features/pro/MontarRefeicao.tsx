import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Lock, LockOpen, Plus, Star } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Button, Sheet, Stepper, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { n0, n1 } from '@/lib/format';
import { pluralizar } from '@/lib/medidas';
import { chaveRefeicao, type MetaRefeicao, montarPelaMeta, type Sugestao } from '@/lib/montarRefeicao';
import type { Alimento } from '@/lib/types';
import { SeletorAlimento } from './SeletorAlimento';

// "Montar pela meta": o profissional marca os alimentos (favoritos da refeicao ou outros)
// e o app calcula quanto de cada um para bater calorias e macros da refeicao.

interface RefeicaoBase {
  nome: string;
  metas: { calorias: number | null; proteinasG: number | null; carboidratosG: number | null; gordurasG: number | null };
  itens: { alimentoId: string; gramas: number }[];
}

const useFavoritos = () => useQuery({ queryKey: ['pro', 'alimentos-favoritos'], queryFn: () => api<{ refeicao: string; alimentoId: string }[]>('/api/alimentos/favoritos'), staleTime: 5 * 60_000 });

export function MontarPelaMetaSheet({
  refeicao,
  metaDia,
  totalRefeicoes,
  mapa,
  onClose,
  onAplicar,
}: {
  refeicao: RefeicaoBase | null;
  metaDia: MetaRefeicao;
  totalRefeicoes: number;
  mapa: Map<string, Alimento>;
  onClose: () => void;
  onAplicar: (metas: MetaRefeicao, itens: Sugestao[]) => void;
}) {
  const qc = useQueryClient();
  const favoritos = useFavoritos();
  const chave = refeicao ? chaveRefeicao(refeicao.nome) : '';
  const favDaRefeicao = useMemo(() => (favoritos.data ?? []).filter((f) => f.refeicao === chave).map((f) => f.alimentoId), [favoritos.data, chave]);
  const [meta, setMeta] = useState<MetaRefeicao>({ calorias: null, proteinasG: null, carboidratosG: null, gordurasG: null });
  const [metaSugerida, setMetaSugerida] = useState(false);
  const [lista, setLista] = useState<string[]>([]);
  const [marcados, setMarcados] = useState<Set<string>>(new Set());
  const [fixos, setFixos] = useState<Map<string, number>>(new Map());
  const [picker, setPicker] = useState(false);

  // Ao abrir: metas da refeicao (ou uma fatia igual das metas do dia) e os alimentos que ja estao nela.
  useEffect(() => {
    if (!refeicao) return;
    const temMeta = [refeicao.metas.calorias, refeicao.metas.proteinasG, refeicao.metas.carboidratosG, refeicao.metas.gordurasG].some((x) => x != null);
    const fatia = (v: number | null) => (v == null ? null : Math.round(v / Math.max(1, totalRefeicoes)));
    setMeta(temMeta ? { ...refeicao.metas } : { calorias: fatia(metaDia.calorias), proteinasG: fatia(metaDia.proteinasG), carboidratosG: fatia(metaDia.carboidratosG), gordurasG: fatia(metaDia.gordurasG) });
    setMetaSugerida(!temMeta && metaDia.calorias != null);
    const atuais = [...new Set(refeicao.itens.map((i) => i.alimentoId))].filter((id) => mapa.has(id));
    setLista(atuais);
    setMarcados(new Set(atuais));
    setFixos(new Map());
    // So ao abrir a janela.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [refeicao?.nome, !!refeicao]);

  // Favoritos entram na lista (desmarcados) assim que chegam.
  useEffect(() => {
    if (!refeicao) return;
    setLista((l) => [...l, ...favDaRefeicao.filter((id) => mapa.has(id) && !l.includes(id))]);
  }, [favDaRefeicao, refeicao, mapa]);

  const salvarFav = useMutation({
    mutationFn: (ids: string[]) => api<{ refeicao: string; alimentoId: string }[]>('/api/alimentos/favoritos', { method: 'PUT', json: { refeicao: refeicao!.nome, alimentoIds: ids } }),
    onSuccess: (r) => qc.setQueryData(['pro', 'alimentos-favoritos'], r),
    onError: (e) => toast(e.message, 'bad'),
  });
  const alternarFav = (id: string) => {
    const atual = new Set(favDaRefeicao);
    if (atual.has(id)) atual.delete(id);
    else atual.add(id);
    salvarFav.mutate([...atual]);
  };

  const escolhidos = lista.filter((id) => marcados.has(id) && mapa.has(id)).map((id) => ({ alimento: mapa.get(id)!, fixoG: fixos.get(id) ?? null }));
  const temMeta = [meta.calorias, meta.proteinasG, meta.carboidratosG, meta.gordurasG].some((x) => x != null && x > 0);
  const resultado = useMemo(() => (temMeta && escolhidos.length ? montarPelaMeta(meta, escolhidos) : null), [meta, escolhidos, temMeta]);

  const gramasAtuais = (id: string) => refeicao?.itens.find((i) => i.alimentoId === id)?.gramas ?? mapa.get(id)?.gramasPorUnidade ?? 100;
  const travar = (id: string) =>
    setFixos((m) => {
      const n = new Map(m);
      if (n.has(id)) n.delete(id);
      else n.set(id, resultado?.sugestoes.find((s) => s.alimentoId === id)?.gramas ?? gramasAtuais(id));
      return n;
    });

  const linhaMeta = (k: keyof MetaRefeicao, label: string, cor: string, step: number, feito?: number) => (
    <div className="flex items-center justify-between gap-3 py-1.5">
      <div>
        <p className={clsx('font-semibold', cor)}>{label}</p>
        {feito != null && meta[k] ? (
          <p className={clsx('num text-[12px]', Math.abs(feito - meta[k]!) > meta[k]! * 0.1 ? 'text-warn' : 'text-muted')}>
            calculado {n0(feito)}
            {k === 'calorias' ? ' kcal' : ' g'}
          </p>
        ) : null}
      </div>
      <Stepper value={meta[k]} onChange={(v) => setMeta((m) => ({ ...m, [k]: v || null }))} step={step} suffix={k === 'calorias' ? '' : 'g'} />
    </div>
  );

  return (
    <>
      <Sheet
        open={!!refeicao && !picker}
        onClose={onClose}
        title={`Montar ${refeicao?.nome ?? 'refeição'} pela meta`}
        tall
        footer={
          <Button className="w-full" size="lg" disabled={!resultado?.sugestoes.length} onClick={() => resultado && onAplicar(meta, resultado.sugestoes)}>
            Aplicar na refeição
          </Button>
        }
      >
        <p className="text-[13px] text-muted">Marque os alimentos e o app calcula quanto de cada um para chegar na meta. Use o cadeado para manter uma quantidade fixa.</p>

        <div className="mt-3 rounded-xl2 border border-line/70 px-3 py-1">
          {metaSugerida && <p className="pt-2 text-[12px] text-info">A refeição não tinha meta. Sugerimos uma parte igual das metas do dia; ajuste se quiser.</p>}
          {linhaMeta('calorias', 'Calorias', 'text-fg', 25, resultado?.total.kcal)}
          {linhaMeta('proteinasG', 'Proteína', 'text-prot', 5, resultado?.total.p)}
          {linhaMeta('carboidratosG', 'Carboidrato', 'text-carb', 5, resultado?.total.c)}
          {linhaMeta('gordurasG', 'Gordura', 'text-fat', 2, resultado?.total.g)}
        </div>

        <div className="mt-4 flex items-center justify-between">
          <p className="font-display font-semibold">Alimentos</p>
          <Button size="sm" variant="soft" icon={<Plus className="h-4 w-4" />} onClick={() => setPicker(true)}>
            Outro alimento
          </Button>
        </div>
        {!lista.length && (
          <p className="mt-2 rounded-xl bg-raised/60 p-3 text-sm text-muted">
            Adicione os alimentos desta refeição. Toque na estrela para guardar como favorito do {refeicao?.nome ?? 'refeição'}; da próxima vez eles já aparecem aqui.
          </p>
        )}
        <div className="mt-2 flex flex-col gap-2">
          {lista.map((id) => {
            const al = mapa.get(id);
            if (!al) return null;
            const marcado = marcados.has(id);
            const fixo = fixos.has(id);
            const sug = resultado?.sugestoes.find((s) => s.alimentoId === id);
            const fav = favDaRefeicao.includes(id);
            return (
              <div key={id} className={clsx('flex items-center gap-2 rounded-xl border p-2 pl-3', marcado ? 'border-accent/40 bg-accent/5' : 'border-line/70')}>
                <button
                  className="min-w-0 flex-1 text-left"
                  aria-pressed={marcado}
                  onClick={() =>
                    setMarcados((s) => {
                      const n = new Set(s);
                      if (n.has(id)) n.delete(id);
                      else n.add(id);
                      return n;
                    })
                  }
                >
                  <span className="flex items-center gap-2">
                    <span className={clsx('grid h-5 w-5 shrink-0 place-items-center rounded-md border text-[12px]', marcado ? 'border-accent bg-accent text-accent-ink' : 'border-line')}>{marcado ? '✓' : ''}</span>
                    <span className="truncate font-semibold">{al.nome}</span>
                  </span>
                  <span className="num block pl-7 text-[12px] text-muted">
                    {marcado && sug
                      ? `${sug.unidade === 'g' ? `${n0(sug.gramas)} g` : `${n1(sug.quantidade)} ${pluralizar(sug.unidade, sug.quantidade)} (${n0(sug.gramas)} g)`}${fixo ? ' · fixo' : ''}`
                      : marcado && resultado
                        ? 'Não precisou entrar'
                        : `${n0(al.caloriasPor100g)} kcal · P ${n0(al.proteinasPor100g)} · C ${n0(al.carboidratosPor100g)} · G ${n0(al.gordurasPor100g)} por 100 g`}
                  </span>
                </button>
                {marcado && (
                  <button aria-label={fixo ? 'Liberar quantidade' : 'Fixar quantidade'} onClick={() => travar(id)} className={clsx('grid h-9 w-9 place-items-center rounded-lg', fixo ? 'text-accent' : 'text-faint')}>
                    {fixo ? <Lock className="h-4 w-4" /> : <LockOpen className="h-4 w-4" />}
                  </button>
                )}
                <button aria-label={fav ? 'Tirar dos favoritos' : 'Guardar como favorito'} disabled={salvarFav.isPending} onClick={() => alternarFav(id)} className={clsx('grid h-9 w-9 place-items-center rounded-lg', fav ? 'text-warn' : 'text-faint')}>
                  <Star className={clsx('h-4 w-4', fav && 'fill-current')} />
                </button>
              </div>
            );
          })}
        </div>
        {!temMeta && <p className="mt-3 text-[13px] text-warn">Defina ao menos a meta de calorias para calcular.</p>}
        {lista.length > 0 && <p className="mt-3 text-[12px] text-faint">Ao aplicar, a refeição fica só com os alimentos marcados. Quem já estava nela mantém as substituições.</p>}
      </Sheet>
      <SeletorAlimento
        open={picker}
        onClose={() => setPicker(false)}
        titulo="Adicionar à conta"
        onPick={(a) => {
          setLista((l) => (l.includes(a.id) ? l : [...l, a.id]));
          setMarcados((s) => new Set(s).add(a.id));
          setPicker(false);
        }}
      />
    </>
  );
}
