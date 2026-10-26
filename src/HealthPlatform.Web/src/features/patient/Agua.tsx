import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { Droplet, Minus } from 'lucide-react';
import { forwardRef } from 'react';
import { toast } from '@/components/ui';
import { api } from '@/lib/api';
import { hojeIso, n0 } from '@/lib/format';
import { type Meta, useMetas } from './queries';

// Registro rapido de agua na tela Hoje.
// Com meta hidrica do profissional, soma no registro da meta do dia (o mesmo que o coach e o profissional leem).
// Sem meta, cada copo vira um registro de Hidratacao no diario.

interface RegistroDiario {
  id: string;
  dataHoraUtc: string;
  tipo: string;
  valorNumerico: number | null;
  unidade: string | null;
}

const ehAgua = (m: Meta) => {
  const t = `${m.tipo} ${m.nome}`.toLowerCase();
  return m.status === 'Ativa' && (t.includes('hidrat') || t.includes('agua') || t.includes('água'));
};
const emLitros = (u: string | null | undefined) => ['l', 'litro', 'litros'].includes((u ?? '').trim().toLowerCase());
const paraMl = (v: number, u: string | null | undefined) => (emLitros(u) ? v * 1000 : v);
const deMl = (ml: number, u: string | null | undefined) => (emLitros(u) ? ml / 1000 : ml);
const textoMl = (ml: number) => (ml >= 1000 ? `${(ml / 1000).toLocaleString('pt-BR', { maximumFractionDigits: 2 })} L` : `${n0(ml)} ml`);

export const AguaCard = forwardRef<HTMLDivElement, { delay?: number }>(function AguaCard({ delay = 0 }, ref) {
  const qc = useQueryClient();
  const metas = useMetas();
  const meta = (metas.data?.metas ?? []).find(ehAgua);
  const diario = useQuery({
    queryKey: ['p', 'diario-registros'],
    queryFn: () => api<{ total: number; itens: RegistroDiario[] }>('/api/portal/me/diario'),
    enabled: metas.isSuccess && !meta,
  });
  const hoje = hojeIso();

  const registroMeta = meta?.registros.find((r) => r.data === hoje);
  const doDiario = (diario.data?.itens ?? []).filter((r) => r.tipo === 'Hidratacao' && new Date(r.dataHoraUtc).toDateString() === new Date().toDateString());
  const bebidoMl = meta
    ? paraMl(registroMeta?.valor ?? 0, meta.unidade)
    : doDiario.reduce((s, r) => s + (r.valorNumerico == null ? 0 : r.unidade?.toLowerCase() === 'ml' ? r.valorNumerico : r.valorNumerico * 1000), 0);
  const metaMl = meta?.valorObjetivo ? paraMl(meta.valorObjetivo, meta.unidade) : null;
  const pct = metaMl ? Math.min(100, (bebidoMl / metaMl) * 100) : null;

  const somar = useMutation({
    mutationFn: async (ml: number) => {
      if (meta) {
        const total = Math.max(0, bebidoMl + ml);
        const valor = Math.round(deMl(total, meta.unidade) * 1000) / 1000;
        return api(`/api/portal/me/metas/${meta.id}/registro`, { method: 'POST', json: { data: hoje, valor, concluida: null, observacao: null } });
      }
      return api('/api/portal/me/diario', {
        method: 'POST',
        json: { dataHoraUtc: new Date().toISOString(), tipo: 'Hidratacao', descricao: 'Água', valorNumerico: ml / 1000, unidade: 'L', escala: null, imagemUrl: null },
      });
    },
    onSuccess: (_, ml) => {
      qc.invalidateQueries({ queryKey: ['p', meta ? 'metas' : 'diario-registros'] });
      qc.invalidateQueries({ queryKey: ['p', 'home'] });
      if (ml > 0 && metaMl && bebidoMl < metaMl && bebidoMl + ml >= metaMl) toast('Meta de água do dia batida. 💧');
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível registrar.', 'bad'),
  });

  if (!metas.isSuccess) return null;

  return (
    <motion.div ref={ref} id="agua" initial={{ opacity: 0, y: 14 }} animate={{ opacity: 1, y: 0 }} transition={{ delay, duration: 0.45 }} className="mt-3 scroll-mt-24 rounded-xl2 border border-line bg-surface p-4">
      <div className="flex items-center gap-3">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-info/15 text-info">
          <Droplet className="h-5 w-5" />
        </span>
        <div className="min-w-0 flex-1">
          <p className="font-semibold">Água hoje</p>
          <p className="num text-[13px] text-muted">
            {textoMl(bebidoMl)}
            {metaMl ? ` de ${textoMl(metaMl)}` : bebidoMl ? '' : ' · toque num copo quando beber'}
          </p>
        </div>
        {meta && bebidoMl > 0 && (
          <button aria-label="Tirar 250 ml" disabled={somar.isPending} onClick={() => somar.mutate(-250)} className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-raised text-muted transition active:scale-95">
            <Minus className="h-4 w-4" />
          </button>
        )}
      </div>
      {pct != null && (
        <div className="mt-3 h-2 overflow-hidden rounded-full bg-line/60">
          <motion.div className="h-full rounded-full bg-info" initial={false} animate={{ width: `${pct}%` }} transition={{ duration: 0.5 }} />
        </div>
      )}
      <div className="mt-3 grid grid-cols-3 gap-2">
        {[
          [250, 'Copo', '250 ml'],
          [500, 'Garrafa', '500 ml'],
          [1000, 'Garrafa', '1 L'],
        ].map(([ml, rotulo, qtd]) => (
          <button
            key={ml}
            disabled={somar.isPending}
            onClick={() => somar.mutate(ml as number)}
            className="flex flex-col items-center rounded-2xl bg-info/10 py-2.5 text-info transition active:scale-95 disabled:opacity-60"
          >
            <span className="text-sm font-semibold">+ {qtd}</span>
            <span className="text-[11px] text-info/80">{rotulo}</span>
          </button>
        ))}
      </div>
    </motion.div>
  );
});
