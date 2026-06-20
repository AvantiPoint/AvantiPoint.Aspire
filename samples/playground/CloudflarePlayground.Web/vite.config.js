import { defineConfig } from 'vite';

// The AppHost injects the API URL as VITE_API_URL (run mode: the local API endpoint).
// `define` guarantees the value reaches client code regardless of how env vars are surfaced.
const apiUrl = process.env.VITE_API_URL || process.env.API_URL || '';

export default defineConfig({
  define: {
    'import.meta.env.VITE_API_URL': JSON.stringify(apiUrl),
  },
  server: {
    host: true,
  },
  build: {
    outDir: 'dist',
  },
});
