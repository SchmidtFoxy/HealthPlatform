import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { n0, n1 } from '@/lib/format';

// Catalogo de suplementos da equipe (Biblioteca e editor de dieta).

export interface Suplemento {
  id: string;
  nome: string;
  marca: string | null;
  categoria: string;
  forma: string | null;
  porcaoQuantidade: number;
  porcaoUnidade: string;
  caloriasPorPorcao: number;
  proteinasGPorPorcao: number;
  carboidratosGPorPorcao: number;
  gordurasGPorPorcao: number;
  fibrasGPorPorcao: number;
  cafeinaMgPorPorcao: number | null;
  composicao: string | null;
  instrucoesUso: string | null;
  observacoes: string | null;
  ativo: boolean;
}

export const CATEGORIAS_SUPLEMENTO = ['Proteína', 'Creatina', 'Pré-treino', 'Barrinha proteica', 'Eletrólitos', 'Vitaminas e minerais', 'Carboidrato', 'Recuperação', 'Outros'];

export const useSuplementos = (incluirInativos = false) =>
  useQuery({ queryKey: ['pro', 'suplementos', incluirInativos], queryFn: () => api<Suplemento[]>(`/api/suplementos?incluirInativos=${incluirInativos}`), staleTime: 60_000 });

export const porcaoSuplemento = (x: Suplemento) =>
  `${n1(x.porcaoQuantidade)} ${x.porcaoUnidade} · ${n0(x.caloriasPorPorcao)} kcal · P ${n1(x.proteinasGPorPorcao)} · C ${n1(x.carboidratosGPorPorcao)} · G ${n1(x.gordurasGPorPorcao)}${x.cafeinaMgPorPorcao != null ? ` · cafeína ${n0(x.cafeinaMgPorPorcao)} mg` : ''}`;
