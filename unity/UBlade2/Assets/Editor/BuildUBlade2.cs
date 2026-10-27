using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildUBlade2
{
    private const string HeroDir = "Assets/Resources/Hero";
    private const string MaterialDir = "Assets/Resources/Hero/Materials";
    private const string ControllerPath = "Assets/Resources/Hero/WayfarerController.controller";
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string BuildPath = "Build/UBlade2.exe";

    private static string RepoRoot
    {
        get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..")); }
    }

    private static string ArtifactDir
    {
        get { return Path.Combine(RepoRoot, "artifacts", "ublade2"); }
    }

    [Serializable]
    public class HeroImportReport
    {
        public string schema = "ublade2.hero.import/v1";
        public string generatedAt;
        public int bones;
        public string[] clips;
        public string smile;
        public int blendShapes;
        public int maps;
        public int lod0Triangles;
        public int lod1Triangles;
        public int lod2Triangles;
        public float lod0Height;
        public int prefabLodLevels;
        public string prefabPath;
        public string[] lod0MaterialNames;
        public string[] missing;
        public bool passed;
    }


    private static string ReadVersion()
    {
        try
        {
            var path = Path.Combine(RepoRoot, "BuildNotes.md");
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    if (line.StartsWith("Current version:", StringComparison.Ordinal))
                    {
                        return line.Substring("Current version:".Length).Trim();
                    }
                }
            }
        }
        catch (Exception)
        {
        }
        return "0.0.0";
    }


    private static bool ContainsShader(SerializedProperty array, Shader shader)
    {
        for (int index = 0; index < array.arraySize; index++)
        {
            if (array.GetArrayElementAtIndex(index).objectReferenceValue == shader)
            {
                return true;
            }
        }
        return false;
    }

    private static void AddAlwaysIncludedShader(SerializedProperty array, Shader shader)
    {
        if (shader == null || ContainsShader(array, shader))
        {
            return;
        }
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        array.GetArrayElementAtIndex(index).objectReferenceValue = shader;
    }

    private static void EnsureMobileShaders()
    {
        var unlitColor = Shader.Find("Unlit/Color");
        var unlitTexture = Shader.Find("Unlit/Texture");
        if (unlitColor == null || unlitTexture == null)
        {
            Debug.LogWarning("UBLADE2_MOBILE_SHADER_WARN missing unlit shader");
            return;
        }
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/GraphicsSettings.asset");
        if (asset == null)
        {
            Debug.LogWarning("UBLADE2_MOBILE_SHADER_WARN missing GraphicsSettings.asset");
            return;
        }
        var serialized = new SerializedObject(asset);
        var array = serialized.FindProperty("m_AlwaysIncludedShaders");
        if (array == null)
        {
            Debug.LogWarning("UBLADE2_MOBILE_SHADER_WARN missing always-included list");
            return;
        }
        AddAlwaysIncludedShader(array, unlitColor);
        AddAlwaysIncludedShader(array, unlitTexture);
        serialized.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("UBLADE2_MOBILE_SHADER_OK unlit-color unlit-texture always-included");
    }

    [MenuItem("UBlade2/Inspect hero import")]
    public static void Inspect()
    {
        int code = 0;
        try
        {
            var report = VerifyHero();
            WriteReport(report);
            LogReport(report);
            if (!report.passed)
            {
                code = 1;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("UBLADE2_INSPECT FAILED " + exception);
            code = 1;
        }
        EditorApplication.Exit(code);
    }

    [MenuItem("UBlade2/Build Windows")]
    public static void Build()
    {
        try
        {
            EnsureMobileShaders();
            EnsureClipLoopSettings();
            EnsureModelReadable("wayfarer");
            EnsureModelReadable("wayfarer_lod1");
            EnsureModelReadable("wayfarer_lod2");
            EnsureMaterials();
            var clips = LoadClips();
            var controller = EnsureAnimatorController(clips);
            EnsureHeroPrefab(controller);
            var report = VerifyHero();
            WriteReport(report);
            LogReport(report);
            if (!report.passed)
            {
                throw new Exception("hero import missing: " + string.Join(",", report.missing));
            }

            PlayerSettings.productName = "UBlade2";
            PlayerSettings.companyName = "C7-IMI";
            PlayerSettings.bundleVersion = ReadVersion();
            Debug.Log("UBLADE2_VERSION " + PlayerSettings.bundleVersion);
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = true;

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var host = new GameObject("UBlade2");
            host.AddComponent<UBlade2Game>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Directory.CreateDirectory("Build");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var build = BuildPipeline.BuildPlayer(options);
            Debug.Log("UBLADE2_BUILD " + build.summary.result + " bytes=" + build.summary.totalSize);
            if (build.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("build result " + build.summary.result);
            }
            Directory.CreateDirectory(ArtifactDir);
            File.WriteAllText(
                Path.Combine(ArtifactDir, "build.json"),
                "{\"schema\":\"ublade2.build/v1\",\"passed\":true,\"result\":\"" + build.summary.result +
                "\",\"bytes\":" + build.summary.totalSize + ",\"exe\":\"unity/UBlade2/Build/UBlade2.exe\"}\n");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("UBLADE2_BUILD FAILED " + exception);
            EditorApplication.Exit(1);
        }
    }


    [MenuItem("UBlade2/Build Android APK")]
    public static void BuildAndroid()
    {
        try
        {
            EnsureMobileShaders();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    throw new Exception("could not switch the active build target to Android");
                }
            }
            foreach (var architectureName in System.Enum.GetNames(typeof(AndroidArchitecture)))
            {
                Debug.Log("UBLADE2_ANDROID_ARCH " + architectureName);
            }
            Debug.Log("UBLADE2_ANDROID_SDKROOT " + EditorPrefs.GetString("AndroidSdkRoot", "(bundled)"));
            Debug.Log("UBLADE2_ANDROID_JDKROOT " + EditorPrefs.GetString("AndroidJdkRoot", "(bundled)"));
            Debug.Log("UBLADE2_ANDROID_NDKROOT " + EditorPrefs.GetString("AndroidNdkRoot", "(bundled)"));

            PlayerSettings.productName = "UBlade2";
            PlayerSettings.companyName = "C7-IMI";
            PlayerSettings.bundleVersion = ReadVersion();
            Debug.Log("UBLADE2_VERSION " + PlayerSettings.bundleVersion);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.c7imi.ublade2");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan });

            Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var host = new GameObject("UBlade2");
                host.AddComponent<UBlade2Game>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Directory.CreateDirectory("Build/Android");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/Android/UBlade2.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var build = BuildPipeline.BuildPlayer(options);
            Debug.Log("UBLADE2_ANDROID_BUILD " + build.summary.result + " bytes=" + build.summary.totalSize);
            if (build.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("android build result " + build.summary.result);
            }
            var artifact = Path.Combine(RepoRoot, "artifacts", "ublade2-android");
            Directory.CreateDirectory(artifact);
            File.WriteAllText(
                Path.Combine(artifact, "build.json"),
                "{\"schema\":\"ublade2.android.build/v1\",\"passed\":true,\"result\":\"" + build.summary.result +
                "\",\"bytes\":" + build.summary.totalSize + ",\"apk\":\"unity/UBlade2/Build/Android/UBlade2.apk\"}\n");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("UBLADE2_ANDROID_BUILD FAILED " + exception);
            EditorApplication.Exit(1);
        }
    }

    private static void LogReport(HeroImportReport report)
    {
        Debug.Log("WAYFARER_IMPORT_OK bones=" + report.bones +
                  " clips=" + string.Join(",", report.clips) +
                  " smile=" + report.smile +
                  " maps=" + report.maps +
                  " lod0Tris=" + report.lod0Triangles +
                  " lod1Tris=" + report.lod1Triangles +
                  " lod2Tris=" + report.lod2Triangles);
        Debug.Log("WAYFARER_PREFAB_OK path=" + report.prefabPath + " lods=" + report.prefabLodLevels);
        Debug.Log("WAYFARER_IMPORT_DETAIL height=" + report.lod0Height.ToString("0.000") +
                  " materials=" + string.Join(",", report.lod0MaterialNames) +
                  " missing=" + (report.missing.Length == 0 ? "none" : string.Join(",", report.missing)));
    }

    private static void WriteReport(HeroImportReport report)
    {
        Directory.CreateDirectory(ArtifactDir);
        File.WriteAllText(Path.Combine(ArtifactDir, "hero-import.json"), JsonUtility.ToJson(report, true) + "\n");
    }

    private static HeroImportReport VerifyHero()
    {
        var missing = new List<string>();
        var report = new HeroImportReport();
        report.generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var model0 = LoadModel("wayfarer");
        var model1 = LoadModel("wayfarer_lod1");
        var model2 = LoadModel("wayfarer_lod2");
        var lod0 = FirstSkinned(model0, "wayfarer");
        var lod1 = FirstSkinned(model1, "wayfarer_lod1");
        var lod2 = FirstSkinned(model2, "wayfarer_lod2");

        report.bones = lod0.bones.Length;
        if (report.bones != 23)
        {
            missing.Add("bones=" + report.bones + " expected 23");
        }

        var clips = AssetDatabase.LoadAllAssetsAtPath(HeroDir + "/wayfarer.fbx")
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__"))
            .Select(clip => clip.name)
            .OrderBy(name => name)
            .ToArray();
        report.clips = clips;
        foreach (var wanted in new[] { "idle", "run", "reach" })
        {
            if (!clips.Any(clip => clip.ToLowerInvariant().Contains(wanted)))
            {
                missing.Add("clip:" + wanted);
            }
        }

        var mesh0 = lod0.sharedMesh;
        report.blendShapes = mesh0 != null ? mesh0.blendShapeCount : 0;
        report.smile = "missing";
        if (mesh0 != null)
        {
            for (int index = 0; index < mesh0.blendShapeCount; index++)
            {
                if (mesh0.GetBlendShapeName(index).ToLowerInvariant().Contains("smile"))
                {
                    report.smile = mesh0.GetBlendShapeName(index);
                }
            }
        }
        if (report.smile == "missing")
        {
            missing.Add("smile blend shape");
        }

        report.lod0Triangles = TriangleCount(mesh0);
        report.lod1Triangles = TriangleCount(lod1.sharedMesh);
        report.lod2Triangles = TriangleCount(lod2.sharedMesh);
        report.lod0Height = lod0.bounds.size.y;
        if (report.lod0Height < 1.0f || report.lod0Height > 3.0f)
        {
            missing.Add("height=" + report.lod0Height.ToString("0.00") + " expected about 1.9");
        }
        if (report.lod1Triangles >= report.lod0Triangles || report.lod2Triangles >= report.lod1Triangles)
        {
            missing.Add("lod triangles not decreasing: " + report.lod0Triangles + "/" + report.lod1Triangles + "/" + report.lod2Triangles);
        }

        var maps = 0;
        foreach (var map in new[] { "wayfarer_atlas.png", "wayfarer_normal.png", "wayfarer_roughmetal.png" })
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(HeroDir + "/textures/" + map);
            if (texture == null)
            {
                missing.Add("map:" + map + " missing");
            }
            else if (texture.width != 512 || texture.height != 512)
            {
                missing.Add("map:" + map + "=" + texture.width + "x" + texture.height + " expected 512x512");
            }
            else
            {
                maps++;
            }
        }
        report.maps = maps;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroDir + "/WayfarerLOD.prefab");
        report.prefabPath = HeroDir + "/WayfarerLOD.prefab";
        report.prefabLodLevels = 0;
        if (prefab == null)
        {
            missing.Add("hero prefab " + report.prefabPath);
        }
        else
        {
            var group = prefab.GetComponent<LODGroup>();
            if (group == null)
            {
                missing.Add("hero prefab LODGroup");
            }
            else
            {
                report.prefabLodLevels = group.GetLODs().Length;
                if (report.prefabLodLevels != 3)
                {
                    missing.Add("hero prefab LOD levels=" + report.prefabLodLevels);
                }
            }
        }

        report.lod0MaterialNames = lod0.sharedMaterials
            .Select(material => material == null ? "(null)" : material.name)
            .ToArray();
        report.missing = missing.ToArray();
        report.passed = missing.Count == 0;
        return report;
    }

    private static int TriangleCount(Mesh mesh)
    {
        return mesh == null || mesh.triangles == null ? 0 : mesh.triangles.Length / 3;
    }

    private static GameObject LoadModel(string name)
    {
        var path = HeroDir + "/" + name + ".fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null)
        {
            throw new Exception("missing hero model " + path);
        }
        return model;
    }

    private static SkinnedMeshRenderer FirstSkinned(GameObject model, string label)
    {
        var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault();
        if (renderer == null)
        {
            throw new Exception("no SkinnedMeshRenderer in " + label);
        }
        return renderer;
    }

    private static AnimationClip[] LoadClips()
    {
        return AssetDatabase.LoadAllAssetsAtPath(HeroDir + "/wayfarer.fbx")
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__"))
            .ToArray();
    }

    private static AnimationClip FindClip(AnimationClip[] clips, string wanted)
    {
        return clips.FirstOrDefault(clip => clip.name.ToLowerInvariant().Contains(wanted));
    }

    private static void EnsureClipLoopSettings()
    {
        var importer = AssetImporter.GetAtPath(HeroDir + "/wayfarer.fbx") as ModelImporter;
        if (importer == null)
        {
            return;
        }
        importer.importAnimation = true;
        if (importer.animationType != ModelImporterAnimationType.Generic && importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
        }
        var clutches = importer.clipAnimations;
        if (clutches == null || clutches.Length == 0)
        {
            clutches = importer.defaultClipAnimations;
        }
        if (clutches == null || clutches.Length == 0)
        {
            return;
        }
        foreach (var clip in clutches)
        {
            var name = clip.name.ToLowerInvariant();
            clip.loopTime = name.Contains("idle") || name.Contains("run");
        }
        importer.clipAnimations = clutches;
        importer.SaveAndReimport();
    }

    private static void EnsureModelReadable(string name)
    {
        var importer = AssetImporter.GetAtPath(HeroDir + "/" + name + ".fbx") as ModelImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }


    private static void EnsureHeroPrefab(RuntimeAnimatorController controller)
    {
        var path = HeroDir + "/WayfarerLOD.prefab";
        AssetDatabase.DeleteAsset(path);
        var root = new GameObject("WayfarerLOD");
        try
        {
            // The Blender/glTF model faces -Z; the CharacterController's forward is +Z in Unity.
            // Rotate the visual so the hero looks where the controller moves.
            root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var modelNames = new[] { "wayfarer", "wayfarer_lod1", "wayfarer_lod2" };
            var renderers = new Renderer[3];
            for (int index = 0; index < modelNames.Length; index++)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(HeroDir + "/" + modelNames[index] + ".fbx");
                if (model == null)
                {
                    throw new Exception("missing hero model " + modelNames[index]);
                }
                var instance = UnityEngine.Object.Instantiate(model);
                instance.name = "LOD" + index;
                instance.transform.SetParent(root.transform, false);
                var animator = instance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                renderers[index] = FirstSkinned(instance, modelNames[index]);
            }
            var group = root.AddComponent<LODGroup>();
            var lods = new LOD[3];
            lods[0] = new LOD(0.30f, new Renderer[] { renderers[0] });
            lods[1] = new LOD(0.12f, new Renderer[] { renderers[1] });
            lods[2] = new LOD(0.04f, new Renderer[] { renderers[2] });
            group.SetLODs(lods);
            group.RecalculateBounds();
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (saved == null)
            {
                throw new Exception("could not save hero prefab " + path);
            }
            Debug.Log("UBLADE_PREFAB_SAVED path=" + path + " lods=3");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void EnsureMaterials()
    {
        Directory.CreateDirectory(MaterialDir);
        AssetDatabase.Refresh();
        var normalPath = HeroDir + "/textures/wayfarer_normal.png";
        var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(HeroDir + "/textures/wayfarer_atlas.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            throw new Exception("Standard shader not found");
        }
        var tints = new Dictionary<string, Color>
        {
            { "Wayfarer_Cloth", new Color(0.34f, 0.40f, 0.50f) },
            { "Wayfarer_Leather", new Color(0.48f, 0.38f, 0.30f) },
            { "Wayfarer_Skin", new Color(0.86f, 0.72f, 0.62f) },
            { "Wayfarer_Accent", new Color(0.18f, 0.72f, 0.86f) }
        };
        foreach (var pair in tints)
        {
            var path = MaterialDir + "/" + pair.Key + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_MainTex", atlas);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Metallic", 0.15f);
            material.SetFloat("_Glossiness", 0.38f);
            material.color = Color.white;
            material.SetColor("_Color", pair.Value);
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("UBLADE_MATERIALS_OK count=" + tints.Count +
                  " atlas=" + (atlas == null ? "missing" : atlas.width + "x" + atlas.height) +
                  " normal=" + (normal == null ? "missing" : normal.width + "x" + normal.height));
    }

    private static RuntimeAnimatorController EnsureAnimatorController(AnimationClip[] clips)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var idle = FindClip(clips, "idle");
        var run = FindClip(clips, "run");
        var reach = FindClip(clips, "reach");
        if (idle == null || run == null || reach == null)
        {
            throw new Exception("missing clips idle/run/reach; have " + string.Join(",", clips.Select(clip => clip.name)));
        }
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Reach", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var idleState = machine.AddState("Idle");
        idleState.motion = idle;
        var runState = machine.AddState("Run");
        runState.motion = run;
        var reachState = machine.AddState("Reach");
        reachState.motion = reach;
        machine.defaultState = idleState;
        var toRun = idleState.AddTransition(runState);
        toRun.hasExitTime = false;
        toRun.duration = 0.15f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var toIdle = runState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.2f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        var toReach = machine.AddAnyStateTransition(reachState);
        toReach.hasExitTime = false;
        toReach.duration = 0.08f;
        toReach.canTransitionToSelf = false;
        toReach.AddCondition(AnimatorConditionMode.If, 0f, "Reach");
        var outReach = reachState.AddTransition(idleState);
        outReach.hasExitTime = true;
        outReach.exitTime = 0.75f;
        outReach.duration = 0.1f;
        AssetDatabase.SaveAssets();
        Debug.Log("UBLADE_ANIMATOR_OK states=Idle,Run,Reach clips=" + idle.name + "," + run.name + "," + reach.name);
        return controller;
    }
}
