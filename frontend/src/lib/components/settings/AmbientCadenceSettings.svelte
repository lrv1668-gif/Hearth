<script lang="ts">
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { cadenceOptions } from '$lib/constants/ambient';
</script>

<div class="flex flex-wrap gap-2">
    {#each cadenceOptions as opt}
        <button
            onclick={() => (settings.cadenceSeconds = opt.value)}
            class="type-label rounded-full border px-4 py-1.5 tracking-wide transition-colors
                {settings.cadenceSeconds === opt.value
                ? 'pointer-events-none border-[var(--text-1)] bg-[var(--text-1)] text-[var(--bg)]'
                : 'border-[var(--border)] text-[var(--text-1)] hover:border-[var(--text-2)] hover:bg-[var(--surface-hi)]'}"
        >
            {opt.label}
        </button>
    {/each}
</div>
