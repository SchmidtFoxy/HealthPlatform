import type { Alimento } from './types';

// Medidas caseiras: prescrever em gramas, em unidade (ex.: 2 paes) ou em ml.

export type Modo = 'g' | 'un' | 'ml';

export const modoDe = (unidade: string): Modo => (unidade === 'g' || !unidade ? 'g' : unidade === 'ml' ? 'ml' : 'un');

/** Plural da primeira palavra: unidade -> unidades, pao -> paes, colher de sopa -> colheres de sopa. */
export function pluralizar(nome: string, qtd: number) {
  if (qtd <= 1 || !nome) return nome;
  const [p, ...resto] = nome.split(' ');
  const irregulares: Record<string, string> = { pão: 'pães', mão: 'mãos', pao: 'paes' };
  const w = p.toLowerCase();
  let pl: string;
  if (irregulares[w]) pl = irregulares[w];
  else if (/[sx]$/.test(w)) pl = p;
  else if (/ão$/.test(w)) pl = p.slice(0, -2) + 'ões';
  else if (/[aeiouáéíóúâêô]$/.test(w)) pl = p + 's';
  else if (/[rz]$/.test(w)) pl = p + 'es';
  else if (/m$/.test(w)) pl = p.slice(0, -1) + 'ns';
  else if (/l$/.test(w)) pl = p.slice(0, -1) + 'is';
  else pl = p + 's';
  return [pl, ...resto].join(' ');
}

/** Corpo do PUT /api/alimentos/{id} com a medida caseira trocada. */
export const corpoAlimento = (a: Alimento, medida: Partial<Pick<Alimento, 'unidadeNome' | 'gramasPorUnidade' | 'gramasPorMl'>>) => ({
  nome: a.nome,
  categoria: a.categoria,
  caloriasPor100g: a.caloriasPor100g,
  proteinasPor100g: a.proteinasPor100g,
  carboidratosPor100g: a.carboidratosPor100g,
  gordurasPor100g: a.gordurasPor100g,
  fibrasPor100g: a.fibrasPor100g,
  unidadeNome: a.unidadeNome ?? null,
  gramasPorUnidade: a.gramasPorUnidade ?? null,
  gramasPorMl: a.gramasPorMl ?? null,
  ...medida,
});
