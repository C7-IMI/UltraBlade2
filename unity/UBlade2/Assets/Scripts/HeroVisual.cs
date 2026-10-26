using System.Linq;
using UnityEngine;

public class HeroVisual : MonoBehaviour
{
    public Animator[] animators = new Animator[0];
    public SkinnedMeshRenderer lod0;
    public SkinnedMeshRenderer lod1;
    public SkinnedMeshRenderer lod2;
    public LODGroup lodGroup;
    public Transform[] capeBones = new Transform[0];
    public int smileIndex = -1;
    public float smileWeight;
    public float targetSmile;
    public float speed;
    public int lodLevel;
    public bool heroVisible;
    public int heroRectX;
    public int heroRectY;
    public int heroRectWidth;
    public int heroRectHeight;
    public int bones;
    public int lod0Triangles;
    public int lod1Triangles;
    public int lod2Triangles;
    private float swingX;
    private float swingY;
    private int lastLoggedLevel = -1;

    public static HeroVisual Create(Transform parent, RuntimeAnimatorController controller)
    {
        var root = new GameObject("HeroVisual");
        root.transform.SetParent(parent, false);
        var visual = root.AddComponent<HeroVisual>();
        visual.Build(controller);
        return visual;
    }

    private void Build(RuntimeAnimatorController controller)
    {
        var prefab = Resources.Load<GameObject>("Hero/WayfarerLOD");
        if (prefab == null)
        {
            throw new System.Exception("hero prefab missing from Resources/Hero/WayfarerLOD");
        }
        var instance = Instantiate(prefab, transform);
        instance.name = "WayfarerLOD";
        lodGroup = instance.GetComponent<LODGroup>();
        if (lodGroup == null)
        {
            throw new System.Exception("hero prefab has no LODGroup");
        }
        var lods = lodGroup.GetLODs();
        if (lods.Length != 3)
        {
            throw new System.Exception("hero prefab LODGroup has " + lods.Length + " levels, expected 3");
        }
        lod0 = (SkinnedMeshRenderer)lods[0].renderers[0];
        lod1 = (SkinnedMeshRenderer)lods[1].renderers[0];
        lod2 = (SkinnedMeshRenderer)lods[2].renderers[0];
        if (lod0 == null || lod1 == null || lod2 == null)
        {
            throw new System.Exception("hero prefab LOD renderers are not skinned meshes");
        }
        bones = lod0.bones.Length;
        lod0Triangles = Triangles(lod0.sharedMesh);
        lod1Triangles = Triangles(lod1.sharedMesh);
        lod2Triangles = Triangles(lod2.sharedMesh);
        smileIndex = lod0.sharedMesh.GetBlendShapeIndex("Smile");
        if (smileIndex < 0)
        {
            for (int index = 0; index < lod0.sharedMesh.blendShapeCount; index++)
            {
                if (lod0.sharedMesh.GetBlendShapeName(index).ToLowerInvariant().Contains("smile"))
                {
                    smileIndex = index;
                }
            }
        }

        ApplyMaterials(lod0);
        ApplyMaterials(lod1);
        ApplyMaterials(lod2);
        animators = instance.GetComponentsInChildren<Animator>(true);
        foreach (var animator in animators)
        {
            if (animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = controller;
            }
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
        }
        capeBones = lod0.bones.Where(bone => bone != null && bone.name.ToLowerInvariant().StartsWith("cape_")).Take(3).ToArray();

        Debug.Log("UBLADE_HERO_OK lodLevels=3 bones=" + bones +
                  " smile=" + (smileIndex >= 0 ? lod0.sharedMesh.GetBlendShapeName(smileIndex) : "missing") +
                  " cape=" + capeBones.Length +
                  " lod0Tris=" + lod0Triangles +
                  " lod1Tris=" + lod1Triangles +
                  " lod2Tris=" + lod2Triangles +
                  " animators=" + animators.Length +
                  " prefab=WayfarerLOD");
    }

    private static SkinnedMeshRenderer FirstSkinned(GameObject root)
    {
        var renderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault();
        if (renderer == null)
        {
            throw new System.Exception("no SkinnedMeshRenderer under " + root.name);
        }
        return renderer;
    }

    private static int Triangles(Mesh mesh)
    {
        return mesh == null || mesh.triangles == null ? 0 : mesh.triangles.Length / 3;
    }

