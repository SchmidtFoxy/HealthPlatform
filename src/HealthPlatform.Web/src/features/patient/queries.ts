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
    mutationFn: ({ refeicaoId, status }: { refeicaoId: string; status: Exclude<StatusRefeicao, 'Pendente'> }) =>
      api(`/api/portal/me/refeicoes/${refeicaoId}/adesao`, { method: 'POST', json: { status, observacao: null } }),
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
