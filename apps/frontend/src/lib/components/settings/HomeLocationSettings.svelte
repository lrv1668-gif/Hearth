<script lang="ts">
    import { onMount } from 'svelte';
    import { LocateFixed, Search } from '@lucide/svelte';
    import { api, HomeSettingsRejected, type Place } from '$lib/api';
    import {
        browserTimeZone,
        canonicalTimeZone,
        homeLocation,
        isTimeZone,
        knownTimeZones,
        loadHomeLocation,
        saveHomeLocation,
    } from '$lib/stores/HomeLocationStore.svelte.ts';
    import Button from '../ui/Button.svelte';
    import Field from '../ui/Field.svelte';
    import InlineMessage from '../ui/InlineMessage.svelte';
    import TextInput from '../ui/TextInput.svelte';

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
        if (query.trim().length < 2 || searching) return;
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
                time_zone: timeZone.trim() ? canonicalTimeZone(timeZone.trim()) : null,
            });
            resetDraft();
            status = { kind: 'saved', text: 'Saved. Weather, birds and the almanac update within a minute.' };
        } catch (e) {
            status = {
                kind: 'error',
                text:
                    e instanceof HomeSettingsRejected
                        ? `Couldn’t save: ${e.message}.`
                        : 'Couldn’t save — is the Display service running?',
            };
        } finally {
            saving = false;
        }
    }
    import SettingsGroup from '../ui/SettingsGroup.svelte';
</script>

{#if homeLocation.error}
    <InlineMessage tone="error" class="mb-4">
        Can't reach the Display service — the location can't be loaded or saved. Services use their .env values
        meanwhile.
    </InlineMessage>
{:else if homeLocation.loaded && !homeLocation.saved}
    <p class="type-label mb-4 text-(--text-2)">
        Nothing saved yet — the services use LATITUDE, LONGITUDE and TZ from their .env files, if set.
    </p>
{/if}
<div class="flex flex-col gap-5">
    <SettingsGroup title="Find it">
        <div class="flex flex-col gap-3">
            <div class="flex flex-col gap-1">
                <div>
                    <Button onclick={useMyLocation} disabled={!canLocate || locating}>
                        <LocateFixed class="icon-sm" />
                        {locating ? 'Locating…' : 'Use my location'}
                    </Button>
                </div>
                {#if !canLocate}
                    <p class="type-label text-(--text-2)">
                        Your browser only shares its location with pages on https or localhost — search for a city or
                        type coordinates instead.
                    </p>
                {/if}
            </div>
            <form onsubmit={search} class="flex gap-2">
                <TextInput
                    type="search"
                    bind:value={query}
                    placeholder="Search for a city"
                    aria-label="Search for a city"
                    class="w-full"
                />
                <Button type="submit" disabled={query.trim().length < 2 || searching}>
                    <Search class="icon-sm" />
                    {searching ? 'Searching…' : 'Search'}
                </Button>
            </form>
            {#if places}
                <div class="flex flex-col">
                    {#each places as place}
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
    </SettingsGroup>

    <SettingsGroup title="Location">
        <div class="flex flex-col gap-3">
            <Field label="Name">
                <TextInput
                    type="text"
                    bind:value={name}
                    placeholder="Optional, e.g. Home"
                    maxlength={100}
                    class="w-full"
                />
            </Field>
            <div class="grid grid-cols-2 gap-2">
                <Field label="Latitude">
                    <TextInput
                        type="text"
                        inputmode="decimal"
                        bind:value={latitude}
                        placeholder="e.g. 39.7392"
                        class="w-full"
                    />
                </Field>
                <Field label="Longitude">
                    <TextInput
                        type="text"
                        inputmode="decimal"
                        bind:value={longitude}
                        placeholder="e.g. -104.9903"
                        class="w-full"
                    />
                </Field>
            </div>
        </div>
    </SettingsGroup>

    <SettingsGroup title="Time zone" description="The frame's date and clock, and the almanac's sunrise milestones.">
        <div class="flex flex-col gap-3">
            <Field label="IANA zone">
                <TextInput
                    type="text"
                    list="home-time-zones"
                    bind:value={timeZone}
                    placeholder={thisBrowser}
                    autocomplete="off"
                    spellcheck="false"
                    class="w-full"
                />
                <datalist id="home-time-zones">
                    {#each zones as zone}<option value={zone}></option>{/each}
                </datalist>
            </Field>
            {#if homeLocation.loaded && !homeLocation.timeZone && timeZone === thisBrowser}
                <p class="type-label text-(--text-2)">From this browser — saved along with the location.</p>
            {:else if timeZone !== thisBrowser}
                <div>
                    <Button variant="link" onclick={() => (timeZone = thisBrowser)}>
                        Use this browser's zone ({thisBrowser})
                    </Button>
                </div>
            {/if}
        </div>
    </SettingsGroup>

    <div class="flex flex-col gap-2 border-t border-(--border) pt-4 sm:flex-row sm:items-center sm:justify-between">
        <p
            class="type-label {status?.kind === 'error' || problem ? 'text-(--danger)' : 'text-(--text-2)'}"
            aria-live="polite"
        >
            {problem ?? status?.text ?? ''}
        </p>
        <div class="flex justify-end">
            <Button
                variant="primary"
                onclick={save}
                disabled={!!problem || saving || !homeLocation.loaded || homeLocation.error}
            >
                {saving ? 'Saving…' : 'Save'}
            </Button>
        </div>
    </div>
</div>
