import { useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { useEffect, useMemo, useState } from 'react';
import { Button, Field, inputCls, Segmented, Sheet } from '@/components/ui';
import { api } from '@/lib/api';
import {
  calcularObjetivo,
  calcularTmb,
  FAIXAS_MACROS,
  FATORES_ATIVIDADE,
  type Formula,
  gkgResidual,
  idadeDe,
  type Macro,
  macrosPorPeso,
  type NivelAtividade,
  normalizarSexo,
  type ObjetivoPeso,
  type Sexo,
} from '@/lib/energia';
import { n0, n1 } from '@/lib/format';
import { usePaciente } from './queries';

// Planejamento energetico do editor de dieta: TMB -> GET -> objetivo de peso -> meta calorica -> macros em g/kg.
// Traz de volta a calculadora da versao classica. Os valores sao estimativas e o profissional aplica se quiser.

interface UltimaAvaliacao {
  pesoKg: number | null;
  alturaM: number | null;
  massaMagraKg: number | null;
}

interface Estado {
  peso: string;
  alturaCm: string;
  idade: string;
  sexo: Sexo;
  formula: Formula;
  atividade: NivelAtividade;
  objetivo: ObjetivoPeso;
  desejado: string;
  semanas: string;
  pesoRef: string;
  gkg: Record<Macro, string>;
}

const chave = (pacienteId: string) => `aesyn:calculo-metas:${pacienteId}`;
const ler = (pacienteId: string): Partial<Estado> | null => {
  try {
    const s = localStorage.getItem(chave(pacienteId));
    return s ? (JSON.parse(s) as Partial<Estado>) : null;
  } catch {
    return null;
  }
};
const gravar = (pacienteId: string, e: Estado) => {
  try {
    localStorage.setItem(chave(pacienteId), JSON.stringify(e));
  } catch {
    /* sem armazenamento: so nao lembra na proxima vez */
  }
};

const num = (s: string) => {
  const n = Number(String(s).replace(',', '.'));
  return Number.isFinite(n) ? n : 0;
};
const txt = (v: number | null | undefined, casas = 1) => (v == null ? '' : String(Math.round(v * 10 ** casas) / 10 ** casas));

function Num({ label, value, onChange, suffix, hint, placeholder }: { label: string; value: string; onChange: (v: string) => void; suffix?: string; hint?: string; placeholder?: string }) {
  return (
    <Field label={label} hint={hint}>
      <div className="relative">
        <input className={inputCls + (suffix ? ' pr-12' : '')} inputMode="decimal" placeholder={placeholder} value={value} onChange={(e) => onChange(e.target.value)} />
        {suffix && <span className="pointer-events-none absolute right-3.5 top-1/2 -translate-y-1/2 text-[13px] text-muted">{suffix}</span>}
      </div>
    </Field>
  );
}

function Resultado({ itens }: { itens: [string, string, string, string?][] }) {
  return (
    <div className={clsx('mt-3 grid gap-2 text-center', itens.length === 4 ? 'grid-cols-2 sm:grid-cols-4' : 'grid-cols-3')}>
      {itens.map(([label, valor, unidade, cor]) => (
        <div key={label} className="rounded-xl bg-raised/70 px-1 py-2">
          <p className="text-[11px] text-muted">{label}</p>
          <p className={clsx('num text-lg font-semibold leading-tight', cor)}>{valor}</p>
          <p className="text-[11px] text-faint">{unidade}</p>
        </div>
      ))}
    </div>
  );
}

function Secao({ passo, titulo, sub, children }: { passo: number; titulo: string; sub: string; children: React.ReactNode }) {
  return (
    <section className="border-t border-line/60 pt-4 first:border-t-0 first:pt-0">
      <div className="mb-3 flex items-start gap-3">
        <span className="num grid h-6 w-6 shrink-0 place-items-center rounded-full bg-accent/15 text-[12px] font-semibold text-accent">{passo}</span>
        <div>
          <p className="font-display font-semibold">{titulo}</p>
          <p className="text-[13px] text-muted">{sub}</p>
        </div>
      </div>
      {children}
    </section>
  );
}

export interface MetasCalculadas {
  calorias: number | null;
  proteinasG: number | null;
  carboidratosG: number | null;
  gordurasG: number | null;
}

export function CalculoMetasSheet({ open, onClose, pacienteId, onAplicar }: { open: boolean; onClose: () => void; pacienteId: string; onAplicar: (m: MetasCalculadas) => void }) {
  const paciente = usePaciente(pacienteId);
  const avaliacoes = useQuery({
    queryKey: ['pro', 'pront', 'avaliacoes', pacienteId],
    queryFn: () => api<UltimaAvaliacao[]>(`/api/pacientes/${pacienteId}/avaliacoes`),
    enabled: open && !!pacienteId,
  });
  const ultima = avaliacoes.data?.[0] ?? null;
  const massaMagra = ultima?.massaMagraKg ?? 0;
  const [e, setE] = useState<Estado | null>(null);

  // Preenche ao abrir: o que o profissional usou da ultima vez para este paciente; senao, ficha e ultima avaliacao.
  useEffect(() => {
    if (!open || e || paciente.isPending || avaliacoes.isPending) return;
    const salvo = ler(pacienteId);
    const peso = txt(ultima?.pesoKg);
    const base: Estado = {
      peso,
      alturaCm: txt(ultima?.alturaM != null ? ultima.alturaM * 100 : null, 0),
      idade: txt(idadeDe(paciente.data?.dataNascimento), 0),
      sexo: normalizarSexo(paciente.data?.sexo),
      formula: 'mifflin',
      atividade: 'moderado',
      objetivo: 'perder',
      desejado: '',
      semanas: '12',
      pesoRef: peso,
      gkg: { p: '2', g: '1', c: '4' },
    };
    setE(salvo ? { ...base, ...salvo, gkg: { ...base.gkg, ...salvo.gkg } } : base);
  }, [open, e, paciente.isPending, paciente.data, avaliacoes.isPending, ultima, pacienteId]);

  useEffect(() => {
    if (e) gravar(pacienteId, e);
  }, [e, pacienteId]);

  const set = <K extends keyof Estado>(k: K, v: Estado[K]) => setE((x) => (x ? { ...x, [k]: v } : x));

  const calc = useMemo(() => {
    if (!e) return null;
    const formula: Formula = e.formula === 'katch' && massaMagra > 0 ? 'katch' : 'mifflin';
    const tmb = calcularTmb({ formula, peso: num(e.peso), alturaCm: num(e.alturaCm), idade: num(e.idade), sexo: e.sexo, massaMagra });
    const fator = FATORES_ATIVIDADE[e.atividade].fator;
    const get = tmb ? tmb * fator : null;
    const obj = calcularObjetivo({ get, tmb, atual: num(e.peso), desejado: num(e.desejado), semanas: num(e.semanas), tipo: e.objetivo });
    const alvo = obj.ok ? Math.round(obj.alvo) : get ? Math.round(get) : null;
    const gkg = { p: num(e.gkg.p), g: num(e.gkg.g), c: num(e.gkg.c) };
    const pesoRef = num(e.pesoRef) || num(e.peso);
    const macros = pesoRef > 0 ? macrosPorPeso(pesoRef, gkg) : null;
    return { formula, tmb, fator, get, obj, alvo, gkg, pesoRef, macros };
  }, [e, massaMagra]);

  const fechar = () => {
    onClose();
    setE(null);
  };

  const aplicar = () => {
    if (!calc) return;
    const { obj, alvo, macros } = calc;
    if (obj.ok && (obj.abaixoTmb || obj.agressivo) && !confirm(`O alvo de ${n0(alvo)} kcal/dia está abaixo da TMB estimada e/ou num ritmo agressivo. Aplicar mesmo assim?`)) return;
    if (macros && alvo && Math.abs(macros.kcal - alvo) > 100 && !confirm(`Os macros somam ${n0(macros.kcal)} kcal e o alvo é ${n0(alvo)} kcal. Aplicar assim mesmo? (Dá para fechar a conta com um dos macros antes.)`)) return;
    onAplicar({
      calorias: alvo,
      proteinasG: macros ? Math.round(macros.p) : null,
      carboidratosG: macros ? Math.round(macros.c) : null,
      gordurasG: macros ? Math.round(macros.g) : null,
    });
    fechar();
  };

  const fecharCom = (m: Macro) => {
    if (!calc?.alvo || !e) return;
    const v = gkgResidual(calc.pesoRef, calc.gkg, calc.alvo, m);
    if (v == null) return alert('Os outros dois macros já passam da meta calórica. Reduza-os ou feche com outro macro.');
    set('gkg', { ...e.gkg, [m]: v.toFixed(2) });
  };

  const atividade = e ? FATORES_ATIVIDADE[e.atividade] : null;
  const diferenca = calc?.macros && calc.alvo ? calc.macros.kcal - calc.alvo : null;

  return (
    <Sheet
      open={open}
      onClose={fechar}
      title="Calcular metas"
      tall
      footer={
        <Button className="w-full" size="lg" disabled={!calc?.alvo && !calc?.macros} onClick={aplicar}>
          {calc?.alvo ? `Usar ${n0(calc.alvo)} kcal${calc.macros ? ' e os macros' : ''} nas metas` : 'Usar os macros nas metas'}
        </Button>
      }
    >
      {!e || !calc ? (
        <p className="py-10 text-center text-sm text-muted">Carregando dados do paciente…</p>
      ) : (
        <div className="flex flex-col gap-5 pb-2">
          <Secao passo={1} titulo="Gasto energético" sub={ultima ? 'Peso e altura da última avaliação, idade e sexo da ficha. Tudo editável.' : 'Sem avaliação registrada: preencha os dados.'}>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <Num label="Peso" suffix="kg" value={e.peso} onChange={(v) => setE({ ...e, peso: v, pesoRef: e.pesoRef === e.peso ? v : e.pesoRef })} placeholder="80" />
              <Num label="Altura" suffix="cm" value={e.alturaCm} onChange={(v) => set('alturaCm', v)} placeholder="175" />
              <Num label="Idade" suffix="anos" value={e.idade} onChange={(v) => set('idade', v)} placeholder="35" />
              <Field label="Sexo biológico" group>
                <Segmented
                  compact
                  options={[
                    { value: 'masculino', label: 'M' },
                    { value: 'feminino', label: 'F' },
                  ]}
                  value={e.sexo || ('' as 'masculino')}
                  onChange={(v) => set('sexo', v)}
                />
              </Field>
            </div>
            <div className="mt-3 grid gap-3 sm:grid-cols-2">
              <Field label="Fórmula da TMB" group>
                <Segmented
                  compact
                  options={[
                    { value: 'mifflin' as Formula, label: 'Mifflin-St Jeor' },
                    ...(massaMagra > 0 ? [{ value: 'katch' as const, label: `Katch-McArdle` }] : []),
                  ]}
                  value={calc.formula}
                  onChange={(v) => set('formula', v)}
                />
              </Field>
              <Field label="Nível de atividade" hint={atividade?.ajuda}>
                <select className={inputCls} value={e.atividade} onChange={(x) => set('atividade', x.target.value as NivelAtividade)}>
                  {Object.entries(FATORES_ATIVIDADE).map(([k, a]) => (
                    <option key={k} value={k}>
                      {a.label} × {String(a.fator).replace('.', ',')}
                    </option>
                  ))}
                </select>
              </Field>
            </div>
            {calc.formula === 'katch' && <p className="mt-2 text-[12px] text-muted">Usando a massa magra da última avaliação: {n1(massaMagra)} kg.</p>}
            <Resultado
              itens={[
                ['TMB (basal)', calc.tmb ? n0(calc.tmb) : '—', 'kcal/dia'],
                ['GET (com atividade)', calc.get ? n0(calc.get) : '—', 'kcal/dia', 'text-accent'],
                ['Fator', String(calc.fator).replace('.', ','), '× TMB'],
              ]}
            />
            {!calc.tmb && <p className="mt-2 text-[12px] text-warn">Preencha peso, altura, idade e sexo para calcular.</p>}
          </Secao>

          <Secao passo={2} titulo="Meta de peso" sub="Quantos kg ganhar ou perder e em quanto tempo. Vira o alvo calórico do dia.">
            <Segmented
              options={[
                { value: 'perder', label: 'Perder' },
                { value: 'manter', label: 'Manter' },
                { value: 'ganhar', label: 'Ganhar' },
              ]}
              value={e.objetivo}
              onChange={(v) => set('objetivo', v)}
            />
            {e.objetivo !== 'manter' && (
              <div className="mt-3 grid grid-cols-2 gap-3">
                <Num
                  label="Peso desejado"
                  suffix="kg"
                  value={e.desejado}
                  onChange={(v) => set('desejado', v)}
                  placeholder={num(e.peso) ? txt(num(e.peso) + (e.objetivo === 'perder' ? -5 : 3), 0) : '75'}
                  hint={num(e.peso) && num(e.desejado) ? `${num(e.desejado) > num(e.peso) ? '+' : ''}${n1(num(e.desejado) - num(e.peso))} kg` : undefined}
                />
                <Num label="Prazo" suffix="sem." value={e.semanas} onChange={(v) => set('semanas', v)} />
              </div>
            )}
            {calc.obj.ok ? (
              <>
                <Resultado
                  itens={[
                    ['Ajuste', `${calc.obj.ajuste > 0 ? '+' : ''}${n0(calc.obj.ajuste)}`, 'kcal/dia'],
                    ['Alvo do dia', n0(calc.obj.alvo), 'kcal/dia', 'text-accent'],
                    ['Ritmo', `${calc.obj.ritmoSemana > 0 ? '+' : ''}${calc.obj.ritmoSemana.toFixed(2).replace('.', ',')}`, 'kg/semana'],
                  ]}
                />
                <p className={clsx('mt-2 rounded-xl px-3 py-2 text-[13px]', calc.obj.avisos.length ? 'bg-warn/10 text-warn' : 'bg-ok/10 text-ok')}>
                  {calc.obj.avisos.length ? calc.obj.avisos.join(' ') : 'Dentro dos alertas básicos. Revise contexto clínico e adesão antes de aplicar.'}
                </p>
              </>
            ) : (
              <p className="mt-3 text-[13px] text-muted">{calc.obj.motivo}</p>
            )}
            <p className="mt-2 text-[12px] text-faint">Estimativa de ~7.700 kcal por kg. Não prevê adaptação metabólica, retenção hídrica ou mudança de composição corporal.</p>
          </Secao>

          <Secao passo={3} titulo="Macros por peso" sub="Gramas por kg viram as metas do dia. 4 kcal/g para proteína e carboidrato, 9 kcal/g para gordura.">
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <Num label="Peso de referência" suffix="kg" value={e.pesoRef} onChange={(v) => set('pesoRef', v)} />
              <Num label="Proteína" suffix="g/kg" value={e.gkg.p} onChange={(v) => set('gkg', { ...e.gkg, p: v })} hint={`Ref.: ${FAIXAS_MACROS.p.min}–${FAIXAS_MACROS.p.max}`} />
              <Num label="Gordura" suffix="g/kg" value={e.gkg.g} onChange={(v) => set('gkg', { ...e.gkg, g: v })} hint="Ref.: 0,8–1,2" />
              <Num label="Carboidrato" suffix="g/kg" value={e.gkg.c} onChange={(v) => set('gkg', { ...e.gkg, c: v })} hint={`Ref.: ${FAIXAS_MACROS.c.min}–${FAIXAS_MACROS.c.max}`} />
            </div>
            {calc.macros ? (
              <Resultado
                itens={[
                  ['Proteína', n0(calc.macros.p), 'g/dia', 'text-prot'],
                  ['Carboidrato', n0(calc.macros.c), 'g/dia', 'text-carb'],
                  ['Gordura', n0(calc.macros.g), 'g/dia', 'text-fat'],
                  ['Energia', n0(calc.macros.kcal), 'kcal/dia'],
                ]}
              />
            ) : (
              <p className="mt-3 text-[13px] text-muted">Informe o peso para calcular.</p>
            )}
            {diferenca != null && (
              <div className={clsx('mt-2 rounded-xl px-3 py-2 text-[13px]', Math.abs(diferenca) <= 100 ? 'bg-ok/10 text-ok' : 'bg-warn/10 text-warn')}>
                <p>
                  Os macros somam <b className="num">{n0(calc.macros!.kcal)} kcal</b>, {diferenca > 0 ? '+' : ''}
                  {n0(diferenca)} kcal do alvo de {n0(calc.alvo)}.
                </p>
                {Math.abs(diferenca) > 100 && (
                  <div className="mt-2 flex flex-wrap gap-2">
                    <span className="self-center text-[12px] text-muted">Fechar a conta com:</span>
                    {(
                      [
                        ['c', 'Carboidrato'],
                        ['g', 'Gordura'],
                        ['p', 'Proteína'],
                      ] as [Macro, string][]
                    ).map(([m, l]) => (
                      <Button key={m} size="sm" variant="soft" onClick={() => fecharCom(m)}>
                        {l}
                      </Button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </Secao>
          <p className="text-[12px] text-faint">Valores são estimativas de apoio. A decisão e a revisão continuam com o profissional.</p>
        </div>
      )}
    </Sheet>
  );
}
