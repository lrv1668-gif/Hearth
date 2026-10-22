// The Settings page's sections. The active one lives in the URL (`/settings?section=frame`)
// so it survives a refresh and other pages can link straight to it.
export const settingsSectionIds = ['display', 'home', 'dashboard', 'ambient', 'connections', 'frame'] as const;

export type SettingsSection = (typeof settingsSectionIds)[number];

export const DEFAULT_SETTINGS_SECTION: SettingsSection = 'display';

export function parseSettingsSection(value: string | null): SettingsSection {
    return settingsSectionIds.find((id) => id === value) ?? DEFAULT_SETTINGS_SECTION;
}

export function settingsHref(section: SettingsSection): string {
    return `/settings?section=${section}`;
}
