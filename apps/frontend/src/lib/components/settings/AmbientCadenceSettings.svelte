<script lang="ts">
    import ChoiceChips from '../ui/ChoiceChips.svelte';
    import ToggleRow from '../ui/ToggleRow.svelte';
    import { settings } from '#lib/stores/SettingsStore.svelte.ts';
    import { cadenceOptions, nightCadenceOptions } from '#lib/constants/ambient.ts';
    import SettingsGroup from '../ui/SettingsGroup.svelte';
</script>

<div class="flex flex-col gap-5">
    <SettingsGroup title="Change photo every">
        <p class="type-label mb-2 text-(--text-2)">Day</p>
        <ChoiceChips
            label="Day"
            options={cadenceOptions}
            value={settings.cadenceSeconds}
            onchange={(v) => (settings.cadenceSeconds = v)}
        />

        <p class="type-label mt-4 mb-2 text-(--text-2)">Night (10pm – 6am)</p>
        <ChoiceChips
            label="Night"
            options={nightCadenceOptions}
            value={settings.nightCadenceSeconds}
            onchange={(v) => (settings.nightCadenceSeconds = v)}
        />
    </SettingsGroup>

    <SettingsGroup>
        <ToggleRow
            checked={settings.ambientMotion}
            onchange={() => (settings.ambientMotion = !settings.ambientMotion)}
            title="Motion"
            description="Slow pan and zoom across each photo. Turn off for e-ink displays."
        />
    </SettingsGroup>
</div>
