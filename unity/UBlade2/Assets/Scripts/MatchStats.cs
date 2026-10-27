using System.Globalization;
using UnityEngine;

// Match ledger for a drome game. Pure data: no scene objects, no Time, no
// PlayerPrefs. The host records events here and reads DromerLines/GoalieLines for
// HudSnapshot.statsLines and HudSnapshot.goalieStatsLines.
//
// PERCENTAGES: FgPercent/EfgPercent/SavePercent return fractions, not 0..100.
// FG% and SV% are 0..1; EFG% is weighted on a two-point scale, so a night of
// four-point goals can reach 2.0 (200%).
//
// ATTRIBUTION: RecordShot/RecordGoal/RecordPossession/RecordInterception/
// RecordInjury/RecordFatal/RecordMultiplierPass take only a side, so they credit that side's
// "active dromer" row. Point the two fields at the skater with the rok:
//   stats.homeActiveDromer = rok.CarrierIndex;        // 0..3
//   stats.awayActiveDromer = rok.CarrierIndex - 4;    // 0..3
// The indexed overloads credit an explicit row when a caller already knows who
// did it (a goal scorer, the skater who was checked).
public sealed class DromerStats
{
    public string name = string.Empty;
    public int points;
    public int fgm;
    public int fga;
    public int twoX;
    public int fourX;
    public int possessionSeconds;
    public int interceptions;
    public int multiplierPasses;
    public int injuries;
    public int fatalities;

    // Field-goal fraction: makes over attempts, 0..1.
    public float FgPercent()
    {
        return fga > 0 ? Mathf.Clamp01(fgm / (float)fga) : 0f;
    }

    // Effective field-goal fraction on a two-point scale: a two-point goal counts
    // once, a four-point goal twice. 0 when there are no attempts, up to 2.
    public float EfgPercent()
    {
        return fga > 0 ? Mathf.Clamp((fgm + fourX) / (float)fga, 0f, 2f) : 0f;
    }
}

public sealed class GoalieStats
{
    public string name = string.Empty;
    public int shotsAgainst;
    public int saves;
    public int errors;

    // Saves over shots against, 0..1.
    public float SavePercent()
    {
        return shotsAgainst > 0 ? Mathf.Clamp01(saves / (float)shotsAgainst) : 0f;
    }
}

public sealed class MatchStats
{
    // Skater row the team-level Record* calls credit. See the attribution note above.
    public int homeActiveDromer;
    public int awayActiveDromer;

    public DromerStats[] Home;
    public DromerStats[] Away;
    public GoalieStats HomeGoalie;
    public GoalieStats AwayGoalie;

    private readonly string homeTeamName;
    private readonly string awayTeamName;
    private float homePossessionCarry;
    private float awayPossessionCarry;

    public MatchStats(TeamDefinition home, TeamDefinition away)
    {
        homeTeamName = TeamName(home, "HOME");
        awayTeamName = TeamName(away, "AWAY");
        Home = Build(home, "HOME");
        Away = Build(away, "AWAY");
        HomeGoalie = new GoalieStats { name = GoalieName(home, "HOME GOALIE") };
        AwayGoalie = new GoalieStats { name = GoalieName(away, "AWAY GOALIE") };
    }

    // One shot. Every call counts an attempt; a goal also counts a make, adds the
    // reported points (default 2, or 4 when viaMultiplier is set and points is
    // unknown) and files the goal as a two-pointer or a four-pointer. When the
    // attempt was already counted at release (RecordShot with goal false), record
    // the made goal with RecordGoal so the attempt is not counted twice.
    public void RecordShot(bool home, bool goal, int points, bool viaMultiplier)
    {
        RecordShot(ActiveIndex(home), home, goal, points, viaMultiplier);
    }

    public void RecordShot(int dromer, bool home, bool goal, int points, bool viaMultiplier)
    {
        DromerStats row = Row(TeamRows(home), dromer);
        if (row == null)
        {
            return;
        }

        row.fga++;
        if (goal)
        {
            RecordGoal(dromer, home, points, viaMultiplier);
        }
    }

    // A made goal without an attempt, for callers that counted the attempt when the
    // shot left the stick. Adds the points and files twoX/fourX exactly like
    // RecordShot(home, true, ...) does.
    public void RecordGoal(bool home, int points, bool viaMultiplier)
    {
        RecordGoal(ActiveIndex(home), home, points, viaMultiplier);
    }

    public void RecordGoal(int dromer, bool home, int points, bool viaMultiplier)
    {
        DromerStats row = Row(TeamRows(home), dromer);
        if (row == null)
        {
            return;
        }

        int value = points;
        if (value <= 0)
        {
            value = viaMultiplier ? 4 : 2;
        }

        row.fgm++;
        row.points += value;
        if (value >= 4)
        {
            row.fourX++;
        }
        else
        {
            row.twoX++;
        }
    }

