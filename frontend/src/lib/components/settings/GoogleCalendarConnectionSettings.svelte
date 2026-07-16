<script lang="ts">
    import { onMount } from 'svelte';
    import { CalendarDays, RefreshCw } from '@lucide/svelte';
    import { calendarStore, loadCalendarStatus, refreshCalendarItems } from '$lib/stores/CalendarStore.svelte.ts';
    import { api } from '$lib/api';

    onMount(() => loadCalendarStatus());

    let refreshing = $state(false);

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

<div class="flex items-center gap-4">
    <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-[var(--surface-hi)]">
        <CalendarDays class="icon-md text-[var(--text-2)]" />
    </div>
    <div class="flex flex-1 items-center justify-between gap-4">
        <div>
            <p class="type-body font-medium text-[var(--text-1)]">Google Calendar</p>
            <p class="type-label text-[var(--text-2)]">
                {calendarStore.googleConnected ? 'Connected — events and tasks are syncing.' : 'Not connected.'}
            </p>
        </div>
        <div class="flex items-center gap-3">
            {#if calendarStore.googleConnected}
                <div class="flex flex-col items-center gap-2 sm:flex-row">
                    <span class="type-body text-[var(--text-1)]">Connected</span>
                    <button
                        onclick={handleRefresh}
                        disabled={refreshing}
                        class="type-body flex items-center gap-1.5 rounded-full border border-[var(--border)] px-3 py-1 text-[var(--text-1)] transition-colors hover:border-[var(--text-2)] disabled:opacity-50"
                        aria-label="Refresh calendar data"
                    >
                        <RefreshCw class="icon-xs {refreshing ? 'animate-spin' : ''}" />
                        {refreshing ? 'Refreshing…' : 'Refresh'}
                    </button>
                    <button
                        onclick={handleDisconnect}
                        class="type-body rounded-full border border-[var(--border)] px-3 py-1 text-[var(--text-1)] transition-colors hover:border-[var(--text-2)] hover:text-[var(--text-1)]"
                    >
                        Disconnect
                    </button>
                </div>
            {:else}
                <a
                    href="/calendar/google/auth"
                    class="type-body rounded-full border border-[var(--border)] px-3 py-1 text-[var(--text-1)] transition-colors hover:border-[var(--text-2)] hover:text-[var(--text-1)]"
                >
                    Connect
                </a>
            {/if}
        </div>
    </div>
</div>
