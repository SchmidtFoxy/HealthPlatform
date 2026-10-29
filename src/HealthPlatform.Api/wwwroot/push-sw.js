// Notificacoes push do AESYN. Importado pelo service worker do app (/sw.js, gerado pelo Vite PWA),
// que fica no escopo da raiz: assim as inscricoes feitas em versoes anteriores continuam valendo.
self.addEventListener('push', (event) => {
  let payload = { title: 'AESYN Performance', body: 'Você tem uma nova atualização.', icon: '/icons/icon-192.png', badge: '/icons/icon-192.png', data: { link: '' } };
  try {
    if (event.data) payload = { ...payload, ...event.data.json() };
  } catch {
    if (event.data) payload.body = event.data.text();
  }
  const link = String(payload.data?.link || '');
  const chatTag = link.startsWith('chat') ? `aesyn-${link.replace(/[^a-zA-Z0-9:_-]/g, '')}` : '';
  const options = {
    body: payload.body,
    icon: payload.icon || '/icons/icon-192.png',
    badge: payload.badge || '/icons/icon-192.png',
    tag: payload.tag || chatTag || 'aesyn-update',
    renotify: false,
    data: payload.data || {},
  };
  event.waitUntil(self.registration.showNotification(payload.title || 'AESYN Performance', options));
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const link = String(event.notification.data?.link || '');
  const target = link ? `/?aesynPushLink=${encodeURIComponent(link)}` : '/';
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then((list) => {
      const current = list.find((x) => new URL(x.url).origin === self.location.origin);
      if (current) {
        current.focus();
        current.postMessage({ type: 'AESYN_PUSH_OPEN', link });
        return;
      }
      return clients.openWindow(target);
    }),
  );
});
