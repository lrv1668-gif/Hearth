<script lang="ts">
    interface Props {
        provider: string;
    }
    let { provider }: Props = $props();
</script>

{#if provider === 'google'}
    <span
        class="inline-flex h-3 w-3 items-center justify-center rounded-full bg-(--bg) font-bold ring-1 ring-(--border)"
        style="font-size: 6px; color: var(--text-2); line-height: 1;"
        aria-hidden="true">G</span
    >
{/if}
