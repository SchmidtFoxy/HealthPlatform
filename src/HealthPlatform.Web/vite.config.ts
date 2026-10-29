import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';
import { readFileSync } from 'node:fs';
import { fileURLToPath, URL } from 'node:url';

const versao = readFileSync(new URL('../../VERSION.txt', import.meta.url), 'utf8').trim();

// O app e servido pela propria API na raiz do site. O build vai inteiro para wwwroot
// (public/ traz os icones, o push-sw.js e o /app/sw.js que desliga o service worker antigo).
export default defineConfig({
  base: '/',
  define: { __APP_VERSION__: JSON.stringify(versao) },
  resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      injectRegister: false,
      scope: '/',
      manifest: {
        // id antigo mantido: celulares com o app instalado continuam reconhecendo o mesmo app.
        id: '/app/',
        name: 'AESYN',
        short_name: 'AESYN',
        description: 'Treino, nutrição e acompanhamento em um só lugar.',
        lang: 'pt-BR',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        background_color: '#061210',
        theme_color: '#061210',
        icons: [
          { src: '/icons/icon-192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
          { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
          { src: '/icons/maskable-192.png', sizes: '192x192', type: 'image/png', purpose: 'maskable' },
          { src: '/icons/maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        navigateFallback: '/index.html',
        navigateFallbackDenylist: [/^\/api\//, /^\/app\//, /^\/swagger/],
        globPatterns: ['**/*.{js,css,html,woff2,svg}', 'abertura-simbolo.webp'],
        globIgnores: ['app/**', 'push-sw.js'],
        importScripts: ['/push-sw.js'],
      },
    }),
  ],
  build: {
    outDir: '../HealthPlatform.Api/wwwroot',
    emptyOutDir: true,
    chunkSizeWarningLimit: 800,
  },
  server: {
    port: 5173,
    proxy: { '/api': 'http://127.0.0.1:5080', '/icons': 'http://127.0.0.1:5080' },
  },
});
