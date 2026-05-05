<script lang="ts">
    import { settings, updateCadence, toggleCategory, toggleAttribution } from '$lib/stores/SettingsStore.svelte.ts';
    import Toggle from '$lib/components/Toggle.svelte';
    import SubTitle from '../SubTitle.svelte';
    import { cadenceOptions, categoryOptions } from '$lib/constants/ambient';
</script>

<div class="space-y-6">
    <!-- Cadence -->
    <div class="space-y-3">
        <SubTitle subTitleText="Photo cadence" subTitleDescription="Choose how often photos cycle." />
        <div class="flex gap-2">
            {#each cadenceOptions as opt}
                <button
                    onclick={() => updateCadence(opt.value)}
                    class="px-4 py-1.5 rounded-full text-xs tracking-wide border transition-colors
                        {settings.cadenceSeconds === opt.value
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
        <SubTitle
            subTitleText="Photo categories"
            subTitleDescription="Select one or more categories from below to filter what category of photos will be pulled . If none are
        selected, then no filtering is made when pulling a random photo."
        />

        <div class="flex flex-col gap-2">
            {#each categoryOptions as cat}
                <label class="flex items-center gap-3 cursor-pointer">
                    <Toggle
                        checked={settings.photoCategories.includes(cat.id)}
                        onchange={() => toggleCategory(cat.id)}
                    />
                    <span class="type-body text-[var(--text-1)]">{cat.label}</span>
                </label>
            {/each}
        </div>
    </div>

    <!-- Attribution -->
    <div class="space-y-3">
        <SubTitle
            subTitleText="Photographer info"
            subTitleDescription="Control whether or not photographer info shows up on the very bottom of the Ambient page."
        />
        <label class="flex items-center gap-3 cursor-pointer">
            <Toggle checked={settings.showAttribution} onchange={toggleAttribution} />
            <span class="type-body text-[var(--text-1)]">Show photographer name in ambient mode</span>
        </label>
    </div>
</div>
