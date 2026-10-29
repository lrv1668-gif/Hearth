<script lang="ts">
    import { onMount } from 'svelte';
    import { SECOND } from '$lib/utils';
    import { Trophy } from '@lucide/svelte';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';
    import ScrollFadeList from '$lib/components/ScrollFadeList.svelte';
    import { sportsStore, loadSports } from '$lib/stores/SportsStore.svelte.ts';
    import { broadcastLabel, opponentLabel, resultLabel, resultWhen, sides, upcomingLabel } from '$lib/sports';
    import { settingsHref } from '$lib/constants/settingsSections';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    // The service says how soon to ask again: every 30 seconds while a followed game is live
    onMount(() => {
        let timer: ReturnType<typeof setTimeout>;
        const schedule = () => {
            timer = setTimeout(async () => {
                await loadSports();
                schedule();
            }, sportsStore.nextPollSeconds * SECOND);
        };
        loadPromise = loadSports().then(schedule);
        return () => clearTimeout(timer);
    });
</script>

<SkeletonLoader promise={loadPromise}>
    {#if sportsStore.error}
        <p class="type-label text-(--text-2)">Scores unavailable.</p>
    {:else if sportsStore.teams.length === 0}
        <div class="flex flex-col gap-1 py-4 text-(--text-2)">
            <Trophy class="icon-sm mb-1" />
            <p class="type-label">No teams followed yet.</p>
            <a
                href={settingsHref('dashboard')}
                class="type-label text-(--accent) transition-colors hover:text-(--accent-hi)"
            >
                Follow a team in Settings →
            </a>
        </div>
    {:else}
        <ScrollFadeList class="gap-4">
            {#each sportsStore.teams as t (`${t.team.league}:${t.team.id}`)}
                <div class="flex flex-col gap-1.5">
                    <div class="flex items-center gap-2">
                        {#if t.team.logo}
                            <img src={t.team.logo} alt="" class="icon-md shrink-0 object-contain" loading="lazy" />
                        {/if}
                        <p class="type-body min-w-0 flex-1 truncate font-medium text-(--text-1)">{t.team.name}</p>
                    </div>

                    {#if t.live}
                        {@const { us, them } = sides(t.live, t.team.id)}
                        {@const tv = broadcastLabel(t.live)}
                        <div class="flex items-center gap-3 rounded-lg bg-(--surface-hi) px-3 py-2">
                            <span class="flex shrink-0 items-center gap-1.5">
                                <span class="h-2 w-2 animate-pulse rounded-full bg-(--accent)" aria-hidden="true"
                                ></span>
                                <span class="type-label font-semibold text-(--accent)">{t.live.status}</span>
                            </span>
                            <p class="type-title min-w-0 flex-1 font-semibold text-(--text-1) tabular-nums">
                                {us.abbreviation}
                                {us.score ?? 0}<span class="text-(--text-2)">–</span>{them.score ?? 0}
                                {them.abbreviation}
                            </p>
                            {#if tv}<span class="type-label shrink-0 text-(--text-2)">{tv}</span>{/if}
                        </div>
                    {/if}

                    {#if t.last}
                        <p class="type-label text-(--text-2)">
                            <span class="font-medium text-(--text-1)">{resultLabel(t)}</span>
                            · {resultWhen(t.last)}
                        </p>
                    {/if}

                    {#if t.next && !t.live}
                        {@const tv = broadcastLabel(t.next)}
                        <p class="type-label text-(--text-2)">
                            Next <span class="font-medium text-(--text-1)">{opponentLabel(t.next, t.team.id)}</span>
                            · {upcomingLabel(t.next.start)}{#if tv}&nbsp;· {tv}{/if}
                            {#if t.next.note}<span class="block">{t.next.note}</span>{/if}
                        </p>
                    {/if}

                    {#if !t.live && !t.last && !t.next}
                        <p class="type-label text-(--text-2)">No games scheduled.</p>
                    {/if}
                </div>
            {/each}
        </ScrollFadeList>
    {/if}
</SkeletonLoader>
