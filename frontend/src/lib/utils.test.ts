import { describe, expect, it } from 'vitest';
import { eventDateKey, formatTime, providerLabel, stripHtml } from './utils';

describe('stripHtml', () => {
    it('strips tags', () => {
        expect(stripHtml('<p>Hello <b>world</b></p>')).toBe('Hello world');
    });

    it('collapses repeated whitespace', () => {
        expect(stripHtml('a   b\n\nc')).toBe('a b c');
    });

    it('trims leading and trailing whitespace', () => {
        expect(stripHtml('  <div> padded </div>  ')).toBe('padded');
    });
});

describe('providerLabel', () => {
    it('maps google to Google Calendar', () => {
        expect(providerLabel('google')).toBe('Google Calendar');
    });

    it('passes through unknown providers unchanged', () => {
        expect(providerLabel('outlook')).toBe('outlook');
    });
});

describe('formatTime', () => {
    it('omits minutes on the hour', () => {
        expect(formatTime('13:00')).toBe('1PM');
    });

    it('zero-pads non-zero minutes', () => {
        expect(formatTime('13:05')).toBe('1:05PM');
    });

    it('treats hour 0 as 12AM', () => {
        expect(formatTime('00:00')).toBe('12AM');
    });

    it('treats hour 12 as 12PM', () => {
        expect(formatTime('12:00')).toBe('12PM');
    });
});

describe('eventDateKey', () => {
    it('returns empty string when start is missing', () => {
        expect(eventDateKey({ start: undefined, is_all_day: false })).toBe('');
        expect(eventDateKey({ start: null, is_all_day: true })).toBe('');
    });

    it('all-day event slices the date directly, avoiding UTC shift', () => {
        expect(eventDateKey({ start: '2026-06-15T00:00:00Z', is_all_day: true })).toBe('2026-06-15');
    });
});
