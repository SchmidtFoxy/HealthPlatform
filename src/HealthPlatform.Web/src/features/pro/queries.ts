import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import type {
  Alimento,
  DashboardProfissional,
  Exercicio,
  HistoricoTreinos,
  ListaPacientes,
  Paciente,
  PlanoAlimentarPro,
  PlanoTreinoPro,
  ResumoClinico,
  SinaisDiarios,
} from '@/lib/types';

export const useDashboard = () =>
  useQuery({ queryKey: ['pro', 'dashboard'], queryFn: () => api<DashboardProfissional>(`/api/profissional/dashboard?offsetMinutos=${-new Date().getTimezoneOffset()}`) });
export const useSinais = () => useQuery({ queryKey: ['pro', 'sinais'], queryFn: () => api<SinaisDiarios>('/api/profissional/sinais-diarios') });
export const usePacientes = (busca = '') =>
  useQuery({
    queryKey: ['pro', 'pacientes', busca],
    queryFn: () => api<ListaPacientes>(`/api/pacientes?tamanhoPagina=100&busca=${encodeURIComponent(busca)}`),
    placeholderData: (prev) => prev,
  });
export const usePaciente = (id: string) => useQuery({ queryKey: ['pro', 'paciente', id], queryFn: () => api<Paciente>(`/api/pacientes/${id}`) });
export const useResumoClinico = (id: string) => useQuery({ queryKey: ['pro', 'resumo', id], queryFn: () => api<ResumoClinico>(`/api/pacientes/${id}/resumo-clinico`) });
export const usePlanosAlimentares = (id: string) =>
  useQuery({ queryKey: ['pro', 'dietas', id], queryFn: () => api<PlanoAlimentarPro[]>(`/api/pacientes/${id}/planos-alimentares`) });
export const usePlanosTreino = (id: string) => useQuery({ queryKey: ['pro', 'treinos', id], queryFn: () => api<PlanoTreinoPro[]>(`/api/pacientes/${id}/treinos`) });
export const useHistoricoPaciente = (id: string) =>
  useQuery({ queryKey: ['pro', 'historico', id], queryFn: () => api<HistoricoTreinos>(`/api/pacientes/${id}/treinos/historico?dias=90`) });
export const useAlimentos = () =>
  useQuery({ queryKey: ['pro', 'alimentos'], queryFn: () => api<Alimento[]>('/api/alimentos'), staleTime: 5 * 60_000 });
export const useExercicios = () =>
  useQuery({
    queryKey: ['pro', 'exercicios'],
    queryFn: async () => {
      const r = await api<Exercicio[] | { itens: Exercicio[] }>('/api/exercicios');
      return Array.isArray(r) ? r : r.itens;
    },
    staleTime: 5 * 60_000,
  });
