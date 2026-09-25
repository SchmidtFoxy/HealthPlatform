const HP_SW_CACHE = 'aesyn-static-v0.19.42';
const HP_SW_ASSETS = ['/', '/manifest.webmanifest', '/app.css?v=0.19.42', '/app.js?v=0.19.42', '/icons/icon-192.png', '/icons/icon-512.png', '/icons/apple-touch-icon.png'];
self.addEventListener('install', event => { event.waitUntil(caches.open(HP_SW_CACHE).then(cache => cache.addAll(HP_SW_ASSETS)).then(() => self.skipWaiting())); });
self.addEventListener('activate', event => { event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key.startsWith('aesyn-static-') && key !== HP_SW_CACHE).map(key => caches.delete(key)))).then(() => self.clients.claim())); });
self.addEventListener('fetch', event => {
  const request = event.request;
  if (request.method !== 'GET') return;
  const url = new URL(request.url);
  if (url.origin !== self.location.origin || url.pathname.startsWith('/api/')) return;
  if (request.mode === 'navigate') { event.respondWith(fetch(request).then(response => response).catch(() => caches.match('/'))); return; }
  if (!/\.(?:css|js|png|webmanifest)$/.test(url.pathname)) return;
  event.respondWith(caches.match(request).then(cached => cached || fetch(request).then(response => { if (response && response.ok) caches.open(HP_SW_CACHE).then(cache => cache.put(request, response.clone())); return response; })));
});


// ===== v0.19.39 — Push Notifications End-to-End =====
self.addEventListener('push', event => {
  let payload={title:'AESYN Performance',body:'Você tem uma nova atualização.',icon:'/icons/icon-192.png',badge:'/icons/icon-192.png',data:{link:''}};
  try { if(event.data) payload={...payload,...event.data.json()}; } catch { if(event.data) payload.body=event.data.text(); }
  const options={body:payload.body,icon:payload.icon||'/icons/icon-192.png',badge:payload.badge||'/icons/icon-192.png',tag:payload.tag||'aesyn-update',renotify:false,data:payload.data||{}};
  event.waitUntil(self.registration.showNotification(payload.title||'AESYN Performance',options));
});
self.addEventListener('notificationclick', event => {
  event.notification.close();
  const link=String(event.notification.data?.link||'');
  const target=link ? `/?aesynPushLink=${encodeURIComponent(link)}` : '/';
  event.waitUntil(clients.matchAll({type:'window',includeUncontrolled:true}).then(list=>{
    const current=list.find(x=>new URL(x.url).origin===self.location.origin);
    if(current){ current.focus(); current.postMessage({type:'AESYN_PUSH_OPEN',link}); return; }
    return clients.openWindow(target);
  }));
});
