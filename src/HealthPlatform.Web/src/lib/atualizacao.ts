import { registerSW } from 'virtual:pwa-register';

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

// Clique numa notificacao com o app aberto: o service worker da versao classica avisa a janela.
// Paciente abre chat/arquivos aqui mesmo; o resto ainda vive na classica.
export function ouvirLinksDeNotificacao() {
  navigator.serviceWorker?.addEventListener('message', (e) => {
    if (e.data?.type !== 'AESYN_PUSH_OPEN') return;
    const link = String(e.data.link || '');
    if (link === 'chat' || link === 'arquivos') location.assign(`/app/${link}`);
    else if (link) location.assign(`/?aesynPushLink=${encodeURIComponent(link)}`);
  });
}
