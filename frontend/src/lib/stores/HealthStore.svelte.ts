import { api } from '$lib/api';
import { healthServices, type HealthService } from '$lib/constants/widgets';

export type ServiceHealth =
    | { status: 'unknown' }
    | { status: 'configured' }
    | { status: 'unconfigured'; missing: string[] }
    | { status: 'unreachable' };

const serviceIds = Object.keys(healthServices) as HealthService[];

class HealthStore {
    services = $state<Record<HealthService, ServiceHealth>>(
        Object.fromEntries(serviceIds.map((s) => [s, { status: 'unknown' }])) as Record<HealthService, ServiceHealth>
    );
}

export const healthStore = new HealthStore();

// True whenever the service can't be enabled: unconfigured, unreachable, or not yet checked.
export function serviceBlocked(service: HealthService): boolean {
    return healthStore.services[service].status !== 'configured';
}

export function serviceHint(service: HealthService): string | null {
    const health = healthStore.services[service];
    if (health.status === 'unconfigured')
        return `Needs ${health.missing.join(', ')} — add to ${healthServices[service].envPath} and restart.`;
    if (health.status === 'unreachable') return "Service unreachable — check that Hearth's backend is running.";
    return null;
}

export async function loadHealth() {
    await Promise.all(
        serviceIds.map(async (service) => {
            const res = await api.health.check(service);
            healthStore.services[service] =
                res === null
                    ? { status: 'unreachable' }
                    : res.configured
                      ? { status: 'configured' }
                      : { status: 'unconfigured', missing: res.missing };
        })
    );
}