    // Whole seconds stay on the row; the fractional remainder carries on the side
    // so per-frame calls do not lose time to rounding.
    public void RecordPossession(bool home, float seconds)
    {
        RecordPossession(ActiveIndex(home), home, seconds);
    }

    public void RecordPossession(int dromer, bool home, float seconds)
    {
        if (!(seconds > 0f) || float.IsNaN(seconds) || float.IsInfinity(seconds))
        {
            return;
        }

        DromerStats row = Row(TeamRows(home), dromer);
        if (row == null)
        {
            return;
        }

        seconds = Mathf.Min(seconds, 3600f);
        if (home)
        {
            homePossessionCarry += seconds;
            int whole = (int)homePossessionCarry;
            if (whole > 0)
            {
                homePossessionCarry -= whole;
                row.possessionSeconds += whole;
            }
        }
        else
        {
            awayPossessionCarry += seconds;
            int whole = (int)awayPossessionCarry;
            if (whole > 0)
            {
                awayPossessionCarry -= whole;
                row.possessionSeconds += whole;
            }
        }
    }

    public void RecordInterception(bool home)
    {
        RecordInterception(ActiveIndex(home), home);
    }

    public void RecordInterception(int dromer, bool home)
    {
        DromerStats row = Row(TeamRows(home), dromer);
        if (row != null)
        {
            row.interceptions++;
        }
    }

    public void RecordInjury(bool home)
    {
        RecordInjury(ActiveIndex(home), home);
    }

    public void RecordInjury(int dromer, bool home)
    {
        DromerStats row = Row(TeamRows(home), dromer);
        if (row != null)
        {
            row.injuries++;
        }
    }

    // A fatality is filed as a fatality only; call RecordInjury as well if the game
    // also counts the dead skater as injured.
    public void RecordFatal(bool home)
    {
        RecordFatal(ActiveIndex(home), home);
    }

    public void RecordFatal(int dromer, bool home)
    {
        DromerStats row = Row(TeamRows(home), dromer);
        if (row != null)
        {
            row.fatalities++;
        }
    }

    public void RecordMultiplierPass(bool home)
    {
        RecordMultiplierPass(ActiveIndex(home), home);
    }

    public void RecordMultiplierPass(int dromer, bool home)
    {
        DromerStats row = Row(TeamRows(home), dromer);
        if (row != null)
        {
            row.multiplierPasses++;
        }
    }

    // Every call is a shot against that goalie; saved also counts the save.
    public void RecordSave(bool home, bool saved)
    {
        GoalieStats goalie = home ? HomeGoalie : AwayGoalie;
        if (goalie == null)
        {
            return;
        }

        goalie.shotsAgainst++;
        if (saved)
        {
            goalie.saves++;
        }
    }

    public void RecordError(bool home)
    {
        GoalieStats goalie = home ? HomeGoalie : AwayGoalie;
        if (goalie != null)
        {
            goalie.errors++;
        }
    }

    // Per-frame possession; same ledger as RecordPossession. Use one or the other.
    public void UpdatePossession(bool home, float dt)
    {
        RecordPossession(home, dt);
    }

    // Home block then away block: a team-total line followed by one line per
    // skater, so the panel reads like the 1996 post-game scoresheet.
    public string[] DromerLines()
    {
        DromerStats[] home = Home != null ? Home : new DromerStats[0];
        DromerStats[] away = Away != null ? Away : new DromerStats[0];
        string[] lines = new string[home.Length + away.Length + 2];
        int index = 0;

        lines[index++] = TeamLine(homeTeamName, home);
        for (int i = 0; i < home.Length; i++)
        {
            lines[index++] = DromerLine(home[i]);
        }

        lines[index++] = TeamLine(awayTeamName, away);
        for (int i = 0; i < away.Length; i++)
        {
            lines[index++] = DromerLine(away[i]);
        }

        return lines;
    }

    public string[] GoalieLines()
    {
        return new string[]
        {
            GoalieLine(HomeGoalie, homeTeamName),
            GoalieLine(AwayGoalie, awayTeamName)
        };
    }

    private DromerStats[] TeamRows(bool home)
    {
        return home ? Home : Away;
    }

    private int ActiveIndex(bool home)
    {
        return home ? homeActiveDromer : awayActiveDromer;
    }

