using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Window type a player must use to counter a boss attack.</summary>
public enum AttackWindowType
{
    Parry,
    Dodge
}

/// <summary>Catalogue identifier for each boss attack.</summary>
public enum BossAttackId
{
    QuickSlash,
    HeavySlam,
    SpinAttack,
    GrabAttack,
    DelayedHeavy,
    ComboString,
    UnstoppableRush,
    LastResort
}

/// <summary>Boss fight phase thresholds.</summary>
public enum BossCombatPhase
{
    Phase1 = 1,
    Phase2 = 2,
    Phase3 = 3,
    Phase4 = 4
}

/// <summary>Timing grade for a parry attempt.</summary>
public enum ParryTimingGrade
{
    Perfect,
    Good,
    Late,
    Miss
}

/// <summary>Timing grade for a dodge roll attempt.</summary>
public enum RollTimingGrade
{
    Perfect,
    Early,
    Late,
    Miss
}

/// <summary>Serializable definition of a boss attack.</summary>
[Serializable]
public struct BossAttack
{
    public string name;
    public float damage;
    public bool isParryable;
    public float telegraphDuration;
    public string attackType;
    public AnimationClip telegraph;
    public AnimationClip attack;

    public BossAttackId id;
    public int minPhase;
    public int maxPhase;
    public bool isUnblockable;
    public bool isUndodgeable;
    public bool guaranteedNoCounter;
    public int comboChainLength;
    // Fields used by BossAIController (PascalCase for catalogue initializer)
    public string      Name;
    public BossAttackId Id;
    public bool        IsParryable;
    public float       DamageMultiplier;
    public float       TelegraphSeconds;
    public float       HitboxSeconds;
    public float       RecoverySeconds;
    public int         MinPhase;
    public int         AnimatorTriggerHash;
}

/// <summary>Live adaptive difficulty snapshot consumed by combat systems.</summary>
[Serializable]
public struct DifficultySettings
{
    public float bossDamageMultiplier;
    public float bossAttackInterval;
    public float parryableRatio;
    public float playerMaxHP;
    public float bossMaxHP;
    public float playerDamageMultiplier;
    public float telegraphDuration;
    public float edgeMultiplier;

    public float bossAttackIntervalMin;
    public float bossAttackIntervalMax;
    public float playerMaxStamina;
    public float hiddenAssistMultiplier;
}

/// <summary>Combat summary persisted after each fight.</summary>
[Serializable]
public struct CombatData
{
    public float fightDuration;
    public float averageReactionTime;
    public float parrySuccessRate;
    public float dodgeSuccessRate;
    public float damageDealtTotal;
    public float damageTakenTotal;
    public int uniqueCombosUsed;
    public float finalSkillScore;
    public List<string> comboHistory;
}

/// <summary>Rolling combat analytics recalculated during the fight.</summary>
[Serializable]
public struct CombatAnalyticsSnapshot
{
    public float skillScore;
    public float aggressionIndex;
    public float patternPredictability;
    public float adaptationRate;
    public float averageReactionTime;
    public float parrySuccessRate;
    public float dodgeSuccessRate;
    public float damageRatio;
    public float comboVarietyScore;
    public float patternRepetitionScore;
    public float attackFrequency;
    public float attackTime;
    public float dodgeTime;
    public float blockTime;
    public string favoriteCombo;
}

/// <summary>Detailed result of a parry attempt.</summary>
[Serializable]
public struct ParryResolution
{
    public bool success;
    public bool shouldStaggerBoss;
    public float timingPrecisionMs;
    public float playerDamageScale;
    public float counterDamageMultiplier;
    public ParryTimingGrade grade;
}

/// <summary>Detailed result of a dodge roll attempt.</summary>
[Serializable]
public struct RollResolution
{
    public bool success;
    public float timingPrecisionMs;
    public float playerDamageScale;
    public RollTimingGrade grade;
}
