<script lang="ts">
    import type { Snippet } from 'svelte';
    import Expandable from '../Expandable.svelte';

    interface Props {
        title: string;
        children: Snippet;
    }

    let { title, children }: Props = $props();
</script>

<Expandable {children}>
    {#snippet header()}
        <h2 class="type-title">{title}</h2>
    {/snippet}
</Expandable>
