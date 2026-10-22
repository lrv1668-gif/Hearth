import { describe, expect, it } from 'vitest';
import { parseSettingsSection, settingsHref } from './settingsSections';

describe('parseSettingsSection', () => {
    it('returns a known section', () => {
        expect(parseSettingsSection('frame')).toBe('frame');
    });

    it('falls back to display when no section is given', () => {
        expect(parseSettingsSection(null)).toBe('display');
    });

    it('falls back to display for an unknown or mis-cased section', () => {
        expect(parseSettingsSection('bogus')).toBe('display');
        expect(parseSettingsSection('Frame')).toBe('display');
    });
});

describe('settingsHref', () => {
    it('links to the section through the query string', () => {
        expect(settingsHref('connections')).toBe('/settings?section=connections');
    });
});
