import { describe, expect, it } from 'vitest';
import type { SportsCompetitor, SportsGame, TeamGames } from './api';
import { broadcastLabel, opponentLabel, resultLabel, scoreLine, upcomingLabel } from './sports';

const side = (id: string, name: string, score: number | null, shootout: number | null = null): SportsCompetitor => ({
    id,
    abbreviation: name.slice(0, 3).toUpperCase(),
    name,
    short_name: name,
    logo: null,
    score,
    shootout,
    record: null,
});

const game = (home: SportsCompetitor, away: SportsCompetitor, over: Partial<SportsGame> = {}): SportsGame => ({
    id: '1',
    league: 'nwsl',
    sport: 'soccer',
    start: new Date(2026, 9, 24, 19, 0).toISOString(),
    state: 'post',
    completed: true,
    status: 'FT',
    home,
    away,
    broadcasts: [],
    note: null,
    ...over,
});

const team = {
    league: 'nwsl',
    id: 'la',
    abbreviation: 'LA',
    name: 'Angel City FC',
    short_name: 'Angel City',
    logo: null,
    color: null,
};

describe('scoreLine and opponentLabel', () => {
    it('reads from the followed team’s side, home or away', () => {
        const away = game(side('den', 'Denver', 2), side('la', 'Angel City', 3));
        expect(scoreLine(away, 'la')).toBe('3–2');
        expect(opponentLabel(away, 'la')).toBe('at Denver');

        const home = game(side('la', 'Angel City', 2), side('bay', 'Bay', 0));
        expect(scoreLine(home, 'la')).toBe('2–0');
        expect(opponentLabel(home, 'la')).toBe('vs Bay');
    });

    it('adds a soccer shootout, not a hockey one', () => {
        const pens = game(side('la', 'Angel City', 2, 4), side('bay', 'Bay', 2, 3));
        expect(scoreLine(pens, 'la')).toBe('2–2 (4–3 pens)');
        expect(scoreLine({ ...pens, sport: 'hockey' }, 'la')).toBe('2–2');
    });
});

describe('resultLabel', () => {
    it('words a win, loss and draw', () => {
        const t = (outcome: TeamGames['outcome'], us: number, them: number): TeamGames => ({
            team,
            live: null,
            last: game(side('la', 'Angel City', us), side('por', 'Portland', them)),
            outcome,
            next: null,
        });
        expect(resultLabel(t('win', 2, 1))).toBe('Won 2–1 vs Portland');
        expect(resultLabel(t('loss', 0, 1))).toBe('Lost 0–1 vs Portland');
        expect(resultLabel(t('draw', 1, 1))).toBe('Drew 1–1 vs Portland');
    });

    it('is null with no result yet', () => {
        expect(resultLabel({ team, live: null, last: null, outcome: null, next: null })).toBeNull();
    });
});

describe('upcomingLabel', () => {
    // Saturday 24 October 2026, 10am local
    const now = new Date(2026, 9, 24, 10, 0);
    const at = (day: number, hour: number, minute = 0) => new Date(2026, 9, day, hour, minute).toISOString();

    it('says today or tonight, then tomorrow, the weekday and the date', () => {
        expect(upcomingLabel(at(24, 13), now)).toBe('Today 1:00 PM');
        expect(upcomingLabel(at(24, 19, 10), now)).toBe('Tonight 7:10 PM');
        expect(upcomingLabel(at(25, 15), now)).toBe('Tomorrow 3:00 PM');
        expect(upcomingLabel(at(29, 19, 30), now)).toBe('Thursday 7:30 PM');
        expect(upcomingLabel(at(31, 19, 30), now)).toBe('Oct 31, 7:30 PM');
    });
});

describe('broadcastLabel', () => {
    it('joins at most two networks', () => {
        const g = game(side('a', 'A', null), side('b', 'B', null), {
            broadcasts: ['ESPN', 'ESPN Deportes', 'Disney+'],
        });
        expect(broadcastLabel(g)).toBe('ESPN · ESPN Deportes');
        expect(broadcastLabel({ ...g, broadcasts: [] })).toBeNull();
    });
});
