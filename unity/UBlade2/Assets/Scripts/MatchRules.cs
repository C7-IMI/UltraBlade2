using System;
using UnityEngine;

public enum MatchPhase
{
    Kickoff,
    Playing,
    PointFlash,
    Ended
}

public sealed class MatchConfig
{
    public string playerTeam = "WAYFARERS";
    public string aiTeam = "VOID";
    public Color playerColor = new Color(0.2f, 0.75f, 1f);
    public Color aiColor = new Color(1f, 0.35f, 0.3f);
    public string arenaName = "Neon Rink";
    public float duration = 120f;
    public int targetScore = 5;
    public int difficulty = 2;
    public int round = 1;
    public int totalRounds = 3;

    public MatchConfig Clone()
    {
        return new MatchConfig
        {
            playerTeam = playerTeam,
            aiTeam = aiTeam,
            playerColor = playerColor,
            aiColor = aiColor,
            arenaName = arenaName,
            duration = duration,
            targetScore = targetScore,
            difficulty = difficulty,
            round = round,
            totalRounds = totalRounds
        };
    }
}

// Pure match state for the crystal-collection duel. It owns no scene objects and
// raises events only; the game host applies movement, presentation and persistence.
public sealed class MatchRules
{
    public const float KickoffSeconds = 2f;
    public const float PointFlashSeconds = 1f;
    public const float CrystalRespawnSeconds = 2.5f;

    private static readonly Vector3[] DefaultCrystalPositions =
    {
        new Vector3(3f, 1.1f, 0f),
        new Vector3(-3f, 1.1f, 2f),
        new Vector3(0f, 1.1f, -3f)
    };

    private readonly MatchConfig config;
    private readonly Vector3[] crystalPositions;
    private readonly bool[] crystalActive;
    private readonly float[] respawnTimers;

    private MatchPhase phase;
    private float phaseTimer;
    private float timeLeft;
    private int playerScore;
    private int aiScore;
    private int winner;
    private bool matchEndedRaised;

    public event Action<int, bool> CrystalCollected;
    public event Action<int> MatchEnded;

    public MatchRules(MatchConfig config, Vector3[] crystalPositions)
    {
        this.config = config != null ? config.Clone() : new MatchConfig();
        Vector3[] source = crystalPositions != null ? crystalPositions : DefaultCrystalPositions;
        this.crystalPositions = new Vector3[source.Length];
        Array.Copy(source, this.crystalPositions, source.Length);
        crystalActive = new bool[this.crystalPositions.Length];
        respawnTimers = new float[this.crystalPositions.Length];
        Reset();
    }

    public MatchConfig Config
    {
        get { return config; }
    }

    public MatchPhase Phase
    {
        get { return phase; }
    }

    public float PhaseTimer
    {
        get { return phaseTimer; }
    }

    public float TimeLeft
    {
        get { return timeLeft; }
    }

    public int PlayerScore
    {
        get { return playerScore; }
    }

    public int AiScore
    {
        get { return aiScore; }
    }

    // 0 = no winner yet / draw, 1 = player, 2 = AI. The final value is fixed when
    // the point flash finishes, not when the target score is reached.
    public int Winner
    {
        get { return winner; }
    }

    public bool[] CrystalActive
    {
        get { return crystalActive; }
    }

    public Vector3[] CrystalPositions
    {
        get { return crystalPositions; }
    }

    // Returns the rules to the kickoff state without raising events. BeginMatch is
    // the explicit match-start entry point and resets the same state.
    public void Reset()
    {
        phase = MatchPhase.Kickoff;
        phaseTimer = KickoffSeconds;
        float duration = config.duration;
        timeLeft = duration > 0f && !float.IsInfinity(duration) ? duration : 0f;
        playerScore = 0;
        aiScore = 0;
        winner = 0;
        matchEndedRaised = false;
        for (int i = 0; i < crystalActive.Length; i++)
        {
            crystalActive[i] = true;
            respawnTimers[i] = 0f;
        }
    }

    public void BeginMatch()
    {
        Reset();
        phase = MatchPhase.Kickoff;
        phaseTimer = KickoffSeconds;
    }

    public void Update(float dt)
    {
        if (!(dt > 0f) || float.IsInfinity(dt))
        {
            return;
        }

        TickRespawns(dt);

        if (phase == MatchPhase.Kickoff)
        {
            phaseTimer = Mathf.Max(0f, phaseTimer - dt);
            if (phaseTimer <= 0f)
            {
                phase = MatchPhase.Playing;
                phaseTimer = 0f;
            }
        }
        else if (phase == MatchPhase.Playing)
        {
            timeLeft = Mathf.Max(0f, timeLeft - dt);
            if (timeLeft <= 0f || TargetReached())
            {
                EnterPointFlash();
            }
        }
        else if (phase == MatchPhase.PointFlash)
        {
            phaseTimer = Mathf.Max(0f, phaseTimer - dt);
            if (phaseTimer <= 0f)
            {
                EnterEnded();
            }
        }
    }

    // Score changes are only accepted during live play. Reaching the target score
    // does not change the phase here; Update performs the point flash and ending.
    public bool CollectCrystal(int index, bool byPlayer)
    {
        if (phase != MatchPhase.Playing)
        {
            return false;
        }
        if (index < 0 || index >= crystalActive.Length || !crystalActive[index])
        {
            return false;
        }

        crystalActive[index] = false;
        respawnTimers[index] = CrystalRespawnSeconds;
        if (byPlayer)
        {
            playerScore++;
        }
        else
        {
            aiScore++;
        }

        Action<int, bool> handler = CrystalCollected;
        if (handler != null)
        {
            handler(index, byPlayer);
        }
        return true;
    }

    private void TickRespawns(float dt)
    {
        for (int i = 0; i < crystalActive.Length; i++)
        {
            if (crystalActive[i])
            {
                continue;
            }
            float remaining = respawnTimers[i] - dt;
            if (remaining <= 0f)
            {
                crystalActive[i] = true;
                respawnTimers[i] = 0f;
            }
            else
            {
                respawnTimers[i] = remaining;
            }
        }
    }

    private bool TargetReached()
    {
        return playerScore >= config.targetScore || aiScore >= config.targetScore;
    }

    private void EnterPointFlash()
    {
        phase = MatchPhase.PointFlash;
        phaseTimer = PointFlashSeconds;
    }

    private void EnterEnded()
    {
        phase = MatchPhase.Ended;
        phaseTimer = 0f;
        winner = playerScore > aiScore ? 1 : (aiScore > playerScore ? 2 : 0);
        if (matchEndedRaised)
        {
            return;
        }
        matchEndedRaised = true;

        Action<int> handler = MatchEnded;
        if (handler != null)
        {
            handler(winner);
        }
    }
}
