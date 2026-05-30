<script lang="ts">
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { categoryOptions } from '$lib/constants/ambient';
    import Toggle from '$lib/components/Toggle.svelte';
</script>

<div class="flex flex-col gap-6">
    <div class="flex flex-col gap-2">
        <p class="type-body font-medium text-[var(--text-1)]">Categories</p>
        <p class="type-label text-[var(--text-2)]">Filter photos by category. Leave all off to show everything.</p>
        <div class="mt-1 flex flex-col gap-2">
            {#each categoryOptions as cat}
                <label class="flex cursor-pointer items-center gap-3">
                    <Toggle
                        checked={settings.photoCategories.includes(cat.id)}
                        onchange={() => settings.toggleCategory(cat.id)}
                    />
                    <span class="type-body text-[var(--text-1)]">{cat.label}</span>
                </label>
            {/each}
        </div>
    </div>

    <div class="flex flex-col gap-2 border-t border-[var(--border)] pt-4">
        <p class="type-body font-medium text-[var(--text-1)]">Attribution</p>
        <label class="flex cursor-pointer items-center gap-3">
            <Toggle
                checked={settings.showAttribution}
                onchange={() => (settings.showAttribution = !settings.showAttribution)}
            />
            <span class="type-body text-[var(--text-1)]">Show photographer name in ambient mode</span>
        </label>
    </div>
</div>