    private static Animator SetupAnimator(GameObject root, RuntimeAnimatorController controller)
    {
        var animator = root.GetComponent<Animator>();
        if (animator == null)
        {
            animator = root.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = controller;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.applyRootMotion = false;
        return animator;
    }

    private static void ApplyMaterials(SkinnedMeshRenderer renderer)
    {
        var library = Resources.LoadAll<Material>("Hero/Materials");
        if (library == null || library.Length == 0)
        {
            return;
        }
        var current = renderer.sharedMaterials;
        var next = new Material[current.Length];
        for (int index = 0; index < current.Length; index++)
        {
            var name = current[index] != null ? current[index].name.ToLowerInvariant() : "";
            Material match = null;
            foreach (var candidate in library)
            {
                var candidateName = candidate.name.ToLowerInvariant();
                var key = candidateName.Replace("wayfarer_", "");
                if (name.Contains(key))
                {
                    match = candidate;
                    break;
                }
            }
            if (match == null && index < library.Length)
            {
                match = library[index];
            }
            next[index] = match != null ? match : current[index];
        }
        renderer.sharedMaterials = next;
    }

    public void SetMotion(float planarSpeed)
    {
        speed = planarSpeed;
        if (animators == null)
        {
            return;
        }
        foreach (var animator in animators)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", planarSpeed);
            }
        }
    }

    public void PlayReach()
    {
        targetSmile = 100f;
        if (animators == null)
        {
            return;
        }
        foreach (var animator in animators)
        {
            if (animator != null)
            {
                animator.SetTrigger("Reach");
            }
        }
    }

    private void LateUpdate()
    {
        if (lod0 == null || animators == null || animators.Length == 0)
        {
            return;
        }
        smileWeight = Mathf.MoveTowards(smileWeight, targetSmile, Time.deltaTime * 600f);
        if (smileIndex >= 0)
        {
            lod0.SetBlendShapeWeight(smileIndex, smileWeight);
        }
        if (capeBones != null && capeBones.Length > 0)
        {
            swingX = Mathf.Lerp(swingX, speed * 5f, Time.deltaTime * 3f);
            swingY = Mathf.Sin(Time.time * 4f) * speed * 1.5f;
            foreach (var bone in capeBones)
            {
                if (bone != null)
                {
                    bone.localRotation = bone.localRotation * Quaternion.Euler(swingX, swingY, 0f);
                }
            }
        }
        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }
        var distance = Vector3.Distance(camera.transform.position, transform.position + Vector3.up * 1.0f);
        var lods = lodGroup.GetLODs();
        var height = lodGroup.size * 0.5f / Mathf.Max(0.01f, distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
        var level = lods.Length - 1;
        for (int index = 0; index < lods.Length; index++)
        {
            if (height >= lods[index].screenRelativeTransitionHeight)
            {
                level = index;
                break;
            }
        }
        lodLevel = level;
        if (level != lastLoggedLevel)
        {
            lastLoggedLevel = level;
            Debug.Log("UBLADE_LOD level=" + level +
                      " distance=" + distance.ToString("0.00") +
                      " height=" + height.ToString("0.000") +
                      " transitionHeights=0.30/0.12/0.04");
        }
        var bounds = lod0.bounds;
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;
        var center = bounds.center;
        var extents = bounds.extents;
        for (int corner = 0; corner < 8; corner++)
        {
            var point = center + new Vector3(
                ((corner & 1) == 0 ? -extents.x : extents.x),
                ((corner & 2) == 0 ? -extents.y : extents.y),
                ((corner & 4) == 0 ? -extents.z : extents.z));
            var viewport = camera.WorldToViewportPoint(point);
            if (viewport.z <= 0f)
            {
                continue;
            }
            minX = Mathf.Min(minX, viewport.x);
            minY = Mathf.Min(minY, viewport.y);
            maxX = Mathf.Max(maxX, viewport.x);
            maxY = Mathf.Max(maxY, viewport.y);
        }
        if (minX <= maxX && minY <= maxY)
        {
            heroRectX = Mathf.Clamp(Mathf.RoundToInt(minX * Screen.width), 0, Screen.width - 1);
            heroRectY = Mathf.Clamp(Mathf.RoundToInt((1f - maxY) * Screen.height), 0, Screen.height - 1);
            heroRectWidth = Mathf.Clamp(Mathf.RoundToInt((maxX - minX) * Screen.width), 1, Screen.width - heroRectX);
            heroRectHeight = Mathf.Clamp(Mathf.RoundToInt((maxY - minY) * Screen.height), 1, Screen.height - heroRectY);
        }
        heroVisible = heroRectWidth * heroRectHeight > 2000 &&
                      heroRectX >= 0 && heroRectY >= 0 &&
                      heroRectX + heroRectWidth <= Screen.width &&
                      heroRectY + heroRectHeight <= Screen.height;
    }

    public int LevelForDistance(float distance)
    {
        var lods = lodGroup.GetLODs();
        var height = lodGroup.size * 0.5f / Mathf.Max(0.01f, distance * Mathf.Tan(55f * 0.5f * Mathf.Deg2Rad));
        for (int index = 0; index < lods.Length; index++)
        {
            if (height >= lods[index].screenRelativeTransitionHeight)
            {
                return index;
            }
        }
        return lods.Length - 1;
    }
}
