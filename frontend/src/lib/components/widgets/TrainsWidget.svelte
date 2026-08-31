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

    // Renders one or more upcoming times for a single line as a compact list, e.g.
    // "5, 20, 38 min" — the unit only appears once, after the last time.
    function timesLabel(times: (number | null)[]): string {
        const valid = times.filter((m): m is number => m !== null);
        if (valid.length === 0) return '';
        if (valid.length === 1) return minutesLabel(valid[0]);
        const last = valid[valid.length - 1];
        const rest = valid.slice(0, -1).map((m) => (m <= 0 ? 'due' : String(m)));
        return `${rest.join(', ')}, ${minutesLabel(last)}`;
    }

    const MAX_TIMES_PER_LINE = 3;

    function isBus(mode: string): boolean {
        return mode === 'bus' || mode === 'trolleybus';
    }

    interface DisplayLine {
        key: string;
        routeShortName: string;
        headsign: string | null;
        mode: string;
        times: (number | null)[];
    }

    const displayGroups = $derived(
        trainsStore.groups.map((group) => {
            const byLine = new Map<string, DisplayLine>();
            for (const d of group.departures) {
                const key = `${d.route_short_name}|${d.headsign ?? ''}|${d.mode}`;
                let line = byLine.get(key);
                if (!line) {
                    line = { key, routeShortName: d.route_short_name, headsign: d.headsign, mode: d.mode, times: [] };
                    byLine.set(key, line);
                }
                line.times.push(minutesUntil(d.estimated_departure ?? d.scheduled_departure ?? ''));
            }
            let lines = Array.from(byLine.values())
                .map((line) => ({
                    ...line,
                    times: line.times.sort((a, b) => (a ?? Infinity) - (b ?? Infinity)).slice(0, MAX_TIMES_PER_LINE),
                }))
                .sort((a, b) => (a.times[0] ?? Infinity) - (b.times[0] ?? Infinity));

            // An unset filter means "show all"; a set filter (even []) means "show only these" —
            // [] is how the Settings "Deselect all" control hides every line for a stop.
            const lineFilter = settings.trainStops.find((s) => s.stopKey === group.stop_key)?.lineFilter;
            if (lineFilter) {
                const allowed = new Set(lineFilter);
                lines = lines.filter((line) => allowed.has(line.key));
            }

            // Cluster by mode (buses vs trains) while keeping each cluster soonest-first;
            // whichever cluster has the single most urgent line leads the stop.
            const trainLines = lines.filter((line) => !isBus(line.mode));
            const busLines = lines.filter((line) => isBus(line.mode));
            const trainSoonest = trainLines[0]?.times[0] ?? Infinity;
            const busSoonest = busLines[0]?.times[0] ?? Infinity;
            lines = trainSoonest <= busSoonest ? [...trainLines, ...busLines] : [...busLines, ...trainLines];

            return {
                stopKey: group.stop_key,
                label: stopLabel(group.stop_key, group.stop_name),
                lines,
            };
        })
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
    {:else if displayGroups.every((g) => g.lines.length === 0)}
        <p class="type-label text-(--text-3)">No upcoming departures.</p>
    {:else}
        <div class="relative">
            <div
                bind:this={listEl}
                class="scroll-thin flex max-h-[min(25vh,320px)] flex-col gap-3 overflow-y-auto"
                onscroll={updateAtBottom}
            >
                {#each displayGroups as group (group.stopKey)}
                    {#if group.lines.length > 0}
                        <div class="flex flex-col gap-1.5">
                            <p class="type-label font-medium text-(--text-2)">{group.label}</p>
                            <ul class="flex flex-col gap-1.5">
                                {#each group.lines as line (line.key)}
                                    <li class="flex items-center gap-2.5">
                                        {#if isBus(line.mode)}
                                            <Bus class="icon-sm shrink-0 text-(--text-3)" />
                                        {:else}
                                            <TrainFront class="icon-sm shrink-0 text-(--text-3)" />
                                        {/if}
                                        <p class="type-body min-w-0 flex-1 truncate text-(--text-1)">
                                            <span class="font-medium">{line.routeShortName}</span>
                                            {#if line.headsign}
                                                <span class="text-(--text-2)">→</span>
                                                <span class="text-(--text-1)">{line.headsign}</span>
                                            {/if}
                                        </p>
                                        <span class="type-label shrink-0 text-(--text-2)">
                                            {timesLabel(line.times)}
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
