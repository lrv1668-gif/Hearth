<script lang="ts">
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { categoryOptions } from '$lib/constants/ambient';
    import Chip from '../ui/Chip.svelte';
</script>

<p class="type-label mb-3 text-(--text-2)">Pick any to filter by. Leave all off to show nature.</p>
<div class="flex flex-wrap gap-2" role="group" aria-label="Photo categories">
    {#each categoryOptions.toSorted((a, b) => a.label.localeCompare(b.label)) as cat (cat.id)}
        <Chip
            pressed={frameSettings.photoCategories.includes(cat.id)}
            onclick={() => frameSettings.toggleCategory(cat.id)}
        >
            {cat.label}
        </Chip>
    {/each}
</div>
