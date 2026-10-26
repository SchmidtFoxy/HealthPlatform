import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import type { HistoricoDiario, HistoricoTreinos, HomePaciente, MeusTreinos, PlanoAlimentarPortal, StatusRefeicao } from '@/lib/types';

export const useHome = () => useQuery({ queryKey: ['p', 'home'], queryFn: () => api<HomePaciente>('/api/portal/me/home') });
export const useMeusTreinos = () => useQuery({ queryKey: ['p', 'treinos'], queryFn: () => api<MeusTreinos>('/api/portal/me/treino/todos') });
export const useHistoricoTreinos = (dias = 90) =>
  useQuery({ queryKey: ['p', 'historico', dias], queryFn: () => api<HistoricoTreinos>(`/api/portal/me/treinos/historico?dias=${dias}`) });
export const usePlanoAlimentar = () => useQuery({ queryKey: ['p', 'plano'], queryFn: () => api<PlanoAlimentarPortal>('/api/portal/me/plano') });
export const useChatNaoLidas = () =>
  useQuery({ queryKey: ['p', 'chat-nao-lidas'], queryFn: () => api<{ total: number }>('/api/chat/nao-lidas'), refetchInterval: 60_000 });
export const useHistoricoDiario = (dias = 14) =>
  useQuery({ queryKey: ['p', 'diario', dias], queryFn: () => api<HistoricoDiario>(`/api/portal/me/historico-diario?dias=${dias}`) });

export interface CheckIn {
  data: string;
  sonoHoras: number;
  sonoQualidade: number;
  energiaNivel: number;
  dorNivel: number;
  disposicaoNivel: number;
  recuperacaoNivel: number;
}

export function useRegistrarCheckIn() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (c: CheckIn) => api('/api/portal/me/prontidao', { method: 'POST', json: c }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['p'] }),
  });
}

export function useAdesaoRefeicao() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ refeicaoId, status, observacao }: { refeicaoId: string; status: Exclude<StatusRefeicao, 'Pendente'>; observacao?: string }) =>
      api(`/api/portal/me/refeicoes/${refeicaoId}/adesao`, { method: 'POST', json: { status, observacao: observacao ?? null } }),
    onMutate: async ({ refeicaoId, status }) => {
      await qc.cancelQueries({ queryKey: ['p', 'home'] });
      const antes = qc.getQueryData<HomePaciente>(['p', 'home']);
      if (antes)
        qc.setQueryData<HomePaciente>(['p', 'home'], {
          ...antes,
          adesaoNutricional: {
            ...antes.adesaoNutricional,
            refeicoes: antes.adesaoNutricional.refeicoes.map((r) => (r.refeicaoId === refeicaoId ? { ...r, status } : r)),
          },
        });
      return { antes };
    },
    onError: (_e, _v, ctx) => ctx?.antes && qc.setQueryData(['p', 'home'], ctx.antes),
    onSettled: () => qc.invalidateQueries({ queryKey: ['p', 'home'] }),
  });
}

// ---------------------------------------------------------------- Cuidados (pedidos, medicamentos, metas)

export interface Solicitacao {
  id: string;
  tipo: string;
  titulo: string;
  descricao: string | null;
  dataLimiteUtc: string | null;
  status: 'Pendente' | 'Enviada' | 'Revisada' | 'Cancelada' | string;
  profissionalNome: string;
  respostaPaciente: string | null;
  linkResposta: string | null;
  respondidaEmUtc: string | null;
  observacaoRevisao: string | null;
  createdAtUtc: string;
}

export interface Medicamento {
  id: string;
  nome: string;
  dose: number | null;
  unidade: string | null;
  via: string | null;
  frequencia: string | null;
  horariosLocais: string | null;
  orientacao: string | null;
}

export interface RegistroMedicamento {
  id: string;
  medicamentoId: string;
  dataHoraPrevistaUtc: string;
  status: 'Tomado' | 'Pulado' | string;
}

export interface Meta {
  id: string;
  nome: string;
  tipo: string;
  valorObjetivo: number | null;
  unidade: string | null;
  frequencia: string;
  status: string;
  observacoes: string | null;
  registros: { id: string; data: string; valor: number | null; concluida: boolean | null }[];
  resumo: { registros: number; concluidos: number; percentualConclusao: number };
}

export const useSolicitacoes = () =>
  useQuery({ queryKey: ['p', 'solicitacoes'], queryFn: () => api<{ pendentes: number; aguardandoRevisao: number; itens: Solicitacao[] }>('/api/portal/me/solicitacoes') });
export const useMedicamentos = () =>
  useQuery({
    queryKey: ['p', 'medicamentos'],
    queryFn: () =>
      api<{ data: string; medicamentos: Medicamento[]; registrosHoje: RegistroMedicamento[] }>(`/api/portal/me/medicamentos?offsetMinutos=${-new Date().getTimezoneOffset()}`),
  });
export const useMetas = () => useQuery({ queryKey: ['p', 'metas'], queryFn: () => api<{ inicio: string; fim: string; metas: Meta[] }>('/api/portal/me/metas?dias=30') });

/** Horarios de hoje de um medicamento ("08:00,20:00") como instantes UTC, no fuso do aparelho. */
export function horariosHoje(m: Medicamento): { hora: string; utc: string }[] {
  const lista = (m.horariosLocais ?? '').split(',').map((h) => h.trim()).filter(Boolean);
  return (lista.length ? lista : ['08:00']).map((hora) => {
    const [h, min] = hora.split(':').map(Number);
    const d = new Date();
    d.setHours(h, min || 0, 0, 0);
    return { hora: hora.slice(0, 5), utc: d.toISOString() };
  });
}
