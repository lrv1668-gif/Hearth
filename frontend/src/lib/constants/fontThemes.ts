/* Font theme presets. src/fonts.css is the source of truth for each preset's
   family stack, weights, and size scale — entries here are picker metadata
   only. Keep ids in sync with the [data-font] blocks in fonts.css. */
export const fontThemes = [
    { id: 'inter', label: 'Modern', tag: 'Sans' },
    { id: 'system', label: 'System', tag: 'Sans' },
    { id: 'nunito', label: 'Soft', tag: 'Rounded' },
    { id: 'source-serif', label: 'Quill', tag: 'Serif' },
    { id: 'space-grotesk', label: 'Air', tag: 'Grotesk' },
    { id: 'roboto-slab', label: 'Press', tag: 'Slab' },
] as const;

export type FontTheme = (typeof fontThemes)[number];
export type FontThemeId = FontTheme['id'];

export const DEFAULT_FONT_THEME: FontThemeId = fontThemes[0].id;

export function isValidFontThemeId(value: string | null): value is FontThemeId {
    return !!value && fontThemes.some((f) => f.id === value);
}
