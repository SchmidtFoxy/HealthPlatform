import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ArrowRight } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Field, inputCls, Segmented, Sheet, Stepper, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { hojeIso, n0, n1 } from '@/lib/format';

// Duplicar um plano como nova versao, ja com ajuste: dieta por % ou calorias alvo,
// treino por carga, series, repeticoes e descanso. A previa vem do servidor antes de criar.

interface Totais {
  calorias: number;
  proteinasG: number;
  carboidratosG: number;
  gordurasG: number;
}

type ModoDieta = 'igual' | 'percentual' | 'calorias';

export function NovaVersaoSheet({ tipo, pacienteId, plano, onClose }: { tipo: 'dieta' | 'treino'; pacienteId: string; plano: { id: string; nome: string } | null; onClose: () => void }) {
  const qc = useQueryClient();
  const nav = useNavigate();
  const [nome, setNome] = useState('');
  const [inicio, setInicio] = useState(hojeIso());
  const [concluir, setConcluir] = useState(false);
  const [modo, setModo] = useState<ModoDieta>('igual');
  const [pct, setPct] = useState(-10);
  const [kcal, setKcal] = useState<number | null>(null);
  const [carga, setCarga] = useState(0);
  const [series, setSeries] = useState(0);
  const [reps, setReps] = useState(0);
  const [descanso, setDescanso] = useState(0);
  useEffect(() => {
    if (!plano) return;
    setNome(`${plano.nome} (nova versão)`);
    setInicio(hojeIso());
    setConcluir(false);
    setModo('igual');
    setPct(-10);
    setKcal(null);
    setCarga(0);
    setSeries(0);
    setReps(0);
    setDescanso(0);
  }, [plano]);

  const qsDieta = modo === 'percentual' ? `percentual=${pct}` : modo === 'calorias' && kcal ? `caloriasAlvo=${kcal}` : null;
  const simDieta = useQuery({
    queryKey: ['pro', 'simular-ajuste', plano?.id, qsDieta],
    queryFn: () => api<{ totaisAtuais: Totais; totaisProjetados: Totais; itensAfetados: number }>(`/api/planos-alimentares/${plano!.id}/simular-ajuste?${qsDieta}`),
    enabled: !!plano && tipo === 'dieta' && !!qsDieta,
    retry: false,
  });
  const mudaTreino = carga !== 0 || series !== 0 || reps !== 0 || descanso !== 0;
  const simTreino = useQuery({
    queryKey: ['pro', 'simular-progressao', plano?.id, carga, series, reps, descanso],
    queryFn: () =>
      api<{ exercicios: number; exerciciosComCarga: number; prescricoesRepeticoesAjustadas: number; prescricoesRepeticoesPreservadas: number; somaCargasAtual: number; somaCargasProjetada: number }>(
        `/api/treinos/${plano!.id}/simular-progressao?cargaPercentual=${carga}&seriesDelta=${series}&repeticoesDelta=${reps}&descansoDeltaSegundos=${descanso}`,
      ),
    enabled: !!plano && tipo === 'treino' && mudaTreino,
    retry: false,
  });

  const criar = useMutation({
    mutationFn: () =>
      tipo === 'dieta'
        ? api<{ id: string }>(`/api/planos-alimentares/${plano!.id}/duplicar`, {
            method: 'POST',
            json: {
              nome: nome.trim(),
              dataInicio: inicio,
              ajustePercentual: modo === 'percentual' ? pct : null,
              caloriasAlvo: modo === 'calorias' ? kcal : null,
              concluirPlanoAnterior: concluir,
            },
          })
        : api<{ id: string }>(`/api/treinos/${plano!.id}/duplicar`, {
            method: 'POST',
            json: {
              nome: nome.trim(),
              dataInicio: inicio,
              ajusteCargaPercentual: carga,
              ajusteSeries: series,
              ajusteRepeticoes: reps,
              ajusteDescansoSegundos: descanso,
              concluirPlanoAnterior: concluir,
            },
          }),
    onSuccess: (novo) => {
      qc.invalidateQueries({ queryKey: ['pro', tipo === 'dieta' ? 'dietas' : 'treinos', pacienteId] });
      onClose();
      nav(`/pro/pacientes/${pacienteId}/${tipo}/${novo.id}`);
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível criar.', 'bad'),
  });

  const erroPrevia = (tipo === 'dieta' ? simDieta.error : simTreino.error) as Error | null;
  return (
    <Sheet
      open={!!plano}
      onClose={onClose}
      title="Duplicar como nova versão"
      footer={
        <Button size="lg" className="w-full" disabled={!nome.trim() || (tipo === 'dieta' && modo === 'calorias' && !kcal) || !!erroPrevia} loading={criar.isPending} onClick={() => criar.mutate()}>
          Criar e abrir
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome">
          <input className={inputCls} value={nome} maxLength={160} onChange={(e) => setNome(e.target.value)} />
        </Field>
        <Field label="Começa em">
          <input type="date" className={inputCls} value={inicio} onChange={(e) => setInicio(e.target.value)} />
        </Field>

        {tipo === 'dieta' ? (
          <>
            <Field label="Ajuste" group>
              <Segmented<ModoDieta>
                compact
                value={modo}
                onChange={setModo}
                options={[
                  { value: 'igual', label: 'Igual' },
                  { value: 'percentual', label: 'Em %' },
                  { value: 'calorias', label: 'Calorias alvo' },
                ]}
              />
            </Field>
            {modo === 'percentual' && (
              <div className="flex items-center justify-between gap-3">
                <p className="text-[14px] text-muted">Todas as quantidades mudam</p>
                <Stepper value={pct} onChange={(v) => setPct(v)} step={5} min={-50} max={100} suffix="%" />
              </div>
            )}
            {modo === 'calorias' && (
              <Field label="Calorias do dia na nova versão">
                <input type="number" inputMode="numeric" className={inputCls} value={kcal ?? ''} placeholder="Ex.: 1800" onChange={(e) => setKcal(e.target.value ? Number(e.target.value) : null)} />
              </Field>
            )}
            {simDieta.data && qsDieta && (
              <div className="rounded-xl bg-raised/60 p-3 text-[14px]">
                {(
                  [
                    ['Calorias', 'calorias', ' kcal'],
                    ['Proteína', 'proteinasG', 'g'],
                    ['Carboidrato', 'carboidratosG', 'g'],
                    ['Gordura', 'gordurasG', 'g'],
                  ] as const
                ).map(([l, k, u]) => (
                  <p key={k} className="num flex items-center justify-between gap-2 py-0.5">
                    <span className="text-muted">{l}</span>
                    <span className="flex items-center gap-1.5">
                      {n0(simDieta.data.totaisAtuais[k])}
                      <ArrowRight className="h-3.5 w-3.5 text-faint" />
                      <b>
                        {n0(simDieta.data.totaisProjetados[k])}
                        {u}
                      </b>
                    </span>
                  </p>
                ))}
                <p className="mt-1 text-[12px] text-faint">{simDieta.data.itensAfetados} itens com a quantidade ajustada.</p>
              </div>
            )}
          </>
        ) : (
          <>
            <div className="flex flex-col gap-2">
              {(
                [
                  ['Carga', carga, setCarga, 5, -50, 100, '%'],
                  ['Séries', series, setSeries, 1, -5, 10, ''],
                  ['Repetições', reps, setReps, 1, -20, 30, ''],
                  ['Descanso', descanso, setDescanso, 15, -300, 300, 's'],
                ] as const
              ).map(([l, v, set, step, min, max, suf]) => (
                <div key={l} className="flex items-center justify-between gap-3">
                  <p className="font-semibold">{l}</p>
                  <Stepper value={v} onChange={(x) => set(x || 0)} step={step} min={min} max={max} suffix={suf} />
                </div>
              ))}
              <p className="text-[12px] text-faint">Valores somados ao que está em cada exercício. Zero mantém igual.</p>
            </div>
            {simTreino.data && mudaTreino && (
              <div className="rounded-xl bg-raised/60 p-3 text-[14px]">
                {carga !== 0 && (
                  <p className="num flex items-center justify-between gap-2">
                    <span className="text-muted">Soma das cargas</span>
                    <span className="flex items-center gap-1.5">
                      {n1(simTreino.data.somaCargasAtual)}
                      <ArrowRight className="h-3.5 w-3.5 text-faint" />
                      <b>{n1(simTreino.data.somaCargasProjetada)}</b>
                    </span>
                  </p>
                )}
                <p className="text-muted">
                  {simTreino.data.exercicios} exercícios, {simTreino.data.exerciciosComCarga} com carga.
                  {reps !== 0 && ` ${simTreino.data.prescricoesRepeticoesAjustadas} repetições ajustadas, ${simTreino.data.prescricoesRepeticoesPreservadas} mantidas (texto livre).`}
                </p>
              </div>
            )}
          </>
        )}
        {erroPrevia && <p className="text-[13px] text-bad">{erroPrevia.message}</p>}

        <label className={clsx('flex items-center justify-between gap-3 rounded-xl bg-raised/60 p-3 text-[14px]')}>
          <span>
            Concluir o plano atual
            <span className="block text-[12px] text-muted">A nova versão passa a valer e a atual vai para o histórico.</span>
          </span>
          <input type="checkbox" checked={concluir} onChange={(e) => setConcluir(e.target.checked)} className="h-5 w-5 accent-[rgb(var(--accent))]" />
        </label>
      </div>
    </Sheet>
  );
}
