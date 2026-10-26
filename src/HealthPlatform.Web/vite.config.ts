import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';
import { fileURLToPath, URL } from 'node:url';

// O novo front e servido pela propria API em /app, ao lado do front classico.
export default defineConfig({
  base: '/app/',
  resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      scope: '/app/',
      manifest: {
        id: '/app/',
        name: 'AESYN',
        short_name: 'AESYN',
        description: 'Treino, nutrição e acompanhamento em um só lugar.',
        lang: 'pt-BR',
        start_url: '/app/',
        scope: '/app/',
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
        navigateFallback: '/app/index.html',
        navigateFallbackAllowlist: [/^\/app/],
        globPatterns: ['**/*.{js,css,html,woff2,svg}'],
      },
    }),
  ],
  build: {
    outDir: '../HealthPlatform.Api/wwwroot/app',
    emptyOutDir: true,
    chunkSizeWarningLimit: 800,
  },
  server: {
    port: 5173,
    proxy: { '/api': 'http://127.0.0.1:5080', '/icons': 'http://127.0.0.1:5080' },
  },
});
