/* The frame's Custom theme: a handful of colour roles the builder in Settings → Frame
   picks from what the panel prints cleanly (lib/frame/inks.ts), rated as they're chosen,
   and expanded here into the full set of frame tokens. Saved by the Display service as
   `custom_theme`; /frame applies it inline over the `epaper-custom` fallback block. */
import { FRAME_INKS } from '#lib/constants/frameThemes.ts';
import { contrast, FRAME_FILLS, fillFor, nearestFill } from './inks.ts';

export const PAPERS = ['#ffffff', '#000000'] as const;

export const CUSTOM_ROLES = [
    'paper',
    'text',
    'secondary',
    'rule',
    'accent',
    'block',
    'surface',
    'band',
    'golden',
] as const;
export type CustomRole = (typeof CUSTOM_ROLES)[number];
export type CustomFrameTheme = Record<CustomRole, string>;

type RoleKind = 'paper' | 'ink' | 'fill';

export const ROLE_INFO: Record<CustomRole, { label: string; hint: string; kind: RoleKind }> = {
    paper: { label: 'Paper', hint: 'The background', kind: 'paper' },
    text: { label: 'Type', hint: 'Headings, times and list items', kind: 'ink' },
    secondary: { label: 'Secondary type', hint: 'Details, captions and icons', kind: 'ink' },
    rule: { label: 'Rules', hint: 'Dividers and outlines', kind: 'ink' },
    accent: { label: 'Accent', hint: 'Today, now and other marks', kind: 'ink' },
    block: { label: 'Block', hint: 'The date block and other solid shapes', kind: 'ink' },
    surface: { label: 'Tint', hint: 'Cards and the forecast band', kind: 'fill' },
    band: { label: 'Band', hint: 'Highlighted rows and daylight on the Day Arc', kind: 'fill' },
    golden: { label: 'Golden hour', hint: 'Sunrise and sunset on the Day Arc', kind: 'fill' },
};

/** Almanac's colours — what Custom starts from. */
export const DEFAULT_CUSTOM_THEME: CustomFrameTheme = {
    paper: '#ffffff',
    text: '#000000',
    secondary: '#5e5b91',
    rule: '#5e5b91',
    accent: '#f16f74',
    block: '#5e5b91',
    surface: '#ffffdb',
    band: '#ffffb7',
    golden: '#f8b771',
};

/** The colours a role may take: paper is white or black, lines and type a pure ink
 *  (a dithered mix breaks thin strokes apart), fills any printable fill. */
export function roleOptions(role: CustomRole): readonly string[] {
    const kind = ROLE_INFO[role].kind;
    if (kind === 'paper') return PAPERS;
    if (kind === 'ink') return FRAME_INKS;
    return FRAME_FILLS.map((f) => f.hex);
}

export function isCustomFrameTheme(value: unknown): value is CustomFrameTheme {
    if (!value || typeof value !== 'object') return false;
    const t = value as Record<string, unknown>;
    return CUSTOM_ROLES.every((r) => typeof t[r] === 'string' && roleOptions(r).includes(t[r] as string));
}

// ── Ratings ────────────────────────────────────────────────────────────────────

export type RatingLevel = 'good' | 'ok' | 'avoid';
export interface Rating {
    level: RatingLevel;
    reason: string;
}

const ratio = (n: number) => `${n.toFixed(1)}:1`;
const good = (reason: string): Rating => ({ level: 'good', reason });
const ok = (reason: string): Rating => ({ level: 'ok', reason });
const avoid = (reason: string): Rating => ({ level: 'avoid', reason });

/** The weakest contrast `ink` has against the paper and both fills. */
function worstOnBackgrounds(t: CustomFrameTheme, ink: string): number {
    return Math.min(contrast(ink, t.paper), contrast(ink, t.surface), contrast(ink, t.band));
}

