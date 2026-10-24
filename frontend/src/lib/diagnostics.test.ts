import { describe, expect, it } from 'vitest';
import { summarizeHealth } from './diagnostics';

describe('summarizeHealth', () => {
    it('treats no answer as down', () => {
        expect(summarizeHealth(null)).toEqual({ tone: 'down', text: 'Not responding', details: [] });
    });

    it('hides the details of healthy checks', () => {
        const summary = summarizeHealth({
            status: 'healthy',
            checks: [{ name: 'database', status: 'healthy', description: 'Readable' }],
        });
        expect(summary).toEqual({ tone: 'ok', text: 'Running', details: [] });
    });

    it('calls a service missing only its keys not set up', () => {
        const summary = summarizeHealth({
            status: 'degraded',
            checks: [
                { name: 'database', status: 'healthy', description: 'Readable' },
                { name: 'config', status: 'degraded', description: 'Not set: EBIRD_API_KEY' },
            ],
        });
        expect(summary).toEqual({ tone: 'unset', text: 'Not set up', details: ['Not set: EBIRD_API_KEY'] });
    });

    it('lists the checks that need attention', () => {
        const summary = summarizeHealth({
            status: 'degraded',
            checks: [
                { name: 'config', status: 'degraded', description: 'Not set: EBIRD_API_KEY' },
                { name: 'upstream', status: 'degraded', description: 'Can’t refresh birds' },
            ],
        });
        expect(summary).toEqual({
            tone: 'attention',
            text: 'Needs attention',
            details: ['Not set: EBIRD_API_KEY', 'Can’t refresh birds'],
        });
    });

    it('always shows the frame check', () => {
        const summary = summarizeHealth({
            status: 'healthy',
            checks: [{ name: 'frame', status: 'healthy', description: 'Rendered 2 min ago' }],
        });
        expect(summary.details).toEqual(['Rendered 2 min ago']);
    });

    it('falls back to the check name when there is no description', () => {
        const summary = summarizeHealth({
            status: 'unhealthy',
            checks: [{ name: 'database', status: 'unhealthy', description: null }],
        });
        expect(summary).toEqual({ tone: 'down', text: 'Not working', details: ['database: unhealthy'] });
    });
});
