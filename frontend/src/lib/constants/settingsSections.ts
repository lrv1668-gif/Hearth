import type { Component } from 'svelte';
import { Image, LayoutList, MapPin, Monitor, Plug, Tv } from '@lucide/svelte';

// The Settings page's sections. The active one lives in the URL (`/settings?section=frame`)
// so it survives a refresh and other pages can link straight to it. A section's cards
// fill in its sub-items in the desktop sidebar on their own (`SettingsSubnav`), so a new
// card needs no entry here.
export const settingsSections = [
    {
        id: 'display',
        label: 'Display',
        subtitle: 'Theme · type · kiosk',
        icon: Monitor,
        heading: 'How Hearth looks',
        description:
            'The theme drives every surface, type colour, and accent across the dashboard and the frame. Kiosk mode applies to this browser only.',
    },
    {
        id: 'home',
        label: 'Home',
        subtitle: 'Location · time zone',
        icon: MapPin,
        heading: 'Home',
        description:
            'Where Hearth lives — for the weather, nearby birds, daylight, and the frame’s clock. Shared by every device.',
    },
    {
        id: 'dashboard',
        label: 'Dashboard',
        subtitle: 'Widgets · layout',
        icon: LayoutList,
        heading: 'Dashboard & Widgets',
        description: 'Manage which widgets appear and configure their layout on the dashboard page.',
    },
    {
        id: 'ambient',
        label: 'Ambient',
        subtitle: 'Photos · slideshow',
        icon: Image,
        heading: 'Ambient Mode',
        description:
            'Choose the photos for Ambient mode — shared by the frame and every device — and how this browser’s slideshow plays them.',
    },
    {
        id: 'connections',
        label: 'Connections',
        subtitle: 'Spotify · calendar',
        icon: Plug,
        heading: 'Connections',
        description: 'Connect the outside services Hearth reads from, like Spotify and Google Calendar.',
    },
    {
        id: 'frame',
        label: 'Frame',
        subtitle: 'Modes · e-paper look',
        icon: Tv,
        heading: 'Frame',
        description:
            'Control what the e-paper frame shows, when, and how it looks. These settings are shared by every device.',
    },
] as const satisfies readonly {
    id: string;
    label: string;
    subtitle: string;
    icon: Component;
    heading: string;
    description: string;
}[];

export type SettingsSection = (typeof settingsSections)[number]['id'];

export const DEFAULT_SETTINGS_SECTION: SettingsSection = 'display';

export function parseSettingsSection(value: string | null): SettingsSection {
    return settingsSections.find((s) => s.id === value)?.id ?? DEFAULT_SETTINGS_SECTION;
}

export function settingsSection(id: SettingsSection) {
    return settingsSections.find((s) => s.id === id)!;
}

export function settingsHref(section: SettingsSection): string {
    return `/settings?section=${section}`;
}

// The id a Settings card is given, so the sidebar can scroll to it.
export function settingsCardId(title: string): string {
    return `settings-card-${title
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-|-$/g, '')}`;
}
