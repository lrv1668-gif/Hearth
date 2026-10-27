// The e-paper frame's modes — the ids /frame?mode= takes. Kept in sync with the Display
// service's FrameModes (src/Display/Records/FrameModes.cs); the order is the cycle the
// frame's Button B (POST /display/mode/next) steps through.
export const frameModes = [
    { id: 'ambient', label: 'Ambient', description: 'A full-frame photo with a small date and weather card' },
    { id: 'daily', label: 'Daily', description: 'Today, the week ahead and your chosen widgets' },
    { id: 'photo', label: 'Photo', description: 'Your chosen photo, or an album of them' },
    { id: 'countdown', label: 'Countdown', description: 'One countdown, as big as the frame allows' },
    { id: 'night', label: 'Night', description: 'Tonight’s moon and the next sunrise' },
    { id: 'note', label: 'Note', description: 'Your own words — house rules, a welcome, a reminder' },
] as const;

export type FrameModeId = (typeof frameModes)[number]['id'];

export const DEFAULT_FRAME_MODE: FrameModeId = 'ambient';

export function isFrameMode(value: unknown): value is FrameModeId {
    return frameModes.some((m) => m.id === value);
}

export function frameModeLabel(id: string): string {
    return frameModes.find((m) => m.id === id)?.label ?? id;
}

/** How much of Ambient's info card Photo mode keeps. */
export const photoCards = [
    { value: 'full', label: 'Full card' },
    { value: 'caption', label: 'Caption only' },
    { value: 'none', label: 'No card' },
] as const;

export type PhotoCard = (typeof photoCards)[number]['value'];

export function isPhotoCard(value: unknown): value is PhotoCard {
    return photoCards.some((c) => c.value === value);
}

/** How long Ambient and Photo keep a photo on the panel before the next render picks
 *  another — each change is a full ~30 s e-paper refresh. Display's photo_interval_minutes. */
export const photoIntervals = [
    { minutes: 30, label: '30 min' },
    { minutes: 60, label: '1 hour' },
    { minutes: 180, label: '3 hours' },
    { minutes: 360, label: '6 hours' },
] as const;

export const DEFAULT_PHOTO_INTERVAL_MINUTES = 60;

/** Display's FrameSettings range (15 min – a day). */
export function isPhotoInterval(value: unknown): value is number {
    return Number.isInteger(value) && (value as number) >= 15 && (value as number) <= 1440;
}

/** How often a rotating schedule rule steps to its next mode. Display's ModeRule.RotateMinutes (5 min – a day). */
export const rotateIntervals = [
    { minutes: 15, label: '15 min' },
    { minutes: 30, label: '30 min' },
    { minutes: 60, label: '1 hour' },
    { minutes: 120, label: '2 hours' },
    { minutes: 240, label: '4 hours' },
] as const;

export const DEFAULT_ROTATE_MINUTES = 30;

/** A rule holds at most this many modes — Display's ModeRule.MaxRotate + 1. */
export const MAX_RULE_MODES = 6;

/** Schedule rule days, Sunday first — Display's ModeRule.DayIds. */
export const modeDays = [
    { id: 'sun', label: 'S', name: 'Sunday' },
    { id: 'mon', label: 'M', name: 'Monday' },
    { id: 'tue', label: 'T', name: 'Tuesday' },
    { id: 'wed', label: 'W', name: 'Wednesday' },
    { id: 'thu', label: 'T', name: 'Thursday' },
    { id: 'fri', label: 'F', name: 'Friday' },
    { id: 'sat', label: 'S', name: 'Saturday' },
] as const;

export type ModeDay = (typeof modeDays)[number]['id'];