/** How `hex` would look in `role`, given the rest of `theme`. */
export function rate(theme: CustomFrameTheme, role: CustomRole, hex: string): Rating {
    const t = { ...theme, [role]: hex };
    switch (role) {
        case 'paper':
            return hex === '#000000'
                ? good('A dark frame — black & white mode shows white on black')
                : good('The panel’s brightest white');
        case 'text': {
            if (hex === t.paper) return avoid('Same as the paper — type would vanish');
            const c = worstOnBackgrounds(t, hex);
            if (c >= 7) return good(`${ratio(c)} — crisp at every size`);
            if (c >= 4.5) return ok(`${ratio(c)} — readable, but softer than black or white`);
            return avoid(`Only ${ratio(c)} on the paper or a fill — too faint for reading type`);
        }
        case 'secondary': {
            if (hex === t.paper) return avoid('Same as the paper — details would vanish');
            const c = worstOnBackgrounds(t, hex);
            if (c >= 4.5) return good(`${ratio(c)} — readable at any size`);
            if (c >= 3) return ok(`${ratio(c)} — fine at frame sizes, not for small print`);
            return avoid(`Only ${ratio(c)} — details and captions would be hard to read`);
        }
        case 'rule': {
            if (hex === t.paper) return avoid('Same as the paper — rules would disappear');
            const c = contrast(hex, t.paper);
            if (c >= 3) return good(`${ratio(c)} on the paper — clear lines`);
            if (c >= 1.5) return ok(`${ratio(c)} — faint, so rules will be subtle`);
            return avoid(`Only ${ratio(c)} — rules would barely show`);
        }
        case 'accent': {
            if (hex === t.paper) return avoid('Same as the paper — marks would vanish');
            const c = worstOnBackgrounds(t, hex);
            if (hex === t.text) return ok('Same as the type — today and now won’t stand out');
            if (c >= 2.5) return good(`${ratio(c)} — stands out for marks and big numerals`);
            if (c >= 1.5) return ok(`${ratio(c)} — for marks and large numerals only`);
            return avoid(`Only ${ratio(c)} on the paper or a fill — marks would wash out`);
        }
        case 'block': {
            if (hex === t.paper) return avoid('Same as the paper — the date block would vanish');
            const c = contrast(hex, t.paper);
            if (c >= 2.5) return good(`${ratio(c)} on the paper — a bold solid shape`);
            if (c >= 1.5) return ok(`${ratio(c)} — the date block will look washed out`);
            return avoid(`Only ${ratio(c)} — the date block would barely show`);
        }
        case 'surface':
        case 'band':
        case 'golden':
            return rateFill(t, role, hex);
    }
}

function rateFill(t: CustomFrameTheme, role: CustomRole, hex: string): Rating {
    const fill = fillFor(hex);
    if (!fill) return avoid('Not a colour the panel can print');
    if (role !== 'golden') {
        const text = contrast(t.text, hex);
        const secondary = contrast(t.secondary, hex);
        if (text < 4.5) return avoid(`Type is only ${ratio(text)} on it`);
        if (secondary < 3) return avoid(`Secondary type is only ${ratio(secondary)} on it`);
    }
    if (hex === t.paper) return good('No tint — plain paper');
    switch (fill.grain) {
        case 'solid':
            return good('A pure ink — prints solid, no dithering');
        case 'even':
            return good('Inks of similar lightness — dithers into a fine, even texture');
        case 'textured':
            return ok('Dithers into a visible pattern — fine for large fills');
        case 'speckled':
            return avoid('Inks far apart in lightness — dithers into speckle, not a colour');
    }
}

/** Every role's rating in the theme as it stands. */
export function rateTheme(t: CustomFrameTheme): { role: CustomRole; rating: Rating }[] {
    return CUSTOM_ROLES.map((role) => ({ role, rating: rate(t, role, t[role]) }));
}

// ── Tokens ─────────────────────────────────────────────────────────────────────

/** The pure ink to set on `bg`: the theme's type or paper when either reads at 4.5:1,
 *  else whichever ink has the most contrast. */
function inkOn(bg: string, t: CustomFrameTheme): string {
    for (const ink of [t.text, t.paper]) if (ink !== bg && contrast(ink, bg) >= 4.5) return ink;
    return [...FRAME_INKS].sort((a, b) => contrast(b, bg) - contrast(a, bg))[0];
}

