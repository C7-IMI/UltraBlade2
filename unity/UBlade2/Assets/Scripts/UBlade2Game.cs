using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class UBlade2Game : MonoBehaviour
{
    private readonly List<GameObject> crystals = new List<GameObject>();
    private readonly List<GameObject> arenaDecor = new List<GameObject>();
    private CharacterController controller;
    private Transform player;
    private Camera cam;
    private HeroVisual hero;
    private GameObject aiObject;
    private Transform aiTransform;
    private HeroVisual aiHero;
    private Renderer floorRenderer;
    private int collected;
    private int frames;
    private bool demo;
    private bool matchMode;
    private bool leagueTest;
    private bool autoplay;
    private bool captureAutoplay;
    private bool mobileCapture;
    private string capturePath;
    private string evidencePath;
    private string menuCapturePath;
    private string matchCapturePath;
    private string uiCaptureDir;
    private string mobileCapturePath;
    private float vy;
    private const float Speed = 6f;
    private const int MaxDemoFrames = 900;

    private MatchRules match;
    private CombatSystem combat;
    private AiController aiController;
    private LeagueData league;
    private GameSettings settings;
    private UiModel uiModel = new UiModel();
    private GameScreen screen = GameScreen.MainMenu;
    private GameScreen previousScreen = GameScreen.MainMenu;
    private int matchFrames;
    private bool resultResolved;
    private bool matchRanked;
    private bool leaguePersist = true;
    private LeagueOpponent currentOpponent;
    private int arenaIndex;
    private string banner = "";
    private float bannerTimer;
    private int collectFlash;
    private float collectFlashTimer;
    private bool audioReady;
    private bool worldReady;
    private int stunFlash;

    private static readonly string[] ArenaNames = { "Neon Rink", "Sky Deck", "Foundry", "Void Dome" };

    private static readonly Color[] ArenaFloorColors =
    {
        new Color(0.06f, 0.12f, 0.16f),
        new Color(0.07f, 0.08f, 0.16f),
        new Color(0.14f, 0.08f, 0.06f),
        new Color(0.10f, 0.05f, 0.12f)
    };

    private static readonly Color[] ArenaAccentColors =
    {
        new Color(0.15f, 0.85f, 0.85f),
        new Color(0.35f, 0.55f, 1.0f),
        new Color(1.0f, 0.55f, 0.2f),
        new Color(0.75f, 0.35f, 1.0f)
    };

    private static readonly Vector3[][] ArenaCrystals =
    {
        new[] { new Vector3(3f, 1.1f, 0f), new Vector3(-3f, 1.1f, 2f), new Vector3(0f, 1.1f, -3f) },
        new[] { new Vector3(5f, 1.1f, 3f), new Vector3(-5f, 1.1f, -2f), new Vector3(1f, 1.1f, -5f) },
        new[] { new Vector3(-4f, 1.1f, 4f), new Vector3(4f, 1.1f, 4f), new Vector3(0f, 1.1f, -4f) },
        new[] { new Vector3(6f, 1.1f, 0f), new Vector3(-6f, 1.1f, 1f), new Vector3(0f, 1.1f, 6f) }
    };

    [Serializable]
    private class RuntimeEvidence
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
        public string schema = "ublade2.match.runtime/v1";
        public bool passed;
        public int playerScore;
        public int aiScore;
        public int winner;
        public int frames;
        public string phase;
        public float duration;
        public int targetScore;
        public int difficulty;
        public string arena;
        public int round;
        public int totalRounds;
        public string playerTeam;
        public string aiTeam;
        public string screen;
        public string unity;
        public string generatedAt;
    }

    [Serializable]
    private class LeagueEvidence
    {
        public string schema = "ublade2.league.save/v1";
        public bool passed;
        public int round;
        public int wins;
        public int points;
        public string saveKey;
        public string generatedAt;
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
        leagueTest = HasArg("--league-test");
        matchCapturePath = GetArg("--match-capture=");
        menuCapturePath = GetArg("--menu-capture=");
        uiCaptureDir = GetArg("--ui-capture=");
#if UNITY_ANDROID && !UNITY_EDITOR
        demo = demo || AndroidIntentBool("ublade2_demo");
        if (AndroidIntentBool("ublade2_capture"))
        {
            mobileCapture = true;
            mobileCapturePath = Path.Combine(Application.persistentDataPath, "ublade2-mobile-frame.png");
        }
#endif
        capturePath = GetArg("--capture=");
        evidencePath = GetArg("--evidence=");
        autoplay = matchMode || !string.IsNullOrEmpty(matchCapturePath) || mobileCapture;
        captureAutoplay = !string.IsNullOrEmpty(matchCapturePath) || mobileCapture;

        settings = GameSettings.Load("ublade2.settings.");
        if (!Application.isMobilePlatform && !PlayerPrefs.HasKey("ublade2.settings.touch"))
        {
            settings.touchControls = false;
        }
        if (autoplay)
        {
            league = new LeagueData();
            league.ResetSeason();
            league.BeginSeason();
            leaguePersist = false;
        }
        else
        {
            league = LeagueData.Load();
            leaguePersist = true;
        }

        BuildWorld();
        aiObject.SetActive(!(demo || !string.IsNullOrEmpty(capturePath)));
        worldReady = true;

        audioReady = !Application.isBatchMode;
        if (audioReady)
        {
            GameAudio.Initialize(gameObject);
            GameAudio.ApplySettings(settings);
        }

        Debug.Log("UBLADE2_START demo=" + demo + " crystals=" + crystals.Count +
                  " unity=" + Application.unityVersion +
                  " screen=" + Screen.width + "x" + Screen.height +
                  " capture=" + (string.IsNullOrEmpty(capturePath) ? "none" : capturePath) +
                  " mode=" + (demo ? "demo" : matchMode ? "match" : captureAutoplay ? "capture" : "play"));

        if (leagueTest)
        {
            StartCoroutine(RunLeagueSelfTest());
            return;
        }
        if (demo)
        {
            StartCoroutine(RunDemo());
            return;
        }
        if (matchMode)
        {
            StartCoroutine(RunHeadlessMatch());
            return;
        }
        if (captureAutoplay)
        {
            string captureOut = mobileCapture ? mobileCapturePath : matchCapturePath;
            StartCoroutine(RunMatchCapture(captureOut, mobileCapture));
            return;
        }
        if (!string.IsNullOrEmpty(menuCapturePath))
        {
            screen = GameScreen.MainMenu;
            StartCoroutine(RunMenuCapture());
            return;
        }
        if (!string.IsNullOrEmpty(uiCaptureDir))
        {
            screen = GameScreen.MainMenu;
            StartCoroutine(RunUiCapture());
            return;
        }
        screen = GameScreen.MainMenu;
        if (audioReady)
        {
            GameAudio.StartMusic(0);
            GameAudio.PlayWhistle();
        }
        UpdateUiModel();
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
        catch (System.Exception)
        {
            return false;
        }
    }
