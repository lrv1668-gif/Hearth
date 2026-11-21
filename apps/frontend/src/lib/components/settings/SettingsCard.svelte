<script lang="ts">
    import type { Snippet } from 'svelte';
    import { settingsCardId } from '#lib/constants/settingsSections.ts';

    // One card of a settings section. A titled card is listed under its section in the
    // sidebar (SettingsSubnav reads data-settings-card) unless `listed` is false.
    let {
        title = '',
        description = '',
        listed = true,
        children,
    }: { title?: string; description?: string; listed?: boolean; children: Snippet } = $props();

    const inNav = $derived(listed && !!title);
</script>

<div
    id={inNav ? settingsCardId(title) : undefined}
    data-settings-card={inNav ? title : undefined}
    tabindex="-1"
    class="scroll-mt-6 rounded-xl border-2 border-(--border) bg-(--surface) p-4 outline-none sm:p-6"
>
    <!-- A hairline under the header marks where the card's own settings start -->
    {#if title || description}
        <div class="mb-5 border-b border-(--border) pb-4">
            {#if title}<h2 class="type-subtitle font-semibold text-(--text-1)">{title}</h2>{/if}
            {#if description}<p class="type-body mt-1 text-(--text-2)">{description}</p>{/if}
        </div>
    {/if}
    {@render children()}
</div>
