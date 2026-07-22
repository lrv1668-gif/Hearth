<script lang="ts">
    import { Sunrise, Sunset } from '@lucide/svelte';
    import { weatherStore } from '$lib/stores/WeatherStore.svelte.ts';
    import { taskStore } from '$lib/stores/TaskStore.svelte.ts';
    import { calendarStore } from '$lib/stores/CalendarStore.svelte.ts';
    import { eventDateKey } from '$lib/utils';

    interface Mark {
        at: Date;
        title: string;
    }

    let now = $state(new Date());

    $effect(() => {
        const id = setInterval(() => (now = new Date()), 60_000);
        return () => clearInterval(id);
    });

    function localDateKey(d: Date): string {
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    }

    const todayKey = $derived(localDateKey(now));
    const todayForecast = $derived(weatherStore.forecast.find((f) => f.date === todayKey) ?? null);

    // Open-Meteo returns local ISO times without an offset ("2026-07-17T05:43"),
    // which new Date() parses as local time — correct for a same-location display.
    const sunrise = $derived(todayForecast ? new Date(todayForecast.sunrise) : null);
    const sunset = $derived(todayForecast ? new Date(todayForecast.sunset) : null);

    // Fraction of the day [0, 1] for positioning along the ribbon.
    function frac(d: Date): number {
        return (d.getHours() * 60 + d.getMinutes()) / 1440;
    }

    function pct(d: Date): number {
        return frac(d) * 100;
    }

    const goldenStart = $derived(sunset ? new Date(sunset.getTime() - 3_600_000) : null);
    const morningGoldenEnd = $derived(sunrise ? new Date(sunrise.getTime() + 3_600_000) : null);
    const solarNoonPct = $derived(sunrise && sunset ? (pct(sunrise) + pct(sunset)) / 2 : null);

    const marks = $derived.by<Mark[]>(() => {
        const eventMarks = calendarStore.items
            .filter((e) => e.kind === 'event' && !e.is_all_day && e.start && eventDateKey(e) === todayKey)
            .map((e) => ({ at: new Date(e.start!), title: e.title }));

        const taskMarks = taskStore.tasks
            .filter((t) => !t.is_countdown && !t.done && t.due_date === todayKey && t.due_time)
            .map((t) => {
                const [h, m] = t.due_time!.split(':').map(Number);
                const at = new Date(now);
                at.setHours(h, m, 0, 0);
                return { at, title: t.title };
            });

        return [...eventMarks, ...taskMarks].sort((a, b) => a.at.getTime() - b.at.getTime());
    });

    const daylightLabel = $derived.by(() => {
        if (!sunrise || !sunset) return '';
        const minutes = Math.round((sunset.getTime() - sunrise.getTime()) / 60_000);
        return `${Math.floor(minutes / 60)}h ${minutes % 60}m of daylight`;
    });

    function clock(d: Date): string {
        return d.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
    }
</script>

{#if !sunrise || !sunset || !goldenStart || !morningGoldenEnd}
    <p class="type-label text-(--text-3)">Sun times unavailable — set up the Weather service.</p>
{:else}
    <div class="flex flex-col gap-2">
        <div class="relative h-8">
            <!-- night track -->
            <div class="absolute inset-x-0 top-1/2 h-2 -translate-y-1/2 rounded-full bg-(--surface-hi)"></div>

            <!-- daylight band -->
            <div
                class="absolute top-1/2 h-2 -translate-y-1/2 rounded-full bg-(--border)"
                style="left: {pct(sunrise)}%; width: {pct(sunset) - pct(sunrise)}%"
            ></div>

            <!-- golden hour bands -->
            <div
                class="absolute top-1/2 h-2 -translate-y-1/2 rounded-full bg-(--accent) opacity-40"
                style="left: {pct(sunrise)}%; width: {pct(morningGoldenEnd) - pct(sunrise)}%"
            ></div>
            <div
                class="absolute top-1/2 h-2 -translate-y-1/2 rounded-full bg-(--accent) opacity-40"
                style="left: {pct(goldenStart)}%; width: {pct(sunset) - pct(goldenStart)}%"
            ></div>

            <!-- solar noon tick -->
            {#if solarNoonPct !== null}
                <div
                    class="absolute top-1/2 h-3.5 w-px -translate-y-1/2 bg-(--text-4)"
                    style="left: {solarNoonPct}%"
                ></div>
            {/if}

            <!-- event and task marks -->
            {#each marks as mark (mark.title + mark.at.getTime())}
                <div
                    class="absolute top-0.5 h-1.5 w-1.5 rounded-full {mark.at < now
                        ? 'bg-(--text-4)'
                        : 'bg-(--text-2)'}"
                    style="left: calc({pct(mark.at)}% - 3px)"
                    title="{mark.title} · {clock(mark.at)}"
                ></div>
            {/each}

            <!-- now -->
            <div
                class="absolute top-1/2 h-3 w-3 -translate-y-1/2 rounded-full bg-(--accent) ring-2 ring-(--bg)"
                style="left: calc({pct(now)}% - 6px)"
            ></div>
        </div>

        <div class="flex items-baseline justify-between gap-2">
            <span class="type-label flex items-center gap-1 text-(--text-2)">
                <Sunrise class="icon-xs" />
                {clock(sunrise)}
            </span>
            <span class="type-label text-(--text-2)">{daylightLabel}</span>
            <span class="type-label flex items-center gap-1 text-(--text-2)">
                {clock(sunset)}
                <Sunset class="icon-xs" />
            </span>
        </div>
    </div>
{/if}
