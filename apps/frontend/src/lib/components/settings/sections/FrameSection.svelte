<script lang="ts">
    import { Eye } from '@lucide/svelte';
    import { innerHeight } from 'svelte/reactivity/window';
    import InlineMessage from '#lib/components/ui/InlineMessage.svelte';
    import Modal from '#lib/components/ui/Modal.svelte';
    import SettingsCard from '../SettingsCard.svelte';
    import WidgetLayoutEditor from '../WidgetLayoutEditor.svelte';
    import FramePreview from '../FramePreview.svelte';
    import FrameStatusBar from '../FrameStatusBar.svelte';
    import FrameAppearanceSettings from '../FrameAppearanceSettings.svelte';
    import FrameModeSettings from '../FrameModeSettings.svelte';
    import FrameModeOptions from '../FrameModeOptions.svelte';
    import FrameTemplateEditor from '../FrameTemplateEditor.svelte';
    import { frameSettings } from '#lib/stores/FrameSettingsStore.svelte.ts';
    import {
        fixedFrameFooter,
        fixedFrameHeader,
        frameWidgets,
        normalizeFrameLayout,
        type FrameWidgetId,
    } from '#lib/constants/frameWidgets.ts';
    import { MAX_COLUMN_WIDGETS } from '#lib/constants/frameLimits.ts';
    import { settingsHref } from '#lib/constants/settingsSections.ts';

    // `mainHeight` is the scroll area's height, which the pinned preview must fit;
    // `floatingPreview` tells the page the floating Preview button is showing, so it
    // leaves room for it under the last card.
    let { mainHeight, floatingPreview = $bindable(false) }: { mainHeight: number; floatingPreview?: boolean } =
        $props();

    const labelFor = (id: string) => frameWidgets.find((w) => w.id === id)?.label ?? id;
    const frameLayout = $derived(normalizeFrameLayout(frameSettings.layout));

    // The preview sits beside the options once there's room for both (the @4xl container
    // query — 56rem), and in a sheet behind a floating button below that. Width 0 means
    // not measured yet: show neither, so a phone never mounts the inline one.
    let columnWidth = $state(0);
    const rem = () => parseFloat(getComputedStyle(document.documentElement).fontSize);
    const measured = $derived(columnWidth > 0);
    const previewInline = $derived(measured && columnWidth >= 56 * rem());
    $effect(() => {
        floatingPreview = measured && !previewInline;
    });
    let previewOpen = $state(false);
    // The inline preview is pinned in view, so it must fit the scroll area's height: what's
    // left after its card's title, description and padding, and a gap above and below.
    // Those are read from where the preview starts, never from the preview's own size, so
    // its height can't feed back into the fit.
    let previewWrap = $state<HTMLElement>();
    const previewMaxHeight = $derived.by(() => {
        void columnWidth; // the card's description rewraps as the column changes
        const card = previewWrap?.parentElement;
        if (!previewWrap || !card || !mainHeight) return 0;
        const style = getComputedStyle(card);
        const chrome =
            previewWrap.offsetTop -
            card.offsetTop +
            parseFloat(style.paddingBottom) +
            parseFloat(style.borderBottomWidth);
        return mainHeight - chrome - 3 * rem();
    });
    // The full-size preview fills its dialog: 85vh, less the dialog's header and padding.
    const fullPreviewMaxHeight = $derived((innerHeight.current ?? 0) * 0.85 - 5.5 * rem());
</script>

<!--
    Wide (container ≥ 56rem): options on the left, the preview pinned beside them so it
    stays in view while scrolling. Narrow: one column, and the preview opens in a sheet
    from a floating button instead, so it's one tap away from any card rather than below
    all of them. The left column becomes `display: contents` on narrow screens so its
    cards keep their order.
-->
<FrameStatusBar />
<div class="@container" bind:clientWidth={columnWidth}>
    <div class="flex flex-col gap-4 @4xl:grid @4xl:grid-cols-[minmax(0,1fr)_minmax(320px,42%)] @4xl:items-start">
        <div class="contents @4xl:flex @4xl:flex-col @4xl:gap-4">
            <div class="order-1">
                <SettingsCard
                    title="Mode"
                    description="What the frame shows now, and a schedule for the rest of the day."
                >
                    <FrameModeSettings />
                    <p class="type-caption mt-4 text-(--text-2)">
                        Ambient and Photo mode — their photos, how often they change and where Ambient’s card sits — are
                        set in <a href={settingsHref('photos')} class="underline">Photos</a>.
                    </p>
                </SettingsCard>
            </div>
            <div class="order-1">
                <SettingsCard
                    title="Look"
                    description="Colours, typeface, orientation, and panel size for the wall frame. Themes use only colours the panel can print cleanly."
                >
                    {#if frameSettings.error}
                        <InlineMessage tone="error" class="mb-4">
                            Can't reach the Display service — changes here won't be saved or reach the frame.
                        </InlineMessage>
                    {/if}
                    <FrameAppearanceSettings />
                </SettingsCard>
            </div>
            <div class="order-3">
                <SettingsCard
                    title="Daily layout"
                    description="What Daily mode shows: pick a layout template and a widget for each slot, or Custom for free columns."
                >
                    <FrameTemplateEditor />
                    {#if frameSettings.template === 'columns'}
                        <div class="mt-4">
                            <WidgetLayoutEditor
                                dragType="frame-widget"
                                columns={frameLayout}
                                {labelFor}
                                onchange={(c) => (frameSettings.layout = normalizeFrameLayout(c))}
                                width={frameSettings.split}
                                onwidthchange={(w) => (frameSettings.split = w)}
                                header={fixedFrameHeader}
                                footer={fixedFrameFooter}
                                maxPerColumn={MAX_COLUMN_WIDGETS}
                                widgets={frameWidgets}
                                ontoggle={(id) => frameSettings.toggleWidget(id as FrameWidgetId)}
                            />
                            <p class="type-caption mt-2 text-(--text-2)">
                                Lists shrink to fit what shares their column. Portrait keeps the two columns side by
                                side, so they are narrower.
                            </p>
                        </div>
                    {/if}
                </SettingsCard>
            </div>
            <div class="order-3">
                <SettingsCard title="Countdown, Note & Guest" description="What Countdown, Note and Guest modes show.">
                    <FrameModeOptions />
                </SettingsCard>
            </div>
        </div>
        {#if previewInline}
            <div class="order-2 @4xl:sticky @4xl:top-0">
                <SettingsCard
                    title="Preview"
                    description="The real frame page, scaled down. Updates as you change settings."
                    listed={false}
                >
                    <div bind:this={previewWrap}>
                        <FramePreview maxHeight={previewMaxHeight} onexpand={() => (previewOpen = true)} />
                    </div>
                </SettingsCard>
            </div>
        {/if}
    </div>
</div>
{#if floatingPreview}
    <button
        onclick={() => (previewOpen = true)}
        class="type-label fixed right-4 bottom-[calc(5rem+env(safe-area-inset-bottom))] z-20 flex min-h-11 items-center gap-2 rounded-full bg-(--accent) px-4 font-semibold text-(--accent-fg) shadow-lg transition-colors hover:bg-(--accent-hi) lg:bottom-6"
    >
        <Eye class="icon-sm" aria-hidden="true" />
        Preview
    </button>
{/if}
<!-- The full-size preview: the card's expand button on wide screens, the floating button on narrow ones -->
<Modal bind:open={previewOpen} title="Preview" size={frameSettings.orientation === 'portrait' ? 'xl' : 'full'}>
    <FramePreview maxHeight={fullPreviewMaxHeight} />
</Modal>
