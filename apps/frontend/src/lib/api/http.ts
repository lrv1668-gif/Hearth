// The request helpers every api/<service>.ts module builds on. Not re-exported from
// #lib/api.ts: callers go through an `api.<service>` namespace.

/** After `timeoutMs` a request counts as unreachable. */
export type Timeout = { timeoutMs?: number };

/** fetch options plus an optional timeout. */
type RequestOptions = Omit<RequestInit, 'signal'> & Timeout;

/** null when the service can't be reached, fails or doesn't answer with JSON. */
export async function get<T>(url: string, options?: Timeout): Promise<T | null> {
    const res = await tryFetch(url, options);
    if (!res?.ok) return null;
    return (res.json() as Promise<T>).catch(() => null);
}

/** For a setting that may not be saved yet: null on a 204, undefined when the service
 *  can't be reached, fails or doesn't answer with JSON. */
export async function getSaved<T>(url: string, options?: Timeout): Promise<T | null | undefined> {
    const res = await tryFetch(url, options);
    if (!res?.ok) return undefined;
    if (res.status === 204) return null;
    return (res.json() as Promise<T>).catch(() => undefined);
}

/** null instead of throwing when the service can't be reached or `timeoutMs` passes. */
export function tryFetch(url: string, { timeoutMs, ...init }: RequestOptions = {}): Promise<Response | null> {
    const signal = timeoutMs === undefined ? undefined : AbortSignal.timeout(timeoutMs);
    return fetch(url, { ...init, signal }).catch(() => null);
}

export async function post<T>(url: string, body: unknown): Promise<T> {
    const res = await sendJson('POST', url, body);
    if (!res.ok) throw new Error(`POST ${url} failed: ${res.status}`);
    return res.json() as Promise<T>;
}

export async function put<T>(url: string, body: unknown): Promise<T> {
    const res = await sendJson('PUT', url, body);
    if (!res.ok) throw new Error(`PUT ${url} failed: ${res.status}`);
    return res.json() as Promise<T>;
}

export async function patch(url: string, body: unknown): Promise<boolean> {
    return (await sendJson('PATCH', url, body)).ok;
}

export function sendJson(method: 'POST' | 'PUT' | 'PATCH', url: string, body: unknown): Promise<Response> {
    return fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
    });
}

export async function del(url: string): Promise<boolean> {
    const res = await fetch(url, { method: 'DELETE' });
    return res.ok;
}
