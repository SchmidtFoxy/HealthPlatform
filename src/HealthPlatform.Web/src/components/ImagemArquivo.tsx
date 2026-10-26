import { useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { AnimatePresence, motion } from 'framer-motion';
import { Download, ImageOff, X } from 'lucide-react';
import { useEffect } from 'react';
import { createPortal } from 'react-dom';
import { urlDoArquivo } from '@/lib/arquivos';

// Fotos enviadas aparecem como imagem, do jeito do WhatsApp: miniatura no lugar do icone
// e toque para abrir em tela cheia. O download pede o token, entao a imagem vem como blob.

export const ehImagem = (a: { contentType?: string | null; nome?: string | null }) =>
  !!a.contentType?.startsWith('image/') || /\.(png|jpe?g|webp|gif|heic|heif)$/i.test(a.nome ?? '');

export function useImagemArquivo(id: string | null | undefined, pacienteId?: string) {
  return useQuery({
    queryKey: ['arquivo-blob', pacienteId ?? 'me', id],
    queryFn: () => urlDoArquivo(id!, pacienteId),
    enabled: !!id,
    staleTime: Infinity,
    gcTime: 10 * 60_000,
    retry: 1,
  });
}

/** Miniatura da foto, na proporcao original da imagem (limitada por maxAltura), como no WhatsApp. */
export function ImagemArquivo({ id, pacienteId, alt, className, maxAltura = 420, onAbrir }: { id: string; pacienteId?: string; alt: string; className?: string; maxAltura?: number; onAbrir?: () => void }) {
  const q = useImagemArquivo(id, pacienteId);
  if (q.isError)
    return (
      <span className={clsx('grid aspect-[4/3] place-items-center gap-1 bg-raised text-[12px] text-muted', className)}>
        <ImageOff className="h-5 w-5" />
      </span>
    );
  if (!q.data) return <span className={clsx('block aspect-[4/3] animate-pulse bg-raised', className)} />;
  return (
    <button type="button" onClick={onAbrir} aria-label={`Abrir ${alt}`} className={clsx('block overflow-hidden', className)}>
      <img src={q.data} alt={alt} style={{ maxHeight: maxAltura }} className="block h-auto w-full object-cover object-top" loading="lazy" />
    </button>
  );
}

/** Foto em tela cheia sobre fundo escuro, com fechar e baixar. */
export function VisualizadorImagem({ arquivo, pacienteId, onClose }: { arquivo: { id: string; nome: string; legenda?: string | null } | null; pacienteId?: string; onClose: () => void }) {
  const q = useImagemArquivo(arquivo?.id, pacienteId);
  useEffect(() => {
    if (!arquivo) return;
    const h = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', h);
    return () => window.removeEventListener('keydown', h);
  }, [arquivo, onClose]);

  return createPortal(
    <AnimatePresence>
      {arquivo && (
        <motion.div
          role="dialog"
          aria-label={arquivo.nome}
          className="fixed inset-0 z-[90] flex flex-col bg-black/95 text-white"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
        >
          <div className="flex items-center gap-2 px-3 pb-2 pt-safe" onClick={(e) => e.stopPropagation()}>
            <button aria-label="Fechar" onClick={onClose} className="mt-2 grid h-11 w-11 place-items-center rounded-full hover:bg-white/10">
              <X className="h-6 w-6" />
            </button>
            <p className="mt-2 min-w-0 flex-1 truncate text-sm font-semibold">{arquivo.nome}</p>
            {q.data && (
              <a href={q.data} download={arquivo.nome} aria-label="Baixar" className="mt-2 grid h-11 w-11 place-items-center rounded-full hover:bg-white/10">
                <Download className="h-5 w-5" />
              </a>
            )}
          </div>
          <div className="flex min-h-0 flex-1 items-center justify-center p-2">
            {q.data ? (
              <motion.img
                src={q.data}
                alt={arquivo.nome}
                className="max-h-full max-w-full touch-pinch-zoom select-none object-contain"
                initial={{ scale: 0.94 }}
                animate={{ scale: 1 }}
                onClick={(e) => e.stopPropagation()}
              />
            ) : q.isError ? (
              <p className="text-sm text-white/70">Não foi possível carregar a imagem.</p>
            ) : (
              <span className="h-10 w-10 animate-spin rounded-full border-2 border-white/30 border-t-white" />
            )}
          </div>
          {arquivo.legenda && <p className="px-4 pb-safe pt-2 text-center text-sm text-white/80">{arquivo.legenda}</p>}
          <div className="pb-safe" />
        </motion.div>
      )}
    </AnimatePresence>,
    document.body,
  );
}
