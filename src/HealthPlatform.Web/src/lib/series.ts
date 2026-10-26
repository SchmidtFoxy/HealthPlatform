// Séries por linha: cada série pode ter reps e carga próprias (pirâmide, aquecimento, top set).
// O formato é o mesmo do builder clássico e da API de modelos: blocos de séries iguais, guardados
// como um marcador base64url em "observacoes" do item. Sem marcador, todas as séries seguem
// Séries + Repetições + Carga do item.

export interface SerieAlvo {
  reps: string;
  carga: number | null;
}

interface Bloco {
  Ordem: number;
  Nome: string | null;
  Series: number;
  Repeticoes: string;
  Carga: number | null;
  UnidadeCarga: string | null;
  DescansoSegundos: number | null;
  Observacoes: string | null;
}

const MARCADOR = /\s*\[AESYN:BLOCOS_SERIES:([A-Za-z0-9_-]+)\]\s*/;

const campo = (o: Record<string, unknown>, nome: string) => o[nome] ?? o[nome[0].toLowerCase() + nome.slice(1)];

function decodificar(b64url: string): Record<string, unknown>[] {
  try {
    let b64 = b64url.replace(/-/g, '+').replace(/_/g, '/');
    while (b64.length % 4) b64 += '=';
    const bytes = Uint8Array.from(atob(b64), (c) => c.charCodeAt(0));
    const dados = JSON.parse(new TextDecoder().decode(bytes));
    return Array.isArray(dados) ? dados : [];
  } catch {
    return [];
  }
}

function codificar(json: string) {
  const bytes = new TextEncoder().encode(json);
  let bin = '';
  bytes.forEach((b) => (bin += String.fromCharCode(b)));
  return btoa(bin).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
}

/** Observação do item sem o marcador técnico. */
export const obsLimpa = (obs: string | null | undefined) => (obs ?? '').replace(MARCADOR, ' ').trim() || null;

/** Séries planejadas do item, uma por linha. */
export function seriesDoItem(it: { series: number; repeticoes: string; carga: number | null; observacoes?: string | null }): SerieAlvo[] {
  const m = (it.observacoes ?? '').match(MARCADOR);
  const blocos = m ? decodificar(m[1]) : [];
  const lista: SerieAlvo[] = [];
  [...blocos]
    .sort((a, b) => Number(campo(a, 'Ordem') ?? 0) - Number(campo(b, 'Ordem') ?? 0))
    .forEach((b) => {
      const n = Math.max(1, Number(campo(b, 'Series') ?? 1));
      const carga = campo(b, 'Carga');
      for (let i = 0; i < n; i++) lista.push({ reps: String(campo(b, 'Repeticoes') ?? ''), carga: carga == null ? null : Number(carga) });
    });
  if (lista.length) return lista;
  const partes = it.repeticoes.split('/').map((x) => x.trim()).filter(Boolean);
  return Array.from({ length: Math.max(1, it.series) }, (_, i) => ({
    reps: partes.length > 1 ? (partes[i] ?? partes[partes.length - 1]) : it.repeticoes,
    carga: it.carga,
  }));
}

const iguais = (a: SerieAlvo, b: SerieAlvo) => a.reps === b.reps && a.carga === b.carga;

/**
 * Grava as séries de volta no item: Séries, Repetições ("12/10/8" quando variam), Carga (a maior)
 * e o marcador em observacoes quando as séries não são todas iguais.
 */
export function aplicarSeries<T extends { series: number; repeticoes: string; carga: number | null; unidadeCarga: string | null; descansoSegundos: number | null; observacoes: string | null }>(
  it: T,
  lista: SerieAlvo[],
): T {
  const limpa = obsLimpa(it.observacoes);
  const todasIguais = lista.every((s) => iguais(s, lista[0]));
  const cargas = lista.map((s) => s.carga).filter((c): c is number => c != null);
  const base = {
    ...it,
    series: lista.length,
    repeticoes: todasIguais ? lista[0].reps : lista.map((s) => s.reps).join('/'),
    carga: todasIguais ? lista[0].carga : cargas.length ? Math.max(...cargas) : null,
  };
  if (todasIguais) return { ...base, observacoes: limpa };
  const blocos: Bloco[] = [];
  for (const s of lista) {
    const ult = blocos[blocos.length - 1];
    if (ult && ult.Repeticoes === s.reps && ult.Carga === s.carga) ult.Series++;
    else
      blocos.push({ Ordem: blocos.length + 1, Nome: null, Series: 1, Repeticoes: s.reps, Carga: s.carga, UnidadeCarga: it.unidadeCarga, DescansoSegundos: it.descansoSegundos, Observacoes: null });
  }
  const marcador = `[AESYN:BLOCOS_SERIES:${codificar(JSON.stringify(blocos))}]`;
  return { ...base, observacoes: limpa ? `${limpa} ${marcador}` : marcador };
}
