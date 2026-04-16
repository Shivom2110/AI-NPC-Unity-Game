using System;

/// <summary>
/// Global combat event bus used by the adaptive combat systems.
/// </summary>
public static class CombatEventSystem
{
    public static event Action<string, bool, float> OnPlayerAttack;
    public static event Action<bool, float> OnPlayerParry;
    public static event Action<bool, float> OnPlayerDodge;
    public static event Action<float, string> OnPlayerDamaged;
    public static event Action<BossAttack, float> OnBossAttackStart;
    public static event Action<float> OnBossAttackLand;
    public static event Action<int> OnBossPhaseChange;
    public static event Action<float, float> OnSkillScoreChanged;
    public static event Action<DifficultySettings> OnDifficultyAdjusted;
    public static event Action<bool, float, float> OnFightEnd;

    public static event Action<string, bool> OnComboResolved;
    public static event Action<string, float> OnPlayerActionToken;
    public static event Action<bool> OnPlayerDefeated;
    public static event Action OnBossDefeated;

    /// <summary>Publishes a player attack result.</summary>
    public static void RaisePlayerAttack(string attackType, bool landed, float damage)
    {
        OnPlayerAttack?.Invoke(attackType, landed, damage);
        OnPlayerActionToken?.Invoke(attackType, damage);
    }

    /// <summary>Publishes a parry result.</summary>
    public static void RaisePlayerParry(bool success, float timingPrecision)
    {
        OnPlayerParry?.Invoke(success, timingPrecision);
        OnPlayerActionToken?.Invoke(success ? "parry_success" : "parry_fail", timingPrecision);
    }

    /// <summary>Publishes a dodge result.</summary>
    public static void RaisePlayerDodge(bool success, float timingPrecision)
    {
        OnPlayerDodge?.Invoke(success, timingPrecision);
        OnPlayerActionToken?.Invoke(success ? "dodge_success" : "dodge_fail", timingPrecision);
    }

    /// <summary>Publishes player damage taken.</summary>
    public static void RaisePlayerDamaged(float damage, string attackType)
    {
        OnPlayerDamaged?.Invoke(damage, attackType);
    }

    /// <summary>Publishes the start of a boss attack telegraph.</summary>
    public static void RaiseBossAttackStart(BossAttack attack, float telegraphDuration)
    {
        OnBossAttackStart?.Invoke(attack, telegraphDuration);
    }

    /// <summary>Publishes a boss attack landing.</summary>
    public static void RaiseBossAttackLand(float damage)
    {
        OnBossAttackLand?.Invoke(damage);
    }

    /// <summary>Publishes a boss phase transition.</summary>
    public static void RaiseBossPhaseChange(int newPhase)
    {
        OnBossPhaseChange?.Invoke(newPhase);
    }

    /// <summary>Publishes a skill score change.</summary>
    public static void RaiseSkillScoreChanged(float oldScore, float newScore)
    {
        OnSkillScoreChanged?.Invoke(oldScore, newScore);
    }

    /// <summary>Publishes a difficulty update.</summary>
    public static void RaiseDifficultyAdjusted(DifficultySettings newSettings)
    {
        OnDifficultyAdjusted?.Invoke(newSettings);
    }

    /// <summary>Publishes the end of a fight.</summary>
    public static void RaiseFightEnd(bool playerWon, float fightDuration, float finalSkillScore)
    {
        OnFightEnd?.Invoke(playerWon, fightDuration, finalSkillScore);
    }

    /// <summary>Publishes a combo resolution.</summary>
    public static void RaiseComboResolved(string comboSignature, bool landed)
    {
        OnComboResolved?.Invoke(comboSignature, landed);
        OnPlayerActionToken?.Invoke(comboSignature, landed ? 1f : 0f);
    }

    /// <summary>Publishes a player defeat state.</summary>
    public static void RaisePlayerDefeated(bool playerWon)
    {
        OnPlayerDefeated?.Invoke(playerWon);
    }

    /// <summary>Publishes a boss defeat notification.</summary>
    public static void RaiseBossDefeated()
    {
        OnBossDefeated?.Invoke();
    }
}
