<script lang="ts" generics="T extends string | number | null">
    import Chip from './Chip.svelte';

    interface Props {
        options: readonly { value: T; label: string; description?: string }[];
        value: T;
        onchange: (value: T) => void;
        /** Names the group for screen readers. */
        label: string;
    }

    let { options, value, onchange, label }: Props = $props();
</script>

<!--
@component
A wrapping row of chips where exactly one is chosen. For two to four short options
that read as one control, SegmentedControl is tighter.
-->
<div class="flex flex-wrap gap-2" role="group" aria-label={label}>
    {#each options as o (o.value)}
        <Chip pressed={value === o.value} title={o.description} onclick={() => onchange(o.value)}>{o.label}</Chip>
    {/each}
</div>
