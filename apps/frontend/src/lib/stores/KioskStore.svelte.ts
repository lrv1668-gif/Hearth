import { page } from '$app/state';
import { settings } from './SettingsStore.svelte.ts';

export const kioskStore = {
    get isKiosk() {
        return settings.kioskMode || page.url.searchParams.get('kiosk') === '1';
    },
};
