import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/register': 'http://localhost:5228',
      '/login': 'http://localhost:5228',
      '/images': 'http://localhost:5228'
    }
  }
})
