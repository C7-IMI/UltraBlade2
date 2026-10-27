using UnityEngine;

// Shared HyperBlade-fidelity data types. Values come from the original 1996 HyperBlade
// DATA/*.HTM team definitions and DATA/*.ENV drome layouts (see docs/HYPERBLADE-REFERENCE.md).

public sealed class PlayerDefinition
{
    public string name;
    public int weightClass = 6;
    public int stamina = 5;
    public int checking = 6;
    public int balance = 6;
    public int speed = 9;
    public int shotPower = 8;
    public int shotAccuracy = 8;
    public int passAccuracy = 8;
    public int offensiveAbility = 8;
    public int rokHandling = 7;
    public int stealing = 5;
    public int damage = 110;
}

public sealed class GoalieDefinition
{
    public string name = "Goalie";
    public int weightClass = 5;
    public int strength = 7;
    public int reactionTime = 8;
    public int balance = 7;
    public int speed = 6;
    public int passAccuracy = 8;
    public int intelligence = 60;
    public int underPressure = 5;
    public int saveRight = 7;
    public int saveLeft = 6;
    public int saveHigh = 8;
    public int saveLow = 8;
    public int shieldBlocking = 7;
    public int catchingAbility = 6;
    public int aggression = 8;
    public int overall = 8;
}

public sealed class TeamDefinition
{
    public string id;
    public string displayName;
    public string city;
    public string dromeEnv;
    public Color primary;
    public Color secondary;
    public PlayerDefinition[] skaters;
    public GoalieDefinition goalie;
}

public enum DromePropKind
{
    Ramp,
    Multiplier,
    Turnstile,
    Hurdle,
    LaserHurdle,
    LimboPole,
    Randomizer,
    RedRandomizer,
    Armory
}

public sealed class DromeProp
{
    public DromePropKind kind;
    public Vector2 position;
    public float yaw;
}

public sealed class ArenaDefinition
{
    public string env;
    public string city;
    public string displayName;
    public Color floorColor = new Color(0.32f, 0.33f, 0.35f);
    public Color rimColor = new Color(0.7f, 0.08f, 0.08f);
    public float radiusX = 18.75f;
    public float radiusZ = 25.5f;
    public Vector2 goalNorth = new Vector2(0f, 24.1f);
    public Vector2 goalSouth = new Vector2(0f, -24.1f);
    public DromeProp[] props;
}
