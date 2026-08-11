<script lang="ts">
    import { onMount } from 'svelte';
    import { Bus, TrainFront } from '@lucide/svelte';
    import { trainsStore, loadTrainDepartures } from '$lib/stores/TrainsStore.svelte.ts';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));
    let listEl = $state<HTMLDivElement | null>(null);
    let atBottom = $state(false);

    onMount(() => {
        loadPromise = loadTrainDepartures();
    });

    function updateAtBottom() {
        if (!listEl) return;
        atBottom = listEl.scrollTop + listEl.clientHeight >= listEl.scrollHeight - 1;
    }

    // Recheck when the list renders or its contents change, so the fade only
    // shows when there actually is more to scroll to.
    $effect(() => {
        trainsStore.groups;
        updateAtBottom();
    });

    function stopLabel(stopKey: string, fallback: string | null): string {
        return settings.trainStops.find((s) => s.stopKey === stopKey)?.label ?? fallback ?? stopKey;
    }

    // Departure times are "HH:MM:SS" in the stop's local time (GTFS allows hours >= 24 for
    // post-midnight trips), with no date attached — compare against wall-clock time-of-day.
    function minutesUntil(time: string): number | null {
        const match = time.match(/^(\d{1,2}):(\d{2}):(\d{2})$/);
        if (!match) return null;
        const [, h, m, s] = match.map(Number);
        const now = new Date();
        const nowSeconds = now.getHours() * 3600 + now.getMinutes() * 60 + now.getSeconds();
        let diff = h * 3600 + m * 60 + s - nowSeconds;
        if (diff < -3600) diff += 24 * 3600;
        return Math.round(diff / 60);
    }

    function minutesLabel(minutes: number | null): string {
        if (minutes === null) return '';
        if (minutes <= 0) return 'due';
        if (minutes === 1) return '1 min';
        return `${minutes} min`;
    }

    interface DisplayDeparture {
        routeShortName: string;
        headsign: string | null;
        mode: string;
        minutes: number | null;
    }

    const displayGroups = $derived(
        trainsStore.groups.map((group) => ({
            stopKey: group.stop_key,
            label: stopLabel(group.stop_key, group.stop_name),
            departures: group.departures
                .map(
                    (d): DisplayDeparture => ({
                        routeShortName: d.route_short_name,
                        headsign: d.headsign,
                        mode: d.mode,
                        minutes: minutesUntil(d.estimated_departure ?? d.scheduled_departure ?? ''),
                    })
                )
                .sort((a, b) => (a.minutes ?? Infinity) - (b.minutes ?? Infinity)),
        }))
    );
</script>

<SkeletonLoader promise={loadPromise}>
    {#if settings.trainStops.length === 0}
        <div class="flex flex-col gap-1 py-4 text-(--text-3)">
            <TrainFront class="icon-sm mb-1" />
            <p class="type-label">No stops added yet.</p>
            <a href="/settings" class="type-label text-(--accent) transition-colors hover:text-(--accent-hi)">
                Add a stop in Settings →
            </a>
        </div>
    {:else if displayGroups.every((g) => g.departures.length === 0)}
        <p class="type-label text-(--text-3)">No upcoming departures.</p>
    {:else}
        <div class="relative">
            <div
                bind:this={listEl}
                class="scroll-thin flex max-h-[min(25vh,320px)] flex-col gap-3 overflow-y-auto"
                onscroll={updateAtBottom}
            >
                {#each displayGroups as group (group.stopKey)}
                    {#if group.departures.length > 0}
                        <div class="flex flex-col gap-1.5">
                            <p class="type-label font-medium text-(--text-2)">{group.label}</p>
                            <ul class="flex flex-col gap-1.5">
                                {#each group.departures as departure, i (i)}
                                    <li class="flex items-center gap-2.5">
                                        {#if departure.mode === 'bus' || departure.mode === 'trolleybus'}
                                            <Bus class="icon-sm shrink-0 text-(--text-3)" />
                                        {:else}
                                            <TrainFront class="icon-sm shrink-0 text-(--text-3)" />
                                        {/if}
                                        <p class="type-body min-w-0 flex-1 truncate text-(--text-1)">
                                            <span class="font-medium">{departure.routeShortName}</span>
                                            {#if departure.headsign}
                                                <span class="text-(--text-2)">{departure.headsign}</span>
                                            {/if}
                                        </p>
                                        <span class="type-label shrink-0 text-(--text-2)">
                                            {minutesLabel(departure.minutes)}
                                        </span>
                                    </li>
                                {/each}
                            </ul>
                        </div>
                    {/if}
                {/each}
            </div>
            {#if !atBottom}
                <div
                    class="pointer-events-none absolute right-0 bottom-0 left-0 h-8"
                    style="background: linear-gradient(to bottom, transparent, var(--bg))"
                ></div>
            {/if}
        </div>
    {/if}
</SkeletonLoader>
