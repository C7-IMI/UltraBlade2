using System;
using UnityEngine;

// Runtime IMGUI presentation for the playable UltraBlade 2 slice. This class only
// reads UiModel and returns the requested UiAction.
// Every interactive rect is at least 48 reference px tall, i.e. >= 44 px at 720p.
public static class GameUI
{
    private const float ReferenceHeight = 720f;

    private static readonly Color Accent = new Color(0.20f, 0.93f, 1f);
    private static readonly Color Magenta = new Color(1f, 0.23f, 0.62f);
    private static readonly Color Ink = new Color(0.88f, 0.95f, 1f);
    private static readonly Color Muted = new Color(0.53f, 0.67f, 0.75f);
    private static readonly Color Good = new Color(0.35f, 1f, 0.68f);
    private static readonly Color Bad = new Color(1f, 0.33f, 0.35f);
    private static readonly Color Neutral = new Color(0.86f, 0.9f, 0.98f);

    private static readonly string[] DifficultyLabels = { "EASY", "NORMAL", "HARD" };
    private static readonly string[] EmptyLines = new string[0];

    private static float scale = 1f;
    private static int styleKey = int.MinValue;
    private static UiModel fallbackModel;

    private static Texture2D solidTexture;
    private static Texture2D panelTexture;
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
    private static GUIStyle bannerText;
    private static GUIStyle cooldownLabel;
    private static GUIStyle stunLabel;

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

        scale = Mathf.Clamp(Screen.height / ReferenceHeight, 0.5f, 3f);
        EnsureStyles();

        switch (target.screen)
        {
            case GameScreen.Settings:
                return DrawSettings(target.settings);
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
        DrawBackdrop(0.94f);

        float width = SafeWidth(1000f);
        DrawBigTitle(Centered(66f, width, 96f), Clean(model.title, "ULTRABLADE 2"), Accent);
        LabelTinted(Centered(172f, width, 30f),
            Clean(model.subtitle, "WAYFARER PROTOCOL").ToUpperInvariant(), menuSubtitle, new Color(0.72f, 0.86f, 0.94f));

        int played = Mathf.Max(0, model.seasonWins) + Mathf.Max(0, model.seasonLosses);
        if (played > 0)
        {
            string record = "SEASON RECORD   W" + Mathf.Max(0, model.seasonWins) +
                            "   L" + Mathf.Max(0, model.seasonLosses) +
                            "   /   " + Mathf.Max(0, model.seasonPoints) + " PTS";
            LabelTinted(Centered(212f, width, 28f), record, seasonLine, Accent);
        }

        float buttonWidth = Mathf.Min(430f, ViewWidth - 96f);
        float y = 262f;
        if (Button(Centered(y, buttonWidth, 58f), "START MATCH", primaryButton))
        {
            return UiAction.StartMatch;
        }
        y += 68f;
        if (Button(Centered(y, buttonWidth, 58f), "LEAGUE", secondaryButton))
        {
            return UiAction.OpenLeague;
        }
        y += 68f;
        if (Button(Centered(y, buttonWidth, 58f), "SETTINGS", secondaryButton))
        {
            return UiAction.OpenSettings;
        }
        y += 68f;
        if (Button(Centered(y, buttonWidth, 58f), "QUIT", dangerButton))
        {
            return UiAction.QuitGame;
        }

        LabelTinted(Centered(566f, ViewWidth - 96f, 24f),
            model.touch ? "TOUCH CONTROLS ENABLED IN MATCH" : "KEYBOARD + MOUSE", hintCenter,
            model.touch ? Accent : Muted);
        LabelTinted(Centered(600f, ViewWidth - 96f, 24f),
            model.touch
                ? "DRAG THE LEFT SIDE TO MOVE   ATTACK / DASH ON THE RIGHT"
                : "WASD / ARROWS MOVE   SPACE / CLICK ATTACK   SHIFT DASH",
            hintCenter, Muted);
        LabelTinted(Centered(650f, ViewWidth - 96f, 22f),
            model.touch
                ? "BACK BUTTON PAUSES A MATCH   /   SEASON PROGRESS SAVES AUTOMATICALLY"
                : "ESC / P PAUSES A MATCH   /   SEASON PROGRESS SAVES AUTOMATICALLY",
            hintCenter, new Color(Muted.r, Muted.g, Muted.b, 0.55f));

        return UiAction.None;
    }

