using System;
using UnityEngine;

// Drome goalie: stays on its line, slides to cover the mouth, darts out for a loose
// rok inside the crease, and commits to a dive when a shot is placed wide. The
// ratings are the 1996 GoalieDefinition values (saveRight/saveLeft decide which
// side he covers, saveHigh/saveLow the height, reactionTime and underPressure the
// timing, catchingAbility whether a save is held rather than spilled).
//
// The save roll is deterministic: one System.Random stream per goalie, seeded from
// the side, one roll per shot. TrySave resolves a shot once; repeating it inside
// AttemptWindowSeconds for the same aim point returns the same answer without
// counting the shot again, while a rebound aimed at a new spot is a fresh shot.
//
// Position is a floor-plane point (y is the goal centre's y). Reset keeps the
// tally so period breaks do not wipe the match statistics. Shot power is accepted
// as a 0..1 unit, a 1..10 rating, or a raw speed in metres per second.
public sealed class GoalieSystem
{
    public const float GuardDepth = 0.55f;
    public const float DiveSeconds = 0.45f;
    public const float RecoverSeconds = 0.25f;
    public const float AttemptWindowSeconds = 0.4f;
    public const float DiveSpeed = 2.1f;
    public const float MaxLateralLimit = 2.9f;
    public const float ShotHeightBias = 1.15f;
    public const float AimTolerance = 0.6f;
    public const int RandomSeed = 1996;

    public Vector3 Position;
    public int ShotsAgainst;
    public int Saves;
    public bool IsDiving;

    // True when the last save was caught rather than spilled, so the host can hand
    // the rok to the goalie for the restart instead of letting it drop in the crease.
    public bool CaughtLastSave { get; private set; }

    public GoalieDefinition Definition { get; private set; }
    public bool PlayerTeam { get; private set; }
    public Vector3 GoalCenter { get; private set; }
    public float CreaseRadius { get; private set; }

    // true = the player team's goalie made the save. MatchStats.RecordSave uses the
    // same home/away convention.
    public event Action<bool> SaveMade;

    private readonly System.Random random = new System.Random(RandomSeed);
    private float fieldDir;
    private float guardDepth;
    private float lateralLimit;
    private float moveSpeed;
    private float reactionDelay;
    private float dartDepth;
    private float trackedX;
    private float trackedDepth;
    private float trackTimer;
    private float diveTimer;
    private float recoverTimer;
    private float diveTargetX;
    private float attemptWindow;
    private Vector3 lastShotTarget;
    private bool lastSaveResult;

    public GoalieSystem(GoalieDefinition goalie, bool playerTeam, Vector3 goalCenter, float creaseRadius)
    {
        Definition = goalie != null ? goalie : new GoalieDefinition();
        PlayerTeam = playerTeam;
        CreaseRadius = creaseRadius > 0.25f && !float.IsInfinity(creaseRadius) ? creaseRadius : 3.2f;

        moveSpeed = 3.2f + 0.6f * Definition.speed;
        reactionDelay = Mathf.Clamp(0.36f - 0.028f * Definition.reactionTime, 0.05f, 0.4f);

        Reset(goalCenter);
    }

    // Place the goalie back on its line at the given goal centre and clear the dive,
    // reaction and save-window state. ShotsAgainst/Saves are deliberately kept.
    public void Reset(Vector3 goalCenter)
    {
        GoalCenter = new Vector3(goalCenter.x, goalCenter.y, goalCenter.z);
        fieldDir = goalCenter.z > 0f ? -1f : 1f;
        guardDepth = Mathf.Clamp(CreaseRadius * 0.2f, 0.35f, 0.75f);

        float averageSave = (Definition.saveRight + Definition.saveLeft) * 0.5f;
        lateralLimit = Mathf.Min(
            Mathf.Clamp(1.2f + 0.2f * averageSave, 1.8f, MaxLateralLimit),
            Mathf.Max(0.8f, CreaseRadius * 0.9f));
        dartDepth = Mathf.Max(guardDepth, Mathf.Min(CreaseRadius * 0.8f, 2.6f));

        Position = new Vector3(goalCenter.x, goalCenter.y, goalCenter.z + fieldDir * guardDepth);
        trackedX = goalCenter.x;
        trackedDepth = guardDepth;
        trackTimer = 0f;
        diveTimer = 0f;
        recoverTimer = 0f;
        diveTargetX = goalCenter.x;
        attemptWindow = 0f;
        lastSaveResult = false;
        CaughtLastSave = false;
        IsDiving = false;
    }

