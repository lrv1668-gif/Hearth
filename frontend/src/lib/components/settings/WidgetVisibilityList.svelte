<script lang="ts">
    import Toggle from '$lib/components/Toggle.svelte';

    interface Props {
        widgets: readonly { id: string; label: string; description: string }[];
        isOn: (id: string) => boolean;
        ontoggle: (id: string) => void;
        /** When set and true for an off widget, its toggle is disabled (e.g. every column is full). */
        isBlocked?: (id: string) => boolean;
        /** Always-shown items, listed below as locked-on toggles. */
        fixed: readonly { label: string; caption: string }[];
    }

    let { widgets, isOn, ontoggle, isBlocked, fixed }: Props = $props();
</script>

<!--
@component
Widget on/off toggles plus a locked "Always shown" list — shared by the Dashboard and
Frame settings so both screens behave the same.
-->
<div class="flex flex-col gap-5">
    <!-- Configurable widgets -->
    <div class="flex flex-col gap-2">
        {#each widgets.toSorted((a, b) => a.label.localeCompare(b.label)) as widget (widget.id)}
            {@const on = isOn(widget.id)}
            {@const blocked = !on && (isBlocked?.(widget.id) ?? false)}
            <label class="flex items-center gap-3 {blocked ? 'cursor-not-allowed opacity-50' : 'cursor-pointer'}">
                <div class="mt-0.5 shrink-0">
                    <Toggle checked={on} onchange={() => !blocked && ontoggle(widget.id)} />
                </div>
                <div>
                    <p class="type-body text-(--text-1) select-none">{widget.label}</p>
                    <p class="type-label text-(--text-2) select-none">
                        {blocked ? 'Both columns are full — remove a widget first' : widget.description}
                    </p>
                </div>
            </label>
        {/each}
    </div>

    <!-- Fixed widgets -->
    <div class="border-t border-(--border) pt-4">
        <p class="type-label mb-3 font-semibold tracking-[0.12em] text-(--text-2) uppercase">Always shown</p>
        <div class="flex flex-col gap-2">
            {#each fixed as w (w.label)}
                <div class="flex items-center gap-3 opacity-50">
                    <div class="mt-0.5 shrink-0">
                        <Toggle checked={true} onchange={() => {}} />
                    </div>
                    <div>
                        <p class="type-body text-(--text-1) select-none">{w.label}</p>
                        <p class="type-label text-(--text-2) select-none">{w.caption}</p>
                    </div>
                </div>
            {/each}
        </div>
    </div>
</div>
