import { defaultKeybindingsHandlerIgnore } from 'tinykeys';
import { settingsSections } from '#lib/constants/settingsSections.ts';

// Keyboard shortcuts, bound with tinykeys where they act (Settings, and
// KeyboardShortcuts.svelte for the global ones). "Mod" is ⌘ on a Mac and Ctrl everywhere
// else — tinykeys' `$mod`. This file holds the shared filters and the list the "?" sheet shows.

// tinykeys picks `$mod` from navigator.platform, so the labels read it too.
export function isMacPlatform(platform: string): boolean {
    return /mac|iphone|ipad|ipod/i.test(platform);
}

/** Single-key shortcuts: skip typing in a field (tinykeys' default) and an open dialog. */
export function ignoreWhileTyping(e: KeyboardEvent): boolean {
    return defaultKeybindingsHandlerIgnore(e) || e.defaultPrevented || !!document.querySelector('dialog[open]');
}

/** Mod shortcuts mean nothing to a plain field, so they only skip rich-text editing. */
export function ignoreWhileEditing(e: KeyboardEvent): boolean {
    return e.repeat || e.isComposing || !!(e.target as Element | null)?.closest?.('[contenteditable]');
}

/** `['Mod', 'B']` → `⌘B` on a Mac, `Ctrl+B` elsewhere. */
export function formatKeys(keys: readonly string[], mac: boolean): string {
    const named = keys.map((k) => (k === 'Mod' ? (mac ? '⌘' : 'Ctrl') : k));
    return mac ? named.join('') : named.join('+');
}

export type Shortcut = {
    /** Each entry is one key combo; `then` joins a chord like G then D, `or` alternatives. */
    keys: readonly (readonly string[])[];
    join?: 'then' | 'or';
    description: string;
    scope: 'global' | 'settings';
};

export const SIDEBAR_KEYS = ['Mod', 'B'] as const;

// The "G then …" chord: the second key and where it goes.
export const goToRoutes = [
    { key: 'd', href: '/', label: 'Dashboard' },
    { key: 'c', href: '/calendar', label: 'Calendar' },
    { key: 'a', href: '/ambient', label: 'Ambient' },
    { key: 's', href: '/settings', label: 'Settings' },
] as const;

export const shortcuts: Shortcut[] = [
    ...goToRoutes.map((r) => ({
        keys: [['G'], [r.key.toUpperCase()]],
        join: 'then' as const,
        description: `Go to ${r.label}`,
        scope: 'global' as const,
    })),
    { keys: [['?']], description: 'Show keyboard shortcuts', scope: 'global' },
    { keys: [SIDEBAR_KEYS], description: 'Hide or show the sidebar', scope: 'settings' },
    { keys: [[`1–${settingsSections.length}`]], description: 'Jump to a section', scope: 'settings' },
    { keys: [['J'], ['K']], join: 'or', description: 'Next / previous section', scope: 'settings' },
];
