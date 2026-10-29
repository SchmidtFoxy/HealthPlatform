// Calculos de planejamento energetico do editor de dieta (mesmas formulas da versao classica, v0.19.7–v0.19.24).

export const FATORES_ATIVIDADE = {
  sedentario: { label: 'Sedentário', fator: 1.2, ajuda: 'Pouca atividade física estruturada.' },
  leve: { label: 'Levemente ativo', fator: 1.375, ajuda: 'Atividade leve em parte da semana.' },
  moderado: { label: 'Moderadamente ativo', fator: 1.55, ajuda: 'Treinos regulares e rotina ativa.' },
  alto: { label: 'Muito ativo', fator: 1.725, ajuda: 'Treino intenso/frequente ou rotina fisicamente exigente.' },
  extremo: { label: 'Extremamente ativo', fator: 1.9, ajuda: 'Volume muito alto de treino e/ou trabalho físico intenso.' },
} as const;
export type NivelAtividade = keyof typeof FATORES_ATIVIDADE;

export type Formula = 'mifflin' | 'katch';
export type Sexo = 'masculino' | 'feminino' | '';

/** ~7.700 kcal por kg de peso corporal. */
export const KCAL_POR_KG = 7700;

export const FAIXAS_MACROS = { p: { min: 2, max: 3 }, g: { min: 0.8, max: 1.2 }, c: { min: 3, max: 7 } };
const KCAL_G = { p: 4, c: 4, g: 9 } as const;
export type Macro = keyof typeof KCAL_G;

export function idadeDe(dataNascimento: string | null | undefined): number | null {
  if (!dataNascimento) return null;
  const nasc = new Date(`${dataNascimento.slice(0, 10)}T12:00:00`);
  if (Number.isNaN(nasc.getTime())) return null;
  const hoje = new Date();
  let idade = hoje.getFullYear() - nasc.getFullYear();
  const m = hoje.getMonth() - nasc.getMonth();
  if (m < 0 || (m === 0 && hoje.getDate() < nasc.getDate())) idade--;
  return idade > 0 ? idade : null;
}

export function normalizarSexo(v: string | null | undefined): Sexo {
  const s = (v ?? '').trim().toLowerCase();
  if (/^m|masc|male|homem/.test(s)) return 'masculino';
  if (/^f|fem|female|mulher/.test(s)) return 'feminino';
  return '';
}

/** TMB por Mifflin-St Jeor ou, com massa magra, Katch-McArdle. Null quando faltam dados. */
export function calcularTmb(x: { formula: Formula; peso: number; alturaCm: number; idade: number; sexo: Sexo; massaMagra: number }): number | null {
  if (x.formula === 'katch') return x.massaMagra > 0 ? 370 + 21.6 * x.massaMagra : null;
  if (!(x.peso > 0 && x.alturaCm > 0 && x.idade > 0 && x.sexo)) return null;
  return 10 * x.peso + 6.25 * x.alturaCm - 5 * x.idade + (x.sexo === 'masculino' ? 5 : -161);
}

export type ObjetivoPeso = 'perder' | 'manter' | 'ganhar';
export type ResultadoObjetivo =
  | { ok: true; ajuste: number; alvo: number; ritmoSemana: number; ritmoPct: number; avisos: string[]; abaixoTmb: boolean; agressivo: boolean }
  | { ok: false; motivo: string };

/** Alvo calorico diario para sair do peso atual e chegar ao desejado no prazo, a partir do GET. */
export function calcularObjetivo(x: { get: number | null; tmb: number | null; atual: number; desejado: number; semanas: number; tipo: ObjetivoPeso }): ResultadoObjetivo {
  if (!x.get) return { ok: false, motivo: 'Calcule primeiro o GET.' };
  if (!(x.atual > 0) || !(x.semanas > 0)) return { ok: false, motivo: 'Informe peso atual e prazo.' };
  if (x.tipo === 'manter') return { ok: true, ajuste: 0, alvo: x.get, ritmoSemana: 0, ritmoPct: 0, avisos: [], abaixoTmb: false, agressivo: false };
  if (!(x.desejado > 0)) return { ok: false, motivo: 'Informe o peso desejado.' };
  const delta = x.desejado - x.atual;
  if (x.tipo === 'perder' && delta >= 0) return { ok: false, motivo: 'Para perder, o peso desejado precisa ser menor que o atual.' };
  if (x.tipo === 'ganhar' && delta <= 0) return { ok: false, motivo: 'Para ganhar, o peso desejado precisa ser maior que o atual.' };
  const ajuste = (delta * KCAL_POR_KG) / (x.semanas * 7);
  const alvo = x.get + ajuste;
  const ritmoSemana = delta / x.semanas;
  const ritmoPct = Math.abs(ritmoSemana / x.atual) * 100;
  const abaixoTmb = !!x.tmb && alvo < x.tmb;
  const agressivo = ritmoPct > 1;
  const avisos: string[] = [];
  if (agressivo) avisos.push(`Ritmo de ${ritmoPct.toFixed(1).replace('.', ',')}% do peso por semana: revise a agressividade da meta.`);
  if (abaixoTmb) avisos.push('O alvo ficou abaixo da TMB estimada e precisa de revisão profissional.');
  if (alvo < 1200) avisos.push('Alvo energético muito baixo. Revise prazo e objetivo.');
  return { ok: true, ajuste, alvo, ritmoSemana, ritmoPct, avisos, abaixoTmb, agressivo };
}

export type GPorKg = Record<Macro, number>;

/** Gramas por dia e energia total a partir de g/kg. */
export function macrosPorPeso(peso: number, gkg: GPorKg) {
  const g = { p: peso * Math.max(0, gkg.p), c: peso * Math.max(0, gkg.c), g: peso * Math.max(0, gkg.g) };
  return { ...g, kcal: g.p * 4 + g.c * 4 + g.g * 9 };
}

/** g/kg do macro residual que faz a energia dos macros bater com a meta, mantendo os outros dois. Null se impossivel. */
export function gkgResidual(peso: number, gkg: GPorKg, metaKcal: number, residual: Macro): number | null {
  if (!(peso > 0) || !(metaKcal > 0)) return null;
  const fixos = (Object.keys(KCAL_G) as Macro[]).filter((m) => m !== residual).reduce((s, m) => s + peso * Math.max(0, gkg[m]) * KCAL_G[m], 0);
  const resto = metaKcal - fixos;
  if (resto < 0) return null;
  return resto / KCAL_G[residual] / peso;
}
