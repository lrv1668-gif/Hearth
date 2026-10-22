// Reduces a frame screenshot to the Spectra 6 panel's six inks (HARDWARE.md → Display
// service): ordered dithering for Daily, error diffusion for Ambient.
import { ditherImage, replaceColors } from 'epdoptimize';

// The measured inks, white-balanced so paper is #fff — what the panel is sent and what
// the epaper-* palettes in frontend/src/themes.css are built from. inky's order:
// black, white, yellow, red, blue, green.
export const INKS = ['#000000', '#ffffff', '#ffff6e', '#f16f74', '#5e5b91', '#5a8d6c'];

// inky's SATURATED_PALETTE: how the inks actually look on the reflective panel, without
// white balance. Only for previewing on a screen — never sent to the panel.
export const PAPER_INKS = ['#000000', '#a1a4a5', '#d0be47', '#9c484b', '#3d3b5e', '#3a5b46'];

const NAMES = ['black', 'white', 'yellow', 'red', 'blue', 'green'];
const PALETTE = INKS.map((color, i) => ({ name: NAMES[i], color, deviceColor: color }));
const RGB = INKS.map((hex) => [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16)));

// epdoptimize's error-diffusion kernels (an unknown name would silently fall back to
// Floyd–Steinberg, so callers check against this)
export const KERNELS = [
    'floydSteinberg',
    'falseFloydSteinberg',
    'atkinson',
    'burkes',
    'jarvis',
    'jarvisJudiceNinke',
    'stucki',
    'sierra2',
    'sierra2-4a',
    'sierra3',
    'fan',
    'shiauFan',
    'shiauFan2',
];

// Every pair of inks — ordered dithering mixes along the line between one pair
const PAIRS = RGB.flatMap((_, a) => RGB.slice(a + 1).map((_, i) => [a, a + 1 + i]));

const BAYER = [
    [0, 8, 2, 10],
    [12, 4, 14, 6],
    [3, 11, 1, 9],
    [15, 7, 13, 5],
];

/**
 * Dither `source` into `dest` (canvases of the same size).
 * `method`: "ordered" (Daily) or "errorDiffusion" (Ambient, any epdoptimize kernel).
 */
export async function dither(source, dest, { method, kernel = 'floydSteinberg' }) {
    if (method === 'errorDiffusion') {
        await ditherImage(source, dest, {
            palette: PALETTE,
            ditheringType: 'errorDiffusion',
            errorDiffusionMatrix: kernel,
            colorMatching: 'rgb',
        });
        return;
    }
    const image = source.getContext('2d').getImageData(0, 0, source.width, source.height);
    orderedPairs(image);
    dest.getContext('2d').putImageData(image, 0, 0);
}

/** Repaint the six inks with how they look on paper (same pixels, different colours). */
export function paintPaper(source, dest) {
    replaceColors(source, dest, { originalColors: INKS, replaceColors: PAPER_INKS });
}

// 4×4 Bayer between two inks: the pixel is placed on the line between the pair of inks
// that passes closest to it, and the threshold picks an end. epdoptimize's ordered modes
// add one offset to every channel, which collapses the frame themes' two-ink mixes
// (#f8b771 comes out solid yellow) — this keeps them in exact proportion. Choosing the
// closest line rather than the two nearest inks keeps antialiased edges to the two inks
// either side of them (grey-ish black-on-yellow is nearest green and blue), and a pixel
// that already is an ink sits at an end, so pure-ink text and lines come through untouched.
function orderedPairs({ data, width }) {
    for (let p = 0; p < data.length; p += 4) {
        const x = (p / 4) % width;
        const y = Math.floor(p / 4 / width);
        const { a, b, t } = closestLine(data[p], data[p + 1], data[p + 2]);
        const ink = RGB[t * 16 > BAYER[y % 4][x % 4] + 0.5 ? b : a];
        data[p] = ink[0];
        data[p + 1] = ink[1];
        data[p + 2] = ink[2];
        data[p + 3] = 255;
    }
}

// The pair of inks whose connecting line passes closest to r,g,b, and how far along it.
// A pixel blended from three colours (black type antialiased onto a pale tint) can land
// nearest an unrelated line — red–green runs through that olive-grey — and dither to a
// stray dot; it's a few dozen pixels a frame, where nearest-two-inks gave over a thousand.
function closestLine(r, g, b) {
    let best = { a: 0, b: 1, t: 0 };
    let bestDistance = Infinity;
    for (const [i, j] of PAIRS) {
        const [ir, ig, ib] = RGB[i];
        const dr = RGB[j][0] - ir;
        const dg = RGB[j][1] - ig;
        const db = RGB[j][2] - ib;
        const along = ((r - ir) * dr + (g - ig) * dg + (b - ib) * db) / (dr * dr + dg * dg + db * db);
        const t = Math.min(1, Math.max(0, along));
        const distance = (r - ir - t * dr) ** 2 + (g - ig - t * dg) ** 2 + (b - ib - t * db) ** 2;
        if (distance < bestDistance) {
            bestDistance = distance;
            best = { a: i, b: j, t };
        }
    }
    return best;
}
