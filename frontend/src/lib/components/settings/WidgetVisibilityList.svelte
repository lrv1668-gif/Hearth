<script lang="ts">
    import ToggleRow from '../ui/ToggleRow.svelte';

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
            <ToggleRow
                checked={on}
                onchange={() => ontoggle(widget.id)}
                disabled={blocked}
                title={widget.label}
                description={blocked ? 'Both columns are full — remove a widget first' : widget.description}
            />
        {/each}
    </div>

    <!-- Fixed widgets -->
    <div class="border-t border-(--border) pt-4">
        <p class="eyebrow mb-3">Always shown</p>
        <div class="flex flex-col gap-2">
            {#each fixed as w (w.label)}
                <ToggleRow checked={true} onchange={() => {}} disabled title={w.label} description={w.caption} />
            {/each}
        </div>
    </div>
</div>
