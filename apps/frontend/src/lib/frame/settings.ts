// The frame's shared appearance settings — ambient position, text and panel size,
// rotation, dithering, quiet hours — their validators, and the /frame URL they make.
import type { AmbientPosition, Photo } from '#lib/api.ts';
import type { FrameLayout } from '#lib/constants/frameWidgets.ts';
import type { FrameTemplateId } from '#lib/constants/frameTemplates.ts';
import type { FrameModeId } from '#lib/constants/frameModes.ts';
import { CUSTOM_FRAME_THEME } from '#lib/constants/frameThemes.ts';
import { encodeCustomTheme, type CustomFrameTheme } from './customTheme.ts';
import {
    MAX_FRAME_SIZE,
    MAX_FRAME_TEXT_SIZE,
    MIN_FRAME_SIZE,
    MIN_FRAME_TEXT_SIZE,
} from '#lib/constants/frameLimits.ts';

/** Where Ambient mode's info card can sit — top/bottom row, left/middle/right — in
 *  picker (reading) order. */
export const AMBIENT_POSITIONS = [
    { value: 'top-left', label: 'Top left' },
    { value: 'top-middle', label: 'Top middle' },
    { value: 'top-right', label: 'Top right' },
    { value: 'bottom-left', label: 'Bottom left' },
    { value: 'bottom-middle', label: 'Bottom middle' },
    { value: 'bottom-right', label: 'Bottom right' },
] as const satisfies readonly { value: AmbientPosition; label: string }[];
export const DEFAULT_AMBIENT_POSITION: AmbientPosition = 'bottom-left';

export function isAmbientPosition(value: string | null | undefined): value is AmbientPosition {
    return AMBIENT_POSITIONS.some((c) => c.value === value);
}

/* ── Text size ────────────────────────────────────────────────────────────────
   A percent on the frame's reading type (titles down to captions) and icons, for a
   panel read from closer or further than the tuned 3–4 m. Never below 100: the
   tuned sizes are the e-paper legibility floor. Larger fits fewer list items. */
export function isFrameTextSize(value: unknown): value is number {
    return (
        Number.isInteger(value) && (value as number) >= MIN_FRAME_TEXT_SIZE && (value as number) <= MAX_FRAME_TEXT_SIZE
    );
}

/* ── Panel size ───────────────────────────────────────────────────────────────
   The panel's native size is landscape (width ≥ height); portrait swaps it. The
   frame's type, icons and spacing are tuned for the 1600×1200 Inky Impression
   13.3" and scale so that reference fits the panel in both directions — a
   wider-aspect panel (800×480) scales by its short edge, so portrait columns keep
   room. Line weights stay 3px so they land on whole pixels. */
export const FRAME_REFERENCE_SIZE = { width: 1600, height: 1200 } as const;
export const DEFAULT_FRAME_SIZE = { width: 1600, height: 1200 } as const;

/** Which way a portrait frame is turned onto the panel's native landscape for
 *  GET /display/frame, in clockwise degrees — depends on which side the frame hangs. */
export type PortraitRotation = 90 | 270;
export const DEFAULT_PORTRAIT_ROTATION: PortraitRotation = 90;

export function isPortraitRotation(value: unknown): value is PortraitRotation {
    return value === 90 || value === 270;
}

/** Who dithers GET /display/frame: Hearth's renderer (per mode), or the frame client —
 *  sent undithered, so inkypull lets inky Floyd–Steinberg it on the Pi. */
export type FrameDithering = 'hearth' | 'frame';
export const DEFAULT_FRAME_DITHERING: FrameDithering = 'hearth';

export function isFrameDithering(value: unknown): value is FrameDithering {
    return value === 'hearth' || value === 'frame';
}

/* ── Quiet hours ──────────────────────────────────────────────────────────────
   A nightly window (home time) in which GET /display/frame keeps its picture instead
   of redrawing on the interval; a mode change still draws. An end before the start
   runs past midnight; Display refuses equal times. */
export interface QuietHours {
    start: string;
    end: string;
}
export const DEFAULT_QUIET_HOURS: QuietHours = { start: '23:00', end: '06:00' };

