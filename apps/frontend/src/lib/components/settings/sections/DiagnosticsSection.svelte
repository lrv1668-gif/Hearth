<script lang="ts">
    import SettingsCard from '../SettingsCard.svelte';
    import ServiceHealthSettings from '../ServiceHealthSettings.svelte';
    import LogsSettings from '../LogsSettings.svelte';
</script>

<div class="flex flex-col gap-4">
    <SettingsCard
        title="Services"
        description="Each part of Hearth checks itself: its database, the keys it needs, and whether its outside source is answering. Checked every 30 seconds."
    >
        <ServiceHealthSettings />
    </SettingsCard>
    <SettingsCard title="Logs" description="Live logs from every container on the host, searchable, in Dozzle.">
        <LogsSettings />
    </SettingsCard>
</div>
