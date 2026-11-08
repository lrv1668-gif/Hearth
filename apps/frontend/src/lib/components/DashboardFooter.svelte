<script lang="ts">
    import { Monitor } from '@lucide/svelte';
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';

    function formatRefreshed(iso: string): string {
        return new Date(iso).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    }

    let { class: extraClass = '' }: { class?: string } = $props();
</script>

<footer class="hidden shrink-0 border-t-2 border-(--border) px-8 py-2 lg:block {extraClass}">
    <div class="flex items-center justify-center gap-2">
        {#if weatherStore.current?.fetched_at}
            <div class="type-label flex flex-shrink-0 items-center gap-2 rounded-lg bg-(--surface) p-2">
                <Monitor class="icon-sm text-(--text-2)" />
                <p>Refreshed @ {formatRefreshed(weatherStore.current.fetched_at)}</p>
            </div>
        {/if}
    </div>
</footer>
