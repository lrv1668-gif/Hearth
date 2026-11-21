<script lang="ts">
    import { Ban, Check, CircleAlert } from '@lucide/svelte';
    import { frameThemes } from '#lib/constants/frameThemes.ts';
    import {
        CUSTOM_ROLES,
        customThemeFromTokens,
        PAPERS,
        rate,
        rateTheme,
        ROLE_INFO,
        roleOptions,
        type CustomFrameTheme,
        type CustomRole,
        type RatingLevel,
    } from '#lib/frame/customTheme.ts';
    import { inkName } from '#lib/frame/inks.ts';
    import { frameSettings } from '#lib/stores/FrameSettingsStore.svelte.ts';
    import Chip from '../ui/Chip.svelte';
    import SegmentedControl from '../ui/SegmentedControl.svelte';
    import ToggleRow from '../ui/ToggleRow.svelte';

    const theme = $derived(frameSettings.customTheme);
    const ratings = $derived(rateTheme(theme));
    // Avoids first, then the use-with-care notes
    const issues = $derived(
        ratings
            .filter((r) => r.rating.level !== 'good')
            .sort((a, b) => +(b.rating.level === 'avoid') - +(a.rating.level === 'avoid'))
    );
    const avoided = $derived(issues.filter((r) => r.rating.level === 'avoid').length);

    /** Show the fills that would dither into speckle or hide type, not just the ones that print well. */
    let showAll = $state(false);

    const ICONS = { good: Check, ok: CircleAlert, avoid: Ban } as const;
    const LEVEL_LABEL: Record<RatingLevel, string> = { good: 'Prints well', ok: 'Use with care', avoid: 'Avoid' };

    function set(patch: Partial<CustomFrameTheme>) {
        frameSettings.customTheme = { ...frameSettings.customTheme, ...patch };
    }

    // Flipping the paper flips what would vanish on it: type in the new paper's colour
    // takes the old one's, and fills that were plain paper stay plain paper.
    function setPaper(paper: string) {
        const old = theme.paper;
        if (paper === old) return;
        const next = { ...theme, paper };
        for (const role of CUSTOM_ROLES) {
            const kind = ROLE_INFO[role].kind;
            if (kind === 'ink' && next[role] === paper) next[role] = old;
            if (kind === 'fill' && next[role] === old) next[role] = paper;
        }
        for (const role of ['surface', 'band'] as const)
            if (rate(next, role, next[role]).level === 'avoid') next[role] = paper;
        frameSettings.customTheme = next;
    }

    // A built-in theme's colours, read from its own [data-theme] block in themes.css
    function startFrom(id: string) {
        const probe = document.createElement('div');
        probe.dataset.theme = `epaper-${id}`;
        probe.hidden = true;
        document.body.append(probe);
        const style = getComputedStyle(probe);
        const tokens = Object.fromEntries(
            ['bg', 'text-1', 'text-2', 'border', 'accent', 'block', 'surface', 'surface-hi', 'arc-golden'].map((t) => [
                t,
                style.getPropertyValue(`--${t}`),
            ])
        );
        probe.remove();
        frameSettings.customTheme = customThemeFromTokens(tokens);
    }

    /** A role's colours, each rated against the rest of the theme. */
    function options(role: CustomRole) {
        return roleOptions(role)
            .map((hex) => ({ hex, rating: rate(theme, role, hex) }))
            .filter((o) => showAll || o.rating.level !== 'avoid' || o.hex === theme[role]);
    }
</script>

