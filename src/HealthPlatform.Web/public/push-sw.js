// Notificacoes push do AESYN. Importado pelo service worker do app (/sw.js, gerado pelo Vite PWA),
// que fica no escopo da raiz: assim as inscricoes feitas em versoes anteriores continuam valendo.

// Tela da conversa de cada link de chat (o mesmo que destinoDoLink, em src/lib/push.ts).
const telaDaConversa = (link) => {
  if (link === 'chat') return '/chat';
  const m = /^chat-profissional:([0-9a-f-]{36})$/i.exec(link);
  return m ? `/pro/mensagens/${m[1]}` : '';
};

self.addEventListener('push', (event) => {
  let payload = { title: 'AESYN Performance', body: 'Você tem uma nova atualização.', icon: '/icons/icon-192.png', badge: '/icons/icon-192.png', data: { link: '' } };
  try {
    if (event.data) payload = { ...payload, ...event.data.json() };
  } catch {
    if (event.data) payload.body = event.data.text();
  }
  const link = String(payload.data?.link || '');
  const conversa = telaDaConversa(link);
  const tag = payload.tag || (conversa ? `aesyn-${link.replace(/[^a-zA-Z0-9:_-]/g, '')}` : 'aesyn-update');

  event.waitUntil(
    (async () => {
      const janelas = await clients.matchAll({ type: 'window', includeUncontrolled: true });
      // O app aberto atualiza a conversa e o contador na hora, sem esperar a proxima consulta.
      janelas.forEach((j) => j.postMessage({ type: 'AESYN_PUSH_RECEBIDO', link }));

      // Varias mensagens da mesma conversa viram uma notificacao so, com a contagem.
      let body = payload.body;
      let total = 1;
      if (conversa) {
        const anteriores = await self.registration.getNotifications({ tag });
        total = anteriores.reduce((n, x) => Math.max(n, Number(x.data?.total) || 1), 0) + 1;
        if (total > 1) body = `${payload.body}\n${total} mensagens novas`;
      }

      await self.registration.showNotification(payload.title || 'AESYN Performance', {
        body,
        icon: payload.icon || '/icons/icon-192.png',
        badge: payload.badge || '/icons/icon-192.png',
        tag,
        renotify: payload.renotify ?? !!conversa,
        vibrate: conversa ? [120, 60, 120] : undefined,
        timestamp: Date.now(),
        data: { ...(payload.data || {}), total },
      });

      // Quem ja esta com a conversa aberta na tela nao precisa do aviso: ele some logo depois de aparecer.
      const naConversa = conversa && janelas.some((j) => j.focused && j.visibilityState === 'visible' && new URL(j.url).pathname === conversa);
      if (naConversa) (await self.registration.getNotifications({ tag })).forEach((n) => n.close());
    })(),
  );
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const link = String(event.notification.data?.link || '');
  const target = link ? `/?aesynPushLink=${encodeURIComponent(link)}` : '/';
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(async (list) => {
      const doSite = list.filter((x) => new URL(x.url).origin === self.location.origin);
      // App ja aberto (em segundo plano): traz para frente e vai para a conversa sem recarregar.
      const current = doSite.find((x) => x.focused) ?? doSite.find((x) => x.visibilityState === 'visible') ?? doSite[0];
      if (current) {
        try {
          await current.focus();
        } catch {
          /* alguns navegadores so deixam focar a partir do clique; a mensagem abaixo resolve mesmo assim */
        }
        current.postMessage({ type: 'AESYN_PUSH_OPEN', link });
        return;
      }
      // App fechado: abre do zero (com a abertura animada) direto na tela do link.
      return clients.openWindow(target);
    }),
  );
});
