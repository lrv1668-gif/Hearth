// Runs tasks one at a time, each under a hard deadline. A task that never settles would
// otherwise stall every task behind it for good; past the deadline its caller gets a
// TimeoutError, onTimeout runs (e.g. to throw away a wedged browser) and the next task starts.

export class TimeoutError extends Error {
    name = 'TimeoutError';
}

export function createQueue({ deadlineMs, onTimeout = () => {} }) {
    let tail = Promise.resolve();
    let pending = 0;

    function run(task) {
        pending++;
        const result = tail.then(() => withDeadline(task)).finally(() => pending--);
        tail = result.catch(() => {});
        return result;
    }

    function withDeadline(task) {
        const timedOut = new AbortController();
        let timer;
        const deadline = new Promise((_, reject) => {
            timer = setTimeout(() => {
                timedOut.abort();
                reject(new TimeoutError(`render took longer than ${deadlineMs} ms`));
                onTimeout();
            }, deadlineMs);
        });
        return Promise.race([Promise.resolve().then(() => task(timedOut.signal)), deadline]).finally(() =>
            clearTimeout(timer)
        );
    }

    return {
        run,
        get pending() {
            return pending;
        },
    };
}
