<script lang="ts">
    import { toggleableWidgets } from '$lib/constants/widgets';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import Toggle from '$lib/components/Toggle.svelte';
</script>

<div class="flex flex-col gap-2">
    {#each toggleableWidgets as widget}
        <div>
            <label class="flex cursor-pointer items-center gap-3">
                <div class="mt-0.5 shrink-0">
                    <Toggle
                        checked={settings.widgetColumns.left.includes(widget.id) ||
                            settings.widgetColumns.right.includes(widget.id)}
                        onchange={() => settings.toggleWidget(widget.id)}
                    />
                </div>
                <div>
                    <p class="type-body select-none text-[var(--text-1)]">{widget.label}</p>
                    <p class="type-label select-none text-[var(--text-2)]">{widget.description}</p>
                </div>
            </label>
        </div>
    {/each}
</div>
