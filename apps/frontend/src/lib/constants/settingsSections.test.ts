import { describe, expect, it } from 'vitest';
import { parseSettingsSection, settingsCardId, settingsHref } from './settingsSections';

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
        expect(parseSettingsSection('constructor')).toBe('display');
    });

    it('sends a renamed section to its new name', () => {
        expect(parseSettingsSection('ambient')).toBe('photos');
    });
});

describe('settingsHref', () => {
    it('links to the section through the query string', () => {
        expect(settingsHref('connections')).toBe('/settings?section=connections');
    });
});

describe('settingsCardId', () => {
    it('slugs the card title under a settings prefix', () => {
        expect(settingsCardId('Photo, Countdown & Note')).toBe('settings-card-photo-countdown-note');
        expect(settingsCardId('Trains & Buses')).toBe('settings-card-trains-buses');
    });
});
