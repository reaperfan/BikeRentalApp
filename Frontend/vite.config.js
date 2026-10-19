import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // A backend felé továbbítja az /api kéréseket, így nem kell CORS
    proxy: {
      '/api': 'http://localhost:5006',
    },
  },
})
