import { describe, expect, it } from 'vitest';
import { api, HomeSettingsRejected, type HomeSettings } from '#lib/api.ts';
import { mockFetch } from '#lib/test/fetch.ts';

const { respond, respondRaw, unreachable, call } = mockFetch();

const home: HomeSettings = {
    latitude: 47.6,
    longitude: -122.3,
    time_zone: 'America/Los_Angeles',
    name: 'Seattle',
};

describe('GET helpers', () => {
    it('returns the parsed body on success', async () => {
        respond(200, [{ id: 1 }]);
        expect(await api.tasks.list()).toEqual([{ id: 1 }]);
    });

    it('returns null on an error status', async () => {
        respond(500, { error: 'boom' });
        expect(await api.tasks.list()).toBeNull();
    });

    it('returns null when the service is unreachable', async () => {
        unreachable();
        expect(await api.weather.current()).toBeNull();
    });

    it('returns null when the body is not JSON', async () => {
        respondRaw(200, '<html>Bad gateway</html>');
        expect(await api.quote.today()).toBeNull();
    });
});

describe('JSON writes', () => {
    it('posts the body as JSON', async () => {
        respond(200, { id: 7 });
        await api.tasks.create({ title: 'Water plants' });

        const { url, init } = call();
        expect(url).toBe('/tasks');
        expect(init?.method).toBe('POST');
        expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json');
        expect(JSON.parse(String(init?.body))).toEqual({ title: 'Water plants' });
    });

    it('throws when a POST fails', async () => {
        respond(400);
        await expect(api.tasks.create({ title: '' })).rejects.toThrow('POST /tasks failed: 400');
    });

    it('throws when a PUT fails', async () => {
        respond(500);
        await expect(api.tasks.update(3, { done: true, title: 'x' })).rejects.toThrow('PUT /tasks/3 failed: 500');
    });

    it('reports PATCH and DELETE success as a boolean', async () => {
        respond(204);
        respond(404);
        expect(await api.photos.setCaption('abc', 'Sunset')).toBe(true);
        expect(await api.photos.delete('abc')).toBe(false);
        expect(call(0).init?.method).toBe('PATCH');
        expect(call(1).init?.method).toBe('DELETE');
    });
});

describe('query strings', () => {
    it('deletes a whole series only when asked', async () => {
        respond(204);
        respond(204);
        await api.tasks.delete(5);
        await api.tasks.delete(5, true);
        expect(call(0).url).toBe('/tasks/5');
        expect(call(1).url).toBe('/tasks/5?series=true');
    });

    it('repeats the url param for each feed', async () => {
        respond(200, []);
        await api.rss.articles(['https://a.example/feed', 'https://b.example/rss?x=1'], 5);

        const params = new URL(call().url, 'http://hearth').searchParams;
        expect(params.get('count')).toBe('5');
        expect(params.getAll('url')).toEqual(['https://a.example/feed', 'https://b.example/rss?x=1']);
    });

    it('repeats the stop param for each stop', async () => {
        respond(200, []);
        await api.trains.departures(['s-1', 's-2']);
        expect(new URL(call().url, 'http://hearth').searchParams.getAll('stop')).toEqual(['s-1', 's-2']);
    });

    it('encodes path segments', async () => {
        respond(204);
        await api.sports.unfollow('soccer/usa', 'a b');
        expect(call().url).toBe('/sports/followed/soccer%2Fusa/a%20b');
    });

    it('clears an override for one photo only when given its id', async () => {
        respond(204);
        respond(204);
        await api.display.clearOverride();
        await api.display.clearOverride('p 1');
        expect(call(0).url).toBe('/display/mode/override');
        expect(call(1).url).toBe('/display/mode/override?photo_id=p+1');
    });
});

describe('spotify.nowPlaying', () => {
    it('is undefined when Spotify is not connected', async () => {
        respond(401);
        expect(await api.spotify.nowPlaying()).toBeUndefined();
    });

    it('is null when nothing is playing', async () => {
        respond(204);
        expect(await api.spotify.nowPlaying()).toBeNull();
    });

    it('returns the track', async () => {
        respond(200, { title: 'Clair de Lune' });
        expect(await api.spotify.nowPlaying()).toEqual({ title: 'Clair de Lune' });
    });

    it('throws when Spotify cannot be reached', async () => {
        respond(502);
        await expect(api.spotify.nowPlaying()).rejects.toThrow('502');
    });
});

