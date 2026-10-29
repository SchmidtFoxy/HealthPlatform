import React from 'react';
import ReactDOM from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { App } from './App';
import { SessionProvider } from './lib/session';
import { ApiError } from './lib/api';
import { ouvirLinksDeNotificacao, vigiarAtualizacao } from './lib/atualizacao';
import './styles.css';

declare global {
  interface Window {
    __aesynAbertura?: { pronto: () => void };
  }
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: true,
      retry: (n, e) => !(e instanceof ApiError && e.status < 500) && n < 2,
    },
  },
});

vigiarAtualizacao();
ouvirLinksDeNotificacao();

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <SessionProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </SessionProvider>
    </QueryClientProvider>
  </React.StrictMode>,
);

// Tira a abertura animada (index.html) depois que o app desenhou o primeiro quadro.
requestAnimationFrame(() => window.__aesynAbertura?.pronto());