    // rokInOwnHalf is the caller's rink split (the rok is on this goalie's side of
    // the centre line). A loose rok inside the crease pulls the goalie out to it;
    // a carried or flying rok in the own half makes him slide across the mouth.
    public void Update(float dt, Vector3 rokPos, RokState rokState, bool rokInOwnHalf)
    {
        if (!(dt > 0f) || float.IsInfinity(dt))
        {
            return;
        }

        dt = Mathf.Min(dt, 0.25f);
        attemptWindow = TickTimer(attemptWindow, dt);
        trackTimer = TickTimer(trackTimer, dt);
        if (recoverTimer > 0f)
        {
            recoverTimer = TickTimer(recoverTimer, dt);
        }

        if (IsDiving)
        {
            diveTimer -= dt;
            if (diveTimer <= 0f)
            {
                diveTimer = 0f;
                recoverTimer = RecoverSeconds;
                IsDiving = false;
            }
        }

        bool darting = rokState == RokState.Loose && rokInOwnHalf;
        bool guarding = rokInOwnHalf || rokState == RokState.InFlight;

        float desiredX = GoalCenter.x;
        float desiredDepth = guardDepth;

        if (darting)
        {
            desiredX = rokPos.x;
            float reach = FlatDistance(GoalCenter, rokPos);
            desiredDepth = Mathf.Clamp(reach - 0.7f, guardDepth, dartDepth);
        }
        else if (guarding)
        {
            desiredX = rokPos.x;
        }

        if (IsDiving)
        {
            trackedX = diveTargetX;
        }
        else if (trackTimer <= 0f)
        {
            trackedX = desiredX;
            trackedDepth = desiredDepth;
            trackTimer = reactionDelay;
        }

        trackedX = Mathf.Clamp(trackedX, GoalCenter.x - lateralLimit, GoalCenter.x + lateralLimit);
        trackedDepth = Mathf.Clamp(trackedDepth, guardDepth, dartDepth);

        Vector3 target = new Vector3(trackedX, GoalCenter.y, GoalCenter.z + fieldDir * trackedDepth);
        float speed = moveSpeed;
        if (IsDiving)
        {
            speed *= DiveSpeed;
        }
        else if (recoverTimer > 0f)
        {
            speed *= 0.45f;
        }

        Position = Vector3.MoveTowards(Position, target, speed * dt);
        Position = ClampToCrease(Position);
    }

