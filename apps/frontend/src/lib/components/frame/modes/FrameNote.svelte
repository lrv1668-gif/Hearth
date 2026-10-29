<script lang="ts">
    import { fitFontSize, getFrameScale, MAX_FRAME_TEXT_SIZE } from '$lib/frame';

    let { title = '', body = '' }: { title?: string; body?: string } = $props();

    const heading = $derived(title.trim());
    // Keep the line breaks as typed, but drop blank lines at either end
    const text = $derived(body.replace(/^\s*\n|\n\s*$/g, '').trimEnd());
    const lines = $derived(text ? text.split('\n') : []);

    // Body text size bounds at the 1600×1200 reference: never below the caption floor,
    // and a short note can grow to poster size.
    const MIN_PX = 22;
    const MAX_PX = 320;

    const scale = getFrameScale();
    let box = $state<HTMLElement>();
    let content = $state<HTMLElement>();
    let size = $state(MAX_PX);

    // Keep each typed line on one line unless that costs more than this share of the size
    // wrapping would allow (a long paragraph would otherwise shrink to the floor).
    const NO_WRAP_SHARE = 0.6;

    function fit() {
        if (!box || !content) return;
        const el = content;
        const area = box;
        const largest = (whiteSpace: string, max = MAX_PX * scale()) =>
            fitFontSize(
                (px) => {
                    el.style.fontSize = `${px}px`;
                    el.style.whiteSpace = whiteSpace;
                    return el.scrollHeight <= area.clientHeight && el.scrollWidth <= area.clientWidth;
                },
                MIN_PX * scale(),
                max
            );
        const wrapped = largest('normal');
        const unwrapped = largest('nowrap');
        const keepLines = unwrapped >= wrapped * NO_WRAP_SHARE;
        // The note already fills the panel, so the frame's text size (--frame-text) moves
        // a kept-lines note from its unwrapped size towards the wrapped one in proportion:
        // larger trades unbroken lines for bigger, wrapped text, a step at a time.
        const textSize = Number(getComputedStyle(area).getPropertyValue('--frame-text')) || 1;
        const share = Math.min(1, Math.max(0, (textSize - 1) / (MAX_FRAME_TEXT_SIZE / 100 - 1)));
        const target = Math.round(unwrapped + (wrapped - unwrapped) * share);
        const grow = keepLines && target > unwrapped;
        size = !keepLines ? wrapped : grow ? largest('normal', target) : unwrapped;
        el.style.whiteSpace = keepLines && !grow ? 'nowrap' : 'normal';
        el.style.fontSize = `${size}px`;
    }

    // Refit when the words change, once web fonts have loaded, and when the Settings
    // preview swaps the font preset or text size in place (the root's data-font, style).
    $effect(() => {
        void heading;
        void text;
        fit();
        let live = true;
        document.fonts.ready.then(() => live && fit());
        const root = box?.closest<HTMLElement>('.frame');
        const observer = new MutationObserver(fit);
        if (root) observer.observe(root, { attributes: true, attributeFilter: ['data-font', 'style'] });
        return () => {
            live = false;
            observer.disconnect();
        };
    });
</script>

<!--
@component
Note mode for the e-paper frame: your own words — house rules for a party, a welcome,
a reminder. A heading in the display face over a --block rule, then the body with its
line breaks kept (blank lines space out sections). The text is sized to the panel: the
largest size at which everything fits — preferring one where no typed line wraps — is
found by measuring (fitFontSize), so a short
note fills the frame and a long one shrinks — down to the caption floor. That's why
sizes here are set inline in px rather than with .type-* classes. Flat inks only, so it
takes Daily's ordered dither.
-->
<div bind:this={box} class="absolute inset-x-24 inset-y-20 flex items-center">
    {#if heading || text}
        <div bind:this={content} class="note w-full" style:font-size="{size}px">
            {#if heading}
                <p class="font-(family-name:--display-family) text-[1.6em] leading-[1.05] font-semibold">
                    {heading}
                </p>
                {#if text}<div class="mt-[0.45em] mb-[0.6em] h-[0.18em] w-[2.4em] bg-(--block)"></div>{/if}
            {/if}
            <!-- One paragraph per typed line, a little apart so a wrapped line still reads
                 as one item; a blank line becomes a wider gap. -->
            {#each lines as line, i (i)}
                {#if line.trim()}
                    <p class="mb-[0.3em] leading-[1.2]">{line}</p>
                {:else}
                    <div class="h-[0.6em]" aria-hidden="true"></div>
                {/if}
            {/each}
        </div>
    {:else}
        <div class="w-full text-center">
            <p class="type-title font-(family-name:--display-family) text-(--text-2) italic">Nothing to show yet</p>
            <p class="type-body mt-4 text-(--text-2)">Write your note in Settings → Frame.</p>
        </div>
    {/if}
</div>

<style>
    .note {
        overflow-wrap: anywhere;
        /* Prose, not figures: the frame's tabular numerals also widen hyphens ("Wi-Fi") */
        font-variant-numeric: normal;
    }
</style>
