using System;
using UnityEngine;

// The rok is the single ball of a drome match: it is carried at hip height, thrown
// as a pass, or shot at a goal. This module owns the rok's own state only; it never
// reads the scene, never touches Time, and never creates a GameObject.
//
// Slot convention (same as TeamDefinition.skaters): 0..3 are the player team's
// skaters, 4..7 the opponent's. A loose rok reports CarrierIndex -1 and
// CarrierIsPlayerTeam false.
//
// Wiring order for one frame:
//   1. TryPickup / TrackCarrier for the skaters in scoop range;
//   2. Update(dt, floorCollision, arenaRadiusX, arenaRadiusZ);
//   3. the defending goalie's TrySave, then TryGoal(goalNorth, goalSouth, ...);
//   4. Reset(kickoffPoint) at kickoff, after a goal and at the start of a period.
// Pass and Shoot release the rok themselves; do not call Drop after them.
//
// Skill inputs (Shoot power/accuracy, Pass accuracy) are unit values 0..1. A value
// above 1 is read as a HyperBlade 1..10 rating and divided by ten, so a caller may
// pass PlayerDefinition.shotAccuracy (8) directly: see Normalize01.
public enum RokState
{
    Loose,
    Carried,
    InFlight,
    InGoal
}

public sealed class RokSystem
{
    // Rok size and flight. Gravity is arcade-high so shots arrive quickly and low
    // bounces settle in about a second, as the 1996 rok behaved.
    public const float RokRadius = 0.36f;
    public const float RestHeight = 0.36f;
    public const float Gravity = 18f;
    public const float MaxSpeed = 42f;
    public const float WallRestitution = 0.62f;
    public const float WallTangent = 0.92f;
    public const float FloorRestitution = 0.48f;
    public const float MinBounceSpeed = 1.2f;
    public const float GroundFriction = 8f;
    public const float AirDrag = 0.15f;

    // Shot and pass envelopes. A flat-out rok shot crosses the drome in under a
    // second; a pass is deliberately slower so a skater can run onto it.
    public const float MinShotSpeed = 15f;
    public const float MaxShotSpeed = 38f;
    public const float MinPassSpeed = 8f;
    public const float MaxPassSpeed = 26f;
    public const float CarryDistance = 0.95f;
    public const float CarryHeight = 0.9f;
    public const float CarryResponse = 20f;
    public const float AimHeight = 1.35f;

    // Goal mouth, in metres, from the 1996 drome goals: 5.2 m wide, 3.2 m high and
    // cut into the rim at the saved goal line (ArenaCatalog goalNorth/goalSouth).
    public const float GoalMouthHalfWidth = 2.6f;
    public const float GoalMouthHeight = 3.2f;

    public const float CatchableSpeed = 30f;
    public const float PickupLockSeconds = 0.3f;
    public const float RestSpeed = 0.45f;
    public const float MaxStepSeconds = 1f / 60f;
    public const float MaxFrameSeconds = 0.25f;
    public const int MaxSubSteps = 6;
    public const float KillHeight = -20f;
    public const int RandomSeed = 1996;

    // State. Position is the rok centre; a carried rok hovers at hip height in
    // front of its carrier and keeps its last known carrier pose until the caller
    // updates it with TrackCarrier.
    public RokState State;
    public Vector3 Position;
    public Vector3 Velocity;
    public int CarrierIndex = -1;
    public bool CarrierIsPlayerTeam;

    // PickedUp/Shot/Passed carry the skater slot (0..3 player team, 4..7 opponent,
    // -1 when the rok was not carried). GoalScored carries the mouth that was hit
    // and the scoring side (1 player, 2 AI).
    public event Action<int> PickedUp;
    public event Action<int> Shot;
    public event Action<bool, int> GoalScored;
    public event Action<int> Passed;

    private readonly System.Random random = new System.Random(RandomSeed);
    private Vector3 carrierPosition;
    private Vector3 carrierFacing = Vector3.forward;
    private float pickupLock;
    private int lockedIndex = -1;

    public RokSystem(Vector3 spawn)
    {
        Reset(spawn);
    }

