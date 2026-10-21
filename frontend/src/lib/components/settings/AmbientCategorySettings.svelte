<script lang="ts">
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { categoryOptions } from '$lib/constants/ambient';
    import Toggle from '$lib/components/Toggle.svelte';
</script>

<div class="flex flex-col gap-2">
    <p class="type-label text-(--text-2)">Filter photos by category. Leave all off to show nature.</p>
    <div class="mt-1 flex flex-col gap-2">
        {#each categoryOptions.toSorted((a, b) => a.label.localeCompare(b.label)) as cat}
            <label class="flex cursor-pointer items-center gap-3">
                <Toggle
                    checked={frameSettings.photoCategories.includes(cat.id)}
                    onchange={() => frameSettings.toggleCategory(cat.id)}
                />
                <span class="type-body text-(--text-1)">{cat.label}</span>
            </label>
        {/each}
    </div>
</div>
