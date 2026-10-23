import { browser } from '$app/environment';
import { api, type AmbientPosition, type FrameSettings } from '$lib/api';
import {
    DEFAULT_AMBIENT_POSITION,
    DEFAULT_FRAME_SIZE,
    DEFAULT_PORTRAIT_ROTATION,
    isAmbientPosition,
    isFrameSize,
    isPortraitRotation,
    type PortraitRotation,
} from '$lib/frame';
import { categoryOptions } from '$lib/constants/ambient';
import type { PhotoCategory, PhotoSource } from '$lib/constants/photos';
import {
    DEFAULT_PHOTO_INTERVAL_MINUTES,
    isPhotoCard,
    isPhotoInterval,
    type PhotoCard,
} from '$lib/constants/frameModes';
import { DEFAULT_FRAME_THEME, isValidFrameThemeId, type FrameThemeId } from '$lib/constants/frameThemes';
import { DEFAULT_FONT_THEME, isValidFontThemeId, type FontThemeId } from '$lib/constants/fontThemes';
import {
    clampSplit,
    DEFAULT_FRAME_LAYOUT,
    DEFAULT_FRAME_SPLIT,
    MAX_COLUMN_WIDGETS,
    normalizeFrameLayout,
    type FrameLayout,
    type FrameWidgetId,
} from '$lib/constants/frameWidgets';

/** Where SettingsStore parks frame settings it finds in an old localStorage snapshot,
 *  so they can be moved to the Display service once (see loadFrameSettings). */
export const LEGACY_FRAME_SETTINGS_KEY = 'hearth-frame-legacy';

const SAVE_DELAY_MS = 400;

/**
 * The e-paper frame's look, Daily layout and Ambient photos. One shared copy lives in
 * the Display service — the frame is a single physical display, so every client edits
 * the same settings and the frame's headless renderer reads them too. The browser
 * /ambient slideshow draws from the same photo source and categories.
 */
class FrameSettingsStore {
    theme = $state<FrameThemeId>(DEFAULT_FRAME_THEME);
    font = $state<FontThemeId>(DEFAULT_FONT_THEME);
    orientation = $state<'landscape' | 'portrait'>('landscape');
    /** The panel's native (landscape) size; always valid (see isFrameSize). */
    width = $state<number>(DEFAULT_FRAME_SIZE.width);
    height = $state<number>(DEFAULT_FRAME_SIZE.height);
    layout = $state<FrameLayout>(DEFAULT_FRAME_LAYOUT);
    split = $state(DEFAULT_FRAME_SPLIT);
    ambientPosition = $state<AmbientPosition>(DEFAULT_AMBIENT_POSITION);
    photoSource = $state<PhotoSource>('unsplash');
    photoCategories = $state<PhotoCategory[]>(['nature', 'architecture']);
    /** Photo mode: the chosen uploads and how much of the info card to keep. */
    photoIds = $state<string[]>([]);
    photoCard = $state<PhotoCard>('full');
    /** Countdown mode: the countdown task to show; null shows the next one coming up. */
    countdownId = $state<number | null>(null);
    /** Note mode: a heading and a body, kept as typed (line breaks included). */
    noteTitle = $state('');
    noteBody = $state('');
    /** The panel render: minutes Ambient and Photo keep a photo, and which way portrait turns. */
    photoIntervalMinutes = $state(DEFAULT_PHOTO_INTERVAL_MINUTES);
    portraitRotation = $state<PortraitRotation>(DEFAULT_PORTRAIT_ROTATION);
    /** True once the Display service has answered (or failed to). */
    loaded = $state(false);
    /** The Display service couldn't be reached — showing (and not saving) defaults. */
    error = $state(false);

    toggleCategory(cat: PhotoCategory) {
        this.photoCategories = this.photoCategories.includes(cat)
            ? this.photoCategories.filter((c) => c !== cat)
            : [...this.photoCategories, cat];
    }

    togglePhoto(id: string) {
        this.photoIds = this.photoIds.includes(id) ? this.photoIds.filter((p) => p !== id) : [...this.photoIds, id];
    }

    /** Removes the widget, or adds it to the column with more room (no-op if both are full). */
    toggleWidget(id: FrameWidgetId) {
        const { left, right } = normalizeFrameLayout(this.layout);
        if (left.includes(id) || right.includes(id)) {
            this.layout = { left: left.filter((w) => w !== id), right: right.filter((w) => w !== id) };
        } else if (Math.min(left.length, right.length) < MAX_COLUMN_WIDGETS) {
            this.layout =
                left.length <= right.length ? { left: [...left, id], right } : { left, right: [...right, id] };
        }
    }

