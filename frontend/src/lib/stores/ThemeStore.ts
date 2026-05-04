import { browser } from '$app/environment';
import { writable } from 'svelte/store';
import { DEFAULT_THEME, isValidThemeId, type ThemeId } from '../constants/themes';

function getInitialTheme(): ThemeId {
    if (!browser) return DEFAULT_THEME;
    const stored = localStorage.getItem('hearth-theme');
    return isValidThemeId(stored) ? stored : DEFAULT_THEME;
}

export const theme = writable<ThemeId>(getInitialTheme());

export function initTheme() {
    // Set the initial theme on the site
    const currentTheme = getInitialTheme();
    document.documentElement.dataset.theme = currentTheme;

    // Subscribe to future changes
    if (browser) {
        theme.subscribe((value) => {
            localStorage.setItem('hearth-theme', value);
            document.documentElement.dataset.theme = value;
        });
    }
}

export function setTheme(id: ThemeId) {
    theme.set(id);
}
