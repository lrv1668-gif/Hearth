<script lang="ts">
    import CardColumns from '#lib/components/ui/CardColumns.svelte';
    import SettingsCard from '../SettingsCard.svelte';
    import WidgetLayoutEditor from '../WidgetLayoutEditor.svelte';
    import FeedSettings from '../FeedSettings.svelte';
    import TrainStopSettings from '../TrainStopSettings.svelte';
    import SportsTeamSettings from '../SportsTeamSettings.svelte';
    import { settings } from '#lib/stores/SettingsStore.svelte.ts';
    import {
        allWidgets,
        fixedFooterWidgets,
        fixedHeaderWidgets,
        toggleableWidgets,
        type AllWidgetId,
        type WidgetId,
    } from '#lib/constants/widgets.ts';

    const labelFor = (id: string) => allWidgets.find((w) => w.id === id)?.label ?? id;
</script>

<div class="flex flex-col gap-4">
    <SettingsCard
        title="Widgets"
        description="Drag widgets between columns, hide the ones you don’t need, and adjust the column width. Phones show one column — the left first, then the right — with Now Playing above it; the rest of the footer shows on wide screens only."
    >
        <WidgetLayoutEditor
            dragType="dashboard-widget"
            columns={settings.widgetColumns}
            {labelFor}
            onchange={(c) =>
                (settings.widgetColumns = {
                    left: c.left as AllWidgetId[],
                    right: c.right as AllWidgetId[],
                })}
            width={settings.leftColumnWidth}
            onwidthchange={(w) => (settings.leftColumnWidth = w)}
            header={fixedHeaderWidgets}
            footer={fixedFooterWidgets}
            widgets={toggleableWidgets}
            ontoggle={(id) => settings.toggleWidget(id as WidgetId)}
        />
    </SettingsCard>
    <CardColumns>
        {#snippet left()}
            <SettingsCard title="News Feeds" description="Manage RSS feeds and how many articles to show.">
                <FeedSettings />
            </SettingsCard>
            <SettingsCard
                title="Sports"
                description="The teams the Sports widget and the frame’s Last Result & Next Game follow. Shared by every device."
            >
                <SportsTeamSettings />
            </SettingsCard>
        {/snippet}
        {#snippet right()}
            <SettingsCard title="Trains & Buses" description="Manage the transit stops you want to see departures for.">
                <TrainStopSettings />
            </SettingsCard>
        {/snippet}
    </CardColumns>
</div>
