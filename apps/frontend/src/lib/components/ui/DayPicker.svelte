<script lang="ts" generics="T extends string">
    interface Props {
        /** `label` is shown, `name` is read out (e.g. "M" / "Monday"). */
        days: readonly { id: T; label: string; name: string }[];
        selected: readonly T[];
        ontoggle: (day: T) => void;
        /** Names the group for screen readers. */
        label?: string;
    }

    let { days, selected, ontoggle, label = 'Days' }: Props = $props();
</script>

<!--
@component
Days of the week as round toggles — any number can be on.
-->
<div class="flex flex-wrap gap-1" role="group" aria-label={label}>
    {#each days as d (d.id)}
        {@const on = selected.includes(d.id)}
        <button
            type="button"
            onclick={() => ontoggle(d.id)}
            aria-pressed={on}
            aria-label={d.name}
            class="type-label flex h-9 min-w-9 items-center justify-center rounded-full border px-2 transition-colors
                {on
                ? 'border-(--accent) bg-(--accent) font-semibold text-(--accent-fg)'
                : 'border-(--border) text-(--text-2) hover:border-(--text-2) hover:text-(--text-1)'}"
        >
            {d.label}
        </button>
    {/each}
</div>
