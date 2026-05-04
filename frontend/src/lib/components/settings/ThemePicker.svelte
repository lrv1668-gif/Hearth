<script lang="ts">
    import { themes, THEME_GROUPS } from '$lib/constants/themes';
    import { themeStore, setTheme } from '$lib/stores/ThemeStore.svelte.ts';
</script>

<div class="flex flex-col gap-5">
    {#each THEME_GROUPS as group}
        <div class="flex flex-col gap-3">
            <p class="type-body tracking-widest uppercase text-[var(--text-1)]">{group}</p>
            <div class="flex gap-4 flex-wrap">
                {#each themes.filter((t) => t.group === group) as t}
                    <button
                        onclick={() => setTheme(t.id)}
                        aria-pressed={themeStore.theme === t.id}
                        class="flex flex-col items-center gap-2 group {themeStore.theme === t.id ? 'pointer-events-none' : ''}"
                    >
                        <span
                            style="background: {t.fill}; border: 2px solid {t.stroke};"
                            class="block w-10 h-10 rounded-full transition-all
                                {themeStore.theme === t.id
                                ? 'ring-2 ring-offset-2 ring-[var(--text-3)] ring-offset-[var(--bg)]'
                                : 'opacity-60 group-hover:opacity-100'}"
                        ></span>
                        <span
                            class="type-label tracking-wide {themeStore.theme === t.id
                                ? 'text-[var(--text-1)] font-medium'
                                : 'text-[var(--text-2)]'}"
                        >
                            {t.label}
                        </span>
                    </button>
                {/each}
            </div>
        </div>
    {/each}
</div>
