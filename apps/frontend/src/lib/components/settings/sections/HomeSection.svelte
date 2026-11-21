<script lang="ts">
    import SettingsCard from '../SettingsCard.svelte';
    import HomeLocationSettings from '../HomeLocationSettings.svelte';
</script>

<SettingsCard
    title="Location & time zone"
    description="Use this device’s location, search for a city, or type coordinates."
>
    <HomeLocationSettings />
</SettingsCard>
