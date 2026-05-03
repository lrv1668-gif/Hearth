<script lang="ts">
    import { settings, updateCadence, toggleCategory, type PhotoCategory } from '$lib/stores/SettingsStore';
    import Toggle from '$lib/components/Toggle.svelte';

    function toggleAttribution() {
        settings.update((s) => ({ ...s, showAttribution: !s.showAttribution }));
    }

    const cadenceOptions = [
        { label: '2m', value: 120 },
        { label: '5m', value: 300 },
        { label: '10m', value: 600 },
        { label: '30m', value: 1800 },
        { label: '1hr', value: 3600 },
        { label: '2hr', value: 7200 },
    ];

    const categoryOptions: { id: PhotoCategory; label: string }[] = [
        { id: 'nature', label: 'Nature' },
        { id: 'architecture', label: 'Architecture' },
        { id: 'interiors', label: 'Interiors' },
        { id: 'abstract', label: 'Abstract art' },
    ];
</script>

<div class="space-y-6">
    <!-- Cadence -->
    <div class="space-y-3">
        <p class="type-body tracking-widest uppercase text-[var(--text-1)]">Photo cadence</p>
        <div class="flex gap-2">
            {#each cadenceOptions as opt}
                <button
                    onclick={() => updateCadence(opt.value)}
                    class="px-4 py-1.5 rounded-full text-xs tracking-wide border transition-colors
                        {$settings.cadenceSeconds === opt.value
                        ? 'bg-[var(--text-1)] text-[var(--bg)] border-[var(--text-1)] pointer-events-none'
                        : 'border-[var(--border)] hover:bg-[var(--text-4)] text-[var(--text-1)] hover:text-[var(--text-1)] hover:border-[var(--text-2)]'} type-label"
                >
                    {opt.label}
                </button>
            {/each}
        </div>
    </div>

    <!-- Categories -->
    <div class="space-y-3">
        <p class="type-body tracking-widest uppercase text-[var(--text-1)]">Photo categories</p>
        <p class="type-label text-[var(--text-2)]">
            Select one or more categories from below to filter what will be pulled in ambient mode. If none are
            selected, then no filtering is made when pulling a random photo.
        </p>
        <div class="flex flex-col gap-2">
            {#each categoryOptions as cat}
                <label class="flex items-center gap-3 cursor-pointer">
                    <Toggle
                        checked={$settings.photoCategories.includes(cat.id)}
                        onchange={() => toggleCategory(cat.id)}
                    />
                    <span class="type-body text-[var(--text-1)]">{cat.label}</span>
                </label>
            {/each}
        </div>
    </div>

    <!-- Attribution -->
    <div class="space-y-3">
        <p class="type-body tracking-widest uppercase text-[var(--text-1)]">Photographer info</p>
        <label class="flex items-center gap-3 cursor-pointer">
            <Toggle checked={$settings.showAttribution} onchange={toggleAttribution} />
            <span class="type-body text-[var(--text-1)]">Show photographer name in ambient mode</span>
        </label>
    </div>
</div>
