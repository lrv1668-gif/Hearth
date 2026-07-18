<script lang="ts">
    import { fontThemes } from '$lib/constants/fontThemes';
    import { fontThemeStore, setFontTheme } from '$lib/stores/FontThemeStore.svelte.ts';
</script>

<div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
    {#each fontThemes as f}
        {@const selected = fontThemeStore.fontTheme === f.id}
        <!-- data-font on the button scopes the preset's [data-font] block to this preview -->
        <button
            data-font={f.id}
            onclick={() => setFontTheme(f.id)}
            aria-pressed={selected}
            style={selected ? 'outline: 2px solid var(--accent); outline-offset: 2px;' : ''}
            class="group relative flex flex-col overflow-hidden rounded-xl border-2 bg-[var(--surface)] text-left transition-all
                {selected ? 'border-[var(--accent)]' : 'border-[var(--border)] opacity-85 hover:opacity-100'}"
        >
            <!-- Preview body -->
            <div class="flex min-w-0 flex-1 flex-col gap-1 p-4">
                <p class="type-title font-semibold text-[var(--text-1)]">Aa</p>
                <p class="type-body truncate font-medium text-[var(--text-1)]">Water the plants</p>
                <p class="type-label truncate text-[var(--text-2)]">71° · Sunny, gentle breeze</p>
            </div>
            <!-- Footer -->
            <div class="flex items-center justify-between gap-2 border-t border-[var(--border)] px-4 py-2.5">
                <span class="type-label min-w-0 truncate font-medium text-[var(--text-1)]">{f.label}</span>
                <span class="type-label shrink-0 uppercase tracking-widest text-[var(--text-3)]">{f.tag}</span>
            </div>
        </button>
    {/each}
</div>
