using System;
using UnityEngine;

[Serializable]
public sealed class LeagueOpponent
{
    public string name;
    public Color color;
    public int difficulty;
    public string arena;

    public LeagueOpponent()
    {
    }

    public LeagueOpponent(string name, Color color, int difficulty, string arena)
    {
        this.name = name;
        this.color = color;
        this.difficulty = difficulty;
        this.arena = arena;
    }
}

// Four-match league season with a single PlayerPrefs save slot. Opponent content is
// static; only progress is serialized, through a small JsonUtility state object.
[Serializable]
public sealed class LeagueData
{
    public const string SaveKey = "ublade2.league.v1";
    public const int WinPoints = 3;

    private const int Rounds = 4;

    private static readonly LeagueOpponent[] Opponents =
    {
        new LeagueOpponent("Razorwings", new Color(0.2f, 0.9f, 0.95f), 1, "Neon Rink"),
        new LeagueOpponent("Ironclads", new Color(0.95f, 0.6f, 0.2f), 2, "Sky Deck"),
        new LeagueOpponent("Nightfall", new Color(0.65f, 0.4f, 0.95f), 2, "Foundry"),
        new LeagueOpponent("Void Syndicate", new Color(0.95f, 0.2f, 0.4f), 3, "Void Dome")
    };

    private int roundIndex;
    private int wins;
    private int losses;
    private int points;
    private string lastResult = string.Empty;
    private int[] results = new int[Rounds];

    [Serializable]
    private sealed class SaveState
    {
        public int round;
        public int wins;
        public int losses;
        public int points;
        public int[] results;
        public string lastResult;

        public SaveState()
        {
        }
    }

    // Zero-based index of the next opponent. It becomes TotalRounds when the
    // season is complete.
    public int Round
    {
        get { return Mathf.Clamp(roundIndex, 0, Rounds); }
    }

    public int TotalRounds
    {
        get { return Rounds; }
    }

    public int Wins
    {
        get { return wins; }
    }

    public int Losses
    {
        get { return losses; }
    }

    public int Points
    {
        get { return points; }
    }

    public bool SeasonComplete
    {
        get { return Round >= Rounds; }
    }

    public LeagueOpponent NextOpponent
    {
        get
        {
            int index = Round;
            return index < Rounds ? Opponents[index] : null;
        }
    }

    // Short human-readable summary of the most recently resolved match. Empty
    // before the first match of a season.
    public string LastResultText
    {
        get { return string.IsNullOrEmpty(lastResult) ? string.Empty : lastResult; }
    }

    public static LeagueData Load()
    {
        LeagueData data = new LeagueData();
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            data.Deserialize(json);
        }
        return data;
    }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, Serialize());
        PlayerPrefs.Save();
    }

    public void ResetSeason()
    {
        roundIndex = 0;
        wins = 0;
        losses = 0;
        points = 0;
        lastResult = string.Empty;
        results = new int[Rounds];
    }

    // Clear progress in memory. Persistence stays explicit: call Save afterwards
    // when a new season should survive a restart.
    public void BeginSeason()
    {
        ResetSeason();
    }

    // Resolve the fixture against NextOpponent. Wins are worth WinPoints and both
    // outcomes advance the round index. Extra calls after the season are ignored.
    public void ResolveMatch(bool playerWon)
    {
        int index = Round;
        if (index >= Rounds)
        {
            return;
        }

        LeagueOpponent opponent = Opponents[index];
        if (playerWon)
        {
            wins++;
            points += WinPoints;
            results[index] = 1;
            lastResult = "WON vs " + opponent.name + "  +" + WinPoints + " PTS";
        }
        else
        {
            losses++;
            results[index] = 2;
            lastResult = "LOST vs " + opponent.name;
        }
        roundIndex = index + 1;
    }

    public string Serialize()
    {
        SaveState state = new SaveState
        {
            round = roundIndex,
            wins = wins,
            losses = losses,
            points = points,
            results = new int[Rounds],
            lastResult = lastResult
        };
        if (results != null)
        {
            Array.Copy(results, state.results, Mathf.Min(results.Length, state.results.Length));
        }
        return JsonUtility.ToJson(state);
    }

    public void Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        SaveState state;
        try
        {
            state = JsonUtility.FromJson<SaveState>(json);
        }
        catch (Exception)
        {
            return;
        }
        if (state == null)
        {
            return;
        }

        roundIndex = Mathf.Clamp(state.round, 0, Rounds);
        wins = Mathf.Max(0, state.wins);
        losses = Mathf.Max(0, state.losses);
        points = Mathf.Max(0, state.points);
        lastResult = state.lastResult != null ? state.lastResult : string.Empty;

        results = new int[Rounds];
        if (state.results != null)
        {
            int count = Mathf.Min(results.Length, state.results.Length);
            for (int i = 0; i < count; i++)
            {
                int value = state.results[i];
                results[i] = value == 1 || value == 2 ? value : 0;
            }
        }
        for (int i = roundIndex; i < results.Length; i++)
        {
            results[i] = 0;
        }
    }

    // Header plus one row per fixture. W/L/PTS are the player's result in that
    // fixture; unplayed fixtures read 0 0 0.
    public string[] TableLines()
    {
        int count = Opponents.Length;
        string[] lines = new string[count + 1];
        lines[0] = "TEAM  W  L  PTS";
        for (int i = 0; i < count; i++)
        {
            int result = results != null && i < results.Length ? results[i] : 0;
            int rowWins = result == 1 ? 1 : 0;
            int rowLosses = result == 2 ? 1 : 0;
            int rowPoints = result == 1 ? WinPoints : 0;
            string team = Opponents[i] != null && !string.IsNullOrEmpty(Opponents[i].name)
                ? Opponents[i].name
                : "TEAM " + (i + 1);
            lines[i + 1] = team + "  " + rowWins + "  " + rowLosses + "  " + rowPoints;
        }
        return lines;
    }
}
