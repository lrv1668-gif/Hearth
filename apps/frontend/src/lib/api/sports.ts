import { del, get, post } from './http.ts';

export interface SportsLeague {
    key: string; // e.g. "nwsl"
    name: string;
    sport: string; // "soccer", "football", "basketball", "baseball", "hockey"
    path: string;
}

export interface SportsTeam {
    league: string;
    id: string;
    abbreviation: string;
    name: string;
    short_name: string;
    logo: string | null;
    color: string | null;
}

export interface SportsCompetitor {
    id: string;
    abbreviation: string;
    name: string;
    short_name: string;
    logo: string | null;
    score: number | null; // null before kickoff
    shootout: number | null;
    record: string | null;
}

export interface SportsGame {
    id: string;
    league: string;
    sport: string; // the league's: "soccer", "hockey"…
    start: string; // ISO 8601 UTC
    state: 'pre' | 'in' | 'post';
    completed: boolean; // false for a postponed or cancelled game
    status: string; // ESPN's short detail: "72'", "HT", "FT", "Final/OT", "Postponed"…
    home: SportsCompetitor;
    away: SportsCompetitor;
    broadcasts: string[];
    note: string | null; // e.g. a playoff round
}

export interface TeamGames {
    team: SportsTeam;
    live: SportsGame | null;
    last: SportsGame | null;
    outcome: 'win' | 'loss' | 'draw' | null;
    next: SportsGame | null;
}

export interface SportsBoard {
    teams: TeamGames[];
    next_poll_seconds: number;
}

export const sports = {
    leagues: (): Promise<SportsLeague[] | null> => get<SportsLeague[]>('/sports/leagues'),

    teams: (league: string): Promise<SportsTeam[] | null> =>
        get<SportsTeam[]>(`/sports/leagues/${encodeURIComponent(league)}/teams`),

    followed: (): Promise<SportsTeam[] | null> => get<SportsTeam[]>('/sports/followed'),

    follow: (league: string, teamId: string): Promise<SportsTeam[]> =>
        post<SportsTeam[]>('/sports/followed', { league, team_id: teamId }),

    unfollow: (league: string, teamId: string): Promise<boolean> =>
        del(`/sports/followed/${encodeURIComponent(league)}/${encodeURIComponent(teamId)}`),

    games: (): Promise<SportsBoard | null> => get<SportsBoard>('/sports/games'),
};
