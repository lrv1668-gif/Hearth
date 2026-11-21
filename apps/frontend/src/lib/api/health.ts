import { get, tryFetch } from './http.ts';
import type { HearthServiceId } from '#lib/constants/services.ts';

export type HealthStatus = 'healthy' | 'degraded' | 'unhealthy';

/** GET /<service>/health: the service's overall status and each of its checks. */
export interface ServiceHealth {
    status: HealthStatus;
    checks: { name: string; status: HealthStatus; description: string | null }[];
}

export const logs = {
    /** Whether Dozzle answers at /logs — off unless the host sets COMPOSE_PROFILES=logs (Caddy then 502s). */
    running: async (): Promise<boolean> => (await tryFetch('/logs/', { timeoutMs: 5000 }))?.ok ?? false,
};

export const health = {
    /** null when the service doesn't answer — a problem it can see itself is still a 200. */
    service: (id: HearthServiceId): Promise<ServiceHealth | null> => get<ServiceHealth>(`/${id}/health`),
};
