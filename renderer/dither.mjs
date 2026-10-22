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

// 4×4 Bayer between each pixel's two nearest inks: the pixel is placed on the line from
// one to the other and the threshold picks an end. epdoptimize's ordered modes add one
// offset to every channel, which collapses the frame themes' two-ink mixes (#f8b771
// comes out solid yellow) — this keeps them in exact proportion, and a pixel that
// already is an ink sits at an end, so pure-ink text and lines come through untouched.
function orderedPairs({ data, width }) {
    for (let p = 0; p < data.length; p += 4) {
        const x = (p / 4) % width;
        const y = Math.floor(p / 4 / width);
        const [a, b] = nearestTwo(data[p], data[p + 1], data[p + 2]);
        const t = along(data, p, RGB[a], RGB[b]);
        const ink = RGB[t * 16 > BAYER[y % 4][x % 4] + 0.5 ? b : a];
        data[p] = ink[0];
        data[p + 1] = ink[1];
        data[p + 2] = ink[2];
        data[p + 3] = 255;
    }
}

function nearestTwo(r, g, b) {
    let first = 0;
    let second = 0;
    let d1 = Infinity;
    let d2 = Infinity;
    RGB.forEach(([ir, ig, ib], i) => {
        const d = (r - ir) ** 2 + (g - ig) ** 2 + (b - ib) ** 2;
        if (d < d1) [second, d2, first, d1] = [first, d1, i, d];
        else if (d < d2) [second, d2] = [i, d];
    });
    return [first, second];
}

// How far the pixel at p sits along the line from ink a to ink b, clamped to 0–1
function along(data, p, a, b) {
    const d = [b[0] - a[0], b[1] - a[1], b[2] - a[2]];
    const t =
        ((data[p] - a[0]) * d[0] + (data[p + 1] - a[1]) * d[1] + (data[p + 2] - a[2]) * d[2]) /
        (d[0] ** 2 + d[1] ** 2 + d[2] ** 2);
    return Math.min(1, Math.max(0, t));
}
