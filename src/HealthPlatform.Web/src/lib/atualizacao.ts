import { registerSW } from 'virtual:pwa-register';
import { lerSessao } from './api';
import { destinoDoLink } from './push';

declare const __APP_VERSION__: string;

// Garante que celulares e PWAs abertos em segundo plano peguem a versao nova sem precisar fechar o app.
export function vigiarAtualizacao() {
  let recarregou = false;
  navigator.serviceWorker?.addEventListener('controllerchange', () => {
    if (recarregou) return;
    recarregou = true;
    location.reload();
  });

  const atualizar = registerSW({
    immediate: true,
    onRegisteredSW(_url, reg) {
      if (!reg) return;
      setInterval(() => reg.update().catch(() => {}), 5 * 60 * 1000);
      document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') reg.update().catch(() => {});
      });
    },
  });

  // Se o servidor ja esta numa versao mais nova e o service worker ainda nao trocou, forca a troca.
  const checar = async () => {
    if (!navigator.onLine) return;
    try {
      const r = await fetch('/api/health', { cache: 'no-store' });
      const nova = (await r.json())?.version as string | undefined;
      if (!nova || nova === __APP_VERSION__) return;
      if (sessionStorage.getItem('aesyn-app-recarregou-para') === nova) return;
      sessionStorage.setItem('aesyn-app-recarregou-para', nova);
      await atualizar(true);
      setTimeout(() => location.reload(), 3000);
    } catch {
      /* sem rede ou API fora: tenta de novo depois */
    }
  };
  setTimeout(checar, 4000);
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') checar();
  });
}

// Clique numa notificacao com o app aberto: o service worker da raiz (/sw.js) avisa a janela.
// A navegacao fica com o React Router (useLinksDeNotificacao), sem recarregar a pagina.
export const EVENTO_ABRIR_LINK = 'aesyn:abrir-link';
export const EVENTO_PUSH_RECEBIDO = 'aesyn:push-recebido';

export function ouvirLinksDeNotificacao() {
  // App aberto pelo clique (/?aesynPushLink=...): vai direto para a tela do link.
  const link = new URLSearchParams(location.search).get('aesynPushLink');
  if (link) history.replaceState(null, '', destinoDoLink(link, lerSessao()?.tipoUsuario === 'Paciente'));

  navigator.serviceWorker?.addEventListener('message', (e) => {
    const link = String(e.data?.link || '');
    if (e.data?.type === 'AESYN_PUSH_RECEBIDO') window.dispatchEvent(new CustomEvent(EVENTO_PUSH_RECEBIDO, { detail: link }));
    if (e.data?.type !== 'AESYN_PUSH_OPEN' || !link) return;
    window.dispatchEvent(new CustomEvent(EVENTO_ABRIR_LINK, { detail: destinoDoLink(link, lerSessao()?.tipoUsuario === 'Paciente') }));
  });
}
