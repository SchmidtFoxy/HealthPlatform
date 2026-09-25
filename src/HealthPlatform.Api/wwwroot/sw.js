const HP_SW_CACHE = 'aesyn-static-v0.19.38';
const HP_SW_ASSETS = ['/', '/manifest.webmanifest', '/app.css?v=0.19.38', '/app.js?v=0.19.38', '/icons/icon-192.png', '/icons/icon-512.png', '/icons/apple-touch-icon.png'];
self.addEventListener('install', event => { event.waitUntil(caches.open(HP_SW_CACHE).then(cache => cache.addAll(HP_SW_ASSETS)).then(() => self.skipWaiting())); });
self.addEventListener('activate', event => { event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key.startsWith('aesyn-static-') && key !== HP_SW_CACHE).map(key => caches.delete(key)))).then(() => self.clients.claim())); });
self.addEventListener('fetch', event => {
  const request = event.request;
  if (request.method !== 'GET') return;
  const url = new URL(request.url);
  if (url.origin !== self.location.origin || url.pathname.startsWith('/api/')) return;
  if (request.mode === 'navigate') { event.respondWith(fetch(request).catch(() => caches.match('/'))); return; }
  if (!/\.(?:css|js|png|webmanifest)$/.test(url.pathname)) return;
  event.respondWith(caches.match(request).then(cached => cached || fetch(request).then(response => { if (response && response.ok) caches.open(HP_SW_CACHE).then(cache => cache.put(request, response.clone())); return response; })));
});
