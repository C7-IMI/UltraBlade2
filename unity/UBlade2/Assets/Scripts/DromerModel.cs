using UnityEngine;

// Procedural low-poly HyperBlade dromer: armoured skater with swept pauldrons, flared helmet,
// a dorsal fin, skates and a long thin jak blade in the left hand. Flat two-tone plates.
public sealed class DromerRig : MonoBehaviour
{
    public Transform torso;
    public Transform leftArm;
    public Transform rightArm;
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform head;
    public float phase;
    public float slashTimer;

    public void SetMotion(float speed)
    {
        var amount = Mathf.Clamp01(speed / 8f);
        phase += Time.deltaTime * (4f + amount * 6f);
        var swing = Mathf.Sin(phase) * 0.35f * amount;
        if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(swing * 68f + 8f, 0f, swing * 4f);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-swing * 68f + 8f, 0f, -swing * 4f);
        if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-swing * 48f, 0f, -8f);
        if (torso != null)
        {
            torso.localRotation = Quaternion.Euler(amount * 14f + 4f, swing * 7f, swing * 6f);
            torso.localPosition = new Vector3(0f, 1.25f + Mathf.Abs(swing) * 0.06f, 0f);
        }
        if (slashTimer > 0f)
        {
            slashTimer -= Time.deltaTime;
            if (leftArm != null)
            {
                leftArm.localRotation = Quaternion.Euler(-70f, -30f, 50f);
            }
        }
        else if (leftArm != null)
        {
            leftArm.localRotation = Quaternion.Euler(20f + swing * 30f, 0f, 12f);
        }
    }

    public void Slash()
    {
        slashTimer = 0.35f;
    }
}

public static class DromerModel
{
    private static Material Mat(Color color)
    {
        var mobile = Application.isMobilePlatform;
        var shader = mobile ? Shader.Find("Unlit/Color") : Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
        var material = new Material(shader);
        material.color = color;
        return material;
    }

    private static Transform Box(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color, Vector3 euler)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = scale;
        box.transform.localRotation = Quaternion.Euler(euler);
        var collider = box.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        box.GetComponent<Renderer>().sharedMaterial = Mat(color);
        return box.transform;
    }

    private static Transform Sphere(Transform parent, string name, Vector3 localPosition, float scale, Color color)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = localPosition;
        sphere.transform.localScale = new Vector3(scale, scale, scale);
        var collider = sphere.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        sphere.GetComponent<Renderer>().sharedMaterial = Mat(color);
        return sphere.transform;
    }

    public static DromerRig Build(Transform parent, Color primary, Color secondary, bool goalie)
    {
        var go = new GameObject(goalie ? "DromerGoalie" : "Dromer");
        go.transform.SetParent(parent, false);
        var rig = go.AddComponent<DromerRig>();
        var dark = new Color(0.10f, 0.10f, 0.12f);
        var steel = new Color(0.55f, 0.56f, 0.58f);

        rig.torso = Box(go.transform, "Torso", new Vector3(0f, 1.25f, 0f), new Vector3(0.62f, 0.72f, 0.38f), primary, Vector3.zero);
        Box(rig.torso, "Chest", new Vector3(0f, 0.18f, -0.18f), new Vector3(0.72f, 0.38f, 0.18f), secondary, Vector3.zero);
        Box(rig.torso, "BackFin", new Vector3(0f, 0.28f, 0.28f), new Vector3(0.08f, 0.5f, 0.42f), secondary, new Vector3(-18f, 0f, 0f));
        Box(go.transform, "Pelvis", new Vector3(0f, 0.8f, 0f), new Vector3(0.5f, 0.3f, 0.34f), dark, Vector3.zero);
        if (goalie)
        {
            Box(go.transform, "PadsL", new Vector3(-0.26f, 0.42f, 0f), new Vector3(0.3f, 0.75f, 0.42f), secondary, Vector3.zero);
            Box(go.transform, "PadsR", new Vector3(0.26f, 0.42f, 0f), new Vector3(0.3f, 0.75f, 0.42f), secondary, Vector3.zero);
            Box(go.transform, "ChestPad", new Vector3(0f, 1.28f, 0f), new Vector3(0.82f, 0.6f, 0.5f), secondary, Vector3.zero);
        }
        rig.head = Sphere(go.transform, "Head", new Vector3(0f, 1.95f, 0f), 0.34f, steel);
        Box(rig.head, "Helmet", new Vector3(0f, 0.22f, 0.02f), new Vector3(0.42f, 0.24f, 0.42f), primary, Vector3.zero);
        Box(rig.head, "HelmetFin", new Vector3(0f, 0.42f, 0.04f), new Vector3(0.06f, 0.3f, 0.24f), secondary, new Vector3(-12f, 0f, 0f));

        rig.leftArm = Box(go.transform, "LeftArm", new Vector3(-0.48f, 1.55f, 0f), new Vector3(0.18f, 0.7f, 0.2f), primary, new Vector3(20f, 0f, 12f));
        Box(rig.leftArm, "LeftPauldron", new Vector3(-0.14f, 0.34f, 0f), new Vector3(0.42f, 0.22f, 0.36f), secondary, new Vector3(0f, 0f, -24f));
        Box(rig.leftArm, "LeftGlove", new Vector3(0f, -0.42f, 0f), new Vector3(0.22f, 0.2f, 0.22f), dark, Vector3.zero);
        var jak = Box(rig.leftArm, "Jak", new Vector3(-0.08f, -0.72f, -0.24f), new Vector3(0.05f, 0.08f, 1.15f), steel, new Vector3(-8f, 0f, 0f));
        jak.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.75f, 0.78f, 0.8f));

        rig.rightArm = Box(go.transform, "RightArm", new Vector3(0.48f, 1.55f, 0f), new Vector3(0.18f, 0.7f, 0.2f), primary, new Vector3(-20f, 0f, -10f));
        Box(rig.rightArm, "RightPauldron", new Vector3(0.14f, 0.34f, 0f), new Vector3(0.42f, 0.22f, 0.36f), secondary, new Vector3(0f, 0f, 24f));
        Box(rig.rightArm, "RightGlove", new Vector3(0f, -0.42f, 0f), new Vector3(0.22f, 0.2f, 0.22f), dark, Vector3.zero);

        rig.leftLeg = Box(go.transform, "LeftLeg", new Vector3(-0.20f, 0.38f, 0f), new Vector3(0.20f, 0.78f, 0.24f), dark, Vector3.zero);
        rig.rightLeg = Box(go.transform, "RightLeg", new Vector3(0.20f, 0.38f, 0f), new Vector3(0.20f, 0.78f, 0.24f), dark, Vector3.zero);
        Box(rig.leftLeg, "LeftSkate", new Vector3(0f, -0.48f, 0.08f), new Vector3(0.22f, 0.08f, 0.52f), steel, Vector3.zero);
        Box(rig.rightLeg, "RightSkate", new Vector3(0f, -0.48f, 0.08f), new Vector3(0.22f, 0.08f, 0.52f), steel, Vector3.zero);
        return rig;
    }
}
