<script lang="ts">
    interface Props {
        provider: string;
    }
    let { provider }: Props = $props();
</script>

{#if provider === 'google'}
    <span
        class="inline-flex h-3 w-3 items-center justify-center rounded-full bg-[var(--bg)] ring-1 ring-[var(--border)]"
        style="font-size: 6px; font-weight: 800; color: var(--text-2); line-height: 1;"
        aria-hidden="true">G</span
    >
{/if}
