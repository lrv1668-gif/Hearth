<script lang="ts">
    import { toggleableWidgets, fixedHeaderWidgets, fixedFooterWidgets } from '$lib/constants/widgets';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import Toggle from '$lib/components/Toggle.svelte';
</script>

<div class="flex flex-col gap-5">
    <!-- Configurable widgets -->
    <div class="flex flex-col gap-2">
        {#each toggleableWidgets.toSorted((a, b) => a.label.localeCompare(b.label)) as widget}
            <label class="flex cursor-pointer items-center gap-3">
                <div class="mt-0.5 shrink-0">
                    <Toggle
                        checked={settings.widgetColumns.left.includes(widget.id) ||
                            settings.widgetColumns.right.includes(widget.id)}
                        onchange={() => settings.toggleWidget(widget.id)}
                    />
                </div>
                <div>
                    <p class="type-body text-(--text-1) select-none">{widget.label}</p>
                    <p class="type-label text-(--text-2) select-none">{widget.description}</p>
                </div>
            </label>
        {/each}
    </div>

    <!-- Fixed widgets -->
    <div class="border-t border-(--border) pt-4">
        <p class="type-label mb-3 font-semibold tracking-[0.12em] text-(--text-3) uppercase">Always shown</p>
        <div class="flex flex-col gap-2">
            {#each fixedHeaderWidgets as w}
                <div class="flex items-center gap-3 opacity-50">
                    <div class="mt-0.5 shrink-0">
                        <Toggle checked={true} onchange={() => {}} />
                    </div>
                    <div>
                        <p class="type-body text-(--text-1) select-none">{w.label}</p>
                        <p class="type-label text-(--text-2) select-none">Fixed in the dashboard header</p>
                    </div>
                </div>
            {/each}
            {#each fixedFooterWidgets as w}
                <div class="flex items-center gap-3 opacity-50">
                    <div class="mt-0.5 shrink-0">
                        <Toggle checked={true} onchange={() => {}} />
                    </div>
                    <div>
                        <p class="type-body text-(--text-1) select-none">{w.label}</p>
                        <p class="type-label text-(--text-2) select-none">Fixed in the dashboard footer</p>
                    </div>
                </div>
            {/each}
        </div>
    </div>
</div>
