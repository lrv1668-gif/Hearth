// Turns a portrait capture onto the panel's native landscape for GET /display/frame.
// Pixel for pixel — no resampling — so the dither pattern reaches the panel intact.

export const ROTATIONS = [0, 90, 180, 270];

/**
 * `{ data, width, height }` (RGBA, like ImageData) rotated `degrees` clockwise, as a new
 * object of the same shape. 90 and 270 swap width and height.
 */
export function rotatePixels({ data, width, height }, degrees) {
    if (degrees === 0) return { data: new Uint8ClampedArray(data), width, height };
    const turned = degrees === 90 || degrees === 270;
    const outWidth = turned ? height : width;
    const outHeight = turned ? width : height;
    const out = new Uint8ClampedArray(data.length);
    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            const [ox, oy] =
                degrees === 90 ? [height - 1 - y, x] : degrees === 180 ? [width - 1 - x, height - 1 - y] : [y, width - 1 - x];
            const from = (y * width + x) * 4;
            const to = (oy * outWidth + ox) * 4;
            out[to] = data[from];
            out[to + 1] = data[from + 1];
            out[to + 2] = data[from + 2];
            out[to + 3] = data[from + 3];
        }
    }
    return { data: out, width: outWidth, height: outHeight };
}
