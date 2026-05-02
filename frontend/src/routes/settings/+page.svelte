<script lang="ts">
    import SettingsSection from '$lib/components/settings/SettingsSection.svelte';
    import ThemePicker from '$lib/components/settings/ThemePicker.svelte';
    import AmbientSettings from '$lib/components/settings/AmbientSettings.svelte';
</script>

<svelte:head>
    <title>Hearth — Settings</title>
</svelte:head>

<div class="max-w-2xl mx-auto px-8 py-10 space-y-6">
    <SettingsSection title="Appearance">
        <ThemePicker />
    </SettingsSection>

    <SettingsSection title="Ambient Mode">
        <AmbientSettings />
    </SettingsSection>
</div>
