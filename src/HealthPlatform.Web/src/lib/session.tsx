import { createContext, ReactNode, useContext, useEffect, useState } from 'react';
import { aoMudarSessao, lerSessao, Sessao } from './api';

const Ctx = createContext<Sessao | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [s, setS] = useState(lerSessao);
  useEffect(() => {
    const off = aoMudarSessao(setS);
    // Outra aba (ou o front classico) entrou ou saiu.
    const onStorage = (e: StorageEvent) => e.key?.startsWith('hp_') && setS(lerSessao());
    window.addEventListener('storage', onStorage);
    return () => {
      off();
      window.removeEventListener('storage', onStorage);
    };
  }, []);
  return <Ctx.Provider value={s}>{children}</Ctx.Provider>;
}

export const useSessao = () => useContext(Ctx);
