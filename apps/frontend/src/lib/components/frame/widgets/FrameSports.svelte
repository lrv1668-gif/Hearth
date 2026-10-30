<script lang="ts">
    import { sportsStore } from '#lib/stores/SportsStore.svelte.ts';
    import { broadcastLabel, opponentLabel, resultLabel, resultWhen, upcomingLabel } from '#lib/sports.ts';
    import { itemsThatFit } from '#lib/frame.ts';
    import type { FrameSize } from '#lib/constants/frameTemplates.ts';
    import FrameEmpty from '../FrameEmpty.svelte';

    // `size`: the template slot's size class; omitted in the free columns.
    let { now, lines, size }: { now: Date; lines: number; size?: FrameSize } = $props();
    const small = $derived(size === undefined || size === 's');

    // A team (name, result, next game) is ~3.5 lines with its gap; the last needs no gap.
    const shown = $derived(sportsStore.teams.slice(0, Math.min(4, itemsThatFit(lines + 0.5, 3.5))));
</script>

<!--
@component
The e-paper frame's Last Result & Next Game card: each followed team's last result and next
game. A game in progress shows as "Playing now" with no score — the panel redraws every
15 minutes, so a live score would be wrong most of the time it's up.
-->
{#if sportsStore.error}
    <FrameEmpty text="Scores unavailable" {small} />
{:else if shown.length === 0}
    <FrameEmpty text="Follow a team in Settings to show its results" {small} />
{:else}
    <ul class="type-body flex flex-col gap-[0.6em]">
        {#each shown as t (`${t.team.league}:${t.team.id}`)}
            {@const result = resultLabel(t)}
            <li class="flex min-w-0 gap-[0.6em]">
                <!-- The abbreviation on a solid --block tab: crests dither badly to six inks. -->
                <span
                    class="type-label flex h-[1.6em] min-w-[3.2em] shrink-0 items-center justify-center bg-(--block) px-[0.3em] font-bold tracking-wide text-(--on-block)"
                >
                    {t.team.abbreviation}
                </span>
                <div class="min-w-0 flex-1">
                    <p class="truncate font-semibold text-(--text-1)">{t.team.name}</p>
                    {#if t.live}
                        {@const tv = broadcastLabel(t.live, 1)}
                        <p class="type-label truncate text-(--text-1)">
                            Playing now {opponentLabel(t.live, t.team.id)}{#if tv}&nbsp;· on {tv}{/if}
                        </p>
                    {:else if result && t.last}
                        <p class="type-label truncate text-(--text-1)">
                            {result} <span class="text-(--text-2)">· {resultWhen(t.last, now)}</span>
                        </p>
                    {/if}
                    {#if t.next && !t.live}
                        {@const tv = broadcastLabel(t.next, 1)}
                        <p class="type-label truncate text-(--text-2)">
                            Next {opponentLabel(t.next, t.team.id)} · {upcomingLabel(t.next.start, now)}{#if tv}&nbsp;· {tv}{/if}
                        </p>
                    {:else if !t.live && !t.last}
                        <p class="type-label truncate text-(--text-2)">No games scheduled.</p>
                    {/if}
                </div>
            </li>
        {/each}
    </ul>
{/if}
