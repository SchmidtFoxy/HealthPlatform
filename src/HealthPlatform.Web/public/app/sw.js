// O app saiu de /app e agora fica na raiz do site. Este service worker substitui o antigo de /app:
// apaga o cache dele (os nomes do Workbox levam o escopo /app/), se desregistra e recarrega as abas, que o servidor manda para o endereco novo.
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      const chaves = await caches.keys();
      await Promise.all(chaves.filter((k) => k.includes('/app/')).map((k) => caches.delete(k)));
      await self.registration.unregister();
      const abas = await self.clients.matchAll({ type: 'window' });
      abas.forEach((aba) => aba.navigate(aba.url).catch(() => {}));
    })(),
  );
});
