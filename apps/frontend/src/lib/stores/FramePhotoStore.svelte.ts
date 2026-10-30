import { api, type Photo } from '#lib/api.ts';

class FramePhotoStore {
    /** null until loaded, or when there's no photo to show. */
    photo = $state<Photo | null>(null);
    /** Display couldn't be reached, so there's no telling which photo the panel has. */
    error = $state(false);
}

export const framePhotoStore = new FramePhotoStore();

/** Loads Daily's photo of the day and waits for it to decode, so the frame's screenshot
 *  never catches the slot blank. `held` is the frame URL's photo — Display's panel render
 *  passes the one it holds; elsewhere (a browser, Settings' preview) it's asked for. */
export async function loadFramePhoto(held: Photo | null = null) {
    const photo = held ?? (await api.display.photo('daily'));
    framePhotoStore.error = photo === undefined;
    framePhotoStore.photo = photo ?? null;
    if (photo) await preload(photo.url);
}

function preload(url: string): Promise<void> {
    // No crossOrigin, like the <img>: the frame only draws the photo, it never reads its pixels back.
    const img = new Image();
    img.src = url;
    return img.decode().catch(() => {});
}
