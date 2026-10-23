<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/state';
    import { CalendarDays, RefreshCw } from '@lucide/svelte';
    import { calendarStore, loadCalendarStatus, refreshCalendarItems } from '$lib/stores/CalendarStore.svelte.ts';
    import { api } from '$lib/api';
    import Button from '../ui/Button.svelte';
    import ConnectionRow from '../ui/ConnectionRow.svelte';

    onMount(() => loadCalendarStatus());

    let refreshing = $state(false);

    // Set by the Calendar service's OAuth callback when Google refused the sign-in.
    const errorCode = $derived(page.url.searchParams.get('calendar_error'));
    const errorMessage = $derived.by(() => {
        if (!errorCode) return null;
        if (errorCode === 'invalid_client')
            return 'Google rejected Hearth’s client secret. Check GOOGLE_CLIENT_SECRET in src/Calendar/.env matches the OAuth client in Google Cloud Console, then restart the calendar service.';
        if (errorCode === 'access_denied') return 'Google access wasn’t granted, so nothing was connected.';
        return `Couldn’t connect to Google (${errorCode}). The calendar service’s log has details.`;
    });

    async function handleRefresh() {
        refreshing = true;
        await refreshCalendarItems();
        refreshing = false;
    }

    async function handleDisconnect() {
        await api.calendar.googleDisconnect();
        calendarStore.items = [];
        await loadCalendarStatus();
    }
</script>

<ConnectionRow
    icon={CalendarDays}
    name="Google Calendar"
    status={calendarStore.googleConnected ? 'Connected — events and tasks are syncing.' : 'Not connected.'}
    error={errorMessage}
>
    {#snippet actions()}
        {#if calendarStore.googleConnected}
            <Button onclick={handleRefresh} disabled={refreshing} aria-label="Refresh calendar data">
                <RefreshCw class="icon-xs {refreshing ? 'animate-spin' : ''}" aria-hidden="true" />
                {refreshing ? 'Refreshing…' : 'Refresh'}
            </Button>
            <Button onclick={handleDisconnect}>Disconnect</Button>
        {:else}
            <Button href="/calendar/google/auth">Connect</Button>
        {/if}
    {/snippet}
</ConnectionRow>
