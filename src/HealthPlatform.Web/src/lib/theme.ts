import { useEffect, useState } from 'react';

export type Tema = 'system' | 'dark' | 'light';

function aplicar(t: Tema) {
  const dark = t === 'dark' || (t === 'system' && matchMedia('(prefers-color-scheme: dark)').matches);
  document.documentElement.dataset.theme = dark ? 'dark' : 'light';
  document.querySelector('meta[name="theme-color"]')?.setAttribute('content', dark ? '#061210' : '#f1f6f5');
}

export function useTema() {
  const [tema, setTema] = useState<Tema>(() => {
    try {
      return (localStorage.getItem('aesyn_theme') as Tema) || 'system';
    } catch {
      return 'system';
    }
  });
  useEffect(() => {
    aplicar(tema);
    try {
      localStorage.setItem('aesyn_theme', tema);
    } catch {
      /* sem armazenamento */
    }
    if (tema !== 'system') return;
    const mq = matchMedia('(prefers-color-scheme: dark)');
    const fn = () => aplicar('system');
    mq.addEventListener('change', fn);
    return () => mq.removeEventListener('change', fn);
  }, [tema]);
  return [tema, setTema] as const;
}
