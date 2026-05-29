<script lang="ts">
    import SettingsSection from '$lib/components/settings/SettingsSection.svelte';
    import ThemePicker from '$lib/components/settings/ThemePicker.svelte';
    import AmbientSettings from '$lib/components/settings/AmbientSettings.svelte';
    import WidgetSettings from '$lib/components/settings/WidgetSettings.svelte';
    import IntegrationsSettings from '$lib/components/settings/IntegrationsSettings.svelte';
</script>

<svelte:head>
    <title>Hearth — Settings</title>
</svelte:head>

<div class="mx-auto w-full max-w-3xl space-y-6 px-8 py-10">
    <SettingsSection title="Appearance">
        <ThemePicker />
    </SettingsSection>

    <SettingsSection title="Ambient Mode">
        <AmbientSettings />
    </SettingsSection>

    <SettingsSection title="Widgets">
        <WidgetSettings />
    </SettingsSection>

    <SettingsSection title="Connections">
        <IntegrationsSettings />
    </SettingsSection>
</div>
