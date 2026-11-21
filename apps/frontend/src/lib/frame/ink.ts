// Ambient's date plate: picking the ink that stands out from the photo around it.
import type { AmbientPosition } from '#lib/api.ts';

/* ── Ambient plate ink ────────────────────────────────────────────────────────
   The Ambient plate's date block is a solid pure-ink square on top of the photo.
   It stands out best in an ink the photo around it doesn't already use, so the
   frame samples the photo just outside the plate and picks, from the theme's own
   inks, the one farthest from those colours (in OKLab, so "far" means far to the eye). */

/** Where the plate sits inside an Ambient frame: mirrors FrameAmbient's .plate CSS. */
export const PLATE_INSET = 64;

export interface Rect {
    x: number;
    y: number;
    w: number;
    h: number;
}

/** The plate's box in frame pixels for an Ambient position, on a frame at `scale`. */
export function plateRect(
    position: AmbientPosition,
    plate: { w: number; h: number },
    frame: { w: number; h: number },
    scale = 1
): Rect {
    const [row, col] = position.split('-');
    const inset = PLATE_INSET * scale;
    const x = col === 'left' ? inset : col === 'right' ? frame.w - inset - plate.w : (frame.w - plate.w) / 2;
    const y = row === 'top' ? inset : frame.h - inset - plate.h;
    return { x, y, w: plate.w, h: plate.h };
}

type Oklab = [number, number, number];

/** sRGB (0–255) → OKLab (Björn Ottosson's matrices). */
export function oklab(r: number, g: number, b: number): Oklab {
    const lin = (c: number) => {
        c /= 255;
        return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    };
    const [lr, lg, lb] = [lin(r), lin(g), lin(b)];
    const l = Math.cbrt(0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb);
    const m = Math.cbrt(0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb);
    const s = Math.cbrt(0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb);
    return [
        0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s,
        1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s,
        0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s,
    ];
}

function hexRgb(hex: string): [number, number, number] {
    return [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16)) as [number, number, number];
}

function relativeLuminance(hex: string): number {
    const [r, g, b] = hexRgb(hex).map((c) => {
        c /= 255;
        return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    });
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

/** Black or white, whichever reads better set on `ink` (WCAG contrast). */
export function onBlockInk(ink: string): '#000000' | '#ffffff' {
    const lum = relativeLuminance(ink);
    return (lum + 0.05) / 0.05 >= 1.05 / (lum + 0.05) ? '#000000' : '#ffffff';
}

/** Pixels as RGBA bytes, row-major — the shape of a canvas ImageData. */
export interface Pixels {
    width: number;
    height: number;
    data: ArrayLike<number>;
}

/** How far around the plate (frame px at scale 1) counts as its surroundings. */
const RING = 48;
/** OKLab distance at which an ink reads as clearly apart from the photo around it. */
const STANDS_OUT = 0.25;
/** Below that, how much better another ink must score before the theme's own --block gives way. */
const KEEP_PREFERRED = 0.85;

/**
 * The block ink that stands out most against the photo around `plate`. `pixels` is
 * the photo as drawn on the frame (cover-cropped) at any scale; `frame` is the frame's
 * size, so `plate` maps onto it. Each candidate scores the distance to its 10th-
 * percentile nearest sample — low when a fair patch of the surroundings matches it.
 * The theme's `preferred` ink stays while it stands out, or unless another clearly beats it.
 * `scale` is the frame's (see frameCanvas), which widens or narrows the sampled ring.
 */
export function pickBlockInk(
    pixels: Pixels,
    frame: { w: number; h: number },
    plate: Rect,
    candidates: readonly string[],
    preferred: string,
    scale = 1
): string {
    if (candidates.length === 0) return preferred;
    const margin = RING * scale;
    const sx = pixels.width / frame.w;
    const sy = pixels.height / frame.h;
    const inside = (x: number, y: number, r: Rect) => x >= r.x && x < r.x + r.w && y >= r.y && y < r.y + r.h;
    const ring = { x: plate.x - margin, y: plate.y - margin, w: plate.w + 2 * margin, h: plate.h + 2 * margin };

    const samples: Oklab[] = [];
    const x0 = Math.max(0, Math.floor(ring.x * sx));
    const x1 = Math.min(pixels.width, Math.ceil((ring.x + ring.w) * sx));
    const y0 = Math.max(0, Math.floor(ring.y * sy));
    const y1 = Math.min(pixels.height, Math.ceil((ring.y + ring.h) * sy));
    for (let py = y0; py < y1; py++) {
        for (let px = x0; px < x1; px++) {
            if (inside((px + 0.5) / sx, (py + 0.5) / sy, plate)) continue;
            const i = (py * pixels.width + px) * 4;
            samples.push(oklab(pixels.data[i], pixels.data[i + 1], pixels.data[i + 2]));
        }
    }
    if (samples.length === 0) return preferred;

    const score = (ink: string): number => {
        const [L, a, b] = oklab(...hexRgb(ink));
        const d = samples.map(([l2, a2, b2]) => Math.hypot(L - l2, a - a2, b - b2)).sort((p, q) => p - q);
        return d[Math.floor(d.length * 0.1)];
    };
    const scored = candidates.map((ink) => ({ ink, score: score(ink) }));
    const best = scored.reduce((p, q) => (q.score > p.score ? q : p));
    const kept = scored.find((s) => s.ink === preferred);
    return kept && (kept.score >= STANDS_OUT || kept.score >= best.score * KEEP_PREFERRED) ? preferred : best.ink;
}
