<script lang="ts">
    import { onMount } from 'svelte';
    import { LocateFixed, Search } from '@lucide/svelte';
    import { api, type Place } from '$lib/api';
    import {
        browserTimeZone,
        homeLocation,
        isTimeZone,
        knownTimeZones,
        loadHomeLocation,
        saveHomeLocation,
    } from '$lib/stores/HomeLocationStore.svelte.ts';

    const inputClass =
        'type-body w-full rounded-lg border border-(--border) bg-(--surface) px-3 py-2 text-(--text-1) transition-colors outline-none placeholder:text-(--text-4) focus:border-(--text-3)';
    const buttonClass =
        'type-label inline-flex items-center gap-1.5 rounded-full border border-(--border) px-4 py-1.5 text-(--text-1) transition-colors hover:bg-(--surface-hi) disabled:pointer-events-none disabled:opacity-40';

    // The form edits a draft; Save sends it whole.
    let latitude = $state('');
    let longitude = $state('');
    let name = $state('');
    let timeZone = $state('');
    let status = $state<{ kind: 'saved' | 'error' | 'info'; text: string } | null>(null);
    let saving = $state(false);

    const thisBrowser = browserTimeZone();
    const zones = knownTimeZones();

    function resetDraft() {
        latitude = homeLocation.latitude?.toString() ?? '';
        longitude = homeLocation.longitude?.toString() ?? '';
        name = homeLocation.name ?? '';
        // Until a zone is saved, offer this browser's — but don't save it unasked: the
        // first device to open Settings may not be where the frame hangs.
        timeZone = homeLocation.timeZone ?? thisBrowser;
    }

    onMount(() => {
        loadHomeLocation().then(resetDraft);
    });

    // "Use my location" needs a secure context: https, or localhost
    const canLocate = typeof window !== 'undefined' && window.isSecureContext && 'geolocation' in navigator;
    let locating = $state(false);

    function useMyLocation() {
        locating = true;
        status = null;
        navigator.geolocation.getCurrentPosition(
            (pos) => {
                latitude = pos.coords.latitude.toFixed(4);
                longitude = pos.coords.longitude.toFixed(4);
                name = '';
                // This device is there now, so its zone is the place's
                timeZone = thisBrowser;
                locating = false;
                status = { kind: 'info', text: 'Location found — save to use it.' };
            },
            (err) => {
                locating = false;
                status = {
                    kind: 'error',
                    text:
                        err.code === err.PERMISSION_DENIED
                            ? 'Location permission was denied. Search for a city or type coordinates instead.'
                            : 'Couldn’t get this device’s location. Search for a city or type coordinates instead.',
                };
            },
            { timeout: 15000, maximumAge: 600000 }
        );
    }

    let query = $state('');
    let places = $state<Place[] | null>(null);
    let searching = $state(false);

    async function search(e: SubmitEvent) {
        e.preventDefault();
        if (query.trim().length < 2) return;
        searching = true;
        const found = await api.display.places(query.trim());
        searching = false;
        places = found ?? [];
        status = found ? null : { kind: 'error', text: 'City search isn’t available right now.' };
    }

    function pick(place: Place) {
        latitude = place.latitude.toFixed(4);
        longitude = place.longitude.toFixed(4);
        name = place.label;
        if (place.time_zone) timeZone = place.time_zone;
        places = null;
        query = '';
        status = { kind: 'info', text: `${place.label} — save to use it.` };
    }

    function coordinate(value: string, limit: number): number | null | undefined {
        if (value.trim() === '') return null;
        const n = Number(value);
        return Number.isFinite(n) && Math.abs(n) <= limit ? n : undefined;
    }

    const lat = $derived(coordinate(latitude, 90));
    const lon = $derived(coordinate(longitude, 180));
    const problem = $derived(
        lat === undefined
            ? 'Latitude must be a number from -90 to 90.'
            : lon === undefined
              ? 'Longitude must be a number from -180 to 180.'
              : (lat === null) !== (lon === null)
                ? 'Enter both latitude and longitude, or neither.'
                : timeZone.trim() !== '' && !isTimeZone(timeZone.trim())
                  ? 'Pick a time zone from the list, e.g. America/Chicago.'
                  : null
    );

    async function save() {
        if (problem) return;
        saving = true;
        try {
            await saveHomeLocation({
                latitude: lat ?? null,
                longitude: lon ?? null,
                name: name.trim() || null,
                time_zone: timeZone.trim() || null,
            });
            resetDraft();
            status = { kind: 'saved', text: 'Saved. Weather, birds and the almanac update within a minute.' };
        } catch {
            status = { kind: 'error', text: 'Couldn’t save — is the Display service running?' };
        } finally {
            saving = false;
        }
    }
</script>

