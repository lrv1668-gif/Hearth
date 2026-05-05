<script lang="ts">
    import { themes, THEME_GROUPS } from '$lib/constants/themes';
    import { themeStore, setTheme } from '$lib/stores/ThemeStore.svelte.ts';
    import SubTitle from '../SubTitle.svelte';
</script>

<SubTitle subTitleText="Themes" subTitleDescription="Choose a theme that best fits you and your home's style." />

<div class="flex flex-col gap-5">
    {#each THEME_GROUPS as group}
        <div class="flex flex-col gap-3">
            <p class="type-label uppercase tracking-widest text-[var(--text-1)]">{group}</p>
            <div class="flex flex-wrap gap-4">
                {#each themes.filter((t) => t.group === group) as t}
                    <button
                        onclick={() => setTheme(t.id)}
                        aria-pressed={themeStore.theme === t.id}
                        class="group flex flex-col items-center gap-2 {themeStore.theme === t.id
                            ? 'pointer-events-none'
                            : ''}"
                    >
                        <span
                            style="background: {t.fill}; border: 2px solid {t.stroke};"
                            class="block h-10 w-10 rounded-full transition-all
                                {themeStore.theme === t.id
                                ? 'ring-2 ring-[var(--text-3)] ring-offset-2 ring-offset-[var(--bg)]'
                                : 'opacity-60 group-hover:opacity-100'}"
                        ></span>
                        <span
                            class="type-label tracking-wide {themeStore.theme === t.id
                                ? 'font-medium text-[var(--text-1)]'
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
