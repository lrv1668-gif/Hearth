import assert from 'node:assert/strict';
import { test } from 'node:test';
import { captureTimeZone, isTimeZone } from './timezone.mjs';

test('isTimeZone accepts IANA zones and rejects anything else', () => {
    assert.equal(isTimeZone('America/Chicago'), true);
    assert.equal(isTimeZone('UTC'), true);
    assert.equal(isTimeZone('Not/AZone'), false);
    assert.equal(isTimeZone(''), false);
    assert.equal(isTimeZone(undefined), false);
});

test('captureTimeZone prefers the requested zone over the fallback', () => {
    assert.deepEqual(captureTimeZone('Europe/London', 'America/Chicago'), { zone: 'Europe/London' });
});

test('captureTimeZone falls back to TZ when no zone is requested', () => {
    assert.deepEqual(captureTimeZone(null, 'America/Chicago'), { zone: 'America/Chicago' });
    assert.deepEqual(captureTimeZone('', undefined), { zone: undefined });
});

test('captureTimeZone reports an unknown requested zone', () => {
    const { zone, error } = captureTimeZone('Mars/Olympus', 'America/Chicago');
    assert.equal(zone, undefined);
    assert.match(error, /Mars\/Olympus/);
});
