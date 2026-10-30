import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createCanvas } from 'canvas';
import { dither, greyscale, INKS, PAPER_INKS, paintPaper, photoBoxes } from './dither.mjs';

function filled(colour, size = 16) {
    const canvas = createCanvas(size, size);
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = colour;
    ctx.fillRect(0, 0, size, size);
    return canvas;
}

function pixels(canvas) {
    const { data } = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height);
    const out = [];
    for (let p = 0; p < data.length; p += 4)
        out.push('#' + [data[p], data[p + 1], data[p + 2]].map((v) => v.toString(16).padStart(2, '0')).join(''));
    return out;
}

function counts(canvas) {
    const out = {};
    for (const colour of pixels(canvas)) out[colour] = (out[colour] ?? 0) + 1;
    return out;
}

/** A 64×16 blue-to-amber gradient, standing in for a photo. */
function photoLike() {
    const canvas = createCanvas(64, 16);
    const ctx = canvas.getContext('2d');
    const gradient = ctx.createLinearGradient(0, 0, 64, 0);
    gradient.addColorStop(0, '#1d4f8a');
    gradient.addColorStop(1, '#e8a33c');
    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, 64, 16);
    return canvas;
}

async function dithered(colour, method) {
    const source = filled(colour);
    const dest = createCanvas(source.width, source.height);
    await dither(source, dest, { method });
    return dest;
}

for (const method of ['ordered', 'errorDiffusion']) {
    test(`${method}: each pure ink comes through unchanged`, async () => {
        for (const ink of INKS) assert.deepEqual(counts(await dithered(ink, method)), { [ink]: 256 });
    });

    test(`${method}: a photo-like gradient uses only the six inks`, async () => {
        const source = photoLike();
        const dest = createCanvas(64, 16);
        await dither(source, dest, { method });
        for (const colour of Object.keys(counts(dest))) assert.ok(INKS.includes(colour), colour);
    });
}

// B&W mode: a greyed photo's mid-greys sit nearest blue and green, so it needs its own inks
for (const method of ['ordered', 'errorDiffusion']) {
    test(`${method}: B&W keeps a greyed gradient to black and white`, async () => {
        const source = photoLike();
        greyscale(source);
        const dest = createCanvas(64, 16);
        await dither(source, dest, { method, palette: 'bw' });
        const colours = Object.keys(counts(dest)).sort();
        assert.deepEqual(colours, ['#000000', '#ffffff']);
    });
}

test('ordered: B&W dithers mid-grey half black, half white', async () => {
    const dest = createCanvas(16, 16);
    await dither(filled('#808080'), dest, { method: 'ordered', palette: 'bw' });
    assert.deepEqual(counts(dest), { '#000000': 128, '#ffffff': 128 });
});

test('greyscale greys by luma', () => {
    const canvas = filled('#ff0000', 1);
    greyscale(canvas);
    assert.deepEqual(pixels(canvas), ['#4c4c4c']);
});

// The frame themes' approved two-ink mixes (themes.test.ts), and their exact split
test('ordered: two-ink mixes dither in exact proportion', async () => {
    assert.deepEqual(counts(await dithered('#fffeb6', 'ordered')), { '#ffffff': 128, '#ffff6e': 128 });
    assert.deepEqual(counts(await dithered('#f8b771', 'ordered')), { '#ffff6e': 128, '#f16f74': 128 });
    assert.deepEqual(counts(await dithered('#2f2e49', 'ordered')), { '#000000': 128, '#5e5b91': 128 });
});

// Antialiased text: a half-covered pixel of black type on the yellow forecast band is
// nearest green and blue, but sits on the black–yellow line
test('ordered: antialiased edges keep to the inks either side', async () => {
    for (const [edge, inks] of [
        ['#808037', ['#000000', '#ffff6e']],
        ['#afadc8', ['#5e5b91', '#ffffff']],
        ['#78373a', ['#000000', '#f16f74']],
    ])
        for (const colour of Object.keys(counts(await dithered(edge, 'ordered'))))
            assert.ok(inks.includes(colour), `${edge} → ${colour}`);
});

test('ordered: a tint repeats every 4 pixels', async () => {
    const out = pixels(await dithered('#fffedb', 'ordered'));
    for (let y = 0; y < 16; y++)
        for (let x = 0; x < 16; x++) assert.equal(out[y * 16 + x], out[(y % 4) * 16 + (x % 4)]);
});

test('paintPaper moves each ink to its paper colour, pixel for pixel', async () => {
    const source = await dithered('#f8b771', 'ordered');
    const paper = createCanvas(16, 16);
    paintPaper(source, paper);
    const paperOf = (ink) => PAPER_INKS[INKS.indexOf(ink)];
    assert.deepEqual(pixels(paper), pixels(source).map(paperOf));
});

// Daily's photos: error-diffused inside the ordered page, each on its own
test('ordered: a photo box is error-diffused, the page around it ordered', async () => {
    const source = createCanvas(64, 32);
    const ctx = source.getContext('2d');
    ctx.fillStyle = '#f8b771';
    ctx.fillRect(0, 0, 64, 32);
    ctx.drawImage(photoLike(), 0, 16);
    const photo = { x: 0, y: 16, width: 64, height: 16 };

    const hybrid = createCanvas(64, 32);
    await dither(source, hybrid, { method: 'ordered', photos: [photo] });
    const ordered = createCanvas(64, 32);
    await dither(source, ordered, { method: 'ordered' });
    const diffused = createCanvas(64, 16);
    await dither(photoLike(), diffused, { method: 'errorDiffusion' });

    const out = pixels(hybrid);
    assert.deepEqual(out.slice(0, 64 * 16), pixels(ordered).slice(0, 64 * 16));
    assert.deepEqual(out.slice(64 * 16), pixels(diffused));
});

test('ordered: B&W photo boxes keep to black and white', async () => {
    const source = photoLike();
    greyscale(source);
    const dest = createCanvas(64, 16);
    await dither(source, dest, { method: 'ordered', palette: 'bw', photos: [{ x: 8, y: 0, width: 48, height: 16 }] });
    assert.deepEqual(Object.keys(counts(dest)).sort(), ['#000000', '#ffffff']);
});

test('photoBoxes rounds inwards, clips to the frame and drops empty boxes', () => {
    assert.deepEqual(
        photoBoxes(
            [
                { x: 10.4, y: 20.6, width: 100.2, height: 50 },
                { x: -5, y: 1190, width: 40, height: 40 },
                { x: 1700, y: 0, width: 50, height: 50 },
                { x: 3.2, y: 3.2, width: 0.5, height: 10 },
            ],
            1600,
            1200
        ),
        [
            { x: 11, y: 21, width: 99, height: 49 },
            { x: 0, y: 1190, width: 35, height: 10 },
        ]
    );
});
