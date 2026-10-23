<script lang="ts">
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { categoryOptions } from '$lib/constants/ambient';
    import ToggleRow from '../ui/ToggleRow.svelte';
</script>

<div class="flex flex-col gap-2">
    <p class="type-label text-(--text-2)">Filter photos by category. Leave all off to show nature.</p>
    <div class="mt-1 flex flex-col gap-2">
        {#each categoryOptions.toSorted((a, b) => a.label.localeCompare(b.label)) as cat}
            <ToggleRow
                checked={frameSettings.photoCategories.includes(cat.id)}
                onchange={() => frameSettings.toggleCategory(cat.id)}
                title={cat.label}
            />
        {/each}
    </div>
</div>
