import { describe, expect, it } from 'vitest';
import { checkPhotoFiles, photoQuery, pickPinned, resolveSource, summarizeUploadErrors, uploadToPhoto } from './photos';

const upload = (id: string) => ({ id, url: `/${id}.jpg`, thumb_url: `/${id}-t.jpg`, caption: null });

describe('pickPinned', () => {
    const uploads = [upload('a'), upload('b'), upload('c')];

    it('shows the one pinned photo', () => {
        expect(pickPinned(['b'], uploads)?.id).toBe('b');
    });

    it('picks at random within the album, skipping photos not in it', () => {
        expect(pickPinned(['a', 'c'], uploads, 0)?.id).toBe('a');
        expect(pickPinned(['a', 'c'], uploads, 0.99)?.id).toBe('c');
    });

    it('is null when every chosen photo has been deleted', () => {
        expect(pickPinned(['gone'], uploads)).toBeNull();
        expect(pickPinned([], uploads)).toBeNull();
    });
});

describe('photoQuery', () => {
    it('joins categories, falling back to nature', () => {
        expect(photoQuery(['nature', 'architecture'])).toBe('nature,architecture');
        expect(photoQuery([])).toBe('nature');
    });
});

describe('resolveSource', () => {
    it('passes single sources through', () => {
        expect(resolveSource('unsplash', true)).toBe('unsplash');
        expect(resolveSource('local', false)).toBe('local');
    });

    it('splits "both" between uploads and Unsplash', () => {
        expect(resolveSource('both', true, 0.2)).toBe('local');
        expect(resolveSource('both', true, 0.8)).toBe('unsplash');
    });

    it('uses Unsplash for "both" when there are no uploads', () => {
        expect(resolveSource('both', false, 0.2)).toBe('unsplash');
    });
});

describe('uploadToPhoto', () => {
    it('uses the caption as the credit line', () => {
        const photo = uploadToPhoto({ id: 'u1', url: '/u1.jpg', thumb_url: '/u1-t.jpg', caption: 'Lake at dawn' });
        expect(photo).toMatchObject({
            id: 'u1',
            description: 'Lake at dawn',
            source: 'local',
            photographer_name: null,
        });
    });
});

describe('checkPhotoFiles', () => {
    const file = (name: string, type: string, mb: number) =>
        new File([new Uint8Array(Math.round(mb * 1024 * 1024))], name, { type });

    it('keeps images and explains the rest', () => {
        const { valid, errors, batchError } = checkPhotoFiles([
            file('a.jpg', 'image/jpeg', 0.01),
            file('b.gif', 'image/gif', 0.01),
            file('c.png', 'image/png', 26),
        ]);
        expect(valid.map((f) => f.name)).toEqual(['a.jpg']);
        expect(errors).toEqual(['b.gif: not a JPEG, PNG, or WebP', 'c.png: exceeds 25 MB']);
        expect(batchError).toBeNull();
    });

    it('refuses a batch over the size cap', () => {
        const big = Array.from({ length: 8 }, (_, i) => file(`${i}.jpg`, 'image/jpeg', 24));
        expect(checkPhotoFiles(big).batchError).toMatch(/^Selected files total 192 MB/);
    });
});

describe('summarizeUploadErrors', () => {
    it('shows one problem as-is and counts several', () => {
        expect(summarizeUploadErrors([])).toBeNull();
        expect(summarizeUploadErrors(['a.gif: nope'])).toBe('a.gif: nope');
        expect(summarizeUploadErrors(['x', 'y'])).toBe('2 files had issues.');
    });
});
