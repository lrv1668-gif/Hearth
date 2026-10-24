import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createQueue } from './queue.mjs';

test('runs tasks one at a time in order', async () => {
    const queue = createQueue({ deadlineMs: 1000 });
    const order = [];
    const slow = queue.run(async () => {
        await new Promise((resolve) => setTimeout(resolve, 20));
        order.push('slow');
        return 1;
    });
    const fast = queue.run(async () => {
        order.push('fast');
        return 2;
    });
    assert.equal(queue.pending, 2);
    assert.deepEqual(await Promise.all([slow, fast]), [1, 2]);
    assert.deepEqual(order, ['slow', 'fast']);
    assert.equal(queue.pending, 0);
});

test('a failed task does not stop the next', async () => {
    const queue = createQueue({ deadlineMs: 1000 });
    const failed = queue.run(async () => {
        throw new Error('boom');
    });
    const next = queue.run(async () => 'ok');
    await assert.rejects(failed, /boom/);
    assert.equal(await next, 'ok');
});

test('a task that never settles times out, aborts and lets the next run', async () => {
    let timeouts = 0;
    const queue = createQueue({ deadlineMs: 30, onTimeout: () => timeouts++ });
    let signal;
    const hung = queue.run((s) => {
        signal = s;
        return new Promise(() => {});
    });
    const next = queue.run(async () => 'ok');
    await assert.rejects(hung, { name: 'TimeoutError' });
    assert.equal(signal.aborted, true);
    assert.equal(timeouts, 1);
    assert.equal(await next, 'ok');
    assert.equal(queue.pending, 0);
});
