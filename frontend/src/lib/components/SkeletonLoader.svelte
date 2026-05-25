<script lang="ts">
    import type { Snippet } from 'svelte';
    import Skeleton from './Skeleton.svelte';

    interface Props {
        promise: Promise<unknown>;
        children: Snippet;
    }

    let { promise, children }: Props = $props();
</script>

{#await promise}
    <div class="flex flex-col gap-2">
        <Skeleton class="h-4 w-3/4" />
        <Skeleton class="h-4 w-full" />
        <Skeleton class="h-4 w-1/2" />
    </div>
{:then}
    {@render children()}
{:catch}
    {@render children()}
{/await}