    // Kickoff / post-goal reset. Clears the carrier and the goal latch but never
    // rewinds the deterministic spread stream, so repeated kickoffs do not replay
    // the same shot error.
    public void Reset(Vector3 position)
    {
        Position = new Vector3(position.x, Mathf.Max(position.y, RestHeight), position.z);
        Velocity = Vector3.zero;
        State = RokState.Loose;
        CarrierIndex = -1;
        CarrierIsPlayerTeam = false;
        carrierPosition = Position;
        carrierFacing = Vector3.forward;
        pickupLock = 0f;
        lockedIndex = -1;
    }

    public void Update(float dt, bool floorCollision, float arenaRadiusX, float arenaRadiusZ)
    {
        if (!(dt > 0f) || float.IsInfinity(dt))
        {
            return;
        }

        dt = Mathf.Min(dt, MaxFrameSeconds);
        pickupLock = TickTimer(pickupLock, dt);

        if (State == RokState.InGoal)
        {
            Velocity = Vector3.zero;
            return;
        }

        if (State == RokState.Carried)
        {
            UpdateCarried(dt, arenaRadiusX, arenaRadiusZ);
            return;
        }

        int steps = Mathf.Clamp(Mathf.CeilToInt(dt / MaxStepSeconds), 1, MaxSubSteps);
        float step = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            Step(step, floorCollision, arenaRadiusX, arenaRadiusZ);
        }
    }

    // Scoop a loose rok, or catch an in-flight one that is not travelling too fast
    // to handle (CatchableSpeed). The releaser of the last shot/pass/drop is locked
    // out for PickupLockSeconds so a player cannot re-scoop his own release.
    public bool TryPickup(int playerIndex, bool isPlayerTeam, Vector3 playerPos, float scoopRadius)
    {
        if (!IsValidSlot(playerIndex, isPlayerTeam))
        {
            return false;
        }
        if (State == RokState.Carried || State == RokState.InGoal)
        {
            return false;
        }
        if (!(scoopRadius > 0f) || float.IsInfinity(scoopRadius))
        {
            return false;
        }
        if (pickupLock > 0f && lockedIndex == playerIndex)
        {
            return false;
        }
        if (State == RokState.InFlight && HorizontalSpeed() > CatchableSpeed)
        {
            return false;
        }

        float dx = playerPos.x - Position.x;
        float dz = playerPos.z - Position.z;
        if (dx * dx + dz * dz > scoopRadius * scoopRadius)
        {
            return false;
        }
        if (Mathf.Abs(playerPos.y - Position.y) > 1.8f)
        {
            return false;
        }

        State = RokState.Carried;
        CarrierIndex = playerIndex;
        CarrierIsPlayerTeam = isPlayerTeam;
        Velocity = Vector3.zero;
        carrierPosition = playerPos;
        carrierFacing = isPlayerTeam ? Vector3.forward : Vector3.back;
        pickupLock = 0f;
        lockedIndex = -1;
        RaisePickedUp(playerIndex);
        return true;
    }

    // Optional per-frame carrier pose. Without it the carried rok keeps hovering at
    // the pose it was picked up from, so the host should call this every frame with
    // the carrier's world position and heading.
    public void TrackCarrier(Vector3 carrierPos, Vector3 facing)
    {
        if (State != RokState.Carried)
        {
            return;
        }

        carrierPosition = carrierPos;
        if (facing.sqrMagnitude > 0.0001f)
        {
            carrierFacing = facing.normalized;
        }
    }

    // Knock the rok loose. A checked or passing carrier releases it at the hover
    // point; an in-flight rok (typically after a goalie save) keeps 30% of its
    // momentum and falls where it is. Safe to call in any live state.
    public void Drop()
    {
        if (State != RokState.Carried && State != RokState.InFlight)
        {
            return;
        }

        int previous = CarrierIndex;
        if (State == RokState.InFlight)
        {
            Velocity *= 0.3f;
            ClampSpeed();
        }
        else
        {
            Velocity = Vector3.zero;
        }

        State = RokState.Loose;
        CarrierIndex = -1;
        CarrierIsPlayerTeam = false;
        if (previous >= 0)
        {
            lockedIndex = previous;
            pickupLock = PickupLockSeconds;
        }
    }

    // Release a pass toward a team mate. Accuracy 0..1 (or a 1..10 rating) sets the
    // lateral error; the pass arcs just enough to clear the floor. from is the
    // release point and re-seats the rok there, so the host may pass the carrier's
    // position even if the hover had lagged behind it.
    public void Pass(Vector3 from, Vector3 to, float accuracy)
    {
        if (State == RokState.InGoal)
        {
            return;
        }

        int passer = State == RokState.Carried ? CarrierIndex : -1;
        float acc = Normalize01(accuracy);

        Vector3 delta = to - from;
        Vector2 flatDelta = new Vector2(delta.x, delta.z);
        float distance = flatDelta.magnitude;
        Vector3 direction = new Vector3(delta.x, 0f, delta.z);
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = DefaultFacing();
        }
        direction.Normalize();

        float spreadDegrees = (1f - acc) * 12f + 0.4f;
        direction = Yaw(direction, NextSigned() * spreadDegrees);

        float speed = Mathf.Clamp(distance * 1.55f + 4f, MinPassSpeed, MaxPassSpeed);
        float lift = Mathf.Clamp(1.6f + distance * 0.1f, 1.6f, 4.5f);

        Position = new Vector3(from.x, Mathf.Max(from.y + 0.25f, RestHeight + 0.2f), from.z);
        Velocity = direction * speed + Vector3.up * lift;
        ClampSpeed();

        State = RokState.InFlight;
        CarrierIndex = -1;
        CarrierIsPlayerTeam = false;
        if (passer >= 0)
        {
            lockedIndex = passer;
            pickupLock = PickupLockSeconds;
        }
        RaisePassed(passer);
    }

    // Shoot at a goal target point. Power and accuracy are 0..1 (or 1..10 ratings);
    // power sets speed and widens the error, accuracy narrows it. The vertical
    // component is solved ballistically so the rok arrives at target.y under
    // Gravity instead of flying over the mouth. from is the release point and
    // re-seats the rok there.
    public void Shoot(Vector3 from, Vector3 target, float power, float accuracy)
    {
        if (State == RokState.InGoal)
        {
            return;
        }

        int shooter = State == RokState.Carried ? CarrierIndex : -1;
        float pow = Normalize01(power);
        float acc = Normalize01(accuracy);

        Vector3 delta = target - from;
        float distance = new Vector2(delta.x, delta.z).magnitude;
        Vector3 direction = new Vector3(delta.x, 0f, delta.z);
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = DefaultFacing();
        }
        direction.Normalize();

        float spreadDegrees = (1f - acc) * 9f + pow * 2.5f + 0.3f;
        direction = Yaw(direction, NextSigned() * spreadDegrees);

        float speed = Mathf.Lerp(MinShotSpeed, MaxShotSpeed, pow);
        float originY = Mathf.Max(from.y + 0.35f, RestHeight + 0.2f);
        float travel = distance / Mathf.Max(speed, 1f);
        float lift = (target.y - originY) / Mathf.Max(travel, 0.05f) + 0.5f * Gravity * travel;
        lift = Mathf.Clamp(lift + NextSigned() * 0.6f, -8f, 12f);

        Position = new Vector3(from.x, originY, from.z);
        Velocity = direction * speed + Vector3.up * lift;
        ClampSpeed();

        State = RokState.InFlight;
        CarrierIndex = -1;
        CarrierIsPlayerTeam = false;
        if (shooter >= 0)
        {
            lockedIndex = shooter;
            pickupLock = PickupLockSeconds;
        }
        RaiseShot(shooter);
    }

    // Goal detection: the rok has crossed a goal line inside the mouth box
    // (|x - goal.x| < 2.6, height < 3.2) while not carried. A carried rok never
    // scores, and the InGoal latch keeps one crossing to one goal until Reset.
    // Returns true only on the crossing frame; out values are false/0 otherwise.
    public bool TryGoal(Vector3 goalNorth, Vector3 goalSouth, out bool northGoal, out int scoringTeam)
    {
        northGoal = false;
        scoringTeam = 0;

        if (State == RokState.InGoal || State == RokState.Carried)
        {
            return false;
        }
        if (Position.y >= GoalMouthHeight)
        {
            return false;
        }

        bool inNorth = Position.z >= goalNorth.z &&
                       Mathf.Abs(Position.x - goalNorth.x) < GoalMouthHalfWidth;
        bool inSouth = Position.z <= goalSouth.z &&
                       Mathf.Abs(Position.x - goalSouth.x) < GoalMouthHalfWidth;
        if (!inNorth && !inSouth)
        {
            return false;
        }
        if (inNorth && inSouth)
        {
            inNorth = Mathf.Abs(Position.z - goalNorth.z) <= Mathf.Abs(Position.z - goalSouth.z);
        }

        northGoal = inNorth;
        scoringTeam = inNorth ? 1 : 2;
        State = RokState.InGoal;
        Velocity = Vector3.zero;
        CarrierIndex = -1;
        CarrierIsPlayerTeam = false;
        RaiseGoalScored(northGoal, scoringTeam);
        return true;
    }

    // The goal mouth the given side attacks: the player team attacks the north
    // goal (scoringTeam 1), the AI the south goal (scoringTeam 2). Aim height is
    // below the 3.2 m bar, over the goalie's pads.
    public Vector3 HomeTarget(bool playerTeam, Vector3 goalNorth, Vector3 goalSouth)
    {
        Vector3 goal = playerTeam ? goalNorth : goalSouth;
        return new Vector3(goal.x, AimHeight, goal.z);
    }

    // Unit skill input. Values in [0,1] are used as-is; values above 1 are treated
    // as HyperBlade 1..10 ratings and divided by ten. NaN reads as 0.
    public static float Normalize01(float valueOrRating)
    {
        if (float.IsNaN(valueOrRating) || float.IsNegativeInfinity(valueOrRating) || valueOrRating <= 0f)
        {
            return 0f;
        }
        if (float.IsPositiveInfinity(valueOrRating))
        {
            return 1f;
        }
        if (valueOrRating > 1f)
        {
            return Mathf.Clamp(valueOrRating / 10f, 0f, 1f);
        }
        return Mathf.Clamp(valueOrRating, 0f, 1f);
    }

    private void Step(float dt, bool floorCollision, float arenaRadiusX, float arenaRadiusZ)
    {
        Velocity.y -= Gravity * dt;
        Position += Velocity * dt;

        if (floorCollision && Position.y <= RestHeight)
        {
            Position.y = RestHeight;
            if (Velocity.y < 0f)
            {
                float bounce = -Velocity.y * FloorRestitution;
                Velocity.y = bounce > MinBounceSpeed ? bounce : 0f;
            }
            ApplyGroundFriction(dt);
        }
        else
        {
            ApplyAirDrag(dt);
        }

        BounceOffWall(arenaRadiusX, arenaRadiusZ);

        if (Position.y < KillHeight)
        {
            Reset(new Vector3(0f, RestHeight, 0f));
            return;
        }

        if (State == RokState.InFlight &&
            Position.y <= RestHeight + 0.02f &&
            HorizontalSpeed() < RestSpeed &&
            Mathf.Abs(Velocity.y) < RestSpeed)
        {
            Velocity = Vector3.zero;
            State = RokState.Loose;
        }

        ClampSpeed();
    }

    private void UpdateCarried(float dt, float arenaRadiusX, float arenaRadiusZ)
    {
        Vector3 target = HoverPoint();
        if (arenaRadiusX > 0.01f && arenaRadiusZ > 0.01f)
        {
            target = ClampInside(target, arenaRadiusX, arenaRadiusZ);
        }

        Vector3 previous = Position;
        float blend = 1f - Mathf.Exp(-CarryResponse * dt);
        Position = Vector3.Lerp(Position, target, blend);
        Velocity = (Position - previous) / dt;
        ClampSpeed();
    }

    private Vector3 HoverPoint()
    {
        Vector3 facing = carrierFacing;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.0001f)
        {
            facing = DefaultFacing();
        }
        else
        {
            facing.Normalize();
        }

        float y = Mathf.Max(carrierPosition.y, RestHeight + 0.3f);
        return new Vector3(
            carrierPosition.x + facing.x * CarryDistance,
            y,
            carrierPosition.z + facing.z * CarryDistance);
    }

    private Vector3 DefaultFacing()
    {
        return CarrierIsPlayerTeam ? Vector3.forward : Vector3.back;
    }

    private void ApplyGroundFriction(float dt)
    {
        float speed = HorizontalSpeed();
        if (speed <= 0f)
        {
            return;
        }

        float next = speed - GroundFriction * dt;
        if (next <= 0f)
        {
            Velocity.x = 0f;
            Velocity.z = 0f;
            return;
        }

        float scale = next / speed;
        Velocity.x *= scale;
        Velocity.z *= scale;
    }

    private void ApplyAirDrag(float dt)
    {
        float damp = Mathf.Clamp01(1f - AirDrag * dt);
        Velocity.x *= damp;
        Velocity.z *= damp;
    }

    // Elliptical drome wall: project the rok back onto the rim along the radius and
    // reflect the outward velocity about the ellipse normal, with a small tangent
    // loss so wall shots die against the boards.
    private void BounceOffWall(float arenaRadiusX, float arenaRadiusZ)
    {
        if (!(arenaRadiusX > 0.01f) || !(arenaRadiusZ > 0.01f))
        {
            return;
        }

        float ax = arenaRadiusX;
        float az = arenaRadiusZ;
        float ellipse = (Position.x * Position.x) / (ax * ax) + (Position.z * Position.z) / (az * az);
        if (ellipse <= 1f)
        {
            return;
        }

        float inverse = 1f / Mathf.Sqrt(ellipse);
        Position.x *= inverse;
        Position.z *= inverse;

        float nx = Position.x / (ax * ax);
        float nz = Position.z / (az * az);
        float normalLength = Mathf.Sqrt(nx * nx + nz * nz);
        if (normalLength <= 0.0001f)
        {
            return;
        }
        nx /= normalLength;
        nz /= normalLength;

        float outward = Velocity.x * nx + Velocity.z * nz;
        if (outward <= 0.01f)
        {
            return;
        }

        float tangentX = Velocity.x - outward * nx;
        float tangentZ = Velocity.z - outward * nz;
        float reflected = -outward * WallRestitution;
        Velocity.x = tangentX * WallTangent + reflected * nx;
        Velocity.z = tangentZ * WallTangent + reflected * nz;
    }

    private Vector3 ClampInside(Vector3 point, float arenaRadiusX, float arenaRadiusZ)
    {
        float ellipse = (point.x * point.x) / (arenaRadiusX * arenaRadiusX) +
                        (point.z * point.z) / (arenaRadiusZ * arenaRadiusZ);
        if (ellipse <= 1f)
        {
            return point;
        }

        float inverse = 1f / Mathf.Sqrt(ellipse);
        return new Vector3(point.x * inverse, point.y, point.z * inverse);
    }

    // Rotate a flat vector by degrees around +Y.
    private static Vector3 Yaw(Vector3 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector3(
            direction.x * cos - direction.z * sin,
            0f,
            direction.x * sin + direction.z * cos);
    }

    private float HorizontalSpeed()
    {
        return Mathf.Sqrt(Velocity.x * Velocity.x + Velocity.z * Velocity.z);
    }

    private void ClampSpeed()
    {
        float square = Velocity.sqrMagnitude;
        if (square > MaxSpeed * MaxSpeed)
        {
            Velocity *= MaxSpeed / Mathf.Sqrt(square);
        }
    }

    private float NextSigned()
    {
        return (float)random.NextDouble() * 2f - 1f;
    }

    private static bool IsValidSlot(int playerIndex, bool isPlayerTeam)
    {
        if (isPlayerTeam)
        {
            return playerIndex >= 0 && playerIndex <= 3;
        }
        return playerIndex >= 4 && playerIndex <= 7;
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

    private void RaisePickedUp(int playerIndex)
    {
        Action<int> handler = PickedUp;
        if (handler != null)
        {
            handler(playerIndex);
        }
    }

    private void RaiseShot(int playerIndex)
    {
        Action<int> handler = Shot;
        if (handler != null)
        {
            handler(playerIndex);
        }
    }

    private void RaisePassed(int playerIndex)
    {
        Action<int> handler = Passed;
        if (handler != null)
        {
            handler(playerIndex);
        }
    }

    private void RaiseGoalScored(bool northGoal, int scoringTeam)
    {
        Action<bool, int> handler = GoalScored;
        if (handler != null)
        {
            handler(northGoal, scoringTeam);
        }
    }
}
