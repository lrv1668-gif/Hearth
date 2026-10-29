import type { Photo, PhotoGroup, UploadedPhoto } from '$lib/api';
import type { PhotoSource } from '$lib/constants/photos';

/** The /ambient slideshow's photo choice (the frame's is the Display service's FramePhotos). */

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

/** The photos the Photos service holds per Unsplash search (its PhotoFetcher's count). */
export const UNSPLASH_POOL = 20;

/** Which source to draw the next photo from: "both" picks uploads in proportion to how many
 *  there are against UNSPLASH_POOL, so every photo comes round about as often. */
export function resolveSource(source: PhotoSource, uploads: number, random = Math.random()): PhotoSource {
    if (source !== 'both') return source;
    return random < uploads / (uploads + UNSPLASH_POOL) ? 'local' : 'unsplash';
}

/** How Photo mode's picker treats a click: `one` picks just that photo, `several` adds or removes it. */
export type PickMode = 'one' | 'several';

/**
 * The picked ids after clicking `id`. In `several` mode a `range` click (shift) adds every
 * photo from `anchor` (the last one clicked) through `id`, in the grid's `order`.
 */
export function pickPhotoIn(
    picked: readonly string[],
    id: string,
    mode: PickMode,
    order: readonly string[] = [],
    anchor: string | null = null,
    range = false
): string[] {
    if (mode === 'one') return picked.length === 1 && picked[0] === id ? [] : [id];
    const from = anchor ? order.indexOf(anchor) : -1;
    const to = order.indexOf(id);
    if (range && from >= 0 && to >= 0) {
        const run = order.slice(Math.min(from, to), Math.max(from, to) + 1);
        return [...picked, ...run.filter((r) => !picked.includes(r))];
    }
    return picked.includes(id) ? picked.filter((p) => p !== id) : [...picked, id];
}

/** Whether all of a group's photos are picked (an empty group never is). */
export function isGroupPicked(picked: readonly string[], group: PhotoGroup): boolean {
    return group.photo_ids.length > 0 && group.photo_ids.every((id) => picked.includes(id));
}

/** Adds a group's photos to the pick, or takes them out when they're all in it already. */
export function toggleGroup(picked: readonly string[], group: PhotoGroup): string[] {
    if (isGroupPicked(picked, group)) return picked.filter((id) => !group.photo_ids.includes(id));
    return [...picked, ...group.photo_ids.filter((id) => !picked.includes(id))];
}

/** /frame's ?photo= param (a JSON Photo — the one Display holds, passed by its panel
 *  render and Settings' preview), or null when it's missing or malformed. The URL must
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
