/* What the e-paper panel can print cleanly: its six inks, and the two-ink mixes the
   renderer's ordered dithering (apps/renderer/dither.mjs) lays down as an even pattern.
   Shared by the frame theme builder and themes.test.ts so the two can't drift. */
import { FRAME_INKS } from '#lib/constants/frameThemes.ts';

export const INK_NAMES = ['Black', 'White', 'Yellow', 'Red', 'Blue', 'Green'] as const;

/** Mix ratios that land on whole 4×4 Bayer levels (4, 8 and 12 of 16). */
export const MIX_STEPS = [0.25, 0.5, 0.75] as const;

function rgb(hex: string): [number, number, number] {
    return [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16)) as [number, number, number];
}

function toHex(channels: number[]): string {
    return '#' + channels.map((c) => Math.round(c).toString(16).padStart(2, '0')).join('');
}

/** WCAG relative luminance. */
export function luminance(hex: string): number {
    const [r, g, b] = rgb(hex).map((v) => {
        const c = v / 255;
        return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    });
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

/** WCAG contrast ratio, 1–21. */
export function contrast(a: string, b: string): number {
    const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
    return (hi + 0.05) / (lo + 0.05);
}

/** CIE L* (0–100): how light an ink looks, for judging how a mix will dither. */
export function lightness(hex: string): number {
    const y = luminance(hex);
    return y > 216 / 24389 ? 116 * Math.cbrt(y) - 16 : (24389 / 27) * y;
}

/** `t` of the way from `a` to `b`, in plain sRGB — the space the renderer dithers in. */
export function mixHex(a: string, b: string, t: number): string {
    const [ca, cb] = [rgb(a), rgb(b)];
    return toHex(ca.map((v, i) => v + (cb[i] - v) * t));
}

/**
 * How a fill dithers on the panel:
 * - solid: a pure ink, printed as is
 * - even: two inks of near-equal lightness — a fine, quiet texture (cream, olive, teal)
 * - textured: a visible but regular pattern — fine for large fills (amber, navy, pink)
 * - speckled: inks far apart in lightness — reads as dots, not a colour
 */
export type Grain = 'solid' | 'even' | 'textured' | 'speckled';

export interface FrameFill {
    hex: string;
    label: string;
    grain: Grain;
}

// L* gaps between a mix's two inks: white–yellow 2, red–green 9, blue–green 13 mix
// evenly; yellow–red 35 (amber) and black–blue 41 (navy) are the furthest apart the
// built-in themes go.
const EVEN_GAP = 15;
const TEXTURED_GAP = 45;

export function grainOf(a: string, b: string): Grain {
    const gap = Math.abs(lightness(a) - lightness(b));
    return gap <= EVEN_GAP ? 'even' : gap <= TEXTURED_GAP ? 'textured' : 'speckled';
}

/** Every fill the panel can print: the six inks, then each pair of inks at 25/50/75%. */
export const FRAME_FILLS: readonly FrameFill[] = [
    ...FRAME_INKS.map((hex, i) => ({ hex, label: INK_NAMES[i], grain: 'solid' as const })),
    ...FRAME_INKS.flatMap((a, i) =>
        FRAME_INKS.slice(i + 1).flatMap((b, k) => {
            const j = i + 1 + k;
            return MIX_STEPS.map((t) => ({
                hex: mixHex(a, b, t),
                label: `${INK_NAMES[i]} + ${t * 100}% ${INK_NAMES[j].toLowerCase()}`,
                grain: grainOf(a, b),
            }));
        })
    ),
];

/** The printable fill closest to `hex` (RGB distance), and how far off it is. */
export function nearestFill(hex: string): { fill: FrameFill; distance: number } {
    const c = rgb(hex);
    let best = { fill: FRAME_FILLS[0], distance: Infinity };
    for (const fill of FRAME_FILLS) {
        const f = rgb(fill.hex);
        const distance = Math.hypot(c[0] - f[0], c[1] - f[1], c[2] - f[2]);
        if (distance < best.distance) best = { fill, distance };
    }
    return best;
}

export function fillFor(hex: string): FrameFill | undefined {
    return FRAME_FILLS.find((f) => f.hex === hex);
}

export function inkName(hex: string): string {
    const i = (FRAME_INKS as readonly string[]).indexOf(hex);
    return i >= 0 ? INK_NAMES[i] : (fillFor(hex)?.label ?? hex);
}
