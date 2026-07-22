<script lang="ts">
    import Toggle from '$lib/components/Toggle.svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { cadenceOptions, nightCadenceOptions } from '$lib/constants/ambient';
</script>

{#snippet chip(label: string, selected: boolean, onclick: () => void)}
    <button
        {onclick}
        class="type-label rounded-full border px-4 py-1.5 tracking-wide transition-colors
            {selected
            ? 'pointer-events-none border-(--text-1) bg-(--text-1) text-(--bg)'
            : 'border-(--border) text-(--text-1) hover:border-(--text-2) hover:bg-(--surface-hi)'}"
    >
        {label}
    </button>
{/snippet}

<div class="space-y-5">
    <div>
        <p class="type-label mb-2 text-(--text-2)">Day</p>
        <div class="flex flex-wrap gap-2">
            {#each cadenceOptions as opt}
                {@render chip(
                    opt.label,
                    settings.cadenceSeconds === opt.value,
                    () => (settings.cadenceSeconds = opt.value)
                )}
            {/each}
        </div>
    </div>

    <div>
        <p class="type-label mb-2 text-(--text-2)">Night (10pm – 6am)</p>
        <div class="flex flex-wrap gap-2">
            {#each nightCadenceOptions as opt}
                {@render chip(
                    opt.label,
                    settings.nightCadenceSeconds === opt.value,
                    () => (settings.nightCadenceSeconds = opt.value)
                )}
            {/each}
        </div>
    </div>

    <label class="flex cursor-pointer items-center gap-3 border-t border-(--border) pt-4">
        <div class="shrink-0">
            <Toggle
                checked={settings.ambientMotion}
                onchange={() => (settings.ambientMotion = !settings.ambientMotion)}
            />
        </div>
        <div>
            <p class="type-body text-(--text-1) select-none">Motion</p>
            <p class="type-label text-(--text-2) select-none">
                Slow pan and zoom across each photo. Turn off for e-ink displays.
            </p>
        </div>
    </label>
</div>
