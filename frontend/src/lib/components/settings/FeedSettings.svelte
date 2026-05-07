<script lang="ts">
    import { settings, updateRssArticleCount } from '$lib/stores/SettingsStore.svelte.ts';

    const countOptions = [5, 10, 15, 20];
</script>

<div class="space-y-3">
    <div class="flex gap-2">
        {#each countOptions as n}
            <button
                onclick={() => updateRssArticleCount(n)}
                class="rounded-full border px-4 py-1.5 text-xs tracking-wide transition-colors
                    {settings.rssArticleCount === n
                    ? 'pointer-events-none border-[var(--text-1)] bg-[var(--text-1)] text-[var(--bg)]'
                    : 'border-[var(--border)] text-[var(--text-1)] hover:border-[var(--text-2)] hover:bg-[var(--text-4)] hover:text-[var(--text-1)]'} type-label"
            >
                {n}
            </button>
        {/each}
    </div>
</div>