<!--
@component
Settings → Frame's builder for the Custom frame theme: a colour per role, chosen from
what the panel prints cleanly — pure inks for type and lines, the inks and their two-ink
mixes for fills — each rated against the rest of the theme as it stands
(lib/frame/customTheme.ts).
-->
<div class="flex flex-col gap-5">
    <div>
        <p class="type-label mb-2 text-(--text-2)">Start from</p>
        <div class="flex flex-wrap gap-2" role="group" aria-label="Start from a built-in theme">
            {#each frameThemes as t (t.id)}
                <Chip pressed={false} onclick={() => startFrom(t.id)}>
                    <span class="flex gap-0.5 rounded-sm p-0.5" style="background: {t.swatches[0]}" aria-hidden="true">
                        {#each t.swatches.slice(1, 4) as color}
                            <span class="size-2.5 rounded-[2px]" style="background: {color}"></span>
                        {/each}
                    </span>
                    {t.label}
                </Chip>
            {/each}
        </div>
    </div>

    <div>
        <p class="type-label mb-2 text-(--text-2)">Paper</p>
        <SegmentedControl
            options={[
                { value: PAPERS[0], label: 'White' },
                { value: PAPERS[1], label: 'Black' },
            ]}
            value={theme.paper}
            onchange={setPaper}
            label="Paper"
        />
    </div>

    <!-- How the theme as a whole will print -->
    <div class="rounded-lg border border-(--border) p-3" aria-live="polite">
        {#if avoided === 0}
            <p class="type-label flex items-center gap-1.5 font-medium text-(--text-1)">
                <Check class="icon-sm shrink-0 text-(--accent)" aria-hidden="true" />
                {issues.length === 0 ? 'Every colour prints cleanly on the panel' : 'Prints well on the panel'}
            </p>
        {:else}
            <p class="type-label font-medium text-(--danger)">
                {avoided === 1 ? 'One choice' : `${avoided} choices`} won’t print well
            </p>
        {/if}
        {#if issues.length > 0}
            <ul class="mt-1.5 flex flex-col gap-1">
                {#each issues as { role, rating } (role)}
                    {@const Icon = ICONS[rating.level]}
                    <li
                        class="type-caption flex items-start gap-1.5 {rating.level === 'avoid'
                            ? 'text-(--danger)'
                            : 'text-(--text-2)'}"
                    >
                        <Icon class="icon-xs mt-0.5 shrink-0" aria-hidden="true" />
                        <span><span class="font-medium">{ROLE_INFO[role].label}:</span> {rating.reason}</span>
                    </li>
                {/each}
            </ul>
        {/if}
    </div>

    {#each CUSTOM_ROLES.filter((r) => r !== 'paper') as role (role)}
        {@const info = ROLE_INFO[role]}
        {@const current = rate(theme, role, theme[role])}
        {@const Icon = ICONS[current.level]}
        <div>
            <p class="type-label text-(--text-1)">
                <span class="font-medium">{info.label}</span>
                <span class="text-(--text-2)"> · {info.hint}</span>
            </p>
            <!-- Swatches on the frame's own paper, so each reads as it will on the panel -->
            <div
                class="mt-2 flex flex-wrap gap-1.5 rounded-lg border border-(--border) p-2"
                style="background: {theme.paper}"
                role="radiogroup"
                aria-label={info.label}
            >
                {#each options(role) as { hex, rating } (hex)}
                    {@const selected = theme[role] === hex}
                    <button
                        type="button"
                        role="radio"
                        aria-checked={selected}
                        aria-label="{inkName(hex)} — {LEVEL_LABEL[rating.level]}: {rating.reason}"
                        title="{inkName(hex)} — {rating.reason}"
                        class="swatch {rating.level}"
                        class:selected
                        style="background: {hex}"
                        onclick={() => set({ [role]: hex })}
                    ></button>
                {/each}
            </div>
            <p
                class="type-caption mt-1.5 flex items-start gap-1.5 {current.level === 'avoid'
                    ? 'text-(--danger)'
                    : 'text-(--text-2)'}"
            >
                <Icon class="icon-xs mt-0.5 shrink-0" aria-hidden="true" />
                <span><span class="font-medium">{inkName(theme[role])}</span> — {current.reason}</span>
            </p>
        </div>
    {/each}

    <ToggleRow
        title="Show colours that won’t print well"
        description="Colours that would vanish on the paper, dither into speckle or hide the type on them."
        checked={showAll}
        onchange={() => (showAll = !showAll)}
    />
</div>

<style>
    .swatch {
        position: relative;
        width: 2rem;
        height: 2rem;
        border-radius: 0.375rem;
        /* A hairline, so white on white and black on black still show */
        box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--text-2) 50%, transparent);
        outline-offset: 2px;
    }
    .swatch.selected {
        outline: 3px solid var(--accent);
    }
    /* Use with care: a small dot in the corner */
    .swatch.ok::after {
        content: '';
        position: absolute;
        top: 3px;
        right: 3px;
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: var(--text-1);
        box-shadow: 0 0 0 1.5px var(--bg);
    }
    /* Avoid: struck through */
    .swatch.avoid {
        opacity: 0.55;
        background-image: linear-gradient(
            to top right,
            transparent calc(50% - 1px),
            var(--text-1) calc(50% - 1px),
            var(--text-1) calc(50% + 1px),
            transparent calc(50% + 1px)
        ) !important;
    }
</style>
