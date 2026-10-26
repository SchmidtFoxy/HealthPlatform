// Treino em andamento fica salvo no aparelho: fechar o app, perder sinal ou
// trocar de tela no meio do treino nao apaga nenhuma serie registrada.

import type { SessaoTreinoPortal } from '@/lib/types';
import { repsAlvo } from '@/lib/format';
import { seriesDoItem } from '@/lib/series';

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
  /** Exercicios trocados por uma alternativa do catalogo, por item da prescricao. */
  trocas?: Record<string, Troca>;
  /** Exercicios que o paciente acrescentou por conta propria neste treino. */
  extras?: Extra[];
}

export interface Extra {
  /** Chave local (prefixo "extra-"), usada como indice em series. */
  id: string;
  exercicioId: string;
  nome: string;
  grupoMuscular: string | null;
  equipamento: string | null;
}

export interface Troca {
  exercicioId: string;
  nome: string;
  motivo: string;
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
    // Cada serie nasce com a meta dela (piramide, top set...). Sem variacao, vale a carga da ultima vez.
    const alvos = seriesDoItem(it);
    const varia = alvos.some((a) => a.reps !== alvos[0].reps || a.carga !== alvos[0].carga);
    const ultima = ultimaCarga.get(it.id);
    series[it.id] = alvos.map((a) => ({ reps: repsAlvo(a.reps), carga: varia ? (a.carga ?? ultima ?? null) : (ultima ?? a.carga ?? null), feita: false }));
  }
  return { sessaoId: sessao.id, sessaoNome: sessao.nome, inicio: new Date().toISOString(), atual: 0, series };
}
