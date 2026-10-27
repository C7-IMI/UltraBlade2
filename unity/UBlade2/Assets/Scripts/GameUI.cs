using System;
using UnityEngine;

// Runtime IMGUI presentation for the playable UltraBlade 2 slice.
//
// 1996 HyperBlade restyle: dark brushed steel, neon red / white / cold blue, chunky
// low-poly HUD blocks, elliptical drome radar and small cam frame boxes. Every texture
// below is generated in code - there is no asset dependency - and the class only reads
// UiModel / HudSnapshot and returns the requested UiAction.
//
// Every interactive rect is at least 48 reference px tall, i.e. >= 44 px at 720p.
public static class GameUI
{
    private const float ReferenceHeight = 720f;

    // ------------------------------------------------- HyperBlade 1996 palette
    private static readonly Color NeonRed = new Color(1f, 0.16f, 0.12f);
    private static readonly Color Blood = new Color(0.58f, 0.05f, 0.06f);
    private static readonly Color Cold = new Color(0.42f, 0.68f, 0.98f);
    private static readonly Color Ice = new Color(0.95f, 0.96f, 0.98f);
    private static readonly Color SteelLight = new Color(0.34f, 0.36f, 0.39f);
    private static readonly Color Ink = new Color(0.90f, 0.92f, 0.95f);
    private static readonly Color Muted = new Color(0.56f, 0.60f, 0.65f);
    private static readonly Color Good = new Color(0.40f, 0.86f, 0.38f);
    private static readonly Color Bad = new Color(0.96f, 0.27f, 0.24f);
    private static readonly Color Warn = new Color(1f, 0.83f, 0.18f);
    private static readonly Color Neutral = new Color(0.88f, 0.90f, 0.94f);

    private static readonly string[] DifficultyLabels = { "EASY", "NORMAL", "HARD" };
    private static readonly string[] EmptyLines = new string[0];
    private static readonly float[] EmptyHealth = new float[0];

    private const int MaxStatKeys = 14;

    // Box-score columns: dromer table and goalie table.
    private static readonly string[] DromerColumns = { "PTS", "FGM", "FGA", "FG%", "2X", "4X", "POSS", "INT", "MULT", "INJ", "FAT" };
    private static readonly string[] GoalieColumns = { "SHT", "SAV", "ERR" };

    private static float scale = 1f;
    private static int styleKey = int.MinValue;
    private static UiModel fallbackModel;

    private static Texture2D solidTexture;
    private static Texture2D panelTexture;
    private static Texture2D brushTexture;
    private static Texture2D scrimDownTexture;
    private static Texture2D scrimUpTexture;
    private static Texture2D dotTexture;
    private static Texture2D ringTexture;
    private static Texture2D discTexture;
    private static Texture2D buttonTexture;
    private static Texture2D buttonHoverTexture;
    private static Texture2D buttonActiveTexture;
    private static Texture2D primaryTexture;
    private static Texture2D primaryHoverTexture;
    private static Texture2D primaryActiveTexture;
    private static Texture2D dangerTexture;
    private static Texture2D dangerHoverTexture;
    private static Texture2D dangerActiveTexture;

    private static GUIStyle panelStyle;
    private static GUIStyle ghostStyle;
    private static GUIStyle secondaryButton;
    private static GUIStyle primaryButton;
    private static GUIStyle dangerButton;
    private static GUIStyle compactButton;
    private static GUIStyle bigTitle;
    private static GUIStyle sectionTitle;
    private static GUIStyle sectionSubtitle;
    private static GUIStyle menuSubtitle;
    private static GUIStyle seasonLine;
    private static GUIStyle bodyLeft;
    private static GUIStyle bodyCenter;
    private static GUIStyle bodyRight;
    private static GUIStyle smallCenter;
    private static GUIStyle hintCenter;
    private static GUIStyle standings;
    private static GUIStyle scoreLeft;
    private static GUIStyle scoreRight;
    private static GUIStyle teamLeft;
    private static GUIStyle teamRight;
    private static GUIStyle timerText;
    private static GUIStyle stripScore;
    private static GUIStyle bannerText;
    private static GUIStyle stunLabel;
    private static GUIStyle clockLabel;
    private static GUIStyle hudSmall;
    private static GUIStyle hudSmallRight;
    private static GUIStyle hudTiny;
    private static GUIStyle hudTinyRight;
    private static GUIStyle hudTinyCenter;
    private static GUIStyle rosterName;
    private static GUIStyle rosterValue;
    private static GUIStyle barLabel;
    private static GUIStyle barValue;
    private static GUIStyle camLabel;
    private static GUIStyle statsTitle;
    private static GUIStyle statsHeader;
    private static GUIStyle statsHeaderLeft;
    private static GUIStyle statsName;
    private static GUIStyle statsValue;

    // Banner edge timers so GOAL / SAVED / MULTIPLIER flashes stay readable for a beat
    // even when the host only sets the flag for a single frame.
    private static bool lastGoalieSaved;
    private static int lastMultiplier = 1;
    private static float savedFlashUntil;
    private static float multiplierFlashUntil;

    public static UiAction Draw(UiModel model)
    {
        UiModel target = model;
        if (target == null)
        {
            if (fallbackModel == null)
            {
                fallbackModel = new UiModel();
            }
            target = fallbackModel;
        }
        if (target.settings == null)
        {
            target.settings = new GameSettings();
        }
        if (target.hud == null)
        {
            target.hud = new HudSnapshot();
        }
        if (target.leagueLines == null)
        {
            target.leagueLines = EmptyLines;
        }

        // Same short-edge scale as before; the lower guard is wide enough that a window
        // resized far below 720p keeps the whole 720-reference HUD on screen.
        scale = Mathf.Clamp(Screen.height / ReferenceHeight, 0.25f, 3f);
        EnsureStyles();

        switch (target.screen)
        {
            case GameScreen.Settings:
                return DrawSettings(target);
            case GameScreen.League:
                return DrawLeague(target);
            case GameScreen.Match:
                DrawMatchHud(target);
                return UiAction.None;
            case GameScreen.Paused:
                return DrawPaused(target);
            case GameScreen.Result:
                return DrawResult(target);
            case GameScreen.MainMenu:
            default:
                return DrawMainMenu(target);
        }
    }

    // ---------------------------------------------------------------- screens

    private static UiAction DrawMainMenu(UiModel model)
    {
        HudSnapshot hud = model.hud;
        DrawBackdrop(0.97f);

        float titleWidth = SafeWidth(1060f);
        DrawTitlePlate(Centered(30f, titleWidth, 122f), Clean(model.title, "ULTRABLADE 2"));
        LabelTinted(Centered(152f, SafeWidth(1100f), 24f),
            Clean(model.subtitle, "TRANSNATIONAL HYPERBLADE LEAGUE").ToUpperInvariant(), menuSubtitle, Cold);

        string opponent = !string.IsNullOrEmpty(model.nextOpponent)
            ? model.nextOpponent.ToUpperInvariant()
            : Clean(hud.aiTeam, "VOID").ToUpperInvariant();
        DrawMatchupStrip(Centered(186f, Mathf.Min(820f, ViewWidth - 72f), 52f),
            Clean(hud.playerTeam, "WAYFARERS").ToUpperInvariant(), Opaque(hud.playerColor), -1,
            opponent, Opaque(hud.aiColor), -1,
            "NEXT MATCH   /   " + Clean(hud.arenaName, "NEON RINK").ToUpperInvariant());

        int played = Mathf.Max(0, model.seasonWins) + Mathf.Max(0, model.seasonLosses);
        if (played > 0)
        {
            string record = "SEASON RECORD   W" + Mathf.Max(0, model.seasonWins) +
                            "   L" + Mathf.Max(0, model.seasonLosses) +
                            "   /   " + Mathf.Max(0, model.seasonPoints) + " PTS";
            LabelTinted(Centered(250f, SafeWidth(1100f), 26f), record, seasonLine, Warn);
        }

        float buttonWidth = Mathf.Min(430f, ViewWidth - 120f);
        float y = 284f;
        if (Button(Centered(y, buttonWidth, 56f), "START MATCH", primaryButton))
        {
            return UiAction.StartMatch;
        }
        y += 66f;
        if (Button(Centered(y, buttonWidth, 56f), "LEAGUE", secondaryButton))
        {
            return UiAction.OpenLeague;
        }
        y += 66f;
        if (Button(Centered(y, buttonWidth, 56f), "SETTINGS", secondaryButton))
        {
            return UiAction.OpenSettings;
        }
        y += 66f;
        if (Button(Centered(y, buttonWidth, 56f), "QUIT", dangerButton))
        {
            return UiAction.QuitGame;
        }

        LabelTinted(Centered(552f, ViewWidth - 96f, 24f),
            model.touch ? "TOUCH CONTROLS ENABLED IN MATCH" : "KEYBOARD + MOUSE", hintCenter,
            model.touch ? Warn : Muted);
        LabelTinted(Centered(578f, ViewWidth - 96f, 24f),
            model.touch
                ? "DRAG THE LEFT SIDE TO MOVE   ATTACK / DASH ON THE RIGHT"
                : "ARROWS / WASD MOVE   S ATTACK   D / SHIFT SPEEDBURST   SPACE POWER SHOT   F QUICK SHOT   C PASS   E JUMP   1 / 2 SUB",
            hintCenter, Muted);
        LabelTinted(Centered(606f, ViewWidth - 96f, 22f),
            model.touch
                ? "BACK BUTTON PAUSES A MATCH   /   SEASON PROGRESS SAVES AUTOMATICALLY"
                : "ESC / P PAUSES A MATCH   /   SEASON PROGRESS SAVES AUTOMATICALLY",
            hintCenter, new Color(Muted.r, Muted.g, Muted.b, 0.62f));

        LabelTinted(Centered(668f, ViewWidth - 96f, 20f),
            "14 DROMES   /   3 PERIODS   /   4 DROMERS PER SIDE   /   ROK COMBAT",
            hintCenter, new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.55f));