    private static UiAction DrawSettings(GameSettings settings)
    {
        DrawBackdrop(0.96f);
        LabelTinted(Centered(30f, SafeWidth(1000f), 54f), "SETTINGS", sectionTitle, Accent);
        LabelTinted(Centered(88f, SafeWidth(1100f), 22f),
            "AUDIO LEVELS   /   MATCH DIFFICULTY   /   INPUT", sectionSubtitle, Muted);

        float width = Mathf.Min(820f, ViewWidth - 64f);
        float left = (ViewWidth - width) * 0.5f;
        Rect panel = R(left, 118f, width, 544f);
        DrawPanel(panel);

        float rowLeft = left + 34f;
        float rowWidth = width - 68f;
        float rowY = 148f;
        const float RowStep = 66f;

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

        if (Button(Centered(570f, 260f, 54f), "BACK", secondaryButton))
        {
            return UiAction.Back;
        }

        LabelTinted(Centered(674f, ViewWidth - 96f, 22f),
            "VOLUME STEPS +/- 10%", hintCenter, Muted);
        return UiAction.None;
    }

    private static UiAction DrawLeague(UiModel model)
    {
        HudSnapshot hud = model.hud;
        string[] lines = model.leagueLines;
        DrawBackdrop();

        LabelTinted(Centered(26f, SafeWidth(1000f), 56f), "LEAGUE", sectionTitle, Accent);
        string record = "SEASON RECORD   W" + Mathf.Max(0, model.seasonWins) +
                        "   L" + Mathf.Max(0, model.seasonLosses) +
                        "   /   " + Mathf.Max(0, model.seasonPoints) + " PTS";
        LabelTinted(Centered(88f, SafeWidth(1100f), 26f), record, seasonLine, Ink);

        float width = Mathf.Min(940f, ViewWidth - 64f);
        float left = (ViewWidth - width) * 0.5f;
        Rect panel = R(left, 120f, width, 352f);
        DrawPanel(panel);
        LabelTinted(R(left + 26f, 132f, width - 52f, 22f), "STANDINGS", smallCenter, Accent);

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
            LabelTinted(R(left + 26f, 240f, width - 52f, 32f), "NO STANDINGS YET - START A MATCH", bodyCenter, Muted);
        }
        else
        {
            const int MaxRows = 10;
            int visible = Mathf.Min(count, MaxRows);
            float rowHeight = Mathf.Clamp(300f / Mathf.Max(1, visible), 20f, 30f);
            float rowY = 160f;
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
                Rect rowRect = R(left + 18f, rowY, width - 36f, rowHeight - 2f);
                if (highlight)
                {
                    Fill(rowRect, solidTexture, new Color(Accent.r, Accent.g, Accent.b, 0.12f));
                }
                else if ((row % 2) == 0)
                {
                    Fill(rowRect, solidTexture, new Color(1f, 1f, 1f, 0.025f));
                }
                LabelTinted(R(left + 30f, rowY, width - 60f, rowHeight - 2f), text, standings,
                    highlight ? Accent : Ink);
                rowY += rowHeight;
                row++;
            }
        }

        string next = string.IsNullOrEmpty(model.nextOpponent) ? "TBD" : model.nextOpponent.ToUpperInvariant();
        LabelTinted(Centered(492f, SafeWidth(1100f), 28f), "NEXT OPPONENT: " + next, bodyCenter, Ink);
        if (model.seasonComplete)
        {
            LabelTinted(Centered(522f, SafeWidth(1100f), 24f), "SEASON COMPLETE - START A NEW SEASON", smallCenter, Good);
        }

        if (Button(R(left, 566f, 250f, 58f), "BACK", secondaryButton))
        {
            return UiAction.Back;
        }
        if (model.seasonComplete)
        {
            if (Button(R(left + width - 360f, 566f, 360f, 58f), "NEW SEASON", primaryButton))
            {
                return UiAction.NewSeason;
            }
        }
        else if (Button(R(left + width - 360f, 566f, 360f, 58f), "START NEXT MATCH", primaryButton))
        {
            return UiAction.StartNextMatch;
        }

        LabelTinted(Centered(650f, ViewWidth - 96f, 22f),
            "OPPONENTS ROTATE THROUGH THE TABLE   /   WINS ADD POINTS", hintCenter, Muted);
        return UiAction.None;
    }

    private static UiAction DrawPaused(UiModel model)
    {
        HudSnapshot hud = model.hud;
        DrawDim(0.74f);
        float width = Mathf.Min(600f, ViewWidth - 72f);
        float left = (ViewWidth - width) * 0.5f;
        Rect panel = R(left, 106f, width, 478f);
        DrawPanel(panel);

        LabelTinted(Centered(132f, width - 40f, 58f), "PAUSED", sectionTitle, Accent);
        LabelTinted(Centered(190f, width - 40f, 22f), "MATCH SUSPENDED", sectionSubtitle, Muted);
        LabelTinted(Centered(218f, width - 40f, 40f),
            Clean(hud.playerTeam, "WAYFARERS") + "   " + Mathf.Max(0, hud.playerScore) +
            "  -  " + Mathf.Max(0, hud.aiScore) + "   " + Clean(hud.aiTeam, "VOID"),
            bodyCenter, Ink);
        LabelTinted(Centered(252f, width - 40f, 22f), FormatTime(hud.timeLeft) + " REMAINING", smallCenter, Muted);

        float buttonWidth = Mathf.Min(400f, width - 96f);
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

        LabelTinted(Centered(552f, width - 40f, 22f),
            model.touch ? "BACK BUTTON TO RESUME" : "ESC / P TO RESUME", hintCenter, Muted);
        return UiAction.None;
    }

    private static UiAction DrawResult(UiModel model)
    {
        HudSnapshot hud = model.hud;
        string outcome = ResultLabel(hud);
        Color tint = outcome == "WIN" ? Good : outcome == "LOSE" ? Bad : Neutral;
        DrawBackdrop(0.95f);

        float width = Mathf.Min(880f, ViewWidth - 64f);
        float left = (ViewWidth - width) * 0.5f;
        Rect panel = R(left, 84f, width, 536f);
        DrawPanel(panel);

        DrawBigTitle(R(left + 40f, 104f, width - 80f, 96f), outcome, tint);
        LabelTinted(Centered(210f, width - 40f, 22f), "FINAL SCORE", sectionSubtitle, Muted);

        Color playerColor = Opaque(hud.playerColor);
        Color aiColor = Opaque(hud.aiColor);
        float half = width * 0.5f;
        LabelTinted(R(left + 36f, 242f, half - 56f, 52f),
            Clean(hud.playerTeam, "WAYFARERS") + "   " + Mathf.Max(0, hud.playerScore),
            scoreLeft, playerColor);
        LabelTinted(R(left + half + 20f, 242f, half - 56f, 52f),
            Mathf.Max(0, hud.aiScore) + "   " + Clean(hud.aiTeam, "VOID"),
            scoreRight, aiColor);
        Fill(R(left + half - 1f, 248f, 2f, 40f), solidTexture, new Color(1f, 1f, 1f, 0.22f));

        string leagueLine = "SEASON   W" + Mathf.Max(0, model.seasonWins) +
                            "   L" + Mathf.Max(0, model.seasonLosses) +
                            "   /   " + Mathf.Max(0, model.seasonPoints) + " PTS";
        LabelTinted(Centered(322f, width - 80f, 28f), leagueLine, bodyCenter, Ink);
        if (model.seasonComplete)
        {
            LabelTinted(Centered(354f, width - 80f, 24f), "SEASON COMPLETE", smallCenter, Good);
        }
        else if (!string.IsNullOrEmpty(model.nextOpponent))
        {
            LabelTinted(Centered(354f, width - 80f, 24f), "NEXT: " + model.nextOpponent.ToUpperInvariant(), smallCenter, Muted);
        }

        float available = Mathf.Min(width - 96f, 760f);
        float buttonWidth = (available - 32f) / 3f;
        float buttonX = left + (width - available) * 0.5f;
        float buttonY = 448f;
        UiAction advance = model.seasonComplete ? UiAction.QuitToMenu : UiAction.StartNextMatch;
        if (Button(R(buttonX, buttonY, buttonWidth, 58f), "CONTINUE", primaryButton))
        {
            return advance;
        }
        if (Button(R(buttonX + buttonWidth + 16f, buttonY, buttonWidth, 58f), "REMATCH", secondaryButton))
        {
            return UiAction.RestartMatch;
        }
        if (Button(R(buttonX + (buttonWidth + 16f) * 2f, buttonY, buttonWidth, 58f), "MENU", secondaryButton))
        {
            return UiAction.QuitToMenu;
        }

        LabelTinted(Centered(526f, width - 60f, 22f),
            model.seasonComplete
                ? "CONTINUE RETURNS TO THE MENU"
                : "CONTINUE ADVANCES TO THE NEXT MATCH",
            hintCenter, Muted);
        return UiAction.None;
    }

    private static void DrawMatchHud(UiModel model)
    {
        HudSnapshot hud = model.hud;
        float vw = ViewWidth;
        Color playerColor = Opaque(hud.playerColor);
        Color aiColor = Opaque(hud.aiColor);

        const float BoxWidth = 300f;
        const float Side = 20f;
        Rect leftBox = R(Side, 16f, BoxWidth, 88f);
        Rect rightBox = R(vw - Side - BoxWidth, 16f, BoxWidth, 88f);
        DrawHudBox(leftBox, playerColor);
        DrawHudBox(rightBox, aiColor);

        LabelTinted(R(Side + 16f, 24f, BoxWidth - 32f, 24f),
            Clean(hud.playerTeam, "WAYFARERS"), teamLeft, Lighten(playerColor));
        LabelTinted(R(vw - Side - BoxWidth + 16f, 24f, BoxWidth - 32f, 24f),
            Clean(hud.aiTeam, "VOID"), teamRight, Lighten(aiColor));
        Color playerScore = hud.playerStunned ? Bad : playerColor;
        if (hud.collectFlash == 1)
        {
            playerScore = Color.Lerp(playerScore, Color.white, 0.45f);
        }
        Color aiScore = hud.aiStunned ? Bad : aiColor;
        if (hud.collectFlash == 2)
        {
            aiScore = Color.Lerp(aiScore, Color.white, 0.45f);
        }
        LabelTinted(R(Side + 16f, 48f, BoxWidth - 32f, 48f),
            Mathf.Max(0, hud.playerScore).ToString(), scoreLeft, playerScore);
        LabelTinted(R(vw - Side - BoxWidth + 16f, 48f, BoxWidth - 32f, 48f),
            Mathf.Max(0, hud.aiScore).ToString(), scoreRight, aiScore);

        Fill(R(Side, 16f, 6f, 88f), solidTexture, playerColor);
        Fill(R(vw - Side - 6f, 16f, 6f, 88f), solidTexture, aiColor);

        DrawStunBadge(R(Side, 202f, 156f, 26f), hud.playerStunned);
        DrawStunBadge(R(vw - Side - 156f, 202f, 156f, 26f), hud.aiStunned);

        float timerWidth = 280f;
        float timerX = (vw - timerWidth) * 0.5f;
        Fill(R(timerX, 16f, timerWidth, 88f), solidTexture, new Color(0.01f, 0.02f, 0.03f, 0.48f));
        Fill(R(timerX, 16f, timerWidth, 2f), solidTexture, new Color(Accent.r, Accent.g, Accent.b, 0.5f));
        bool urgent = hud.timeLeft <= 10f;
        LabelTinted(R(timerX, 17f, timerWidth, 52f), FormatTime(hud.timeLeft), timerText,
            urgent ? Bad : Ink);
        LabelTinted(R(timerX, 72f, timerWidth, 20f), "TIME", smallCenter, Muted);

        string info = "TARGET " + Mathf.Max(1, hud.targetScore) +
                      "   ROUND " + Mathf.Max(1, hud.round) + "/" + Mathf.Max(1, hud.totalRounds) +
                      "   " + Clean(hud.arenaName, "NEON RINK");
        LabelTinted(Centered(110f, Mathf.Min(760f, vw - 80f), 24f), info, hintCenter, new Color(0.72f, 0.84f, 0.9f));

        string banner = hud.banner;
        if (string.IsNullOrEmpty(banner) && hud.kickoff)
        {
            banner = "KICKOFF";
        }
        if (!string.IsNullOrEmpty(banner))
        {
            float bannerWidth = Mathf.Min(920f, vw - 80f);
            Rect bannerRect = R((vw - bannerWidth) * 0.5f, 178f, bannerWidth, 64f);
            Fill(bannerRect, solidTexture, new Color(0.04f, 0.015f, 0.05f, 0.74f));
            Fill(new Rect(bannerRect.x, bannerRect.y, bannerRect.width, 2f * scale), solidTexture, Magenta);
            Fill(new Rect(bannerRect.x, bannerRect.yMax - 2f * scale, bannerRect.width, 2f * scale), solidTexture,
                new Color(Magenta.r, Magenta.g, Magenta.b, 0.45f));
            LabelTinted(bannerRect, banner, bannerText, new Color(1f, 0.88f, 0.96f));
        }

        Rect cooldownPanel = R(Side, 112f, 246f, 80f);
        Fill(cooldownPanel, solidTexture, new Color(0.01f, 0.02f, 0.03f, 0.48f));
        Fill(new Rect(cooldownPanel.x, cooldownPanel.y, 2f * scale, cooldownPanel.height), solidTexture,
            new Color(Accent.r, Accent.g, Accent.b, 0.45f));
        // Cooldown maxima follow the slice's combat timers (1.1 s attack, 2 s dash).
        DrawCooldown(R(Side + 12f, 120f, 220f, 28f), "ATTACK", ReadyFraction(hud.attackCooldown, 1.1f));
        DrawCooldown(R(Side + 12f, 152f, 220f, 28f), "DASH", ReadyFraction(hud.dashCooldown, 2f));

        string hint = model.touch
            ? "TOUCH: DRAG LEFT TO MOVE   ATTACK / DASH ON THE RIGHT"
            : "WASD / ARROWS MOVE   SPACE / CLICK ATTACK   SHIFT DASH   ESC / P PAUSE";
        float hintWidth = Mathf.Min(760f, Mathf.Max(320f, vw - 220f));
        LabelTinted(Centered(674f, hintWidth, 24f), hint, hintCenter, new Color(0.72f, 0.82f, 0.88f, 0.9f));
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

    private static void LabelTinted(Rect rect, string text, GUIStyle style, Color color)
    {
        if (style == null || string.IsNullOrEmpty(text) || color.a <= 0f)
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
        Color glow = style == dangerButton ? Bad : Accent;
        Fill(new Rect(rect.x - scale, rect.y - scale, rect.width + 2f * scale, rect.height + 2f * scale),
            solidTexture, new Color(glow.r, glow.g, glow.b, style == secondaryButton ? 0.16f : 0.26f));
        return GUI.Button(rect, text, style);
    }

    private static void DrawBigTitle(Rect rect, string text, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        float offset = 3f * scale;
        Color glow = new Color(color.r, color.g, color.b, 0.16f);
        LabelTinted(new Rect(rect.x - offset, rect.y, rect.width, rect.height), text, bigTitle, glow);
        LabelTinted(new Rect(rect.x + offset, rect.y, rect.width, rect.height), text, bigTitle, glow);
        LabelTinted(new Rect(rect.x, rect.y - offset, rect.width, rect.height), text, bigTitle, glow);
        LabelTinted(new Rect(rect.x, rect.y + offset, rect.width, rect.height), text, bigTitle, glow);
        LabelTinted(rect, text, bigTitle, color);
    }

    private static void DrawBackdrop(float alpha = 0.95f)
    {
        Fill(new Rect(0f, 0f, Screen.width, Screen.height), solidTexture,
            new Color(0.012f, 0.02f, 0.032f, alpha));
        float thick = Mathf.Max(1f, 3f * scale);
        Fill(new Rect(0f, 0f, Screen.width, thick), solidTexture,
            new Color(Accent.r, Accent.g, Accent.b, 0.5f));
        Fill(new Rect(0f, thick + scale, Screen.width, Mathf.Max(1f, scale)), solidTexture,
            new Color(Accent.r, Accent.g, Accent.b, 0.16f));
        Fill(new Rect(0f, Screen.height - thick, Screen.width, thick), solidTexture,
            new Color(Magenta.r, Magenta.g, Magenta.b, 0.4f));
        float railX = 22f * scale;
        float railThickness = Mathf.Max(1f, scale);
        Fill(new Rect(railX, 24f * scale, railThickness, Screen.height - 48f * scale), solidTexture,
            new Color(Accent.r, Accent.g, Accent.b, 0.12f));
        Fill(new Rect(Screen.width - railX - railThickness, 24f * scale, railThickness,
            Screen.height - 48f * scale), solidTexture, new Color(Accent.r, Accent.g, Accent.b, 0.12f));
        for (int i = 1; i < 9; i++)
        {
            float y = i * 72f * scale;
            if (y > Screen.height - 24f * scale)
            {
                break;
            }
            Fill(new Rect(0f, y, Screen.width, railThickness), solidTexture, new Color(1f, 1f, 1f, 0.018f));
        }
    }

    private static void DrawDim(float alpha)
    {
        Fill(new Rect(0f, 0f, Screen.width, Screen.height), solidTexture,
            new Color(0.005f, 0.01f, 0.018f, alpha));
    }

    private static void DrawPanel(Rect rect)
    {
        GUI.Box(rect, GUIContent.none, panelStyle);
        float edge = Mathf.Max(1f, 2f * scale);
        Color cyan = new Color(Accent.r, Accent.g, Accent.b, 0.3f);
        Fill(new Rect(rect.x, rect.y, rect.width, edge), solidTexture, cyan);
        Fill(new Rect(rect.x, rect.yMax - edge, rect.width, edge), solidTexture, cyan);
        Fill(new Rect(rect.x, rect.y, edge, rect.height), solidTexture, new Color(Accent.r, Accent.g, Accent.b, 0.14f));
        Fill(new Rect(rect.xMax - edge, rect.y, edge, rect.height), solidTexture, new Color(Accent.r, Accent.g, Accent.b, 0.14f));
        float tick = 16f * scale;
        Color pink = new Color(Magenta.r, Magenta.g, Magenta.b, 0.8f);
        Fill(new Rect(rect.x, rect.y, tick, edge), solidTexture, pink);
        Fill(new Rect(rect.xMax - tick, rect.yMax - edge, tick, edge), solidTexture, pink);
    }

    private static void DrawHudBox(Rect rect, Color teamColor)
    {
        Fill(rect, solidTexture, new Color(0.01f, 0.02f, 0.03f, 0.56f));
        Fill(new Rect(rect.x, rect.y, rect.width, 2f * scale), solidTexture,
            new Color(teamColor.r, teamColor.g, teamColor.b, 0.75f));
        Fill(new Rect(rect.x, rect.yMax - 2f * scale, rect.width, 2f * scale), solidTexture,
            new Color(teamColor.r, teamColor.g, teamColor.b, 0.28f));
    }

    private static void DrawStunBadge(Rect rect, bool stunned)
    {
        if (!stunned)
        {
            return;
        }
        Fill(rect, solidTexture, new Color(Bad.r, Bad.g, Bad.b, 0.18f));
        Color edge = new Color(Bad.r, Bad.g, Bad.b, 0.9f);
        Fill(new Rect(rect.x, rect.y, rect.width, 2f * scale), solidTexture, edge);
        Fill(new Rect(rect.x, rect.yMax - 2f * scale, rect.width, 2f * scale), solidTexture, edge);
        LabelTinted(rect, "STUNNED", stunLabel, new Color(1f, 0.68f, 0.68f));
    }

    private static float ReadyFraction(float cooldown, float maximum)
    {
        if (float.IsNaN(cooldown) || float.IsInfinity(cooldown) || cooldown <= 0f)
        {
            return 1f;
        }
        return Mathf.Clamp01(1f - cooldown / Mathf.Max(0.01f, maximum));
    }

    private static void DrawCooldown(Rect rect, string label, float ready)
    {
        ready = Mathf.Clamp01(ready);
        LabelTinted(new Rect(rect.x, rect.y, 62f * scale, rect.height), label, cooldownLabel, Muted);
        float barHeight = Mathf.Max(3f, 8f * scale);
        Rect bar = new Rect(rect.x + 66f * scale, rect.y + (rect.height - barHeight) * 0.5f,
            Mathf.Max(10f * scale, rect.width - 66f * scale), barHeight);
        Fill(bar, solidTexture, new Color(1f, 1f, 1f, 0.1f));
        if (ready > 0f)
        {
            Color fill = ready >= 0.999f ? Good : new Color(Accent.r, Accent.g, Accent.b, 0.75f);
            Fill(new Rect(bar.x, bar.y, bar.width * ready, bar.height), solidTexture, fill);
        }
    }

    private static float VolumeRow(float x, float y, float width, string label, float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            value = 0.5f;
        }
        value = Mathf.Clamp01(value);
        LabelTinted(R(x, y, 170f, 56f), label, bodyLeft, Ink);

        float controlY = y + 2f;
        if (Button(R(x + 178f, controlY, 50f, 52f), "-", compactButton))
        {
            value = ClampVolume(value - 0.1f);
        }
        float plusX = x + width - 50f;
        if (Button(R(plusX, controlY, 50f, 52f), "+", compactButton))
        {
            value = ClampVolume(value + 0.1f);
        }
        float valueX = plusX - 82f;
        float trackX = x + 240f;
        float trackWidth = Mathf.Max(70f, valueX - 14f - trackX);
        Rect track = R(trackX, y + 24f, trackWidth, 8f);
        Fill(track, solidTexture, new Color(1f, 1f, 1f, 0.09f));
        if (value > 0f)
        {
            Fill(new Rect(track.x, track.y, track.width * value, track.height), solidTexture,
                new Color(Accent.r, Accent.g, Accent.b, 0.9f));
        }
        Rect knob = R(trackX + trackWidth * value - 3f, y + 19f, 6f, 18f);
        Fill(knob, solidTexture, Color.white);
        LabelTinted(R(valueX, y, 70f, 56f), Mathf.RoundToInt(value * 100f) + "%", bodyRight, Ink);

        Rect hitRect = R(trackX, y + 4f, trackWidth, 48f);
        bool hover = hitRect.Contains(Event.current.mousePosition);
        if (hover)
        {
            Fill(new Rect(track.x, track.y - 2f * scale, track.width, track.height + 4f * scale), solidTexture,
                new Color(Accent.r, Accent.g, Accent.b, 0.1f));
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

    private static string Clean(string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
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

    private static string ResultLabel(HudSnapshot hud)
    {
        string text = hud.resultText == null ? "" : hud.resultText;
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

    private static Color Opaque(Color color)
    {
        color.a = 1f;
        return color;
    }

    private static Color Lighten(Color color)
    {
        return Color.Lerp(Opaque(color), Color.white, 0.25f);
    }

    // ------------------------------------------------------------- style setup

    private static Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
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
            panelTexture = Solid(new Color(0.012f, 0.028f, 0.042f, 0.93f));
            created = true;
        }
        if (buttonTexture == null)
        {
            buttonTexture = Solid(new Color(0.02f, 0.06f, 0.08f, 0.92f));
            buttonHoverTexture = Solid(new Color(0.04f, 0.16f, 0.2f, 0.96f));
            buttonActiveTexture = Solid(new Color(0.06f, 0.28f, 0.34f, 1f));
            created = true;
        }
        if (primaryTexture == null)
        {
            primaryTexture = Solid(new Color(0.02f, 0.2f, 0.25f, 0.96f));
            primaryHoverTexture = Solid(new Color(0.05f, 0.36f, 0.44f, 1f));
            primaryActiveTexture = Solid(new Color(0.1f, 0.56f, 0.66f, 1f));
            created = true;
        }
        if (dangerTexture == null)
        {
            dangerTexture = Solid(new Color(0.16f, 0.03f, 0.05f, 0.92f));
            dangerHoverTexture = Solid(new Color(0.34f, 0.06f, 0.1f, 0.96f));
            dangerActiveTexture = Solid(new Color(0.5f, 0.1f, 0.16f, 1f));
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

        bigTitle = LabelStyle(72, TextAnchor.MiddleCenter, true);
        sectionTitle = LabelStyle(42, TextAnchor.MiddleCenter, true);
        sectionSubtitle = LabelStyle(18, TextAnchor.MiddleCenter, false);
        menuSubtitle = LabelStyle(20, TextAnchor.MiddleCenter, true);
        seasonLine = LabelStyle(18, TextAnchor.MiddleCenter, true);
        bodyLeft = LabelStyle(20, TextAnchor.MiddleLeft, false);
        bodyCenter = LabelStyle(20, TextAnchor.MiddleCenter, false);
        bodyRight = LabelStyle(20, TextAnchor.MiddleRight, false);
        smallCenter = LabelStyle(16, TextAnchor.MiddleCenter, false);
        hintCenter = LabelStyle(15, TextAnchor.MiddleCenter, false);
        standings = LabelStyle(18, TextAnchor.MiddleLeft, false);
        scoreLeft = LabelStyle(44, TextAnchor.MiddleLeft, true);
        scoreRight = LabelStyle(44, TextAnchor.MiddleRight, true);
        teamLeft = LabelStyle(17, TextAnchor.MiddleLeft, true);
        teamRight = LabelStyle(17, TextAnchor.MiddleRight, true);
        timerText = LabelStyle(48, TextAnchor.MiddleCenter, true);
        bannerText = LabelStyle(30, TextAnchor.MiddleCenter, true);
        cooldownLabel = LabelStyle(13, TextAnchor.MiddleLeft, true);
        stunLabel = LabelStyle(14, TextAnchor.MiddleCenter, true);
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
