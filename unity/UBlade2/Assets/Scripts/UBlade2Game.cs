using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// UltraBlade 2 - HyperBlade-fidelity playable slice.
// The match is a 2-vs-2 skater sport (plus goalies) on an elliptical drome: pick up the rok,
// carry/pass/shoot it into the small wall goal, hit opponents with the jak, avoid the drome
// hazards, score 2 (or 4 through the multiplier) across three periods.
public class UBlade2Game : MonoBehaviour
{
    private const float BaseSpeed = 18f;

    private GameSettings settings;
    private LeagueData league;
    private UiModel uiModel = new UiModel();
    private GameScreen screen = GameScreen.MainMenu;
    private GameScreen previousScreen = GameScreen.MainMenu;

    private MatchRules match;
    private RokSystem rok;
    private MatchStats stats;
    private GoalieSystem homeGoalie;
    private GoalieSystem awayGoalie;
    private GameObject homeGoalieVisual;
    private GameObject awayGoalieVisual;
    private DromeInstance drome;
    private TeamDefinition homeTeam;
    private TeamDefinition awayTeam;
    private ArenaDefinition arena;

    private Skater[] homeSkaters = new Skater[4];
    private Skater[] awaySkaters = new Skater[4];
    private readonly List<GameObject> worldObjects = new List<GameObject>();
    private GameObject dromeRoot;

    private Camera cam;
    private Transform camPivot;
    private bool demo;
    private bool matchMode;
    private bool captureAutoplay;
    private bool debugOverview;
    private bool playTest;
    private bool leagueTest;
    private string capturePath;
    private string evidencePath;
    private string menuCapturePath;
    private string matchCapturePath;
    private string uiCaptureDir;
    private string mobileCapturePath;
    private bool mobileCapture;
    private bool worldReady;
    private bool audioReady;
    private bool resultResolved;
    private bool ranked = true;
    private int matchFrames;
    private int legacyCollected;
    private int legacyFrames;
    private float vy;
    private float bannerTimer;
    private string banner = "";
    private readonly List<string> feed = new List<string>();
    private float attackCooldown;
    private float playerPower;
    private bool chargingShot;
    private float speedBurstMeter = 1f;
    private bool speedburst;
    private int multiplierLevel = 1;
    private bool multiplierArmed;
    private float hazardCooldown;
    private int multiplierOwner;
    private float savedFlashTimer;
    private MatchPhase lastPhase;
    private AiController homeAi;
    private AiController awayAi0;
    private AiController awayAi1;
    private AiController autopilotAi;

    private readonly List<GameObject> legacyCrystals = new List<GameObject>();
    private Transform legacyPlayer;
    private CharacterController legacyController;
    private HeroVisual legacyHero;
    private Transform legacyCamera;

    [Serializable]
    private class LegacyEvidence
    {
        public string schema = "ublade2.hero.runtime/v1";
        public bool passed;
        public bool heroVisible;
        public int lodLevel;
        public float smileWeight;
        public int capeBones;
        public int bones;
        public int lod0Triangles;
        public int lod1Triangles;
        public int lod2Triangles;
        public int collected;
        public int frames;
        public int heroRectX;
        public int heroRectY;
        public int heroRectWidth;
        public int heroRectHeight;
        public int heroScreenPixels;
        public string screen;
        public string unity;
        public string captureFile;
        public string generatedAt;
    }

    [Serializable]
    private class MatchEvidence
    {
        public string schema = "ublade2.match.runtime/v2";
        public bool passed;
        public int homeScore;
        public int awayScore;
        public int homeGoals;
        public int awayGoals;
        public int winner;
        public int frames;
        public int periods;
        public string phase;
        public string homeTeam;
        public string awayTeam;
        public string arena;
        public string screen;
        public string unity;
        public string generatedAt;
    }

    [Serializable]
    private class LeagueEvidence
    {
        public string schema = "ublade2.season.save/v1";
        public bool passed;
        public int round;
        public int wins;
        public int points;
        public string playerTeam;
        public string saveKey;
        public string generatedAt;
    }

    private sealed class Skater
    {
        public GameObject go;
        public Transform transform;
        public CharacterController controller;
        public DromerRig rig;
        public bool home;
        public int rosterIndex;
        public string name;
        public float health = 150f;
        public float stun;
        public float attackCooldown;
        public float hitCooldown;
        public bool downed;
        public bool active;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureHost()
    {
        if (FindObjectOfType<UBlade2Game>() == null)
        {
            var host = new GameObject("UBlade2");
            host.AddComponent<UBlade2Game>();
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        demo = HasArg("--demo");
        matchMode = HasArg("--match");
        playTest = HasArg("--play-test");
        leagueTest = HasArg("--league-test");
        matchCapturePath = GetArg("--match-capture=");
        menuCapturePath = GetArg("--menu-capture=");
        uiCaptureDir = GetArg("--ui-capture=");
        capturePath = GetArg("--capture=");
        evidencePath = GetArg("--evidence=");
#if UNITY_ANDROID && !UNITY_EDITOR
        demo = demo || AndroidIntentBool("ublade2_demo");
        if (AndroidIntentBool("ublade2_capture"))
        {
            mobileCapture = true;
            mobileCapturePath = Path.Combine(Application.persistentDataPath, "ublade2-mobile-frame.png");
        }
#endif
        captureAutoplay = !string.IsNullOrEmpty(matchCapturePath) || mobileCapture;
        settings = GameSettings.Load("ublade2.settings.");
        if (!Application.isMobilePlatform && !PlayerPrefs.HasKey("ublade2.settings.touch"))
        {
            settings.touchControls = false;
        }
        league = LeagueData.Load();
        if (league.SeasonComplete)
        {
            league.ResetSeason();
            league.BeginSeason();
        }
        audioReady = !Application.isBatchMode;
        if (audioReady)
        {
            GameAudio.Initialize(gameObject);
            GameAudio.ApplySettings(settings);
        }

        Debug.Log("UBLADE2_START demo=" + demo + " crystals=" + (demo ? 3 : 0) +
                  " unity=" + Application.unityVersion +
                  " screen=" + Screen.width + "x" + Screen.height +
                  " capture=" + (string.IsNullOrEmpty(capturePath) ? "none" : capturePath) +
                  " mode=" + (demo ? "demo" : matchMode ? "match" : captureAutoplay ? "capture" : "play"));

        if (demo)
        {
            BuildLegacyWorld();
            worldReady = true;
            StartCoroutine(RunLegacyDemo());
            return;
        }
        if (leagueTest)
        {
            worldReady = true;
            StartCoroutine(RunLeagueTest());
            return;
        }
        BuildMenuWorld();
        worldReady = true;

        if (playTest)
        {
            ranked = false;
            screen = GameScreen.Match;
            BeginMatch(false, 180f);
            Debug.Log("UBLADE2_PLAY_TEST_START player=" + homeSkaters[0].transform.position);
            return;
        }
        if (matchMode)
        {
            league.ResetSeason();
            league.BeginSeason();
            StartCoroutine(RunHeadlessMatch());
            return;
        }
        if (captureAutoplay)
        {
            league.ResetSeason();
            league.BeginSeason();
            StartCoroutine(RunMatchCapture(mobileCapture ? mobileCapturePath : matchCapturePath, mobileCapture));
            return;
        }
        if (!string.IsNullOrEmpty(menuCapturePath) || !string.IsNullOrEmpty(uiCaptureDir))
        {
            screen = GameScreen.MainMenu;
            UpdateMenuModel();
            StartCoroutine(!string.IsNullOrEmpty(menuCapturePath) ? RunMenuCapture() : RunUiCapture());
            return;
        }
        screen = GameScreen.MainMenu;
        UpdateMenuModel();
        if (audioReady)
        {
            GameAudio.StartMusic(0);
            GameAudio.PlayWhistle();
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool AndroidIntentBool(string key)
    {
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
            {
                if (intent == null || !intent.Call<bool>("hasExtra", key))
                {
                    return false;
                }
                return intent.Call<bool>("getBooleanExtra", key, false);
            }
        }
        catch (Exception)
        {
            return false;
        }
    }
#endif

    private static bool HasArg(string value) { return Array.Exists(Environment.GetCommandLineArgs(), a => a == value); }
    private static string GetArg(string prefix)
    {
        foreach (var value in Environment.GetCommandLineArgs())
        {
            if (value.StartsWith(prefix, StringComparison.Ordinal)) return value.Substring(prefix.Length);
        }
        return null;
    }

    // ---------------------------------------------------------------- world

    private Material Mat(Color color, float emission)
    {
        var mobile = Application.isMobilePlatform;
        var shader = mobile ? Shader.Find("Unlit/Color") : Shader.Find("Standard");
        var material = new Material(shader != null ? shader : Shader.Find("Legacy Shaders/Diffuse"));
        material.color = color;
        if (!mobile && emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        return material;
    }

    private void TintActor(Transform root, Color primary, Color secondary)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.materials;
            for (int index = 0; index < materials.Length; index++)
            {
                if (materials[index] == null) continue;
                materials[index].color = index % 2 == 0 ? primary : secondary;
                if (materials[index].HasProperty("_EmissionColor"))
                {
                    materials[index].EnableKeyword("_EMISSION");
                    materials[index].SetColor("_EmissionColor", primary * 0.15f);
                }
            }
            renderer.materials = materials;
        }
    }

    private void ClearWorld()
    {
        if (dromeRoot != null) Destroy(dromeRoot);
        dromeRoot = null;
        drome = null;
        foreach (var obj in worldObjects)
        {
            if (obj != null) Destroy(obj);
        }
        worldObjects.Clear();
        homeSkaters = new Skater[4];
        awaySkaters = new Skater[4];
    }

    private void BuildMenuWorld()
    {
        ClearWorld();
        var next = league.NextOpponent;
        homeTeam = league.PlayerTeam;
        awayTeam = next != null ? next : TeamCatalog.Teams[0];
        arena = ArenaCatalog.ForTeam(awayTeam);
        drome = DromeBuilder.Build(transform, arena, homeTeam.primary, awayTeam.primary);
        dromeRoot = drome.root != null ? drome.root.gameObject : null;
        cam = Camera.main;
        if (cam != null) cam.transform.position = new Vector3(0f, 26f, -44f);
        RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.68f);
        SimulateMenuCamera();
        Debug.Log("UBLADE2_HYPERBLADE_WORLD team=" + homeTeam.id + " opponent=" + awayTeam.id + " arena=" + arena.env + " props=" + (drome.props != null ? drome.props.Length : 0));
    }

