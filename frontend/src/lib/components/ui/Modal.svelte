<script lang="ts">
    import type { Snippet } from 'svelte';
    import { X } from '@lucide/svelte';
    import IconButton from './IconButton.svelte';

    interface Props {
        open?: boolean;
        /** Called when the user dismisses it — Escape, the backdrop or the close button. */
        onclose?: () => void;
        title: string;
        subtitle?: string;
        size?: 'sm' | 'md' | 'lg' | 'xl';
        /** Rules the header off from a body that scrolls under it. */
        divided?: boolean;
        /** Buttons beside the close button. */
        actions?: Snippet;
        /** A message pinned under the header, outside the scrolling body (e.g. an upload error). */
        notice?: Snippet;
        children: Snippet;
    }

    let {
        open = $bindable(false),
        onclose,
        title,
        subtitle,
        size = 'md',
        divided = false,
        actions,
        notice,
        children,
    }: Props = $props();

    const id = $props.id();
    const widths = { sm: 'max-w-sm', md: 'max-w-lg', lg: 'max-w-2xl', xl: 'max-w-3xl' };

    let dialog = $state<HTMLDialogElement>();

    $effect(() => {
        if (!dialog) return;
        if (open && !dialog.open) dialog.showModal();
        else if (!open && dialog.open) dialog.close();
    });

    function dismiss() {
        open = false;
        onclose?.();
    }
</script>

<!--
@component
The app's dialog: a native <dialog> centred on wider screens and a bottom sheet on
phones (the `sheet` rules in app.css), with a titled header, a close button and a
body that scrolls. The body is mounted only while open.
-->
<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<dialog
    bind:this={dialog}
    onclose={() => open && dismiss()}
    onclick={(e) => e.target === dialog && dismiss()}
    aria-labelledby={id}
    class="sheet w-full {widths[size]} bg-transparent p-0 backdrop:bg-black/40 backdrop:backdrop-blur-sm"
>
    <!-- One panel <div> as the dialog's only child — the phone sheet styles target it -->
    <div
        class="mx-4 flex max-h-[85vh] flex-col overflow-hidden rounded-2xl border border-(--border) bg-(--bg) shadow-xl"
    >
        {#if open}
            <div
                class="flex shrink-0 items-start justify-between gap-3 px-5 pt-5
                    {divided ? 'border-b border-(--border) pb-4' : 'pb-3'}"
            >
                <div class="min-w-0">
                    <h2 {id} class="type-subtitle font-semibold break-words text-(--text-1)">{title}</h2>
                    {#if subtitle}<p class="type-label text-(--text-2)">{subtitle}</p>{/if}
                </div>
                <div class="flex shrink-0 items-center gap-2">
                    {@render actions?.()}
                    <IconButton label="Close" onclick={dismiss} class="-m-2">
                        <X class="icon-md" />
                    </IconButton>
                </div>
            </div>
            {#if notice}
                <div class="shrink-0 border-b border-(--border) px-5 py-2.5">{@render notice()}</div>
            {/if}
            <div class="min-h-0 flex-1 overflow-y-auto px-5 pb-5 {divided ? 'pt-4' : ''}">
                {@render children()}
            </div>
        {/if}
    </div>
</dialog>
