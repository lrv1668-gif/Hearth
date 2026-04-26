import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
    plugins: [sveltekit()],
    server: {
        proxy: {
            '/tasks':   process.env.TASKS_URL   ?? 'http://localhost:8081',
            '/spotify': process.env.SPOTIFY_URL ?? 'http://localhost:8083',
        },
        hmr: process.env.HMR_CLIENT_PORT
            ? { clientPort: parseInt(process.env.HMR_CLIENT_PORT) }
            : undefined,
    }
});
