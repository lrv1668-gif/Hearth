import { describe, expect, it } from 'vitest';
import { photoQuery, resolveSource, uploadToPhoto } from './photos';

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
