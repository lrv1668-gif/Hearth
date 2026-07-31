import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vitest/config';

export default defineConfig({
    plugins: [sveltekit()],
    test: {
        environment: 'node',
        include: ['src/**/*.{test,spec}.ts'],
    },
    server: {
        proxy: {
            '/tasks': process.env.TASKS_URL ?? 'http://localhost:8081',
            '/spotify': process.env.SPOTIFY_URL ?? 'http://localhost:8083',
            '/weather': process.env.WEATHER_URL ?? 'http://localhost:8082',
            '/photos': process.env.PHOTOS_URL ?? 'http://localhost:8084',
            '/rss': process.env.RSS_URL ?? 'http://localhost:8085',
            '/quote': process.env.QUOTE_URL ?? 'http://localhost:8086',
            '/birds': process.env.BIRDS_URL ?? 'http://localhost:8088',
            '/almanac': process.env.ALMANAC_URL ?? 'http://localhost:8089',
            '/plants/': process.env.PLANTS_URL ?? 'http://localhost:8090',
            '/calendar/google': process.env.CALENDAR_URL ?? 'http://localhost:8087',
            '/calendar/items': process.env.CALENDAR_URL ?? 'http://localhost:8087',
        },
        hmr: process.env.HMR_CLIENT_PORT ? { clientPort: parseInt(process.env.HMR_CLIENT_PORT) } : undefined,
    },
});
