export const themes = [
    { id: 'ash', label: 'Ash', fill: '#1d1d1d', stroke: '#6a6a6a', group: 'Black & White' },
    { id: 'chalk', label: 'Chalk', fill: '#ebebeb', stroke: '#909090', group: 'Black & White' },
    { id: 'smoke', label: 'Smoke', fill: '#282828', stroke: '#787878', group: 'Black & White' },
    { id: 'blush', label: 'Blush', fill: '#f5e8e8', stroke: '#b87878', group: 'Color' },
    { id: 'dusk', label: 'Dusk', fill: '#151b2a', stroke: '#4e5478', group: 'Color' },
    { id: 'forest', label: 'Forest', fill: '#192118', stroke: '#4a6845', group: 'Color' },
    { id: 'frost', label: 'Frost', fill: '#e5ecf2', stroke: '#7a98b4', group: 'Color' },
    { id: 'linen', label: 'Linen', fill: '#f0e8da', stroke: '#9e8c75', group: 'Color' },
    { id: 'sage', label: 'Sage', fill: '#e2ece0', stroke: '#6e9068', group: 'Color' },
    { id: 'sky', label: 'Sky', fill: '#dce8f4', stroke: '#6888b0', group: 'Color' },
    { id: 'slate', label: 'Slate', fill: '#191e24', stroke: '#4a6070', group: 'Color' },
    { id: 'stone', label: 'Stone', fill: '#26201a', stroke: '#806050', group: 'Color' },
    { id: 'terracotta', label: 'Terracotta', fill: '#ece0d0', stroke: '#ac7850', group: 'Color' },
    { id: 'tide', label: 'Tide', fill: '#142028', stroke: '#2e6868', group: 'Color' },
] as const;

export type Theme = (typeof themes)[number];
export type ThemeId = Theme['id'];
export type ThemeGroup = Theme['group'];

export const DEFAULT_THEME: ThemeId = themes[0].id;
export const THEME_GROUPS: ThemeGroup[] = [...new Set(themes.map((theme) => theme.group))];

export function isValidThemeId(value: string | null): value is ThemeId {
    return !!value && themes.some((t) => t.id === value);
}