    private void BeginMatch(bool autoplay, float periodSeconds)
    {
        ClearWorld();
        var next = ranked ? league.NextOpponent : null;
        homeTeam = league.PlayerTeam;
        awayTeam = next != null ? next : TeamCatalog.Teams[(Array.IndexOf(TeamCatalog.Teams, homeTeam) + 1) % TeamCatalog.Teams.Length];
        arena = ArenaCatalog.ForTeam(awayTeam);
        drome = DromeBuilder.Build(transform, arena, homeTeam.primary, awayTeam.primary);
        dromeRoot = drome.root != null ? drome.root.gameObject : null;
        cam = Camera.main;
        if (cam == null)
        {
            var camObject = new GameObject("Main Camera");
            camObject.tag = "MainCamera";
            cam = camObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
        cam.fieldOfView = 85f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1200f;
        cam.transform.position = new Vector3(0f, 11.5f, -22f);
        RenderSettings.ambientLight = new Color(0.72f, 0.74f, 0.78f);

        var matchConfig = new MatchConfig
        {
            homeTeam = homeTeam,
            awayTeam = awayTeam,
            arena = arena,
            periods = 3,
            periodSeconds = periodSeconds,
            difficulty = 2
        };
        match = new MatchRules(matchConfig);
        homeAi = new AiController(2);
        awayAi0 = new AiController(2);
        awayAi1 = new AiController(2);
        autopilotAi = new AiController(2);
        match.GoalScored += OnGoalScored;
        match.MatchEnded += OnMatchEnded;
        match.PeriodEnded += OnPeriodEnded;
        rok = new RokSystem(drome.rokSpawn);
        stats = new MatchStats(homeTeam, awayTeam);
        var homeGoalPosition = new Vector3(0f, 0.05f, drome != null ? drome.goalSouthMouth.z + 0.7f : -arena.radiusZ + 2.0f);
        var awayGoalPosition = new Vector3(0f, 0.05f, drome != null ? drome.goalNorthMouth.z - 0.7f : arena.radiusZ - 2.0f);
        homeGoalie = new GoalieSystem(homeTeam.goalie, true, homeGoalPosition, 4.2f);
        awayGoalie = new GoalieSystem(awayTeam.goalie, false, awayGoalPosition, 4.2f);
        homeGoalie.SaveMade += saved => OnSave(saved, true);
        awayGoalie.SaveMade += saved => OnSave(saved, false);

        var homeGoal = homeGoalPosition;
        var awayGoal = awayGoalPosition;
        for (int i = 0; i < 4; i++)
        {
            homeSkaters[i] = CreateSkater(true, i);
            awaySkaters[i] = CreateSkater(false, i);
        }
        ActivateLine(homeSkaters, 0, 1);
        ActivateLine(awaySkaters, 0, 1);
        homeGoalieVisual = CreateGoalieVisual(true, homeTeam.goalie, homeGoal);
        awayGoalieVisual = CreateGoalieVisual(false, awayTeam.goalie, awayGoal);
        ResetKickoffPositions();
        match.BeginMatch();
        lastPhase = match.Phase;
        matchFrames = 0;
        resultResolved = false;
        feed.Clear();
        banner = "PERIOD 1";
        bannerTimer = 1.5f;
        screen = GameScreen.Match;
        if (audioReady)
        {
            GameAudio.ApplySettings(settings);
            GameAudio.StartMusic(Array.IndexOf(ArenaCatalog.Arenas, arena));
            GameAudio.PlayWhistle();
        }
        UpdateHud();
        Debug.Log("UBLADE2_MATCH_START home=" + homeTeam.id + " away=" + awayTeam.id + " arena=" + arena.env +
                  " periods=3 periodSeconds=" + periodSeconds + " ranked=" + ranked + " autoplay=" + autoplay);
        if (!autoplay) screen = GameScreen.Match;
    }

    private Skater CreateSkater(bool home, int rosterIndex)
    {
        var team = home ? homeTeam : awayTeam;
        var definition = team.skaters[rosterIndex % team.skaters.Length];
        var go = new GameObject((home ? "Home_" : "Away_") + definition.name);
        go.transform.position = new Vector3(home ? -7f : 7f, 0.05f, home ? -6f : 6f);
        worldObjects.Add(go);
        var skater = new Skater
        {
            go = go,
            transform = go.transform,
            home = home,
            rosterIndex = rosterIndex,
            name = definition.name,
            active = false
        };
        skater.rig = DromerModel.Build(go.transform, team.primary, team.secondary, false);
        go.SetActive(false);
        return skater;
    }

    private void ActivateLine(Skater[] line, int first, int second)
    {
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == null) continue;
            line[i].active = i == first || i == second;
            if (line[i].go != null) line[i].go.SetActive(line[i].active);
        }
    }

    private GameObject CreateGoalieVisual(bool home, GoalieDefinition goalie, Vector3 position)
    {
        var go = new GameObject((home ? "HomeGoalie_" : "AwayGoalie_") + goalie.name);
        go.transform.position = position;
        DromerModel.Build(go.transform, home ? homeTeam.primary : awayTeam.primary, home ? homeTeam.secondary : awayTeam.secondary, true);
        worldObjects.Add(go);
        return go;
    }

