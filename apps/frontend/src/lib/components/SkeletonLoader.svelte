<script lang="ts">
    import type { Snippet } from 'svelte';
    import Skeleton from './Skeleton.svelte';

    interface Props {
        promise: Promise<unknown>;
        children: Snippet;
        /** A placeholder shaped like the content; three text bars when omitted. */
        skeleton?: Snippet;
    }

    let { promise, children, skeleton }: Props = $props();
</script>

{#await promise}
    <div aria-busy="true" aria-label="Loading">
        {#if skeleton}
            {@render skeleton()}
        {:else}
            <div class="flex flex-col gap-2">
                <Skeleton class="h-4 w-3/4" />
                <Skeleton class="h-4 w-full" />
                <Skeleton class="h-4 w-1/2" />
            </div>
        {/if}
    </div>
{:then}
    {@render children()}
{:catch}
    {@render children()}
{/await}
