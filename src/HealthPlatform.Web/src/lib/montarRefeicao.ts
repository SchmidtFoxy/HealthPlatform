import type { Alimento } from './types';

// "Montar pela meta": acha as gramas de cada alimento escolhido para a refeicao bater calorias e macros.
// E um calculo (minimos quadrados com quantidades >= 0), nao um modelo de linguagem: roda no aparelho e da sempre o mesmo resultado.

export interface MetaRefeicao {
  calorias: number | null;
  proteinasG: number | null;
  carboidratosG: number | null;
  gordurasG: number | null;
}

export interface Escolhido {
  alimento: Alimento;
  /** Gramas travadas pelo profissional (ex.: 2 g de canela). Sem valor, o calculo decide. */
  fixoG: number | null;
}

export interface Sugestao {
  alimentoId: string;
  quantidade: number;
  unidade: string;
  gramas: number;
}

type Macros = { kcal: number; p: number; c: number; g: number };

export const macrosPorGrama = (a: Alimento, gramas: number): Macros => ({
  kcal: (a.caloriasPor100g * gramas) / 100,
  p: (a.proteinasPor100g * gramas) / 100,
  c: (a.carboidratosPor100g * gramas) / 100,
  g: (a.gordurasPor100g * gramas) / 100,
});

/** Porcao de referencia: a medida caseira, ou 30 g para alimentos muito caloricos, ou 100 g. */
const porcaoRef = (a: Alimento) => a.gramasPorUnidade || (a.caloriasPor100g > 300 ? 30 : 100);

/** Resolve min ||Ax - b|| com x >= 0 (Lawson-Hanson). Para poucas variaveis, como aqui. */
export function nnls(A: number[][], b: number[]): number[] {
  const n = A[0]?.length ?? 0;
  const x = new Array<number>(n).fill(0);
  const passivo = new Array<boolean>(n).fill(false);
  const tol = 1e-10;
  const gradiente = (v: number[]) => {
    const r = b.map((bi, i) => bi - A[i].reduce((s, aij, j) => s + aij * v[j], 0));
    return Array.from({ length: n }, (_, j) => A.reduce((s, linha, i) => s + linha[j] * r[i], 0));
  };
  const resolverPassivo = () => {
    const idx = passivo.map((p, j) => (p ? j : -1)).filter((j) => j >= 0);
    const k = idx.length;
    // Equacoes normais com um ridge minimo para nao travar em colunas parecidas.
    const M = idx.map((j1) => idx.map((j2) => A.reduce((s, linha) => s + linha[j1] * linha[j2], 0) + (j1 === j2 ? 1e-9 : 0)));
    const v = idx.map((j) => A.reduce((s, linha, i) => s + linha[j] * b[i], 0));
    for (let c = 0; c < k; c++) {
      let piv = c;
      for (let r = c + 1; r < k; r++) if (Math.abs(M[r][c]) > Math.abs(M[piv][c])) piv = r;
      [M[c], M[piv]] = [M[piv], M[c]];
      [v[c], v[piv]] = [v[piv], v[c]];
      for (let r = c + 1; r < k; r++) {
        const f = M[r][c] / (M[c][c] || tol);
        for (let cc = c; cc < k; cc++) M[r][cc] -= f * M[c][cc];
        v[r] -= f * v[c];
      }
    }
    const sol = new Array<number>(k).fill(0);
    for (let r = k - 1; r >= 0; r--) sol[r] = (v[r] - M[r].slice(r + 1).reduce((s, mrc, i) => s + mrc * sol[r + 1 + i], 0)) / (M[r][r] || tol);
    const z = new Array<number>(n).fill(0);
    idx.forEach((j, i) => (z[j] = sol[i]));
    return z;
  };
  for (let iter = 0; iter < 3 * n + 10; iter++) {
    const w = gradiente(x);
    let j = -1;
    for (let jj = 0; jj < n; jj++) if (!passivo[jj] && w[jj] > tol && (j < 0 || w[jj] > w[j])) j = jj;
    if (j < 0) break;
    passivo[j] = true;
    for (let inner = 0; inner < 3 * n + 10; inner++) {
      const z = resolverPassivo();
      const negativos = z.map((zj, jj) => passivo[jj] && zj <= tol);
      if (!negativos.some(Boolean)) {
        z.forEach((zj, jj) => (x[jj] = zj));
        break;
      }
      let alfa = 1;
      negativos.forEach((neg, jj) => {
        if (neg && x[jj] - z[jj] > 0) alfa = Math.min(alfa, x[jj] / (x[jj] - z[jj]));
      });
      for (let jj = 0; jj < n; jj++) {
        x[jj] += alfa * (z[jj] - x[jj]);
        if (passivo[jj] && x[jj] <= tol) {
          x[jj] = 0;
          passivo[jj] = false;
        }
      }
    }
  }
  return x;
}

