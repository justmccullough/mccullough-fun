import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    open: true,
    watch: {
      // Unity projects churn (and lock) files while building; the dev server never needs them.
      ignored: ['**/unity/**'],
    },
  },
})
