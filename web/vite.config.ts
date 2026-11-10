import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// En développement, /api est relayé vers l'API .NET : pas de souci de CORS côté navigateur.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
})
