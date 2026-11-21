import { describe, expect, it } from 'vitest';
import {
    customThemeTokens,
    decodeCustomTheme,
    DEFAULT_CUSTOM_THEME,
    encodeCustomTheme,
    isCustomFrameTheme,
    rate,
    rateTheme,
} from './customTheme.ts';

const theme = DEFAULT_CUSTOM_THEME;

describe('rate', () => {
    it('rates the default theme with no avoids', () => {
        expect(rateTheme(theme).filter((r) => r.rating.level === 'avoid')).toEqual([]);
    });

    it('steers type away from faint inks', () => {
        expect(rate(theme, 'text', '#000000').level).toBe('good');
        expect(rate(theme, 'text', '#5e5b91').level).toBe('ok');
        expect(rate(theme, 'text', '#ffff6e').level).toBe('avoid');
        expect(rate(theme, 'text', '#ffffff').level).toBe('avoid');
    });

    it('allows green secondary type on white only at frame sizes, and not red', () => {
        expect(rate(theme, 'secondary', '#5a8d6c').level).toBe('ok');
        expect(rate(theme, 'secondary', '#f16f74').level).toBe('avoid');
    });

    it('grades fills by grain and by the type on them', () => {
        expect(rate(theme, 'surface', '#ffffff').level).toBe('good'); // plain paper
        expect(rate(theme, 'surface', '#ffffdb').level).toBe('good'); // even
        expect(rate(theme, 'golden', '#f8b771').level).toBe('ok'); // textured
        expect(rate(theme, 'golden', '#bfbfd3').level).toBe('avoid'); // white + 25% blue speckles
        expect(rate(theme, 'surface', '#5e5b91').level).toBe('avoid'); // black type on blue
    });

    it('reads the rest of the theme on a dark paper', () => {
        const night = { ...theme, paper: '#000000', text: '#ffffff', surface: '#000000', band: '#000000' };
        expect(rate(night, 'secondary', '#ffff6e').level).toBe('good');
        expect(rate(night, 'rule', '#000000').level).toBe('avoid');
    });
});

describe('customThemeTokens', () => {
    it('sets legible ink on the block and the accent', () => {
        const tokens = customThemeTokens(theme);
        expect(tokens['on-block']).toBe('#ffffff');
        expect(tokens['accent-fg']).toBe('#000000');
    });

    it('outlines daylight only when it is the paper colour', () => {
        expect(customThemeTokens(theme)['arc-outline']).toBeUndefined();
        expect(customThemeTokens({ ...theme, band: '#ffffff' })['arc-outline']).toBe(theme.rule);
    });
});

describe('?colors=', () => {
    it('round-trips', () => {
        expect(decodeCustomTheme(encodeCustomTheme(theme))).toEqual(theme);
    });

    it('rejects anything but allowed colours', () => {
        expect(decodeCustomTheme('ffffff')).toBeNull();
        expect(decodeCustomTheme(encodeCustomTheme({ ...theme, text: '#123456' }))).toBeNull();
        expect(decodeCustomTheme(encodeCustomTheme({ ...theme, paper: '#ffff6e' }))).toBeNull();
        expect(decodeCustomTheme(encodeCustomTheme(theme).replace('ffffff', 'ff;}x'))).toBeNull();
    });
});

describe('isCustomFrameTheme', () => {
    it('rejects mixes in ink roles', () => {
        expect(isCustomFrameTheme(theme)).toBe(true);
        expect(isCustomFrameTheme({ ...theme, text: '#ffffdb' })).toBe(false);
        expect(isCustomFrameTheme(null)).toBe(false);
    });
});
