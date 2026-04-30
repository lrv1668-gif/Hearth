<script lang="ts">
    import { themes, type ThemeId } from '$lib/themes';

    interface Props {
        theme: ThemeId;
        onChange: (id: ThemeId) => void;
    }

    let { theme, onChange }: Props = $props();
</script>

<div class="flex items-center gap-2" role="group" aria-label="Color theme">
    {#each themes as t}
        <button
            onclick={() => onChange(t.id)}
            title={t.label}
            aria-pressed={theme === t.id}
            style="background: {t.fill}; border: 1.5px solid {t.stroke};"
            class="w-4 h-4 rounded-full transition-all
             {theme === t.id
                ? 'ring-2 ring-offset-2 ring-[var(--text-3)] ring-offset-[var(--bg)] opacity-100'
                : 'opacity-40 hover:opacity-80'}"
        ></button>
    {/each}
</div>