    // Resolve one shot at this goal. The caller should call this once as the shot
    // arrives, before TryGoal, and then deflect the rok (rok.Drop() for a spill, or
    // rok.Reset(goalie.Position + ...) when CaughtLastSave is true). Shot power may
    // be a 0..1 unit, a 1..10 rating, or the rok's speed in metres per second.
    //
    // target may be the exact aim point or the goal centre; when an axis of target
    // sits on the goal's own axis (within AimTolerance) the rok's current offset on
    // that axis is used instead, so a caller that only knows the goal line still
    // gets corner shots and dives.
    public bool TrySave(Vector3 rokPos, Vector3 target, float shotPower)
    {
        if (attemptWindow > 0f)
        {
            float overlapX = target.x - lastShotTarget.x;
            float overlapY = target.y - lastShotTarget.y;
            if (overlapX * overlapX + overlapY * overlapY < 0.75f * 0.75f)
            {
                return lastSaveResult;
            }
        }

        attemptWindow = AttemptWindowSeconds;
        lastShotTarget = target;
        ShotsAgainst++;
        CaughtLastSave = false;

        float power = NormalizeShotPower(shotPower);
        float shotSpeed = Mathf.Lerp(RokSystem.MinShotSpeed, RokSystem.MaxShotSpeed, power);
        float approach = Mathf.Max(0.5f, Mathf.Abs(rokPos.z - GoalCenter.z));
        float travel = approach / Mathf.Max(shotSpeed, 1f);
        float timeFactor = 0.7f + 0.3f * Mathf.Clamp01(travel / 0.45f);

        float aimX = Mathf.Abs(target.x - GoalCenter.x) > AimTolerance ? target.x : rokPos.x;
        float aimY = Mathf.Abs(target.y - GoalCenter.y) > AimTolerance ? target.y : rokPos.y;

        float side = aimX - Position.x;
        float lateralGap = Mathf.Abs(side);
        float verticalGap = Mathf.Abs(aimY - ShotHeightBias);
        float lateralReach = 0.55f + 0.16f * (side >= 0f ? Definition.saveRight : Definition.saveLeft);
        float verticalReach = 0.7f + 0.14f * (aimY > 1.7f ? Definition.saveHigh : Definition.saveLow);
        float diveReach = lateralReach * 1.55f;

        // Coverage is geometric: a shot at the goalie's body is nearly always held
        // and one at the fingertips is a coin flip, before skill, power and travel
        // time scale it. Ratings then only nudge that base rather than dominating it.
        float placement = Mathf.Clamp01(1f - lateralGap / Mathf.Max(0.01f, lateralReach));
        float height = Mathf.Clamp01(1f - verticalGap / Mathf.Max(0.01f, verticalReach));
        float reaction = Mathf.Clamp01(Definition.reactionTime / 10f);
        float composure = Mathf.Clamp01(1f - Definition.underPressure * 0.05f);
        float read = Mathf.Clamp01(Definition.intelligence / 100f);
        float overall = Mathf.Clamp01(Definition.overall / 10f);
        float skill = Mathf.Clamp01(0.45f * reaction + 0.25f * composure + 0.3f * read);

        float chance;
        if (lateralGap <= lateralReach && verticalGap <= verticalReach)
        {
            float coverage = placement * (0.65f + 0.35f * height);
            chance = 0.15f + 0.8f * coverage;
        }
        else if (lateralGap <= diveReach && verticalGap <= verticalReach * 1.2f)
        {
            chance = 0.1f + 0.015f * Definition.aggression;
        }
        else
        {
            chance = 0.03f;
        }

        chance *= 0.75f + 0.25f * skill;
        chance *= 0.9f + 0.1f * overall;
        chance *= 1f - 0.22f * power;
        chance *= timeFactor;
        chance = Mathf.Clamp(chance, 0.01f, 0.95f);

        if (lateralGap > lateralReach * 0.55f || verticalGap > verticalReach * 0.8f)
        {
            IsDiving = true;
            diveTimer = DiveSeconds;
            diveTargetX = aimX;
        }

        bool saved = random.NextDouble() <= chance;
        lastSaveResult = saved;
        if (!saved)
        {
            return false;
        }

        Saves++;
        float catchChance = Mathf.Clamp01(0.15f + 0.05f * Definition.catchingAbility + 0.3f * placement);
        CaughtLastSave = random.NextDouble() <= catchChance;

        Action<bool> handler = SaveMade;
        if (handler != null)
        {
            handler(PlayerTeam);
        }
        return true;
    }

    // Keep the goalie inside the crease circle, on the field side of the goal line,
    // and within the lateral limit that the mouth can be covered from.
    private Vector3 ClampToCrease(Vector3 point)
    {
        float offsetX = point.x - GoalCenter.x;
        float offsetZ = point.z - GoalCenter.z;

        float minDepth = Mathf.Min(0.3f, CreaseRadius * 0.5f);
        float depth = Mathf.Clamp(offsetZ * fieldDir, minDepth, CreaseRadius) * fieldDir;

        float maxX = Mathf.Min(CreaseRadius, lateralLimit);
        float x = Mathf.Clamp(offsetX, -maxX, maxX);

        Vector2 bounded = new Vector2(x, depth);
        float length = bounded.magnitude;
        if (length > CreaseRadius)
        {
            bounded *= CreaseRadius / length;
        }

        return new Vector3(GoalCenter.x + bounded.x, GoalCenter.y, GoalCenter.z + bounded.y);
    }

    // Shot power: a 0..1 unit, a 1..10 rating, or a raw speed in m/s. Values above
    // 10 are read as a speed and mapped across the rok shot envelope, so a caller
    // holding rok.Velocity.magnitude gets the power the shot actually had.
    private static float NormalizeShotPower(float powerOrSpeed)
    {
        if (float.IsNaN(powerOrSpeed) || powerOrSpeed <= 0f)
        {
            return 0f;
        }
        if (powerOrSpeed > 10f)
        {
            float range = RokSystem.MaxShotSpeed - RokSystem.MinShotSpeed;
            return range > 0.01f
                ? Mathf.Clamp01((powerOrSpeed - RokSystem.MinShotSpeed) / range)
                : 1f;
        }
        return RokSystem.Normalize01(powerOrSpeed);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static float TickTimer(float remaining, float dt)
    {
        if (!(remaining > 0f) || float.IsInfinity(remaining))
        {
            return 0f;
        }
        float next = remaining - dt;
        return next > 0f ? next : 0f;
    }
}
