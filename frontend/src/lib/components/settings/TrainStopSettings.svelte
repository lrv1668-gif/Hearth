<script lang="ts">
    import { settings, subscribeToTrainStop, unsubscribeFromTrainStop } from '$lib/stores/SettingsStore.svelte.ts';
    import { Trash2 } from '@lucide/svelte';

    let newLabel = $state('');
    let newStopKey = $state('');

    function isValidNewStop() {
        return newLabel.trim() !== '' && newStopKey.trim() !== '';
    }

    function handleAdd() {
        subscribeToTrainStop(newLabel.trim(), newStopKey.trim());
        newLabel = '';
        newStopKey = '';
    }
</script>

<div class="flex flex-col gap-6">
    <!-- Subscribed stops list -->
    <div class="flex flex-col gap-1">
        <p class="type-body font-medium text-(--text-1)">Watched stops</p>
        <div class="mt-2 flex flex-col">
            {#if settings.trainStops.length === 0}
                <p class="type-label py-4 text-center text-(--text-3)">No stops added yet.</p>
            {:else}
                {#each settings.trainStops as stop}
                    <div class="group flex items-center gap-3 rounded-lg px-3 py-2 hover:bg-(--surface-hi)">
                        <div class="min-w-0 flex-1">
                            <p class="type-body font-medium text-(--text-1)">{stop.label}</p>
                            <p class="type-label truncate text-(--text-2)">{stop.stopKey}</p>
                        </div>
                        <button
                            onclick={() => unsubscribeFromTrainStop(stop.stopKey)}
                            class="shrink-0 text-(--text-2) opacity-0 transition-opacity group-hover:opacity-100 hover:text-(--text-1)"
                            aria-label="Remove {stop.label}"
                        >
                            <Trash2 class="icon-sm" />
                        </button>
                    </div>
                {/each}
            {/if}
        </div>
    </div>

    <!-- Add stop form -->
    <div class="flex flex-col gap-3 border-t border-(--border) pt-4">
        <p class="type-body font-medium text-(--text-1)">Add a stop</p>
        <p class="type-label text-(--text-2)">
            The stop key is a Transitland stop identifier, not a plain stop name — look yours up at
            <a
                href="https://www.transit.land/"
                target="_blank"
                rel="noopener noreferrer"
                class="text-(--accent) transition-colors hover:text-(--accent-hi)"
            >
                transit.land
            </a>
            (its Onestop ID, e.g. <span class="font-medium">s-9xj5pvewxk-us36~flatironstationeastside</span>).
        </p>
        <div class="flex flex-col gap-2">
            <label class="flex flex-col gap-1">
                <span class="type-caption tracking-widest text-(--text-2) uppercase">Label</span>
                <input
                    type="text"
                    bind:value={newLabel}
                    placeholder="Union Station"
                    class="type-body w-full rounded-lg border border-(--border) bg-(--surface) px-3 py-2 text-(--text-1) transition-colors outline-none placeholder:text-(--text-4) focus:border-(--text-3)"
                />
            </label>
            <label class="flex flex-col gap-1">
                <span class="type-caption tracking-widest text-(--text-2) uppercase">Stop key</span>
                <input
                    type="text"
                    bind:value={newStopKey}
                    placeholder="s-9xj5pvewxk-us36~flatironstationeastside"
                    class="type-body w-full rounded-lg border border-(--border) bg-(--surface) px-3 py-2 text-(--text-1) transition-colors outline-none placeholder:text-(--text-4) focus:border-(--text-3)"
                />
            </label>
        </div>
        <div class="flex justify-end">
            <button
                onclick={handleAdd}
                disabled={!isValidNewStop()}
                class="type-label rounded-full border border-(--border) px-4 py-1.5 text-(--text-1) transition-colors hover:bg-(--surface-hi) disabled:pointer-events-none disabled:opacity-40"
            >
                Add stop
            </button>
        </div>
    </div>
</div>
