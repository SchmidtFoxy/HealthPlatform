import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ClipboardCheck, Pencil, Plus, TrendingDown, TrendingUp } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button, Card, Field, inputCls, Sheet, Skeleton, Sparkline, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta, n0, n1 } from '@/lib/format';

// Check-ins de evolucao: o retrato periodico de peso, adesao e disposicao que o
// paciente envia (ou o profissional registra) e que fica ligado as fases atuais.

export interface CheckIn {
  id: string;
  dataUtc: string;
  pesoKg: number | null;
  adesaoAlimentacaoPercentual: number | null;
  adesaoTreinoPercentual: number | null;
  fomeNivel: number | null;
  energiaNivel: number | null;
  sonoNivel: number | null;
  percepcaoEvolucaoNivel: number | null;
  observacoes: string | null;
  origem: string | null;
  faseNutricionalId: string | null;
  faseNutricionalNome: string | null;
  faseTreinoId: string | null;
  faseTreinoNome: string | null;
}

interface Historico {
  total: number;
  atual: CheckIn | null;
  itens: CheckIn[];
}

interface Fase {
  id: string;
  nome: string;
}

/** paciente: null para o proprio paciente (portal); o id para a equipe. */
const rota = (paciente: string | null) => (paciente ? `/api/pacientes/${paciente}/check-ins` : '/api/portal/me/check-ins');
const chave = (paciente: string | null) => ['check-ins', paciente ?? 'eu'];

export function useCheckIns(paciente: string | null) {
  return useQuery({ queryKey: chave(paciente), queryFn: () => api<Historico>(`${rota(paciente)}?limite=30`) });
}

const METRICAS = [
  { k: 'pesoKg', l: 'Peso', u: ' kg', fmt: n1, min: undefined, max: undefined },
  { k: 'adesaoAlimentacaoPercentual', l: 'Adesão à dieta', u: '%', fmt: n0, min: 0, max: 100 },
  { k: 'adesaoTreinoPercentual', l: 'Adesão ao treino', u: '%', fmt: n0, min: 0, max: 100 },
  { k: 'energiaNivel', l: 'Energia', u: '/10', fmt: n0, min: 0, max: 10 },
] as const;

