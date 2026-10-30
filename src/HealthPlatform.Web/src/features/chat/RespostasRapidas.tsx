import { Plus, X } from 'lucide-react';
import { useState } from 'react';
import { Sheet } from '@/components/ui';

// Respostas prontas do profissional, guardadas neste aparelho. {nome} vira o primeiro nome do paciente.

const CHAVE = 'aesyn-chat-respostas-rapidas';

const PADRAO = [
  'Oi, {nome}! Recebi sua mensagem e já vou olhar com calma.',
  'Ótimo trabalho, {nome}! Continue assim. 💪',
  'Pode me mandar uma foto para eu avaliar melhor?',
  'Vou ajustar seu plano e te aviso assim que estiver pronto.',
  'Como você está se sentindo hoje, de 0 a 10?',
  'Se sentir qualquer dor fora do normal, pare o exercício e me avise.',
];

function ler(): string[] {
  try {
    const v = JSON.parse(localStorage.getItem(CHAVE) ?? 'null');
    return Array.isArray(v) ? v.filter((x) => typeof x === 'string') : PADRAO;
  } catch {
    return PADRAO;
  }
}

function gravar(lista: string[]) {
  try {
    localStorage.setItem(CHAVE, JSON.stringify(lista));
  } catch {
    /* sem armazenamento: vale so ate fechar */
  }
}

export const preencherResposta = (modelo: string, nomePaciente?: string) => modelo.split('{nome}').join((nomePaciente ?? '').trim().split(/\s+/)[0] || '');

export function RespostasRapidas({ open, onClose, onEscolher, textoAtual, nomePaciente }: { open: boolean; onClose: () => void; onEscolher: (texto: string) => void; textoAtual: string; nomePaciente?: string }) {
  const [lista, setLista] = useState(ler);
  const atualizar = (nova: string[]) => {
    setLista(nova);
    gravar(nova);
  };
  const atual = textoAtual.trim();
  const podeSalvar = !!atual && !lista.includes(atual);

  return (
    <Sheet open={open} onClose={onClose} title="Respostas rápidas">
      <div className="flex flex-col gap-2 pb-2">
        {lista.map((r) => (
          <div key={r} className="flex items-center gap-1 rounded-2xl border border-line bg-bg">
            <button
              type="button"
              onClick={() => {
                onEscolher(preencherResposta(r, nomePaciente));
                onClose();
              }}
              className="min-w-0 flex-1 px-3.5 py-3 text-left text-[14px] leading-snug transition hover:text-accent"
            >
              {preencherResposta(r, nomePaciente)}
            </button>
            <button type="button" aria-label="Remover resposta" onClick={() => atualizar(lista.filter((x) => x !== r))} className="mr-1 grid h-9 w-9 shrink-0 place-items-center rounded-full text-faint hover:bg-raised hover:text-bad">
              <X className="h-4 w-4" />
            </button>
          </div>
        ))}
        {!lista.length && <p className="py-4 text-center text-sm text-muted">Nenhuma resposta salva. Escreva uma mensagem e salve aqui para usar de novo.</p>}
        <button
          type="button"
          disabled={!podeSalvar}
          onClick={() => atualizar([...lista, atual])}
          className="mt-1 flex items-center justify-center gap-2 rounded-2xl border border-dashed border-line px-3.5 py-3 text-[14px] font-semibold text-muted transition hover:border-accent/50 hover:text-accent disabled:opacity-50 disabled:hover:border-line disabled:hover:text-muted"
        >
          <Plus className="h-4 w-4" />
          {podeSalvar ? 'Salvar o texto que estou escrevendo' : 'Escreva no campo para salvar uma nova'}
        </button>
        <p className="text-center text-[12px] text-faint">Use {'{nome}'} para o primeiro nome do paciente. Ficam salvas neste aparelho.</p>
      </div>
    </Sheet>
  );
}
