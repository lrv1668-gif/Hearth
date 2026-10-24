<script lang="ts">
    import { onMount } from 'svelte';
    import { CircleCheck, CircleDashed, CircleX, RefreshCw, TriangleAlert } from '@lucide/svelte';
    import { api, type ServiceHealth } from '$lib/api';
    import { hearthServices, type HearthServiceId } from '$lib/constants/services';
    import { summarizeHealth, type HealthTone } from '$lib/diagnostics';
    import { poll } from '$lib/utils';
    import IconButton from '../ui/IconButton.svelte';
    import InlineMessage from '../ui/InlineMessage.svelte';
    import ListRow from '../ui/ListRow.svelte';

    // undefined until the first answer; null when a service didn't answer
    let reports = $state<Partial<Record<HearthServiceId, ServiceHealth | null>>>({});
    let checking = $state(false);

    async function load() {
        checking = true;
        const results = await Promise.all(hearthServices.map((s) => api.health.service(s.id)));
        reports = Object.fromEntries(hearthServices.map((s, i) => [s.id, results[i]]));
        checking = false;
    }

    onMount(() => {
        load();
        return poll(load, 30_000);
    });

    const loaded = $derived(Object.keys(reports).length > 0);
    const summaries = $derived(hearthServices.map((s) => ({ ...s, summary: summarizeHealth(reports[s.id] ?? null) })));
    const needAttention = $derived(
        summaries.filter((s) => s.summary.tone === 'attention' || s.summary.tone === 'down').length
    );
    const notSetUp = $derived(summaries.filter((s) => s.summary.tone === 'unset').length);

    const toneIcon: Record<HealthTone, typeof CircleCheck> = {
        ok: CircleCheck,
        unset: CircleDashed,
        attention: TriangleAlert,
        down: CircleX,
    };
    const toneColor: Record<HealthTone, string> = {
        ok: 'text-(--done)',
        unset: 'text-(--text-3)',
        attention: 'text-(--accent)',
        down: 'text-(--accent)',
    };
</script>

<div class="mb-2 flex items-center justify-between gap-2">
    <InlineMessage>
        {#if !loaded}
            Checking…
        {:else if needAttention === 0}
            Everything is running{notSetUp > 0 ? ` — ${notSetUp} not set up` : ''}.
        {:else}
            {needAttention} of {hearthServices.length} need attention.
        {/if}
    </InlineMessage>
    <IconButton label="Check again" onclick={load} disabled={checking}>
        <RefreshCw class="icon-sm {checking ? 'animate-spin' : ''}" />
    </IconButton>
</div>

{#if loaded}
    <div class="-mx-3 flex flex-col">
        {#each summaries as s (s.id)}
            {@const Icon = toneIcon[s.summary.tone]}
            <ListRow title={s.label} subtitle={s.summary.text}>
                {#snippet meta()}
                    {#each s.summary.details as line}
                        <p class="type-label text-(--text-2)">{line}</p>
                    {/each}
                {/snippet}
                {#snippet actions()}
                    <Icon class="icon-md mr-2 shrink-0 {toneColor[s.summary.tone]}" aria-hidden="true" />
                {/snippet}
            </ListRow>
        {/each}
    </div>
{/if}
