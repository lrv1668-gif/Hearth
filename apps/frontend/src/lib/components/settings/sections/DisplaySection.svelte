<script lang="ts">
    import Button from '#lib/components/ui/Button.svelte';
    import SettingsGroup from '#lib/components/ui/SettingsGroup.svelte';
    import SizeSlider from '#lib/components/ui/SizeSlider.svelte';
    import ToggleRow from '#lib/components/ui/ToggleRow.svelte';
    import SettingsCard from '../SettingsCard.svelte';
    import ThemePicker from '../ThemePicker.svelte';
    import FontThemePicker from '../FontThemePicker.svelte';
    import { settings } from '#lib/stores/SettingsStore.svelte.ts';
    import { fontSizeStore, setFontSize, MIN_FONT_SCALE, MAX_FONT_SCALE } from '#lib/stores/FontSizeStore.svelte.ts';
</script>

<div class="flex flex-col gap-4">
    <SettingsCard
        title="Theme"
        description="Each theme is a complete colour set. Pick one to preview — applies instantly."
    >
        <ThemePicker />
    </SettingsCard>
    <SettingsCard
        title="Type"
        description="Each font theme sets typeface, weight, and size. Pick one to preview — applies instantly."
    >
        <div class="flex flex-col gap-5">
            <SettingsGroup title="Font"><FontThemePicker /></SettingsGroup>
            <SettingsGroup title="Size" description="Scales all text and icons across the dashboard.">
                <SizeSlider
                    value={fontSizeStore.scale}
                    min={MIN_FONT_SCALE}
                    max={MAX_FONT_SCALE}
                    oninput={setFontSize}
                />
            </SettingsGroup>
        </div>
    </SettingsCard>
    <SettingsCard title="Kiosk mode" description="How Hearth presents in this browser.">
        <ToggleRow
            checked={settings.kioskMode}
            onchange={() => (settings.kioskMode = !settings.kioskMode)}
            title="Kiosk Mode"
        >
            Hides the navigation bar and shows the time, date, and weather header. Best for always-on wall displays. Can
            also be activated with <code class="font-mono">?kiosk=1</code> in the URL.
        </ToggleRow>
        {#if settings.kioskMode}
            <div class="mt-3">
                <Button variant="link" href="/">Go to Dashboard →</Button>
                <p class="type-label mt-0.5 text-(--text-2)">
                    The navigation bar is hidden in kiosk mode — use this link to return to the dashboard.
                </p>
            </div>
        {/if}
    </SettingsCard>
</div>
