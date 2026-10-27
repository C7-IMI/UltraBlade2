using System;
using System.Collections.Generic;
using UnityEngine;

// Season/gauntlet data for the HyperBlade-fidelity slice. The player picks a team and faces every
// other team in the 14-team league; wins score 3 points. Saved in PlayerPrefs under ublade2.season.v1.
[Serializable]
public sealed class LeagueData
{
    public const string SaveKey = "ublade2.season.v1";
    public const int WinPoints = 3;

    [Serializable]
    private sealed class SaveState
    {
        public string playerTeamId;
        public int round;
        public int wins;
        public int losses;
        public int points;
        public string lastResult;
    }

    private SaveState state = new SaveState();

    public LeagueData()
    {
        ResetSeason();
    }

    public static LeagueData Load()
    {
        var league = new LeagueData();
        var raw = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(raw))
        {
            league.ResetSeason();
            return league;
        }
        try
        {
            var saved = JsonUtility.FromJson<SaveState>(raw);
            if (saved != null)
            {
                league.state = saved;
            }
            league.Normalize();
        }
        catch (Exception)
        {
            league.ResetSeason();
        }
        return league;
    }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(state));
        PlayerPrefs.Save();
    }

    public void Normalize()
    {
        if (state.playerTeamId == null || TeamCatalog.Find(state.playerTeamId) == null)
        {
            state.playerTeamId = "tokyo";
        }
        if (state.round < 0 || state.round > TotalRounds)
        {
            state.round = Mathf.Clamp(state.round, 0, TotalRounds);
        }
        state.wins = Mathf.Max(0, state.wins);
        state.losses = Mathf.Max(0, state.losses);
        state.points = Mathf.Max(0, state.points);
    }

    public void ResetSeason()
    {
        state = new SaveState
        {
            playerTeamId = string.IsNullOrEmpty(state.playerTeamId) ? "tokyo" : state.playerTeamId,
            round = 0,
            wins = 0,
            losses = 0,
            points = 0,
            lastResult = ""
        };
        Normalize();
    }

    public void BeginSeason()
    {
        state.round = 0;
        state.wins = 0;
        state.losses = 0;
        state.points = 0;
        state.lastResult = "";
    }

    public void SetPlayerTeam(string teamId)
    {
        state.playerTeamId = teamId;
        ResetSeason();
    }

    public string PlayerTeamId { get { return state.playerTeamId; } }
    public TeamDefinition PlayerTeam { get { return TeamCatalog.Find(state.playerTeamId); } }
    public int Round { get { return state.round; } }
    public int Wins { get { return state.wins; } }
    public int Losses { get { return state.losses; } }
    public int Points { get { return state.points; } }
    public string LastResultText { get { return state.lastResult; } }
    public int TotalRounds { get { return TeamCatalog.Teams.Length - 1; } }
    public bool SeasonComplete { get { return state.round >= TotalRounds; } }

    public TeamDefinition NextOpponent
    {
        get
        {
            if (SeasonComplete)
            {
                return null;
            }
            var opponents = Opponents();
            return opponents[state.round % opponents.Count];
        }
    }

    private List<TeamDefinition> Opponents()
    {
        var list = new List<TeamDefinition>();
        foreach (var team in TeamCatalog.Teams)
        {
            if (team.id != state.playerTeamId)
            {
                list.Add(team);
            }
        }
        return list;
    }

    public void ResolveMatch(bool playerWon)
    {
        if (playerWon)
        {
            state.wins++;
            state.points += WinPoints;
            state.lastResult = "WON vs " + (NextOpponent != null ? NextOpponent.displayName : "?") + "  +" + WinPoints + " PTS";
        }
        else
        {
            state.losses++;
            state.lastResult = "LOST vs " + (NextOpponent != null ? NextOpponent.displayName : "?");
        }
        state.round = Mathf.Min(TotalRounds, state.round + 1);
    }

    public string[] TableLines()
    {
        var player = PlayerTeam;
        var lines = new List<string>();
        lines.Add("TEAM                     W  L  PTS");
        foreach (var team in TeamCatalog.Teams)
        {
            if (team.id == state.playerTeamId)
            {
                lines.Add(Pad(team.displayName) + "  " + state.wins + "  " + state.losses + "   " + state.points);
            }
            else
            {
                lines.Add(Pad(team.displayName) + "  0  0   0");
            }
        }
        return lines.ToArray();
    }

    private static string Pad(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "                        ";
        }
        if (text.Length >= 22)
        {
            return text.Substring(0, 22);
        }
        return text + new string(' ', 22 - text.Length);
    }
}