<div class="flex flex-col gap-6">
    {#if homeLocation.error}
        <p class="type-label text-(--accent)">
            Can't reach the Display service — the location can't be loaded or saved. Services use their .env values
            meanwhile.
        </p>
    {:else if homeLocation.loaded && !homeLocation.saved}
        <p class="type-label text-(--text-2)">
            Nothing saved yet — the services use LATITUDE, LONGITUDE and TZ from their .env files, if set.
        </p>
    {/if}

    <div class="flex flex-col gap-3">
        <p class="type-body font-medium text-(--text-1)">Find it</p>
        <div class="flex flex-col gap-1">
            <div>
                <button onclick={useMyLocation} disabled={!canLocate || locating} class={buttonClass}>
                    <LocateFixed class="icon-sm" />
                    {locating ? 'Locating…' : 'Use my location'}
                </button>
            </div>
            {#if !canLocate}
                <p class="type-label text-(--text-2)">
                    Your browser only shares its location with pages on https or localhost — search for a city or type
                    coordinates instead.
                </p>
            {/if}
        </div>
        <form onsubmit={search} class="flex gap-2">
            <input
                type="search"
                bind:value={query}
                placeholder="Search for a city"
                aria-label="Search for a city"
                class={inputClass}
            />
            <button type="submit" disabled={query.trim().length < 2 || searching} class={buttonClass}>
                <Search class="icon-sm" />
                {searching ? 'Searching…' : 'Search'}
            </button>
        </form>
        {#if places}
            <div class="flex flex-col">
                {#each places as place (`${place.latitude},${place.longitude}`)}
                    <button
                        onclick={() => pick(place)}
                        class="flex flex-col rounded-lg px-3 py-2 text-left transition-colors hover:bg-(--surface-hi)"
                    >
                        <span class="type-body text-(--text-1)">{place.label}</span>
                        <span class="type-label text-(--text-2)">
                            {place.latitude.toFixed(2)}, {place.longitude.toFixed(2)}{place.time_zone
                                ? ` · ${place.time_zone}`
                                : ''}
                        </span>
                    </button>
                {:else}
                    <p class="type-label py-2 text-(--text-2)">No places match “{query}”.</p>
                {/each}
            </div>
        {/if}
    </div>

    <div class="flex flex-col gap-3 border-t border-(--border) pt-4">
        <p class="type-body font-medium text-(--text-1)">Location</p>
        <label class="flex flex-col gap-1">
            <span class="type-caption tracking-widest text-(--text-2) uppercase">Name</span>
            <input type="text" bind:value={name} placeholder="Optional, e.g. Home" maxlength="100" class={inputClass} />
        </label>
        <div class="grid grid-cols-2 gap-2">
            <label class="flex flex-col gap-1">
                <span class="type-caption tracking-widest text-(--text-2) uppercase">Latitude</span>
                <input type="text" inputmode="decimal" bind:value={latitude} placeholder="39.7392" class={inputClass} />
            </label>
            <label class="flex flex-col gap-1">
                <span class="type-caption tracking-widest text-(--text-2) uppercase">Longitude</span>
                <input
                    type="text"
                    inputmode="decimal"
                    bind:value={longitude}
                    placeholder="-104.9903"
                    class={inputClass}
                />
            </label>
        </div>
    </div>

    <div class="flex flex-col gap-3 border-t border-(--border) pt-4">
        <div>
            <p class="type-body font-medium text-(--text-1)">Time zone</p>
            <p class="type-label text-(--text-2)">The frame's date and clock, and the almanac's sunrise milestones.</p>
        </div>
        <label class="flex flex-col gap-1">
            <span class="type-caption tracking-widest text-(--text-2) uppercase">IANA zone</span>
            <input
                type="text"
                list="home-time-zones"
                bind:value={timeZone}
                placeholder={thisBrowser}
                autocomplete="off"
                spellcheck="false"
                class={inputClass}
            />
            <datalist id="home-time-zones">
                {#each zones as zone}<option value={zone}></option>{/each}
            </datalist>
        </label>
        {#if homeLocation.loaded && !homeLocation.timeZone && timeZone === thisBrowser}
            <p class="type-label text-(--text-2)">From this browser — not saved yet.</p>
        {:else if timeZone !== thisBrowser}
            <div>
                <button
                    onclick={() => (timeZone = thisBrowser)}
                    class="type-label text-(--accent) transition-colors hover:text-(--accent-hi)"
                >
                    Use this browser's zone ({thisBrowser})
                </button>
            </div>
        {/if}
    </div>

    <div class="flex flex-col gap-2 border-t border-(--border) pt-4 sm:flex-row sm:items-center sm:justify-between">
        <p
            class="type-label {status?.kind === 'error' || problem ? 'text-(--accent)' : 'text-(--text-2)'}"
            aria-live="polite"
        >
            {problem ?? status?.text ?? ''}
        </p>
        <div class="flex justify-end">
            <button
                onclick={save}
                disabled={!!problem || saving || !homeLocation.loaded || homeLocation.error}
                class={buttonClass}
            >
                {saving ? 'Saving…' : 'Save'}
            </button>
        </div>
    </div>
</div>