#endif

    private static bool HasArg(string value)
    {
        return Array.Exists(Environment.GetCommandLineArgs(), argument => argument == value);
    }

    private static string GetArg(string prefix)
    {
        foreach (var value in Environment.GetCommandLineArgs())
        {
            if (value.StartsWith(prefix, StringComparison.Ordinal))
            {
                return value.Substring(prefix.Length);
            }
        }
        return null;
    }

    private Material Mat(Color color, float emission)
    {
        var mobile = Application.isMobilePlatform;
        var shader = mobile ? Shader.Find("Unlit/Color") : Shader.Find("Standard");
        if (mobile)
        {
            color = new Color(Mathf.Clamp01(color.r * 2.4f + 0.06f), Mathf.Clamp01(color.g * 2.4f + 0.06f), Mathf.Clamp01(color.b * 2.4f + 0.06f));
        }
        var material = new Material(shader != null ? shader : Shader.Find("Legacy Shaders/Diffuse"));
        material.color = color;
        if (!mobile && emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        return material;
    }

    private void ApplyMobileUnlit(Transform root)
    {
        var unlitTexture = Shader.Find("Unlit/Texture");
        var unlitColor = Shader.Find("Unlit/Color");
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                var source = materials[index];
                Material target = null;
                if (unlitTexture != null && source != null && source.HasProperty("_MainTex") && source.GetTexture("_MainTex") != null)
                {
                    target = new Material(unlitTexture);
                    target.SetTexture("_MainTex", source.GetTexture("_MainTex"));
                }
                else if (unlitColor != null)
                {
                    target = new Material(unlitColor);
                    target.color = source != null ? source.color : Color.white;
                }
                if (target != null)
                {
                    materials[index] = target;
                }
            }
            renderer.sharedMaterials = materials;
        }
    }


    private void ApplyMobileSolid(Transform root, Color color)
    {
        var unlitColor = Shader.Find("Unlit/Color");
        if (unlitColor == null)
        {
            return;
        }
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                var solid = new Material(unlitColor);
                solid.color = color;
                materials[index] = solid;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private GameObject AddBox(string name, Vector3 position, Vector3 scale, Color color)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = Mat(color, 0f);
        return box;
    }

    private void BuildWorld()
    {
        RenderSettings.ambientLight = new Color(0.24f, 0.28f, 0.34f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.06f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.010f;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(44f, 1f, 44f);
        floorRenderer = floor.GetComponent<Renderer>();
        floorRenderer.sharedMaterial = Mat(ArenaFloorColors[0], 0f);

        var playerObject = new GameObject("Player");
        playerObject.transform.position = new Vector3(-2f, 0.05f, 0f);
        player = playerObject.transform;
        controller = playerObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.4f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        var controllerAsset = Resources.Load<RuntimeAnimatorController>("Hero/WayfarerController");
        if (controllerAsset == null)
        {
            throw new Exception("missing Resources/Hero/WayfarerController");
        }
        hero = HeroVisual.Create(player, controllerAsset);
        if (Application.isMobilePlatform)
        {
            ApplyMobileUnlit(hero.transform);
            ApplyMobileSolid(hero.transform, new Color(0.35f, 0.85f, 1f));
            RenderSettings.ambientLight = new Color(0.75f, 0.8f, 0.9f);
        }

        aiObject = new GameObject("Opponent");
        aiObject.transform.position = new Vector3(2f, 0.05f, 0f);
        aiTransform = aiObject.transform;
        aiHero = HeroVisual.Create(aiTransform, controllerAsset);
        TintOpponent(aiTransform);
        if (Application.isMobilePlatform)
        {
            ApplyMobileSolid(aiTransform, new Color(1f, 0.35f, 0.3f));
        }
        var aiLight = new GameObject("Opponent Light").AddComponent<Light>();
        aiLight.type = LightType.Point;
        aiLight.color = new Color(1f, 0.3f, 0.25f);
        aiLight.range = 5f;
        aiLight.intensity = 1.6f;
        aiLight.transform.position = new Vector3(2f, 2.4f, 0f);
        aiLight.transform.SetParent(aiTransform, true);

        var crystalColor = new Color(0.16f, 0.86f, 0.76f);
        for (int index = 0; index < 3; index++)
        {
            AddCrystal("Crystal_" + index, ArenaCrystals[0][index], crystalColor);
        }

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cam = cameraObject.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
        cam.fieldOfView = 58f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 220f;
        cameraObject.transform.position = new Vector3(2.3f, 2.5f, -4.8f);

        var lightObject = new GameObject("Key Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.82f, 0.91f, 1f);
        light.intensity = 1.15f;
        lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

        var rimObject = new GameObject("Rim Light");
        var rim = rimObject.AddComponent<Light>();
        rim.type = LightType.Directional;
        rim.color = new Color(0.30f, 0.62f, 0.95f);
        rim.intensity = 0.5f;
        rimObject.transform.rotation = Quaternion.Euler(20f, 140f, 0f);

        BuildArena(0);
        Debug.Log("UBLADE2_WORLD crystals=" + crystals.Count + " speed=" + Speed + " hero=Wayfarer");
    }

    private void TintOpponent(Transform root)
    {
        var tint = new Color(1f, 0.32f, 0.28f);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.materials;
            for (int index = 0; index < materials.Length; index++)
            {
                if (materials[index] == null)
                {
                    continue;
                }
                materials[index].color = tint;
                if (materials[index].HasProperty("_EmissionColor"))
                {
                    materials[index].EnableKeyword("_EMISSION");
                    materials[index].SetColor("_EmissionColor", tint * 0.35f);
                }
            }
            renderer.materials = materials;
        }
    }

    private void BuildArena(int index)
    {
        arenaIndex = index;
        foreach (var decor in arenaDecor)
        {
            if (decor != null)
            {
                Destroy(decor);
            }
        }
        arenaDecor.Clear();

        if (floorRenderer != null)
        {
            floorRenderer.sharedMaterial = Mat(ArenaFloorColors[index], 0f);
        }
        RenderSettings.fogColor = ArenaFloorColors[index] * 0.45f;
        if (cam != null)
        {
            cam.backgroundColor = Application.isMobilePlatform
                ? new Color(0.10f, 0.16f, 0.24f)
                : ArenaFloorColors[index] * 0.35f;
        }

        var accent = ArenaAccentColors[index];
        var wallColor = ArenaFloorColors[index] * 1.5f;
        arenaDecor.Add(AddBox("Wall North", new Vector3(0f, 1.5f, 21f), new Vector3(44f, 3f, 0.8f), wallColor));
        arenaDecor.Add(AddBox("Wall South", new Vector3(0f, 1.5f, -21f), new Vector3(44f, 3f, 0.8f), wallColor));
        arenaDecor.Add(AddBox("Wall East", new Vector3(21f, 1.5f, 0f), new Vector3(0.8f, 3f, 44f), wallColor));
        arenaDecor.Add(AddBox("Wall West", new Vector3(-21f, 1.5f, 0f), new Vector3(0.8f, 3f, 44f), wallColor));

        switch (index)
        {
            case 0:
                arenaDecor.Add(AddBox("Goal A", new Vector3(0f, 1.2f, 12f), new Vector3(8f, 2.4f, 0.5f), accent));
                arenaDecor.Add(AddBox("Goal B", new Vector3(0f, 1.2f, -12f), new Vector3(8f, 2.4f, 0.5f), accent));
                arenaDecor.Add(AddBox("Platform A", new Vector3(-8f, 0.5f, 6f), new Vector3(4f, 1f, 4f), accent * 0.35f));
                arenaDecor.Add(AddBox("Platform B", new Vector3(8f, 0.5f, -6f), new Vector3(4f, 1f, 4f), accent * 0.35f));
                break;
            case 1:
                arenaDecor.Add(AddBox("Tower A", new Vector3(-7f, 1.5f, 7f), new Vector3(2f, 3f, 2f), accent));
                arenaDecor.Add(AddBox("Tower B", new Vector3(7f, 1.5f, 7f), new Vector3(2f, 3f, 2f), accent));
                arenaDecor.Add(AddBox("Tower C", new Vector3(-7f, 1.5f, -7f), new Vector3(2f, 3f, 2f), accent));
                arenaDecor.Add(AddBox("Tower D", new Vector3(7f, 1.5f, -7f), new Vector3(2f, 3f, 2f), accent));
                break;
            case 2:
                arenaDecor.Add(AddBox("Forge A", new Vector3(-6f, 0.75f, 0f), new Vector3(3f, 1.5f, 8f), accent * 0.5f));
                arenaDecor.Add(AddBox("Forge B", new Vector3(6f, 0.75f, 0f), new Vector3(3f, 1.5f, 8f), accent * 0.5f));
                arenaDecor.Add(AddBox("Beam A", new Vector3(0f, 2f, 0f), new Vector3(1f, 4f, 1f), accent));
                break;
            default:
                arenaDecor.Add(AddBox("Pylon A", new Vector3(-9f, 2f, 0f), new Vector3(1.5f, 4f, 1.5f), accent));
                arenaDecor.Add(AddBox("Pylon B", new Vector3(9f, 2f, 0f), new Vector3(1.5f, 4f, 1.5f), accent));
                arenaDecor.Add(AddBox("Pylon C", new Vector3(0f, 2f, 9f), new Vector3(1.5f, 4f, 1.5f), accent));
                arenaDecor.Add(AddBox("Pylon D", new Vector3(0f, 2f, -9f), new Vector3(1.5f, 4f, 1.5f), accent));
                break;
        }

        for (int crystalIndex = 0; crystalIndex < crystals.Count && crystalIndex < 3; crystalIndex++)
        {
            if (crystals[crystalIndex] != null)
            {
                crystals[crystalIndex].transform.position = ArenaCrystals[index][crystalIndex];
                crystals[crystalIndex].SetActive(true);
            }
        }
    }

    private void AddCrystal(string name, Vector3 position, Color color)
    {
        var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal.name = name;
        crystal.transform.position = position;
        crystal.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
        crystal.transform.rotation = Quaternion.Euler(0f, 45f, 45f);
        crystal.GetComponent<Renderer>().sharedMaterial = Mat(color, 2.2f);
        crystals.Add(crystal);
    }

    private void Update()
    {
        if (!worldReady)
        {
            return;
        }
        if (demo)
        {
            if (hero != null)
            {
                hero.SetMotion(0f);
            }
            return;
        }
        if (matchMode || captureAutoplay)
        {
            return;
        }
        GameInput.Update();
        if (screen == GameScreen.Match)
        {
            if (GameInput.PausePressed)
            {
                screen = GameScreen.Paused;
                return;
            }
            StepMatch(Time.deltaTime);
        }
        else if (screen == GameScreen.Paused && GameInput.PausePressed)
        {
            screen = GameScreen.Match;
        }
        else if (hero != null)
        {
            hero.SetMotion(0f);
            if (aiHero != null)
            {
                aiHero.SetMotion(0f);
            }
        }
    }

    private void StepMatch(float dt)
    {
        if (match == null || match.Phase == MatchPhase.Ended)
        {
            UpdateHud();
            return;
        }
        matchFrames++;
        match.Update(dt);
        combat.Update(dt);
        if (combat.PlayerStunned && stunFlash <= 0)
        {
            stunFlash = 2;
        }
        else if (stunFlash > 0)
        {
            stunFlash--;
        }

        if (match.Phase == MatchPhase.Playing)
        {
            Vector3 move;
            bool attack;
            bool dash;
            if (autoplay)
            {
                move = AutopilotDirection(out attack, out dash);
            }
            else
            {
                move = new Vector3(GameInput.Move.x, 0f, GameInput.Move.y);
                attack = GameInput.AttackPressed;
                dash = GameInput.DashPressed;
            }
            if (combat.PlayerStunned)
            {
                move = Vector3.zero;
                attack = false;
                dash = false;
            }
            var playerPosition = player.position;
            MovePlayer(move, dt * (combat.PlayerDashing ? 2.2f : 1f));
            hero.SetMotion(move.magnitude * Speed);
            if (attack && combat.PlayerTryAttack(playerPosition, aiTransform.position))
            {
                HandleHit(true);
            }
            if (dash && combat.PlayerTryDash())
            {
                if (audioReady) GameAudio.PlayDash();
            }

            var aiPosition = aiTransform.position;
            aiController.SetStunned(combat.AiStunRemaining);
            Vector3 aiMove;
            bool aiAttack;
            bool aiDash;
            aiController.Think(dt, aiPosition, player.position, match.CrystalPositions, match.CrystalActive, out aiMove, out aiAttack, out aiDash);
            if (!combat.AiStunned && aiMove.sqrMagnitude > 0.0001f)
            {
                var speed = 4.4f + match.Config.difficulty * 0.7f;
                var next = Vector3.MoveTowards(aiPosition, aiPosition + aiMove.normalized * 10f, speed * dt);
                next.y = 0.05f;
                aiTransform.position = next;
                var face = next - aiPosition;
                face.y = 0f;
                if (face.sqrMagnitude > 0.0001f)
                {
                    aiTransform.rotation = Quaternion.LookRotation(face, Vector3.up);
                }
            }
            aiHero.SetMotion(aiMove.magnitude * 5f);
            if (aiAttack && combat.AiTryAttack(aiTransform.position, player.position))
            {
                HandleHit(false);
            }
            if (aiDash)
            {
                combat.AiTryDash();
            }

            for (int index = 0; index < 3; index++)
            {
                if (!match.CrystalActive[index])
                {
                    continue;
                }
                if (!combat.PlayerStunned && PlanarDistance(player.position, match.CrystalPositions[index]) < 1.35f)
                {
                    match.CollectCrystal(index, true);
                }
                else if (!combat.AiStunned && PlanarDistance(aiTransform.position, match.CrystalPositions[index]) < 1.35f)
                {
                    match.CollectCrystal(index, false);
                }
            }
        }

        SyncCrystals();
        UpdateHud();
    }

    private Vector3 AutopilotDirection(out bool attack, out bool dash)
    {
        attack = false;
        dash = false;
        var playerPosition = player.position;
        if (PlanarDistance(playerPosition, aiTransform.position) < 2.2f && combat.PlayerAttackCooldown <= 0f)
        {
            attack = true;
        }
        var target = Vector3.zero;
        var bestDistance = float.MaxValue;
        for (int index = 0; index < 3; index++)
        {
            if (!match.CrystalActive[index])
            {
                continue;
            }
            var distance = PlanarDistance(playerPosition, match.CrystalPositions[index]);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                target = match.CrystalPositions[index];
            }
            if (PlanarDistance(match.CrystalPositions[index], aiTransform.position) < 1.5f)
            {
                attack = PlanarDistance(playerPosition, aiTransform.position) < 2.4f;
            }
        }
        if (bestDistance > 7f && matchFrames % 90 == 0)
        {
            dash = true;
        }
        var direction = target - playerPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
        }
        return direction;
    }

    private void HandleHit(bool playerHitAi)
    {
        if (audioReady)
        {
            GameAudio.PlayHit(!playerHitAi);
        }
        banner = playerHitAi ? "STRIKE!" : "HIT!";
        bannerTimer = 0.8f;
        if (playerHitAi)
        {
            if (aiTransform != null)
            {
                aiTransform.position += new Vector3(0f, 0f, 0f) + aiTransform.forward * 0.9f;
            }
            if (aiHero != null)
            {
                aiHero.PlayReach();
            }
        }
        else
        {
            controller.Move(player.forward * -0.9f);
            if (hero != null)
            {
                hero.PlayReach();
            }
        }
    }

    private void SyncCrystals()
    {
        if (match == null)
        {
            return;
        }
        for (int index = 0; index < crystals.Count && index < 3; index++)
        {
            var crystal = crystals[index];
            if (crystal == null)
            {
                continue;
            }
            var active = index < match.CrystalActive.Length && match.CrystalActive[index];
            if (crystal.activeSelf != active)
            {
                crystal.SetActive(active);
            }
        }
    }

    private void UpdateHud()
    {
        if (match == null)
        {
            return;
        }
        var config = match.Config;
        var hud = uiModel.hud;
        hud.playerScore = match.PlayerScore;
        hud.aiScore = match.AiScore;
        hud.timeLeft = match.TimeLeft;
        hud.targetScore = config.targetScore;
        hud.round = config.round;
        hud.totalRounds = config.totalRounds;
        hud.arenaName = config.arenaName;
        hud.playerTeam = config.playerTeam;
        hud.aiTeam = config.aiTeam;
        hud.playerColor = config.playerColor;
        hud.aiColor = config.aiColor;
        hud.kickoff = match.Phase == MatchPhase.Kickoff;
        hud.playerStunned = combat != null && combat.PlayerStunned;
        hud.aiStunned = combat != null && combat.AiStunned;
        hud.attackCooldown = combat != null ? combat.PlayerAttackCooldown : 0f;
        hud.dashCooldown = combat != null ? combat.PlayerDashRemaining : 0f;
        hud.collectFlash = collectFlash;
        hud.banner = banner;
        hud.matchOver = match.Phase == MatchPhase.Ended;
        hud.difficulty = config.difficulty;
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.deltaTime;
            if (bannerTimer <= 0f && match.Phase != MatchPhase.PointFlash)
            {
                banner = "";
            }
        }
        if (collectFlashTimer > 0f)
        {
            collectFlashTimer -= Time.deltaTime;
            if (collectFlashTimer <= 0f)
            {
                collectFlash = 0;
            }
        }
        if (match.Phase == MatchPhase.PointFlash && string.IsNullOrEmpty(banner))
        {
            banner = "POINT!";
        }
        uiModel.seasonWins = league != null ? league.Wins : 0;
        uiModel.seasonLosses = league != null ? league.Losses : 0;
        uiModel.seasonPoints = league != null ? league.Points : 0;
        uiModel.seasonComplete = league != null && league.SeasonComplete;
        uiModel.leagueLines = league != null ? league.TableLines() : new string[0];
        uiModel.nextOpponent = league != null && league.NextOpponent != null ? league.NextOpponent.name : "";
        uiModel.touch = Application.isMobilePlatform;
        uiModel.settings = settings;
    }

    private void BeginMatch(bool autopilotPlayer, float duration, int targetScore, LeagueOpponent opponentOverride = null, bool ranked = true)
    {
        leaguePersist = !autoplay && ranked;
        matchRanked = ranked && leaguePersist;
        if (opponentOverride != null)
        {
            currentOpponent = opponentOverride;
            matchRanked = false;
            leaguePersist = false;
        }
        else
        {
            if (league.SeasonComplete || league.NextOpponent == null)
            {
                league.ResetSeason();
                league.BeginSeason();
            }
            currentOpponent = league.NextOpponent;
        }
        if (currentOpponent == null)
        {
            currentOpponent = new LeagueOpponent { name = "Razorwings", color = new Color(1f, 0.35f, 0.3f), difficulty = 2, arena = ArenaNames[0] };
        }
        int arena = Array.IndexOf(ArenaNames, currentOpponent.arena);
        if (arena < 0)
        {
            arena = 0;
        }
        BuildArena(arena);
        var effectiveDifficulty = Mathf.Clamp(currentOpponent.difficulty + settings.difficulty - 2, 1, 3);
        var config = new MatchConfig
        {
            playerTeam = "WAYFARERS",
            aiTeam = currentOpponent.name.ToUpperInvariant(),
            playerColor = new Color(0.2f, 0.75f, 1f),
            aiColor = currentOpponent.color,
            arenaName = currentOpponent.arena,
            duration = duration,
            targetScore = targetScore,
            difficulty = effectiveDifficulty,
            round = Mathf.Clamp(league.Round + 1, 1, league.TotalRounds),
            totalRounds = league.TotalRounds
        };
        match = new MatchRules(config, ArenaCrystals[arena]);
        match.CrystalCollected += OnCrystalCollected;
        match.MatchEnded += OnMatchEnded;
        combat = new CombatSystem();
        aiController = new AiController(effectiveDifficulty);
        this.autoplay = autopilotPlayer || matchMode || captureAutoplay;
        player.position = new Vector3(-2f, 0.05f, 0f);
        controller.enabled = false;
        controller.enabled = true;
        aiTransform.position = new Vector3(2f, 0.05f, 0f);
        hero.SetMotion(0f);
        aiHero.SetMotion(0f);
        matchFrames = 0;
        resultResolved = false;
        banner = "";
        bannerTimer = 0f;
        collectFlash = 0;
        collectFlashTimer = 0f;
        screen = GameScreen.Match;
        match.BeginMatch();
        UpdateHud();
        if (audioReady)
        {
            GameAudio.ApplySettings(settings);
            GameAudio.StartMusic(arena);
            GameAudio.PlayWhistle();
        }
        Debug.Log("UBLADE2_MATCH_START opponent=" + currentOpponent.name + " arena=" + currentOpponent.arena +
                  " difficulty=" + effectiveDifficulty + " duration=" + duration +
                  " target=" + targetScore + " ranked=" + matchRanked + " autoplay=" + this.autoplay);
    }

    private void OnCrystalCollected(int index, bool byPlayer)
    {
        if (crystals != null && index >= 0 && index < crystals.Count && crystals[index] != null)
        {
            crystals[index].SetActive(false);
        }
        collectFlash = byPlayer ? 1 : 2;
        collectFlashTimer = 0.7f;
        if (audioReady)
        {
            GameAudio.PlayCollect(byPlayer ? 1 : 0);
        }
        if (byPlayer)
        {
            if (hero != null) hero.PlayReach();
            banner = "+1 WAYFARERS";
        }
        else
        {
            if (aiHero != null) aiHero.PlayReach();
            banner = "+1 " + (match != null ? match.Config.aiTeam : "OPPONENT");
        }
        bannerTimer = 1.0f;
    }

    private void OnMatchEnded(int winner)
    {
        if (resultResolved)
        {
            return;
        }
        resultResolved = true;
        if (matchRanked && league != null)
        {
            league.ResolveMatch(winner == 1);
            if (leaguePersist)
            {
                league.Save();
            }
        }
        screen = GameScreen.Result;
        uiModel.hud.resultText = winner == 1 ? "VICTORY" : winner == 2 ? "DEFEAT" : "DRAW";
        if (audioReady)
        {
            if (winner == 1) GameAudio.PlayVictory();
            else if (winner == 2) GameAudio.PlayDefeat();
            GameAudio.PlayWhistle();
        }
        Debug.Log("UBLADE2_MATCH_END player=" + match.PlayerScore + " ai=" + match.AiScore +
                  " winner=" + winner + " frames=" + matchFrames + " phase=" + match.Phase +
                  " ranked=" + matchRanked);
        UpdateHud();
    }

    private void UpdateUiModel()
    {
        uiModel.screen = screen;
        uiModel.settings = settings;
        uiModel.hud.matchOver = match != null && match.Phase == MatchPhase.Ended;
        if (settings != null)
        {
            uiModel.touch = Application.isMobilePlatform;
        }
        if (league != null)
        {
            uiModel.seasonWins = league.Wins;
            uiModel.seasonLosses = league.Losses;
            uiModel.seasonPoints = league.Points;
            uiModel.seasonComplete = league.SeasonComplete;
            uiModel.leagueLines = league.TableLines();
            uiModel.nextOpponent = league.NextOpponent != null ? league.NextOpponent.name : "";
            uiModel.hud.round = Mathf.Clamp(league.Round + 1, 1, Mathf.Max(1, league.TotalRounds));
            uiModel.hud.totalRounds = league.TotalRounds;
        }
        if (settings != null)
        {
            uiModel.hud.difficulty = settings.difficulty;
            uiModel.hud.targetScore = match != null ? match.Config.targetScore : 5;
        }
    }

    private void OnGUI()
    {
        if (!worldReady || demo || matchMode || !string.IsNullOrEmpty(capturePath) && !captureAutoplay)
        {
            return;
        }
        if (matchCapturePath == null && menuCapturePath == null && mobileCapture == false && string.IsNullOrEmpty(capturePath))
        {
            // interactive play
        }
        UpdateUiModel();
        if (screen == GameScreen.Settings)
        {
            ApplyDisplaySettings();
            if (audioReady)
            {
                GameAudio.ApplySettings(settings);
            }
        }
        var action = GameUI.Draw(uiModel);
        if (screen == GameScreen.Match && Application.isMobilePlatform && settings != null && settings.touchControls)
        {
            GameInput.DrawTouchControls(settings);
        }
        if (action != UiAction.None)
        {
            if (audioReady)
            {
                GameAudio.PlayClick();
            }
            HandleAction(action);
        }
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
                BeginMatch(false, 120f, 5);
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
                BeginMatch(false, 120f, 5, currentOpponent, false);
                break;
            case UiAction.QuitToMenu:
                screen = GameScreen.MainMenu;
                if (audioReady)
                {
                    GameAudio.StartMusic(0);
                }
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
        UpdateUiModel();
    }

    private void MovePlayer(Vector3 direction, float deltaTime)
    {
        if (controller == null)
        {
            return;
        }
        if (controller.isGrounded && vy < 0f)
        {
            vy = -2f;
        }
        vy -= 9.81f * deltaTime;
        var planar = new Vector3(direction.x, 0f, direction.z);
        if (planar.sqrMagnitude > 0.0001f)
        {
            planar.Normalize();
            player.rotation = Quaternion.LookRotation(planar, Vector3.up);
        }
        var speed = Speed * (combat != null && combat.PlayerDashing ? 1.0f : 1.0f);
        controller.Move(planar * speed * deltaTime + Vector3.up * vy * deltaTime);
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        var delta = a - b;
        delta.y = 0f;
        return delta.magnitude;
    }

    private IEnumerator RunDemo()
    {
        while (collected < 3 && frames < MaxDemoFrames)
        {
            frames++;
            var target = NearestCrystal();
            if (target != null)
            {
                var direction = target.transform.position - player.position;
                direction.y = 0f;
                MovePlayer(direction, Time.fixedDeltaTime);
                hero.SetMotion(Speed);
            }
            CollectNearby();
            if (frames % 100 == 0)
            {
                Debug.Log("UBLADE2_FRAME " + frames + " pos=" + player.position + " crystals=" + crystals.Count);
            }
            yield return new WaitForFixedUpdate();
        }

        if (collected == 3)
        {
            Debug.Log("GYPSY_DEMO_OK collected=3 frames=" + frames);
            hero.PlayReach();
            if (!string.IsNullOrEmpty(capturePath))
            {
                yield return new WaitForSeconds(0.7f);
                var folder = Path.GetDirectoryName(capturePath);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(capturePath);
                Debug.Log("UBLADE2_CAPTURE_WRITTEN path=" + capturePath);
                yield return new WaitForSeconds(0.8f);
            }
            WriteEvidence();
            yield return new WaitForSeconds(0.2f);
            Application.Quit(0);
            yield return null;
            ExitProcess(0);
        }
        else
        {
            Debug.Log("GYPSY_DEMO_FAIL collected=" + collected + " frames=" + frames);
            Application.Quit(1);
            yield return null;
            ExitProcess(1);
        }
    }

    private void CollectNearby()
    {
        for (int index = crystals.Count - 1; index >= 0; index--)
        {
            var crystal = crystals[index];
            if (crystal == null)
            {
                crystals.RemoveAt(index);
                continue;
            }
            var delta = crystal.transform.position - player.position;
            delta.y = 0f;
            if (delta.magnitude < 1.35f)
            {
                crystals.RemoveAt(index);
                Destroy(crystal);
                collected++;
                hero.PlayReach();
                if (audioReady) GameAudio.PlayCollect(1);
                Debug.Log("UBLADE2_COLLECT collected=" + collected + " frames=" + frames);
            }
        }
    }

    private GameObject NearestCrystal()
    {
        GameObject best = null;
        var bestDistance = float.MaxValue;
        foreach (var crystal in crystals)
        {
            if (crystal == null)
            {
                continue;
            }
            var delta = crystal.transform.position - player.position;
            delta.y = 0f;
            if (delta.magnitude < bestDistance)
            {
                bestDistance = delta.magnitude;
                best = crystal;
            }
        }
        return best;
    }

    private IEnumerator RunHeadlessMatch()
    {
        league.ResetSeason();
        league.BeginSeason();
        BeginMatch(true, 20f, 2);
        while (screen == GameScreen.Match && matchFrames < 20000)
        {
            StepMatch(Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
        WriteMatchEvidence();
        Debug.Log("UBLADE2_MATCH_OK player=" + match.PlayerScore + " ai=" + match.AiScore +
                  " winner=" + match.Winner + " frames=" + matchFrames + " phase=" + match.Phase +
                  " duration=20 target=2");
        yield return new WaitForSeconds(0.3f);
        Application.Quit(0);
        yield return null;
        ExitProcess(0);
    }

    private IEnumerator RunMatchCapture(string path, bool keepRunning)
    {
        league.ResetSeason();
        league.BeginSeason();
        BeginMatch(true, 120f, 5);
        while (matchFrames < 240 && screen == GameScreen.Match)
        {
            StepMatch(Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }
        WriteMatchEvidence();
        if (keepRunning)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            yield return new WaitForEndOfFrame();
            var written = 0;
            try
            {
                var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
                texture.Apply();
                var bytes = texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                written = bytes.Length;
                Destroy(texture);
            }
            catch (Exception exception)
            {
                Debug.Log("UBLADE2_MOBILE_CAPTURE_FAIL " + exception);
            }
            yield return new WaitForSeconds(0.3f);
            if (File.Exists(path) && written > 0)
            {
                Debug.Log("UBLADE2_MOBILE_CAPTURE_OK path=" + path + " bytes=" + written + " screen=" + Screen.width + "x" + Screen.height + " frames=" + matchFrames);
            }
            else
            {
                Debug.Log("UBLADE2_MOBILE_CAPTURE_FAIL path=" + path + " bytes=" + written);
            }
#else
            Debug.Log("UBLADE2_MOBILE_CAPTURE_FAIL unsupported platform");
#endif
            yield break;
        }
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(path);
        yield return new WaitForSeconds(0.9f);
        Debug.Log("UBLADE2_MATCH_CAPTURE_OK path=" + path + " screen=" + Screen.width + "x" + Screen.height + " frames=" + matchFrames);
        yield return new WaitForSeconds(0.4f);
        Application.Quit(0);
        yield return null;
        ExitProcess(0);
    }

    private IEnumerator RunUiCapture()
    {
        var folder = uiCaptureDir;
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }
        screen = GameScreen.MainMenu;
        UpdateUiModel();
        yield return new WaitForSeconds(0.8f);
        yield return CaptureUiFrame("menu.png");
        screen = GameScreen.League;
        UpdateUiModel();
        yield return new WaitForSeconds(0.4f);
        yield return CaptureUiFrame("league.png");
        screen = GameScreen.Settings;
        UpdateUiModel();
        yield return new WaitForSeconds(0.4f);
        yield return CaptureUiFrame("settings.png");
        BeginMatch(false, 120f, 5);
        for (int index = 0; index < 40; index++)
        {
            StepMatch(Time.fixedDeltaTime);
        }
        yield return new WaitForSeconds(0.3f);
        yield return CaptureUiFrame("match.png");
        screen = GameScreen.Paused;
        yield return new WaitForSeconds(0.4f);
        yield return CaptureUiFrame("pause.png");
        uiModel.hud.resultText = "VICTORY";
        screen = GameScreen.Result;
        yield return new WaitForSeconds(0.4f);
        yield return CaptureUiFrame("result.png");
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

    private IEnumerator RunMenuCapture()
    {
        screen = GameScreen.MainMenu;
        yield return new WaitForSeconds(1.2f);
        UpdateUiModel();
        var folder = Path.GetDirectoryName(menuCapturePath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(menuCapturePath);
        yield return new WaitForSeconds(0.9f);
        Debug.Log("UBLADE2_MENU_CAPTURE_OK path=" + menuCapturePath + " screen=" + Screen.width + "x" + Screen.height);
        yield return new WaitForSeconds(0.4f);
        Application.Quit(0);
        yield return null;
        ExitProcess(0);
    }

    private IEnumerator RunLeagueSelfTest()
    {
        var probe = new LeagueData();
        probe.ResetSeason();
        probe.BeginSeason();
        probe.ResolveMatch(true);
        probe.Save();
        var loaded = LeagueData.Load();
        var passed = loaded.Round == probe.Round && loaded.Wins == 1 && loaded.Points >= 3;
        var evidence = new LeagueEvidence
        {
            passed = passed,
            round = loaded.Round,
            wins = loaded.Wins,
            points = loaded.Points,
            saveKey = LeagueData.SaveKey,
            generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        if (!string.IsNullOrEmpty(evidencePath))
        {
            var folder = Path.GetDirectoryName(evidencePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }
            File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true) + "\n");
        }
        Debug.Log("UBLADE2_LEAGUE_SAVE_OK round=" + loaded.Round + " wins=" + loaded.Wins +
                  " points=" + loaded.Points + " key=" + LeagueData.SaveKey + " passed=" + passed);
        PlayerPrefs.DeleteKey(LeagueData.SaveKey);
        PlayerPrefs.Save();
        yield return new WaitForSeconds(0.2f);
        Application.Quit(passed ? 0 : 1);
        yield return null;
        ExitProcess(passed ? 0 : 1);
    }

    private void WriteMatchEvidence()
    {
        if (string.IsNullOrEmpty(evidencePath))
        {
            return;
        }
        var evidence = new MatchEvidence
        {
            passed = match != null && match.Phase == MatchPhase.Ended,
            playerScore = match != null ? match.PlayerScore : 0,
            aiScore = match != null ? match.AiScore : 0,
            winner = match != null ? match.Winner : 0,
            frames = matchFrames,
            phase = match != null ? match.Phase.ToString() : "none",
            duration = match != null ? match.Config.duration : 0f,
            targetScore = match != null ? match.Config.targetScore : 0,
            difficulty = match != null ? match.Config.difficulty : 0,
            arena = match != null ? match.Config.arenaName : "",
            round = match != null ? match.Config.round : 0,
            totalRounds = match != null ? match.Config.totalRounds : 0,
            playerTeam = match != null ? match.Config.playerTeam : "",
            aiTeam = match != null ? match.Config.aiTeam : "",
            screen = Screen.width + "x" + Screen.height,
            unity = Application.unityVersion,
            generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var folder = Path.GetDirectoryName(evidencePath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }
        File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true) + "\n");
        Debug.Log("UBLADE2_MATCH_EVIDENCE passed=" + evidence.passed + " player=" + evidence.playerScore +
                  " ai=" + evidence.aiScore + " winner=" + evidence.winner + " frames=" + evidence.frames);
    }

    private void WriteEvidence()
    {
        if (string.IsNullOrEmpty(evidencePath))
        {
            return;
        }
        var evidence = new RuntimeEvidence
        {
            passed = hero != null && hero.heroVisible,
            heroVisible = hero != null && hero.heroVisible,
            lodLevel = hero != null ? hero.lodLevel : -1,
            smileWeight = hero != null ? hero.smileWeight : 0f,
            capeBones = hero != null ? hero.capeBones.Length : 0,
            bones = hero != null ? hero.bones : 0,
            lod0Triangles = hero != null ? hero.lod0Triangles : 0,
            lod1Triangles = hero != null ? hero.lod1Triangles : 0,
            lod2Triangles = hero != null ? hero.lod2Triangles : 0,
            collected = collected,
            frames = frames,
            heroRectX = hero != null ? hero.heroRectX : 0,
            heroRectY = hero != null ? hero.heroRectY : 0,
            heroRectWidth = hero != null ? hero.heroRectWidth : 0,
            heroRectHeight = hero != null ? hero.heroRectHeight : 0,
            heroScreenPixels = hero != null ? hero.heroRectWidth * hero.heroRectHeight : 0,
            screen = Screen.width + "x" + Screen.height,
            unity = Application.unityVersion,
            captureFile = capturePath ?? "",
            generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var folder = Path.GetDirectoryName(evidencePath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }
        File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true) + "\n");
        Debug.Log("UBLADE_HERO_RUNTIME_OK visible=" + evidence.heroVisible +
                  " lod=" + evidence.lodLevel +
                  " smile=" + evidence.smileWeight.ToString("0.0") +
                  " cape=" + evidence.capeBones +
                  " rect=" + evidence.heroRectX + "," + evidence.heroRectY + "," + evidence.heroRectWidth + "x" + evidence.heroRectHeight +
                  " bones=" + evidence.bones +
                  " lod0Tris=" + evidence.lod0Triangles +
                  " lod1Tris=" + evidence.lod1Triangles +
                  " lod2Tris=" + evidence.lod2Triangles);
    }

    private void ApplyDisplaySettings()
    {
        if (Application.isMobilePlatform || settings == null || Application.isBatchMode)
        {
            return;
        }
        try
        {
            Screen.fullScreen = settings.fullscreen;
        }
        catch (Exception)
        {
        }
    }

    private void SaveProgress()
    {
        if (settings != null)
        {
            settings.Save("ublade2.settings.");
        }
        if (league != null && !autoplay)
        {
            league.Save();
        }
    }

    private void OnApplicationQuit()
    {
        if (demo || matchMode || captureAutoplay || autoplay || !string.IsNullOrEmpty(capturePath) ||
            !string.IsNullOrEmpty(menuCapturePath) || !string.IsNullOrEmpty(uiCaptureDir))
        {
            return;
        }
        SaveProgress();
    }

    private static void ExitProcess(int code)
    {
        try
        {
            Environment.Exit(code);
        }
        catch (Exception)
        {
        }
    }

    private void LateUpdate()
    {
        if (cam == null || player == null)
        {
            return;
        }
        var offset = player.rotation * new Vector3(1.9f, 2.4f, -4.6f);
        cam.transform.position = player.position + offset;
        cam.transform.LookAt(player.position + new Vector3(0f, 1.05f, 0f));
    }
}
