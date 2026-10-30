import { afterEach, beforeEach, vi } from 'vitest';

/**
 * Stubs the global `fetch` for every test in the calling file, and returns helpers that queue
 * its answers in order. Call it once at the top level of a `*.test.ts` file.
 */
export function mockFetch() {
    const fetchMock = vi.fn<typeof fetch>();

    beforeEach(() => vi.stubGlobal('fetch', fetchMock));
    afterEach(() => {
        fetchMock.mockReset();
        vi.unstubAllGlobals();
    });

    return {
        fetchMock,
        /** The next call answers with `body` as JSON (no body when omitted). */
        respond: (status: number, body?: unknown) =>
            fetchMock.mockResolvedValueOnce(new Response(body === undefined ? null : JSON.stringify(body), { status })),
        /** The next call answers with `body` as-is. */
        respondRaw: (status: number, body: string) => fetchMock.mockResolvedValueOnce(new Response(body, { status })),
        /** The next call fails like an unreachable service. */
        unreachable: () => fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch')),
        /** The URL and options of the nth call. */
        call: (n = 0) => {
            const [input, init] = fetchMock.mock.calls[n];
            return { url: String(input), init };
        },
    };
}
