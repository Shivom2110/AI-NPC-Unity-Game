using UnityEngine;

/// <summary>
/// Converts player skill into concrete combat difficulty values.
/// </summary>
public static class DifficultyEngine
{
    /// <summary>
    /// Evaluates difficulty directly from a skill score.
    /// </summary>
    public static DifficultySettings EvaluateSkillScore(float skillScore, DifficultySettingsAsset settingsAsset)
    {
        DifficultySettingsAsset source = settingsAsset != null
            ? settingsAsset
            : DifficultySettingsAsset.CreateRuntimeDefault();

        float clampedScore = Mathf.Clamp(skillScore, 0f, 100f);
        float t = clampedScore / 100f;   // 0 = total beginner, 1 = expert

        DifficultySettings settings = new DifficultySettings
        {
            bossDamageMultiplier   = EvaluateTier(clampedScore, source.BossDamageTiers,    source.ScoreBands),
            bossAttackInterval     = EvaluateTier(clampedScore, source.AttackIntervalTiers, source.ScoreBands),
            parryableRatio         = EvaluateTier(clampedScore, source.ParryableTiers,      source.ScoreBands),
            playerMaxHP            = EvaluateTier(clampedScore, source.PlayerHpTiers,       source.ScoreBands),
            bossMaxHP              = EvaluateTier(clampedScore, source.BossHpTiers,         source.ScoreBands),
            playerDamageMultiplier = EvaluateTier(clampedScore, source.PlayerDamageTiers,   source.ScoreBands),
            telegraphDuration      = EvaluateTier(clampedScore, source.TelegraphTiers,      source.ScoreBands),
            edgeMultiplier         = EvaluateTier(clampedScore, source.EdgeTiers,           source.ScoreBands),
            playerMaxStamina       = EvaluateTier(clampedScore, source.PlayerStaminaTiers,  source.ScoreBands),
            hiddenAssistMultiplier = 1f,

            // TelegraphScale: tier values are the multiplier on each attack's base telegraph time.
            // Beginners get 2× the base time to react; experts get 0.5× (very tight windows).
            telegraphScale = EvaluateTier(clampedScore, source.TelegraphTiers, source.ScoreBands),

            // Parry / dodge window scales inversely with skill.
            // Beginners: 0.65 s parry / 0.75 s dodge. Experts: 0.25 s / 0.30 s.
            parryWindowSeconds = Mathf.Lerp(0.65f, 0.25f, t),
            dodgeWindowSeconds = Mathf.Lerp(0.75f, 0.30f, t),
        };

        settings.bossAttackIntervalMin = settings.bossAttackInterval * 0.85f;
        settings.bossAttackIntervalMax = settings.bossAttackInterval * 1.15f;
        return settings;
    }

    /// <summary>
    /// Evaluates difficulty from a persistent skill profile.
    /// </summary>
    public static DifficultySettings EvaluateProfile(PlayerSkillProfile profile, DifficultySettingsAsset settingsAsset)
    {
        if (profile == null)
            return EvaluateSkillScore(50f, settingsAsset);

        float effectiveScore = Mathf.Clamp(profile.currentSkillScore * profile.difficultyMultiplier, 0f, 100f);
        return EvaluateSkillScore(effectiveScore, settingsAsset);
    }

    /// <summary>
    /// Smoothly blends two difficulty snapshots.
    /// </summary>
    public static DifficultySettings Blend(DifficultySettings from, DifficultySettings to, float t)
    {
        float blend = Mathf.Clamp01(t);

        // Guard against zeroed structs (e.g. first-ever blend from default) so windows never collapse.
        float safeParryFrom = from.parryWindowSeconds > 0.01f ? from.parryWindowSeconds : to.parryWindowSeconds;
        float safeDodgeFrom = from.dodgeWindowSeconds > 0.01f ? from.dodgeWindowSeconds : to.dodgeWindowSeconds;
        float safeTelFrom   = from.telegraphScale     > 0.01f ? from.telegraphScale     : to.telegraphScale;

        return new DifficultySettings
        {
            bossDamageMultiplier   = Mathf.Lerp(from.bossDamageMultiplier,   to.bossDamageMultiplier,   blend),
            bossAttackInterval     = Mathf.Lerp(from.bossAttackInterval,     to.bossAttackInterval,     blend),
            parryableRatio         = Mathf.Lerp(from.parryableRatio,         to.parryableRatio,         blend),
            playerMaxHP            = Mathf.Lerp(from.playerMaxHP,            to.playerMaxHP,            blend),
            bossMaxHP              = Mathf.Lerp(from.bossMaxHP,              to.bossMaxHP,              blend),
            playerDamageMultiplier = Mathf.Lerp(from.playerDamageMultiplier, to.playerDamageMultiplier, blend),
            telegraphDuration      = Mathf.Lerp(from.telegraphDuration,      to.telegraphDuration,      blend),
            edgeMultiplier         = Mathf.Lerp(from.edgeMultiplier,         to.edgeMultiplier,         blend),
            bossAttackIntervalMin  = Mathf.Lerp(from.bossAttackIntervalMin,  to.bossAttackIntervalMin,  blend),
            bossAttackIntervalMax  = Mathf.Lerp(from.bossAttackIntervalMax,  to.bossAttackIntervalMax,  blend),
            playerMaxStamina       = Mathf.Lerp(from.playerMaxStamina,       to.playerMaxStamina,       blend),
            hiddenAssistMultiplier = Mathf.Lerp(from.hiddenAssistMultiplier, to.hiddenAssistMultiplier, blend),
            telegraphScale         = Mathf.Lerp(safeTelFrom,                 to.telegraphScale,         blend),
            parryWindowSeconds     = Mathf.Lerp(safeParryFrom,               to.parryWindowSeconds,     blend),
            dodgeWindowSeconds     = Mathf.Lerp(safeDodgeFrom,               to.dodgeWindowSeconds,     blend),
        };
    }

    private static float EvaluateTier(float score, float[] values, Vector4 scoreBands)
    {
        if (values == null || values.Length < 5)
            return 0f;

        if (score <= scoreBands.x)
            return Mathf.Lerp(values[0], values[1], Mathf.InverseLerp(0f, scoreBands.x, score));

        if (score <= scoreBands.y)
            return Mathf.Lerp(values[1], values[2], Mathf.InverseLerp(scoreBands.x, scoreBands.y, score));

        if (score <= scoreBands.z)
            return Mathf.Lerp(values[2], values[3], Mathf.InverseLerp(scoreBands.y, scoreBands.z, score));

        if (score <= scoreBands.w)
            return Mathf.Lerp(values[3], values[4], Mathf.InverseLerp(scoreBands.z, scoreBands.w, score));

        return values[4];
    }
}
