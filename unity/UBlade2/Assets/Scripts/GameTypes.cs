using System;
using UnityEngine;

// Shared UI/state types for the playable UltraBlade 2 slice. Owned by the game host
// (UBlade2Game.cs); the UI, audio and input modules only consume these.

public enum GameScreen
{
    MainMenu,
    Settings,
    League,
    Match,
    Paused,
    Result
}

public enum UiAction
{
    None,
    StartMatch,
    StartNextMatch,
    OpenLeague,
    OpenSettings,
    Back,
    Resume,
    RestartMatch,
    QuitToMenu,
    QuitGame,
    NewSeason
}

[Serializable]
public sealed class GameSettings
{
    public float masterVolume = 0.85f;
    public float musicVolume = 0.5f;
    public float sfxVolume = 0.8f;
    public int difficulty = 2;
    public bool touchControls = true;
    public bool fullscreen = false;

    public void Clamp()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        difficulty = Mathf.Clamp(difficulty, 1, 3);
    }

    public void Save(string prefix)
    {
        Clamp();
        PlayerPrefs.SetFloat(prefix + "master", masterVolume);
        PlayerPrefs.SetFloat(prefix + "music", musicVolume);
        PlayerPrefs.SetFloat(prefix + "sfx", sfxVolume);
        PlayerPrefs.SetInt(prefix + "difficulty", difficulty);
        PlayerPrefs.SetInt(prefix + "touch", touchControls ? 1 : 0);
        PlayerPrefs.SetInt(prefix + "fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static GameSettings Load(string prefix)
    {
        var settings = new GameSettings
        {
            masterVolume = PlayerPrefs.GetFloat(prefix + "master", 0.85f),
            musicVolume = PlayerPrefs.GetFloat(prefix + "music", 0.5f),
            sfxVolume = PlayerPrefs.GetFloat(prefix + "sfx", 0.8f),
            difficulty = PlayerPrefs.GetInt(prefix + "difficulty", 2),
            touchControls = PlayerPrefs.GetInt(prefix + "touch", 1) != 0,
            fullscreen = PlayerPrefs.GetInt(prefix + "fullscreen", 0) != 0
        };
        settings.Clamp();
        return settings;
    }
}

public sealed class HudSnapshot
{
    public string playerTeam = "WAYFARERS";
    public string aiTeam = "VOID";
    public Color playerColor = new Color(0.2f, 0.75f, 1f);
    public Color aiColor = new Color(1f, 0.35f, 0.3f);
    public int playerScore;
    public int aiScore;
    public int targetScore = 5;
    public float timeLeft = 120f;
    public int round = 1;
    public int totalRounds = 3;
    public string arenaName = "Neon Rink";
    public bool kickoff;
    public bool playerStunned;
    public bool aiStunned;
    public float attackCooldown;
    public float dashCooldown;
    public int collectFlash;
    public string banner = "";
    public bool matchOver;
    public string resultText = "";
    public int difficulty = 2;

    // HyperBlade-fidelity match presentation fields.
    public int period = 1;
    public int totalPeriods = 3;
    public float periodTime = 120f;
    public string rokCarrier = "";
    public bool playerHasRok;
    public float shotPower;
    public float speedBurst = 1f;
    public int multiplier = 1;
    public bool goalieSaved;
    public string[] rosterNames = new string[0];
    public float[] rosterHealth = new float[0];
    public int activePlayer;
    public string[] eventFeed = new string[0];
    public string[] statsLines = new string[0];
    public string[] goalieStatsLines = new string[0];
}

public sealed class UiModel
{
    public GameScreen screen = GameScreen.MainMenu;
    public GameSettings settings = new GameSettings();
    public HudSnapshot hud = new HudSnapshot();
    public string[] leagueLines = new string[0];
    public bool seasonComplete;
    public string title = "ULTRABLADE 2";
    public string subtitle = "TRANSNATIONAL HYPERBLADE LEAGUE 2065";
    public string nextOpponent = "";
    public int seasonWins;
    public int seasonLosses;
    public int seasonPoints;
    public bool touch;
}
