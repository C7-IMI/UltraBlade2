using UnityEngine;

// Team AI for the rok sport: chase a loose rok, carry it to the opponent goal and shoot,
// or defend/chase the carrier when the other team has it. Difficulty scales speed/reaction.
public sealed class AiController
{
    public int Difficulty { get; private set; }
    private float reaction;
    private float attackCooldown;
    private float shootCooldown;
    private float stun;
    private float dashCooldown;
    private readonly System.Random random;

    public AiController(int difficulty)
    {
        Difficulty = Mathf.Clamp(difficulty, 1, 3);
        random = new System.Random(9137 + Difficulty * 31);
        Reset();
    }

    public void Reset()
    {
        reaction = ReactionDelay();
        attackCooldown = 0f;
        shootCooldown = 0f;
        dashCooldown = 0f;
        stun = 0f;
    }

    public void SetStunned(float seconds)
    {
        stun = Mathf.Max(stun, seconds);
    }

    private float ReactionDelay()
    {
        if (Difficulty <= 1) return 0.35f;
        if (Difficulty == 2) return 0.2f;
        return 0.08f;
    }

    public float SpeedFactor()
    {
        if (Difficulty <= 1) return 1.0f;
        if (Difficulty == 2) return 1.15f;
        return 1.3f;
    }

    // selfHasRok: this skater is the rok carrier. rokLoose: nobody carries it.
    public void Think(float dt, Vector3 self, Vector3 attackGoal, Vector3 ownGoal, Vector3 rokPosition,
        bool rokLoose, bool selfHasRok, bool opponentHasRok, Vector3 opponentCarrier,
        out Vector3 move, out bool attack, out bool shoot, out bool dash)
    {
        move = Vector3.zero;
        attack = false;
        shoot = false;
        dash = false;
        if (stun > 0f)
        {
            stun -= dt;
            return;
        }
        attackCooldown -= dt;
        shootCooldown -= dt;
        dashCooldown -= dt;
        reaction -= dt;

        Vector3 target;
        if (selfHasRok)
        {
            target = attackGoal;
            var distanceToGoal = HorizontalDistance(self, attackGoal);
            if (reaction <= 0f && distanceToGoal < 17f && shootCooldown <= 0f)
            {
                shoot = true;
                shootCooldown = 1.4f - Difficulty * 0.2f;
            }
        }
        else if (rokLoose)
        {
            target = rokPosition;
        }
        else if (opponentHasRok)
        {
            target = opponentCarrier;
            if (HorizontalDistance(self, opponentCarrier) < 2.5f && attackCooldown <= 0f && reaction <= 0f)
            {
                attack = true;
                attackCooldown = 1.1f;
            }
        }
        else
        {
            var defensePoint = Vector3.Lerp(ownGoal, rokPosition, 0.45f);
            target = defensePoint;
        }

        var direction = target - self;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.05f)
        {
            move = direction.normalized;
        }
        if (dashCooldown <= 0f && move.sqrMagnitude > 0.5f && HorizontalDistance(self, target) > 9f && random.NextDouble() < 0.01 + Difficulty * 0.004)
        {
            dash = true;
            dashCooldown = 2.2f;
        }
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var delta = a - b;
        delta.y = 0f;
        return delta.magnitude;
    }
}
