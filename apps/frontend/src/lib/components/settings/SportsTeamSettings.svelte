<script lang="ts">
    import { onMount } from 'svelte';
    import { Plus } from '@lucide/svelte';
    import { api, type SportsLeague, type SportsTeam } from '#lib/api.ts';
    import { followedTeams, followTeam, loadFollowedTeams, unfollowTeam } from '#lib/stores/SportsStore.svelte.ts';
    import Button from '../ui/Button.svelte';
    import Field from '../ui/Field.svelte';
    import InlineMessage from '../ui/InlineMessage.svelte';
    import ListRow from '../ui/ListRow.svelte';
    import Select from '../ui/Select.svelte';

    let leagues = $state<SportsLeague[]>([]);
    let league = $state('nwsl');
    let teams = $state<SportsTeam[] | 'loading' | 'error'>('loading');
    let teamId = $state('');
    let saving = $state(false);
    let failed = $state(false);
    // The add form folds behind a button once there's a team, so the list stays short.
    let adding = $state(false);
    const formOpen = $derived(adding || (followedTeams.loaded && followedTeams.teams.length === 0));

    const leagueName = (key: string) => leagues.find((l) => l.key === key)?.name ?? key.toUpperCase();
    const isFollowed = (t: SportsTeam) => followedTeams.teams.some((f) => f.league === t.league && f.id === t.id);

    onMount(async () => {
        loadFollowedTeams();
        leagues = (await api.sports.leagues()) ?? [];
    });

    // Each league change fetches its teams (cached for a day on the server)
    $effect(() => {
        const key = league;
        teams = 'loading';
        teamId = '';
        api.sports.teams(key).then((result) => {
            if (league === key) teams = result ?? 'error';
        });
    });

    async function handleAdd() {
        saving = true;
        failed = false;
        try {
            await followTeam(league, teamId);
            teamId = '';
            adding = false;
        } catch {
            failed = true;
        } finally {
            saving = false;
        }
    }
    import SettingsGroup from '../ui/SettingsGroup.svelte';
</script>

<div class="flex flex-col gap-5">
    <SettingsGroup title="Followed teams">
        <div class="flex flex-col">
            {#each followedTeams.teams as team (`${team.league}:${team.id}`)}
                <ListRow
                    title={team.name}
                    subtitle={leagueName(team.league)}
                    onremove={() => unfollowTeam(team.league, team.id)}
                    removeLabel="Unfollow {team.name}"
                />
            {:else}
                <p class="type-label py-4 text-center text-(--text-2)">
                    {followedTeams.loaded ? 'No teams followed yet.' : 'Loading…'}
                </p>
            {/each}
        </div>
    </SettingsGroup>

    {#if !formOpen}
        <SettingsGroup>
            <Button onclick={() => (adding = true)}><Plus class="icon-sm" /> Follow a team</Button>
        </SettingsGroup>
    {:else}
        <SettingsGroup title="Follow a team">
            <div class="flex flex-col gap-3">
                <div class="flex flex-col gap-2">
                    <Field label="League">
                        <Select bind:value={league} class="w-full">
                            {#each leagues as l (l.key)}
                                <option value={l.key}>{l.name}</option>
                            {/each}
                        </Select>
                    </Field>
                    <Field label="Team">
                        <Select bind:value={teamId} class="w-full" disabled={!Array.isArray(teams)}>
                            <option value="" disabled>
                                {teams === 'loading'
                                    ? 'Loading teams…'
                                    : teams === 'error'
                                      ? 'Teams unavailable'
                                      : 'Choose a team'}
                            </option>
                            {#if Array.isArray(teams)}
                                {#each teams as t (t.id)}
                                    <option value={t.id} disabled={isFollowed(t)}>{t.name}</option>
                                {/each}
                            {/if}
                        </Select>
                    </Field>
                </div>
                {#if failed}
                    <InlineMessage tone="error">Couldn’t follow that team — try again.</InlineMessage>
                {/if}
                <p class="type-label text-(--text-2)">Scores, fixtures and broadcasts come from ESPN.</p>
                <div class="flex justify-end gap-2">
                    {#if followedTeams.teams.length > 0}
                        <Button variant="ghost" onclick={() => (adding = false)}>Cancel</Button>
                    {/if}
                    <Button onclick={handleAdd} disabled={!teamId || saving}>Follow team</Button>
                </div>
            </div>
        </SettingsGroup>
    {/if}
</div>