    private static DromerStats Row(DromerStats[] rows, int index)
    {
        if (rows == null || rows.Length == 0)
        {
            return null;
        }

        int clamped = index < 0 ? 0 : (index >= rows.Length ? rows.Length - 1 : index);
        return rows[clamped];
    }

    private static DromerStats[] Build(TeamDefinition team, string fallback)
    {
        PlayerDefinition[] skaters = team != null ? team.skaters : null;
        int count = skaters != null && skaters.Length > 0 ? skaters.Length : 4;
        DromerStats[] rows = new DromerStats[count];

        for (int i = 0; i < count; i++)
        {
            string playerName = null;
            if (skaters != null && i < skaters.Length && skaters[i] != null)
            {
                playerName = skaters[i].name;
            }

            rows[i] = new DromerStats
            {
                name = string.IsNullOrEmpty(playerName) ? fallback + " " + Num(i + 1) : playerName
            };
        }

        return rows;
    }

    private static string TeamName(TeamDefinition team, string fallback)
    {
        if (team == null)
        {
            return fallback;
        }
        if (!string.IsNullOrEmpty(team.displayName))
        {
            return team.displayName;
        }
        if (!string.IsNullOrEmpty(team.city))
        {
            return team.city;
        }
        return string.IsNullOrEmpty(team.id) ? fallback : team.id;
    }

    private static string GoalieName(TeamDefinition team, string fallback)
    {
        if (team != null && team.goalie != null && !string.IsNullOrEmpty(team.goalie.name))
        {
            return team.goalie.name;
        }
        return fallback;
    }

    private static string DromerLine(DromerStats row)
    {
        if (row == null)
        {
            return "DROMER  NO DATA";
        }

        return Label(row.name, "DROMER")
            + "  PTS " + Num(row.points)
            + "  FG " + Num(row.fgm) + "/" + Num(row.fga) + " (" + Rate(row.FgPercent(), row.fga, 1f) + ")"
            + "  EFG " + Rate(row.EfgPercent(), row.fga, 2f)
            + "  POSS " + Clock(row.possessionSeconds)
            + "  INT " + Num(row.interceptions)
            + "  MP " + Num(row.multiplierPasses)
            + "  INJ " + Num(row.injuries)
            + "  FAT " + Num(row.fatalities);
    }

    private static string TeamLine(string teamName, DromerStats[] rows)
    {
        int points = 0;
        int made = 0;
        int attempts = 0;
        int fours = 0;
        int possession = 0;
        int injuries = 0;
        int fatalities = 0;

        if (rows != null)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                DromerStats row = rows[i];
                if (row == null)
                {
                    continue;
                }

                points += row.points;
                made += row.fgm;
                attempts += row.fga;
                fours += row.fourX;
                possession += row.possessionSeconds;
                injuries += row.injuries;
                fatalities += row.fatalities;
            }
        }

        float fg = attempts > 0 ? Mathf.Clamp01(made / (float)attempts) : 0f;
        float efg = attempts > 0 ? Mathf.Clamp((made + fours) / (float)attempts, 0f, 2f) : 0f;

        return Label(teamName, "TEAM")
            + "  PTS " + Num(points)
            + "  FG " + Num(made) + "/" + Num(attempts) + " (" + Rate(fg, attempts, 1f) + ")"
            + "  EFG " + Rate(efg, attempts, 2f)
            + "  POSS " + Clock(possession)
            + "  INJ " + Num(injuries)
            + "  FAT " + Num(fatalities);
    }

    private static string GoalieLine(GoalieStats goalie, string teamName)
    {
        if (goalie == null)
        {
            return "GOALIE  NO DATA";
        }

        string line = Label(goalie.name, "GOALIE");
        if (!string.IsNullOrEmpty(teamName))
        {
            line += " (" + teamName.ToUpperInvariant() + ")";
        }

        return line
            + "  SA " + Num(goalie.shotsAgainst)
            + "  SV " + Num(goalie.saves)
            + "  SV% " + Rate(goalie.SavePercent(), goalie.shotsAgainst, 1f)
            + "  ERR " + Num(goalie.errors);
    }

    private static string Label(string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value.ToUpperInvariant();
    }

    private static string Rate(float fraction, int attempts, float maximum)
    {
        if (attempts <= 0)
        {
            return "--";
        }

        int percent = Mathf.RoundToInt(Mathf.Clamp(fraction, 0f, maximum) * 100f);
        return Num(percent) + "%";
    }

    private static string Clock(int seconds)
    {
        if (seconds < 0)
        {
            seconds = 0;
        }

        int minutes = seconds / 60;
        int rest = seconds - minutes * 60;
        return Num(minutes) + ":" + (rest < 10 ? "0" + Num(rest) : Num(rest));
    }

    private static string Num(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
