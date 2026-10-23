import assert from 'node:assert/strict';
import { test } from 'node:test';
import { rotatePixels } from './rotate.mjs';

// A 3×2 image whose pixels are numbered 1–6 in reading order (red channel only):
//   1 2 3
//   4 5 6
function numbered() {
    const data = new Uint8ClampedArray(3 * 2 * 4);
    for (let i = 0; i < 6; i++) data.set([i + 1, 0, 0, 255], i * 4);
    return { data, width: 3, height: 2 };
}

const reds = ({ data }) => Array.from({ length: data.length / 4 }, (_, i) => data[i * 4]);

test('rotatePixels turns 90° clockwise and swaps the sides', () => {
    const out = rotatePixels(numbered(), 90);
    assert.equal(out.width, 2);
    assert.equal(out.height, 3);
    //   4 1
    //   5 2
    //   6 3
    assert.deepEqual(reds(out), [4, 1, 5, 2, 6, 3]);
});

test('rotatePixels turns 270° clockwise (90° counter-clockwise)', () => {
    const out = rotatePixels(numbered(), 270);
    assert.equal(out.width, 2);
    assert.equal(out.height, 3);
    //   3 6
    //   2 5
    //   1 4
    assert.deepEqual(reds(out), [3, 6, 2, 5, 1, 4]);
});

test('rotatePixels turns 180° and keeps the sides', () => {
    const out = rotatePixels(numbered(), 180);
    assert.deepEqual([out.width, out.height], [3, 2]);
    assert.deepEqual(reds(out), [6, 5, 4, 3, 2, 1]);
});

test('rotatePixels keeps every channel of each pixel', () => {
    const image = { data: new Uint8ClampedArray([10, 20, 30, 255, 40, 50, 60, 255]), width: 2, height: 1 };
    assert.deepEqual(Array.from(rotatePixels(image, 90).data), [10, 20, 30, 255, 40, 50, 60, 255]);
    assert.deepEqual(Array.from(rotatePixels(image, 0).data), Array.from(image.data));
});
