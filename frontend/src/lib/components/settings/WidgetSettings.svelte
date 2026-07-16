<script lang="ts">
    import { toggleableWidgets } from '$lib/constants/widgets';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import Toggle from '$lib/components/Toggle.svelte';
    import WidgetOrderEditor from '$lib/components/settings/WidgetOrderEditor.svelte';
    import SubTitle from '../SubTitle.svelte';
    import FeedSettings from './FeedSettings.svelte';
</script>

<div class="flex flex-col gap-2 space-y-4">
    <div>
        <SubTitle
            subTitleText="Visibility"
            subTitleDescription="Enable/disable widgets you want to appear on the Schedules page."
        />
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

                    {#if widget.id === 'rss-feeds' && (settings.widgetColumns.left.includes('rss-feeds') || settings.widgetColumns.right.includes('rss-feeds'))}
                        <div class="ml-12 border-l-2 border-l-[var(--border)] pl-4">
                            <FeedSettings />
                        </div>
                    {/if}
                </div>
            {/each}
        </div>
    </div>
    <div>
        <SubTitle
            subTitleText="Layout"
            subTitleDescription="Drag widgets between columns to reorder them on the schedule page. Drag the middle slider to set what you
                want the column width to be."
        />
        <WidgetOrderEditor />
    </div>
</div>