    private void ResetKickoffPositions()
    {
        if (homeAi != null) homeAi.Reset();
        if (awayAi0 != null) awayAi0.Reset();
        if (awayAi1 != null) awayAi1.Reset();
        if (autopilotAi != null) autopilotAi.Reset();
        if (homeGoalieVisual != null && arena != null) homeGoalieVisual.transform.position = new Vector3(0f, 0.05f, drome != null ? drome.goalSouthMouth.z + 1.2f : -arena.radiusZ + 2f);
        if (awayGoalieVisual != null && arena != null) awayGoalieVisual.transform.position = new Vector3(0f, 0.05f, drome != null ? drome.goalNorthMouth.z - 1.2f : arena.radiusZ - 2f);
        if (homeSkaters[0] != null) PlaceSkater(homeSkaters[0], new Vector3(-9f, 0.05f, -20f));
        if (homeSkaters[1] != null) PlaceSkater(homeSkaters[1], new Vector3(9f, 0.05f, -24f));
        if (awaySkaters[0] != null) PlaceSkater(awaySkaters[0], new Vector3(-9f, 0.05f, 20f));
        if (awaySkaters[1] != null) PlaceSkater(awaySkaters[1], new Vector3(9f, 0.05f, 24f));
        foreach (var skater in homeSkaters) if (skater != null) { skater.health = 150f; skater.stun = 0f; skater.downed = false; if (skater.go != null && skater.active) skater.go.SetActive(true); }
        foreach (var skater in awaySkaters) if (skater != null) { skater.health = 150f; skater.stun = 0f; skater.downed = false; if (skater.go != null && skater.active) skater.go.SetActive(true); }
        if (rok != null) rok.Reset(drome != null ? drome.rokSpawn : new Vector3(0f, 0.5f, 0f));
        if (homeGoalie != null) homeGoalie.Reset(new Vector3(0f, 0.05f, drome != null ? drome.goalSouthMouth.z + 1.2f : -arena.radiusZ + 2f));
        if (awayGoalie != null) awayGoalie.Reset(new Vector3(0f, 0.05f, drome != null ? drome.goalNorthMouth.z - 1.2f : arena.radiusZ - 2f));
        multiplierLevel = 1;
        multiplierArmed = false;
        multiplierOwner = 0;
        chargingShot = false;
        playerPower = 0f;
    }

    private void PlaceSkater(Skater skater, Vector3 position)
    {
        if (skater.controller != null) skater.controller.enabled = false;
        skater.transform.position = position;
        if (skater.controller != null) skater.controller.enabled = true;
    }

    // ---------------------------------------------------------------- update

    private void Update()
    {
        if (!worldReady) return;
        if (demo) return;
        if (matchMode || captureAutoplay) return;
        if (screen == GameScreen.Match)
        {
            GameInput.Update();
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                screen = GameScreen.Paused;
                return;
            }
            StepMatch(Time.deltaTime, false);
            if (playTest && matchFrames % 30 == 0)
            {
                Debug.Log("UBLADE2_DEBUG_INPUT frame=" + matchFrames + " move=" + GameInput.Move + " player=" + homeSkaters[0].transform.position);
            }
        }
        else if (screen == GameScreen.Paused && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
        {
            screen = GameScreen.Match;
        }
        else
        {
            GameInput.Update();
            SimulateMenuCamera();
        }
    }

    private void SimulateMenuCamera()
    {
        if (cam == null) return;
        var t = Time.time * 0.05f;
        cam.transform.position = new Vector3(Mathf.Sin(t) * 28f, 22f, -44f);
        cam.transform.LookAt(Vector3.zero);
        cam.fieldOfView = 85f;
    }

    private void StepMatch(float dt, bool autopilot)
    {
        if (match == null) return;
        matchFrames++;
        match.Update(dt);
        if (match.Phase == MatchPhase.Kickoff && lastPhase != MatchPhase.Kickoff)
        {
            ResetKickoffPositions();
            banner = match.Period > 1 ? "PERIOD " + match.Period : "KICKOFF";
            bannerTimer = 1.2f;
        }
        lastPhase = match.Phase;

        if (match.Phase == MatchPhase.Playing || match.Phase == MatchPhase.GoalFlash)
        {
            UpdateSkaterInput(dt, autopilot, autopilotAi);
            UpdateAiSkater(homeSkaters[1], dt, true, 1, homeAi);
            UpdateAiSkater(awaySkaters[0], dt, false, 0, awayAi0);
            UpdateAiSkater(awaySkaters[1], dt, false, 1, awayAi1);
            foreach (var skater in homeSkaters) if (skater != null && skater.hitCooldown > 0f) skater.hitCooldown -= dt;
            foreach (var skater in awaySkaters) if (skater != null && skater.hitCooldown > 0f) skater.hitCooldown -= dt;
            UpdateRokPlay(dt);
            UpdateGoalie(homeGoalie, dt, true);
            UpdateGoalie(awayGoalie, dt, false);
            CheckGoal();
            UpdateHazards(dt);
            if (speedBurstMeter < 1f && !speedburst)
            {
                speedBurstMeter = Mathf.Min(1f, speedBurstMeter + dt * 0.18f);
            }
        }

        if (matchFrames % 120 == 0)
        {
            var renderers = drome.root != null ? drome.root.GetComponentsInChildren<Renderer>() : new Renderer[0];
            Debug.Log("UBLADE2_DEBUG_STEP frame=" + matchFrames + " screen=" + screen + " camNull=" + (cam == null) + " camPos=" + (cam != null ? cam.transform.position.ToString() : "null") + " camEnabled=" + (cam != null && cam.enabled) + " target=" + (homeSkaters[0] != null ? homeSkaters[0].transform.position.ToString() : "null") + " drome=" + (drome.root != null ? drome.root.name : "NULL") + " renderers=" + renderers.Length + " radius=" + arena.radiusX + "x" + arena.radiusZ);
        }
        if (matchFrames % 240 == 0 && rok != null)
        {
            var carrier = CarrierSkater(rok.CarrierIndex);
            Debug.Log("UBLADE2_DEBUG_STATE frame=" + matchFrames + " phase=" + match.Phase + " rokState=" + rok.State + " carrier=" + (carrier != null ? carrier.name : "-") + " score=" + match.HomeScore + "-" + match.AwayScore);
        }
        if (screen == GameScreen.Match && match.MatchOver && !resultResolved)
        {
            FinishMatch();
        }
        UpdateHud();
    }