/** Cartoes com o ultimo valor e a tendencia, seguidos do historico. */
export function CheckInsPainel({ paciente, titulo, onNovo, onEditar }: { paciente: string | null; titulo?: string; onNovo: () => void; onEditar?: (c: CheckIn) => void }) {
  const q = useCheckIns(paciente);
  const itens = q.data?.itens ?? [];

  return (
    <section>
      <div className="mb-3 flex items-center justify-between gap-2">
        <div className="min-w-0">
          {titulo && <h2 className="font-display text-[17px] font-semibold tracking-tight">{titulo}</h2>}
          <p className="text-sm text-muted">{itens.length ? `${q.data!.total} ${q.data!.total === 1 ? 'registro' : 'registros'} · último ${dataCurta(itens[itens.length - 1].dataUtc)}` : 'Peso, adesão e disposição ao longo das fases'}</p>
        </div>
        <Button size="sm" icon={<Plus className="h-4 w-4" />} onClick={onNovo}>
          Novo check-in
        </Button>
      </div>
      {q.isLoading ? (
        <Skeleton className="h-40" />
      ) : !itens.length ? (
        <Card className="flex items-start gap-3 p-4 text-sm text-muted">
          <ClipboardCheck className="h-5 w-5 shrink-0 text-faint" />
          <span>Nenhum check-in ainda. Ele leva um minuto e mostra como o plano está funcionando: peso, quanto da dieta e do treino foi seguido, fome, energia e sono.</span>
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-2 gap-2 sm:gap-3 lg:grid-cols-4">
            {METRICAS.map((m) => {
              const serie = itens.map((c) => c[m.k]);
              const vals = serie.filter((v): v is number => v != null);
              const delta = vals.length > 1 ? vals[vals.length - 1] - vals[vals.length - 2] : null;
              return (
                <Card key={m.k} className="flex flex-col p-4">
                  <p className="text-[13px] text-muted">{m.l}</p>
                  <p className="num mt-1 text-[24px] font-semibold leading-none">
                    {vals.length ? m.fmt(vals[vals.length - 1]) : '—'}
                    {vals.length > 0 && <span className="ml-0.5 text-[13px] font-normal text-muted">{m.u.trim()}</span>}
                  </p>
                  {delta != null && delta !== 0 && (
                    <p className="num mt-1.5 flex items-center gap-1 text-[12px] font-semibold text-muted">
                      {delta > 0 ? <TrendingUp className="h-3.5 w-3.5" /> : <TrendingDown className="h-3.5 w-3.5" />}
                      {delta > 0 ? '+' : ''}
                      {m.fmt(delta)} desde o anterior
                    </p>
                  )}
                  {vals.length > 1 && <Sparkline points={serie} min={m.min} max={m.max} height={34} className="mt-auto pt-2" />}
                </Card>
              );
            })}
          </div>
          <Card className="mt-3 divide-y divide-line">
            {[...itens].reverse().map((c) => (
              <div key={c.id} className="flex items-start gap-3 p-4">
                <div className="min-w-0 flex-1">
                  <p className="font-semibold">
                    {dataCurta(c.dataUtc)}
                    {c.origem && <span className="ml-2 text-xs font-normal text-faint">{c.origem === 'Paciente' ? 'pelo paciente' : 'pela equipe'}</span>}
                  </p>
                  <p className="num mt-0.5 text-sm text-muted">
                    {[
                      c.pesoKg != null && `${n1(c.pesoKg)} kg`,
                      c.adesaoAlimentacaoPercentual != null && `dieta ${c.adesaoAlimentacaoPercentual}%`,
                      c.adesaoTreinoPercentual != null && `treino ${c.adesaoTreinoPercentual}%`,
                      c.fomeNivel != null && `fome ${c.fomeNivel}`,
                      c.energiaNivel != null && `energia ${c.energiaNivel}`,
                      c.sonoNivel != null && `sono ${c.sonoNivel}`,
                    ]
                      .filter(Boolean)
                      .join(' · ') || 'Sem números'}
                  </p>
                  {c.observacoes && <p className="mt-1 whitespace-pre-line text-sm">{c.observacoes}</p>}
                  {(c.faseNutricionalNome || c.faseTreinoNome) && (
                    <div className="mt-1.5 flex flex-wrap gap-1.5">
                      {[c.faseNutricionalNome, c.faseTreinoNome].filter(Boolean).map((f) => (
                        <span key={f} className="rounded-full bg-raised px-2 py-0.5 text-xs text-muted">
                          {f}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
                {onEditar && (
                  <button onClick={() => onEditar(c)} aria-label="Editar check-in" className="grid h-9 w-9 shrink-0 place-items-center rounded-full text-muted transition hover:bg-raised hover:text-fg">
                    <Pencil className="h-4 w-4" />
                  </button>
                )}
              </div>
            ))}
          </Card>
        </>
      )}
    </section>
  );
}

const NIVEIS = [
  { k: 'fomeNivel', l: 'Fome' },
  { k: 'energiaNivel', l: 'Energia' },
  { k: 'sonoNivel', l: 'Qualidade do sono' },
  { k: 'percepcaoEvolucaoNivel', l: 'Percepção de evolução' },
] as const;

type Form = Record<'pesoKg' | 'adesaoAlimentacaoPercentual' | 'adesaoTreinoPercentual' | 'fomeNivel' | 'energiaNivel' | 'sonoNivel' | 'percepcaoEvolucaoNivel' | 'observacoes' | 'data' | 'faseNutricionalId' | 'faseTreinoId', string>;

const local = (iso: string) => {
  const d = new Date(iso);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
};

const vazio = (): Form => ({
  pesoKg: '',
  adesaoAlimentacaoPercentual: '',
  adesaoTreinoPercentual: '',
  fomeNivel: '',
  energiaNivel: '',
  sonoNivel: '',
  percepcaoEvolucaoNivel: '',
  observacoes: '',
  data: local(new Date().toISOString()),
  faseNutricionalId: '',
  faseTreinoId: '',
});

const doCheckIn = (c: CheckIn): Form => {
  const s = (v: number | null) => (v == null ? '' : String(v));
  return {
    pesoKg: s(c.pesoKg),
    adesaoAlimentacaoPercentual: s(c.adesaoAlimentacaoPercentual),
    adesaoTreinoPercentual: s(c.adesaoTreinoPercentual),
    fomeNivel: s(c.fomeNivel),
    energiaNivel: s(c.energiaNivel),
    sonoNivel: s(c.sonoNivel),
    percepcaoEvolucaoNivel: s(c.percepcaoEvolucaoNivel),
    observacoes: c.observacoes ?? '',
    data: local(c.dataUtc),
    faseNutricionalId: c.faseNutricionalId ?? '',
    faseTreinoId: c.faseTreinoId ?? '',
  };
};

const num = (v: string) => (v.trim() === '' ? null : Number(v.replace(',', '.')));

/** Escala de 0 a 10 em botoes; tocar de novo no valor marcado limpa. */
function Escala({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) {
  return (
    <Field label={label} group>
      <div className="grid grid-cols-11 gap-1">
        {Array.from({ length: 11 }, (_, i) => String(i)).map((v) => (
          <button
            key={v}
            type="button"
            aria-pressed={value === v}
            onClick={() => onChange(value === v ? '' : v)}
            className={clsx('num h-9 rounded-lg text-sm font-semibold transition', value === v ? 'bg-accent text-accent-ink' : 'bg-raised text-muted hover:text-fg')}
          >
            {v}
          </button>
        ))}
      </div>
    </Field>
  );
}

/** Formulario do check-in. Para a equipe (paciente informado) inclui data e fases. */
export function CheckInForm({ paciente, editando, onSalvo, onCancelar }: { paciente: string | null; editando?: CheckIn | null; onSalvo: () => void; onCancelar?: () => void }) {
  const qc = useQueryClient();
  const equipe = !!paciente;
  const [f, setF] = useState<Form>(() => (editando ? doCheckIn(editando) : vazio()));
  useEffect(() => setF(editando ? doCheckIn(editando) : vazio()), [editando]);
  const set = (k: keyof Form) => (v: string) => setF((x) => ({ ...x, [k]: v }));

  const fasesN = useQuery({ queryKey: ['pro', 'fases-nutricionais', paciente], queryFn: () => api<Fase[]>(`/api/pacientes/${paciente}/fases-nutricionais`), enabled: equipe });
  const fasesT = useQuery({ queryKey: ['pro', 'fases-treino', paciente], queryFn: () => api<Fase[]>(`/api/pacientes/${paciente}/fases-treino`), enabled: equipe });

  const salvar = useMutation({
    mutationFn: () => {
      const corpo = {
        pesoKg: num(f.pesoKg),
        adesaoAlimentacaoPercentual: num(f.adesaoAlimentacaoPercentual),
        adesaoTreinoPercentual: num(f.adesaoTreinoPercentual),
        fomeNivel: num(f.fomeNivel),
        energiaNivel: num(f.energiaNivel),
        sonoNivel: num(f.sonoNivel),
        percepcaoEvolucaoNivel: num(f.percepcaoEvolucaoNivel),
        observacoes: f.observacoes.trim() || null,
      };
      if (!equipe) return api('/api/portal/me/check-ins', { method: 'POST', json: corpo });
      const completo = { ...corpo, dataUtc: new Date(f.data).toISOString(), faseNutricionalId: f.faseNutricionalId || null, faseTreinoId: f.faseTreinoId || null };
      return editando ? api(`/api/check-ins/${editando.id}`, { method: 'PUT', json: completo }) : api(rota(paciente), { method: 'POST', json: completo });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chave(paciente) });
      toast(editando ? 'Check-in atualizado.' : equipe ? 'Check-in registrado.' : 'Check-in enviado. Seu profissional vai ver.');
      onSalvo();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });

  const vazioTudo = ['pesoKg', 'adesaoAlimentacaoPercentual', 'adesaoTreinoPercentual', 'fomeNivel', 'energiaNivel', 'sonoNivel', 'percepcaoEvolucaoNivel', 'observacoes'].every((k) => !f[k as keyof Form].trim());

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(e) => {
        e.preventDefault();
        salvar.mutate();
      }}
    >
      {equipe && (
        <Field label="Data e hora">
          <input type="datetime-local" required className={inputCls} value={f.data} onChange={(e) => set('data')(e.target.value)} />
        </Field>
      )}
      <Field label={equipe ? 'Peso (kg)' : 'Peso atual (kg)'}>
        <input inputMode="decimal" className={inputCls} placeholder="Ex.: 72,4" value={f.pesoKg} onChange={(e) => set('pesoKg')(e.target.value)} />
      </Field>
      <div className="grid grid-cols-2 gap-3">
        <Field label="Seguiu a dieta (%)">
          <input type="number" min={0} max={100} inputMode="numeric" className={inputCls} placeholder="0 a 100" value={f.adesaoAlimentacaoPercentual} onChange={(e) => set('adesaoAlimentacaoPercentual')(e.target.value)} />
        </Field>
        <Field label="Seguiu o treino (%)">
          <input type="number" min={0} max={100} inputMode="numeric" className={inputCls} placeholder="0 a 100" value={f.adesaoTreinoPercentual} onChange={(e) => set('adesaoTreinoPercentual')(e.target.value)} />
        </Field>
      </div>
      {NIVEIS.map((n) => (
        <Escala key={n.k} label={`${n.l} (0 a 10)`} value={f[n.k]} onChange={set(n.k)} />
      ))}
      {equipe && (
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Fase nutricional">
            <select className={inputCls} value={f.faseNutricionalId} onChange={(e) => set('faseNutricionalId')(e.target.value)}>
              <option value="">Sem vínculo</option>
              {(fasesN.data ?? []).map((x) => (
                <option key={x.id} value={x.id}>
                  {x.nome}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Fase de treino">
            <select className={inputCls} value={f.faseTreinoId} onChange={(e) => set('faseTreinoId')(e.target.value)}>
              <option value="">Sem vínculo</option>
              {(fasesT.data ?? []).map((x) => (
                <option key={x.id} value={x.id}>
                  {x.nome}
                </option>
              ))}
            </select>
          </Field>
        </div>
      )}
      <Field label="Observações" hint={equipe ? undefined : 'Fica ligado automaticamente às fases atuais do seu plano.'}>
        <textarea
          rows={3}
          className={clsx(inputCls, 'h-auto py-2.5')}
          placeholder={equipe ? 'Fome, disposição, dificuldades, resposta ao plano...' : 'Como foi sua semana, dificuldades, fome, disposição...'}
          value={f.observacoes}
          onChange={(e) => set('observacoes')(e.target.value)}
        />
      </Field>
      <div className="flex gap-2">
        {onCancelar && (
          <Button type="button" variant="ghost" size="lg" onClick={onCancelar}>
            Cancelar
          </Button>
        )}
        <Button type="submit" size="lg" className="flex-1" loading={salvar.isPending} disabled={vazioTudo}>
          {editando ? 'Salvar alterações' : equipe ? 'Registrar check-in' : 'Enviar check-in'}
        </Button>
      </div>
    </form>
  );
}

/** Painel + formulario em sheet, pronto para a tela de progresso e o prontuario. */
export function CheckInsSecao({ paciente, titulo }: { paciente: string | null; titulo?: string }) {
  const [aberto, setAberto] = useState(false);
  const [editando, setEditando] = useState<CheckIn | null>(null);
  const fechar = () => {
    setAberto(false);
    setEditando(null);
  };
  return (
    <>
      <CheckInsPainel
        paciente={paciente}
        titulo={titulo}
        onNovo={() => setAberto(true)}
        onEditar={
          paciente
            ? (c) => {
                setEditando(c);
                setAberto(true);
              }
            : undefined
        }
      />
      <Sheet open={aberto} onClose={fechar} title={editando ? 'Editar check-in' : paciente ? 'Registrar check-in' : 'Como você está respondendo?'}>
        {!paciente && <p className="-mt-1 mb-4 text-sm text-muted">Essas informações ajudam seu profissional a acompanhar a fase atual do plano.</p>}
        <CheckInForm paciente={paciente} editando={editando} onSalvo={fechar} />
      </Sheet>
    </>
  );
}
