using System;
using UnityEngine;

// Deterministic crystal-seeking duel AI. Movement is a world-space velocity built
// from BaseSpeed and the difficulty multiplier; reaction delay gates combat and dash
// decisions so lower difficulties commit to decisions more slowly.
public sealed class AiController
{
    public const float AttackRange = 2.4f;
    public const float DashDistance = 6f;
    public const float BaseSpeed = 5f;
    public const float AttackCooldownSeconds = 1.1f;
    public const float DashCooldownSeconds = 2f;

    private readonly float speedMultiplier;
    private readonly float reactionDelay;

    private System.Random random;
    private float stunRemaining;
    private float reactionTimer;
    private float attackCooldown;
    private float dashCooldown;

    public AiController(int difficulty)
    {
        Difficulty = Mathf.Clamp(difficulty, 1, 3);
        if (Difficulty == 1)
        {
            speedMultiplier = 1f;
            reactionDelay = 0.35f;
        }
        else if (Difficulty == 2)
        {
            speedMultiplier = 1.15f;
            reactionDelay = 0.2f;
        }
        else
        {
            speedMultiplier = 1.3f;
            reactionDelay = 0.08f;
        }
        Reset();
    }

    public int Difficulty { get; private set; }

    public void Reset()
    {
        random = new System.Random(1234 + Difficulty);
        stunRemaining = 0f;
        reactionTimer = reactionDelay;
        attackCooldown = 0f;
        dashCooldown = 0f;
    }

    public void SetStunned(float seconds)
    {
        if (!(seconds > 0f) || float.IsInfinity(seconds))
        {
            return;
        }
        stunRemaining = Mathf.Max(stunRemaining, seconds);
        reactionTimer = reactionDelay;
    }

    public void Think(float dt, Vector3 self, Vector3 player, Vector3[] crystalPositions, bool[] crystalActive,
        out Vector3 move, out bool attack, out bool dash)
    {
        move = Vector3.zero;
        attack = false;
        dash = false;

        if (!(dt > 0f) || float.IsInfinity(dt))
        {
            dt = 0f;
        }
        if (!IsFinite(self))
        {
            return;
        }

        if (stunRemaining > 0f)
        {
            stunRemaining = Mathf.Max(0f, stunRemaining - dt);
            return;
        }

        attackCooldown = Mathf.Max(0f, attackCooldown - dt);
        dashCooldown = Mathf.Max(0f, dashCooldown - dt);
        reactionTimer -= dt;

        bool hasPlayer = IsFinite(player);
        Vector3 safePlayer = hasPlayer ? player : Vector3.zero;
        float playerDistance = hasPlayer ? Vector3.Distance(self, safePlayer) : float.PositiveInfinity;

        Vector3 target;
        float targetDistance;
        bool hasTarget = false;
        int crystalIndex = FindNearestCrystal(self, crystalPositions, crystalActive);
        if (crystalIndex >= 0)
        {
            target = crystalPositions[crystalIndex];
            targetDistance = PlanarDistance(self, target);
            hasTarget = true;
        }
        else if (hasPlayer)
        {
            target = safePlayer;
            targetDistance = PlanarDistance(self, safePlayer);
            hasTarget = true;
        }
        else
        {
            target = Vector3.zero;
            targetDistance = 0f;
        }

        if (hasTarget)
        {
            Vector3 delta = target - self;
            delta.y = 0f;
            float length = delta.magnitude;
            if (length > 0.05f && !float.IsNaN(length) && !float.IsInfinity(length))
            {
                move = delta * ((BaseSpeed * speedMultiplier) / length);
            }
        }

        if (reactionTimer <= 0.0001f)
        {
            reactionTimer = reactionDelay;

            if (hasPlayer && playerDistance <= AttackRange && attackCooldown <= 0f)
            {
                attack = true;
                attackCooldown = AttackCooldownSeconds;
            }

            if (hasTarget && targetDistance > DashDistance && dashCooldown <= 0f)
            {
                dash = true;
                dashCooldown = DashCooldownSeconds;
            }
        }

        if (!IsFinite(move))
        {
            move = Vector3.zero;
        }
    }

    private int FindNearestCrystal(Vector3 self, Vector3[] positions, bool[] active)
    {
        if (positions == null || random == null)
        {
            return -1;
        }

        int count = positions.Length;
        if (active != null && active.Length < count)
        {
            count = active.Length;
        }

        int bestIndex = -1;
        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            if (active != null && !active[i])
            {
                continue;
            }
            Vector3 position = positions[i];
            if (!IsFinite(position))
            {
                continue;
            }

            float distance = PlanarDistance(self, position);
            if (bestIndex < 0 || distance < bestDistance - 0.001f)
            {
                bestIndex = i;
                bestDistance = distance;
            }
            else if (Mathf.Abs(distance - bestDistance) <= 0.001f && random.Next(2) == 0)
            {
                bestIndex = i;
                bestDistance = distance;
            }
        }
        return bestIndex;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
