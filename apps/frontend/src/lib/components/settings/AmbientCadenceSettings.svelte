<script lang="ts">
    import ChoiceChips from '../ui/ChoiceChips.svelte';
    import ToggleRow from '../ui/ToggleRow.svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { cadenceOptions, nightCadenceOptions } from '$lib/constants/ambient';
</script>

<div class="space-y-5">
    <div>
        <p class="type-label mb-2 text-(--text-2)">Day</p>
        <ChoiceChips
            label="Day"
            options={cadenceOptions}
            value={settings.cadenceSeconds}
            onchange={(v) => (settings.cadenceSeconds = v)}
        />
    </div>

    <div>
        <p class="type-label mb-2 text-(--text-2)">Night (10pm – 6am)</p>
        <ChoiceChips
            label="Night"
            options={nightCadenceOptions}
            value={settings.nightCadenceSeconds}
            onchange={(v) => (settings.nightCadenceSeconds = v)}
        />
    </div>

    <div class="border-t border-(--border) pt-4">
        <ToggleRow
            checked={settings.ambientMotion}
            onchange={() => (settings.ambientMotion = !settings.ambientMotion)}
            title="Motion"
            description="Slow pan and zoom across each photo. Turn off for e-ink displays."
        />
    </div>
</div>
