import { describe, expect, it } from 'vitest';
import {
    checkPhotoFiles,
    isGroupPicked,
    photoQuery,
    pickPhotoIn,
    toggleGroup,
    resolveSource,
    summarizeUploadErrors,
    uploadToPhoto,
} from './photos';

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

describe('pickPhotoIn', () => {
    const order = ['a', 'b', 'c', 'd'];

    it('picks just the clicked photo in one-at-a-time mode, and clears it on a second click', () => {
        expect(pickPhotoIn(['a', 'b'], 'c', 'one')).toEqual(['c']);
        expect(pickPhotoIn(['c'], 'c', 'one')).toEqual([]);
    });

    it('adds or removes the clicked photo in several mode', () => {
        expect(pickPhotoIn(['a'], 'c', 'several')).toEqual(['a', 'c']);
        expect(pickPhotoIn(['a', 'c'], 'a', 'several')).toEqual(['c']);
    });

    it('adds the run from the last click on a shift-click, in either direction', () => {
        expect(pickPhotoIn(['a'], 'c', 'several', order, 'a', true)).toEqual(['a', 'b', 'c']);
        expect(pickPhotoIn([], 'b', 'several', order, 'd', true)).toEqual(['b', 'c', 'd']);
    });

    it('treats a shift-click with no anchor as a plain toggle', () => {
        expect(pickPhotoIn([], 'b', 'several', order, null, true)).toEqual(['b']);
    });
});

describe('photo groups', () => {
    const group = { id: 'g', name: 'Winter', photo_ids: ['a', 'b'] };

    it('is picked only when every photo in it is', () => {
        expect(isGroupPicked(['a', 'b', 'c'], group)).toBe(true);
        expect(isGroupPicked(['a'], group)).toBe(false);
        expect(isGroupPicked([], { ...group, photo_ids: [] })).toBe(false);
    });

    it('adds the missing photos, or removes them all when the group is already picked', () => {
        expect(toggleGroup(['a', 'c'], group)).toEqual(['a', 'c', 'b']);
        expect(toggleGroup(['a', 'b', 'c'], group)).toEqual(['c']);
    });
});
