import type { SportsGame, TeamGames } from '#lib/api.ts';
import { playedLabel } from '#lib/music.ts';

/** Display wording for the Sports widget and the frame's Last Result / Next Game card. */

/** The followed team's side of a game, the opponent's, and whether it's at home. */
export function sides(game: SportsGame, teamId: string) {
    const home = game.home.id === teamId;
    return { us: home ? game.home : game.away, them: home ? game.away : game.home, home };
}

/** "vs Bay FC" at home, "at Denver" away — the opponent's short name. */
export function opponentLabel(game: SportsGame, teamId: string): string {
    const { them, home } = sides(game, teamId);
    return `${home ? 'vs' : 'at'} ${them.short_name}`;
}

/** The score from the team's side, "3–2", with a soccer shootout after it: "2–2 (4–3 pens)". */
export function scoreLine(game: SportsGame, teamId: string): string {
    const { us, them } = sides(game, teamId);
    const score = `${us.score ?? 0}–${them.score ?? 0}`;
    if (us.shootout === null || them.shootout === null) return score;
    return game.sport === 'soccer' ? `${score} (${us.shootout}–${them.shootout} pens)` : score;
}

const OUTCOME_WORDS = { win: 'Won', loss: 'Lost', draw: 'Drew' } as const;

/** "Won 3–2 at Denver" — the team's last result. */
export function resultLabel(t: TeamGames): string | null {
    if (!t.last) return null;
    const verb = t.outcome ? OUTCOME_WORDS[t.outcome] : 'Final';
    return `${verb} ${scoreLine(t.last, t.team.id)} ${opponentLabel(t.last, t.team.id)}`;
}

/** When the last result was played, in words that stay true until the next refresh: "last night". */
export function resultWhen(game: SportsGame, now: Date = new Date()): string {
    return playedLabel(game.start, now);
}

function clock(d: Date): string {
    return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
}

function day(d: Date): number {
    return new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
}

/** When the next game starts: "Tonight 7:10 PM", "Tomorrow 1:00 PM", "Saturday 7:30 PM", then "Nov 8, 7:30 PM". */
export function upcomingLabel(start: string, now: Date = new Date()): string {
    const d = new Date(start);
    const daysAhead = Math.round((day(d) - day(now)) / 86_400_000);
    const time = clock(d);
    if (daysAhead <= 0) return `${d.getHours() >= 17 ? 'Tonight' : 'Today'} ${time}`;
    if (daysAhead === 1) return `Tomorrow ${time}`;
    if (daysAhead < 7) return `${d.toLocaleDateString('en-US', { weekday: 'long' })} ${time}`;
    return `${d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}, ${time}`;
}

/** "ESPN · ESPN Deportes" — at most two, so a long rights list doesn't take over the line. */
export function broadcastLabel(game: SportsGame, max = 2): string | null {
    return game.broadcasts.length > 0 ? game.broadcasts.slice(0, max).join(' · ') : null;
}