    snapshot(): FrameSettings {
        const { left, right } = normalizeFrameLayout(this.layout);
        return {
            theme: this.theme,
            font: this.font,
            orientation: this.orientation,
            width: this.width,
            height: this.height,
            left,
            right,
            split: this.split,
            ambient_position: this.ambientPosition,
            photo_source: this.photoSource,
            photo_categories: this.photoCategories,
            photo_ids: this.photoIds,
            photo_card: this.photoCard,
            countdown_id: this.countdownId,
            note_title: this.noteTitle || null,
            note_body: this.noteBody || null,
            photo_interval_minutes: this.photoIntervalMinutes,
            portrait_rotation: this.portraitRotation,
        };
    }

    /** Applies saved settings, dropping ids this frontend doesn't know. */
    apply(saved: Partial<FrameSettings>) {
        if (Array.isArray(saved.photo_ids)) this.photoIds = saved.photo_ids;
        if (isPhotoCard(saved.photo_card)) this.photoCard = saved.photo_card;
        if (saved.countdown_id !== undefined) this.countdownId = saved.countdown_id;
        if (saved.note_title !== undefined) this.noteTitle = saved.note_title ?? '';
        if (saved.note_body !== undefined) this.noteBody = saved.note_body ?? '';
        if (isPhotoInterval(saved.photo_interval_minutes)) this.photoIntervalMinutes = saved.photo_interval_minutes;
        if (isPortraitRotation(saved.portrait_rotation)) this.portraitRotation = saved.portrait_rotation;
        if (isValidFrameThemeId(saved.theme ?? null)) this.theme = saved.theme as FrameThemeId;
        if (isValidFontThemeId(saved.font ?? null)) this.font = saved.font as FontThemeId;
        if (saved.orientation === 'landscape' || saved.orientation === 'portrait') this.orientation = saved.orientation;
        if (isFrameSize(saved.width, saved.height)) {
            this.width = saved.width!;
            this.height = saved.height!;
        }
        if (saved.left && saved.right) this.layout = normalizeFrameLayout({ left: saved.left, right: saved.right });
        if (typeof saved.split === 'number') this.split = clampSplit(saved.split);
        if (isAmbientPosition(saved.ambient_position)) this.ambientPosition = saved.ambient_position;
        if (saved.photo_source) this.photoSource = saved.photo_source;
        if (Array.isArray(saved.photo_categories)) {
            this.photoCategories = saved.photo_categories.filter((c): c is PhotoCategory =>
                categoryOptions.some((o) => o.id === c)
            );
        }
    }
}

export const frameSettings = new FrameSettingsStore();

/** Loads the shared settings. If none are saved yet but this browser has frame
 *  settings from before they moved to the server, those are uploaded once. */
export async function loadFrameSettings() {
    const saved = await api.display.settings();
    if (saved === undefined) {
        frameSettings.error = true;
    } else {
        frameSettings.error = false;
        if (saved) frameSettings.apply(saved);
        else await migrateLegacy();
    }
    frameSettings.loaded = true;
}

async function migrateLegacy() {
    if (!browser) return;
    const raw = localStorage.getItem(LEGACY_FRAME_SETTINGS_KEY);
    if (!raw) return;
    try {
        frameSettings.apply(JSON.parse(raw));
        await api.display.saveSettings(frameSettings.snapshot());
        localStorage.removeItem(LEGACY_FRAME_SETTINGS_KEY);
    } catch {
        // keep the legacy copy for the next attempt
    }
}

/**
 * Saves every change back to the Display service, debounced so dragging the split or
 * a widget sends one request. Call once the settings have loaded (it skips the
 * initial values); returns the cleanup for onMount.
 */
export function syncFrameSettings(): () => void {
    let last = JSON.stringify(frameSettings.snapshot());
    let timer: ReturnType<typeof setTimeout> | undefined;
    const stop = $effect.root(() => {
        $effect(() => {
            const next = frameSettings.snapshot();
            const json = JSON.stringify(next);
            if (json === last || frameSettings.error) return;
            clearTimeout(timer);
            timer = setTimeout(async () => {
                try {
                    await api.display.saveSettings(next);
                    last = json;
                } catch {
                    frameSettings.error = true;
                }
            }, SAVE_DELAY_MS);
        });
    });
    return () => {
        clearTimeout(timer);
        stop();
    };
}
