// Guest mode's Wi-Fi QR code: the WIFI: string phones read to join a network, and the
// module matrix to draw it from.
import { encode } from 'uqr';
import type { GuestSecurity } from '#lib/constants/frameModes.ts';

/** Backslash-escapes the characters the WIFI: format reserves: \ ; , : and ". */
export function escapeWifi(value: string): string {
    return value.replace(/([\\;,:"])/g, '\\$1');
}

/**
 * The string a phone's camera reads to join a network — `WIFI:T:WPA;S:name;P:pass;;` (the
 * ZXing format Android and iOS both take). An open network leaves the password out.
 * Null without a network name, since there's nothing to join.
 */
export function wifiQrText(network: string, password: string, security: GuestSecurity): string | null {
    if (!network) return null;
    const open = security === 'nopass' || !password;
    const parts = [`T:${open ? 'nopass' : security}`, `S:${escapeWifi(network)}`];
    if (!open) parts.push(`P:${escapeWifi(password)}`);
    return `WIFI:${parts.join(';')};;`;
}

/** The QR code's modules, `true` for a dark one, with no border — the frame draws its own
 *  quiet zone. Medium error correction, so a speck of dust on the glass still scans. */
export function qrModules(text: string): boolean[][] {
    return encode(text, { ecc: 'M', border: 0 }).data;
}
