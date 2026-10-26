import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { Plus, Trophy } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button, Card, Field, inputCls, Pill, Sheet, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta, hojeIso, n1 } from '@/lib/format';
import { Chips } from './CuidadosPro';

// Ciclos esportivos: um bloco com objetivo, datas e metas (treinos por semana, consistencia, peso),
// opcionalmente ligado a uma fase de treino e a uma fase nutricional. So um fica ativo por vez.

interface Ciclo {
  id: string;
  nome: string;
  perfilEsportivo: string;
  objetivo: string | null;
  dataInicio: string;
  dataFim: string;
  status: string;
  metaTreinosSemanais: number | null;
  metaConsistenciaPercentual: number | null;
  metaPesoKg: number | null;
  faseTreinoId: string | null;
  faseNutricionalId: string | null;
  observacoes: string | null;
}

interface FaseSimples {
  id: string;
  nome: string;
  status: string;
}

const PERFIS = [
  { value: 'Hipertrofia', label: 'Hipertrofia' },
  { value: 'Força', label: 'Força' },
  { value: 'Emagrecimento', label: 'Emagrecimento' },
  { value: 'Corrida', label: 'Corrida' },
  { value: 'Condicionamento', label: 'Condicionamento' },
  { value: 'Performance', label: 'Performance' },
  { value: 'QualidadeDeVida', label: 'Qualidade de vida' },
  { value: 'Personalizado', label: 'Personalizado' },
];
const STATUS = [
  { value: 'Planejado', label: 'Planejado' },
  { value: 'Ativo', label: 'Ativo' },
  { value: 'Concluido', label: 'Concluído' },
  { value: 'Cancelado', label: 'Cancelado' },
];
const TOM: Record<string, 'neutral' | 'ok' | 'info' | 'bad'> = { Ativo: 'ok', Planejado: 'info', Concluido: 'neutral', Cancelado: 'bad' };
const perfil = (v: string) => PERFIS.find((x) => x.value === v)?.label ?? v;

export function CiclosEsportivosSecao({ pacienteId }: { pacienteId: string }) {
  const [aberto, setAberto] = useState<Ciclo | 'novo' | null>(null);
  const q = useQuery({ queryKey: ['pro', 'ciclos', pacienteId], queryFn: () => api<Ciclo[]>(`/api/pacientes/${pacienteId}/ciclos-esportivos`) });
  const ciclos = [...(q.data ?? [])].sort((a, b) => Number(b.status === 'Ativo') - Number(a.status === 'Ativo') || b.dataInicio.localeCompare(a.dataInicio));
  const hoje = hojeIso();

  return (
    <section>
      <div className="mb-2 flex items-center justify-between gap-3">
        <div>
          <p className="font-semibold">Ciclos esportivos</p>
          <p className="text-[13px] text-muted">Blocos com objetivo, prazo e metas. Só um fica ativo por vez.</p>
        </div>
        <Button size="sm" variant="soft" icon={<Plus className="h-4 w-4" />} onClick={() => setAberto('novo')}>
          Ciclo
        </Button>
      </div>
      {ciclos.length > 0 && (
        <Card className="divide-y divide-line/60">
          {ciclos.map((c) => {
            const total = Math.max(1, (Date.parse(c.dataFim) - Date.parse(c.dataInicio)) / 86400000 + 1);
            const feito = Math.min(total, Math.max(0, (Date.parse(hoje) - Date.parse(c.dataInicio)) / 86400000 + 1));
            const metas = [
              c.metaTreinosSemanais ? `${c.metaTreinosSemanais} treinos/sem` : null,
              c.metaConsistenciaPercentual != null ? `${c.metaConsistenciaPercentual}% consistência` : null,
              c.metaPesoKg ? `${n1(c.metaPesoKg)} kg` : null,
            ].filter(Boolean);
            return (
              <button key={c.id} onClick={() => setAberto(c)} className="block w-full p-3.5 text-left transition hover:bg-raised/40">
                <span className="flex items-start gap-3">
                  <span className="grid h-9 w-9 shrink-0 place-items-center rounded-xl bg-accent/10 text-accent">
                    <Trophy className="h-4 w-4" />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-semibold">{c.nome}</span>
                    <span className="block text-[13px] text-muted">
                      {perfil(c.perfilEsportivo)} · {dataCurta(c.dataInicio)} a {dataCurta(c.dataFim)}
                    </span>
                    {metas.length > 0 && <span className="block text-[13px] text-muted">Metas: {metas.join(' · ')}</span>}
                  </span>
                  <Pill tone={TOM[c.status] ?? 'neutral'}>{STATUS.find((s) => s.value === c.status)?.label ?? c.status}</Pill>
                </span>
                {c.status === 'Ativo' && (
                  <span className="ml-12 mt-2 block h-1.5 overflow-hidden rounded-full bg-line/60">
                    <span className="block h-full rounded-full bg-accent" style={{ width: `${(feito / total) * 100}%` }} />
                  </span>
                )}
              </button>
            );
          })}
        </Card>
      )}
      <CicloSheet pacienteId={pacienteId} ciclo={aberto === 'novo' ? null : aberto} open={aberto !== null} onClose={() => setAberto(null)} />
    </section>
  );
}