        return UiAction.None;
    }

    private static UiAction DrawSettings(UiModel model)
    {
        GameSettings settings = model.settings;
        DrawBackdrop(0.975f);

        DrawSectionTitle(Centered(14f, SafeWidth(1000f), 54f), "SETTINGS", Ice);
        LabelTinted(Centered(70f, SafeWidth(1100f), 22f),
            "AUDIO LEVELS   /   MATCH DIFFICULTY   /   INPUT", sectionSubtitle, Muted);
        DrawMatchupStrip(Centered(94f, Mathf.Min(760f, ViewWidth - 96f), 42f),
            Clean(model.hud.playerTeam, "WAYFARERS").ToUpperInvariant(), Opaque(model.hud.playerColor), -1,
            Clean(model.hud.aiTeam, "VOID").ToUpperInvariant(), Opaque(model.hud.aiColor), -1,
            Clean(model.hud.arenaName, "NEON RINK").ToUpperInvariant());

        float width = Mathf.Min(880f, ViewWidth - 64f);
        float left = (ViewWidth - width) * 0.5f;
        Rect panel = R(left, 146f, width, 462f);
        DrawPlate(panel, NeonRed, Cold);

        float rowLeft = left + 34f;
        float rowWidth = width - 68f;
        float rowY = 170f;
        const float RowStep = 62f;

        settings.masterVolume = VolumeRow(rowLeft, rowY, rowWidth, "MASTER VOLUME", settings.masterVolume);
        rowY += RowStep;
        settings.musicVolume = VolumeRow(rowLeft, rowY, rowWidth, "MUSIC", settings.musicVolume);
        rowY += RowStep;
        settings.sfxVolume = VolumeRow(rowLeft, rowY, rowWidth, "SFX", settings.sfxVolume);
        rowY += RowStep;
        settings.difficulty = DifficultyRow(rowLeft, rowY, rowWidth, settings.difficulty);
        rowY += RowStep;
        settings.touchControls = ToggleRow(rowLeft, rowY, rowWidth, "TOUCH CONTROLS", settings.touchControls);
        rowY += RowStep;
        settings.fullscreen = ToggleRow(rowLeft, rowY, rowWidth, "FULLSCREEN", settings.fullscreen);
        settings.Clamp();

        if (Button(Centered(536f, 280f, 54f), "BACK", secondaryButton))
        {
            return UiAction.Back;
        }

        LabelTinted(Centered(616f, ViewWidth - 96f, 22f),
            "VOLUME STEPS +/- 10%   /   SETTINGS SAVE IMMEDIATELY", hintCenter, Muted);
        return UiAction.None;
    }

    private static UiAction DrawLeague(UiModel model)
    {
        HudSnapshot hud = model.hud;
        string[] lines = model.leagueLines == null ? EmptyLines : model.leagueLines;
        DrawBackdrop(0.97f);

        DrawSectionTitle(Centered(12f, SafeWidth(1000f), 52f), "LEAGUE", Ice);
        string record = "SEASON RECORD   W" + Mathf.Max(0, model.seasonWins) +
                        "   L" + Mathf.Max(0, model.seasonLosses) +
                        "   /   " + Mathf.Max(0, model.seasonPoints) + " PTS";
        LabelTinted(Centered(66f, SafeWidth(1100f), 26f), record, seasonLine, Warn);

        string next = string.IsNullOrEmpty(model.nextOpponent) ? "TBD" : model.nextOpponent.ToUpperInvariant();
        DrawMatchupStrip(Centered(96f, Mathf.Min(820f, ViewWidth - 72f), 46f),
            Clean(hud.playerTeam, "WAYFARERS").ToUpperInvariant(), Opaque(hud.playerColor), -1,
            next, Opaque(hud.aiColor), -1,
            "NEXT OPPONENT   /   " + Clean(hud.arenaName, "NEON RINK").ToUpperInvariant());

        float width = Mathf.Min(960f, ViewWidth - 56f);
        float left = (ViewWidth - width) * 0.5f;
        Rect panel = R(left, 154f, width, 322f);
        DrawPlate(panel, NeonRed, Cold);
        LabelTinted(R(left + 24f, 164f, width - 48f, 22f), "STANDINGS", hudSmall, NeonRed);

        int count = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrEmpty(lines[i]))
            {
                count++;
            }
        }

        if (count == 0)
        {
            LabelTinted(R(left + 26f, 262f, width - 52f, 32f), "NO STANDINGS YET - START A MATCH", bodyCenter, Muted);
        }
        else
        {
            const int MaxRows = 10;
            int visible = Mathf.Min(count, MaxRows);
            float rowHeight = Mathf.Clamp(270f / Mathf.Max(1, visible), 22f, 30f);
            float rowY = 194f;
            int row = 0;
            for (int i = 0; i < lines.Length && row < visible; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                string text = line;
                if (count > MaxRows && row == MaxRows - 1)
                {
                    text = "+" + (count - (MaxRows - 1)) + " MORE";
                }
                bool highlight = !string.IsNullOrEmpty(hud.playerTeam) &&
                                 line.IndexOf(hud.playerTeam, StringComparison.OrdinalIgnoreCase) >= 0;
                Rect rowRect = R(left + 16f, rowY, width - 32f, rowHeight - 2f);
                if (highlight)
                {
                    Fill(rowRect, solidTexture, new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.16f));
                    Fill(R(left + 16f, rowY, 3f, rowHeight - 2f), solidTexture, NeonRed);
                }
                else if ((row % 2) == 0)
                {
                    Fill(rowRect, solidTexture, new Color(1f, 1f, 1f, 0.03f));
                }
                LabelTinted(R(left + 28f, rowY, width - 56f, rowHeight - 2f), text, standings,
                    highlight ? Warn : Ink);
                rowY += rowHeight;
                row++;
            }
        }

        if (model.seasonComplete)
        {
            LabelTinted(Centered(488f, SafeWidth(1100f), 24f), "SEASON COMPLETE - START A NEW SEASON", smallCenter, Good);
        }
        else
        {
            LabelTinted(Centered(488f, SafeWidth(1100f), 24f), "NEXT: " + next, smallCenter, Ink);
        }

        if (Button(R(left, 528f, 260f, 56f), "BACK", secondaryButton))
        {
            return UiAction.Back;
        }
        if (model.seasonComplete)
        {
            if (Button(R(left + width - 380f, 528f, 380f, 56f), "NEW SEASON", primaryButton))
            {
                return UiAction.NewSeason;
            }
        }
        else if (Button(R(left + width - 380f, 528f, 380f, 56f), "START NEXT MATCH", primaryButton))
        {
            return UiAction.StartNextMatch;
        }

        LabelTinted(Centered(608f, ViewWidth - 96f, 22f),
            "OPPONENTS ROTATE THROUGH THE TABLE   /   WINS ADD POINTS", hintCenter, Muted);
        return UiAction.None;
    }

    private static UiAction DrawPaused(UiModel model)
    {
        HudSnapshot hud = model.hud;
        DrawDim(0.80f);
        float width = Mathf.Min(680f, ViewWidth - 64f);
        float left = (ViewWidth - width) * 0.5f;
        Rect panel = R(left, 48f, width, 566f);
        DrawPlate(panel, NeonRed, Cold);

        DrawSectionTitle(Centered(62f, width - 60f, 58f), "PAUSED", Ice);
        LabelTinted(Centered(122f, width - 60f, 22f), "MATCH SUSPENDED - DROME CLOCK FROZEN", sectionSubtitle, Muted);

        DrawMatchupStrip(R(left + 34f, 152f, width - 68f, 62f),
            Clean(hud.playerTeam, "WAYFARERS").ToUpperInvariant(), Opaque(hud.playerColor), Mathf.Max(0, hud.playerScore),
            Clean(hud.aiTeam, "VOID").ToUpperInvariant(), Opaque(hud.aiColor), Mathf.Max(0, hud.aiScore),
            Clean(hud.arenaName, "NEON RINK").ToUpperInvariant());

        LabelTinted(Centered(224f, width - 60f, 24f), PeriodLine(hud), smallCenter, Cold);
        LabelTinted(Centered(250f, width - 60f, 22f), "GOALS ARE WORTH 2 OR 4 POINTS", hintCenter, Muted);

        float buttonWidth = Mathf.Min(420f, width - 96f);
        float y = 288f;
        if (Button(Centered(y, buttonWidth, 54f), "RESUME", primaryButton))
        {
            return UiAction.Resume;
        }
        y += 64f;
        if (Button(Centered(y, buttonWidth, 54f), "RESTART MATCH", secondaryButton))
        {
            return UiAction.RestartMatch;
        }
        y += 64f;
        if (Button(Centered(y, buttonWidth, 54f), "SETTINGS", secondaryButton))
        {
            return UiAction.OpenSettings;
        }
        y += 64f;
        if (Button(Centered(y, buttonWidth, 54f), "QUIT TO MENU", dangerButton))
        {
            return UiAction.QuitToMenu;
        }

        LabelTinted(Centered(556f, width - 60f, 22f),
            model.touch ? "BACK BUTTON TO RESUME" : "ESC / P TO RESUME", hintCenter, Muted);
        return UiAction.None;
    }

    private static UiAction DrawResult(UiModel model)
    {
        HudSnapshot hud = model.hud;
        string outcome = ResultLabel(hud);
        Color tint = outcome == "WIN" ? Ice : outcome == "LOSE" ? Bad : Neutral;
        DrawBackdrop(0.975f);

        float width = Mathf.Min(1140f, ViewWidth - 48f);
        float left = (ViewWidth - width) * 0.5f;
        DrawPlate(R(left, 30f, width, 660f), NeonRed, Cold);

        DrawOutcomeTitle(R(left + 28f, 38f, width - 56f, 96f), outcome, tint);

        DrawMatchupStrip(R(left + 40f, 138f, width - 80f, 54f),
            Clean(hud.playerTeam, "WAYFARERS").ToUpperInvariant(), Opaque(hud.playerColor), Mathf.Max(0, hud.playerScore),
            Clean(hud.aiTeam, "VOID").ToUpperInvariant(), Opaque(hud.aiColor), Mathf.Max(0, hud.aiScore),
            "FINAL   /   " + Clean(hud.arenaName, "NEON RINK").ToUpperInvariant());

        string leagueLine = "SEASON   W" + Mathf.Max(0, model.seasonWins) +
                            "   L" + Mathf.Max(0, model.seasonLosses) +
                            "   /   " + Mathf.Max(0, model.seasonPoints) + " PTS";
        LabelTinted(Centered(198f, width - 80f, 22f), leagueLine, smallCenter, Ink);
        string nextLine = model.seasonComplete
            ? "SEASON COMPLETE"
            : (!string.IsNullOrEmpty(model.nextOpponent) ? "NEXT: " + model.nextOpponent.ToUpperInvariant() : "NO FURTHER FIXTURE");
        LabelTinted(Centered(220f, width - 80f, 20f), nextLine, hintCenter, model.seasonComplete ? Good : Muted);

        string[] stats = hud.statsLines == null ? EmptyLines : hud.statsLines;
        string[] goalie = hud.goalieStatsLines == null ? EmptyLines : hud.goalieStatsLines;
        float tableLeft = left + 32f;
        float tableWidth = width - 64f;
        float y = 246f;
        LabelTinted(R(tableLeft, y, 300f, 16f), "DROMER STATS", statsTitle, NeonRed);
        y += 18f;
        y = DrawStatsTable(tableLeft, y, tableWidth, stats, DromerColumns, "TEAM / DROMER", 10, 21f, 21f,
            "NO DROMER STATS RECORDED", Mathf.Clamp(hud.activePlayer, 0, 3), true);
        y += 6f;
        LabelTinted(R(tableLeft, y, 300f, 16f), "GOALIE", statsTitle, Cold);
        y += 16f;
        DrawStatsTable(tableLeft, y, tableWidth, goalie, GoalieColumns, "GOALIE", 2, 21f, 21f,
            "NO GOALIE STATS RECORDED", -1, false);

        float available = Mathf.Min(width - 96f, 780f);
        float buttonWidth = (available - 32f) / 3f;
        float buttonX = left + (width - available) * 0.5f;
        float buttonY = 590f;
        UiAction advance = model.seasonComplete ? UiAction.QuitToMenu : UiAction.StartNextMatch;
        if (Button(R(buttonX, buttonY, buttonWidth, 56f), "CONTINUE", primaryButton))
        {
            return advance;
        }
        if (Button(R(buttonX + buttonWidth + 16f, buttonY, buttonWidth, 56f), "REMATCH", secondaryButton))
        {
            return UiAction.RestartMatch;
        }
        if (Button(R(buttonX + (buttonWidth + 16f) * 2f, buttonY, buttonWidth, 56f), "MENU", secondaryButton))
        {
            return UiAction.QuitToMenu;
        }

        LabelTinted(Centered(656f, width - 60f, 20f),
            model.seasonComplete
                ? "CONTINUE RETURNS TO THE MENU"
                : "CONTINUE ADVANCES TO THE NEXT MATCH",
            hintCenter, Muted);
        return UiAction.None;
    }

    // -------------------------------------------------------------- match HUD

    private static void DrawMatchHud(UiModel model)
    {
        HudSnapshot hud = model.hud;
        float vw = ViewWidth;
        Color playerColor = Opaque(hud.playerColor);
        Color aiColor = Opaque(hud.aiColor);

        // Rising-edge flashes: the host may only hold these flags for one frame.
        if (hud.goalieSaved && !lastGoalieSaved)
        {
            savedFlashUntil = Time.unscaledTime + 1.5f;
        }
        lastGoalieSaved = hud.goalieSaved;
        if (hud.multiplier > lastMultiplier)
        {
            multiplierFlashUntil = Time.unscaledTime + 1.5f;
        }
        lastMultiplier = hud.multiplier;

        // Top and bottom scrims keep the HUD readable over a bright drome.
        Fill(new Rect(0f, 0f, Screen.width, 126f * scale), scrimDownTexture, new Color(0.01f, 0.012f, 0.016f, 0.80f));
        Fill(new Rect(0f, Screen.height - 210f * scale, Screen.width, 210f * scale), scrimUpTexture,
            new Color(0.01f, 0.012f, 0.016f, 0.66f));

        const float CamWidth = 188f;
        const float Edge = 14f;
        DrawCamFrame(R(Edge, Edge, CamWidth, 62f), "SHOT CAM", true, playerColor);
        DrawCamFrame(R(vw - Edge - CamWidth, Edge, CamWidth, 62f), "REAR VIEW", false, aiColor);

        float boardWidth = Mathf.Clamp(vw - 2f * (CamWidth + 40f), 340f, 620f);
        float boardX = (vw - boardWidth) * 0.5f;
        DrawScoreboard(boardX, 14f, boardWidth, 80f, hud, playerColor, aiColor);
        DrawStatusStrip(boardX, 96f, boardWidth, 20f, hud);
        DrawBanner(vw, hud);

        DrawRoster(Edge, 98f, 210f, hud, playerColor);
        DrawStunBadge(R(Edge, 282f, 210f, 24f), hud.playerStunned, "DROMER STUNNED");
        DrawStunBadge(R(vw - Edge - CamWidth, 84f, CamWidth, 22f), hud.aiStunned, "OPPONENT STUNNED");

        DrawSystems(Edge, 520f, 252f, 186f, hud, model.touch);
        DrawRadar(vw - Edge - 244f, 520f, 244f, 186f, hud, playerColor, aiColor);

        float feedWidth = Mathf.Max(220f, vw - 280f - 258f);
        DrawEventFeed(280f, 596f, feedWidth, 110f, hud);

        string hint = model.touch
            ? "TOUCH: DRAG LEFT TO MOVE   ATTACK / DASH ON THE RIGHT"
            : "ARROWS MOVE   S ATTACK   D / SHIFT SPEEDBURST   SPACE POWER SHOT   F QUICK   C PASS   E JUMP   1 / 2 SUB";
        float hintWidth = Mathf.Min(760f, Mathf.Max(240f, vw - 560f));
        LabelTinted(Centered(570f, hintWidth, 22f), hint, hintCenter, new Color(0.74f, 0.78f, 0.84f, 0.92f));
    }

    private static void DrawScoreboard(float x, float y, float width, float height, HudSnapshot hud,
        Color playerColor, Color aiColor)
    {
        DrawPlate(R(x, y, width, height), NeonRed, Cold);
        Fill(R(x, y, 6f, height), solidTexture, new Color(playerColor.r, playerColor.g, playerColor.b, 0.95f));
        Fill(R(x + width - 6f, y, 6f, height), solidTexture, new Color(aiColor.r, aiColor.g, aiColor.b, 0.95f));

        float clockWidth = Mathf.Clamp(width * 0.30f, 108f, 168f);
        float sideWidth = (width - clockWidth) * 0.5f - 14f;
        float leftX = x + 14f;
        float rightX = x + width - 14f - sideWidth;
        float clockX = x + (width - clockWidth) * 0.5f;

        LabelTinted(R(leftX, y + 6f, sideWidth, 18f), TrimText(Clean(hud.playerTeam, "WAYFARERS").ToUpperInvariant(), 18),
            teamLeft, Lighten(playerColor));
        LabelTinted(R(rightX, y + 6f, sideWidth, 18f), TrimText(Clean(hud.aiTeam, "VOID").ToUpperInvariant(), 18),
            teamRight, Lighten(aiColor));

        Color leftScore = hud.playerStunned ? Bad : playerColor;
        if (hud.collectFlash == 1)
        {
            leftScore = Color.Lerp(leftScore, Color.white, 0.5f);
        }
        Color rightScore = hud.aiStunned ? Bad : aiColor;
        if (hud.collectFlash == 2)
        {
            rightScore = Color.Lerp(rightScore, Color.white, 0.5f);
        }
        LabelTinted(R(leftX - 2f, y + 24f, sideWidth + 4f, 46f), Mathf.Max(0, hud.playerScore).ToString(),
            scoreLeft, leftScore);
        LabelTinted(R(rightX - 2f, y + 24f, sideWidth + 4f, 46f), Mathf.Max(0, hud.aiScore).ToString(),
            scoreRight, rightScore);

        // centre clock block
        Fill(R(clockX, y + 5f, clockWidth, height - 10f), solidTexture, new Color(0.02f, 0.022f, 0.028f, 0.86f));
        Fill(R(clockX, y + 5f, clockWidth, 2f), solidTexture, new Color(Cold.r, Cold.g, Cold.b, 0.85f));
        Fill(R(clockX, y + height - 7f, clockWidth, 2f), solidTexture, new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.55f));

        int totalPeriods = hud.totalPeriods > 0 ? hud.totalPeriods : 3;
        int period = hud.period > 0 ? hud.period : Mathf.Max(1, hud.round);
        period = Mathf.Clamp(period, 1, Mathf.Max(1, totalPeriods));
        LabelTinted(R(clockX, y + 8f, clockWidth, 14f), "PERIOD " + period + "/" + totalPeriods, clockLabel, Cold);

        float seconds = hud.periodTime > 0f ? hud.periodTime : hud.timeLeft;
        bool urgent = seconds > 0f && seconds <= 10f;
        LabelTinted(R(clockX, y + 20f, clockWidth, 46f), FormatTime(seconds), timerText, urgent ? Bad : Ice);

        // period pips: filled = played, hollow = remaining
        float pipSize = Mathf.Max(2f, 4f * scale);
        float pipGap = 6f;
        int shown = Mathf.Min(Mathf.Max(1, totalPeriods), 9);
        float pipTotal = shown * pipSize + (shown - 1) * pipGap * scale;
        float pipX = (clockX + clockWidth * 0.5f) * scale - pipTotal * 0.5f;
        float pipY = (y + 68f) * scale;
        for (int i = 0; i < shown; i++)
        {
            Color pipColor = i < period ? NeonRed : new Color(1f, 1f, 1f, 0.22f);
            Fill(new Rect(pipX + i * (pipSize + pipGap * scale), pipY, pipSize, pipSize), solidTexture, pipColor);
        }
    }

    private static void DrawStatusStrip(float x, float y, float width, float height, HudSnapshot hud)
    {
        Rect strip = R(x, y, width, height);
        Fill(strip, solidTexture, new Color(0.02f, 0.022f, 0.03f, 0.80f));
        Fill(new Rect(strip.x, strip.y, strip.width, Mathf.Max(1f, scale)), solidTexture,
            new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.55f));

        bool held = hud.playerHasRok || !string.IsNullOrEmpty(hud.rokCarrier);
        string carrier = string.IsNullOrEmpty(hud.rokCarrier) ? "LOOSE" : hud.rokCarrier.ToUpperInvariant();
        float marker = Mathf.Max(3f, 5f * scale);
        Fill(new Rect(strip.x + 8f * scale, strip.y + (strip.height - marker) * 0.5f, marker, marker),
            solidTexture, hud.playerHasRok ? Warn : SteelLight);
        const float ChipHalf = 46f;
        LabelTinted(R(x + 18f, y, width * 0.5f - 18f - ChipHalf, height), "ROK: " + TrimText(carrier, 18),
            hudSmall, hud.playerHasRok ? Warn : (held ? Ink : Muted));

        if (hud.multiplier >= 2)
        {
            float chipW = 84f;
            Rect chip = R(x + width * 0.5f - chipW * 0.5f, y + 2f, chipW, height - 4f);
            Fill(chip, solidTexture, new Color(Blood.r, Blood.g, Blood.b, 0.9f));
            Fill(new Rect(chip.x, chip.y, Mathf.Max(1f, 2f * scale), chip.height), solidTexture, NeonRed);
            LabelTinted(chip, "MULT x" + hud.multiplier, clockLabel, Warn);
        }

        string right = "ROK COMBAT   /   " + Clean(hud.arenaName, "NEON RINK").ToUpperInvariant();
        LabelTinted(R(x + width * 0.5f + ChipHalf, y, width * 0.5f - ChipHalf - 8f, height),
            TrimText(right, 40), hudSmallRight, new Color(Muted.r, Muted.g, Muted.b, 0.95f));
    }

    private static void DrawRoster(float x, float y, float width, HudSnapshot hud, Color playerColor)
    {
        string[] names = hud.rosterNames == null ? EmptyLines : hud.rosterNames;
        float[] health = hud.rosterHealth == null ? EmptyHealth : hud.rosterHealth;
        int rows = Mathf.Clamp(Mathf.Max(names.Length, health.Length), 0, 4);
        int drawRows = Mathf.Max(1, rows);
        const float HeaderHeight = 22f;
        const float RowHeight = 38f;
        float panelHeight = HeaderHeight + drawRows * RowHeight + 8f;
        DrawPlate(R(x, y, width, panelHeight), playerColor, Cold);

        LabelTinted(R(x + 10f, y + 4f, width - 60f, 16f),
            TrimText(Clean(hud.playerTeam, "WAYFARERS").ToUpperInvariant(), 18), hudSmall, Lighten(playerColor));
        LabelTinted(R(x + width - 52f, y + 4f, 42f, 16f), "HP", hudSmallRight, Muted);

        int activeIndex = Mathf.Clamp(hud.activePlayer, 0, Mathf.Max(0, drawRows - 1));
        float maxHealth = MaxHealth(health);
        float rowY = y + HeaderHeight;

        if (rows == 0)
        {
            LabelTinted(R(x + 10f, rowY, width - 20f, RowHeight),
                "NO ROSTER FEED", rosterName, new Color(Muted.r, Muted.g, Muted.b, 0.8f));
            return;
        }

        for (int i = 0; i < rows; i++)
        {
            bool controlled = i == activeIndex;
            bool bench = !controlled && rows > 2 && i > 1;
            bool hasHealth = i < health.Length;
            float fraction = hasHealth ? HealthFraction(health, i, maxHealth) : 1f;
            Color barColor = !hasHealth ? SteelLight : controlled ? Warn : (fraction < 0.34f ? Bad : Good);
            float alpha = bench ? 0.45f : 1f;

            if (controlled)
            {
                Fill(R(x + 3f, rowY, width - 6f, RowHeight - 2f), solidTexture,
                    new Color(Warn.r, Warn.g, Warn.b, 0.12f));
                Fill(R(x + 3f, rowY, 3f, RowHeight - 2f), solidTexture, Warn);
            }
            else if ((i % 2) == 1)
            {
                Fill(R(x + 3f, rowY, width - 6f, RowHeight - 2f), solidTexture, new Color(1f, 1f, 1f, 0.035f));
            }

            string name = (names.Length > i && !string.IsNullOrEmpty(names[i]))
                ? names[i].ToUpperInvariant()
                : "DROMER " + (i + 1);
            Color nameColor = controlled ? Warn : Alpha(Ink, alpha);
            LabelTinted(R(x + 10f, rowY + 1f, width - 66f, 17f), TrimText(name, 16), rosterName, nameColor);
            LabelTinted(R(x + width - 52f, rowY + 1f, 42f, 17f),
                hasHealth ? Mathf.RoundToInt(fraction * 100f) + "%" : "--", rosterValue,
                Alpha(controlled ? Warn : Muted, alpha));

            Rect bar = R(x + 10f, rowY + 21f, width - 20f, 7f);
            Fill(bar, solidTexture, new Color(1f, 1f, 1f, 0.10f));
            if (fraction > 0f)
            {
                Fill(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), solidTexture,
                    Alpha(barColor, alpha));
            }
            float notch = bar.width * 0.25f;
            for (int n = 1; n < 4; n++)
            {
                Fill(new Rect(bar.x + notch * n, bar.y, Mathf.Max(1f, scale), bar.height), solidTexture,
                    new Color(0.02f, 0.02f, 0.025f, 0.75f));
            }

            rowY += RowHeight;
        }
    }

    private static void DrawSystems(float x, float y, float width, float height, HudSnapshot hud, bool touch)
    {
        DrawPlate(R(x, y, width, height), Cold, NeonRed);
        LabelTinted(R(x + 10f, y + 6f, width - 20f, 14f), "DROMER SYSTEMS", hudTiny, Muted);

        float rowWidth = width - 20f;
        DrawBarRow(x + 10f, y + 28f, rowWidth, "SPEEDBURST", Safe01(hud.speedBurst), Cold, 14f);
        DrawBarRow(x + 10f, y + 66f, rowWidth, "SHOT POWER", Safe01(hud.shotPower), NeonRed, 14f);
        DrawBarRow(x + 10f, y + 104f, rowWidth, "ATTACK", ReadyFraction(hud.attackCooldown, 1.1f), Ice, 11f);
        DrawBarRow(x + 10f, y + 134f, rowWidth, "DASH", ReadyFraction(hud.dashCooldown, 2f), Warn, 11f);

        LabelTinted(R(x + 10f, y + 158f, width - 20f, 20f),
            touch ? "PAUSE BUTTON FREEZES THE DROME" : "ESC / P  PAUSE", hudTiny,
            new Color(Muted.r, Muted.g, Muted.b, 0.8f));
    }

    // A segmented HyperBlade-style meter: label, trough, fill, tick marks, value.
    private static void DrawBarRow(float x, float y, float width, string label, float fraction, Color color, float height)
    {
        fraction = Safe01(fraction);
        LabelTinted(R(x, y - 1f, 84f, 14f), label, barLabel, Muted);
        LabelTinted(R(x + width - 42f, y - 1f, 42f, 14f), Mathf.RoundToInt(fraction * 100f) + "%", barValue,
            fraction >= 0.999f ? Good : Ink);

        float barX = x + 86f;
        float barWidth = Mathf.Max(24f, width - 86f - 46f);
        Rect bar = R(barX, y + 1f, barWidth, height);
        Fill(bar, solidTexture, new Color(1f, 1f, 1f, 0.10f));
        Fill(new Rect(bar.x, bar.y, bar.width, Mathf.Max(1f, scale)), solidTexture, new Color(1f, 1f, 1f, 0.18f));
        if (fraction > 0f)
        {
            Fill(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), solidTexture,
                new Color(color.r, color.g, color.b, 0.92f));
            Fill(new Rect(bar.x + bar.width * fraction - Mathf.Max(1f, 2f * scale), bar.y,
                Mathf.Max(1f, 2f * scale), bar.height), solidTexture, Color.white);
        }
        float notch = bar.width * 0.25f;
        for (int i = 1; i < 4; i++)
        {
            Fill(new Rect(bar.x + notch * i, bar.y, Mathf.Max(1f, scale), bar.height), solidTexture,
                new Color(0.02f, 0.02f, 0.025f, 0.8f));
        }
    }

    private static void DrawRadar(float x, float y, float width, float height, HudSnapshot hud,
        Color playerColor, Color aiColor)
    {
        DrawPlate(R(x, y, width, height), NeonRed, Cold);
        LabelTinted(R(x + 10f, y + 5f, width - 20f, 14f), "DROME RADAR", hudTiny, NeonRed);
        // HudSnapshot carries no world positions: this is a formation schematic derived
        // from possession/side/health, not a live positional tracker. Say so.
        LabelTinted(R(x + 10f, y + 5f, width - 20f, 14f), "SCHEMATIC", hudTinyRight,
            new Color(Muted.r, Muted.g, Muted.b, 0.8f));

        float cx = (x + width * 0.5f) * scale;
        float cy = (y + height * 0.54f) * scale;
        float rx = width * 0.40f * scale;
        float ry = height * 0.34f * scale;

        Fill(new Rect(cx - rx, cy - ry, rx * 2f, ry * 2f), discTexture, new Color(0.16f, 0.05f, 0.06f, 0.92f));
        Fill(new Rect(cx - rx, cy - ry, rx * 2f, ry * 2f), ringTexture, new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.75f));

        // centre line and circle
        Fill(new Rect(cx - Mathf.Max(1f, scale) * 0.5f, cy - ry, Mathf.Max(1f, scale), ry * 2f), solidTexture,
            new Color(1f, 1f, 1f, 0.16f));
        float centreRx = rx * 0.20f;
        float centreRy = ry * 0.22f;
        Fill(new Rect(cx - centreRx, cy - centreRy, centreRx * 2f, centreRy * 2f), ringTexture,
            new Color(1f, 1f, 1f, 0.16f));

        // goals: attacked (north) is the red pointer, own goal is the cold pointer
        Fill(new Rect(cx - 12f * scale, cy - ry - 3f * scale, 24f * scale, 4f * scale), solidTexture, Ice);
        Fill(new Rect(cx - 12f * scale, cy + ry - scale, 24f * scale, 4f * scale), solidTexture, Cold);

        // possession drives the rok marker; shot charge pushes it at the target goal
        bool playerRok = hud.playerHasRok;
        bool aiRok = !playerRok && !string.IsNullOrEmpty(hud.rokCarrier);
        float advance = Safe01(hud.shotPower);
        float rokNy = playerRok ? (0.30f + 0.45f * advance) : (aiRok ? -0.34f : 0f);
        float rokNx = playerRok ? 0.16f : (aiRok ? -0.20f : 0f);

        float[] formationX = { -0.52f, 0.40f, -0.22f, 0.62f };
        float[] formationY = { 0.16f, 0.02f, -0.42f, -0.20f };
        int active = Mathf.Clamp(hud.activePlayer, 0, 3);
        for (int i = 0; i < 4; i++)
        {
            int slot = (active + i) % 4;
            float px = formationX[slot] * (playerRok ? 1f : 0.82f);
            float py = formationY[slot] * (playerRok ? 1f : 0.82f) + 0.12f;
            bool controlled = i == 0;
            Color dotColor = controlled ? Warn : Alpha(playerColor, 0.9f);
            Dot(cx + px * rx * 0.86f, cy - py * ry * 0.86f, (controlled ? 3.6f : 2.8f) * scale, dotColor);
        }
        for (int i = 0; i < 4; i++)
        {
            int slot = (active + i + 2) % 4;
            float px = -formationX[slot] * 0.92f;
            float py = -formationY[slot] * 0.86f - 0.10f;
            Dot(cx + px * rx * 0.86f, cy - py * ry * 0.86f, 2.8f * scale, Alpha(aiColor, 0.9f));
        }
        // goalies at the goal mouths
        Dot(cx, cy - ry * 0.82f, 2.6f * scale, Alpha(Ice, 0.75f));
        Dot(cx, cy + ry * 0.82f, 2.6f * scale, Alpha(playerColor, 0.75f));

        // rok: white core with a pulse ring, labelled where the eye lands first
        float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.6f));
        float rokX = cx + rokNx * rx * 0.86f;
        float rokY = cy - rokNy * ry * 0.86f;
        Dot(rokX, rokY, 7.5f * scale, new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.35f * pulse));
        Dot(rokX, rokY, 4.2f * scale, new Color(1f, 1f, 1f, 0.95f));

        // goal pointer: chunky low-poly chevron over the attacked goal
        Triangle(cx, cy - ry - 12f * scale, cy - ry - 5f * scale, 5f * scale, NeonRed);
        LabelTinted(R(x + width - 44f, y + height - 20f, 36f, 14f), "ATK", hudTinyRight, NeonRed);
    }

    private static void DrawEventFeed(float x, float y, float width, float height, HudSnapshot hud)
    {
        DrawPlate(R(x, y, width, height), NeonRed, Cold);
        LabelTinted(R(x + 10f, y + 5f, width - 20f, 14f), "EVENT FEED", hudTiny, NeonRed);
        LabelTinted(R(x + 10f, y + 5f, width - 20f, 14f), "LAST 3", hudTinyRight, Muted);

        string[] feed = hud.eventFeed == null ? EmptyLines : hud.eventFeed;
        int start = Mathf.Max(0, feed.Length - 3);
        int drawn = 0;
        for (int i = start; i < feed.Length; i++)
        {
            string line = feed[i];
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            bool newest = i == feed.Length - 1;
            Color color = newest ? Ice : new Color(Muted.r, Muted.g, Muted.b, 0.85f);
            Fill(R(x + 12f, y + 28f + drawn * 26f, 3f, 12f), solidTexture,
                newest ? NeonRed : new Color(Muted.r, Muted.g, Muted.b, 0.6f));
            LabelTinted(R(x + 20f, y + 22f + drawn * 26f, width - 32f, 22f),
                TrimText(line.ToUpperInvariant(), 78), rosterName, color);
            drawn++;
        }
        if (drawn == 0)
        {
            LabelTinted(R(x + 12f, y + 24f, width - 24f, 22f), "NO MATCH EVENTS", rosterName, Muted);
        }
    }

    private static void DrawBanner(float vw, HudSnapshot hud)
    {
        string raw = hud.banner == null ? string.Empty : hud.banner.Trim();
        string text = string.Empty;
        int kind = 0;

        if (raw.Length > 0)
        {
            // The host sends "GOAL! WAYFARERS +4", "SAVE!", "MULTIPLIER x2",
            // "MULTIPLIER NULLIFIED" and event banners; keep the detail, style by kind.
            string upper = raw.ToUpperInvariant();
            if (Contains(upper, "GOAL"))
            {
                text = upper == "GOAL" ? "GOAL!" : upper;
                kind = 1;
            }
            else if (Contains(upper, "SAVE"))
            {
                text = "SAVED";
                kind = 2;
            }
            else if (Contains(upper, "MULT") && !Contains(upper, "NULL"))
            {
                text = MultiplierLabel(hud.multiplier);
                kind = 3;
            }
            else
            {
                text = upper;
                kind = 4;
            }
            text = TrimText(text, 42);
        }
        else if (hud.goalieSaved && Time.unscaledTime < savedFlashUntil)
        {
            text = "SAVED";
            kind = 2;
        }
        else if (hud.multiplier >= 2 && Time.unscaledTime < multiplierFlashUntil)
        {
            text = MultiplierLabel(hud.multiplier);
            kind = 3;
        }
        else if (hud.kickoff)
        {
            text = "KICKOFF";
            kind = 4;
        }

        if (text.Length == 0)
        {
            return;
        }

        Color edge = kind == 1 ? NeonRed : kind == 2 ? Cold : kind == 3 ? Warn : SteelLight;
        Color textColor = kind == 3 ? Warn : Ice;
        float width = Mathf.Min(680f, vw - 80f);
        Rect rect = R((vw - width) * 0.5f, 124f, width, 58f);

        Fill(rect, solidTexture, new Color(0.02f, 0.02f, 0.028f, 0.86f));
        Fill(new Rect(rect.x, rect.y, rect.width, Mathf.Max(2f, 3f * scale)), solidTexture, edge);
        Fill(new Rect(rect.x, rect.yMax - Mathf.Max(2f, 3f * scale), rect.width, Mathf.Max(2f, 3f * scale)),
            solidTexture, new Color(edge.r, edge.g, edge.b, 0.5f));
        if (kind == 1)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, rect.height), solidTexture,
                new Color(Blood.r, Blood.g, Blood.b, 0.35f));
        }

        // angled HyperBlade end caps
        FillSlanted(new Rect(rect.x + 10f * scale, rect.y + 8f * scale, 34f * scale, 5f * scale),
            new Color(edge.r, edge.g, edge.b, 0.85f), -58f);
        FillSlanted(new Rect(rect.xMax - 44f * scale, rect.y + 8f * scale, 34f * scale, 5f * scale),
            new Color(edge.r, edge.g, edge.b, 0.85f), 58f);

        float offset = Mathf.Max(2f, 2.5f * scale);
        LabelTinted(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, bannerText,
            new Color(0f, 0f, 0f, 0.8f));
        LabelTinted(rect, text, bannerText, textColor);
    }

    private static void DrawCamFrame(Rect rect, string label, bool shotCam, Color accent)
    {
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }
        Fill(rect, solidTexture, new Color(0.02f, 0.022f, 0.028f, 0.88f));
        Fill(new Rect(rect.x, rect.y, rect.width, Mathf.Max(2f, 2f * scale)), solidTexture,
            new Color(accent.r, accent.g, accent.b, 0.85f));

        float pad = 4f * scale;
        Rect view = new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, rect.height - pad * 2f - 13f * scale);
        Fill(view, solidTexture, new Color(0.06f, 0.065f, 0.075f, 0.96f));
        float horizon = view.y + view.height * 0.42f;
        Fill(new Rect(view.x, horizon, view.width, view.height - (horizon - view.y)), solidTexture,
            new Color(0.11f, 0.115f, 0.13f, 1f));
        Fill(new Rect(view.x, horizon, view.width, Mathf.Max(1f, scale)), solidTexture,
            new Color(0.62f, 0.65f, 0.70f, 0.55f));
        for (int i = 1; i <= 3; i++)
        {
            float t = i / 4f;
            Fill(new Rect(view.x, horizon + (view.height - (horizon - view.y)) * t, view.width, Mathf.Max(1f, scale)),
                solidTexture, new Color(1f, 1f, 1f, 0.05f));
        }

        float bodyW = Mathf.Max(3f, 5f * scale);
        float bodyH = Mathf.Max(6f, 12f * scale);
        Fill(new Rect(view.x + view.width * 0.26f, horizon - bodyH, bodyW, bodyH), solidTexture,
            new Color(accent.r, accent.g, accent.b, 0.85f));
        Fill(new Rect(view.x + view.width * 0.64f, horizon - bodyH * 1.15f, bodyW, bodyH * 1.15f), solidTexture,
            new Color(0.78f, 0.80f, 0.84f, 0.85f));

        if (!shotCam)
        {
            // rear-view net: crossbar plus uprights
            float netTop = horizon - bodyH * 0.4f;
            Fill(new Rect(view.x + view.width * 0.42f, netTop, view.width * 0.2f, Mathf.Max(1f, scale)),
                solidTexture, new Color(0.85f, 0.88f, 0.92f, 0.55f));
            Fill(new Rect(view.x + view.width * 0.42f, netTop, Mathf.Max(1f, scale), bodyH * 0.4f), solidTexture,
                new Color(0.85f, 0.88f, 0.92f, 0.45f));
            Fill(new Rect(view.x + view.width * 0.62f, netTop, Mathf.Max(1f, scale), bodyH * 0.4f), solidTexture,
                new Color(0.85f, 0.88f, 0.92f, 0.45f));
            Fill(new Rect(view.x + view.width * 0.42f, horizon, view.width * 0.2f, bodyH * 0.06f), solidTexture,
                new Color(0f, 0f, 0f, 0.25f));
        }

        Rect plate = new Rect(rect.x + 3f * scale, rect.yMax - 13f * scale, rect.width - 6f * scale, 11f * scale);
        Fill(plate, solidTexture, new Color(0f, 0f, 0f, 0.85f));
        LabelTinted(plate, label, camLabel, shotCam ? Ice : Lighten(accent));
        if (shotCam)
        {
            float blink = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.2f));
            Dot(rect.xMax - 11f * scale, rect.y + 10f * scale, 3.4f * scale,
                new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.4f + 0.6f * blink));
        }

        float tick = 10f * scale;
        Color tickColor = new Color(1f, 1f, 1f, 0.55f);
        Fill(new Rect(rect.x, rect.y, tick, Mathf.Max(1f, scale)), solidTexture, tickColor);
        Fill(new Rect(rect.xMax - tick, rect.yMax - Mathf.Max(1f, scale), tick, Mathf.Max(1f, scale)),
            solidTexture, tickColor);
    }

    // ----------------------------------------------------------- shared boards

    private static void DrawMatchupStrip(Rect rect, string home, Color homeColor, int homeScore,
        string away, Color awayColor, int awayScore, string detail)
    {
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }
        DrawPlate(rect, homeColor, awayColor);

        float chip = Mathf.Max(3f, 5f * scale);
        Fill(new Rect(rect.x + 2f * scale, rect.y, chip, rect.height), solidTexture, homeColor);
        Fill(new Rect(rect.xMax - 2f * scale - chip, rect.y, chip, rect.height), solidTexture, awayColor);

        float centreWidth = Mathf.Min(120f * scale, rect.width * 0.26f);
        float sideWidth = (rect.width - centreWidth) * 0.5f - 12f * scale;
        float nameY = rect.y + 5f * scale;
        float nameHeight = 20f * scale;

        LabelTinted(new Rect(rect.x + chip + 12f * scale, nameY, sideWidth - chip, nameHeight),
            TrimText(home, 20), teamLeft, Lighten(homeColor));
        LabelTinted(new Rect(rect.x + rect.width - chip - 12f * scale - sideWidth, nameY, sideWidth - chip, nameHeight),
            TrimText(away, 20), teamRight, Lighten(awayColor));

        Rect centre = new Rect(rect.x + (rect.width - centreWidth) * 0.5f, rect.y + 4f * scale,
            centreWidth, rect.height - (rect.height > 46f * scale ? 24f * scale : 8f * scale));
        Fill(centre, solidTexture, new Color(0.02f, 0.02f, 0.028f, 0.85f));
        Fill(new Rect(centre.x, centre.y, centre.width, Mathf.Max(1f, scale)), solidTexture,
            new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.7f));
        string centreText = homeScore >= 0 && awayScore >= 0
            ? homeScore + " : " + awayScore
            : "VS";
        LabelTinted(centre, centreText, homeScore >= 0 ? stripScore : clockLabel,
            homeScore >= 0 ? Ice : Muted);

        if (!string.IsNullOrEmpty(detail))
        {
            LabelTinted(new Rect(rect.x + 10f * scale, rect.yMax - 17f * scale, rect.width - 20f * scale, 14f * scale),
                TrimText(detail, 54), hudTinyCenter, new Color(Muted.r, Muted.g, Muted.b, 0.95f));
        }
    }

    // Box-score table. MatchStats emits labelled rows ("EVANS  PTS 12  FG 4/7 (57%)
    // POSS 0:34  INT 2  MP 1  INJ 0  FAT 0") with a team-total line before each roster.
    // Plain positional rows ("EVANS 12 4 7 57% ...") are accepted as a fallback.
    private static float DrawStatsTable(float x, float y, float width, string[] lines, string[] columns,
        string nameHeader, int maxRows, float rowHeight, float headerHeight, string emptyText,
        int highlightSkater, bool showTeamTotals)
    {
        if (width <= 0f || rowHeight <= 0f)
        {
            return y;
        }
        int columnCount = columns == null ? 0 : columns.Length;
        float nameWidth = width * (columnCount > 6 ? 0.24f : 0.34f);
        float columnWidth = columnCount > 0 ? (width - nameWidth) / columnCount : width;

        Rect header = R(x, y, width, headerHeight);
        Fill(header, solidTexture, new Color(0.02f, 0.022f, 0.03f, 0.85f));
        Fill(new Rect(header.x, header.yMax - Mathf.Max(1f, scale), header.width, Mathf.Max(1f, scale)),
            solidTexture, new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.75f));
        LabelTinted(R(x + 6f, y, nameWidth - 8f, headerHeight), nameHeader, statsHeaderLeft, NeonRed);
        for (int c = 0; c < columnCount; c++)
        {
            LabelTinted(R(x + nameWidth + columnWidth * c, y, columnWidth - 8f, headerHeight),
                columns[c], statsHeader, Muted);
        }

        float rowY = y + headerHeight;
        int drawn = 0;
        int skaterRow = 0;
        for (int i = 0; i < lines.Length && drawn < maxRows; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            string[] tokens = Tokenize(line);
            if (tokens.Length == 0 || IsStatsHeaderLine(tokens))
            {
                continue;
            }
            StatRow row = ParseStatRow(tokens);
            if (row.name.Length == 0)
            {
                continue;
            }
            bool total = showTeamTotals && row.labelled && !RowHas(row, "INT") && !RowHas(row, "MP");
            bool highlight = !total && highlightSkater >= 0 && skaterRow == highlightSkater;
            if (total)
            {
                Fill(R(x, rowY, width, rowHeight), solidTexture, new Color(Cold.r, Cold.g, Cold.b, 0.10f));
            }
            else if (highlight)
            {
                Fill(R(x, rowY, width, rowHeight), solidTexture, new Color(Warn.r, Warn.g, Warn.b, 0.12f));
                Fill(R(x, rowY, 3f, rowHeight), solidTexture, Warn);
            }
            else if ((drawn % 2) == 1)
            {
                Fill(R(x, rowY, width, rowHeight), solidTexture, new Color(1f, 1f, 1f, 0.035f));
            }

            LabelTinted(R(x + 6f, rowY, nameWidth - 8f, rowHeight), TrimText(row.name, 24), statsName,
                total ? Cold : (highlight ? Warn : Ink));
            for (int c = 0; c < columnCount; c++)
            {
                LabelTinted(R(x + nameWidth + columnWidth * c, rowY, columnWidth - 8f, rowHeight),
                    TrimText(ResolveStatColumn(row, columns[c], c), 8), statsValue, c == 0 ? Ice : Ink);
            }
            rowY += rowHeight;
            drawn++;
            if (!total)
            {
                skaterRow++;
            }
        }
        if (drawn == 0)
        {
            LabelTinted(R(x + 6f, rowY, width - 12f, rowHeight), emptyText, bodyLeft,
                new Color(Muted.r, Muted.g, Muted.b, 0.85f));
            rowY += rowHeight;
        }
        return rowY;
    }

    // One parsed box-score row: labelled key/value fields, or bare positional values.
    private sealed class StatRow
    {
        public string name = string.Empty;
        public bool labelled;
        public string[] keys = new string[MaxStatKeys];
        public string[] values = new string[MaxStatKeys];
        public int count;
        public string[] positional = new string[MaxStatKeys];
        public int positionalCount;
    }

    private static StatRow ParseStatRow(string[] tokens)
    {
        var row = new StatRow();
        if (tokens == null || tokens.Length == 0)
        {
            return row;
        }

        int firstKey = -1;
        for (int i = 0; i < tokens.Length; i++)
        {
            if (IsStatKey(tokens[i]))
            {
                firstKey = i;
                break;
            }
        }

        if (firstKey < 0)
        {
            row.name = tokens[0];
            for (int i = 1; i < tokens.Length && row.positionalCount < MaxStatKeys; i++)
            {
                row.positional[row.positionalCount++] = tokens[i];
            }
            return row;
        }

        row.labelled = true;
        for (int i = 0; i < firstKey; i++)
        {
            if (row.name.Length > 0)
            {
                row.name += " ";
            }
            row.name += tokens[i];
        }

        int index = firstKey;
        while (index < tokens.Length && row.count < MaxStatKeys)
        {
            string key = tokens[index].ToUpperInvariant();
            index++;
            string value = index < tokens.Length ? tokens[index] : string.Empty;
            index++;
            // "FG 4/7 (57%)": keep the percentage with the attempts.
            if (index < tokens.Length && tokens[index].Length > 0 && tokens[index][0] == '(')
            {
                value = value + " " + tokens[index];
                index++;
            }
            row.keys[row.count] = key;
            row.values[row.count] = value;
            row.count++;
        }
        return row;
    }

    private static bool IsStatKey(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 4)
        {
            return false;
        }
        switch (token.ToUpperInvariant())
        {
            case "PTS":
            case "FG":
            case "FGM":
            case "FGA":
            case "FG%":
            case "EFG":
            case "2X":
            case "4X":
            case "POSS":
            case "INT":
            case "MP":
            case "MULT":
            case "INJ":
            case "FAT":
            case "SA":
            case "SV":
            case "SV%":
            case "SHT":
            case "SAV":
            case "ERR":
                return true;
            default:
                return false;
        }
    }

    private static string RowValue(StatRow row, string key)
    {
        if (row == null)
        {
            return string.Empty;
        }
        for (int i = 0; i < row.count; i++)
        {
            if (row.keys[i] == key)
            {
                return row.values[i];
            }
        }
        return string.Empty;
    }

    private static bool RowHas(StatRow row, string key)
    {
        return RowValue(row, key).Length > 0;
    }

    private static string FirstValue(StatRow row, string first, string second)
    {
        string value = RowValue(row, first);
        return value.Length > 0 ? value : RowValue(row, second);
    }

    private static string ResolveStatColumn(StatRow row, string column, int columnIndex)
    {
        if (row == null)
        {
            return "-";
        }
        if (!row.labelled)
        {
            return columnIndex < row.positionalCount ? OrDash(row.positional[columnIndex]) : "-";
        }
        switch (column)
        {
            case "PTS":
                return OrDash(RowValue(row, "PTS"));
            case "FGM":
                return FgPart(row, 0);
            case "FGA":
                return FgPart(row, 1);
            case "FG%":
                return FgPart(row, 2);
            case "2X":
                return TwoOrFour(row, true);
            case "4X":
                return TwoOrFour(row, false);
            case "POSS":
                return OrDash(RowValue(row, "POSS"));
            case "INT":
                return OrDash(RowValue(row, "INT"));
            case "MULT":
                return OrDash(FirstValue(row, "MP", "MULT"));
            case "INJ":
                return OrDash(RowValue(row, "INJ"));
            case "FAT":
                return OrDash(RowValue(row, "FAT"));
            case "SHT":
                return OrDash(FirstValue(row, "SA", "SHT"));
            case "SAV":
                return OrDash(FirstValue(row, "SV", "SAV"));
            case "ERR":
                return OrDash(RowValue(row, "ERR"));
            default:
                return OrDash(RowValue(row, column));
        }
    }

    private static string FgPart(StatRow row, int part)
    {
        string raw = RowValue(row, "FG");
        if (raw.Length == 0)
        {
            if (part == 0)
            {
                return OrDash(RowValue(row, "FGM"));
            }
            if (part == 1)
            {
                return OrDash(RowValue(row, "FGA"));
            }
            return OrDash(RowValue(row, "FG%"));
        }

        string made = raw;
        string attempts = string.Empty;
        string percent = string.Empty;
        int slash = raw.IndexOf('/');
        if (slash >= 0)
        {
            made = raw.Substring(0, slash);
            attempts = raw.Substring(slash + 1);
        }
        int space = attempts.IndexOf(' ');
        if (space >= 0)
        {
            percent = attempts.Substring(space + 1);
            attempts = attempts.Substring(0, space);
        }

        if (part == 0)
        {
            return OrDash(made.Trim());
        }
        if (part == 1)
        {
            return OrDash(attempts.Trim());
        }
        return OrDash(percent.Trim('(', ')', ' '));
    }

    // Goals in HyperBlade are worth 2 or 4, so the 2x/4x split follows from points and makes
    // when the feed does not carry explicit 2X/4X fields.
    private static string TwoOrFour(StatRow row, bool twoPoint)
    {
        string explicitValue = RowValue(row, twoPoint ? "2X" : "4X");
        if (explicitValue.Length > 0)
        {
            return explicitValue;
        }

        int points;
        int made;
        if (!int.TryParse(RowValue(row, "PTS"), out points) || !int.TryParse(FgPart(row, 0), out made))
        {
            return "-";
        }
        int extra = points - made * 2;
        if (extra < 0 || (extra % 2) != 0)
        {
            return "-";
        }
        int fours = extra / 2;
        int twos = made - fours;
        if (fours < 0 || twos < 0)
        {
            return "-";
        }
        return (twoPoint ? twos : fours).ToString();
    }

    private static string OrDash(string value)
    {
        return string.IsNullOrEmpty(value) ? "-" : value;
    }

    // ------------------------------------------------------------- primitives

    private static float ViewWidth
    {
        get { return Screen.width / Mathf.Max(0.0001f, scale); }
    }

    private static float SafeWidth(float desired)
    {
        return Mathf.Min(desired, Mathf.Max(120f, ViewWidth - 64f));
    }

    private static Rect R(float x, float y, float width, float height)
    {
        return new Rect(x * scale, y * scale, width * scale, height * scale);
    }

    private static Rect Centered(float y, float width, float height)
    {
        return R((ViewWidth - width) * 0.5f, y, width, height);
    }

    private static void Fill(Rect rect, Texture2D texture, Color color)
    {
        if (texture == null || rect.width <= 0f || rect.height <= 0f || color.a <= 0f)
        {
            return;
        }
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        GUI.color = previous;
    }

    private static void FillTiled(Rect rect, Texture2D texture, Color color, float tilePixels)
    {
        if (texture == null || rect.width <= 0f || rect.height <= 0f || color.a <= 0f)
        {
            return;
        }
        if (tilePixels <= 0f)
        {
            tilePixels = 128f;
        }
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTextureWithTexCoords(rect, texture,
            new Rect(0f, 0f, rect.width / tilePixels, rect.height / tilePixels));
        GUI.color = previous;
    }

    private static void FillSlanted(Rect rect, Color color, float angle)
    {
        if (solidTexture == null || rect.width <= 0f || rect.height <= 0f || color.a <= 0f)
        {
            return;
        }
        Matrix4x4 previous = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, new Vector2(rect.center.x, rect.center.y));
        Fill(rect, solidTexture, color);
        GUI.matrix = previous;
    }

    private static void Dot(float cx, float cy, float radius, Color color)
    {
        if (dotTexture == null || radius <= 0f || color.a <= 0f)
        {
            return;
        }
        Fill(new Rect(cx - radius, cy - radius, radius * 2f, radius * 2f), dotTexture, color);
    }

    private static void Triangle(float cx, float tipY, float baseY, float halfWidth, Color color)
    {
        if (solidTexture == null || halfWidth <= 0f || color.a <= 0f)
        {
            return;
        }
        const int Steps = 6;
        float step = (baseY - tipY) / Steps;
        for (int i = 0; i < Steps; i++)
        {
            float t = (i + 1f) / Steps;
            float width = halfWidth * t;
            float y = tipY + step * i;
            Fill(new Rect(cx - width, y, width * 2f, Mathf.Abs(step) + 0.6f), solidTexture, color);
        }
    }

    private static void LabelTinted(Rect rect, string text, GUIStyle style, Color color)
    {
        if (style == null || string.IsNullOrEmpty(text) || color.a <= 0f || rect.width <= 0f)
        {
            return;
        }
        Color previous = GUI.color;
        GUI.color = color;
        GUI.Label(rect, text, style);
        GUI.color = previous;
    }

    private static bool Button(Rect rect, string text, GUIStyle style)
    {
        if (style == null)
        {
            return false;
        }
        Color glow = style == dangerButton ? Bad : style == primaryButton ? NeonRed : Cold;
        Fill(new Rect(rect.x - scale, rect.y - scale, rect.width + 2f * scale, rect.height + 2f * scale),
            solidTexture, new Color(glow.r, glow.g, glow.b, style == secondaryButton ? 0.20f : 0.30f));
        // left notch keeps the primary action readable even without colour vision
        Color notch = style == dangerButton ? Bad : style == primaryButton ? NeonRed : SteelLight;
        Fill(new Rect(rect.x, rect.y, Mathf.Max(2f, 4f * scale), rect.height), solidTexture,
            new Color(notch.r, notch.g, notch.b, 0.95f));
        return GUI.Button(rect, text, style);
    }

    private static void DrawBackdrop(float alpha = 0.965f)
    {
        Fill(new Rect(0f, 0f, Screen.width, Screen.height), solidTexture,
            new Color(0.030f, 0.033f, 0.040f, alpha));
        FillTiled(new Rect(0f, 0f, Screen.width, Screen.height), brushTexture,
            new Color(1f, 1f, 1f, 0.30f), 170f * scale);
        Fill(new Rect(0f, 0f, Screen.width, 150f * scale), scrimDownTexture, new Color(0f, 0f, 0f, 0.55f));
        Fill(new Rect(0f, Screen.height - 170f * scale, Screen.width, 170f * scale), scrimUpTexture,
            new Color(0f, 0f, 0f, 0.50f));

        float thick = Mathf.Max(1f, 3f * scale);
        Fill(new Rect(0f, 0f, Screen.width, thick), solidTexture, new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.85f));
        Fill(new Rect(0f, thick + scale, Screen.width, Mathf.Max(1f, scale)), solidTexture,
            new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.28f));
        Fill(new Rect(0f, Screen.height - thick, Screen.width, thick), solidTexture,
            new Color(Cold.r, Cold.g, Cold.b, 0.65f));

        float railX = 22f * scale;
        float railThickness = Mathf.Max(1f, scale);
        Fill(new Rect(railX, 34f * scale, railThickness, Screen.height - 68f * scale), solidTexture,
            new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.16f));
        Fill(new Rect(Screen.width - railX - railThickness, 34f * scale, railThickness,
            Screen.height - 68f * scale), solidTexture, new Color(Cold.r, Cold.g, Cold.b, 0.16f));

        float seamStep = 96f * scale;
        for (float y = seamStep; y < Screen.height - 30f * scale; y += seamStep)
        {
            Fill(new Rect(0f, y, Screen.width, railThickness), solidTexture, new Color(1f, 1f, 1f, 0.022f));
        }
        for (int i = 0; i < 2; i++)
        {
            Fill(new Rect(0f, Screen.height * (i == 0 ? 0.22f : 0.78f), Screen.width, 6f * scale),
                scrimDownTexture, new Color(0f, 0f, 0f, 0.30f));
        }
    }

    private static void DrawDim(float alpha)
    {
        Fill(new Rect(0f, 0f, Screen.width, Screen.height), solidTexture,
            new Color(0.008f, 0.010f, 0.014f, alpha));
    }

    private static void DrawPlate(Rect rect, Color topRule, Color bottomRule)
    {
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }
        GUI.Box(rect, GUIContent.none, panelStyle);
        FillTiled(rect, brushTexture, new Color(1f, 1f, 1f, 0.16f), 140f * scale);

        float edge = Mathf.Max(1f, 2f * scale);
        float hair = Mathf.Max(1f, scale);
        Fill(new Rect(rect.x, rect.y, rect.width, edge), solidTexture,
            new Color(topRule.r, topRule.g, topRule.b, 0.85f));
        Fill(new Rect(rect.x, rect.yMax - edge, rect.width, edge), solidTexture,
            new Color(bottomRule.r, bottomRule.g, bottomRule.b, 0.55f));
        Fill(new Rect(rect.x, rect.y, hair, rect.height), solidTexture, new Color(1f, 1f, 1f, 0.07f));
        Fill(new Rect(rect.xMax - hair, rect.y, hair, rect.height), solidTexture, new Color(1f, 1f, 1f, 0.07f));

        float tick = Mathf.Min(rect.width, 16f * scale);
        Fill(new Rect(rect.x, rect.y, tick, hair), solidTexture, new Color(1f, 1f, 1f, 0.55f));
        Fill(new Rect(rect.xMax - tick, rect.yMax - hair, tick, hair), solidTexture,
            new Color(1f, 1f, 1f, 0.55f));
    }

    private static void DrawSectionTitle(Rect rect, string text, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        FillSlanted(new Rect(rect.x + rect.width * 0.22f, rect.y + rect.height * 0.84f, rect.width * 0.56f,
            5f * scale), new Color(Blood.r, Blood.g, Blood.b, 0.9f), -1.4f);
        LabelTinted(rect, text, sectionTitle, color);
    }

    private static void DrawTitlePlate(Rect rect, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        FillSlanted(new Rect(rect.x + rect.width * 0.08f, rect.y + rect.height * 0.50f, rect.width * 0.84f,
            10f * scale), new Color(Blood.r, Blood.g, Blood.b, 0.92f), -2.2f);
        FillSlanted(new Rect(rect.x + rect.width * 0.16f, rect.y + rect.height * 0.82f, rect.width * 0.68f,
            3f * scale), new Color(Cold.r, Cold.g, Cold.b, 0.9f), -2.2f);
        DrawBigTitle(rect, text.ToUpperInvariant(), Ice);
    }

    private static void DrawOutcomeTitle(Rect rect, string text, Color color)
    {
        FillSlanted(new Rect(rect.x + rect.width * 0.18f, rect.y + rect.height * 0.86f, rect.width * 0.64f,
            6f * scale), new Color(Blood.r, Blood.g, Blood.b, 0.9f), -1.6f);
        DrawBigTitle(rect, text, color);
    }

    private static void DrawBigTitle(Rect rect, string text, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        float offset = Mathf.Max(2f, 3f * scale);
        LabelTinted(new Rect(rect.x - offset, rect.y - offset, rect.width, rect.height), text, bigTitle,
            new Color(0.02f, 0.02f, 0.03f, 0.9f));
        LabelTinted(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, bigTitle,
            new Color(Blood.r, Blood.g, Blood.b, 0.85f));
        LabelTinted(new Rect(rect.x + offset, rect.y, rect.width, rect.height), text, bigTitle,
            new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.30f));
        LabelTinted(rect, text, bigTitle, color);
    }

    private static void DrawStunBadge(Rect rect, bool stunned, string label)
    {
        if (!stunned || rect.width <= 0f)
        {
            return;
        }
        Fill(rect, solidTexture, new Color(Bad.r, Bad.g, Bad.b, 0.20f));
        float edge = Mathf.Max(1f, 2f * scale);
        Fill(new Rect(rect.x, rect.y, rect.width, edge), solidTexture, Bad);
        Fill(new Rect(rect.x, rect.yMax - edge, rect.width, edge), solidTexture,
            new Color(Bad.r, Bad.g, Bad.b, 0.55f));
        LabelTinted(rect, label, stunLabel, new Color(1f, 0.86f, 0.86f));
    }

    // -------------------------------------------------------------- controls

    private static float VolumeRow(float x, float y, float width, string label, float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            value = 0.5f;
        }
        value = Mathf.Clamp01(value);
        LabelTinted(R(x, y, 176f, 56f), label, bodyLeft, Ink);

        float controlY = y + 2f;
        if (Button(R(x + 182f, controlY, 50f, 52f), "-", compactButton))
        {
            value = ClampVolume(value - 0.1f);
        }
        float plusX = x + width - 50f;
        if (Button(R(plusX, controlY, 50f, 52f), "+", compactButton))
        {
            value = ClampVolume(value + 0.1f);
        }
        float valueX = plusX - 86f;
        float trackX = x + 244f;
        float trackWidth = Mathf.Max(70f, valueX - 14f - trackX);
        Rect track = R(trackX, y + 24f, trackWidth, 8f);
        Fill(track, solidTexture, new Color(1f, 1f, 1f, 0.10f));
        if (value > 0f)
        {
            Fill(new Rect(track.x, track.y, track.width * value, track.height), solidTexture,
                new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.95f));
        }
        Rect knob = R(trackX + trackWidth * value - 3f, y + 19f, 6f, 18f);
        Fill(knob, solidTexture, Color.white);
        LabelTinted(R(valueX, y, 70f, 56f), Mathf.RoundToInt(value * 100f) + "%", bodyRight, Ink);

        Rect hitRect = R(trackX, y + 4f, trackWidth, 48f);
        bool hover = hitRect.Contains(Event.current.mousePosition);
        if (hover)
        {
            Fill(new Rect(track.x, track.y - 2f * scale, track.width, track.height + 4f * scale), solidTexture,
                new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.10f));
        }
        if (GUI.Button(hitRect, GUIContent.none, ghostStyle))
        {
            float mouseX = Event.current.mousePosition.x / scale;
            value = ClampVolume((mouseX - trackX) / trackWidth);
        }
        return value;
    }

    private static int DifficultyRow(float x, float y, float width, int difficulty)
    {
        LabelTinted(R(x, y, 260f, 56f), "DIFFICULTY", bodyLeft, Ink);
        float segmentWidth = Mathf.Min(150f, (width - 280f) / 3f);
        segmentWidth = Mathf.Max(80f, segmentWidth);
        float startX = x + width - (segmentWidth * 3f + 20f);
        for (int i = 0; i < DifficultyLabels.Length; i++)
        {
            Rect rect = R(startX + i * (segmentWidth + 10f), y + 2f, segmentWidth, 52f);
            if (Button(rect, DifficultyLabels[i], difficulty == i + 1 ? primaryButton : secondaryButton))
            {
                difficulty = i + 1;
            }
        }
        return Mathf.Clamp(difficulty, 1, 3);
    }

    private static bool ToggleRow(float x, float y, float width, string label, bool value)
    {
        LabelTinted(R(x, y, width - 210f, 56f), label, bodyLeft, Ink);
        Rect rect = R(x + width - 180f, y + 2f, 180f, 52f);
        string state = value ? "ON" : "OFF";
        if (Button(rect, state, value ? primaryButton : secondaryButton))
        {
            value = !value;
        }
        return value;
    }

    // -------------------------------------------------------------- utilities

    private static float ClampVolume(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return 0.5f;
        }
        return Mathf.Clamp01(Mathf.Round(value * 100f) / 100f);
    }

    private static float Safe01(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return 0f;
        }
        return Mathf.Clamp01(value);
    }

    private static string Clean(string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private static string TrimText(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || max <= 0)
        {
            return string.Empty;
        }
        return value.Length <= max ? value : value.Substring(0, max);
    }

    private static string FormatTime(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
        {
            seconds = 0f;
        }
        int total = Mathf.CeilToInt(seconds);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    private static string PeriodLine(HudSnapshot hud)
    {
        int totalPeriods = hud.totalPeriods > 0 ? hud.totalPeriods : 3;
        int period = hud.period > 0 ? hud.period : Mathf.Max(1, hud.round);
        period = Mathf.Clamp(period, 1, Mathf.Max(1, totalPeriods));
        float seconds = hud.periodTime > 0f ? hud.periodTime : hud.timeLeft;
        return "PERIOD " + period + "/" + totalPeriods + "   /   " + FormatTime(seconds) + " LEFT";
    }

    private static string MultiplierLabel(int multiplier)
    {
        return "MULTIPLIER x" + Mathf.Clamp(multiplier < 1 ? 2 : multiplier, 2, 9);
    }

    private static string ResultLabel(HudSnapshot hud)
    {
        string text = hud.resultText == null ? string.Empty : hud.resultText;
        if (Contains(text, "WIN") || Contains(text, "VICTORY"))
        {
            return "WIN";
        }
        if (Contains(text, "LOSE") || Contains(text, "LOSS") || Contains(text, "DEFEAT"))
        {
            return "LOSE";
        }
        if (Contains(text, "DRAW") || Contains(text, "TIE"))
        {
            return "DRAW";
        }
        if (hud.playerScore > hud.aiScore)
        {
            return "WIN";
        }
        if (hud.playerScore < hud.aiScore)
        {
            return "LOSE";
        }
        return "DRAW";
    }

    private static bool Contains(string text, string token)
    {
        return !string.IsNullOrEmpty(text) && text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static float MaxHealth(float[] health)
    {
        float max = 0f;
        if (health == null)
        {
            return max;
        }
        for (int i = 0; i < health.Length; i++)
        {
            float value = health[i];
            if (!float.IsNaN(value) && !float.IsInfinity(value) && value > max)
            {
                max = value;
            }
        }
        return max;
    }

    private static float HealthFraction(float[] health, int index, float maxValue)
    {
        if (health == null || index < 0 || index >= health.Length)
        {
            return 0f;
        }
        float value = health[index];
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
        {
            return 0f;
        }
        float denominator = maxValue;
        if (denominator <= 1.5f)
        {
            denominator = 1f;
        }
        else if (denominator <= 100f)
        {
            denominator = 100f;
        }
        return Safe01(value / denominator);
    }

    private static float ReadyFraction(float cooldown, float maximum)
    {
        if (float.IsNaN(cooldown) || float.IsInfinity(cooldown) || cooldown <= 0f)
        {
            return 1f;
        }
        return Mathf.Clamp01(1f - cooldown / Mathf.Max(0.01f, maximum));
    }

    private static string[] Tokenize(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return EmptyLines;
        }
        var tokens = new string[32];
        int count = 0;
        int index = 0;
        while (index < line.Length && count < tokens.Length)
        {
            while (index < line.Length && char.IsWhiteSpace(line[index]))
            {
                index++;
            }
            if (index >= line.Length)
            {
                break;
            }
            int start = index;
            while (index < line.Length && !char.IsWhiteSpace(line[index]))
            {
                index++;
            }
            tokens[count] = line.Substring(start, index - start);
            count++;
        }
        if (count == 0)
        {
            return EmptyLines;
        }
        if (count == tokens.Length)
        {
            return tokens;
        }
        var result = new string[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = tokens[i];
        }
        return result;
    }

    private static bool IsStatsHeaderLine(string[] tokens)
    {
        if (tokens == null || tokens.Length < 2)
        {
            return false;
        }
        return IsStatsHeader(tokens[0]) && (IsStatKey(tokens[1]) || IsStatsHeader(tokens[1]));
    }

    private static bool IsStatsHeader(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return true;
        }
        switch (token.ToUpperInvariant())
        {
            case "PTS":
            case "FGM":
            case "FGA":
            case "FG%":
            case "SHT":
            case "SAV":
            case "ERR":
            case "DROMER":
            case "GOALIE":
            case "PLAYER":
            case "NAME":
            case "TEAM":
            case "STAT":
            case "STATS":
                return true;
            default:
                return false;
        }
    }

    private static Color Opaque(Color color)
    {
        color.a = 1f;
        return color;
    }

    private static Color Alpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
    }

    private static Color Lighten(Color color)
    {
        return Color.Lerp(Opaque(color), Color.white, 0.28f);
    }

    // ------------------------------------------------------------- style setup

    private static Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static Texture2D BuildBrushTexture()
    {
        const int Size = 128;
        var random = new System.Random(1996);
        var columns = new float[Size];
        for (int x = 0; x < Size; x++)
        {
            float streak = 0.20f + 0.62f * (float)random.NextDouble();
            if (random.NextDouble() < 0.07)
            {
                streak = Mathf.Min(1f, streak + 0.3f);
            }
            if (random.NextDouble() < 0.05)
            {
                streak *= 0.45f;
            }
            columns[x] = streak;
        }
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var pixels = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            float row = 1f + ((float)random.NextDouble() - 0.5f) * 0.10f;
            for (int x = 0; x < Size; x++)
            {
                float value = Mathf.Clamp01(columns[x] * row);
                pixels[y * Size + x] = new Color(value, value, value * 1.03f, 1f);
            }
        }
        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static Texture2D BuildGradientTexture(bool opaqueAtTop)
    {
        const int Height = 64;
        var texture = new Texture2D(2, Height, TextureFormat.RGBA32, false);
        for (int y = 0; y < Height; y++)
        {
            float up = y / (float)(Height - 1);
            float alpha = opaqueAtTop ? up : 1f - up;
            alpha = alpha * alpha;
            var color = new Color(1f, 1f, 1f, alpha);
            texture.SetPixel(0, y, color);
            texture.SetPixel(1, y, color);
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static Texture2D BuildDotTexture()
    {
        const int Size = 32;
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = ((x + 0.5f) / Size) * 2f - 1f;
                float dy = ((y + 0.5f) / Size) * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static Texture2D BuildRingTexture()
    {
        const int Size = 128;
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = ((x + 0.5f) / Size) * 2f - 1f;
                float dy = ((y + 0.5f) / Size) * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.94f) / 0.045f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static Texture2D BuildDiscTexture()
    {
        const int Size = 96;
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = ((x + 0.5f) / Size) * 2f - 1f;
                float dy = ((y + 0.5f) / Size) * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((1f - distance) * 14f) * 0.95f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static bool EnsureTextures()
    {
        bool created = false;
        if (solidTexture == null)
        {
            solidTexture = Solid(Color.white);
            created = true;
        }
        if (panelTexture == null)
        {
            panelTexture = Solid(new Color(0.055f, 0.060f, 0.070f, 0.96f));
            brushTexture = BuildBrushTexture();
            scrimDownTexture = BuildGradientTexture(true);
            scrimUpTexture = BuildGradientTexture(false);
            dotTexture = BuildDotTexture();
            ringTexture = BuildRingTexture();
            discTexture = BuildDiscTexture();
            created = true;
        }
        if (buttonTexture == null)
        {
            buttonTexture = Solid(new Color(0.10f, 0.11f, 0.13f, 0.95f));
            buttonHoverTexture = Solid(new Color(0.18f, 0.20f, 0.24f, 0.98f));
            buttonActiveTexture = Solid(new Color(0.28f, 0.31f, 0.36f, 1f));
            created = true;
        }
        if (primaryTexture == null)
        {
            primaryTexture = Solid(new Color(0.36f, 0.04f, 0.05f, 0.97f));
            primaryHoverTexture = Solid(new Color(0.62f, 0.07f, 0.08f, 1f));
            primaryActiveTexture = Solid(new Color(0.82f, 0.10f, 0.10f, 1f));
            created = true;
        }
        if (dangerTexture == null)
        {
            dangerTexture = Solid(new Color(0.07f, 0.06f, 0.07f, 0.94f));
            dangerHoverTexture = Solid(new Color(0.20f, 0.05f, 0.06f, 0.98f));
            dangerActiveTexture = Solid(new Color(0.38f, 0.06f, 0.08f, 1f));
            created = true;
        }
        return created;
    }

    private static int Scaled(int referenceSize)
    {
        return Mathf.Max(9, Mathf.RoundToInt(referenceSize * scale));
    }

    private static GUIStyle LabelStyle(int referenceSize, TextAnchor anchor, bool bold)
    {
        GUIStyle source = GUI.skin != null ? GUI.skin.label : null;
        var style = source != null ? new GUIStyle(source) : new GUIStyle();
        style.fontSize = Scaled(referenceSize);
        style.alignment = anchor;
        style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.focused.textColor = Color.white;
        style.wordWrap = false;
        style.richText = false;
        style.clipping = TextClipping.Clip;
        style.padding = new RectOffset(0, 0, 0, 0);
        style.margin = new RectOffset(0, 0, 0, 0);
        style.border = new RectOffset(0, 0, 0, 0);
        style.contentOffset = Vector2.zero;
        return style;
    }

    private static GUIStyle ButtonStyle(Texture2D normal, Texture2D hover, Texture2D active, int referenceSize)
    {
        GUIStyle source = GUI.skin != null ? GUI.skin.button : null;
        var style = source != null ? new GUIStyle(source) : new GUIStyle();
        style.normal.background = normal;
        style.hover.background = hover;
        style.active.background = active;
        style.focused.background = normal;
        style.onNormal.background = active;
        style.onHover.background = active;
        style.onActive.background = active;
        style.onFocused.background = active;
        style.normal.textColor = Ink;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.focused.textColor = Ink;
        style.onNormal.textColor = Color.white;
        style.onHover.textColor = Color.white;
        style.onActive.textColor = Color.white;
        style.onFocused.textColor = Color.white;
        style.fontSize = Scaled(referenceSize);
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = false;
        style.richText = false;
        style.clipping = TextClipping.Clip;
        style.padding = new RectOffset(0, 0, 0, 0);
        style.margin = new RectOffset(0, 0, 0, 0);
        style.border = new RectOffset(0, 0, 0, 0);
        style.fixedWidth = 0f;
        style.fixedHeight = 0f;
        return style;
    }

    private static void BuildStyles()
    {
        secondaryButton = ButtonStyle(buttonTexture, buttonHoverTexture, buttonActiveTexture, 22);
        primaryButton = ButtonStyle(primaryTexture, primaryHoverTexture, primaryActiveTexture, 22);
        dangerButton = ButtonStyle(dangerTexture, dangerHoverTexture, dangerActiveTexture, 22);
        dangerButton.normal.textColor = new Color(1f, 0.62f, 0.60f);
        dangerButton.focused.textColor = new Color(1f, 0.62f, 0.60f);
        compactButton = ButtonStyle(buttonTexture, buttonHoverTexture, buttonActiveTexture, 26);

        ghostStyle = ButtonStyle(null, null, null, 16);
        ghostStyle.normal.background = null;
        ghostStyle.hover.background = null;
        ghostStyle.active.background = null;
        ghostStyle.focused.background = null;
        ghostStyle.onNormal.background = null;
        ghostStyle.onHover.background = null;
        ghostStyle.onActive.background = null;
        ghostStyle.onFocused.background = null;

        GUIStyle boxSource = GUI.skin != null ? GUI.skin.box : null;
        panelStyle = boxSource != null ? new GUIStyle(boxSource) : new GUIStyle();
        panelStyle.normal.background = panelTexture;
        panelStyle.hover.background = panelTexture;
        panelStyle.active.background = panelTexture;
        panelStyle.focused.background = panelTexture;
        panelStyle.border = new RectOffset(0, 0, 0, 0);
        panelStyle.padding = new RectOffset(0, 0, 0, 0);
        panelStyle.margin = new RectOffset(0, 0, 0, 0);

        bigTitle = LabelStyle(78, TextAnchor.MiddleCenter, true);
        sectionTitle = LabelStyle(44, TextAnchor.MiddleCenter, true);
        sectionSubtitle = LabelStyle(16, TextAnchor.MiddleCenter, false);
        menuSubtitle = LabelStyle(20, TextAnchor.MiddleCenter, true);
        seasonLine = LabelStyle(16, TextAnchor.MiddleCenter, true);
        bodyLeft = LabelStyle(20, TextAnchor.MiddleLeft, false);
        bodyCenter = LabelStyle(20, TextAnchor.MiddleCenter, false);
        bodyRight = LabelStyle(20, TextAnchor.MiddleRight, false);
        smallCenter = LabelStyle(15, TextAnchor.MiddleCenter, false);
        hintCenter = LabelStyle(14, TextAnchor.MiddleCenter, false);
        standings = LabelStyle(17, TextAnchor.MiddleLeft, false);
        scoreLeft = LabelStyle(38, TextAnchor.MiddleLeft, true);
        scoreRight = LabelStyle(38, TextAnchor.MiddleRight, true);
        teamLeft = LabelStyle(15, TextAnchor.MiddleLeft, true);
        teamRight = LabelStyle(15, TextAnchor.MiddleRight, true);
        timerText = LabelStyle(36, TextAnchor.MiddleCenter, true);
        stripScore = LabelStyle(25, TextAnchor.MiddleCenter, true);
        bannerText = LabelStyle(34, TextAnchor.MiddleCenter, true);
        stunLabel = LabelStyle(13, TextAnchor.MiddleCenter, true);
        clockLabel = LabelStyle(12, TextAnchor.MiddleCenter, true);
        hudSmall = LabelStyle(12, TextAnchor.MiddleLeft, true);
        hudSmallRight = LabelStyle(12, TextAnchor.MiddleRight, true);
        hudTiny = LabelStyle(10, TextAnchor.MiddleLeft, true);
        hudTinyRight = LabelStyle(10, TextAnchor.MiddleRight, true);
        hudTinyCenter = LabelStyle(10, TextAnchor.MiddleCenter, true);
        rosterName = LabelStyle(14, TextAnchor.MiddleLeft, true);
        rosterValue = LabelStyle(11, TextAnchor.MiddleRight, true);
        barLabel = LabelStyle(11, TextAnchor.MiddleLeft, true);
        barValue = LabelStyle(11, TextAnchor.MiddleRight, true);
        camLabel = LabelStyle(9, TextAnchor.MiddleCenter, true);
        statsTitle = LabelStyle(13, TextAnchor.MiddleLeft, true);
        statsHeader = LabelStyle(12, TextAnchor.MiddleRight, true);
        statsHeaderLeft = LabelStyle(12, TextAnchor.MiddleLeft, true);
        statsName = LabelStyle(16, TextAnchor.MiddleLeft, true);
        statsValue = LabelStyle(16, TextAnchor.MiddleRight, false);
    }

    private static void EnsureStyles()
    {
        int key = Mathf.RoundToInt(scale * 100f);
        bool rebuiltTextures = EnsureTextures();
        if (!rebuiltTextures && key == styleKey && bigTitle != null)
        {
            return;
        }
        BuildStyles();
        styleKey = key;
    }
}
