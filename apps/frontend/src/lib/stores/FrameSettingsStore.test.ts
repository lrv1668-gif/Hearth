import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mockFetch } from '#lib/test/fetch.ts';
import { frameSettings, LEGACY_FRAME_SETTINGS_KEY, loadFrameSettings } from './FrameSettingsStore.svelte.ts';

vi.mock('$app/env', () => ({ browser: true }));

const { fetchMock, respond, unreachable, call } = mockFetch();

// Re-stubbed per test: mockFetch's afterEach unstubs every global
const storage = new Map<string, string>();

beforeEach(() => {
    storage.clear();
    vi.stubGlobal('localStorage', {
        getItem: (key: string) => storage.get(key) ?? null,
        setItem: (key: string, value: string) => storage.set(key, value),
        removeItem: (key: string) => storage.delete(key),
    });
    frameSettings.error = false;
    frameSettings.loaded = false;
});

describe('loadFrameSettings', () => {
    it('flags an error when Display is unreachable', async () => {
        unreachable();
        await loadFrameSettings();
        expect(frameSettings.error).toBe(true);
        expect(frameSettings.loaded).toBe(true);
    });

    it('applies the saved settings', async () => {
        respond(200, { ...frameSettings.snapshot(), template: 'columns' });
        await loadFrameSettings();
        expect(frameSettings.snapshot().template).toBe('columns');
        expect(frameSettings.error).toBe(false);
    });

    it('uploads legacy browser settings once when none are saved', async () => {
        storage.set(LEGACY_FRAME_SETTINGS_KEY, JSON.stringify({}));
        respond(204);
        respond(200, frameSettings.snapshot());
        await loadFrameSettings();

        expect(call(1).init?.method).toBe('PUT');
        // A pre-template snapshot keeps its columns layout
        expect(JSON.parse(String(call(1).init?.body)).template).toBe('columns');
        expect(storage.has(LEGACY_FRAME_SETTINGS_KEY)).toBe(false);
    });

    it('keeps the legacy copy when the upload fails', async () => {
        storage.set(LEGACY_FRAME_SETTINGS_KEY, JSON.stringify({}));
        respond(204);
        respond(500);
        await loadFrameSettings();

        expect(storage.has(LEGACY_FRAME_SETTINGS_KEY)).toBe(true);
    });

    it('sends nothing when there is no legacy copy', async () => {
        respond(204);
        await loadFrameSettings();
        expect(fetchMock).toHaveBeenCalledOnce();
    });
});
