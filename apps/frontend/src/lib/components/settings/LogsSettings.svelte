<script lang="ts">
    import { onMount } from 'svelte';
    import { ExternalLink } from '@lucide/svelte';
    import { api } from '$lib/api';
    import Button from '../ui/Button.svelte';
    import InlineMessage from '../ui/InlineMessage.svelte';

    // undefined while checking. Dozzle is opt-in (a Compose profile), so off is a normal state.
    let running = $state<boolean>();

    onMount(() => {
        api.logs.running().then((r) => (running = r));
    });
</script>

{#if running === undefined}
    <InlineMessage>Checking…</InlineMessage>
{:else if running}
    <Button href="/logs/" target="_blank" rel="noopener">
        <ExternalLink class="icon-sm" aria-hidden="true" />
        Open logs
    </Button>
    <p class="type-label mt-3 text-(--text-2)">
        Sign in with the login set by <code class="font-mono">infra/deploy/dozzle-user.sh</code> on the host.
    </p>
{:else}
    <p class="type-body font-medium text-(--text-1)">Logs are off</p>
    <p class="type-label mt-1 text-(--text-2)">
        They’re opt-in, since they can read every container on the host. To turn them on, add
        <code class="font-mono">COMPOSE_PROFILES=logs</code> to the host’s <code class="font-mono">infra/.env</code>,
        then run
        <code class="font-mono">infra/deploy/dozzle-user.sh &lt;name&gt;</code> to set a login — it starts them.
    </p>
{/if}
<p class="type-label mt-3 text-(--text-2)">
    The frame’s own Raspberry Pi keeps its logs on the Pi — see them there with
    <code class="font-mono">journalctl</code>.
</p>
