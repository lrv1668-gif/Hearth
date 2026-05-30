<script lang="ts">
    import { themes } from '$lib/constants/themes';
    import { themeStore, setTheme } from '$lib/stores/ThemeStore.svelte.ts';
</script>

<div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
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
            <div class="flex flex-1 flex-col gap-3 p-4">
                <!-- Accent bar -->
                <div style="background: {t.colors.accent};" class="h-0.5 w-8 rounded-full"></div>
                <!-- Fake widget text -->
                <p style="color: {t.colors.text1};" class="type-label font-medium">71° Clear</p>
                <!-- Color swatches -->
                <div class="flex gap-1.5">
                    {#each swatches as color}
                        <span style="background: {color};" class="h-5 w-full rounded-sm"></span>
                    {/each}
                </div>
            </div>
            <!-- Footer -->
            <div
                style="border-color: {t.colors.border};"
                class="flex items-center justify-between border-t px-4 py-2.5"
            >
                <span style="color: {t.colors.text1};" class="type-label font-medium">{t.label}</span>
                <span style="color: {t.colors.text3};" class="type-label uppercase tracking-widest">
                    {t.group === 'Black & White' ? 'B&W' : 'Color'}
                </span>
            </div>
        </button>
    {/each}
</div>