/** The full set of frame tokens (`--bg`, `--text-1`, …) for a custom theme. */
export function customThemeTokens(t: CustomFrameTheme): Record<string, string> {
    return {
        bg: t.paper,
        surface: t.surface,
        'surface-hi': t.band,
        border: t.rule,
        'text-1': t.text,
        'text-2': t.secondary,
        'text-3': t.secondary,
        'text-4': t.secondary,
        done: t.secondary,
        'done-bg': t.secondary,
        accent: t.accent,
        'accent-hi': t.accent,
        'accent-fg': inkOn(t.accent, t),
        block: t.block,
        'on-block': inkOn(t.block, t),
        'arc-night': t.block,
        'arc-day': t.band,
        'arc-golden': t.golden,
        // Daylight in the paper's own colour needs an outline to show
        ...(t.band === t.paper ? { 'arc-outline': t.rule } : {}),
    };
}

/** Every token customThemeTokens can set (arc-outline only sometimes). */
export const CUSTOM_TOKENS = [...Object.keys(customThemeTokens(DEFAULT_CUSTOM_THEME)), 'arc-outline'];

/** The tokens as an inline style declaration list. */
export function customThemeStyle(t: CustomFrameTheme): string {
    return Object.entries(customThemeTokens(t))
        .map(([k, v]) => `--${k}: ${v}`)
        .join('; ');
}

/** A theme's colours for its picker swatches: paper first. */
export function customThemeSwatches(t: CustomFrameTheme): string[] {
    return [...new Set([t.paper, t.text, t.secondary, t.accent, t.block, t.surface, t.band, t.golden])];
}

/** Roles read back from a theme's tokens (a built-in frame theme, to start from);
 *  colours the builder doesn't offer are snapped to the closest it does. */
export function customThemeFromTokens(tokens: Record<string, string>): CustomFrameTheme {
    const ink = (hex: string | undefined, fallback: string) => {
        const v = hex?.trim().toLowerCase();
        return v && (FRAME_INKS as readonly string[]).includes(v) ? v : fallback;
    };
    const fill = (hex: string | undefined, fallback: string) => {
        const v = hex?.trim().toLowerCase();
        return v && /^#[0-9a-f]{6}$/.test(v) ? nearestFill(v).fill.hex : fallback;
    };
    const paper = tokens['bg']?.trim().toLowerCase() === '#000000' ? '#000000' : '#ffffff';
    return {
        paper,
        text: ink(tokens['text-1'], paper === '#000000' ? '#ffffff' : '#000000'),
        secondary: ink(tokens['text-2'], DEFAULT_CUSTOM_THEME.secondary),
        rule: ink(tokens['border'], DEFAULT_CUSTOM_THEME.rule),
        accent: ink(tokens['accent'], DEFAULT_CUSTOM_THEME.accent),
        block: ink(tokens['block'], DEFAULT_CUSTOM_THEME.block),
        surface: fill(tokens['surface'], paper),
        band: fill(tokens['surface-hi'], paper),
        golden: fill(tokens['arc-golden'], DEFAULT_CUSTOM_THEME.golden),
    };
}

// ── URL ────────────────────────────────────────────────────────────────────────

/** `?colors=` on /frame: the roles in order, without '#'. */
export function encodeCustomTheme(t: CustomFrameTheme): string {
    return CUSTOM_ROLES.map((r) => t[r].slice(1)).join(',');
}

/** Reads `?colors=`; null unless every role is a colour the builder allows. */
export function decodeCustomTheme(param: string | null): CustomFrameTheme | null {
    if (!param) return null;
    const parts = param.toLowerCase().split(',');
    if (parts.length !== CUSTOM_ROLES.length) return null;
    const theme = Object.fromEntries(CUSTOM_ROLES.map((r, i) => [r, `#${parts[i]}`]));
    return isCustomFrameTheme(theme) ? theme : null;
}
