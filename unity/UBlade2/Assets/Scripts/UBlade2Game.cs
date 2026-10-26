using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class UBlade2Game : MonoBehaviour
{
    private readonly List<GameObject> crystals = new List<GameObject>();
    private CharacterController controller;
    private Transform player;
    private Camera cam;
    private HeroVisual hero;
    private int collected;
    private int frames;
    private bool demo;
    private string capturePath;
    private string evidencePath;
    private float vy;
    private const float Speed = 6f;
    private const int MaxDemoFrames = 900;

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
#if UNITY_ANDROID && !UNITY_EDITOR
        demo = demo || AndroidIntentBool("ublade2_demo");
#endif
        capturePath = GetArg("--capture=");
        evidencePath = GetArg("--evidence=");
        BuildWorld();
        Debug.Log("UBLADE2_START demo=" + demo + " crystals=" + crystals.Count +
                  " unity=" + Application.unityVersion +
                  " screen=" + Screen.width + "x" + Screen.height +
                  " capture=" + (string.IsNullOrEmpty(capturePath) ? "none" : capturePath));
        if (demo)
        {
            StartCoroutine(RunDemo());
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
        // The emulator's Vulkan/gfxstream path renders the lit Standard shader almost black.
        // Use a bright unlit fallback on mobile so the device frame shows the world and hero.
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

    private void BuildWorld()
    {
        RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.24f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.06f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(40f, 1f, 40f);
        floor.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.07f, 0.10f, 0.13f), 0f);

        AddPlatform(new Vector3(-5f, 0.25f, -4f), new Vector3(4f, 0.5f, 4f), new Color(0.12f, 0.20f, 0.25f));
        AddPlatform(new Vector3(5f, 0.5f, -3f), new Vector3(3f, 1f, 3f), new Color(0.12f, 0.20f, 0.25f));
        AddPlatform(new Vector3(-2f, 0.75f, 5f), new Vector3(3f, 1.5f, 3f), new Color(0.12f, 0.20f, 0.25f));

        var playerObject = new GameObject("Player");
        playerObject.transform.position = new Vector3(0f, 0.05f, 0f);
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
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.7f);
        }

        var crystalColor = new Color(0.16f, 0.86f, 0.76f);
        AddCrystal("Crystal_A", new Vector3(3f, 1.1f, 0f), crystalColor);
        AddCrystal("Crystal_B", new Vector3(-3f, 1.1f, 2f), crystalColor);
        AddCrystal("Crystal_C", new Vector3(0f, 1.1f, -3f), crystalColor);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cam = cameraObject.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
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

        Debug.Log("UBLADE2_WORLD crystals=" + crystals.Count + " speed=" + Speed + " hero=Wayfarer");
    }

    private void AddPlatform(Vector3 position, Vector3 scale, Color color)
    {
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Platform";
        platform.transform.position = position;
        platform.transform.localScale = scale;
        platform.GetComponent<Renderer>().sharedMaterial = Mat(color, 0f);
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
        if (demo || controller == null)
        {
            if (hero != null)
            {
                hero.SetMotion(0f);
            }
            return;
        }
        var horizontal = 0f;
        var vertical = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical = -1f;
        var direction = new Vector3(horizontal, 0f, vertical);
        MovePlayer(direction, Time.deltaTime);
        hero.SetMotion(direction.magnitude * Speed);
        CollectNearby();
    }

    private void MovePlayer(Vector3 direction, float deltaTime)
    {
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
        controller.Move(planar * Speed * deltaTime + Vector3.up * vy * deltaTime);
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
            Environment.Exit(0);
        }
        else
        {
            Debug.Log("GYPSY_DEMO_FAIL collected=" + collected + " frames=" + frames);
            Application.Quit(1);
            yield return null;
            Environment.Exit(1);
        }
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