function CicloSheet({ pacienteId, ciclo, open, onClose }: { pacienteId: string; ciclo: Ciclo | null; open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const fasesTreino = useQuery({ queryKey: ['pro', 'fases-simples', 'treino', pacienteId], queryFn: () => api<FaseSimples[]>(`/api/pacientes/${pacienteId}/fases-treino`), enabled: open });
  const fasesNutri = useQuery({ queryKey: ['pro', 'fases-simples', 'nutricao', pacienteId], queryFn: () => api<FaseSimples[]>(`/api/pacientes/${pacienteId}/fases-nutricionais`), enabled: open });
  const vazio = () => {
    const ini = hojeIso();
    const fim = new Date(`${ini}T12:00:00`);
    fim.setDate(fim.getDate() + 83);
    return { nome: '', perfilEsportivo: 'Hipertrofia', objetivo: '', dataInicio: ini, dataFim: fim.toISOString().slice(0, 10), status: 'Ativo', treinos: '', consistencia: '', peso: '', faseTreinoId: '', faseNutricionalId: '', observacoes: '' };
  };
  const [f, setF] = useState(vazio);
  useEffect(() => {
    if (!open) return;
    setF(
      ciclo
        ? {
            nome: ciclo.nome,
            perfilEsportivo: ciclo.perfilEsportivo,
            objetivo: ciclo.objetivo ?? '',
            dataInicio: ciclo.dataInicio.slice(0, 10),
            dataFim: ciclo.dataFim.slice(0, 10),
            status: ciclo.status,
            treinos: ciclo.metaTreinosSemanais?.toString() ?? '',
            consistencia: ciclo.metaConsistenciaPercentual?.toString() ?? '',
            peso: ciclo.metaPesoKg != null ? String(ciclo.metaPesoKg).replace('.', ',') : '',
            faseTreinoId: ciclo.faseTreinoId ?? '',
            faseNutricionalId: ciclo.faseNutricionalId ?? '',
            observacoes: ciclo.observacoes ?? '',
          }
        : vazio(),
    );
  }, [open, ciclo]);
  const set = (m: Partial<typeof f>) => setF((x) => ({ ...x, ...m }));
  const num = (s: string) => (s.trim() === '' ? null : Number(s.replace(',', '.')));

  const salvar = useMutation({
    mutationFn: () =>
      api(ciclo ? `/api/ciclos-esportivos/${ciclo.id}` : `/api/pacientes/${pacienteId}/ciclos-esportivos`, {
        method: ciclo ? 'PUT' : 'POST',
        json: {
          nome: f.nome.trim(),
          perfilEsportivo: f.perfilEsportivo,
          objetivo: f.objetivo.trim() || null,
          dataInicio: f.dataInicio,
          dataFim: f.dataFim,
          status: f.status,
          metaTreinosSemanais: num(f.treinos),
          metaConsistenciaPercentual: num(f.consistencia),
          metaPesoKg: num(f.peso),
          faseTreinoId: f.faseTreinoId || null,
          faseNutricionalId: f.faseNutricionalId || null,
          observacoes: f.observacoes.trim() || null,
        },
      }),
    onSuccess: () => {
      toast(ciclo ? 'Ciclo atualizado.' : 'Ciclo criado.');
      qc.invalidateQueries({ queryKey: ['pro', 'ciclos', pacienteId] });
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });

  const valido = !!f.nome.trim() && !!f.dataInicio && !!f.dataFim && f.dataFim >= f.dataInicio;
  const fase = (lista: FaseSimples[] | undefined, valor: string, onChange: (v: string) => void, rotulo: string) =>
    !!lista?.length && (
      <Field label={rotulo}>
        <select className={inputCls} value={valor} onChange={(e) => onChange(e.target.value)}>
          <option value="">Sem vínculo</option>
          {lista.map((x) => (
            <option key={x.id} value={x.id}>
              {x.nome}
            </option>
          ))}
        </select>
      </Field>
    );

  return (
    <Sheet
      open={open}
      onClose={onClose}
      tall
      title={ciclo ? 'Ciclo esportivo' : 'Novo ciclo esportivo'}
      footer={
        <Button size="lg" className="w-full" disabled={!valido} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          {ciclo ? 'Salvar ciclo' : 'Criar ciclo'}
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome">
          <input className={inputCls} value={f.nome} maxLength={160} placeholder="Ex.: Preparação meia maratona" onChange={(e) => set({ nome: e.target.value })} />
        </Field>
        <Field label="Perfil" group>
          <div className="-mx-1 overflow-x-auto px-1 pb-1">
            <Chips value={f.perfilEsportivo} onChange={(v) => set({ perfilEsportivo: v })} options={PERFIS.some((p) => p.value === f.perfilEsportivo) ? PERFIS : [...PERFIS, { value: f.perfilEsportivo, label: f.perfilEsportivo }]} />
          </div>
        </Field>
        <Field label="Objetivo">
          <input className={inputCls} value={f.objetivo} maxLength={300} placeholder="Ex.: completar 21 km abaixo de 2h" onChange={(e) => set({ objetivo: e.target.value })} />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Início">
            <input type="date" className={inputCls} value={f.dataInicio} onChange={(e) => set({ dataInicio: e.target.value })} />
          </Field>
          <Field label="Fim">
            <input type="date" className={clsx(inputCls, f.dataFim < f.dataInicio && 'border-bad')} value={f.dataFim} onChange={(e) => set({ dataFim: e.target.value })} />
          </Field>
        </div>
        <Field label="Situação" group>
          <Chips value={f.status} onChange={(v) => set({ status: v })} options={STATUS} />
        </Field>
        {f.status === 'Ativo' && ciclo?.status !== 'Ativo' && <p className="-mt-2 text-[12px] text-muted">Se já houver outro ciclo ativo, ele é concluído automaticamente.</p>}
        <div className="grid grid-cols-3 gap-3">
          <Field label="Treinos/sem">
            <input className={inputCls} inputMode="numeric" value={f.treinos} placeholder="—" onChange={(e) => set({ treinos: e.target.value.replace(/\D/g, '').slice(0, 2) })} />
          </Field>
          <Field label="Consistência %">
            <input className={inputCls} inputMode="numeric" value={f.consistencia} placeholder="—" onChange={(e) => set({ consistencia: e.target.value.replace(/\D/g, '').slice(0, 3) })} />
          </Field>
          <Field label="Peso alvo kg">
            <input className={inputCls} inputMode="decimal" value={f.peso} placeholder="—" onChange={(e) => set({ peso: e.target.value.replace(/[^\d,.]/g, '') })} />
          </Field>
        </div>
        {fase(fasesTreino.data, f.faseTreinoId, (v) => set({ faseTreinoId: v }), 'Fase de treino')}
        {fase(fasesNutri.data, f.faseNutricionalId, (v) => set({ faseNutricionalId: v }), 'Fase nutricional')}
        <Field label="Observações">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={f.observacoes} maxLength={1000} onChange={(e) => set({ observacoes: e.target.value })} />
        </Field>
      </div>
    </Sheet>
  );
}
