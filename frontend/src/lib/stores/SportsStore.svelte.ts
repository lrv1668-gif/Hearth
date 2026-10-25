import { api, type SportsTeam, type TeamGames } from '$lib/api';

class SportsStore {
    teams = $state<TeamGames[]>([]);
    /** Seconds until the board is worth asking for again — short while a followed game is live. */
    nextPollSeconds = $state(300);
    error = $state(false);
}

export const sportsStore = new SportsStore();

export async function loadSports() {
    const board = await api.sports.games();
    if (board === null) {
        // Only surface the error when there's nothing to show; a failed refresh keeps the last board.
        sportsStore.error = sportsStore.teams.length === 0;
        return;
    }
    sportsStore.error = false;
    sportsStore.teams = board.teams;
    sportsStore.nextPollSeconds = board.next_poll_seconds;
}

class FollowedTeamsStore {
    teams = $state<SportsTeam[]>([]);
    loaded = $state(false);
}

/** The household's followed teams, for Settings → Sports. Saved on the server, shared by every screen. */
export const followedTeams = new FollowedTeamsStore();

export async function loadFollowedTeams() {
    const teams = await api.sports.followed();
    if (teams) followedTeams.teams = teams;
    followedTeams.loaded = true;
}

export async function followTeam(league: string, teamId: string) {
    followedTeams.teams = await api.sports.follow(league, teamId);
}

export async function unfollowTeam(league: string, teamId: string) {
    if (await api.sports.unfollow(league, teamId)) {
        followedTeams.teams = followedTeams.teams.filter((t) => !(t.league === league && t.id === teamId));
    }
}
