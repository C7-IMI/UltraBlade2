using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Runtime, low-poly, flat-shaded HyperBlade drome built entirely from generated meshes.
//
// The rink is an ELLIPSE (ArenaDefinition.radiusX / radiusZ), not a rectangle: the floor is an
// elliptical disc, the outer wall is an elliptical band that leans inward, both goals are cut into
// that band, and the crowd, jumbotrons, ceiling and floodlights sit above it. Every surface is a
// flat shaded quad or triangle with a solid colour material, so the result reads as the 1996
// original (grey floor, red/white/blue paint, dark grey wall, team colour panels) without art.
//
// Everything is created under one "Drome" child of the supplied parent. Build() calls Clear() first,
// so rebuilding never stacks two dromes. The builder only creates visuals and colliders: it does not
// touch RenderSettings, cameras, the character controller or gameplay state.
//
// Collision: the floor and the inward wall surface carry MeshColliders (props deliberately do not),
// plus a simple shell behind each goal so a CharacterController cannot skate out of the world
// through a goal mouth. Returned positions are in the drome parent's local space; with a parent at
// the world origin they are world positions.

/// <summary>One visual drome prop built from <see cref="ArenaDefinition.props"/>. Visual only.</summary>
public sealed class DromePropPlacement
{
    /// <summary>Prop family, straight from the arena data.</summary>
    public DromePropKind kind;

    /// <summary>Floor position of the prop (y is always 0).</summary>
    public Vector3 position;

    /// <summary>Yaw the prop was built with, in degrees. Ramps, gates and barriers face the rink centre.</summary>
    public float yaw;
}

/// <summary>Everything the gameplay host needs after the drome was built.</summary>
public sealed class DromeInstance
{
    /// <summary>Root of the generated drome. Destroying it removes the whole arena.</summary>
    public Transform root;

    /// <summary>Centre of the north goal mouth, on the floor at the goal line.</summary>
    public Vector3 goalNorthMouth;

    /// <summary>Centre of the south goal mouth, on the floor at the goal line.</summary>
    public Vector3 goalSouthMouth;

    /// <summary>Centre face-off spot, 0.4 above the floor so a spherical rok rests without clipping.</summary>
    public Vector3 rokSpawn;

    /// <summary>Placements for every prop that was built, in arena data order.</summary>
    public DromePropPlacement[] props;

    /// <summary>Rink radius along X.</summary>
    public float radiusX;

    /// <summary>Rink radius along Z.</summary>
    public float radiusZ;
}

/// <summary>
/// Builds the HyperBlade drome at runtime: elliptical painted floor, inward leaning wall with goals
/// cut into it, tiered crowd, jumbotrons, ceiling, floodlights and the arena props.
/// </summary>
public static class DromeBuilder
{
    /// <summary>Nominal clearance between the goal posts, in metres.</summary>
    public const float GoalWidth = 10.0f;

    /// <summary>Nominal clearance between the floor and the goal crossbar, in metres.</summary>
    public const float GoalHeight = 5.5f;

    /// <summary>Height of the rink wall, in metres.</summary>
    public const float WallHeight = 40f;

    /// <summary>How far the top of the wall leans inward, as a fraction of the floor radius.</summary>
    public const float WallTopScale = 0.45f;

    /// <summary>Name of the generated child the builder owns.</summary>
    public const string RootObjectName = "Drome";

    private const int FloorSegments = 72;
    private const int FloorRings = 20;
    private const int WallSegments = 64;
    private const int CrowdSegments = 60;
    private const int CrowdTiers = 3;
    private const int MarkArcSegments = 40;
    private const int CrowdTrimSegments = 72;
    private const float GoalDepth = 2.4f;
    private const float WallThickness = 0.36f;
    private const float CrowdBaseHeight = 24f;
    private const float CrowdTierRise = 1.45f;
    private const float StandsTopHeight = 44f;
    private const float CeilingHeight = 90f;
    private const float CeilingScale = 1.7f;
    private const float PropRadiusLimit = 0.92f;
    private const int PropSeed = 1996;
    private const float Deg2Rad = Mathf.Deg2Rad;
    private const float Rad2Deg = Mathf.Rad2Deg;
    private const float TwoPi = Mathf.PI * 2f;

    /// <summary>
    /// Builds a drome under a new child of <paramref name="parent"/> and returns its instance data.
    /// Any previous drome under the same parent is destroyed first.
    /// </summary>
    /// <param name="parent">Host transform; the drome is added as a child named "Drome".</param>
    /// <param name="arena">Arena data (radii, goal positions, floor colour, props).</param>
    /// <param name="homeColor">Team colour of the north end / home side.</param>
    /// <param name="awayColor">Team colour of the south end / away side.</param>
    public static DromeInstance Build(Transform parent, ArenaDefinition arena, Color homeColor, Color awayColor)
    {
        if (parent == null)
        {
            throw new System.ArgumentNullException("parent");
        }
        if (arena == null)
        {
            throw new System.ArgumentNullException("arena");
        }

        Clear(parent);

        var rootObject = new GameObject(RootObjectName);
        Transform root = rootObject.transform;
        root.SetParent(parent, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        float radiusX = Mathf.Max(6f, arena.radiusX);
        float radiusZ = Mathf.Max(6f, arena.radiusZ);

        Shader shader = FindShader();
        if (shader == null)
        {
            Debug.LogError("DromeBuilder: no usable shader (Standard, Legacy Shaders/Diffuse, Unlit/Color).");
        }
        Palette palette = CreatePalette(arena, homeColor, awayColor, shader);

        Vector2 northPoint = arena.goalNorth;
        Vector2 southPoint = arena.goalSouth;
        if (northPoint.sqrMagnitude < 0.0001f)
        {
            northPoint = new Vector2(0f, radiusZ * 0.94f);
        }
        if (southPoint.sqrMagnitude < 0.0001f)
        {
            southPoint = new Vector2(0f, radiusZ * -0.94f);
        }
        GoalSpec north = MakeGoalSpec(northPoint, radiusX, radiusZ);
        GoalSpec south = MakeGoalSpec(southPoint, radiusX, radiusZ);

        Transform colliderRoot = CreateGroup(root, "Colliders");

        // Floor: a shallow elliptical half-pipe bowl (flat at the two goal ends so the
        // small wall goals stay clear), painted and collided as one surface.
        var floorBuilder = new MeshBuilder();
        AddBowlFloor(floorBuilder, radiusX, radiusZ, BowlDepth, FloorRings, FloorSegments);
        Mesh floorMesh = floorBuilder.ToMesh("Drome Floor");
        GameObject floorObject = CreateMeshObject(root, "Floor", floorMesh, palette[Mat.Floor]);
        var floorCollider = floorObject.AddComponent<MeshCollider>();
        floorCollider.sharedMesh = floorMesh;
        floorCollider.convex = false;

        // Painted markings sit a few millimetres above the floor.
        var markings = new Batch(palette);
        BuildMarkings(markings, radiusX, radiusZ, north, south);

        // Curved outer wall plus the inner collision shell.
        var walls = new Batch(palette);
        var wallCollider = new MeshBuilder();
        float[] wallAngles = BuildWallAngles(north, south);
        BuildWall(walls, wallCollider, radiusX, radiusZ, north, south, wallAngles);
        walls.Flush(CreateGroup(root, "Walls"), "Wall");

        var wallColliderObject = new GameObject("WallCollider");
        wallColliderObject.transform.SetParent(colliderRoot, false);
        var wallMeshCollider = wallColliderObject.AddComponent<MeshCollider>();
        wallMeshCollider.sharedMesh = wallCollider.ToMesh("Drome Wall Collider");

        // Goals cut into the wall, with the recessed case, net and crease.
        var goals = new Batch(palette);
        var goalCollider = new MeshBuilder();
        BuildGoal(goals, markings, goalCollider, north, 0);
        BuildGoal(goals, markings, goalCollider, south, 1);
        goals.Flush(CreateGroup(root, "Goals"), "Goal");
        markings.Flush(CreateGroup(root, "Markings"), "Mark");

        var goalColliderObject = new GameObject("GoalColliders");
        goalColliderObject.transform.SetParent(colliderRoot, false);
        var goalMeshCollider = goalColliderObject.AddComponent<MeshCollider>();
        goalMeshCollider.sharedMesh = goalCollider.ToMesh("Drome Goal Collider");

        // Arena props from the data. Visual only: no colliders are added to any prop.
        DromePropPlacement[] placements = BuildPropPlacements(arena, radiusX, radiusZ);
        var props = new Batch(palette);
        Transform propsRoot = CreateGroup(root, "Props");
        for (int index = 0; index < placements.Length; index++)
        {
            BuildProp(props, palette, propsRoot, placements[index], index);
        }
        props.Flush(propsRoot, "Prop");

        // The original in-drome view has no visible crowd, stands or jumbotron inside the
        // playing volume: the faceted shell disappears overhead. Keep the dark ceiling and
        // floodlights only.
        var ceiling = new Batch(palette);
        BuildCeiling(ceiling, radiusX, radiusZ);
        BuildFloodlights(ceiling, root, radiusX, radiusZ);
        ceiling.Flush(CreateGroup(root, "Ceiling"), "Ceiling");

        return new DromeInstance
        {
            root = root,
            goalNorthMouth = north.mouth,
            goalSouthMouth = south.mouth,
            rokSpawn = new Vector3(0f, 0.4f, 0f),
            props = placements,
            radiusX = radiusX,
            radiusZ = radiusZ
        };
    }

    /// <summary>Destroys every drome previously created under <paramref name="parent"/>.</summary>
    public static void Clear(Transform parent)
    {
        if (parent == null)
        {
            return;
        }
        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            Transform child = parent.GetChild(index);
            if (child == null || !IsDromeRoot(child.name))
            {
                continue;
            }

            var materials = new HashSet<Material>();
            var meshes = new HashSet<Mesh>();
            foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }
            foreach (MeshFilter filter in child.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter != null && filter.sharedMesh != null)
                {
                    meshes.Add(filter.sharedMesh);
                }
            }
            foreach (MeshCollider collider in child.GetComponentsInChildren<MeshCollider>(true))
            {
                if (collider != null && collider.sharedMesh != null)
                {
                    meshes.Add(collider.sharedMesh);
                }
            }

