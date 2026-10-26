// Qual treino o paciente faz hoje, se ja fez, e qual vem depois.
// Planos com dias da semana seguem a agenda; planos sem dias (A, B, C...) seguem em rodizio
// a partir do ultimo treino registrado.

import { sessaoEhDoDia } from '@/lib/format';
import type { ExecucaoHistorico, PlanoTreinoPortal, SessaoTreinoPortal } from '@/lib/types';

const DIA = 864e5;
const DIAS_CURTOS = ['dom', 'seg', 'ter', 'qua', 'qui', 'sex', 'sáb'];

export const mesmoDia = (a: Date, b: Date) => a.toDateString() === b.toDateString();

export interface TreinoDoDia {
  /** Plano em rodizio (nenhuma sessao tem dia da semana). */
  rodizio: boolean;
  /** Sessao a fazer hoje; null quando nao ha nada pendente para hoje. */
  hoje: SessaoTreinoPortal | null;
  /** Treinos registrados hoje, do mais recente ao mais antigo. */
  feitosHoje: ExecucaoHistorico[];
  /** Proximo treino depois de hoje, com quando ele cai ("amanhã", "qui"). */
  proxima: { sessao: SessaoTreinoPortal; quando: string } | null;
  /** Nomes das sessoes ja feitas hoje, para marcar no plano. */
  feitasHojeNomes: Set<string>;
}

const quandoTexto = (dias: number, d: Date) => (dias === 1 ? 'amanhã' : DIAS_CURTOS[d.getDay()]);

export function treinoDoDia(plano: PlanoTreinoPortal | undefined, execucoes: ExecucaoHistorico[] = [], agora = new Date()): TreinoDoDia {
  const vazio: TreinoDoDia = { rodizio: false, hoje: null, feitosHoje: [], proxima: null, feitasHojeNomes: new Set() };
  if (!plano?.sessoes.length) return vazio;
  const sessoes = [...plano.sessoes].sort((a, b) => a.ordem - b.ordem);
  const ordenadas = [...execucoes].sort((a, b) => b.dataHoraInicioUtc.localeCompare(a.dataHoraInicioUtc));
  const feitosHoje = ordenadas.filter((e) => mesmoDia(new Date(e.dataHoraInicioUtc), agora));
  const feitasHojeNomes = new Set(feitosHoje.map((e) => e.sessao));
  const rodizio = sessoes.every((s) => !s.diasSemana?.trim());

  if (rodizio) {
    // Proxima do rodizio = a que vem depois da ultima feita (antes de hoje, se ja treinou hoje).
    const depoisDe = (nome: string | undefined) => {
      const i = sessoes.findIndex((s) => s.nome === nome);
      return sessoes[(i + 1) % sessoes.length];
    };
    if (feitosHoje.length) {
      return { rodizio, hoje: null, feitosHoje, feitasHojeNomes, proxima: { sessao: depoisDe(feitosHoje[0].sessao), quando: 'próximo treino' } };
    }
    const hoje = depoisDe(ordenadas[0]?.sessao);
    return { rodizio, hoje, feitosHoje, feitasHojeNomes, proxima: { sessao: depoisDe(hoje.nome), quando: 'depois' } };
  }

  const doDia = sessoes.filter((s) => !!s.diasSemana?.trim() && sessaoEhDoDia(s.diasSemana, agora));
  const hoje = doDia.find((s) => !feitasHojeNomes.has(s.nome)) ?? null;
  let proxima: TreinoDoDia['proxima'] = null;
  for (let n = 1; n <= 7 && !proxima; n++) {
    const d = new Date(agora.getTime() + n * DIA);
    const s = sessoes.find((x) => !!x.diasSemana?.trim() && sessaoEhDoDia(x.diasSemana, d));
    if (s) proxima = { sessao: s, quando: quandoTexto(n, d) };
  }
  return { rodizio, hoje, feitosHoje, feitasHojeNomes, proxima };
}
