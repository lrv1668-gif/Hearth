<script lang="ts">
    import { frameWidgets, type FrameLayout, type FrameWidgetId } from '#lib/constants/frameWidgets.ts';
    import { frameTemplate, type FrameSize, type FrameTemplateId } from '#lib/constants/frameTemplates.ts';
    import { allocateLines, COLUMN_LINES, slotLines } from '#lib/frame.ts';
    import FrameEditionLine from './FrameEditionLine.svelte';
    import FrameDateBlock from './FrameDateBlock.svelte';
    import FrameSunPath from './FrameSunPath.svelte';
    import FrameForecast from './FrameForecast.svelte';
    import DayArcWidget from '#lib/components/widgets/DayArcWidget.svelte';
    import FrameToday from './widgets/FrameToday.svelte';
    import FrameComingUp from './widgets/FrameComingUp.svelte';
    import FrameAgenda from './widgets/FrameAgenda.svelte';
    import FrameMoon from './widgets/FrameMoon.svelte';
    import FrameQuote from './widgets/FrameQuote.svelte';
    import FrameWeek from './widgets/FrameWeek.svelte';
    import FrameCountdowns from './widgets/FrameCountdowns.svelte';
    import FrameLastPlayed from './widgets/FrameLastPlayed.svelte';
    import FrameSports from './widgets/FrameSports.svelte';
    import AlmanacView from '#lib/components/widgets/views/AlmanacView.svelte';
    import MoonPhaseView from '#lib/components/widgets/views/MoonPhaseView.svelte';
    import DailyQuoteView from '#lib/components/widgets/views/DailyQuoteView.svelte';
    import BirdSightingsView from '#lib/components/widgets/views/BirdSightingsView.svelte';
    import { almanacStore } from '#lib/stores/AlmanacStore.svelte.ts';
    import { dailyQuoteStore } from '#lib/stores/DailyQuoteStore.svelte.ts';
    import { birdsStore } from '#lib/stores/BirdsStore.svelte.ts';

    interface Props {
        now: Date;
        orientation: 'landscape' | 'portrait';
        layout: FrameLayout;
        /** Left column's width, in percent (landscape only). */
        split: number;
        /** The layout template; 'columns' lays out `layout` at `split`. */
        template: FrameTemplateId;
        /** The template's widget per slot, '' for an empty slot. */
        slots: readonly (FrameWidgetId | '')[];
    }

    let { now, orientation, layout, split, template, slots }: Props = $props();

    // A template's slots on a grid of equal tracks, so each slot's size comes from the
    // template, never its content — each slot measures itself for its widget's lines.
    const areas = $derived(frameTemplate(template).areas[orientation]);
    const slotSizes = $derived(frameTemplate(template).slots);
    const templateGrid = $derived(
        `grid-template-areas: ${areas.map((row) => `'${row}'`).join(' ')};` +
            ` grid-template-columns: repeat(${areas[0]?.split(' ').length ?? 1}, minmax(0, 1fr));` +
            ` grid-template-rows: repeat(${areas.length || 1}, minmax(0, 1fr))`
    );
    let slotHeights = $state<number[]>([]);
    const slotBudget = (i: number) => (slotHeights[i] && lineHeight ? slotLines(slotHeights[i] / lineHeight) : 0);

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

{#snippet widget(id: FrameWidgetId, n: number, size?: FrameSize)}
    {@const roomy = size === 'm' || size === 'l'}
    <!-- Safety net: if a widget still outgrows its share, it clips inside its own
         box rather than pushing the next widget off the panel. In a template slot the
         section fills the slot, and the body under the kicker is a size container:
         widgets size to it with container units and @container rules. -->
    <section class="min-h-0 min-w-0 overflow-hidden {size ? 'flex h-full flex-col' : 'shrink'}">
        <!-- Kicker: tracked caps led by a small --block square, no underline — space
             separates the widgets, so the page keeps only its structural rules. -->
        <h2
            class="type-label mb-6 flex shrink-0 items-center gap-3 leading-none font-semibold tracking-[0.16em] text-(--text-2) uppercase"
        >
            <span class="h-3.5 w-3.5 shrink-0 bg-(--block)"></span>
            {label(id)}
        </h2>
        <div class={size ? '[container-type:size] relative min-h-0 flex-1' : 'contents'}>
            {#if id === 'arc'}
                <DayArcWidget hourly />
            {:else if id === 'today'}
                <FrameToday {now} lines={n} {size} />
            {:else if id === 'coming-up'}
                {#if roomy}
                    <FrameAgenda {now} lines={n} large={size === 'l'} />
                {:else}
                    <FrameComingUp {now} lines={n} />
                {/if}
            {:else if id === 'week'}
                <FrameWeek {now} lines={n} />
            {:else if id === 'countdowns'}
                <FrameCountdowns {now} {size} />
            {:else if id === 'almanac'}
                <AlmanacView almanac={almanacStore.almanac} lines={n} />
            {:else if id === 'moon'}
                {#if roomy}
                    <FrameMoon {now} large={size === 'l'} />
                {:else}
                    <MoonPhaseView {now} />
                {/if}
            {:else if id === 'quote'}
                {#if roomy}
                    <FrameQuote large={size === 'l'} />
                {:else}
                    <DailyQuoteView quote={dailyQuoteStore.quote} lines={n} />
                {/if}
            {:else if id === 'birds'}
                <BirdSightingsView sightings={birdsStore.sightings} {now} lines={n} />
            {:else if id === 'last-played'}
                <FrameLastPlayed {now} {size} />
            {:else if id === 'sports'}
                <FrameSports {now} lines={n} />
            {/if}
        </div>
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
the configurable widgets — free columns from `layout` (constants/frameWidgets.ts) or a
template's sized slots (constants/frameTemplates.ts) — and
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

    <!-- The configurable widget slots: free columns, or a template's sized slots -->
    {#if template === 'columns'}
        <div
            bind:clientHeight={middleHeight}
            class="relative grid min-h-0 overflow-hidden {orientation === 'portrait' ? 'gap-12' : 'gap-16'}"
            style="grid-template-columns: {gridColumns}"
        >
            <p bind:clientHeight={lineHeight} class="type-body invisible absolute" aria-hidden="true">X</p>
            {#each columns as column, c (c)}
                <div class="flex min-w-0 flex-col gap-10">
                    {#each column as id (id)}
                        {@render widget(id, lines.get(id) ?? 0)}
                    {/each}
                </div>
            {/each}
        </div>
    {:else}
        <div
            class="relative grid min-h-0 gap-y-10 overflow-hidden {orientation === 'portrait'
                ? 'gap-x-12'
                : 'gap-x-16'}"
            style={templateGrid}
        >
            <p bind:clientHeight={lineHeight} class="type-body invisible absolute" aria-hidden="true">X</p>
            {#each slotSizes as size, i (i)}
                <div
                    bind:clientHeight={slotHeights[i]}
                    class="flex min-h-0 min-w-0 flex-col overflow-hidden"
                    style="grid-area: s{i}"
                    data-slot={i}
                    data-size={size}
                >
                    {#if slots[i]}
                        {@render widget(slots[i], slotBudget(i), size)}
                    {/if}
                </div>
            {/each}
        </div>
    {/if}

    <FrameForecast {now} compact={orientation === 'portrait'} />
</div>
