import { describe, expect, it } from 'vitest';
import type { FrameStatus } from '#lib/api.ts';
import { checkIn, timeAgo } from './status.ts';

describe('checkIn', () => {
    const now = new Date('2026-10-24T12:00:00Z');
    const status = (poll: FrameStatus['last_poll'], etag = '"a"'): FrameStatus => ({
        mode: 'daily',
        rendered_at: null,
        checked_at: null,
        next_render_at: null,
        etag,
        last_error: null,
        last_poll: poll,
    });
    const polled = (minutesAgo: number, etag: string | null = '"a"') => ({
        at: new Date(now.getTime() - minutesAgo * 60_000).toISOString(),
        address: '192.168.1.42',
        user_agent: 'inkypull',
        etag,
    });

    it('says when the frame has the latest picture', () => {
        expect(checkIn(status(polled(3)), now)).toEqual({
            text: 'The frame checked in 3 min ago and has the latest picture.',
            stale: false,
        });
    });

    it('says the latest picture is on its way when the frame holds an older one', () => {
        expect(checkIn(status(polled(0, '"old"')), now).text).toBe(
            'The frame checked in just now; the latest picture reaches it on its next check.'
        );
    });

    it('warns once the frame has been quiet for over an hour', () => {
        expect(checkIn(status(polled(180)), now)).toEqual({
            text: 'The frame checked in 3 hours ago. Check its power and Wi-Fi.',
            stale: true,
        });
    });

    it('warns when nothing has polled since Hearth started', () => {
        expect(checkIn(status(null), now).stale).toBe(true);
    });
});

describe('timeAgo', () => {
    it('counts minutes, hours and days', () => {
        expect(timeAgo(30_000)).toBe('just now');
        expect(timeAgo(59 * 60_000)).toBe('59 min ago');
        expect(timeAgo(60 * 60_000)).toBe('1 hour ago');
        expect(timeAgo(49 * 60 * 60_000)).toBe('2 days ago');
    });
});
