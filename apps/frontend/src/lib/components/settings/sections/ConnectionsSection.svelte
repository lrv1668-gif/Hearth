<script lang="ts">
    import SettingsCard from '../SettingsCard.svelte';
    import SpotifyConnectionSettings from '../SpotifyConnectionSettings.svelte';
    import GoogleCalendarConnectionSettings from '../GoogleCalendarConnectionSettings.svelte';
    import IcsCalendarSettings from '../IcsCalendarSettings.svelte';
</script>

<div class="flex flex-col gap-4">
    <SettingsCard
        title="Music"
        description="Show what’s playing on the dashboard, and what you last played on the frame."
    >
        <SpotifyConnectionSettings />
    </SettingsCard>
    <SettingsCard title="Calendar" description="Sync events to the calendar and upcoming views.">
        <div class="flex flex-col gap-6">
            <GoogleCalendarConnectionSettings />
            <IcsCalendarSettings />
        </div>
    </SettingsCard>
</div>