/** Arredonda para a medida caseira (meia em meia) ou para 5 g (1 g abaixo de 20 g). Zero tira o alimento. */
export function arredondar(a: Alimento, gramas: number): Sugestao | null {
  if (a.gramasPorUnidade) {
    const q = Math.round((gramas / a.gramasPorUnidade) * 2) / 2;
    if (q < 0.5) return null;
    return { alimentoId: a.id, quantidade: q, unidade: a.unidadeNome || 'unidade', gramas: Math.round(q * a.gramasPorUnidade * 100) / 100 };
  }
  const g = gramas < 20 ? Math.round(gramas) : Math.round(gramas / 5) * 5;
  if (g < 3) return null;
  return { alimentoId: a.id, quantidade: g, unidade: 'g', gramas: g };
}

/**
 * Gramas de cada alimento livre para a soma da refeicao chegar perto da meta.
 * Cada macro entra em proporcao da propria meta (errar 5 g de proteina pesa o mesmo que 5% das calorias),
 * e um puxao leve para a porcao de referencia evita respostas como 400 g de um alimento e 0 g dos outros.
 */
export function montarPelaMeta(meta: MetaRefeicao, escolhidos: Escolhido[]): { sugestoes: Sugestao[]; total: Macros } {
  const fixos = escolhidos.filter((e) => e.fixoG != null && e.fixoG > 0);
  const livres = escolhidos.filter((e) => e.fixoG == null);
  const base = fixos.reduce((s, e) => soma(s, macrosPorGrama(e.alimento, e.fixoG!)), zero);
  const alvos: { alvo: number; atual: number; peso: number; de: (a: Alimento) => number }[] = [];
  if (meta.calorias) alvos.push({ alvo: meta.calorias, atual: base.kcal, peso: 1, de: (a) => a.caloriasPor100g / 100 });
  if (meta.proteinasG) alvos.push({ alvo: meta.proteinasG, atual: base.p, peso: 1.3, de: (a) => a.proteinasPor100g / 100 });
  if (meta.carboidratosG) alvos.push({ alvo: meta.carboidratosG, atual: base.c, peso: 1, de: (a) => a.carboidratosPor100g / 100 });
  if (meta.gordurasG) alvos.push({ alvo: meta.gordurasG, atual: base.g, peso: 1, de: (a) => a.gordurasPor100g / 100 });

  let gramasLivres: number[] = livres.map(() => 0);
  if (alvos.length && livres.length) {
    // Variavel = numero de porcoes de referencia, para a escala ficar parecida entre os alimentos.
    const ref = livres.map((e) => porcaoRef(e.alimento));
    const A: number[][] = alvos.map((t) => livres.map((e, j) => (t.peso * t.de(e.alimento) * ref[j]) / t.alvo));
    const b: number[] = alvos.map((t) => (t.peso * (t.alvo - t.atual)) / t.alvo);
    // Puxao proporcional ao peso de cada alimento nas metas, para nao distorcer quando ha um alimento so.
    const normas = livres.map((_, j) => Math.sqrt(A.reduce((s, linha) => s + linha[j] ** 2, 0)) || 1);
    livres.forEach((_, j) => {
      const l = Math.sqrt(0.02) * normas[j];
      A.push(livres.map((__, jj) => (jj === j ? l : 0)));
      b.push(l);
    });
    gramasLivres = nnls(A, b).map((y, j) => y * ref[j]);
  }

  const sugestoes: Sugestao[] = [];
  fixos.forEach((e) => {
    const s = e.alimento.gramasPorUnidade
      ? { alimentoId: e.alimento.id, quantidade: Math.round((e.fixoG! / e.alimento.gramasPorUnidade) * 100) / 100, unidade: e.alimento.unidadeNome || 'unidade', gramas: e.fixoG! }
      : { alimentoId: e.alimento.id, quantidade: e.fixoG!, unidade: 'g', gramas: e.fixoG! };
    sugestoes.push(s);
  });
  livres.forEach((e, j) => {
    const s = arredondar(e.alimento, gramasLivres[j]);
    if (s) sugestoes.push(s);
  });
  // Mantem a ordem em que o profissional escolheu os alimentos.
  const ordem = new Map(escolhidos.map((e, i) => [e.alimento.id, i]));
  sugestoes.sort((a, b) => (ordem.get(a.alimentoId) ?? 0) - (ordem.get(b.alimentoId) ?? 0));
  const mapa = new Map(escolhidos.map((e) => [e.alimento.id, e.alimento]));
  const total = sugestoes.reduce((s, x) => soma(s, macrosPorGrama(mapa.get(x.alimentoId)!, x.gramas)), zero);
  return { sugestoes, total };
}

const zero: Macros = { kcal: 0, p: 0, c: 0, g: 0 };
const soma = (a: Macros, b: Macros): Macros => ({ kcal: a.kcal + b.kcal, p: a.p + b.p, c: a.c + b.c, g: a.g + b.g });

/** Mesma chave do servidor para o nome da refeicao: maiusculas, sem acento, espacos simples. */
export const chaveRefeicao = (nome: string) =>
  nome
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toUpperCase()
    .trim()
    .split(/\s+/)
    .join(' ');
