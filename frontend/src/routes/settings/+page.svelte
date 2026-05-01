<script lang="ts">
    import { themes, themeGroups } from '$lib/themes';
    import { theme, setTheme } from '$lib/ThemeStore';
    import { settings, updateCadence, toggleCategory, type PhotoCategory } from '$lib/SettingsStore';

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

<div class="max-w-2xl mx-auto px-8 py-10 space-y-12">
    <h1 class="text-2xl font-semibold tracking-wide text-[var(--text-1)]">Settings</h1>

    <!-- Appearance -->
    <section class="space-y-5">
        <h2 class="text-xs tracking-widest uppercase text-[var(--text-3)] border-b border-[var(--border)] pb-2">
            Appearance
        </h2>

        <div class="flex flex-col gap-5">
            {#each themeGroups as group}
                <div class="flex flex-col gap-3">
                    <p class="text-xs tracking-widest uppercase text-[var(--text-4)]">{group}</p>
                    <div class="flex gap-4 flex-wrap">
                        {#each themes.filter((t) => t.group === group) as t}
                            <button
                                onclick={() => setTheme(t.id)}
                                aria-pressed={$theme === t.id}
                                class="flex flex-col items-center gap-2 group"
                            >
                                <span
                                    style="background: {t.fill}; border: 2px solid {t.stroke};"
                                    class="block w-10 h-10 rounded-full transition-all
                                        {$theme === t.id
                                        ? 'ring-2 ring-offset-2 ring-[var(--text-2)] ring-offset-[var(--bg)]'
                                        : 'opacity-60 group-hover:opacity-100'}"
                                ></span>
                                <span
                                    class="text-xs tracking-wide {$theme === t.id
                                        ? 'text-[var(--text-1)] font-medium'
                                        : 'text-[var(--text-3)]'}"
                                >
                                    {t.label}
                                </span>
                            </button>
                        {/each}
                    </div>
                </div>
            {/each}
        </div>
    </section>

    <!-- Ambient Mode -->
    <section class="space-y-6">
        <h2 class="text-xs tracking-widest uppercase text-[var(--text-3)] border-b border-[var(--border)] pb-2">
            Ambient Mode
        </h2>

        <!-- Cadence -->
        <div class="space-y-3">
            <p class="text-sm text-[var(--text-2)]">Photo cadence</p>
            <div class="flex gap-2">
                {#each cadenceOptions as opt}
                    <button
                        onclick={() => updateCadence(opt.value)}
                        class="px-4 py-1.5 rounded-full text-xs tracking-wide border transition-colors
                            {$settings.cadenceSeconds === opt.value
                            ? 'bg-[var(--text-1)] text-[var(--bg)] border-[var(--text-1)]'
                            : 'border-[var(--border)] text-[var(--text-3)] hover:text-[var(--text-1)] hover:border-[var(--text-2)]'}"
                    >
                        {opt.label}
                    </button>
                {/each}
            </div>
        </div>

        <!-- Categories -->
        <div class="space-y-3">
            <p class="text-sm text-[var(--text-2)]">Photo categories</p>
            <div class="flex flex-col gap-2">
                {#each categoryOptions as cat}
                    <label class="flex items-center gap-3 cursor-pointer group">
                        <input
                            type="checkbox"
                            checked={$settings.photoCategories.includes(cat.id)}
                            onchange={() => toggleCategory(cat.id)}
                            class="w-4 h-4 accent-[var(--accent)]"
                        />
                        <span class="text-sm text-[var(--text-2)] group-hover:text-[var(--text-1)] transition-colors">
                            {cat.label}
                        </span>
                    </label>
                {/each}
            </div>
        </div>

        <!-- Attribution -->
        <div class="space-y-3">
            <p class="text-sm text-[var(--text-2)]">Photographer info</p>
            <label class="flex items-center gap-3 cursor-pointer group">
                <input
                    type="checkbox"
                    checked={$settings.showAttribution}
                    onchange={toggleAttribution}
                    class="w-4 h-4 accent-[var(--accent)]"
                />
                <span class="text-sm text-[var(--text-2)] group-hover:text-[var(--text-1)] transition-colors">
                    Show photographer name in ambient mode
                </span>
            </label>
        </div>
    </section>
</div>