            DestroyObject(child.gameObject);

            foreach (Material material in materials)
            {
                if (material != null && material.name.StartsWith("Drome ", System.StringComparison.Ordinal))
                {
                    DestroyObject(material);
                }
            }
            foreach (Mesh mesh in meshes)
            {
                DestroyObject(mesh);
            }
        }
    }

    private static Shader FindShader()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Diffuse");
        }
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/VertexLit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        return shader;
    }

    private static Palette CreatePalette(ArenaDefinition arena, Color homeColor, Color awayColor, Shader shader)
    {
        Color home = Lift(homeColor, 0.50f);
        Color away = Lift(awayColor, 0.50f);
        Color floor = Color.Lerp(arena.floorColor, new Color(0.64f, 0.66f, 0.69f, 1f), 0.85f);
        Color crowd = new Color(0.085f, 0.09f, 0.105f, 1f);

        var palette = new Palette();
        palette[Mat.Floor] = CreateMaterial(shader, "Floor", floor, 0f);
        palette[Mat.FloorTrim] = CreateMaterial(shader, "Floor Trim", floor * 0.55f, 0f);
        palette[Mat.MarkWhite] = CreateMaterial(shader, "Mark White", new Color(0.86f, 0.87f, 0.88f, 1f), 0f);
        palette[Mat.MarkRed] = CreateMaterial(shader, "Mark Red", new Color(0.68f, 0.05f, 0.06f, 1f), 0f);
        palette[Mat.MarkBlue] = CreateMaterial(shader, "Mark Blue", new Color(0.08f, 0.18f, 0.48f, 1f), 0f);
        palette[Mat.StarRed] = CreateMaterial(shader, "Star Red", new Color(0.80f, 0.05f, 0.07f, 1f), 1.1f);
        palette[Mat.WallDark] = CreateMaterial(shader, "Wall Dark", new Color(0.52f, 0.54f, 0.58f, 1f), 0f);
        palette[Mat.WallRed] = CreateMaterial(shader, "Wall Red", new Color(0.72f, 0.06f, 0.07f, 1f), 0f);
        palette[Mat.WallWhite] = CreateMaterial(shader, "Wall White", new Color(0.78f, 0.79f, 0.80f, 1f), 0f);
        palette[Mat.TeamHome] = CreateMaterial(shader, "Team Home", home, 0f);
        palette[Mat.TeamAway] = CreateMaterial(shader, "Team Away", away, 0f);
        palette[Mat.CrowdDark] = CreateMaterial(shader, "Crowd Dark", crowd, 0f);
        palette[Mat.CrowdHome] = CreateMaterial(shader, "Crowd Home", Color.Lerp(crowd, home, 0.45f), 0f);
        palette[Mat.CrowdAway] = CreateMaterial(shader, "Crowd Away", Color.Lerp(crowd, away, 0.45f), 0f);
        palette[Mat.CrowdLight] = CreateMaterial(shader, "Crowd Light", new Color(0.40f, 0.41f, 0.44f, 1f), 0f);
        palette[Mat.Metal] = CreateMaterial(shader, "Metal", new Color(0.15f, 0.16f, 0.18f, 1f), 0f);
        palette[Mat.MetalLight] = CreateMaterial(shader, "Metal Light", new Color(0.55f, 0.56f, 0.59f, 1f), 0f);
        palette[Mat.Glow] = CreateMaterial(shader, "Glow", new Color(0.25f, 0.95f, 1.0f, 1f), 2.0f);
        palette[Mat.BeamRed] = CreateMaterial(shader, "Beam Red", new Color(1.0f, 0.12f, 0.10f, 1f), 2.4f);
        palette[Mat.FlagGreen] = CreateMaterial(shader, "Flag Green", new Color(0.15f, 0.95f, 0.35f, 1f), 1.5f);
        palette[Mat.FlagRed] = CreateMaterial(shader, "Flag Red", new Color(1.0f, 0.15f, 0.12f, 1f), 1.8f);
        palette[Mat.ScreenHome] = CreateMaterial(shader, "Screen Home", home, 0.9f);
        palette[Mat.ScreenAway] = CreateMaterial(shader, "Screen Away", away, 0.9f);
        palette[Mat.ScreenWhite] = CreateMaterial(shader, "Screen White", new Color(0.80f, 0.88f, 1.0f, 1f), 0.8f);
        palette[Mat.GoalRed] = CreateMaterial(shader, "Goal Red", new Color(0.62f, 0.05f, 0.05f, 1f), 0.18f);
        palette[Mat.GoalCase] = CreateMaterial(shader, "Goal Case", new Color(0.035f, 0.038f, 0.045f, 1f), 0f);
        palette[Mat.Net] = CreateMaterial(shader, "Net", new Color(0.42f, 0.44f, 0.47f, 1f), 0f);
        palette[Mat.Ceiling] = CreateMaterial(shader, "Ceiling", new Color(0.035f, 0.04f, 0.05f, 1f), 0f);
        return palette;
    }

    private static Material CreateMaterial(Shader shader, string label, Color colour, float emission)
    {
        if (shader == null)
        {
            return null;
        }
        var material = new Material(shader);
        material.name = "Drome " + label;
        material.color = colour;
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.04f);
        }
        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.06f);
        }
        if (emission > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", colour * emission);
        }
        return material;
    }

    // Keeps a dark team colour readable as a painted panel without changing its hue.
    private static Color Lift(Color colour, float minimum)
    {
        float maximum = Mathf.Max(colour.r, Mathf.Max(colour.g, colour.b));
        if (maximum <= 0.0001f)
        {
            return new Color(minimum, minimum, minimum, 1f);
        }
        float scale = Mathf.Max(1f, minimum / maximum);
        return new Color(
            Mathf.Clamp01(colour.r * scale),
            Mathf.Clamp01(colour.g * scale),
            Mathf.Clamp01(colour.b * scale),
            1f);
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        var group = new GameObject(name);
        Transform transform = group.transform;
        transform.SetParent(parent, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        return transform;
    }

    private static GameObject CreateMeshObject(Transform parent, string name, Mesh mesh, Material material)
    {
        var gameObject = new GameObject(name);
        Transform transform = gameObject.transform;
        transform.SetParent(parent, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return gameObject;
    }

    private static bool IsDromeRoot(string name)
    {
        return name == RootObjectName || name.StartsWith(RootObjectName + "_", System.StringComparison.Ordinal);
    }

    private static void DestroyObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }
        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(target);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static Vector3 Point(Vector3 centre, float radiusX, float radiusZ, float angleDeg, float scale, float y)
    {
        float radians = angleDeg * Deg2Rad;
        return new Vector3(
            centre.x + Mathf.Cos(radians) * radiusX * scale,
            y,
            centre.z + Mathf.Sin(radians) * radiusZ * scale);
    }

    private static Vector3 EllipseNormal(float radiusX, float radiusZ, float angleDeg)
    {
        float radians = angleDeg * Deg2Rad;
        return new Vector3(Mathf.Cos(radians) / radiusX, 0f, Mathf.Sin(radians) / radiusZ).normalized;
    }

    private static Vector2 ClampToEllipse(Vector2 point, float radiusX, float radiusZ, float limit)
    {
        float q = (point.x / radiusX) * (point.x / radiusX) + (point.y / radiusZ) * (point.y / radiusZ);
        if (q > limit * limit && q > 0.0001f)
        {
            point *= limit / Mathf.Sqrt(q);
        }
        return point;
    }

    private static GoalSpec MakeGoalSpec(Vector2 point, float radiusX, float radiusZ)
    {
        Vector2 flat = ClampToEllipse(point, radiusX, radiusZ, 0.97f);
        Vector3 outward = new Vector3(flat.x, 0f, flat.y);
        if (outward.sqrMagnitude < 0.0001f)
        {
            outward = Vector3.forward;
        }
        outward.Normalize();

        float cosTheta = flat.x / radiusX;
        float sinTheta = flat.y / radiusZ;
        float thetaRad = Mathf.Atan2(sinTheta, cosTheta);
        float thetaDeg = Mathf.Repeat(thetaRad * Rad2Deg, 360f);
        float tangent = Mathf.Sqrt(
            radiusX * radiusX * Mathf.Sin(thetaRad) * Mathf.Sin(thetaRad) +
            radiusZ * radiusZ * Mathf.Cos(thetaRad) * Mathf.Cos(thetaRad));
        float halfAngleDeg = (GoalWidth * 0.5f) / Mathf.Max(tangent, 0.01f) * Rad2Deg;

        var spec = new GoalSpec();
        spec.mouth = new Vector3(flat.x, 0f, flat.y);
        spec.outward = outward;
        spec.yaw = Mathf.Atan2(outward.x, outward.z) * Rad2Deg;
        spec.thetaDeg = thetaDeg;
        spec.halfAngleDeg = halfAngleDeg;
        return spec;
    }

    private const float WallScaleAtTop = WallTopScale;

    private static float WallScale(float y)
    {
        return Mathf.Lerp(1f, WallScaleAtTop, Mathf.Clamp01(y / WallHeight));
    }

    private static bool InGoalMouth(float midDeg, GoalSpec spec)
    {
        return Mathf.Abs(Mathf.DeltaAngle(midDeg, spec.thetaDeg)) < spec.halfAngleDeg - 0.01f;
    }

    private static float[] BuildWallAngles(GoalSpec north, GoalSpec south)
    {
        var angles = new List<float>(WallSegments + 4);
        for (int index = 0; index < WallSegments; index++)
        {
            angles.Add(index * 360f / WallSegments);
        }
        angles.Add(north.thetaDeg - north.halfAngleDeg);
        angles.Add(north.thetaDeg + north.halfAngleDeg);
        angles.Add(south.thetaDeg - south.halfAngleDeg);
        angles.Add(south.thetaDeg + south.halfAngleDeg);
        for (int index = 0; index < angles.Count; index++)
        {
            angles[index] = Mathf.Repeat(angles[index], 360f);
        }
        angles.Sort();

        var result = new List<float>(angles.Count);
        for (int index = 0; index < angles.Count; index++)
        {
            if (result.Count == 0 || angles[index] - result[result.Count - 1] > 0.01f)
            {
                result.Add(angles[index]);
            }
        }
        if (result.Count > 0 && 360f - result[result.Count - 1] < 0.01f)
        {
            result.RemoveAt(result.Count - 1);
        }
        return result.ToArray();
    }

    private static Mat PanelMaterial(int segment, float midDeg, GoalSpec north, GoalSpec south)
    {
        if (Mathf.Abs(Mathf.DeltaAngle(midDeg, north.thetaDeg)) < 13f)
        {
            return Mat.TeamHome;
        }
        if (Mathf.Abs(Mathf.DeltaAngle(midDeg, south.thetaDeg)) < 13f)
        {
            return Mat.TeamAway;
        }
        if (Mathf.Abs(Mathf.DeltaAngle(midDeg, north.thetaDeg + 90f)) < 8f)
        {
            return Mat.TeamAway;
        }
        if (Mathf.Abs(Mathf.DeltaAngle(midDeg, north.thetaDeg - 90f)) < 8f)
        {
            return Mat.TeamHome;
        }
        int pattern = segment % 6;
        if (pattern == 2)
        {
            return Mat.WallRed;
        }
        if (pattern == 5)
        {
            return Mat.WallWhite;
        }
        return Mat.WallDark;
    }

    private static Mat WallBandMaterial(int band, int segment, float midDeg, GoalSpec north, GoalSpec south)
    {
        if (band == 0)
        {
            return Mat.WallRed;
        }
        if (band == 3)
        {
            return Mat.WallWhite;
        }
        if (band >= 4)
        {
            return Mat.WallDark;
        }
        return PanelMaterial(segment, midDeg, north, south);
    }

    public const float BowlDepth = 6.0f;

    // Same surface function the game uses for skater height. The bowl is deepest on the
    // long sides and fades to flat around the two goal ends, keeping the small wall goals clear.
    public static float SurfaceY(float x, float z, float radiusX, float radiusZ)
    {
        float nx = x / Mathf.Max(0.001f, radiusX);
        float nz = z / Mathf.Max(0.001f, radiusZ);
        float r = Mathf.Sqrt(nx * nx + nz * nz);
        float endFade = Mathf.Clamp01((Mathf.Abs(nz) - 0.55f) / 0.45f);
        return BowlDepth * r * r * (1f - 0.85f * endFade);
    }

    private static void AddBowlFloor(MeshBuilder mesh, float radiusX, float radiusZ, float depth, int rings, int segments)
    {
        rings = Mathf.Max(2, rings);
        segments = Mathf.Max(8, segments);
        for (int ring = 0; ring < rings; ring++)
        {
            float r0 = ring / (float)rings;
            float r1 = (ring + 1) / (float)rings;
            for (int segment = 0; segment < segments; segment++)
            {
                float a0 = segment * TwoPi / segments;
                float a1 = (segment + 1) * TwoPi / segments;
                Vector3 p00 = BowlPoint(radiusX, radiusZ, depth, r0, a0);
                Vector3 p10 = BowlPoint(radiusX, radiusZ, depth, r1, a0);
                Vector3 p11 = BowlPoint(radiusX, radiusZ, depth, r1, a1);
                Vector3 p01 = BowlPoint(radiusX, radiusZ, depth, r0, a1);
                mesh.AddQuadFacing(p00, p10, p11, p01, Vector3.up);
            }
        }
    }

    private static Vector3 BowlPoint(float radiusX, float radiusZ, float depth, float r, float angle)
    {
        float x = Mathf.Cos(angle) * radiusX * r;
        float z = Mathf.Sin(angle) * radiusZ * r;
        return new Vector3(x, SurfaceY(x, z, radiusX, radiusZ), z);
    }

    private static void BuildMarkings(Batch batch, float radiusX, float radiusZ, GoalSpec north, GoalSpec south)
    {
        Matrix4x4 identity = Matrix4x4.identity;
        MeshBuilder trim = batch.Use(Mat.FloorTrim, identity);
        MeshBuilder white = batch.Use(Mat.MarkWhite, identity);
        MeshBuilder red = batch.Use(Mat.MarkRed, identity);
        MeshBuilder blue = batch.Use(Mat.MarkBlue, identity);
        MeshBuilder star = batch.Use(Mat.StarRed, identity);

        // Dark trim ring where the floor meets the wall.
        trim.AddArcBand(Vector3.zero, radiusX, radiusZ, 0.972f, 1f, 0f, 360f, 0.008f, FloorSegments, Vector3.up);

        // Centre face-off ellipse, outer red ring and the red six point star logo.
        white.AddArcBand(Vector3.zero, radiusX, radiusZ, 0.200f, 0.216f, 0f, 360f, 0.014f, FloorSegments, Vector3.up);
        red.AddArcBand(Vector3.zero, radiusX, radiusZ, 0.232f, 0.240f, 0f, 360f, 0.012f, FloorSegments, Vector3.up);
        star.AddStar(Vector3.zero, 6, radiusX * 0.12f, radiusX * 0.045f, 0.030f, 90f, Vector3.up);

        // White, red and blue painted arcs curving to the two long ends, plus face-off dots.
        for (int end = 0; end < 2; end++)
        {
            float centreDeg = end == 0 ? north.thetaDeg : south.thetaDeg;
            white.AddArcBand(Vector3.zero, radiusX, radiusZ, 0.500f, 0.518f, centreDeg - 58f, centreDeg + 58f, 0.016f, MarkArcSegments, Vector3.up);
            red.AddArcBand(Vector3.zero, radiusX, radiusZ, 0.660f, 0.674f, centreDeg - 46f, centreDeg + 46f, 0.018f, MarkArcSegments, Vector3.up);
            blue.AddArcBand(Vector3.zero, radiusX, radiusZ, 0.340f, 0.354f, centreDeg - 32f, centreDeg + 32f, 0.018f, MarkArcSegments, Vector3.up);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 dot = Point(Vector3.zero, radiusX, radiusZ, centreDeg + side * 90f, 0.52f, 0.010f);
                red.AddEllipseDisc(dot, 0.85f, 0.85f, 1, 20, 0.010f, Vector3.up);
            }
        }

        // Goal lines across the rink at each goal mouth.
        for (int end = 0; end < 2; end++)
        {
            GoalSpec spec = end == 0 ? north : south;
            float z = spec.mouth.z;
            float ratio = Mathf.Clamp(z / radiusZ, -0.999f, 0.999f);
            float halfWidth = radiusX * Mathf.Sqrt(1f - ratio * ratio) * 0.985f;
            red.AddQuadFacing(
                new Vector3(-halfWidth, 0.020f, z - 0.07f),
                new Vector3(halfWidth, 0.020f, z - 0.07f),
                new Vector3(halfWidth, 0.020f, z + 0.07f),
                new Vector3(-halfWidth, 0.020f, z + 0.07f),
                Vector3.up);
        }
    }

    private static void BuildWall(Batch batch, MeshBuilder collider, float radiusX, float radiusZ, GoalSpec north, GoalSpec south, float[] angles)
    {
        float[] bands = { 0f, 0.55f, GoalHeight, WallHeight * 0.22f, WallHeight * 0.26f, WallHeight };
        Matrix4x4 identity = Matrix4x4.identity;

        for (int index = 0; index < angles.Length; index++)
        {
            float startDeg = angles[index];
            float endDeg = index + 1 < angles.Length ? angles[index + 1] : angles[0] + 360f;
            float midDeg = (startDeg + endDeg) * 0.5f;
            bool mouth = InGoalMouth(midDeg, north) || InGoalMouth(midDeg, south);
            Vector3 outward = EllipseNormal(radiusX, radiusZ, midDeg);

            for (int band = 0; band < bands.Length - 1; band++)
            {
                float y0 = bands[band];
                float y1 = bands[band + 1];
                if (mouth && y0 < GoalHeight - 0.001f)
                {
                    continue;
                }

                Mat bandMaterial = WallBandMaterial(band, index, midDeg, north, south);
                float scale0 = WallScale(y0);
                float scale1 = WallScale(y1);

                Vector3 inner00 = Point(Vector3.zero, radiusX, radiusZ, startDeg, scale0, y0);
                Vector3 inner10 = Point(Vector3.zero, radiusX, radiusZ, endDeg, scale0, y0);
                Vector3 inner11 = Point(Vector3.zero, radiusX, radiusZ, endDeg, scale1, y1);
                Vector3 inner01 = Point(Vector3.zero, radiusX, radiusZ, startDeg, scale1, y1);

                MeshBuilder inner = batch.Use(bandMaterial, identity);
                inner.AddQuadFacing(inner00, inner10, inner11, inner01, -outward);
                collider.AddQuadFacing(inner00, inner10, inner11, inner01, -outward);

                Vector3 outer00 = Point(Vector3.zero, radiusX + WallThickness, radiusZ + WallThickness, startDeg, scale0, y0);
                Vector3 outer10 = Point(Vector3.zero, radiusX + WallThickness, radiusZ + WallThickness, endDeg, scale0, y0);
                Vector3 outer11 = Point(Vector3.zero, radiusX + WallThickness, radiusZ + WallThickness, endDeg, scale1, y1);
                Vector3 outer01 = Point(Vector3.zero, radiusX + WallThickness, radiusZ + WallThickness, startDeg, scale1, y1);

                MeshBuilder outer = batch.Use(bandMaterial, identity);
                outer.AddQuadFacing(outer00, outer10, outer11, outer01, outward);

                if (band == bands.Length - 2)
                {
                    MeshBuilder cap = batch.Use(Mat.Metal, identity);
                    cap.AddQuadFacing(inner01, inner11, outer11, outer01, Vector3.up);
                }
            }
        }

        BuildGoalJambs(batch, radiusX, radiusZ, north);
        BuildGoalJambs(batch, radiusX, radiusZ, south);
    }

    private static void BuildGoalJambs(Batch batch, float radiusX, float radiusZ, GoalSpec spec)
    {
        MeshBuilder jamb = batch.Use(Mat.WallDark, Matrix4x4.identity);
        for (int side = -1; side <= 1; side += 2)
        {
            float angle = spec.thetaDeg + side * spec.halfAngleDeg;
            float scale0 = WallScale(0f);
            float scale1 = WallScale(GoalHeight);
            Vector3 innerBottom = Point(Vector3.zero, radiusX, radiusZ, angle, scale0, 0f);
            Vector3 innerTop = Point(Vector3.zero, radiusX, radiusZ, angle, scale1, GoalHeight);
            Vector3 outerBottom = Point(Vector3.zero, radiusX + WallThickness, radiusZ + WallThickness, angle, scale0, 0f);
            Vector3 outerTop = Point(Vector3.zero, radiusX + WallThickness, radiusZ + WallThickness, angle, scale1, GoalHeight);
            jamb.AddQuad(innerBottom, outerBottom, outerTop, innerTop);
            jamb.AddQuad(innerTop, outerTop, outerBottom, innerBottom);
        }
    }

    private static void BuildGoal(Batch batch, Batch markings, MeshBuilder collider, GoalSpec spec, int index)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(spec.mouth, Quaternion.Euler(0f, spec.yaw, 0f), Vector3.one);
        float halfWidth = GoalWidth * 0.5f;
        float postX = halfWidth + 0.09f;

        MeshBuilder frame = batch.Use(Mat.GoalRed, matrix);
        frame.AddBox(new Vector3(-postX, GoalHeight * 0.5f, 0f), new Vector3(0.18f, GoalHeight, 0.22f));
        frame.AddBox(new Vector3(postX, GoalHeight * 0.5f, 0f), new Vector3(0.18f, GoalHeight, 0.22f));
        frame.AddBox(new Vector3(0f, GoalHeight + 0.09f, 0f), new Vector3(GoalWidth + 0.36f, 0.18f, 0.22f));
        frame.AddBox(new Vector3(-postX, 0.05f, 0.02f), new Vector3(0.34f, 0.10f, 0.42f));
        frame.AddBox(new Vector3(postX, 0.05f, 0.02f), new Vector3(0.34f, 0.10f, 0.42f));

        MeshBuilder goalCase = batch.Use(Mat.GoalCase, matrix);
        goalCase.AddBox(new Vector3(-2.63f, GoalHeight * 0.5f, GoalDepth * 0.5f), new Vector3(0.14f, GoalHeight, GoalDepth));
        goalCase.AddBox(new Vector3(2.63f, GoalHeight * 0.5f, GoalDepth * 0.5f), new Vector3(0.14f, GoalHeight, GoalDepth));
        goalCase.AddBox(new Vector3(0f, GoalHeight * 0.5f, GoalDepth), new Vector3(GoalWidth + 0.30f, GoalHeight, 0.14f));
        goalCase.AddBox(new Vector3(0f, GoalHeight - 0.06f, GoalDepth * 0.5f), new Vector3(GoalWidth + 0.30f, 0.12f, GoalDepth));
        goalCase.AddBox(new Vector3(0f, -0.044f, GoalDepth * 0.5f), new Vector3(GoalWidth + 0.30f, 0.10f, GoalDepth));

        MeshBuilder net = batch.Use(Mat.Net, matrix);
        for (int bar = 0; bar < 7; bar++)
        {
            float x = Mathf.Lerp(-2.3f, 2.3f, bar / 6f);
            net.AddBox(new Vector3(x, 1.55f, GoalDepth - 0.09f), new Vector3(0.035f, 3.0f, 0.035f));
        }
        for (int bar = 0; bar < 4; bar++)
        {
            float y = 0.35f + bar * 0.78f;
            net.AddBox(new Vector3(0f, y, GoalDepth - 0.09f), new Vector3(4.75f, 0.035f, 0.035f));
        }
        for (int bar = 0; bar < 2; bar++)
        {
            float z = 0.8f + bar * 1.1f;
            net.AddBox(new Vector3(-2.56f, 1.55f, z), new Vector3(0.035f, 3.0f, 0.035f));
            net.AddBox(new Vector3(2.56f, 1.55f, z), new Vector3(0.035f, 3.0f, 0.035f));
        }

        // Crease in front of the mouth (local -Z is toward the rink centre).
        MeshBuilder creaseFill = markings.Use(Mat.MarkBlue, matrix);
        creaseFill.AddArcDisc(Vector3.zero, 2.4f, 2.4f, 180f, 360f, 0.014f, 20, Vector3.up);
        MeshBuilder creaseLine = markings.Use(Mat.MarkRed, matrix);
        creaseLine.AddArcBand(Vector3.zero, 2.4f, 2.4f, 0.965f, 1f, 180f, 360f, 0.022f, 20, Vector3.up);
        creaseLine.AddQuadFacing(
            new Vector3(-halfWidth, 0.022f, -0.07f),
            new Vector3(halfWidth, 0.022f, -0.07f),
            new Vector3(halfWidth, 0.022f, 0.07f),
            new Vector3(-halfWidth, 0.022f, 0.07f),
            Vector3.up);

        // Simple collision shell behind the mouth so a CharacterController stays in the world.
        collider.matrix = matrix;
        float half = 2.7f;
        collider.AddQuadFacing(
            new Vector3(-half, 0f, GoalDepth),
            new Vector3(half, 0f, GoalDepth),
            new Vector3(half, GoalHeight + 0.2f, GoalDepth),
            new Vector3(-half, GoalHeight + 0.2f, GoalDepth),
            Vector3.back);
        collider.AddQuadFacing(
            new Vector3(-half, 0f, -0.1f),
            new Vector3(-half, 0f, GoalDepth),
            new Vector3(-half, GoalHeight + 0.2f, GoalDepth),
            new Vector3(-half, GoalHeight + 0.2f, -0.1f),
            Vector3.right);
        collider.AddQuadFacing(
            new Vector3(half, 0f, -0.1f),
            new Vector3(half, 0f, GoalDepth),
            new Vector3(half, GoalHeight + 0.2f, GoalDepth),
            new Vector3(half, GoalHeight + 0.2f, -0.1f),
            Vector3.left);
        collider.AddQuadFacing(
            new Vector3(-half, 0f, -0.1f),
            new Vector3(half, 0f, -0.1f),
            new Vector3(half, 0f, GoalDepth),
            new Vector3(-half, 0f, GoalDepth),
            Vector3.up);
        collider.matrix = Matrix4x4.identity;
    }

    private static DromePropPlacement[] BuildPropPlacements(ArenaDefinition arena, float radiusX, float radiusZ)
    {
        DromeProp[] source = arena.props ?? new DromeProp[0];
        var placements = new List<DromePropPlacement>(source.Length);
        for (int index = 0; index < source.Length; index++)
        {
            DromeProp prop = source[index];
            if (prop == null)
            {
                continue;
            }

            Vector3 position = new Vector3(prop.position.x, 0f, prop.position.y);
            float q = (position.x / radiusX) * (position.x / radiusX) + (position.z / radiusZ) * (position.z / radiusZ);
            if (q > PropRadiusLimit * PropRadiusLimit)
            {
                position.x *= PropRadiusLimit / Mathf.Sqrt(q);
                position.z *= PropRadiusLimit / Mathf.Sqrt(q);
            }

            float yaw = FacesRink(prop.kind) ? FacingYaw(position) : prop.yaw;
            placements.Add(new DromePropPlacement
            {
                kind = prop.kind,
                position = position,
                yaw = yaw
            });
        }
        return placements.ToArray();
    }

    private static bool FacesRink(DromePropKind kind)
    {
        return kind == DromePropKind.Ramp
            || kind == DromePropKind.Multiplier
            || kind == DromePropKind.Hurdle
            || kind == DromePropKind.LaserHurdle
            || kind == DromePropKind.LimboPole;
    }

    private static float FacingYaw(Vector3 position)
    {
        return Mathf.Atan2(-position.x, -position.z) * Rad2Deg;
    }

    private static void BuildProp(Batch batch, Palette palette, Transform propsRoot, DromePropPlacement placement, int index)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(placement.position, Quaternion.Euler(0f, placement.yaw, 0f), Vector3.one);
        switch (placement.kind)
        {
            case DromePropKind.Ramp:
                BuildRamp(batch, matrix);
                break;
            case DromePropKind.Multiplier:
                BuildMultiplier(batch, matrix);
                break;
            case DromePropKind.Turnstile:
                BuildTurnstile(batch, palette, propsRoot, placement, index);
                break;
            case DromePropKind.Hurdle:
                BuildHurdle(batch, matrix);
                break;
            case DromePropKind.LaserHurdle:
                BuildLaserHurdle(batch, matrix);
                break;
            case DromePropKind.LimboPole:
                BuildLimboPole(batch, matrix);
                break;
            case DromePropKind.Randomizer:
            case DromePropKind.RedRandomizer:
                BuildRandomizer(batch, matrix, placement.kind == DromePropKind.RedRandomizer);
                break;
            case DromePropKind.Armory:
                BuildArmory(batch, matrix);
                break;
            default:
                break;
        }
    }

    private static void BuildRamp(Batch batch, Matrix4x4 matrix)
    {
        MeshBuilder body = batch.Use(Mat.Metal, matrix);
        const float width = 3.6f;
        const float length = 3.2f;
        const float height = 1.45f;
        float halfWidth = width * 0.5f;
        float halfLength = length * 0.5f;

        Vector3 low0 = new Vector3(-halfWidth, 0f, -halfLength);
        Vector3 low1 = new Vector3(halfWidth, 0f, -halfLength);
        Vector3 high1 = new Vector3(halfWidth, height, halfLength);
        Vector3 high0 = new Vector3(-halfWidth, height, halfLength);

        body.AddQuadFacing(low0, low1, high1, high0, Vector3.up);
        body.AddQuadFacing(
            new Vector3(-halfWidth, 0f, halfLength),
            new Vector3(halfWidth, 0f, halfLength),
            high1,
            high0,
            Vector3.forward);
        body.AddTriangleFacing(
            new Vector3(-halfWidth, 0f, -halfLength),
            new Vector3(-halfWidth, 0f, halfLength),
            high0,
            Vector3.left);
        body.AddTriangleFacing(
            new Vector3(halfWidth, 0f, -halfLength),
            new Vector3(halfWidth, 0f, halfLength),
            high1,
            Vector3.right);

        MeshBuilder trim = batch.Use(Mat.GoalRed, matrix);
        trim.AddBox(new Vector3(0f, height + 0.02f, halfLength - 0.06f), new Vector3(width + 0.08f, 0.12f, 0.22f));

        Vector3 slopeNormal = new Vector3(0f, length, -height).normalized;
        float stripeNear = -halfLength * 0.35f;
        float stripeFar = halfLength * 0.35f;

        Vector3 SlopePoint(float x, float z)
        {
            float y = height * (z + halfLength) / length;
            return new Vector3(x, y, z) + slopeNormal * 0.02f;
        }

        trim.AddQuadFacing(
            SlopePoint(-1.35f, stripeNear),
            SlopePoint(-0.55f, stripeNear),
            SlopePoint(-0.55f, stripeFar),
            SlopePoint(-1.35f, stripeFar),
            slopeNormal);
        trim.AddQuadFacing(
            SlopePoint(0.55f, stripeNear),
            SlopePoint(1.35f, stripeNear),
            SlopePoint(1.35f, stripeFar),
            SlopePoint(0.55f, stripeFar),
            slopeNormal);
    }

    private static void BuildMultiplier(Batch batch, Matrix4x4 matrix)
    {
        MeshBuilder metal = batch.Use(Mat.Metal, matrix);
        metal.AddBox(new Vector3(-1.7f, 1.55f, 0f), new Vector3(0.26f, 3.1f, 0.34f));
        metal.AddBox(new Vector3(1.7f, 1.55f, 0f), new Vector3(0.26f, 3.1f, 0.34f));
        metal.AddBox(new Vector3(0f, 3.22f, 0f), new Vector3(3.9f, 0.30f, 0.34f));
        metal.AddBox(new Vector3(-1.7f, 0.08f, 0f), new Vector3(0.80f, 0.16f, 0.80f));
        metal.AddBox(new Vector3(1.7f, 0.08f, 0f), new Vector3(0.80f, 0.16f, 0.80f));

        MeshBuilder glow = batch.Use(Mat.Glow, matrix);
        glow.AddTorus(new Vector3(0f, 1.65f, 0f), Quaternion.identity, 1.42f, 0.11f, 20, 4);

        MeshBuilder accent = batch.Use(Mat.TeamHome, matrix);
        accent.AddBox(new Vector3(0f, 3.22f, 0.19f), new Vector3(3.5f, 0.16f, 0.05f));
    }

    private static void BuildTurnstile(Batch batch, Palette palette, Transform propsRoot, DromePropPlacement placement, int index)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(placement.position, Quaternion.Euler(0f, placement.yaw, 0f), Vector3.one);
        MeshBuilder post = batch.Use(Mat.MetalLight, matrix);
        post.AddBox(new Vector3(0f, 1.15f, 0f), new Vector3(0.22f, 2.30f, 0.22f));
        post.AddBox(new Vector3(0f, 0.06f, 0f), new Vector3(1.00f, 0.12f, 1.00f));
        MeshBuilder hub = batch.Use(Mat.GoalRed, matrix);
        hub.AddBox(new Vector3(0f, 1.90f, 0f), new Vector3(0.34f, 0.34f, 0.34f));

        var spinObject = new GameObject("Turnstile_" + index);
        Transform spin = spinObject.transform;
        spin.SetParent(propsRoot, false);
        spin.localPosition = placement.position;
        spin.localRotation = Quaternion.Euler(0f, placement.yaw, 0f);

        var blades = new MeshBuilder();
        for (int arm = 0; arm < 4; arm++)
        {
            Quaternion rotation = Quaternion.Euler(0f, arm * 90f, 0f);
            blades.AddBox(rotation * new Vector3(0.80f, 0f, 0f) + new Vector3(0f, 1.30f, 0f), rotation, new Vector3(1.60f, 0.09f, 0.32f));
            blades.AddBox(rotation * new Vector3(1.62f, 0f, 0f) + new Vector3(0f, 1.30f, 0f), rotation, new Vector3(0.16f, 0.12f, 0.36f));
        }

        Mesh bladeMesh = blades.ToMesh("Turnstile Blades " + index);
        GameObject bladeObject = CreateMeshObject(spin, "Blades", bladeMesh, palette[Mat.MetalLight]);
        var spinner = bladeObject.AddComponent<DromeSpin>();
        spinner.degreesPerSecond = index % 2 == 0 ? 55f : -55f;
    }

    private static void BuildHurdle(Batch batch, Matrix4x4 matrix)
    {
        MeshBuilder red = batch.Use(Mat.GoalRed, matrix);
        MeshBuilder white = batch.Use(Mat.MarkWhite, matrix);
        for (int block = 0; block < 3; block++)
        {
            MeshBuilder material = block % 2 == 0 ? white : red;
            material.AddBox(new Vector3((block - 1) * 0.92f, 0.31f, 0f), new Vector3(0.88f, 0.62f, 0.50f));
        }
        MeshBuilder feet = batch.Use(Mat.Metal, matrix);
        feet.AddBox(new Vector3(-1.00f, 0.10f, 0f), new Vector3(0.20f, 0.20f, 0.80f));
        feet.AddBox(new Vector3(1.00f, 0.10f, 0f), new Vector3(0.20f, 0.20f, 0.80f));
    }

    private static void BuildLaserHurdle(Batch batch, Matrix4x4 matrix)
    {
        MeshBuilder metal = batch.Use(Mat.MetalLight, matrix);
        metal.AddBox(new Vector3(-1.55f, 0.95f, 0f), new Vector3(0.16f, 1.90f, 0.16f));
        metal.AddBox(new Vector3(1.55f, 0.95f, 0f), new Vector3(0.16f, 1.90f, 0.16f));
        metal.AddBox(new Vector3(-1.55f, 0.05f, 0f), new Vector3(0.44f, 0.10f, 0.44f));
        metal.AddBox(new Vector3(1.55f, 0.05f, 0f), new Vector3(0.44f, 0.10f, 0.44f));

        MeshBuilder beam = batch.Use(Mat.BeamRed, matrix);
        beam.AddBox(new Vector3(0f, 1.15f, 0f), new Vector3(2.94f, 0.05f, 0.05f));
        beam.AddBox(new Vector3(-1.48f, 1.15f, 0f), new Vector3(0.22f, 0.22f, 0.22f));
        beam.AddBox(new Vector3(1.48f, 1.15f, 0f), new Vector3(0.22f, 0.22f, 0.22f));
    }

    private static void BuildLimboPole(Batch batch, Matrix4x4 matrix)
    {
        MeshBuilder metal = batch.Use(Mat.MetalLight, matrix);
        metal.AddBox(new Vector3(-1.30f, 1.15f, 0f), new Vector3(0.14f, 2.30f, 0.14f));
        metal.AddBox(new Vector3(1.30f, 1.15f, 0f), new Vector3(0.14f, 2.30f, 0.14f));

        MeshBuilder bar = batch.Use(Mat.GoalRed, matrix);
        bar.AddBox(new Vector3(0f, 1.25f, 0f), new Vector3(2.70f, 0.12f, 0.12f));
        MeshBuilder marker = batch.Use(Mat.MarkWhite, matrix);
        marker.AddBox(new Vector3(0f, 1.25f, 0f), new Vector3(0.50f, 0.16f, 0.16f));
    }

    private static void BuildRandomizer(Batch batch, Matrix4x4 matrix, bool red)
    {
        MeshBuilder pole = batch.Use(Mat.MetalLight, matrix);
        pole.AddBox(new Vector3(0f, 1.40f, 0f), new Vector3(0.12f, 2.80f, 0.12f));
        pole.AddBox(new Vector3(0f, 0.08f, 0f), new Vector3(0.50f, 0.16f, 0.50f));

        MeshBuilder flag = batch.Use(red ? Mat.FlagRed : Mat.FlagGreen, matrix);
        flag.AddBox(new Vector3(0f, 2.42f, 0.48f), new Vector3(0.04f, 0.62f, 0.90f));
        flag.AddBox(new Vector3(0f, 2.42f, 0.02f), new Vector3(0.12f, 0.12f, 0.12f));
    }

    private static void BuildArmory(Batch batch, Matrix4x4 matrix)
    {
        MeshBuilder crate = batch.Use(Mat.MetalLight, matrix);
        crate.AddBox(new Vector3(0f, 0.50f, 0f), new Vector3(1.40f, 1.00f, 1.40f));
        crate.AddBox(new Vector3(0.55f, 1.18f, -0.28f), Quaternion.Euler(0f, 22f, 0f), new Vector3(0.90f, 0.72f, 0.90f));

        MeshBuilder band = batch.Use(Mat.GoalRed, matrix);
        band.AddBox(new Vector3(0f, 0.42f, 0f), new Vector3(1.44f, 0.14f, 1.44f));
        band.AddBox(new Vector3(0f, 0.76f, 0f), new Vector3(1.44f, 0.08f, 1.44f));

        MeshBuilder trim = batch.Use(Mat.Metal, matrix);
        trim.AddBox(new Vector3(0f, 1.02f, 0f), new Vector3(1.30f, 0.05f, 1.30f));
    }

    private static void BuildCrowd(Batch batch, float radiusX, float radiusZ)
    {
        Matrix4x4 identity = Matrix4x4.identity;
        MeshBuilder seats = batch.Use(Mat.CrowdDark, identity);
        MeshBuilder ledge = batch.Use(Mat.Metal, identity);
        MeshBuilder rail = batch.Use(Mat.GoalRed, identity);
        MeshBuilder back = batch.Use(Mat.Ceiling, identity);
        var random = new System.Random(PropSeed);

        float topTierScale = 1.02f + CrowdTiers * 0.10f;
        float topTierHeight = CrowdBaseHeight + CrowdTiers * CrowdTierRise;

        for (int tier = 0; tier < CrowdTiers; tier++)
        {
            float innerScale = 1.02f + tier * 0.10f;
            float outerScale = innerScale + 0.10f;
            float bottomY = CrowdBaseHeight + tier * CrowdTierRise;
            float topY = bottomY + CrowdTierRise;

            for (int segment = 0; segment < CrowdSegments; segment++)
            {
                float startDeg = segment * 360f / CrowdSegments;
                float endDeg = (segment + 1) * 360f / CrowdSegments;
                float midDeg = (startDeg + endDeg) * 0.5f;
                Vector3 inner0 = Point(Vector3.zero, radiusX, radiusZ, startDeg, innerScale, bottomY);
                Vector3 inner1 = Point(Vector3.zero, radiusX, radiusZ, endDeg, innerScale, bottomY);
                Vector3 outer1 = Point(Vector3.zero, radiusX, radiusZ, endDeg, outerScale, topY);
                Vector3 outer0 = Point(Vector3.zero, radiusX, radiusZ, startDeg, outerScale, topY);
                Vector3 outward = EllipseNormal(radiusX, radiusZ, midDeg);
                seats.AddQuadFacing(inner0, inner1, outer1, outer0, (-outward + Vector3.up * 1.4f).normalized);
            }

            for (int segment = 0; segment < CrowdSegments; segment++)
            {
                for (int seat = 0; seat < 2; seat++)
                {
                    float angle = segment * 360f / CrowdSegments + (seat == 0 ? -1.4f : 1.4f);
                    float scale = Mathf.Lerp(innerScale, outerScale, seat == 0 ? 0.30f : 0.72f);
                    float y = Mathf.Lerp(bottomY, topY, seat == 0 ? 0.34f : 0.42f);
                    Vector3 position = Point(Vector3.zero, radiusX, radiusZ, angle, scale, y);
                    float yaw = Mathf.Atan2(-position.x, -position.z) * Rad2Deg;
                    MeshBuilder spectator = batch.Use(
                        PickSpectator(random),
                        Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one));
                    spectator.AddBox(new Vector3(0f, 0.27f, 0f), new Vector3(0.30f, 0.54f, 0.26f));
                }
            }
        }

        for (int segment = 0; segment < CrowdTrimSegments; segment++)
        {
            float startDeg = segment * 360f / CrowdTrimSegments;
            float endDeg = (segment + 1) * 360f / CrowdTrimSegments;
            float midDeg = (startDeg + endDeg) * 0.5f;
            Vector3 outward = EllipseNormal(radiusX, radiusZ, midDeg);

            Vector3 wall0 = Point(Vector3.zero, radiusX, radiusZ, startDeg, WallTopScale, WallHeight);
            Vector3 wall1 = Point(Vector3.zero, radiusX, radiusZ, endDeg, WallTopScale, WallHeight);
            Vector3 row1 = Point(Vector3.zero, radiusX, radiusZ, endDeg, 1.02f, CrowdBaseHeight);
            Vector3 row0 = Point(Vector3.zero, radiusX, radiusZ, startDeg, 1.02f, CrowdBaseHeight);
            ledge.AddQuadFacing(wall0, wall1, row1, row0, Vector3.up);

            Vector3 rail0 = Point(Vector3.zero, radiusX, radiusZ, startDeg, 1.02f, CrowdBaseHeight);
            Vector3 rail1 = Point(Vector3.zero, radiusX, radiusZ, endDeg, 1.02f, CrowdBaseHeight);
            Vector3 railTop1 = Point(Vector3.zero, radiusX, radiusZ, endDeg, 1.02f, CrowdBaseHeight + 0.55f);
            Vector3 railTop0 = Point(Vector3.zero, radiusX, radiusZ, startDeg, 1.02f, CrowdBaseHeight + 0.55f);
            rail.AddQuadFacing(rail0, rail1, railTop1, railTop0, -outward);

            Vector3 back0 = Point(Vector3.zero, radiusX, radiusZ, startDeg, topTierScale + 0.01f, topTierHeight);
            Vector3 back1 = Point(Vector3.zero, radiusX, radiusZ, endDeg, topTierScale + 0.01f, topTierHeight);
            Vector3 backTop1 = Point(Vector3.zero, radiusX, radiusZ, endDeg, topTierScale + 0.01f, StandsTopHeight);
            Vector3 backTop0 = Point(Vector3.zero, radiusX, radiusZ, startDeg, topTierScale + 0.01f, StandsTopHeight);
            back.AddQuadFacing(back0, back1, backTop1, backTop0, -outward);
        }
    }

    private static Mat PickSpectator(System.Random random)
    {
        int roll = random.Next(100);
        if (roll < 58)
        {
            return Mat.CrowdDark;
        }
        if (roll < 72)
        {
            return Mat.CrowdLight;
        }
        if (roll < 86)
        {
            return Mat.CrowdHome;
        }
        return Mat.CrowdAway;
    }

    private static void BuildJumbotron(Batch batch, GoalSpec spec, int index)
    {
        Vector3 position = spec.mouth - spec.outward * 4.0f + Vector3.up * 11.6f;
        float yaw = Mathf.Atan2(-spec.outward.x, -spec.outward.z) * Rad2Deg;
        Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.Euler(14f, yaw, 0f), Vector3.one);

        MeshBuilder frame = batch.Use(Mat.Metal, matrix);
        frame.AddBox(Vector3.zero, new Vector3(9.80f, 4.90f, 0.70f));
        frame.AddBox(new Vector3(0f, 2.60f, 0f), new Vector3(9.90f, 0.28f, 0.80f));
        frame.AddBox(new Vector3(0f, -2.60f, 0f), new Vector3(9.90f, 0.28f, 0.80f));

        MeshBuilder home = batch.Use(Mat.ScreenHome, matrix);
        home.AddBox(new Vector3(-2.40f, 0f, 0.38f), new Vector3(4.40f, 3.90f, 0.08f));
        MeshBuilder away = batch.Use(Mat.ScreenAway, matrix);
        away.AddBox(new Vector3(2.40f, 0f, 0.38f), new Vector3(4.40f, 3.90f, 0.08f));
        MeshBuilder white = batch.Use(Mat.ScreenWhite, matrix);
        white.AddBox(new Vector3(0f, 0f, 0.38f), new Vector3(0.34f, 3.90f, 0.08f));

        MeshBuilder strut = batch.Use(Mat.Metal, Matrix4x4.identity);
        Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 bottom = position + offset * (side * 3.6f) + Vector3.up * 2.4f;
            float length = CeilingHeight - bottom.y;
            strut.AddBox(
                new Vector3(bottom.x, bottom.y + length * 0.5f, bottom.z),
                new Vector3(0.12f, length, 0.12f));
        }
    }

    private static void BuildCeiling(Batch batch, float radiusX, float radiusZ)
    {
        MeshBuilder ceiling = batch.Use(Mat.Ceiling, Matrix4x4.identity);
        for (int segment = 0; segment < CrowdTrimSegments; segment++)
        {
            float startDeg = segment * 360f / CrowdTrimSegments;
            float endDeg = (segment + 1) * 360f / CrowdTrimSegments;
            float midDeg = (startDeg + endDeg) * 0.5f;
            Vector3 lower0 = Point(Vector3.zero, radiusX, radiusZ, startDeg, 1.33f, StandsTopHeight);
            Vector3 lower1 = Point(Vector3.zero, radiusX, radiusZ, endDeg, 1.33f, StandsTopHeight);
            Vector3 upper1 = Point(Vector3.zero, radiusX, radiusZ, endDeg, CeilingScale, CeilingHeight);
            Vector3 upper0 = Point(Vector3.zero, radiusX, radiusZ, startDeg, CeilingScale, CeilingHeight);
            Vector3 outward = EllipseNormal(radiusX, radiusZ, midDeg);
            ceiling.AddQuadFacing(lower0, lower1, upper1, upper0, (-outward + Vector3.down * 0.4f).normalized);
        }
        ceiling.AddEllipseDisc(Vector3.zero, radiusX * CeilingScale, radiusZ * CeilingScale, 6, FloorSegments, CeilingHeight, Vector3.down);
    }

    private static void BuildFloodlights(Batch batch, Transform root, float radiusX, float radiusZ)
    {
        Transform group = CreateGroup(root, "Floodlights");
        for (int index = 0; index < 4; index++)
        {
            float angle = 45f + index * 90f;
            Vector3 position = Point(Vector3.zero, radiusX, radiusZ, angle, 1.12f, 15.6f);
            float yaw = Mathf.Atan2(-position.x, -position.z) * Rad2Deg;
            Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one);

            MeshBuilder metal = batch.Use(Mat.Metal, matrix);
            metal.AddBox(Vector3.zero, new Vector3(2.70f, 0.62f, 1.15f));
            metal.AddBox(new Vector3(0f, 1.85f, 0f), new Vector3(0.14f, 3.10f, 0.14f));
            metal.AddBox(new Vector3(0f, 0.36f, 0f), new Vector3(0.18f, 0.50f, 0.18f));

            MeshBuilder glow = batch.Use(Mat.ScreenWhite, matrix);
            for (int panel = -1; panel <= 1; panel++)
            {
                glow.AddBox(new Vector3(panel * 0.86f, -0.02f, 0.60f), new Vector3(0.72f, 0.44f, 0.06f));
            }

            var lightObject = new GameObject("Floodlight_" + index);
            Transform lightTransform = lightObject.transform;
            lightTransform.SetParent(group, false);
            lightTransform.localPosition = position + Vector3.down * 0.55f;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.96f, 0.90f);
            light.intensity = 1.2f;
            light.range = 36f;
            light.shadows = LightShadows.None;
        }
    }

    private enum Mat
    {
        Floor = 0,
        FloorTrim,
        MarkWhite,
        MarkRed,
        MarkBlue,
        StarRed,
        WallDark,
        WallRed,
        WallWhite,
        TeamHome,
        TeamAway,
        CrowdDark,
        CrowdHome,
        CrowdAway,
        CrowdLight,
        Metal,
        MetalLight,
        Glow,
        BeamRed,
        FlagGreen,
        FlagRed,
        ScreenHome,
        ScreenAway,
        ScreenWhite,
        GoalRed,
        GoalCase,
        Net,
        Ceiling,
        Count
    }

    private struct GoalSpec
    {
        public Vector3 mouth;
        public Vector3 outward;
        public float yaw;
        public float thetaDeg;
        public float halfAngleDeg;
    }

    private sealed class Palette
    {
        private readonly Material[] items = new Material[(int)Mat.Count];

        public Material this[Mat id]
        {
            get { return items[(int)id]; }
            set { items[(int)id] = value; }
        }
    }

    private sealed class Batch
    {
        private readonly Palette palette;
        private readonly Dictionary<int, MeshBuilder> builders = new Dictionary<int, MeshBuilder>();

        public Batch(Palette palette)
        {
            this.palette = palette;
        }

        public MeshBuilder Use(Mat id, Matrix4x4 matrix)
        {
            MeshBuilder builder;
            if (!builders.TryGetValue((int)id, out builder))
            {
                builder = new MeshBuilder();
                builders.Add((int)id, builder);
            }
            builder.matrix = matrix;
            return builder;
        }

        public void Flush(Transform parent, string prefix)
        {
            foreach (KeyValuePair<int, MeshBuilder> pair in builders)
            {
                if (pair.Value.IsEmpty)
                {
                    continue;
                }
                Material material = palette[(Mat)pair.Key];
                if (material == null)
                {
                    continue;
                }
                Mesh mesh = pair.Value.ToMesh(prefix + " " + ((Mat)pair.Key));
                CreateMeshObject(parent, prefix + "_" + ((Mat)pair.Key), mesh, material);
            }
            builders.Clear();
        }
    }

    // Flat shaded, single material geometry: every triangle gets its own vertices and face normal.
    private sealed class MeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>(512);
        private readonly List<Vector3> normals = new List<Vector3>(512);
        private readonly List<Vector2> uvs = new List<Vector2>(512);
        private readonly List<int> triangles = new List<int>(1024);

        public Matrix4x4 matrix = Matrix4x4.identity;

        public bool IsEmpty
        {
            get { return vertices.Count == 0; }
        }

        public void AddVertex(Vector3 position, Vector3 normal)
        {
            vertices.Add(matrix.MultiplyPoint3x4(position));
            normals.Add(matrix.MultiplyVector(normal));
            uvs.Add(Vector2.zero);
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-12f)
            {
                return;
            }
            normal = normal.normalized;
            int start = vertices.Count;
            AddVertex(a, normal);
            AddVertex(b, normal);
            AddVertex(c, normal);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        public void AddTriangleFacing(Vector3 a, Vector3 b, Vector3 c, Vector3 prefer)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), prefer) < 0f)
            {
                AddTriangle(a, c, b);
            }
            else
            {
                AddTriangle(a, b, c);
            }
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTriangle(a, b, c);
            AddTriangle(a, c, d);
        }

        public void AddQuadFacing(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 prefer)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), prefer) < 0f)
            {
                AddQuad(d, c, b, a);
            }
            else
            {
                AddQuad(a, b, c, d);
            }
        }

        public void AddBox(Vector3 centre, Vector3 size)
        {
            AddBox(centre, Quaternion.identity, size);
        }

        public void AddBox(Vector3 centre, Quaternion rotation, Vector3 size)
        {
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            float halfZ = size.z * 0.5f;

            Vector3 p0 = centre + rotation * new Vector3(-halfX, -halfY, -halfZ);
            Vector3 p1 = centre + rotation * new Vector3(halfX, -halfY, -halfZ);
            Vector3 p2 = centre + rotation * new Vector3(halfX, -halfY, halfZ);
            Vector3 p3 = centre + rotation * new Vector3(-halfX, -halfY, halfZ);
            Vector3 p4 = centre + rotation * new Vector3(-halfX, halfY, -halfZ);
            Vector3 p5 = centre + rotation * new Vector3(halfX, halfY, -halfZ);
            Vector3 p6 = centre + rotation * new Vector3(halfX, halfY, halfZ);
            Vector3 p7 = centre + rotation * new Vector3(-halfX, halfY, halfZ);

            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;

            AddQuadFacing(p0, p1, p2, p3, -up);
            AddQuadFacing(p4, p5, p6, p7, up);
            AddQuadFacing(p0, p1, p5, p4, -forward);
            AddQuadFacing(p3, p2, p6, p7, forward);
            AddQuadFacing(p0, p3, p7, p4, -right);
            AddQuadFacing(p1, p2, p6, p5, right);
        }

        public void AddTorus(Vector3 centre, Quaternion rotation, float majorRadius, float minorRadius, int majorSegments, int minorSegments)
        {
            int major = Mathf.Max(3, majorSegments);
            int minor = Mathf.Max(3, minorSegments);
            for (int index = 0; index < major; index++)
            {
                float u0 = index * TwoPi / major;
                float u1 = (index + 1) * TwoPi / major;
                for (int ring = 0; ring < minor; ring++)
                {
                    float v0 = ring * TwoPi / minor;
                    float v1 = (ring + 1) * TwoPi / minor;
                    Vector3 a = TorusPoint(centre, rotation, majorRadius, minorRadius, u0, v0);
                    Vector3 b = TorusPoint(centre, rotation, majorRadius, minorRadius, u1, v0);
                    Vector3 c = TorusPoint(centre, rotation, majorRadius, minorRadius, u1, v1);
                    Vector3 d = TorusPoint(centre, rotation, majorRadius, minorRadius, u0, v1);

                    float uc = (u0 + u1) * 0.5f;
                    float vc = (v0 + v1) * 0.5f;
                    Vector3 prefer = rotation * new Vector3(
                        Mathf.Cos(uc) * Mathf.Cos(vc),
                        Mathf.Sin(uc) * Mathf.Cos(vc),
                        Mathf.Sin(vc));
                    AddQuadFacing(a, b, c, d, prefer);
                }
            }
        }

        private static Vector3 TorusPoint(Vector3 centre, Quaternion rotation, float majorRadius, float minorRadius, float u, float v)
        {
            float ring = majorRadius + minorRadius * Mathf.Cos(v);
            return centre + rotation * new Vector3(ring * Mathf.Cos(u), ring * Mathf.Sin(u), minorRadius * Mathf.Sin(v));
        }

        public void AddEllipseDisc(Vector3 centre, float radiusX, float radiusZ, int rings, int segments, float y, Vector3 prefer)
        {
            int ringCount = Mathf.Max(1, rings);
            int segmentCount = Mathf.Max(3, segments);
            Vector3 hub = new Vector3(centre.x, y, centre.z);
            for (int segment = 0; segment < segmentCount; segment++)
            {
                float a0 = segment * TwoPi / segmentCount;
                float a1 = (segment + 1) * TwoPi / segmentCount;
                Vector3 p0 = new Vector3(
                    centre.x + Mathf.Cos(a0) * radiusX / ringCount,
                    y,
                    centre.z + Mathf.Sin(a0) * radiusZ / ringCount);
                Vector3 p1 = new Vector3(
                    centre.x + Mathf.Cos(a1) * radiusX / ringCount,
                    y,
                    centre.z + Mathf.Sin(a1) * radiusZ / ringCount);
                AddTriangleFacing(hub, p0, p1, prefer);
            }
            for (int ring = 1; ring < ringCount; ring++)
            {
                float scale0 = (float)ring / ringCount;
                float scale1 = (float)(ring + 1) / ringCount;
                for (int segment = 0; segment < segmentCount; segment++)
                {
                    float a0 = segment * TwoPi / segmentCount;
                    float a1 = (segment + 1) * TwoPi / segmentCount;
                    Vector3 p00 = new Vector3(
                        centre.x + Mathf.Cos(a0) * radiusX * scale0,
                        y,
                        centre.z + Mathf.Sin(a0) * radiusZ * scale0);
                    Vector3 p10 = new Vector3(
                        centre.x + Mathf.Cos(a1) * radiusX * scale0,
                        y,
                        centre.z + Mathf.Sin(a1) * radiusZ * scale0);
                    Vector3 p11 = new Vector3(
                        centre.x + Mathf.Cos(a1) * radiusX * scale1,
                        y,
                        centre.z + Mathf.Sin(a1) * radiusZ * scale1);
                    Vector3 p01 = new Vector3(
                        centre.x + Mathf.Cos(a0) * radiusX * scale1,
                        y,
                        centre.z + Mathf.Sin(a0) * radiusZ * scale1);
                    AddQuadFacing(p00, p10, p11, p01, prefer);
                }
            }
        }

        public void AddArcBand(Vector3 centre, float radiusX, float radiusZ, float innerScale, float outerScale, float startDeg, float endDeg, float y, int segments, Vector3 prefer)
        {
            int segmentCount = Mathf.Max(1, segments);
            for (int segment = 0; segment < segmentCount; segment++)
            {
                float a0 = Mathf.Lerp(startDeg, endDeg, (float)segment / segmentCount);
                float a1 = Mathf.Lerp(startDeg, endDeg, (float)(segment + 1) / segmentCount);
                Vector3 inner0 = Point(centre, radiusX, radiusZ, a0, innerScale, y);
                Vector3 inner1 = Point(centre, radiusX, radiusZ, a1, innerScale, y);
                Vector3 outer1 = Point(centre, radiusX, radiusZ, a1, outerScale, y);
                Vector3 outer0 = Point(centre, radiusX, radiusZ, a0, outerScale, y);
                AddQuadFacing(inner0, inner1, outer1, outer0, prefer);
            }
        }

        public void AddArcDisc(Vector3 centre, float radiusX, float radiusZ, float startDeg, float endDeg, float y, int segments, Vector3 prefer)
        {
            int segmentCount = Mathf.Max(1, segments);
            Vector3 hub = new Vector3(centre.x, y, centre.z);
            for (int segment = 0; segment < segmentCount; segment++)
            {
                float a0 = Mathf.Lerp(startDeg, endDeg, (float)segment / segmentCount);
                float a1 = Mathf.Lerp(startDeg, endDeg, (float)(segment + 1) / segmentCount);
                Vector3 p0 = Point(centre, radiusX, radiusZ, a0, 1f, y);
                Vector3 p1 = Point(centre, radiusX, radiusZ, a1, 1f, y);
                AddTriangleFacing(hub, p0, p1, prefer);
            }
        }

        public void AddStar(Vector3 centre, int points, float outerRadius, float innerRadius, float y, float rotationDeg, Vector3 prefer)
        {
            int pointCount = Mathf.Max(3, points);
            var boundary = new Vector3[pointCount * 2];
            for (int index = 0; index < boundary.Length; index++)
            {
                float angle = rotationDeg + index * 180f / pointCount;
                float radius = index % 2 == 0 ? outerRadius : innerRadius;
                float radians = angle * Deg2Rad;
                boundary[index] = new Vector3(
                    centre.x + Mathf.Cos(radians) * radius,
                    y,
                    centre.z + Mathf.Sin(radians) * radius);
            }
            Vector3 hub = new Vector3(centre.x, y, centre.z);
            for (int index = 0; index < boundary.Length; index++)
            {
                AddTriangleFacing(hub, boundary[index], boundary[(index + 1) % boundary.Length], prefer);
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh();
            mesh.name = name;
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

// Spins the turnstile crosses so they read as the 1996 spinner obstacles. Visual only.
internal sealed class DromeSpin : MonoBehaviour
{
    public float degreesPerSecond = 55f;

    private void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }
}
