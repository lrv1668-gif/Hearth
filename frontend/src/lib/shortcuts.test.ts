import { describe, expect, it } from 'vitest';
import { formatKeys, ignoreWhileEditing, isMacPlatform } from './shortcuts';

describe('isMacPlatform', () => {
    it('recognises macOS and iPad', () => {
        expect(isMacPlatform('MacIntel')).toBe(true);
        expect(isMacPlatform('macOS')).toBe(true);
        expect(isMacPlatform('iPad')).toBe(true);
    });

    it('treats Windows and Linux as Ctrl platforms', () => {
        expect(isMacPlatform('Win32')).toBe(false);
        expect(isMacPlatform('Linux x86_64')).toBe(false);
    });
});

describe('ignoreWhileEditing', () => {
    const event = (target: object | null, extra: Partial<KeyboardEvent> = {}) =>
        ({ repeat: false, isComposing: false, target, ...extra }) as KeyboardEvent;
    const el = (editable: boolean) => ({
        closest: (sel: string) => (editable && sel === '[contenteditable]' ? {} : null),
    });

    it('lets Mod shortcuts through from plain fields and the page', () => {
        expect(ignoreWhileEditing(event(el(false)))).toBe(false);
        expect(ignoreWhileEditing(event(null))).toBe(false);
    });

    it('stands aside for rich-text editing, IME composition and key repeat', () => {
        expect(ignoreWhileEditing(event(el(true)))).toBe(true);
        expect(ignoreWhileEditing(event(el(false), { isComposing: true }))).toBe(true);
        expect(ignoreWhileEditing(event(el(false), { repeat: true }))).toBe(true);
    });
});

describe('formatKeys', () => {
    it('shows ⌘ joined on a Mac', () => {
        expect(formatKeys(['Mod', 'B'], true)).toBe('⌘B');
    });

    it('shows Ctrl+ elsewhere', () => {
        expect(formatKeys(['Mod', 'B'], false)).toBe('Ctrl+B');
    });
});