const HH_MM = /^([01]\d|2[0-3]):[0-5]\d$/;

/** Saved quiet_start/quiet_end as a window, or null when off or malformed. */
export function toQuietHours(start: unknown, end: unknown): QuietHours | null {
    return typeof start === 'string' && typeof end === 'string' && HH_MM.test(start) && HH_MM.test(end) && start !== end
        ? { start, end }
        : null;
}

/** Common Inky Impression panels, by native resolution. */
export const FRAME_SIZE_PRESETS = [
    { label: 'Inky 13.3″', width: 1600, height: 1200 },
    { label: 'Inky 7.3″', width: 800, height: 480 },
    { label: 'Inky 5.7″', width: 600, height: 448 },
    { label: 'Inky 4″', width: 640, height: 400 },
] as const;

/** Whether `width`×`height` is a valid native panel size. */
export function isFrameSize(width: unknown, height: unknown): boolean {
    const ok = (n: unknown) =>
        Number.isInteger(n) && (n as number) >= MIN_FRAME_SIZE && (n as number) <= MAX_FRAME_SIZE;
    return ok(width) && ok(height) && (width as number) >= (height as number);
}

/** Clamps typed dimensions into a valid native size (rounded, in range, long edge first). */
export function normalizeFrameSize(width: number, height: number): { width: number; height: number } {
    const clamp = (n: number, fallback: number) =>
        Number.isFinite(n) ? Math.min(MAX_FRAME_SIZE, Math.max(MIN_FRAME_SIZE, Math.round(n))) : fallback;
    const a = clamp(width, DEFAULT_FRAME_SIZE.width);
    const b = clamp(height, DEFAULT_FRAME_SIZE.height);
    return { width: Math.max(a, b), height: Math.min(a, b) };
}

/** The frame canvas for a native panel size: `w`×`h` as rendered (portrait swaps
 *  them) and `scale`, the multiplier on the 1600×1200-reference type and spacing. */
export function frameCanvas(o: { width: number; height: number; orientation: 'landscape' | 'portrait' }) {
    const portrait = o.orientation === 'portrait';
    return {
        w: portrait ? o.height : o.width,
        h: portrait ? o.width : o.height,
        scale: Math.min(o.width / FRAME_REFERENCE_SIZE.width, o.height / FRAME_REFERENCE_SIZE.height),
    };
}

export interface FrameOptions {
    mode: FrameModeId;
    palette: 'color' | 'bw';
    theme: string;
    font: string;
    /** Percent on the reading type and icons (see isFrameTextSize). */
    textSize: number;
    orientation: 'landscape' | 'portrait';
    /** The panel's native (landscape) size. */
    width: number;
    height: number;
    layout: FrameLayout;
    split: number;
    template: FrameTemplateId;
    /** The template's widget per slot, '' for an empty slot. */
    slots: readonly string[];
    position: AmbientPosition;
    /** Ambient/Photo's photo, fixed instead of a random pick (Settings' preview). */
    photo?: Photo | null;
    /** Custom's colours, sent as ?colors= when the theme is 'custom'. */
    customTheme?: CustomFrameTheme | null;
}

/** The /frame URL for a set of options — what the live preview and the Display
 *  service's renderer load. Every option is spelled out so the URL is self-describing. */
export function frameUrl(o: FrameOptions): string {
    return `/frame?${frameParams(o)}`;
}

/** /frame's query params — also taken by the Display service's dithered preview. */
export function frameParams(o: FrameOptions): URLSearchParams {
    const params = new URLSearchParams({
        mode: o.mode,
        palette: o.palette,
        theme: o.theme,
        font: o.font,
        text: String(o.textSize),
        orientation: o.orientation,
        width: String(o.width),
        height: String(o.height),
        left: o.layout.left.join(','),
        right: o.layout.right.join(','),
        split: String(o.split),
        template: o.template,
        slots: o.slots.join(','),
        position: o.position,
    });
    if (o.theme === CUSTOM_FRAME_THEME && o.customTheme) params.set('colors', encodeCustomTheme(o.customTheme));
    if (o.photo) params.set('photo', JSON.stringify(o.photo));
    return params;
}