describe('spotify.recent', () => {
    it.each([
        [401, 'disconnected'],
        [403, 'reconnect'],
        [204, null],
    ])('maps %i to %s', async (status, expected) => {
        respond(status);
        expect(await api.spotify.recent()).toBe(expected);
    });

    it('throws on other errors', async () => {
        respond(500);
        await expect(api.spotify.recent()).rejects.toThrow('500');
    });
});

describe('display.settings', () => {
    it('is undefined when Display is unreachable', async () => {
        unreachable();
        expect(await api.display.settings()).toBeUndefined();
    });

    it('is undefined on an error status', async () => {
        respond(500);
        expect(await api.display.settings()).toBeUndefined();
    });

    it('is null when nothing is saved yet', async () => {
        respond(204);
        expect(await api.display.settings()).toBeNull();
    });

    it('bounds the request with a timeout', async () => {
        respond(204);
        await api.display.settings();
        expect(call().init?.signal).toBeInstanceOf(AbortSignal);
    });
});

describe('display.home', () => {
    it('is undefined when Display is unreachable', async () => {
        unreachable();
        expect(await api.display.home()).toBeUndefined();
    });

    it('is null when no home is saved yet', async () => {
        respond(204);
        expect(await api.display.home()).toBeNull();
    });

    it('is undefined when the body is not JSON', async () => {
        respondRaw(200, 'oops');
        expect(await api.display.home()).toBeUndefined();
    });

    it('returns the saved home', async () => {
        respond(200, home);
        expect(await api.display.home()).toEqual(home);
    });
});

describe('display.saveHome', () => {
    it("throws HomeSettingsRejected with Display's reason on a 400", async () => {
        respond(400, 'latitude out of range');
        const error = await api.display.saveHome(home).catch((e: unknown) => e);
        expect(error).toBeInstanceOf(HomeSettingsRejected);
        expect((error as Error).message).toBe('latitude out of range');
    });

    it('throws a plain error on other failures', async () => {
        respond(500);
        const error = await api.display.saveHome(home).catch((e: unknown) => e);
        expect(error).not.toBeInstanceOf(HomeSettingsRejected);
        expect((error as Error).message).toBe('PUT /display/home failed: 500');
    });

    it('returns the saved home', async () => {
        respond(200, home);
        expect(await api.display.saveHome(home)).toEqual(home);
    });
});

describe('display.mode and next', () => {
    it('are null when Display is unreachable', async () => {
        unreachable();
        unreachable();
        expect(await api.display.mode()).toBeNull();
        expect(await api.display.next()).toBeNull();
    });

    it('steps to the next mode with a body-less POST', async () => {
        respond(200, { mode: 'art', reason: 'override', until: null, photo_id: null });
        expect(await api.display.next()).toEqual({ mode: 'art', reason: 'override', until: null, photo_id: null });
        expect(call().init?.method).toBe('POST');
        expect(call().init?.body).toBeUndefined();
    });
});

describe('photos', () => {
    it('falls back to Unsplash when sources fail', async () => {
        respond(500);
        expect(await api.photos.sources()).toEqual(['unsplash']);
    });

    it('falls back to an empty list when uploads fail', async () => {
        unreachable();
        respond(500);
        expect(await api.photos.list()).toEqual([]);
        expect(await api.photos.groups()).toEqual([]);
    });

    it('marks every file too large on a 413', async () => {
        respond(413);
        const results = await api.photos.upload([new File(['a'], 'a.jpg'), new File(['b'], 'b.jpg')]);
        expect(results.map((r) => [r.file_name, r.status])).toEqual([
            ['a.jpg', 'error'],
            ['b.jpg', 'error'],
        ]);
        expect(results[0].error).toContain('batch too large');
    });

    it('marks every file failed on other errors', async () => {
        respond(500);
        const [result] = await api.photos.upload([new File(['a'], 'a.jpg')]);
        expect(result.error).toBe('upload failed');
    });

    it('sends each file under the same form field', async () => {
        respond(200, []);
        await api.photos.upload([new File(['a'], 'a.jpg'), new File(['b'], 'b.jpg')]);
        const form = call().init?.body as FormData;
        expect(form.getAll('file').map((f) => (f as File).name)).toEqual(['a.jpg', 'b.jpg']);
    });
});

describe('logs.running', () => {
    it('is false when Dozzle is off', async () => {
        respond(502);
        expect(await api.logs.running()).toBe(false);
    });

    it('is false when the request fails', async () => {
        unreachable();
        expect(await api.logs.running()).toBe(false);
    });

    it('is true when Dozzle answers', async () => {
        respondRaw(200, '<html></html>');
        expect(await api.logs.running()).toBe(true);
    });
});
