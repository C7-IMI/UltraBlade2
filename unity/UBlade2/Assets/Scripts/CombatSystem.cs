using System;
using UnityEngine;

// Pure duel combat timers and hit resolution. Attacks and dashes only change state
// when they succeed; presentation and movement remain with the game host.
public sealed class CombatSystem
{
    public const float AttackRange = 2.4f;
    public const float AttackCooldownSeconds = 1.1f;
    public const float StunSeconds = 1.1f;
    public const float DashCooldownSeconds = 2f;
    public const float DashWindowSeconds = 0.3f;

    public float PlayerStunRemaining;
    public float AiStunRemaining;
    public float PlayerAttackCooldown;
    public float AiAttackCooldown;

    // Seconds until the dash is available again. PlayerDashing/AiDashing expose the
    // short active window (DashWindowSeconds); this field is the recovery cooldown.
    public float PlayerDashRemaining;
    public float AiDashRemaining;

    public bool PlayerStunned;
    public bool AiStunned;
    public bool PlayerDashing;
    public bool AiDashing;

    public int PlayerHits;
    public int AiHits;

    // true = the player hit the AI, false = the AI hit the player.
    public event Action<bool> Hit;

    private float playerDashWindow;
    private float aiDashWindow;

    public void Reset()
    {
        PlayerStunRemaining = 0f;
        AiStunRemaining = 0f;
        PlayerAttackCooldown = 0f;
        AiAttackCooldown = 0f;
        PlayerDashRemaining = 0f;
        AiDashRemaining = 0f;
        PlayerStunned = false;
        AiStunned = false;
        PlayerDashing = false;
        AiDashing = false;
        PlayerHits = 0;
        AiHits = 0;
        playerDashWindow = 0f;
        aiDashWindow = 0f;
    }

    public void Update(float dt)
    {
        if (!(dt > 0f) || float.IsInfinity(dt))
        {
            return;
        }

        PlayerStunRemaining = TickTimer(PlayerStunRemaining, dt);
        AiStunRemaining = TickTimer(AiStunRemaining, dt);
        PlayerAttackCooldown = TickTimer(PlayerAttackCooldown, dt);
        AiAttackCooldown = TickTimer(AiAttackCooldown, dt);
        PlayerDashRemaining = TickTimer(PlayerDashRemaining, dt);
        AiDashRemaining = TickTimer(AiDashRemaining, dt);
        playerDashWindow = TickTimer(playerDashWindow, dt);
        aiDashWindow = TickTimer(aiDashWindow, dt);

        PlayerStunned = PlayerStunRemaining > 0f;
        AiStunned = AiStunRemaining > 0f;
        PlayerDashing = playerDashWindow > 0f;
        AiDashing = aiDashWindow > 0f;
    }

    public bool PlayerTryAttack(Vector3 playerPos, Vector3 aiPos)
    {
        if (PlayerStunned || PlayerAttackCooldown > 0f || !InRange(playerPos, aiPos))
        {
            return false;
        }

        PlayerAttackCooldown = AttackCooldownSeconds;
        AiStunRemaining = Mathf.Max(AiStunRemaining, StunSeconds);
        AiStunned = true;
        PlayerHits++;
        RaiseHit(true);
        return true;
    }

    public bool AiTryAttack(Vector3 aiPos, Vector3 playerPos)
    {
        if (AiStunned || AiAttackCooldown > 0f || !InRange(aiPos, playerPos))
        {
            return false;
        }

        AiAttackCooldown = AttackCooldownSeconds;
        PlayerStunRemaining = Mathf.Max(PlayerStunRemaining, StunSeconds);
        PlayerStunned = true;
        AiHits++;
        RaiseHit(false);
        return true;
    }

    public bool PlayerTryDash()
    {
        if (PlayerStunned || PlayerDashing || PlayerDashRemaining > 0f)
        {
            return false;
        }

        PlayerDashRemaining = DashCooldownSeconds;
        playerDashWindow = DashWindowSeconds;
        PlayerDashing = true;
        return true;
    }

    public bool AiTryDash()
    {
        if (AiStunned || AiDashing || AiDashRemaining > 0f)
        {
            return false;
        }

        AiDashRemaining = DashCooldownSeconds;
        aiDashWindow = DashWindowSeconds;
        AiDashing = true;
        return true;
    }

    public void ApplyStun(bool toPlayer, float seconds)
    {
        if (!(seconds > 0f) || float.IsInfinity(seconds))
        {
            return;
        }

        if (toPlayer)
        {
            PlayerStunRemaining = Mathf.Max(PlayerStunRemaining, seconds);
            PlayerStunned = true;
        }
        else
        {
            AiStunRemaining = Mathf.Max(AiStunRemaining, seconds);
            AiStunned = true;
        }
    }

    private void RaiseHit(bool playerHitAi)
    {
        Action<bool> handler = Hit;
        if (handler != null)
        {
            handler(playerHitAi);
        }
    }

    private static bool InRange(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        float dz = a.z - b.z;
        float square = dx * dx + dy * dy + dz * dz;
        return square <= AttackRange * AttackRange;
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
