<script lang="ts">
    import { toggleableWidgets, fixedHeaderWidgets, fixedFooterWidgets } from '$lib/constants/widgets';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import Toggle from '$lib/components/Toggle.svelte';
</script>

<div class="flex flex-col gap-5">
    <!-- Configurable widgets -->
    <div class="flex flex-col gap-2">
        {#each toggleableWidgets as widget}
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
        {/each}
    </div>

    <!-- Fixed widgets -->
    <div class="border-t border-[var(--border)] pt-4">
        <p class="type-label mb-3 font-semibold uppercase tracking-[0.12em] text-[var(--text-3)]">Always shown</p>
        <div class="flex flex-col gap-2">
            {#each fixedHeaderWidgets as w}
                <div class="flex items-center gap-3 opacity-50">
                    <div class="mt-0.5 shrink-0">
                        <Toggle checked={true} onchange={() => {}} />
                    </div>
                    <div>
                        <p class="type-body select-none text-[var(--text-1)]">{w.label}</p>
                        <p class="type-label select-none text-[var(--text-2)]">Fixed in the dashboard header</p>
                    </div>
                </div>
            {/each}
            {#each fixedFooterWidgets as w}
                <div class="flex items-center gap-3 opacity-50">
                    <div class="mt-0.5 shrink-0">
                        <Toggle checked={true} onchange={() => {}} />
                    </div>
                    <div>
                        <p class="type-body select-none text-[var(--text-1)]">{w.label}</p>
                        <p class="type-label select-none text-[var(--text-2)]">Fixed in the dashboard footer</p>
                    </div>
                </div>
            {/each}
        </div>
    </div>
</div>
