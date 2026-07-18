import { browser } from '$app/environment';
import { DEFAULT_FONT_THEME, isValidFontThemeId, type FontThemeId } from '../constants/fontThemes';

function getInitialFontTheme(): FontThemeId {
    if (!browser) return DEFAULT_FONT_THEME;
    const stored = localStorage.getItem('hearth-font');
    return isValidFontThemeId(stored) ? stored : DEFAULT_FONT_THEME;
}

export const fontThemeStore = $state({ fontTheme: getInitialFontTheme() });

export function initFontTheme() {
    document.documentElement.dataset.font = fontThemeStore.fontTheme;
}

export function setFontTheme(id: FontThemeId) {
    fontThemeStore.fontTheme = id;
    if (browser) {
        localStorage.setItem('hearth-font', id);
        document.documentElement.dataset.font = id;
    }
}
