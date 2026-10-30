import { frameWidgets, isValidFrameWidgetId, type FrameWidgetId } from './frameWidgets';

/* Daily-mode layout templates: a fixed arrangement of slots between the masthead and
   the forecast band, each slot a size class — S (about a quarter of the area), M (a
   half: tall or wide, depending on the template and orientation) or L (all of it).
   A widget lists the sizes it's designed for (frameWidgetSizes) and only goes in
   slots of those sizes, so every combination looks designed. `columns` ("Custom") is
   the older free two-column layout (left/right/split in frameWidgets.ts), kept as a template. */

export type FrameSize = 's' | 'm' | 'l';

export interface FrameTemplate {
    id: string;
    label: string;
    /** One size per slot, in slot order (slot i is grid area "s{i}"). */
    slots: readonly FrameSize[];
    /** CSS grid-template-areas, per orientation — rows of space-separated areas. */
    areas: { landscape: readonly string[]; portrait: readonly string[] };
}

export const frameTemplates = [
    { id: 'full', label: 'Full', slots: ['l'], areas: { landscape: ['s0'], portrait: ['s0'] } },
    {
        id: 'halves',
        label: 'Halves',
        slots: ['m', 'm'],
        // Portrait stacks them: side-by-side halves would be ~500px slivers.
        areas: { landscape: ['s0 s1'], portrait: ['s0', 's1'] },
    },
    {
        id: 'feature',
        label: 'Feature',
        slots: ['m', 's', 's'],
        areas: { landscape: ['s0 s1', 's0 s2'], portrait: ['s0 s1', 's0 s2'] },
    },
    {
        id: 'banner',
        label: 'Banner',
        slots: ['m', 's', 's'],
        areas: { landscape: ['s0 s0', 's1 s2'], portrait: ['s0 s0', 's1 s2'] },
    },
    {
        id: 'quad',
        label: 'Quarters',
        slots: ['s', 's', 's', 's'],
        areas: { landscape: ['s0 s1', 's2 s3'], portrait: ['s0 s1', 's2 s3'] },
    },
    // Last: the free-form fallback, for layouts the templates don't cover
    { id: 'columns', label: 'Custom', slots: [], areas: { landscape: [], portrait: [] } },
] as const satisfies readonly FrameTemplate[];

export type FrameTemplateId = (typeof frameTemplates)[number]['id'];

/** A fresh install's Daily layout. (Display's own default stays `columns`: settings saved
 *  before templates existed had no template and were laid out in columns.) */
export const DEFAULT_FRAME_TEMPLATE: FrameTemplateId = 'feature';
export const DEFAULT_FRAME_SLOTS: readonly FrameWidgetId[] = ['coming-up', 'countdowns', 'today'];

/** Slots in the largest template — the most widgets a template shows. */
export const MAX_TEMPLATE_SLOTS = Math.max(...frameTemplates.map((t) => t.slots.length));

/** The sizes each widget is designed for. Countdowns, Moon, Quote and Last Played
 *  change shape per size; the lists only change how many items fit (their `lines`).
 *  Week Ahead's seven day rows don't read in a quarter, so it's M/L only. */
export const frameWidgetSizes: Record<FrameWidgetId, readonly FrameSize[]> = {
    arc: ['s', 'm', 'l'],
    today: ['s', 'm', 'l'],
    'coming-up': ['s', 'm', 'l'],
    week: ['m', 'l'],
    countdowns: ['s', 'm', 'l'],
    almanac: ['s', 'm', 'l'],
    moon: ['s', 'm', 'l'],
    quote: ['s', 'm', 'l'],
    birds: ['s', 'm', 'l'],
    'last-played': ['s', 'm', 'l'],
    sports: ['s', 'm', 'l'],
    photo: ['s', 'm', 'l'],
};

export function isFrameTemplateId(value: string | null | undefined): value is FrameTemplateId {
    return frameTemplates.some((t) => t.id === value);
}

export function frameTemplate(id: FrameTemplateId): FrameTemplate {
    return frameTemplates.find((t) => t.id === id)!;
}

export function fitsSlot(id: FrameWidgetId, size: FrameSize): boolean {
    return frameWidgetSizes[id].includes(size);
}

/** Widgets that can go in a slot of `size`, in the registry's order. */
export function widgetsForSize(size: FrameSize): FrameWidgetId[] {
    return frameWidgets.map((w) => w.id).filter((id) => fitsSlot(id, size));
}

/** One entry per slot of `template`, '' for an empty slot. Positions are kept: an
 *  unknown, repeated or wrong-size widget empties its own slot rather than shifting
 *  the rest, and the list is padded or trimmed to the template's slot count — so a
 *  hand-edited URL or a template switch still renders sanely. */
export function normalizeSlots(template: FrameTemplateId, slots: readonly string[]): (FrameWidgetId | '')[] {
    const seen = new Set<string>();
    return frameTemplate(template).slots.map((size, i) => {
        const id = slots[i] ?? '';
        if (!isValidFrameWidgetId(id) || seen.has(id) || !fitsSlot(id, size)) return '';
        seen.add(id);
        return id;
    });
}

/** Reads `?template=feature&slots=coming-up,,quote` from the frame URL, falling back
 *  to `fallback` (the saved settings) for a missing param. */
export function parseFrameTemplate(
    params: Pick<URLSearchParams, 'get'>,
    fallback: { template: FrameTemplateId; slots: readonly string[] }
): { template: FrameTemplateId; slots: (FrameWidgetId | '')[] } {
    const raw = params.get('template');
    const template = isFrameTemplateId(raw) ? raw : fallback.template;
    const slots = params.get('slots');
    return { template, slots: normalizeSlots(template, slots === null ? fallback.slots : slots.split(',')) };
}

/** Carries widgets into a new template, in order, each into the first free slot it
 *  fits. The pickiest widgets (fewest sizes) are placed first, so a Week Ahead gets
 *  the template's M slot before a Moon that would fit anywhere takes it. */
export function moveToTemplate(
    template: FrameTemplateId,
    widgets: readonly (FrameWidgetId | '')[]
): (FrameWidgetId | '')[] {
    const sizes = frameTemplate(template).slots;
    const out: (FrameWidgetId | '')[] = sizes.map(() => '');
    const placed = widgets.filter((id): id is FrameWidgetId => id !== '');
    const pickiest = [...placed].sort((a, b) => frameWidgetSizes[a].length - frameWidgetSizes[b].length);
    for (const id of pickiest) {
        const i = sizes.findIndex((size, j) => out[j] === '' && fitsSlot(id, size));
        if (i >= 0) out[i] = id;
    }
    return out;
}
