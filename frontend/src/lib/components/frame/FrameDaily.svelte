<script lang="ts">
    import { frameWidgets, type FrameLayout, type FrameWidgetId } from '$lib/constants/frameWidgets';
    import { allocateLines, COLUMN_LINES } from '$lib/frame';
    import FrameEditionLine from './FrameEditionLine.svelte';
    import FrameDateBlock from './FrameDateBlock.svelte';
    import FrameSunPath from './FrameSunPath.svelte';
    import FrameForecast from './FrameForecast.svelte';
    import DayArcWidget from '$lib/components/widgets/DayArcWidget.svelte';
    import FrameToday from './widgets/FrameToday.svelte';
    import FrameComingUp from './widgets/FrameComingUp.svelte';
    import FrameWeek from './widgets/FrameWeek.svelte';
    import FrameCountdowns from './widgets/FrameCountdowns.svelte';
    import AlmanacView from '$lib/components/widgets/views/AlmanacView.svelte';
    import MoonPhaseView from '$lib/components/widgets/views/MoonPhaseView.svelte';
    import DailyQuoteView from '$lib/components/widgets/views/DailyQuoteView.svelte';
    import BirdSightingsView from '$lib/components/widgets/views/BirdSightingsView.svelte';
    import { almanacStore } from '$lib/stores/AlmanacStore.svelte.ts';
    import { dailyQuoteStore } from '$lib/stores/DailyQuoteStore.svelte.ts';
    import { birdsStore } from '$lib/stores/BirdsStore.svelte.ts';

    interface Props {
        now: Date;
        orientation: 'landscape' | 'portrait';
        layout: FrameLayout;
        /** Left column's width, in percent (landscape only). */
        split: number;
    }

    let { now, orientation, layout, split }: Props = $props();

    // The layout's left and right columns side by side at `split`% (one full-width
    // column if the other is empty), in both orientations. Portrait columns are
    // ~500px at an even split, so the rail and stamps switch to narrow forms.
    const columns = $derived([layout.left, layout.right].filter((c) => c.length > 0));
    const gridColumns = $derived(columns.length === 2 ? `${split}fr ${100 - split}fr` : '1fr');
    // Each widget's share of its column, in lines of body text — lists size themselves
    // to it. Measured from the real layout (a hidden body-text probe gives the line
    // height under the current font preset); the constant is only the first-paint fallback.
    let middleHeight = $state(0);
    let lineHeight = $state(0);
    const totalLines = $derived(middleHeight && lineHeight ? middleHeight / lineHeight : COLUMN_LINES[orientation]);
    const lines = $derived(new Map(columns.flatMap((c) => [...allocateLines(c, totalLines)])));

    const weekday = $derived(now.toLocaleDateString('en-US', { weekday: 'long' }));

    const label = (id: FrameWidgetId) => frameWidgets.find((w) => w.id === id)!.label;
</script>

{#snippet widget(id: FrameWidgetId)}
    {@const n = lines.get(id) ?? 0}
    <!-- Safety net: if a widget still outgrows its share, it clips inside its own
         box rather than pushing the next widget off the panel. -->
    <section class="min-h-0 min-w-0 shrink overflow-hidden">
        <!-- Kicker: tracked caps led by a small --block square, no underline — space
             separates the widgets, so the page keeps only its structural rules. -->
        <h2
            class="type-label mb-6 flex items-center gap-3 leading-none font-semibold tracking-[0.16em] text-(--text-2) uppercase"
        >
            <span class="h-3.5 w-3.5 shrink-0 bg-(--block)"></span>
            {label(id)}
        </h2>
        {#if id === 'arc'}
            <DayArcWidget hourly />
        {:else if id === 'today'}
            <FrameToday {now} lines={n} />
        {:else if id === 'coming-up'}
            <FrameComingUp {now} lines={n} />
        {:else if id === 'week'}
            <FrameWeek {now} lines={n} />
        {:else if id === 'countdowns'}
            <FrameCountdowns {now} />
        {:else if id === 'almanac'}
            <AlmanacView almanac={almanacStore.almanac} lines={n} />
        {:else if id === 'moon'}
            <MoonPhaseView {now} />
        {:else if id === 'quote'}
            <DailyQuoteView quote={dailyQuoteStore.quote} lines={n} />
        {:else if id === 'birds'}
            <BirdSightingsView sightings={birdsStore.sightings} {now} lines={n} />
        {/if}
    </section>
{/snippet}

{#snippet masthead()}
    <FrameEditionLine {now} wrap={orientation === 'portrait'} />
    <!-- Line height 1 plus a little bottom padding keeps the italic descenders (the y
         in "day") inside the box at every font preset. -->
    <p
        class="type-weekday mt-3 pb-[0.08em] font-(family-name:--display-family) leading-none font-medium tracking-[-0.02em] whitespace-nowrap italic"
    >
        {weekday}
    </p>
{/snippet}

<!--
@component
Daily Mode for the e-paper frame, laid out as today's edition of an almanac: a compact
masthead (date block; edition line and weekday; sun path with the temperature),
the configurable widget columns from `layout` (see constants/frameWidgets.ts), and
the forecast band. Zones are separated by 3px rules and space rather than tinted
panels, which would dither into speckle; large shapes use the pure --block ink.
-->
<div class="grid h-full grid-cols-[minmax(0,1fr)] grid-rows-[auto_minmax(0,1fr)_auto] gap-7 px-16 pt-9">
    {#if orientation === 'portrait'}
        <!-- Portrait: the same single row, with a smaller weekday and the compact sun path. -->
        <header class="flex items-end gap-6 border-b-3 border-(--border) pb-5">
            <FrameDateBlock {now} />
            <div class="min-w-0 flex-1">{@render masthead()}</div>
            <FrameSunPath {now} compact />
        </header>
    {:else}
        <header class="flex items-end gap-10 border-b-3 border-(--border) pb-5">
            <FrameDateBlock {now} />
            <div class="min-w-0 flex-1 pb-1">{@render masthead()}</div>
            <FrameSunPath {now} />
        </header>
    {/if}

    <!-- The configurable widget slots -->
    <div
        bind:clientHeight={middleHeight}
        class="relative grid min-h-0 overflow-hidden {orientation === 'portrait' ? 'gap-12' : 'gap-16'}"
        style="grid-template-columns: {gridColumns}"
    >
        <p bind:clientHeight={lineHeight} class="type-body invisible absolute" aria-hidden="true">X</p>
        {#each columns as column, c (c)}
            <div class="flex min-w-0 flex-col gap-10">
                {#each column as id (id)}
                    {@render widget(id)}
                {/each}
            </div>
        {/each}
    </div>

    <FrameForecast {now} />
</div>
