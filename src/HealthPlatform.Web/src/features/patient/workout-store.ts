// Treino em andamento fica salvo no aparelho: fechar o app, perder sinal ou
// trocar de tela no meio do treino nao apaga nenhuma serie registrada.

import type { SessaoTreinoPortal } from '@/lib/types';
import { repsAlvo } from '@/lib/format';

export interface SerieRegistro {
  reps: number | null;
  carga: number | null;
  feita: boolean;
}

export interface TreinoAndamento {
  sessaoId: string;
  sessaoNome: string;
  inicio: string;
  atual: number;
  series: Record<string, SerieRegistro[]>;
}

const KEY = 'aesyn_treino_andamento';

export function treinoEmAndamento(): TreinoAndamento | null {
  try {
    return JSON.parse(localStorage.getItem(KEY) || 'null');
  } catch {
    return null;
  }
}

export function salvarAndamento(t: TreinoAndamento | null) {
  try {
    if (t) localStorage.setItem(KEY, JSON.stringify(t));
    else localStorage.removeItem(KEY);
  } catch {
    /* sem armazenamento: o treino segue apenas em memoria */
  }
}

/** Cria o registro inicial ja preenchido com a prescricao (ou com o que foi feito da ultima vez). */
export function novoAndamento(sessao: SessaoTreinoPortal, ultimaCarga: Map<string, number>): TreinoAndamento {
  const series: Record<string, SerieRegistro[]> = {};
  for (const it of sessao.itens) {
    const carga = ultimaCarga.get(it.id) ?? it.carga ?? null;
    series[it.id] = Array.from({ length: Math.max(1, it.series) }, () => ({ reps: repsAlvo(it.repeticoes), carga, feita: false }));
  }
  return { sessaoId: sessao.id, sessaoNome: sessao.nome, inicio: new Date().toISOString(), atual: 0, series };
}
