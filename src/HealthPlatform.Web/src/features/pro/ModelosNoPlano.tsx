import { useMutation, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { BookCopy } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button, Card, Field, inputCls, Sheet, Skeleton, toast } from '@/components/ui';
import { api } from '@/lib/api';
import { Busca, useModelos } from './BibliotecaPage';

// Sessoes e refeicoes reutilizaveis dentro dos editores de treino e dieta:
// inserir uma sessao-modelo ou refeicao-modelo no rascunho, e guardar a atual como modelo.

export interface ConteudoSessaoModelo {
  nomeOriginal: string;
  diasSemanaOriginal: string | null;
  observacoesOriginais: string | null;
  itens: { exercicioId: string; ordem: number; series: number; repeticoes: string; carga: number | null; unidadeCarga: string | null; descansoSegundos: number | null; tempoSegundos: number | null; observacoes: string | null }[];
}

export interface ConteudoRefeicaoModelo {
  nomeOriginal: string;
  horarioOriginal: string | null;
  observacoesOriginais: string | null;
  metaCalorias: number | null;
  metaProteinasG: number | null;
  metaCarboidratosG: number | null;
  metaGordurasG: number | null;
  metaFibrasG: number | null;
  itens: {
    alimentoId: string;
    quantidade: number;
    unidade: string;
    quantidadeGramas: number;
    observacao: string | null;
    substituicoes: { alimentoId: string; quantidade: number; unidade: string; quantidadeGramas: number; observacao: string | null }[];
  }[];
}

type Tipo = 'sessao' | 'refeicao';
const ROTA_CONTEUDO: Record<Tipo, (id: string) => string> = {
  sessao: (id) => `/api/modelos-sessoes-treino/${id}/conteudo`,
  refeicao: (id) => `/api/modelos-refeicoes/${id}/conteudo`,
};

export function InserirModeloSheet<T extends Tipo>({
  tipo,
  open,
  onClose,
  onInserir,
}: {
  tipo: T;
  open: boolean;
  onClose: () => void;
  onInserir: (conteudo: T extends 'sessao' ? ConteudoSessaoModelo : ConteudoRefeicaoModelo, nome: string) => void;
}) {
  const modelos = useModelos(tipo);
  const [busca, setBusca] = useState('');
  const [carregando, setCarregando] = useState<string | null>(null);
  useEffect(() => {
    if (open) setBusca('');
  }, [open]);
  const termo = busca.trim().toLowerCase();
  const lista = (modelos.data ?? []).filter((m) => !termo || `${m.nome} ${m.categoria ?? ''}`.toLowerCase().includes(termo));

  const escolher = async (id: string, nome: string) => {
    setCarregando(id);
    try {
      const r = await api<{ conteudo: never }>(ROTA_CONTEUDO[tipo](id));
      onInserir(r.conteudo, nome);
      onClose();
    } catch (e) {
      toast(e instanceof Error ? e.message : 'Não foi possível abrir o modelo.', 'bad');
    } finally {
      setCarregando(null);
    }
  };

  const rotulo = tipo === 'sessao' ? 'sessão' : 'refeição';
  return (
    <Sheet open={open} onClose={onClose} title={tipo === 'sessao' ? 'Inserir sessão-modelo' : 'Inserir refeição-modelo'}>
      <div className="flex flex-col gap-3">
        <Busca value={busca} onChange={setBusca} placeholder={`Buscar ${rotulo}`} />
        {modelos.isLoading ? (
          <Skeleton className="h-32" />
        ) : !lista.length ? (
          <p className="rounded-xl bg-raised/60 p-3 text-sm text-muted">
            {modelos.data?.length ? 'Nada encontrado.' : `Nenhuma ${rotulo}-modelo ainda. Use "Salvar como modelo" numa ${rotulo} pronta para começar.`}
          </p>
        ) : (
          <Card className="max-h-[55vh] divide-y divide-line/60 overflow-y-auto">
            {lista.map((m) => (
              <button
                key={m.id}
                disabled={!!carregando}
                onClick={() => escolher(m.id, m.nome)}
                className={clsx('flex w-full items-center gap-3 p-3.5 text-left transition hover:bg-raised/40', carregando === m.id && 'opacity-60')}
              >
                <BookCopy className="h-4 w-4 shrink-0 text-accent" />
                <span className="min-w-0 flex-1">
                  <span className="block truncate font-semibold">{m.nome}</span>
                  <span className="block truncate text-[13px] text-muted">
                    {[m.categoria, tipo === 'sessao' ? `${m.exercicios ?? 0} exercícios` : `${m.itens ?? 0} itens`, tipo === 'sessao' ? m.diasSemana : m.horario?.slice(0, 5)].filter(Boolean).join(' · ')}
                  </span>
                </span>
              </button>
            ))}
          </Card>
        )}
        <p className="text-[12px] text-faint">Entra como uma cópia no rascunho. Ajuste o que quiser e salve o plano.</p>
      </div>
    </Sheet>
  );
}

/** Guarda uma sessao ou refeicao ja salva como modelo da biblioteca. */
export function SalvarParteComoModeloSheet({ tipo, alvo, onClose }: { tipo: Tipo; alvo: { id: string; nome: string } | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [nome, setNome] = useState('');
  const [categoria, setCategoria] = useState('');
  const [descricao, setDescricao] = useState('');
  useEffect(() => {
    if (!alvo) return;
    setNome(alvo.nome);
    setCategoria('');
    setDescricao('');
  }, [alvo]);
  const salvar = useMutation({
    mutationFn: () =>
      api(tipo === 'sessao' ? `/api/sessoes-treino/${alvo!.id}/salvar-como-modelo` : `/api/refeicoes-plano/${alvo!.id}/salvar-como-modelo`, {
        method: 'POST',
        json: { nome: nome.trim(), categoria: categoria.trim() || null, descricao: descricao.trim() || null },
      }),
    onSuccess: () => {
      toast('Modelo salvo na biblioteca.');
      qc.invalidateQueries({ queryKey: ['pro', 'modelos', tipo] });
      onClose();
    },
    onError: (e) => toast(e instanceof Error ? e.message : 'Não foi possível salvar.', 'bad'),
  });
  return (
    <Sheet
      open={!!alvo}
      onClose={onClose}
      title={tipo === 'sessao' ? 'Salvar sessão como modelo' : 'Salvar refeição como modelo'}
      footer={
        <Button size="lg" className="w-full" disabled={!nome.trim()} loading={salvar.isPending} onClick={() => salvar.mutate()}>
          Salvar na biblioteca
        </Button>
      }
    >
      <div className="flex flex-col gap-4">
        <Field label="Nome">
          <input className={inputCls} value={nome} maxLength={160} onChange={(e) => setNome(e.target.value)} />
        </Field>
        <Field label="Categoria">
          <input className={inputCls} value={categoria} maxLength={80} placeholder={tipo === 'sessao' ? 'Ex.: Inferiores, Mobilidade' : 'Ex.: Café da manhã, Pré-treino'} onChange={(e) => setCategoria(e.target.value)} />
        </Field>
        <Field label="Descrição">
          <textarea className={clsx(inputCls, 'h-20 py-2.5')} value={descricao} maxLength={1000} onChange={(e) => setDescricao(e.target.value)} />
        </Field>
      </div>
    </Sheet>
  );
}
