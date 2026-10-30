<script lang="ts">
    import { themes } from '#lib/constants/themes.ts';
    import { themeStore, setTheme } from '#lib/stores/ThemeStore.svelte.ts';
    import Disclosure from '../ui/Disclosure.svelte';

    const current = $derived(themes.find((t) => t.id === themeStore.theme) ?? themes[0]);
</script>

<!-- Folded to the current theme; opens to the full grid -->
<Disclosure>
    {#snippet summary()}
        <span
            class="flex w-16 shrink-0 gap-0.5 rounded-md border p-1.5"
            style="background: {current.colors.bg}; border-color: {current.colors.border}"
            aria-hidden="true"
        >
            {#each [current.colors.accent, current.colors.surfaceHi, current.colors.text2, current.colors.text1] as color}
                <span class="h-3 flex-1 rounded-sm" style="background: {color}"></span>
            {/each}
        </span>
        <span class="type-body font-medium text-(--text-1)">{current.label}</span>
        <span class="type-label text-(--text-2)">{current.group === 'Black & White' ? 'B&W' : 'Color'}</span>
    {/snippet}
    <!-- Columns follow the card's width, not the screen's. Phones get a denser three-up grid: swatches and name only -->
    <div class="@container">
        <div class="grid grid-cols-3 gap-2 sm:gap-3 @xl:grid-cols-4 @4xl:grid-cols-6">
            {#each themes as t}
                {@const selected = themeStore.theme === t.id}
                {@const swatches = [
                    t.colors.surface,
                    t.colors.surfaceHi,
                    t.colors.border,
                    t.colors.text3,
                    t.colors.text2,
                    t.colors.text1,
                ]}
                <button
                    onclick={() => setTheme(t.id)}
                    aria-pressed={selected}
                    style="
                background: {t.colors.bg};
                border-color: {selected ? t.colors.accent : t.colors.border};
                {selected ? `outline: 2px solid ${t.colors.accent}; outline-offset: 2px;` : ''}
            "
                    class="group relative flex flex-col overflow-hidden rounded-xl border-2 text-left transition-all
                {selected ? '' : 'opacity-85 hover:opacity-100'}"
                >
                    <!-- Preview body -->
                    <div class="flex flex-1 flex-col gap-2 p-2.5 sm:gap-3 sm:p-4">
                        <!-- Accent bar -->
                        <div style="background: {t.colors.accent};" class="h-0.5 w-8 rounded-full"></div>
                        <!-- Fake widget text -->
                        <p style="color: {t.colors.text1};" class="type-label hidden font-medium sm:block">71° Clear</p>
                        <!-- Color swatches -->
                        <div class="flex gap-1 sm:gap-1.5">
                            {#each swatches as color}
                                <span style="background: {color};" class="h-4 w-full rounded-sm sm:h-5"></span>
                            {/each}
                        </div>
                    </div>
                    <!-- Footer -->
                    <div
                        style="border-color: {t.colors.border};"
                        class="flex items-center justify-between border-t px-2.5 py-2 sm:px-4 sm:py-2.5"
                    >
                        <span style="color: {t.colors.text1};" class="type-label font-medium">{t.label}</span>
                        <span
                            style="color: {t.colors.text2};"
                            class="type-label hidden tracking-widest uppercase sm:inline"
                        >
                            {t.group === 'Black & White' ? 'B&W' : 'Color'}
                        </span>
                    </div>
                </button>
            {/each}
        </div>
    </div>
</Disclosure>
