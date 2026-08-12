import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'

// The dev server port is fixed so the API's CORS allow-list is deterministic.
// Aspire injects PORT; we honour it but default to 5173.
const port = Number(process.env.PORT) || 5173

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      // Register the worker from src/pwa.ts so we can auto-reload open tabs on update.
      injectRegister: null,
      includeAssets: ['favicon.svg'],
      manifest: {
        name: 'Briefcase',
        short_name: 'Briefcase',
        description: 'Your stuff. Everywhere.',
        theme_color: '#0EA5E9',
        background_color: '#F0F0F0',
        display: 'standalone',
        start_url: '/',
        icons: [
          { src: 'favicon.svg', sizes: 'any', type: 'image/svg+xml' },
        ],
      },
    }),
  ],
  server: {
    port,
    strictPort: true,
    host: true,
  },
  preview: {
    port,
    strictPort: true,
  },
})
