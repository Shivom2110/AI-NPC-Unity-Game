using System;

/// <summary>Window type a player must use to counter a boss attack.</summary>
public enum AttackWindowType { Parry, Dodge }

/// <summary>Catalogue identifier for each boss attack.</summary>
public enum BossAttackId
{
    LightStrike     = 0,
    Kick            = 1,
    HeavySlam       = 2,
    JumpStrike      = 3,
    SpinAttack      = 4,
    GrabAttempt     = 5,
    UnstoppableRush = 6,
}

/// <summary>
/// Immutable definition of a single boss attack.
/// All timing values are base seconds — <see cref="DifficultySettings.TelegraphScale"/>
/// is applied at runtime to shrink or expand the telegraph window based on player skill.
/// </summary>
[Serializable]
public struct BossAttack
{
    /// <summary>Catalogue identifier.</summary>
    public BossAttackId Id;

    /// <summary>Human-readable name shown in debug logs.</summary>
    public string Name;

    /// <summary>Multiplier applied to the boss's base damage value.</summary>
    public float DamageMultiplier;

    /// <summary>If true the player can neutralise this attack with a parry (Q).
    /// If false the player must dodge (double Space) instead.</summary>
    public bool IsParryable;

    /// <summary>Base seconds from when the animation fires to when the hitbox opens.</summary>
    public float TelegraphSeconds;

    /// <summary>Seconds the hitbox remains active (i.e. how long the player has to react).</summary>
    public float HitboxSeconds;

    /// <summary>Seconds the boss is vulnerable / locked in recovery after the attack.</summary>
    public float RecoverySeconds;

    /// <summary>Earliest boss phase (1–4) in which this attack may be selected.</summary>
    public int MinPhase;

    /// <summary>Animator trigger hash fired at the start of the telegraph.</summary>
    public int AnimatorTriggerHash;
}

/// <summary>
/// Live difficulty snapshot produced by <see cref="DifficultyEngine"/> and consumed by
/// <see cref="BossAIController"/>, <see cref="PlayerCombatController"/>, and
/// <see cref="FightProgressionManager"/>.
/// All values are pre-interpolated — consumers apply them directly with no further math.
/// </summary>
public struct DifficultySettings
{
    /// <summary>Multiplier on the boss's base attack damage (includes edge bonus).</summary>
    public float BossDamageMultiplier;

    /// <summary>Multiplier on damage the player deals to the boss.</summary>
    public float PlayerDamageMultiplier;

    /// <summary>Minimum seconds between boss attacks.</summary>
    public float AttackIntervalMin;

    /// <summary>Maximum seconds between boss attacks.</summary>
    public float AttackIntervalMax;

    /// <summary>Seconds the parry window stays open after the boss hitbox activates.</summary>
    public float ParryWindowSeconds;

    /// <summary>Seconds the dodge window stays open after the boss hitbox activates.</summary>
    public float DodgeWindowSeconds;

    /// <summary>Fraction of boss attacks that should be parryable (0 = all unparryable, 1 = all parryable).</summary>
    public float ParryableRatio;

    /// <summary>Multiplier on each attack's base telegraph duration (1.4 = 40% longer, 0.5 = 50% shorter).</summary>
    public float TelegraphScale;

    /// <summary>Target player max HP for this skill tier.</summary>
    public float PlayerMaxHP;

    /// <summary>Target boss max HP for this skill tier.</summary>
    public float BossMaxHP;

    /// <summary>Hidden boss-advantage factor applied on top of BossDamageMultiplier for expert players.</summary>
    public float EdgeMultiplier;
}
