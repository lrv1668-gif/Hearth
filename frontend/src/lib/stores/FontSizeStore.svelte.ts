import { browser } from '$app/environment';

export const MIN_FONT_SCALE = 0.9;
export const MAX_FONT_SCALE = 1.3;
const DEFAULT_FONT_SCALE = 1;

function isValidFontScale(value: number): boolean {
    return Number.isFinite(value) && value >= MIN_FONT_SCALE && value <= MAX_FONT_SCALE;
}

function getInitialFontScale(): number {
    if (!browser) return DEFAULT_FONT_SCALE;
    const stored = Number(localStorage.getItem('hearth-font-size'));
    return isValidFontScale(stored) ? stored : DEFAULT_FONT_SCALE;
}

export const fontSizeStore = $state({ scale: getInitialFontScale() });

function apply(scale: number) {
    document.documentElement.style.setProperty('--font-user-scale', String(scale));
}

export function initFontSize() {
    apply(fontSizeStore.scale);
}

export function setFontSize(scale: number) {
    if (!isValidFontScale(scale)) return;
    fontSizeStore.scale = scale;
    if (browser) {
        localStorage.setItem('hearth-font-size', String(scale));
        apply(scale);
    }
}
