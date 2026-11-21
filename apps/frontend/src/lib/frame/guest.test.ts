import { describe, expect, it } from 'vitest';
import { escapeWifi, qrModules, wifiQrText } from './guest.ts';

describe('escapeWifi', () => {
    it('escapes the reserved characters', () => {
        expect(escapeWifi('a\\b;c,d:e"f')).toBe('a\\\\b\\;c\\,d\\:e\\"f');
    });

    it('leaves everything else, spaces included', () => {
        expect(escapeWifi(' Hearth Guest ')).toBe(' Hearth Guest ');
    });
});

describe('wifiQrText', () => {
    it('spells a WPA network with its password', () => {
        expect(wifiQrText('Hearth Guest', 'tea;time', 'WPA')).toBe('WIFI:T:WPA;S:Hearth Guest;P:tea\\;time;;');
    });

    it('leaves out the password for an open network', () => {
        expect(wifiQrText('Cafe', 'ignored', 'nopass')).toBe('WIFI:T:nopass;S:Cafe;;');
    });

    it('treats a secured network with no password as open', () => {
        expect(wifiQrText('Cafe', '', 'WPA')).toBe('WIFI:T:nopass;S:Cafe;;');
    });

    it('needs a network name', () => {
        expect(wifiQrText('', 'secret', 'WPA')).toBeNull();
    });
});

describe('qrModules', () => {
    it('makes a square matrix starting with a finder pattern', () => {
        const modules = qrModules('WIFI:T:WPA;S:Hearth Guest;P:secret;;');
        expect(modules.length).toBeGreaterThanOrEqual(21);
        expect(modules.every((row) => row.length === modules.length)).toBe(true);
        // The top-left finder: a dark 7-module ring
        expect(modules[0].slice(0, 7).every(Boolean)).toBe(true);
        expect(modules[1][1]).toBe(false);
    });
});
