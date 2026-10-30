import type { ServiceHealth } from '#lib/api.ts';

/**
 * ok: running. unset: running, but an optional integration's keys aren't set — a choice,
 * not a problem. attention: running, but the home location is missing or an upstream is
 * failing. down: broken or unreachable.
 */
export type HealthTone = 'ok' | 'unset' | 'attention' | 'down';

export interface HealthSummary {
    tone: HealthTone;
    /** One line for the service's row. */
    text: string;
    /** A line per check worth reading: the ones that aren't healthy, plus the frame's render and check-in. */
    details: string[];
}

// Healthy checks mostly say nothing new ("Readable", "Configured"); the frame's says when
// it last rendered and checked in, which is worth seeing either way.
const ALWAYS_SHOWN = new Set(['frame']);

/** A service's health report, or null when it didn't answer, as a row in Settings → Diagnostics. */
export function summarizeHealth(report: ServiceHealth | null): HealthSummary {
    if (!report) return { tone: 'down', text: 'Not responding', details: [] };

    const details = report.checks
        .filter((c) => c.status !== 'healthy' || ALWAYS_SHOWN.has(c.name))
        .map((c) => c.description ?? `${c.name}: ${c.status}`);
    const unhealthy = report.checks.filter((c) => c.status !== 'healthy');
    switch (report.status) {
        case 'healthy':
            return { tone: 'ok', text: 'Running', details };
        case 'degraded':
            // Only missing keys: every integration is optional, so this isn't a warning
            return unhealthy.every((c) => c.name === 'config')
                ? { tone: 'unset', text: 'Not set up', details }
                : { tone: 'attention', text: 'Needs attention', details };
        default:
            return { tone: 'down', text: 'Not working', details };
    }
}
