import clsx from 'clsx';
import { useEffect, useState } from 'react';
import { Skeleton } from '@/components/ui';
import { lerSessao } from '@/lib/api';

// As fotos ficam atras de endpoint autenticado; baixa como blob e reaproveita entre telas.
const cacheImagens = new Map<string, Promise<string>>();
export function urlProtegida(path: string) {
  let p = cacheImagens.get(path);
  if (!p) {
    const s = lerSessao();
    p = fetch(path, { headers: s ? { Authorization: `Bearer ${s.token}` } : {} }).then(async (r) => {
      if (!r.ok) throw new Error('Imagem indisponível.');
      return URL.createObjectURL(await r.blob());
    });
    p.catch(() => cacheImagens.delete(path));
    cacheImagens.set(path, p);
  }
  return p;
}

export function useImagem(path: string | null | undefined) {
  const [src, setSrc] = useState<string | null>(null);
  const [erro, setErro] = useState(false);
  useEffect(() => {
    let vivo = true;
    setSrc(null);
    setErro(false);
    if (path)
      urlProtegida(path).then(
        (u) => vivo && setSrc(u),
        () => vivo && setErro(true),
      );
    return () => {
      vivo = false;
    };
  }, [path]);
  return { src, erro };
}

export function Foto({ path, alt, className }: { path: string; alt: string; className?: string }) {
  const { src, erro } = useImagem(path);
  if (erro) return <div className={clsx('grid place-items-center bg-raised text-[13px] text-faint', className)}>Imagem indisponível</div>;
  if (!src) return <Skeleton className={className} />;
  return <img src={src} alt={alt} className={clsx('object-cover', className)} />;
}
