export const themes = [
    { id: 'ash', label: 'Ash', fill: '#1d1d1d', stroke: '#6a6a6a', group: 'Black & White' },
    { id: 'chalk', label: 'Chalk', fill: '#ebebeb', stroke: '#909090', group: 'Black & White' },
    { id: 'dusk', label: 'Dusk', fill: '#151b2a', stroke: '#4e5478', group: 'Color' },
    { id: 'forest', label: 'Forest', fill: '#192118', stroke: '#4a6845', group: 'Color' },
    { id: 'linen', label: 'Linen', fill: '#f0e8da', stroke: '#9e8c75', group: 'Color' },
    { id: 'stone', label: 'Stone', fill: '#26201a', stroke: '#806050', group: 'Color' },
    { id: 'terracotta', label: 'Terracotta', fill: '#ece0d0', stroke: '#ac7850', group: 'Color' },
] as const;

export type Theme = (typeof themes)[number];
export type ThemeId = Theme['id'];
export type ThemeGroup = Theme['group'];

export const DEFAULT_THEME: ThemeId = 'stone';

export const themeGroups: ThemeGroup[] = ['Black & White', 'Color'];

export function isValidThemeId(value: string | null): value is ThemeId {
    return themes.some((t) => t.id === value);
}
