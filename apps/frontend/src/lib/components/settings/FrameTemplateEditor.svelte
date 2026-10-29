<script lang="ts">
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import {
        frameTemplates,
        moveToTemplate,
        normalizeSlots,
        widgetsForSize,
        type FrameSize,
        type FrameTemplate,
        type FrameTemplateId,
    } from '$lib/constants/frameTemplates';
    import { frameWidgets, type FrameWidgetId } from '$lib/constants/frameWidgets';
    import Field from '../ui/Field.svelte';
    import PickerCard from '../ui/PickerCard.svelte';
    import Select from '../ui/Select.svelte';

    const slots = $derived(normalizeSlots(frameSettings.template, frameSettings.slots));
    const template = $derived(frameTemplates.find((t) => t.id === frameSettings.template)!);
    const sizeLabel: Record<FrameSize, string> = { s: 'Small', m: 'Medium', l: 'Large' };
    const label = (id: FrameWidgetId) => frameWidgets.find((w) => w.id === id)!.label;

    function pick(id: FrameTemplateId) {
        if (id === frameSettings.template) return;
        // Leaving Columns brings its widgets along; otherwise the current slots do.
        const current =
            frameSettings.template === 'columns'
                ? [...frameSettings.layout.left, ...frameSettings.layout.right]
                : slots;
        frameSettings.slots = moveToTemplate(id, current);
        frameSettings.template = id;
    }

    function place(i: number, id: FrameWidgetId | '') {
        // A widget shows once: choosing it here empties the slot it was in.
        frameSettings.slots = slots.map((s, j) => (j === i ? id : s === id ? '' : s));
    }

    // Each thumbnail's grid, in the frame's current orientation.
    const thumbStyle = (t: FrameTemplate) => {
        const rows = t.areas[frameSettings.orientation];
        return (
            `grid-template-areas: ${rows.map((r) => `'${r}'`).join(' ')};` +
            ` grid-template-columns: repeat(${rows[0]?.split(' ').length ?? 1}, 1fr);` +
            ` grid-template-rows: repeat(${rows.length || 1}, 1fr)`
        );
    };
</script>

<!--
@component
Settings → Frame's Daily layout: a template picker (thumbnails drawn in the frame's
orientation) and a widget choice per slot, offering only widgets designed for that
slot's size. "Columns" keeps the free two-column editor (WidgetLayoutEditor), which
the caller shows below it.
-->
<div class="flex flex-col gap-4">
    <div class="grid grid-cols-3 gap-3 @lg:grid-cols-6">
        {#each frameTemplates as t (t.id)}
            <PickerCard pressed={t.id === frameSettings.template} onclick={() => pick(t.id)}>
                <div
                    class="m-2 grid gap-1 {frameSettings.orientation === 'portrait' ? 'aspect-[3/4]' : 'aspect-[4/3]'}"
                    style={t.id === 'columns' ? 'grid-template-columns: 1fr 1fr' : thumbStyle(t)}
                    aria-hidden="true"
                >
                    {#if t.id === 'columns'}
                        <div class="flex flex-col gap-1">
                            <span class="flex-1 rounded-sm bg-(--text-3)"></span>
                            <span class="flex-[2] rounded-sm bg-(--text-3)"></span>
                        </div>
                        <div class="flex flex-col gap-1">
                            <span class="flex-[2] rounded-sm bg-(--text-3)"></span>
                            <span class="flex-1 rounded-sm bg-(--text-3)"></span>
                        </div>
                    {:else}
                        {#each t.slots as _, i (i)}
                            <span class="rounded-sm bg-(--text-3)" style="grid-area: s{i}"></span>
                        {/each}
                    {/if}
                </div>
                <span class="type-caption px-2 pb-2 text-center font-semibold">{t.label}</span>
            </PickerCard>
        {/each}
    </div>

    {#if frameSettings.template !== 'columns'}
        <div class="grid gap-3 @md:grid-cols-2">
            {#each template.slots as size, i (i)}
                <Field label="Slot {i + 1} · {sizeLabel[size]}">
                    <Select value={slots[i]} onchange={(e) => place(i, e.currentTarget.value as FrameWidgetId | '')}>
                        <option value="">Empty</option>
                        {#each widgetsForSize(size) as id (id)}
                            <option value={id}>{label(id)}</option>
                        {/each}
                    </Select>
                </Field>
            {/each}
        </div>
        <p class="type-caption text-(--text-2)">
            Each slot lists the widgets designed for its size. Countdowns, Moon Phase, Daily Quote and Last Played
            change shape with the slot; lists show as many items as fit.
        </p>
    {/if}
</div>