    private void UpdateSkaterInput(float dt, bool autopilot, AiController ai)
    {
        var skater = homeSkaters[0];
        if (skater == null || skater.downed) return;
        var definition = homeTeam.skaters[0];
        if (autopilot)
        {
            Vector3 move; bool attack; bool shoot; bool dash;
            var homeGoal = new Vector3(0f, 0.05f, drome != null ? drome.goalSouthMouth.z + 1.2f : -arena.radiusZ + 2f);
            var awayGoal = new Vector3(0f, 0.05f, drome != null ? drome.goalNorthMouth.z - 1.2f : arena.radiusZ - 2f);
            ai.Think(dt, skater.transform.position, awayGoal, homeGoal, rok != null ? rok.Position : Vector3.zero,
                rok != null && rok.State == RokState.Loose, HasRok(skater), OpponentHasRok(),
                OpponentCarrierPosition(), out move, out attack, out shoot, out dash);
            MovePlayer(skater, move, dt);
            if (attack) TryAttack(skater, FindOpponentNear(skater));
            if (shoot) ShootRok(skater, awayGoal);
            return;
        }
        var input = GameInput.Move;
        var forward = Vector3.ProjectOnPlane(cam != null ? cam.transform.forward : Vector3.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        var right = Vector3.Cross(Vector3.up, forward);
        var horizontal = forward * input.y + right * input.x;
        var hasRok = HasRok(skater);
        if (hasRok)
        {
            horizontal *= 0.85f;
            if (Input.GetKey(KeyCode.Space))
            {
                chargingShot = true;
                playerPower = Mathf.Min(1f, playerPower + dt * 1.4f);
            }
            if (chargingShot && Input.GetKeyUp(KeyCode.Space))
            {
                var target = new Vector3(UnityEngine.Random.Range(-2.2f, 2.2f), 0.05f, arena.radiusZ - 2f);
                ShootRok(skater, target);
                chargingShot = false;
                playerPower = 0f;
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                PassRok(skater, homeSkaters[1]);
            }
        }
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.D)) && speedBurstMeter > 0.05f)
        {
            speedburst = true;
            speedBurstMeter = Mathf.Max(0f, speedBurstMeter - dt * 0.9f);
        }
        else
        {
            speedburst = false;
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetMouseButtonDown(1))
        {
            TryAttack(skater, FindOpponentNear(skater));
        }
        if (Input.GetKeyDown(KeyCode.F) && HasRok(skater) && !chargingShot)
        {
            var quickTarget = new Vector3(UnityEngine.Random.Range(-2.2f, 2.2f), 0.05f, arena.radiusZ - 2f);
            ShootRok(skater, quickTarget);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (skater.controller != null && skater.controller.isGrounded) vy = 6.2f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Substitute(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Substitute(3);
        }
        MovePlayer(skater, horizontal, dt);
        if (skater.rig != null) skater.rig.SetMotion(horizontal.magnitude * BaseSpeed);
    }

    private void MovePlayer(Skater skater, Vector3 direction, float dt)
    {
        if (skater == null || skater.downed) return;
        var definition = homeTeam.skaters[0];
        var speed = BaseSpeed * (0.7f + definition.speed * 0.05f) * (speedburst ? 1.55f : 1f) * 1.25f;
        var planar = new Vector3(direction.x, 0f, direction.z);
        if (planar.sqrMagnitude > 0.0001f)
        {
            planar.Normalize();
            skater.transform.rotation = Quaternion.LookRotation(planar, Vector3.up);
        }
        if (skater.controller != null && skater.controller.enabled)
        {
            if (skater.controller.isGrounded && vy < 0f) vy = -2f;
            vy -= 9.81f * dt;
            skater.controller.Move(planar * speed * dt + Vector3.up * vy * dt);
        }
        else
        {
            skater.transform.position += planar * speed * dt;
        }
        ClampToDrome(skater.transform);
    }

    private void ClampToDrome(Transform target)
    {
        if (arena == null) return;
        var position = target.position;
        var nx = position.x / (arena.radiusX - 1.3f);
        var nz = position.z / (arena.radiusZ - 1.8f);
        var length = Mathf.Sqrt(nx * nx + nz * nz);
        if (length > 1f)
        {
            nx /= length;
            nz /= length;
            position.x = nx * (arena.radiusX - 1.3f);
            position.z = nz * (arena.radiusZ - 1.8f);
        }
        position.y = 0.05f + DromeBuilder.SurfaceY(position.x, position.z, arena.radiusX, arena.radiusZ);
        target.position = position;
    }

    private void UpdateAiSkater(Skater skater, float dt, bool isHome, int index, AiController ai)
    {
        if (skater == null || skater.downed || !skater.active) return;
        var homeGoal = new Vector3(0f, 0.05f, -arena.radiusZ + 2f);
        var awayGoal = new Vector3(0f, 0.05f, arena.radiusZ - 2f);
        var attackGoal = isHome ? awayGoal : homeGoal;
        var ownGoal = isHome ? homeGoal : awayGoal;
        Vector3 move; bool attack; bool shoot; bool dash;
        ai.Think(dt, skater.transform.position, attackGoal, ownGoal, rok != null ? rok.Position : Vector3.zero,
            rok != null && rok.State == RokState.Loose, HasRok(skater), OpponentHasRok(),
            OpponentCarrierPosition(), out move, out attack, out shoot, out dash);
        var definition = (isHome ? homeTeam : awayTeam).skaters[index % 4];
        var speed = BaseSpeed * (0.65f + definition.speed * 0.05f) * (dash ? 1.35f : 1f) * 1.2f;
        if (move.sqrMagnitude > 0.001f && skater.transform != null)
        {
            skater.transform.position += move * speed * dt;
            skater.transform.rotation = Quaternion.LookRotation(move, Vector3.up);
            ClampToDrome(skater.transform);
        }
        if (skater.rig != null) skater.rig.SetMotion(move.magnitude * speed);
        if (attack) TryAttack(skater, FindOpponentNear(skater));
        if (shoot) ShootRok(skater, attackGoal);
    }

    private bool HasRok(Skater skater)
    {
        return rok != null && rok.State == RokState.Carried && rok.CarrierIndex >= 0 && CarrierSkater(rok.CarrierIndex) == skater;
    }

    private bool OpponentHasRok()
    {
        return rok != null && rok.State == RokState.Carried && CarrierSkater(rok.CarrierIndex) != null;
    }

    private Vector3 OpponentCarrierPosition()
    {
        var carrier = CarrierSkater(rok != null ? rok.CarrierIndex : -1);
        return carrier != null ? carrier.transform.position : Vector3.zero;
    }

    private Skater CarrierSkater(int carrierIndex)
    {
        if (carrierIndex < 0) return null;
        if (carrierIndex < 4) return homeSkaters[carrierIndex];
        return awaySkaters[carrierIndex - 4];
    }

    private Skater FindOpponentNear(Skater attacker)
    {
        Skater best = null;
        var bestDistance = float.MaxValue;
        foreach (var other in attacker.home ? awaySkaters : homeSkaters)
        {
            if (other == null || other.downed || !other.active) continue;
            var distance = HorizontalDistance(attacker.transform.position, other.transform.position);
            if (distance < 2.6f && distance < bestDistance) { bestDistance = distance; best = other; }
        }
        return best;
    }

    private void TryAttack(Skater attacker, Skater target)
    {
        if (attacker == null || target == null || attacker.attackCooldown > 0f || target.downed) return;
        attacker.attackCooldown = 0.9f;
        var team = attacker.home ? homeTeam : awayTeam;
        var definition = team.skaters[attacker.rosterIndex % 4];
        var damage = 8f + definition.checking * 0.4f + definition.damage * 0.015f;
        target.health -= damage;
        target.stun = 0.7f + definition.checking * 0.02f;
        if (target.go != null) target.transform.position += (target.transform.position - attacker.transform.position).normalized * 0.9f;
        if (target.rig != null) target.rig.Slash();
        ClampToDrome(target.transform);
        banner = "JAK HIT!";
        bannerTimer = 0.7f;
        if (audioReady) GameAudio.PlayHit(!attacker.home);
        stats.RecordInjury(attacker.home);
        if (target.health <= 0f)
        {
            target.health = 0f;
            target.downed = true;
            if (target.go != null) target.go.SetActive(false);
            if (HasRok(target)) rok.Drop();
            stats.RecordFatal(attacker.home);
            AddFeed((attacker.home ? "WAYFARERS" : "RIVALS") + " downed " + target.name);
        }
    }

    private void UpdateRokPlay(float dt)
    {
        if (rok == null) return;
        rok.Update(dt, true, arena.radiusX, arena.radiusZ);
        var rokFloor = DromeBuilder.SurfaceY(rok.Position.x, rok.Position.z, arena.radiusX, arena.radiusZ);
        if (rok.State != RokState.InGoal && rok.Position.y < rokFloor + 0.36f) { rok.Position.y = rokFloor + 0.36f; }
        if (rok.State == RokState.Loose || rok.State == RokState.InFlight)
        {
            var homeWasFlying = rok.State == RokState.InFlight;
            foreach (var skater in homeSkaters)
            {
                if (skater == null || skater.downed || !skater.active) continue;
                if (rok.TryPickup(skater.rosterIndex, true, skater.transform.position, 2.0f))
                {
                    Debug.Log("UBLADE2_DEBUG_PICKUP home index=" + skater.rosterIndex + " name=" + skater.name + " frame=" + matchFrames);
                    if (audioReady) GameAudio.PlayCollect(1);
                    banner = "ROK: " + skater.name;
                    bannerTimer = 0.8f;
                    stats.homeActiveDromer = skater.rosterIndex;
                    if (homeWasFlying && !rok.CarrierIsPlayerTeam) stats.RecordInterception(true);
                    break;
                }
            }
            if (rok.State == RokState.Loose || rok.State == RokState.InFlight)
            {
                var awayWasFlying = rok.State == RokState.InFlight;
                foreach (var skater in awaySkaters)
                {
                    if (skater == null || skater.downed || !skater.active) continue;
                    if (rok.TryPickup(4 + skater.rosterIndex, false, skater.transform.position, 2.0f))
                    {
                        Debug.Log("UBLADE2_DEBUG_PICKUP away index=" + skater.rosterIndex + " name=" + skater.name + " frame=" + matchFrames);
                        banner = "ROK: " + skater.name;
                        bannerTimer = 0.8f;
                        stats.awayActiveDromer = skater.rosterIndex;
                        if (awayWasFlying && rok.CarrierIsPlayerTeam) stats.RecordInterception(false);
                        break;
                    }
                }
            }
        }
        var carrier = CarrierSkater(rok.CarrierIndex);
        if (carrier != null)
        {
            if (carrier.downed || !carrier.active)
            {
                rok.Drop();
            }
            else
            {
                rok.TrackCarrier(carrier.transform.position, carrier.transform.forward);
                stats.UpdatePossession(carrier.home, dt);
            }
        }
    }

    private void ShootRok(Skater skater, Vector3 target)
    {
        if (skater == null || rok == null || !HasRok(skater)) return;
        var team = skater.home ? homeTeam : awayTeam;
        var definition = team.skaters[skater.rosterIndex % 4];
        var power = 0.55f + definition.shotPower * 0.035f;
        var accuracy = 0.4f + definition.shotAccuracy * 0.05f;
        rok.Shoot(skater.transform.position, target, power, accuracy);
        Debug.Log("UBLADE2_DEBUG_SHOT by=" + skater.name + " home=" + skater.home + " power=" + power + " frame=" + matchFrames);
        if (skater.home) { stats.homeActiveDromer = skater.rosterIndex; stats.awayActiveDromer = -1; }
        else { stats.awayActiveDromer = skater.rosterIndex; stats.homeActiveDromer = -1; }
        stats.RecordShot(skater.home, false, 1, multiplierArmed);
        stats.RecordSave(!skater.home, false);
        if (audioReady) GameAudio.PlayHit(false);
        banner = "SHOT!";
        bannerTimer = 0.6f;
        chargingShot = false;
        playerPower = 0f;
    }

    private void PassRok(Skater skater, Skater teammate)
    {
        if (skater == null || teammate == null || !HasRok(skater)) return;
        var team = skater.home ? homeTeam : awayTeam;
        var definition = team.skaters[skater.rosterIndex % 4];
        rok.Pass(skater.transform.position, teammate.transform.position, 0.5f + definition.passAccuracy * 0.05f);
        if (multiplierArmed) stats.RecordMultiplierPass(skater.home);
        banner = "PASS";
        bannerTimer = 0.5f;
    }

    private void CheckGoal()
    {
        if (rok == null) return;
        bool northGoal;
        int scoringTeam;
        var northMouth = drome != null ? new Vector3(drome.goalNorthMouth.x, 1.0f, drome.goalNorthMouth.z) : new Vector3(0f, 1.0f, arena.radiusZ - 2f);
        var southMouth = drome != null ? new Vector3(drome.goalSouthMouth.x, 1.0f, drome.goalSouthMouth.z) : new Vector3(0f, 1.0f, -arena.radiusZ + 2f);
        if (rok.TryGoal(northMouth, southMouth, out northGoal, out scoringTeam))
        {
            var homeScored = northGoal;
            Debug.Log("UBLADE2_DEBUG_GOAL north=" + northGoal + " scoringTeam=" + scoringTeam + " rok=" + rok.Position);
            var points = multiplierLevel;
            stats.RecordGoal(homeScored, points, multiplierArmed);
            match.Goal(homeScored, points);
            AddFeed((homeScored ? "WAYFARERS" : "RIVALS") + " GOAL +" + points);
            banner = homeScored ? "GOAL! WAYFARERS +" + points : "GOAL! RIVALS +" + points;
            bannerTimer = 1.4f;
            if (audioReady) GameAudio.PlayVictory();
            multiplierLevel = 1;
            multiplierArmed = false;
            StartCoroutine(ResetAfterGoal());
            // NOTE: the original Wipe Out Rule needs all four opposing forwards killed. This slice
            // keeps the downed players out until the next goal/period instead of ending the match early.

        }
    }

    private IEnumerator ResetAfterGoal()
    {
        yield return new WaitForSeconds(MatchRules.GoalFlashSeconds);
        ResetKickoffPositions();
    }

    private void UpdateGoalie(GoalieSystem goalie, float dt, bool home)
    {
        if (goalie == null || rok == null) return;
        var ownGoal = new Vector3(0f, 0.05f, home ? -arena.radiusZ + 2f : arena.radiusZ - 2f);
        goalie.Update(dt, rok.Position, rok.State, home ? rok.Position.z < 0f : rok.Position.z > 0f);
        var visual = home ? homeGoalieVisual : awayGoalieVisual;
        if (visual != null) visual.transform.position = new Vector3(goalie.Position.x, 0.05f, goalie.Position.z);
        if (rok.State == RokState.InFlight)
        {
            var headingHome = home ? rok.Velocity.z < 0f : rok.Velocity.z > 0f;
            if (headingHome && HorizontalDistance(rok.Position, goalie.Position) < 3.8f)
            {
                if (goalie.TrySave(rok.Position, ownGoal, rok.Velocity.magnitude))
                {
                    rok.Reset(goalie.Position + new Vector3(0f, 0.4f, home ? 1.2f : -1.2f));
                    savedFlashTimer = 1.0f;
                }
            }
        }
    }

    private void OnSave(bool saved, bool home)
    {
        if (!saved) return;
        if (stats != null)
        {
            if (home && stats.HomeGoalie != null) stats.HomeGoalie.saves++;
            else if (!home && stats.AwayGoalie != null) stats.AwayGoalie.saves++;
        }
        banner = "SAVE!";
        bannerTimer = 0.9f;
        savedFlashTimer = 1.0f;
    }

    private void OnPeriodEnded(int period)
    {
        var ads = new[] { "NoBones: NoBones About It!", "SPECSAVERS: See More", "HUGE! for men", "HELLMET: Pain Is Pleasure" };
        banner = "END OF PERIOD " + period + " - " + ads[(period - 1) % ads.Length];
        bannerTimer = 2.5f;
        AddFeed("END OF PERIOD " + period);
        if (audioReady) GameAudio.PlayWhistle();
        StartCoroutine(ResetAfterGoal());
    }

    private bool AllActiveDowned(bool home)
    {
        var line = home ? homeSkaters : awaySkaters;
        foreach (var skater in line)
        {
            if (skater != null && skater.active && !skater.downed) return false;
        }
        return true;
    }

    private void Substitute(int benchRosterIndex)
    {
        var skater = homeSkaters[0];
        if (skater == null || skater.downed) return;
        if (benchRosterIndex < 0 || benchRosterIndex >= homeTeam.skaters.Length) return;
        skater.rosterIndex = benchRosterIndex;
        skater.name = homeTeam.skaters[benchRosterIndex].name;
        skater.health = 150f;
        banner = "SUB: " + skater.name;
        bannerTimer = 1f;
        AddFeed("SUB " + skater.name);
    }

    private void UpdateHazards(float dt)
    {
        hazardCooldown -= dt;
        if (hazardCooldown > 0f) return;
        if (drome == null || drome.props == null) return;
        foreach (var prop in drome.props)
        {
            var position = new Vector3(prop.position.x, 0.05f, prop.position.y);
            if (prop.kind == DromePropKind.Multiplier)
            {
                if (rok != null && rok.State == RokState.Carried)
                {
                    var carrier = CarrierSkater(rok.CarrierIndex);
                    if (carrier != null && HorizontalDistance(carrier.transform.position, position) < 2.2f)
                    {
                        var owner = carrier.home ? 1 : 2;
                        if (multiplierLevel > 1 && multiplierOwner != 0 && owner != multiplierOwner)
                        {
                            multiplierLevel = 1;
                            multiplierArmed = false;
                            banner = "MULTIPLIER NULLIFIED";
                        }
                        else
                        {
                            multiplierLevel = multiplierLevel < 2 ? 2 : 4;
                            multiplierOwner = owner;
                            multiplierArmed = true;
                            banner = "MULTIPLIER x" + multiplierLevel;
                        }
                        multiplierOwner = owner;
                        bannerTimer = 1.2f;
                        stats.RecordMultiplierPass(carrier.home);
                    }
                }
                continue;
            }
            if (prop.kind == DromePropKind.Randomizer || prop.kind == DromePropKind.RedRandomizer)
            {
                continue;
            }
            if (prop.kind == DromePropKind.Ramp || prop.kind == DromePropKind.Armory)
            {
                continue;
            }
            foreach (var skater in homeSkaters)
            {
                if (skater == null || skater.downed || !skater.active) continue;
                if (skater.hitCooldown > 0f) continue;
                if (HorizontalDistance(skater.transform.position, position) < 1.8f)
                {
                    skater.health -= 6f;
                    skater.stun = 0.6f;
                    skater.hitCooldown = 1.4f;
                    hazardCooldown = 0.8f;
                    banner = "DROME HAZARD";
                    bannerTimer = 0.7f;
                    if (skater.health <= 0f)
                    {
                        skater.downed = true;
                        if (skater.go != null) skater.go.SetActive(false);
                        stats.RecordInjury(true);
                    }
                    break;
                }
            }
        }
    }

    private void OnGoalScored(bool homeScored, int points)
    {
        if (audioReady) GameAudio.PlayWhistle();
    }

    private void OnMatchEnded(int winner)
    {
        if (resultResolved) return;
        resultResolved = true;
        screen = GameScreen.Result;
        var homeWon = winner == 1;
        if (ranked && league != null)
        {
            if (homeWon)
            {
                league.ResolveMatch(true);
            }
            else
            {
                league.BeginSeason();
            }
            league.Save();
        }
        ranked = false;
        uiModel.hud.resultText = winner == 1 ? "VICTORY" : winner == 2 ? "DEFEAT" : "DRAW";
        if (stats != null)
        {
            if (homeGoalie != null) { stats.HomeGoalie.shotsAgainst = homeGoalie.ShotsAgainst; stats.HomeGoalie.saves = homeGoalie.Saves; }
            if (awayGoalie != null) { stats.AwayGoalie.shotsAgainst = awayGoalie.ShotsAgainst; stats.AwayGoalie.saves = awayGoalie.Saves; }
        }
        uiModel.hud.statsLines = stats != null ? stats.DromerLines() : new string[0];
        uiModel.hud.goalieStatsLines = stats != null ? stats.GoalieLines() : new string[0];
        if (audioReady)
        {
            if (winner == 1) GameAudio.PlayVictory();
            else if (winner == 2) GameAudio.PlayDefeat();
        }
        AddFeed("FINAL " + homeTeam.displayName + " " + match.HomeScore + " - " + match.AwayScore + " " + awayTeam.displayName);
        UpdateHud();
        Debug.Log("UBLADE2_MATCH_END home=" + match.HomeScore + " away=" + match.AwayScore + " winner=" + winner +
                  " frames=" + matchFrames + " periods=" + match.Period);
    }

    private void FinishMatch()
    {
        if (resultResolved) return;
        OnMatchEnded(match.Winner);
        uiModel.hud.statsLines = stats != null ? stats.DromerLines() : new string[0];
        uiModel.hud.goalieStatsLines = stats != null ? stats.GoalieLines() : new string[0];
    }

    private void UpdateHud()
    {
        if (match == null) return;
        var hud = uiModel.hud;
        hud.playerTeam = homeTeam.displayName;
        hud.aiTeam = awayTeam.displayName;
        hud.playerColor = homeTeam.primary;
        hud.aiColor = awayTeam.primary;
        hud.playerScore = match.HomeScore;
        hud.aiScore = match.AwayScore;
        hud.period = match.Period;
        hud.totalPeriods = match.Config.periods;
        hud.periodTime = match.PeriodTime;
        hud.timeLeft = match.PeriodTime;
        hud.round = match.Period;
        hud.totalRounds = match.Config.periods;
        hud.arenaName = arena != null ? arena.displayName : "";
        hud.rokCarrier = rok != null && rok.State == RokState.Carried && CarrierSkater(rok.CarrierIndex) != null ? CarrierSkater(rok.CarrierIndex).name : "LOOSE";
        hud.playerHasRok = rok != null && rok.State == RokState.Carried && rok.CarrierIndex >= 0 && rok.CarrierIndex < 4;
        hud.shotPower = playerPower;
        hud.speedBurst = speedBurstMeter;
        hud.multiplier = multiplierLevel;
        hud.goalieSaved = savedFlashTimer > 0f;
        hud.banner = banner;
        hud.matchOver = match.MatchOver;
        hud.rosterNames = new string[homeTeam.skaters.Length];
        hud.rosterHealth = new float[homeTeam.skaters.Length];
        for (int i = 0; i < homeTeam.skaters.Length; i++)
        {
            hud.rosterNames[i] = homeTeam.skaters[i].name;
            var skater = homeSkaters[i];
            hud.rosterHealth[i] = skater != null ? Mathf.Clamp01(skater.health / 150f) : 0f;
        }
        hud.activePlayer = 0;
        hud.eventFeed = feed.ToArray();
        hud.difficulty = match.Config.difficulty;
        if (savedFlashTimer > 0f) savedFlashTimer -= Time.deltaTime;
        if (bannerTimer > 0f) bannerTimer -= Time.deltaTime;
        if (bannerTimer <= 0f) banner = "";
        uiModel.screen = screen;
        uiModel.settings = settings;
        uiModel.seasonWins = league.Wins;
        uiModel.seasonLosses = league.Losses;
        uiModel.seasonPoints = league.Points;
        uiModel.seasonComplete = league.SeasonComplete;
        uiModel.leagueLines = league.TableLines();
        uiModel.nextOpponent = league.NextOpponent != null ? league.NextOpponent.displayName : "";
        uiModel.touch = Application.isMobilePlatform;
    }

    private void AddFeed(string line)
    {
        feed.Add(line);
        while (feed.Count > 6) feed.RemoveAt(0);
    }

    // ---------------------------------------------------------------- modes

    private void BuildMenuUi()
    {
        uiModel.screen = screen;
        uiModel.settings = settings;
        uiModel.seasonWins = league.Wins;
        uiModel.seasonLosses = league.Losses;
        uiModel.seasonPoints = league.Points;
        uiModel.seasonComplete = league.SeasonComplete;
        uiModel.leagueLines = league.TableLines();
        uiModel.nextOpponent = league.NextOpponent != null ? league.NextOpponent.displayName : "";
        uiModel.hud.playerTeam = homeTeam != null ? homeTeam.displayName : "WAYFARERS";
        uiModel.hud.aiTeam = awayTeam != null ? awayTeam.displayName : "RIVALS";
        uiModel.hud.arenaName = arena != null ? arena.displayName : "";
        uiModel.hud.difficulty = settings.difficulty;
        uiModel.touch = Application.isMobilePlatform;
    }

    private void OnGUI()
    {
        if (!worldReady || demo || matchMode) return;
        if (!string.IsNullOrEmpty(capturePath) && !captureAutoplay) return;
        if (match == null) BuildMenuUi();
        if (match == null) UpdateMenuModel();
        var action = GameUI.Draw(uiModel);
        if (screen == GameScreen.Match && Application.isMobilePlatform && settings.touchControls)
        {
            GameInput.DrawTouchControls(settings);
        }
        if (action != UiAction.None)
        {
            if (audioReady) GameAudio.PlayClick();
            HandleAction(action);
        }
    }

    private void UpdateMenuModel()
    {
        uiModel.screen = screen;
        uiModel.settings = settings;
        if (league != null)
        {
            uiModel.seasonWins = league.Wins;
            uiModel.seasonLosses = league.Losses;
            uiModel.seasonPoints = league.Points;
            uiModel.seasonComplete = league.SeasonComplete;
            uiModel.leagueLines = league.TableLines();
            uiModel.nextOpponent = league.NextOpponent != null ? league.NextOpponent.displayName : "";
        }
        uiModel.hud.playerTeam = homeTeam != null ? homeTeam.displayName : "WAYFARERS";
        uiModel.hud.aiTeam = awayTeam != null ? awayTeam.displayName : "RIVALS";
        uiModel.hud.arenaName = arena != null ? arena.displayName : "";
        uiModel.hud.difficulty = settings.difficulty;
        uiModel.touch = Application.isMobilePlatform;
    }

    private void HandleAction(UiAction action)
    {
        switch (action)
        {
            case UiAction.StartMatch:
            case UiAction.StartNextMatch:
                if (league.SeasonComplete)
                {
                    league.ResetSeason();
                    league.BeginSeason();
                    league.Save();
                }
                ranked = true;
                BeginMatch(false, 180f);
                break;
            case UiAction.OpenLeague:
                previousScreen = screen;
                screen = GameScreen.League;
                break;
            case UiAction.OpenSettings:
                previousScreen = screen;
                screen = GameScreen.Settings;
                break;
            case UiAction.Back:
                screen = previousScreen == GameScreen.Settings ? GameScreen.MainMenu : previousScreen;
                break;
            case UiAction.Resume:
                screen = GameScreen.Match;
                break;
            case UiAction.RestartMatch:
                ranked = false;
                BeginMatch(false, 180f);
                break;
            case UiAction.QuitToMenu:
                screen = GameScreen.MainMenu;
                match = null;
                BuildMenuWorld();
                if (audioReady) GameAudio.StartMusic(0);
                break;
            case UiAction.NewSeason:
                league.ResetSeason();
                league.BeginSeason();
                league.Save();
                screen = GameScreen.League;
                break;
            case UiAction.QuitGame:
                SaveProgress();
                Application.Quit(0);
                break;
        }
        UpdateMenuModel();
    }

    private void ApplyDisplaySettings()
    {
        if (Application.isMobilePlatform || settings == null || Application.isBatchMode) return;
        try { Screen.fullScreen = settings.fullscreen; } catch (Exception) { }
    }

    private void SaveProgress()
    {
        if (settings != null) settings.Save("ublade2.settings.");
        if (league != null && !matchMode) league.Save();
    }

    private void OnApplicationQuit()
    {
        if (demo || matchMode || captureAutoplay || leagueTest || !string.IsNullOrEmpty(capturePath) ||
            !string.IsNullOrEmpty(menuCapturePath) || !string.IsNullOrEmpty(uiCaptureDir)) return;
        SaveProgress();
    }

    // ---------------------------------------------------------------- headless/capture

    private IEnumerator RunHeadlessMatch()
    {
        ranked = false;
        BeginMatch(true, 20f);
        while (screen == GameScreen.Match && matchFrames < 20000)
        {
            StepMatch(Time.fixedDeltaTime, true);
            yield return new WaitForFixedUpdate();
        }
        WriteMatchEvidence();
        Debug.Log("UBLADE2_MATCH_OK home=" + match.HomeScore + " away=" + match.AwayScore +
                  " winner=" + match.Winner + " frames=" + matchFrames + " periods=" + match.Config.periods + " phase=" + match.Phase);
        yield return new WaitForSeconds(0.3f);
        Application.Quit(0);
        yield return null;
        ExitProcess(0);
    }

    private IEnumerator RunMatchCapture(string path, bool keepRunning)
    {
        ranked = false;
        screen = GameScreen.Match;
        debugOverview = false;
        BeginMatch(true, 180f);
        while (matchFrames < 430 && screen == GameScreen.Match)
        {
            StepMatch(Time.fixedDeltaTime, true);
            yield return new WaitForFixedUpdate();
        }
        WriteMatchEvidence();
        if (keepRunning)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            yield return new WaitForEndOfFrame();
            try
            {
                var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
                texture.Apply();
                var bytes = texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                Destroy(texture);
                Debug.Log("UBLADE2_MOBILE_CAPTURE_OK path=" + path + " bytes=" + bytes.Length + " screen=" + Screen.width + "x" + Screen.height);
            }
            catch (Exception exception)
            {
                Debug.Log("UBLADE2_MOBILE_CAPTURE_FAIL " + exception);
            }
#endif
            yield break;
        }
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(path);
        yield return new WaitForSeconds(0.9f);
        Debug.Log("UBLADE2_MATCH_CAPTURE_OK path=" + path + " screen=" + Screen.width + "x" + Screen.height + " frames=" + matchFrames);
        yield return new WaitForSeconds(0.4f);
        Application.Quit(0);
        yield return null;
        ExitProcess(0);
    }

    private IEnumerator RunMenuCapture()
    {
        screen = GameScreen.MainMenu;
        yield return new WaitForSeconds(1.2f);
        var folder = Path.GetDirectoryName(menuCapturePath);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(menuCapturePath);
        yield return new WaitForSeconds(0.9f);
        Debug.Log("UBLADE2_MENU_CAPTURE_OK path=" + menuCapturePath + " screen=" + Screen.width + "x" + Screen.height);
        yield return new WaitForSeconds(0.4f);
        Application.Quit(0);
        yield return null;
        ExitProcess(0);
    }

    private IEnumerator RunUiCapture()
    {
        var folder = uiCaptureDir;
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        screen = GameScreen.MainMenu; UpdateMenuModel();
        yield return new WaitForSeconds(0.8f);
        yield return CaptureUiFrame("menu.png");
        screen = GameScreen.League; UpdateMenuModel(); yield return new WaitForSeconds(0.4f); yield return CaptureUiFrame("league.png");
        screen = GameScreen.Settings; UpdateMenuModel(); yield return new WaitForSeconds(0.4f); yield return CaptureUiFrame("settings.png");
        ranked = false;
        screen = GameScreen.Match;
        BeginMatch(true, 180f);
        for (int i = 0; i < 40; i++) StepMatch(Time.fixedDeltaTime, true);
        yield return new WaitForSeconds(0.3f); yield return CaptureUiFrame("match.png");
        screen = GameScreen.Paused; yield return new WaitForSeconds(0.4f); yield return CaptureUiFrame("pause.png");
        uiModel.hud.resultText = "VICTORY";
        uiModel.hud.statsLines = stats != null ? stats.DromerLines() : new string[0];
        uiModel.hud.goalieStatsLines = stats != null ? stats.GoalieLines() : new string[0];
        screen = GameScreen.Result; yield return new WaitForSeconds(0.4f); yield return CaptureUiFrame("result.png");
        Debug.Log("UBLADE2_UI_CAPTURE_OK dir=" + folder + " count=6 screen=" + Screen.width + "x" + Screen.height);
        yield return new WaitForSeconds(0.3f);
        Application.Quit(0);
        yield return null;
        ExitProcess(0);
    }

    private IEnumerator CaptureUiFrame(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(uiCaptureDir, name));
        yield return new WaitForSeconds(0.6f);
    }

    private IEnumerator RunLeagueTest()
    {
        var probe = new LeagueData();
        probe.SetPlayerTeam("tokyo");
        probe.ResetSeason();
        probe.BeginSeason();
        probe.ResolveMatch(true);
        probe.Save();
        var loaded = LeagueData.Load();
        var passed = loaded.Round == 1 && loaded.Wins == 1 && loaded.Points >= 3 && loaded.PlayerTeamId == "tokyo";
        var evidence = new LeagueEvidence
        {
            passed = passed,
            round = loaded.Round,
            wins = loaded.Wins,
            points = loaded.Points,
            playerTeam = loaded.PlayerTeamId,
            saveKey = LeagueData.SaveKey,
            generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        if (!string.IsNullOrEmpty(evidencePath))
        {
            var folder = Path.GetDirectoryName(evidencePath);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true) + "\n");
        }
        Debug.Log("UBLADE2_LEAGUE_SAVE_OK round=" + loaded.Round + " wins=" + loaded.Wins + " points=" + loaded.Points +
                  " team=" + loaded.PlayerTeamId + " key=" + LeagueData.SaveKey + " passed=" + passed);
        PlayerPrefs.DeleteKey(LeagueData.SaveKey);
        PlayerPrefs.Save();
        yield return new WaitForSeconds(0.2f);
        Application.Quit(passed ? 0 : 1);
        yield return null;
        ExitProcess(passed ? 0 : 1);
    }

    private void WriteMatchEvidence()
    {
        if (string.IsNullOrEmpty(evidencePath) || match == null) return;
        var evidence = new MatchEvidence
        {
            passed = match.MatchOver,
            homeScore = match.HomeScore,
            awayScore = match.AwayScore,
            homeGoals = match.HomeGoals,
            awayGoals = match.AwayGoals,
            winner = match.Winner,
            frames = matchFrames,
            periods = match.Config.periods,
            phase = match.Phase.ToString(),
            homeTeam = homeTeam.id,
            awayTeam = awayTeam.id,
            arena = arena != null ? arena.env : "",
            screen = Screen.width + "x" + Screen.height,
            unity = Application.unityVersion,
            generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var folder = Path.GetDirectoryName(evidencePath);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true) + "\n");
    }

    private static void ExitProcess(int code)
    {
        try { Environment.Exit(code); } catch (Exception) { }
    }

    // ---------------------------------------------------------------- legacy demo/capture

    private void BuildLegacyWorld()
    {
        RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.24f);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(40f, 1f, 40f);
        floor.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.07f, 0.10f, 0.13f), 0f);
        var player = new GameObject("LegacyPlayer");
        player.transform.position = new Vector3(0f, 0.05f, 0f);
        legacyPlayer = player.transform;
        legacyController = player.AddComponent<CharacterController>();
        legacyController.height = 1.8f;
        legacyController.radius = 0.4f;
        legacyController.center = new Vector3(0f, 0.9f, 0f);
        var controllerAsset = Resources.Load<RuntimeAnimatorController>("Hero/WayfarerController");
        if (controllerAsset == null) throw new Exception("missing Resources/Hero/WayfarerController");
        legacyHero = HeroVisual.Create(legacyPlayer, controllerAsset);
        AddLegacyCrystal(new Vector3(3f, 1.1f, 0f));
        AddLegacyCrystal(new Vector3(-3f, 1.1f, 2f));
        AddLegacyCrystal(new Vector3(0f, 1.1f, -3f));
        var camObject = new GameObject("Main Camera");
        camObject.tag = "MainCamera";
        cam = camObject.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
        cam.fieldOfView = 58f;
        var lightObject = new GameObject("Key Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.82f, 0.91f, 1f);
        light.intensity = 1.15f;
        lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        Debug.Log("UBLADE2_WORLD crystals=" + legacyCrystals.Count + " speed=" + BaseSpeed + " hero=Wayfarer");
    }

    private void AddLegacyCrystal(Vector3 position)
    {
        var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal.transform.position = position;
        crystal.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
        crystal.transform.rotation = Quaternion.Euler(0f, 45f, 45f);
        crystal.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.16f, 0.86f, 0.76f), 2.2f);
        legacyCrystals.Add(crystal);
    }

    private IEnumerator RunLegacyDemo()
    {
        while (legacyCollected < 3 && legacyFrames < 900)
        {
            legacyFrames++;
            GameObject target = null;
            var best = float.MaxValue;
            foreach (var crystal in legacyCrystals)
            {
                if (crystal == null) continue;
                var distance = (crystal.transform.position - legacyPlayer.position).magnitude;
                if (distance < best) { best = distance; target = crystal; }
            }
            if (target != null)
            {
                var direction = target.transform.position - legacyPlayer.position;
                direction.y = 0f;
                LegacyMove(direction, Time.fixedDeltaTime);
                legacyHero.SetMotion(BaseSpeed);
            }
            for (int index = legacyCrystals.Count - 1; index >= 0; index--)
            {
                if (legacyCrystals[index] == null) { legacyCrystals.RemoveAt(index); continue; }
                var delta = legacyCrystals[index].transform.position - legacyPlayer.position;
                delta.y = 0f;
                if (delta.magnitude < 1.35f)
                {
                    Destroy(legacyCrystals[index]);
                    legacyCrystals.RemoveAt(index);
                    legacyCollected++;
                    legacyHero.PlayReach();
                }
            }
            yield return new WaitForFixedUpdate();
        }
        if (legacyCollected == 3)
        {
            Debug.Log("GYPSY_DEMO_OK collected=3 frames=" + legacyFrames);
            legacyHero.PlayReach();
            if (!string.IsNullOrEmpty(capturePath))
            {
                yield return new WaitForSeconds(0.7f);
                var folder = Path.GetDirectoryName(capturePath);
                if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(capturePath);
                Debug.Log("UBLADE2_CAPTURE_WRITTEN path=" + capturePath);
                yield return new WaitForSeconds(0.8f);
            }
            WriteLegacyEvidence();
            yield return new WaitForSeconds(0.2f);
            Application.Quit(0);
            yield return null;
            ExitProcess(0);
        }
        Debug.Log("GYPSY_DEMO_FAIL collected=" + legacyCollected + " frames=" + legacyFrames);
        Application.Quit(1);
        yield return null;
        ExitProcess(1);
    }

    private void LegacyMove(Vector3 direction, float dt)
    {
        if (legacyController == null) return;
        if (legacyController.isGrounded && vy < 0f) vy = -2f;
        vy -= 9.81f * dt;
        var planar = new Vector3(direction.x, 0f, direction.z);
        if (planar.sqrMagnitude > 0.0001f)
        {
            planar.Normalize();
            legacyPlayer.rotation = Quaternion.LookRotation(planar, Vector3.up);
        }
        legacyController.Move(planar * BaseSpeed * dt + Vector3.up * vy * dt);
    }

    private void WriteLegacyEvidence()
    {
        if (string.IsNullOrEmpty(evidencePath)) return;
        var evidence = new LegacyEvidence
        {
            passed = legacyHero != null && legacyHero.heroVisible,
            heroVisible = legacyHero != null && legacyHero.heroVisible,
            lodLevel = legacyHero != null ? legacyHero.lodLevel : -1,
            smileWeight = legacyHero != null ? legacyHero.smileWeight : 0f,
            capeBones = legacyHero != null ? legacyHero.capeBones.Length : 0,
            bones = legacyHero != null ? legacyHero.bones : 0,
            lod0Triangles = legacyHero != null ? legacyHero.lod0Triangles : 0,
            lod1Triangles = legacyHero != null ? legacyHero.lod1Triangles : 0,
            lod2Triangles = legacyHero != null ? legacyHero.lod2Triangles : 0,
            collected = legacyCollected,
            frames = legacyFrames,
            heroRectX = legacyHero != null ? legacyHero.heroRectX : 0,
            heroRectY = legacyHero != null ? legacyHero.heroRectY : 0,
            heroRectWidth = legacyHero != null ? legacyHero.heroRectWidth : 0,
            heroRectHeight = legacyHero != null ? legacyHero.heroRectHeight : 0,
            heroScreenPixels = legacyHero != null ? legacyHero.heroRectWidth * legacyHero.heroRectHeight : 0,
            screen = Screen.width + "x" + Screen.height,
            unity = Application.unityVersion,
            captureFile = capturePath ?? "",
            generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var folder = Path.GetDirectoryName(evidencePath);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true) + "\n");
        Debug.Log("UBLADE_HERO_RUNTIME_OK visible=" + evidence.heroVisible + " lod=" + evidence.lodLevel +
                  " bones=" + evidence.bones + " rect=" + evidence.heroRectX + "," + evidence.heroRectY + "," + evidence.heroRectWidth + "x" + evidence.heroRectHeight);
    }

    private void LateUpdate()
    {
        if (demo)
        {
            if (cam != null && legacyPlayer != null)
            {
                cam.transform.position = legacyPlayer.position + new Vector3(2.3f, 2.5f, -4.8f);
                cam.transform.LookAt(legacyPlayer.position + new Vector3(0f, 1.05f, 0f));
            }
            return;
        }
        if (cam == null || homeSkaters[0] == null || screen != GameScreen.Match && screen != GameScreen.Paused) return;
        if (debugOverview)
        {
            cam.transform.position = new Vector3(0f, 70f, -5f);
            cam.transform.LookAt(Vector3.zero);
            return;
        }
        var target = homeSkaters[0].transform;
        var desired = target.position + new Vector3(0f, 9.0f, -18.0f);
        float limitX = Mathf.Max(4f, arena.radiusX * 0.88f);
        float limitZ = Mathf.Max(4f, arena.radiusZ * 0.88f);
        float ex = desired.x / limitX;
        float ez = desired.z / limitZ;
        float radial = Mathf.Sqrt(ex * ex + ez * ez);
        if (radial > 1f) { desired.x /= radial; desired.z /= radial; }
        cam.transform.position = Vector3.Lerp(cam.transform.position, desired, 9f * Time.deltaTime);
        cam.transform.LookAt(target.position + new Vector3(0f, 2.2f, 0f));
        if (matchFrames % 120 == 0)
        {
            var evidenceRenderer = drome.root != null ? drome.root.GetComponentsInChildren<Renderer>() : new Renderer[0];
            Debug.Log("UBLADE2_DEBUG_CAM frame=" + matchFrames + " cam=" + cam.transform.position + " target=" + target.position + " fov=" + cam.fieldOfView + " dromeRoot=" + (drome.root != null ? drome.root.name : "NULL") + " renderers=" + evidenceRenderer.Length + " radius=" + arena.radiusX + "x" + arena.radiusZ);
        }
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var delta = a - b;
        delta.y = 0f;
        return delta.magnitude;
    }
}
