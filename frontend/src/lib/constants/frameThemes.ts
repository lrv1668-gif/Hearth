/* E-paper frame themes. src/themes.css is the source of truth for each theme's
   tokens (`[data-theme='epaper-<id>']`); entries here are picker metadata only.
   Colours are limited to the panel's six inks plus a few two-ink mixes — see
   the "Frame palettes" comment in themes.css before adding one. */
export const frameThemes = [
    {
        id: 'almanac',
        label: 'Almanac',
        description: 'Indigo type, brick red, cream',
        dark: false,
        swatches: ['#ffffff', '#000000', '#5e5b91', '#f16f74', '#fffeb6', '#f8b771'],
    },
    {
        id: 'gallery',
        label: 'Gallery',
        description: 'Near-monochrome, like a print',
        dark: false,
        swatches: ['#ffffff', '#000000', '#f16f74', '#f8b771'],
    },
    {
        id: 'garden',
        label: 'Garden',
        description: 'Green ink, butter and amber',
        dark: false,
        swatches: ['#ffffff', '#000000', '#5a8d6c', '#f16f74', '#fffeb6', '#f8b771'],
    },
    {
        id: 'coast',
        label: 'Coast',
        description: 'Indigo on sand, no red',
        dark: false,
        swatches: ['#ffffff', '#000000', '#5e5b91', '#fffeb6', '#ffff6e'],
    },
    {
        id: 'harbor',
        label: 'Harbor',
        description: 'Crisp blue and red, no tints',
        dark: false,
        swatches: ['#ffffff', '#000000', '#5e5b91', '#f16f74'],
    },
    {
        id: 'terracotta',
        label: 'Terracotta',
        description: 'Amber band, red rules, olive',
        dark: false,
        swatches: ['#ffffff', '#000000', '#f16f74', '#5a8d6c', '#f8b771'],
    },
    {
        id: 'letterpress',
        label: 'Letterpress',
        description: 'Black and red, two-colour print',
        dark: false,
        swatches: ['#ffffff', '#000000', '#f16f74', '#fffedb'],
    },
    {
        id: 'night',
        label: 'Night',
        description: 'White and yellow on black',
        dark: true,
        swatches: ['#000000', '#ffffff', '#ffff6e', '#f16f74', '#2f2e49'],
    },
    {
        id: 'ember',
        label: 'Ember',
        description: 'Yellow and red on black',
        dark: true,
        swatches: ['#000000', '#ffff6e', '#f16f74', '#f8b771'],
    },
] as const;

export type FrameThemeId = (typeof frameThemes)[number]['id'];
export const DEFAULT_FRAME_THEME: FrameThemeId = 'almanac';

export type FramePalette = 'color' | 'bw';

/** The `[data-theme]` id for a frame theme in Color or B&W mode. B&W follows the
 *  theme's lightness: pure black-on-white, or white-on-black for dark themes. */
export function frameDataTheme(id: FrameThemeId, palette: FramePalette): string {
    if (palette === 'color') return `epaper-${id}`;
    return frameThemes.find((t) => t.id === id)?.dark ? 'epaper-bw-night' : 'epaper-bw';
}

export function isValidFrameThemeId(value: string | null): value is FrameThemeId {
    return frameThemes.some((t) => t.id === value);
}
