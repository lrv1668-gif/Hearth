using Data.Abstractions;
using Sports.Records;

namespace Sports;

/// <summary>
/// The household's followed teams. Kept server-side rather than per browser so the frame's
/// headless render shows the same teams as every screen.
/// </summary>
public sealed class FollowedTeamStore([FromKeyedServices("sports")] IDatabase db)
{
    public void Migrate()
    {
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS followed_teams (
                league       TEXT    NOT NULL,
                team_id      TEXT    NOT NULL,
                abbreviation TEXT    NOT NULL,
                name         TEXT    NOT NULL,
                short_name   TEXT    NOT NULL,
                logo         TEXT    NULL,
                color        TEXT    NULL,
                position     INTEGER NOT NULL,
                PRIMARY KEY (league, team_id)
            )
            """);
    }

    public IReadOnlyList<Team> List() => db.Query("""
        SELECT league, team_id, abbreviation, name, short_name, logo, color
        FROM followed_teams
        ORDER BY position, name
        """,
        r => new Team(
            r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
            r.Field<string?>("logo"), r.Field<string?>("color"))).ToList();

    /// <summary>Adds the team at the end of the list; following it again refreshes its details in place.</summary>
    public void Follow(Team team)
    {
        db.NonQuery("""
            INSERT INTO followed_teams (league, team_id, abbreviation, name, short_name, logo, color, position)
            VALUES ($league, $id, $abbr, $name, $short, $logo, $color,
                    (SELECT COALESCE(MAX(position), -1) + 1 FROM followed_teams))
            ON CONFLICT (league, team_id) DO UPDATE SET
                abbreviation = excluded.abbreviation, name = excluded.name, short_name = excluded.short_name,
                logo = excluded.logo, color = excluded.color
            """, cmd =>
        {
            cmd.AddParam("$league", team.League);
            cmd.AddParam("$id", team.Id);
            cmd.AddParam("$abbr", team.Abbreviation);
            cmd.AddParam("$name", team.Name);
            cmd.AddParam("$short", team.ShortName);
            cmd.AddParam("$logo", team.Logo);
            cmd.AddParam("$color", team.Color);
        });
    }

    public void Unfollow(string league, string teamId)
    {
        db.NonQuery("DELETE FROM followed_teams WHERE league = $league AND team_id = $id", cmd =>
        {
            cmd.AddParam("$league", league);
            cmd.AddParam("$id", teamId);
        });
    }
}
