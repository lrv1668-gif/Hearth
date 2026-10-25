import { sveltekit } from '@sveltejs/kit/vite';
import tailwindcss from '@tailwindcss/vite';
import { createLogger } from 'vite';
import { defineConfig } from 'vitest/config';

// Vite only warns when a CSS url() can't be resolved and leaves it to 404 at
// runtime — that's how the self-hosted fonts once silently fell out of the
// prod build. Fail the build instead.
const logger = createLogger();
for (const level of ['warn', 'warnOnce'] as const) {
    const log = logger[level];
    logger[level] = (msg, options) => {
        if (msg.includes("didn't resolve at build time")) throw new Error(msg);
        log(msg, options);
    };
}

export default defineConfig({
    // Tailwind's Vite plugin (not @tailwindcss/postcss) so @imported CSS keeps
    // its own base for relative url()s — e.g. @fontsource's ./files/*.woff2
    plugins: [tailwindcss(), sveltekit()],
    customLogger: logger,
    test: {
        environment: 'node',
        include: ['src/**/*.{test,spec}.ts'],
    },
    server: {
        // The renderer sidecar loads /frame through Caddy as http://caddy
        allowedHosts: ['caddy'],
        proxy: {
            '/tasks': process.env.TASKS_URL ?? 'http://localhost:8081',
            '/spotify': process.env.SPOTIFY_URL ?? 'http://localhost:8083',
            '/weather': process.env.WEATHER_URL ?? 'http://localhost:8082',
            '/photos': process.env.PHOTOS_URL ?? 'http://localhost:8084',
            '/rss': process.env.RSS_URL ?? 'http://localhost:8085',
            '/quote': process.env.QUOTE_URL ?? 'http://localhost:8086',
            '/birds': process.env.BIRDS_URL ?? 'http://localhost:8088',
            '/almanac': process.env.ALMANAC_URL ?? 'http://localhost:8089',
            '/trains': process.env.TRAINS_URL ?? 'http://localhost:8091',
            '/sports': process.env.SPORTS_URL ?? 'http://localhost:8094',
            '/display': process.env.DISPLAY_URL ?? 'http://localhost:8090',
            '/calendar/google': process.env.CALENDAR_URL ?? 'http://localhost:8087',
            '/calendar/items': process.env.CALENDAR_URL ?? 'http://localhost:8087',
            '/calendar/health': process.env.CALENDAR_URL ?? 'http://localhost:8087',
            // Dozzle streams over SSE and websockets
            '/logs': { target: process.env.LOGS_URL ?? 'http://localhost:8093', ws: true },
        },
        hmr: process.env.HMR_CLIENT_PORT ? { clientPort: parseInt(process.env.HMR_CLIENT_PORT) } : undefined,
    },
});
