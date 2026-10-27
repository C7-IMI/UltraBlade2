using System;

// HyperBlade-fidelity match rules: three periods (3/5/7 minute options), goals worth 2 points
// (4 through the multiplier), a carried/flying rok, and kickoff resets after every goal.
public enum MatchPhase
{
    Kickoff,
    Playing,
    GoalFlash,
    PeriodBreak,
    Ended
}

public sealed class MatchConfig
{
    public TeamDefinition homeTeam;
    public TeamDefinition awayTeam;
    public ArenaDefinition arena;
    public int periods = 3;
    public float periodSeconds = 120f;
    public int difficulty = 2;
}

public sealed class MatchRules
{
    public const float KickoffSeconds = 2.0f;
    public const float GoalFlashSeconds = 1.6f;
    public const float PeriodBreakSeconds = 2.0f;

    public MatchConfig Config { get; private set; }
    public MatchPhase Phase { get; private set; }
    public float PhaseTimer { get; private set; }
    public int Period { get; private set; }
    public float PeriodTime { get; private set; }
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }
    public int HomeGoals { get; private set; }
    public int AwayGoals { get; private set; }
    public int Winner { get; private set; }
    public bool MatchOver { get { return Phase == MatchPhase.Ended; } }

    public event Action<bool, int> GoalScored;
    public event Action<int> PeriodEnded;
    public event Action<int> MatchEnded;

    public MatchRules(MatchConfig config)
    {
        Config = config;
    }

    public void BeginMatch()
    {
        Period = 1;
        HomeScore = 0;
        AwayScore = 0;
        HomeGoals = 0;
        AwayGoals = 0;
        Winner = 0;
        PeriodTime = Config.periodSeconds;
        Phase = MatchPhase.Kickoff;
        PhaseTimer = KickoffSeconds;
    }

    public void Update(float dt)
    {
        if (Phase == MatchPhase.Ended)
        {
            return;
        }
        if (Phase == MatchPhase.Kickoff || Phase == MatchPhase.GoalFlash || Phase == MatchPhase.PeriodBreak)
        {
            PhaseTimer -= dt;
            if (PhaseTimer > 0f)
            {
                return;
            }
            if (Phase == MatchPhase.PeriodBreak)
            {
                Period++;
                PeriodTime = Config.periodSeconds;
                Phase = MatchPhase.Kickoff;
                PhaseTimer = KickoffSeconds;
            }
            else
            {
                Phase = MatchPhase.Playing;
                PhaseTimer = 0f;
            }
            return;
        }

        PeriodTime -= dt;
        if (PeriodTime <= 0f)
        {
            PeriodTime = 0f;
            if (Period >= Config.periods)
            {
                EndMatch();
                return;
            }
            Phase = MatchPhase.PeriodBreak;
            PhaseTimer = PeriodBreakSeconds;
            if (PeriodEnded != null)
            {
                PeriodEnded(Period);
            }
        }
    }

    public int Goal(bool homeScored, int points)
    {
        if (Phase == MatchPhase.Ended)
        {
            return 0;
        }
        int awarded = points == 4 ? 4 : (points == 2 ? 2 : 1);
        if (homeScored)
        {
            HomeScore += awarded;
            HomeGoals++;
        }
        else
        {
            AwayScore += awarded;
            AwayGoals++;
        }
        Phase = MatchPhase.GoalFlash;
        PhaseTimer = GoalFlashSeconds;
        if (GoalScored != null)
        {
            GoalScored(homeScored, awarded);
        }
        return awarded;
    }

    public void ForceEnd(int winner)
    {
        if (Phase == MatchPhase.Ended)
        {
            return;
        }
        Winner = winner;
        Phase = MatchPhase.Ended;
        if (MatchEnded != null)
        {
            MatchEnded(Winner);
        }
    }

    private void EndMatch()
    {
        if (HomeScore > AwayScore)
        {
            Winner = 1;
        }
        else if (AwayScore > HomeScore)
        {
            Winner = 2;
        }
        else
        {
            Winner = 0;
        }
        Phase = MatchPhase.Ended;
        if (MatchEnded != null)
        {
            MatchEnded(Winner);
        }
    }
}
