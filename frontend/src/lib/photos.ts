import { api, type Photo, type UploadedPhoto } from '$lib/api';
import type { PhotoSource } from '$lib/constants/photos';

/** Ambient photo choice shared by the frame's Ambient mode and the /ambient slideshow. */

/** An uploaded photo in the shape the ambient views render; its caption is the credit line. */
export function uploadToPhoto(up: UploadedPhoto): Photo {
    return {
        id: up.id,
        url: up.url,
        thumb_url: up.thumb_url,
        description: up.caption,
        photographer_name: null,
        unsplash_link: null,
        source: 'local',
    };
}

/** The Unsplash query for the chosen categories — "nature" when none are chosen. */
export function photoQuery(categories: readonly string[]): string {
    return categories.length > 0 ? categories.join(',') : 'nature';
}

/** Which source to draw the next photo from: "both" picks uploads half the time,
 *  when there are any. */
export function resolveSource(source: PhotoSource, hasUploads: boolean, random = Math.random()): PhotoSource {
    if (source !== 'both') return source;
    return hasUploads && random < 0.5 ? 'local' : 'unsplash';
}

/** Photo mode's pick: a random upload among `ids` (one pins it, several make an album),
 *  or null when none of them is still uploaded — the frame then falls back to pickPhoto. */
export function pickPinned(
    ids: readonly string[],
    uploads: readonly UploadedPhoto[],
    random = Math.random()
): Photo | null {
    const chosen = uploads.filter((up) => ids.includes(up.id));
    return chosen.length > 0 ? uploadToPhoto(chosen[Math.floor(random * chosen.length)]) : null;
}

/** /frame's ?photo= param (a JSON Photo — Settings' preview pins one so Live, Screen
 *  and Paper show the same photo), or null when it's missing or malformed. The URL must
 *  be same-origin or Unsplash's CDN, so the renderer can't be pointed anywhere else. */
export function parsePhotoParam(value: string | null): Photo | null {
    if (!value) return null;
    let p: unknown;
    try {
        p = JSON.parse(value);
    } catch {
        return null;
    }
    if (typeof p !== 'object' || p === null) return null;
    const o = p as Record<string, unknown>;
    const str = (v: unknown) => (typeof v === 'string' ? v : null);
    const url = str(o.url);
    const allowed =
        url && ((url.startsWith('/') && !url.startsWith('//')) || url.startsWith('https://images.unsplash.com/'));
    if (!allowed) return null;
    return {
        id: str(o.id) ?? '',
        url,
        thumb_url: str(o.thumb_url),
        description: str(o.description),
        photographer_name: str(o.photographer_name),
        unsplash_link: str(o.unsplash_link),
        source: str(o.source) ?? 'unsplash',
    };
}

/** One photo for a single render (the frame): a random upload, or one from the
 *  Photos service's source. Null when nothing is available. */
export async function pickPhoto(
    source: PhotoSource,
    categories: readonly string[],
    orientation: 'portrait' | 'landscape'
): Promise<Photo | null> {
    const uploads = source === 'local' || source === 'both' ? await api.photos.list() : [];
    const from = resolveSource(source, uploads.length > 0);
    if (from === 'local') {
        return uploads.length > 0 ? uploadToPhoto(uploads[Math.floor(Math.random() * uploads.length)]) : null;
    }
    return api.photos.random(photoQuery(categories), orientation, from);
}

/** What the Photos service accepts per upload. */
export const PHOTO_UPLOAD_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_FILE_MB = 25;
// Under the service's 200 MB request cap, leaving room for the form overhead
const MAX_BATCH_MB = 190;

/** Splits picked files into those worth sending and a reason for each one that isn't. A
 *  batch over the size cap is refused whole (`batchError`), since the service would. */
export function checkPhotoFiles(files: readonly File[]): {
    valid: File[];
    errors: string[];
    batchError: string | null;
} {
    const valid: File[] = [];
    const errors: string[] = [];
    for (const f of files) {
        if (!PHOTO_UPLOAD_TYPES.includes(f.type)) errors.push(`${f.name}: not a JPEG, PNG, or WebP`);
        else if (f.size > MAX_FILE_MB * 1024 * 1024) errors.push(`${f.name}: exceeds ${MAX_FILE_MB} MB`);
        else valid.push(f);
    }
    const totalMB = valid.reduce((sum, f) => sum + f.size, 0) / (1024 * 1024);
    const batchError =
        totalMB > MAX_BATCH_MB
            ? `Selected files total ${Math.round(totalMB)} MB — try uploading fewer at a time (200 MB max per batch).`
            : null;
    return { valid, errors, batchError };
}

/** One message for a batch's problems: the problem itself when there's one, else a count. */
export function summarizeUploadErrors(errors: readonly string[]): string | null {
    if (errors.length === 0) return null;
    return errors.length === 1 ? errors[0] : `${errors.length} files had issues.`;
}
