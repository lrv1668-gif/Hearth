import { browser } from '$app/environment';
import { DEFAULT_THEME, isValidThemeId, type ThemeId } from '../constants/themes';

function getInitialTheme(): ThemeId {
    if (!browser) return DEFAULT_THEME;
    const stored = localStorage.getItem('hearth-theme');
    return isValidThemeId(stored) ? stored : DEFAULT_THEME;
}

export const themeStore = $state({ theme: getInitialTheme() });

export function initTheme() {
    document.documentElement.dataset.theme = themeStore.theme;
}

export function setTheme(id: ThemeId) {
    themeStore.theme = id;
    if (browser) {
        localStorage.setItem('hearth-theme', id);
        document.documentElement.dataset.theme = id;
    }
}
