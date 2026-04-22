using UnityEngine;

/// <summary>
/// Coordinates fight pacing, live difficulty updates, hidden assists, and post-fight profile persistence.
/// </summary>
public class FightProgressionManager : MonoBehaviour
{
    public static FightProgressionManager Instance { get; private set; }

    [Header("Difficulty")]
    [SerializeField] private DifficultySettingsAsset difficultySettingsAsset;
    [SerializeField] private PlayerSkillProfile playerSkillProfile;
    [SerializeField] private float liveAdjustmentInterval = 10f;  // was 30s — much faster response
    [SerializeField] [Range(0f, 1f)] private float liveBlendFactor = 0.40f;

    [Header("Grace Period (fight start)")]
    [SerializeField] private float gracePeriodSeconds = 45f;       // first N seconds always use minimum difficulty
    [SerializeField] [Range(0f, 50f)] private float gracePeriodScoreOverride = 5f;  // skill score treated as this during grace

    [Header("Hidden Assist")]
    [SerializeField] [Range(0.1f, 0.5f)] private float strugglingHealthThreshold = 0.35f;  // was 0.3
    [SerializeField] private float strugglingDuration = 8f;        // was 20s — activates much sooner
    [SerializeField] [Range(0.5f, 1f)] private float hiddenAssistDamageMultiplier = 0.80f; // was 0.85 — stronger assist

    [Header("Heat Mode")]
    [SerializeField] private int parriesForHeatMode = 3;
    [SerializeField] private float heatModeDuration = 10f;
    [SerializeField] [Range(1f, 2f)] private float heatModeDamageMultiplier = 1.25f;

    [Header("Boss Pressure")]
    [SerializeField] [Range(0.05f, 0.5f)] private float fastBossLossThreshold = 0.2f;
    [SerializeField] private float bossPressureSampleSeconds = 8f;  // was 10s — samples boss HP more often

    [Header("Boss Death Spiral")]
    [Tooltip("When boss HP drops below this fraction the difficulty is forced to max, making the boss feel desperate and dangerous.")]
    [SerializeField] [Range(0.05f, 0.4f)] private float deathSpiralThreshold = 0.20f;
    [SerializeField] [Range(0f, 100f)] private float deathSpiralScoreFloor = 90f;  // treat player skill as at least this when boss is nearly dead

    private DifficultySettings currentSettings;
    private bool fightActive;
    private float fightStartTime = -1f;
    private float nextAdjustmentTime;
    private float lowHealthStartTime = -1f;
    private float heatModeEndTime = -1f;
    private int successfulParryStreak;
    private bool hiddenAssistActive;
    private bool deathSpiralActive;
    private float lastBossHealthPercent = 1f;
    private float lastBossHealthSampleTime;

    /// <summary>Returns the live difficulty settings currently applied to combat.</summary>
    public DifficultySettings CurrentSettings => currentSettings;

    /// <summary>Returns the runtime persistent player skill profile.</summary>
    public PlayerSkillProfile Profile => playerSkillProfile;

    /// <summary>Returns the player's current damage multiplier.</summary>
    public float CurrentPlayerDamageMultiplier => currentSettings.playerDamageMultiplier;

    /// <summary>True while the player is in Heat Mode (3 consecutive successful parries).</summary>
    public bool IsHeatModeActive => fightActive && Time.time < heatModeEndTime;

    /// <summary>True while hidden assist is reducing boss damage for a struggling player.</summary>
    public bool IsHiddenAssistActive => fightActive && hiddenAssistActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (difficultySettingsAsset == null)
            difficultySettingsAsset = DifficultySettingsAsset.CreateRuntimeDefault();

        if (playerSkillProfile == null)
            playerSkillProfile = PlayerSkillProfile.CreateRuntimeProfile();

        currentSettings = playerSkillProfile.GetDifficultyRecommendation();
    }

    private void OnEnable()
    {
        CombatEventSystem.OnPlayerParry += HandlePlayerParry;
        CombatEventSystem.OnBossDefeated += HandleBossDefeated;
        CombatEventSystem.OnPlayerDefeated += HandlePlayerDefeated;
    }

    private void OnDisable()
    {
        CombatEventSystem.OnPlayerParry -= HandlePlayerParry;
        CombatEventSystem.OnBossDefeated -= HandleBossDefeated;
        CombatEventSystem.OnPlayerDefeated -= HandlePlayerDefeated;
    }

    private void Update()
    {
        BossAIController activeBoss = BossAIController.ActiveBoss;
        bool bossInCombat = activeBoss != null && activeBoss.IsInCombat();

        if (bossInCombat && !fightActive)
            StartFight();

        if (!fightActive)
            return;

        if (Time.time >= nextAdjustmentTime)
        {
            RecalculateDifficulty();
            nextAdjustmentTime = Time.time + liveAdjustmentInterval;
        }

        UpdateHiddenAssist();
        UpdateHeatMode();
        UpdateBossPressure();
    }

    /// <summary>
    /// Starts a new adaptive fight session.
    /// </summary>
    public void StartFight()
    {
        fightActive = true;
        fightStartTime = Time.time;
        nextAdjustmentTime = Time.time + liveAdjustmentInterval;
        lowHealthStartTime = -1f;
        heatModeEndTime = -1f;
        successfulParryStreak = 0;
        hiddenAssistActive = false;
        deathSpiralActive = false;

        CombatTracker.Instance?.ResetFight();
        CombatTracker.Instance?.NotifyFightStarted();
        ComboHitSystem.Instance?.ResetFight();

        currentSettings = playerSkillProfile.GetDifficultyRecommendation();
        BroadcastSettings(1f);

        BossAIController activeBoss = BossAIController.ActiveBoss;
        if (activeBoss != null)
        {
            lastBossHealthPercent = activeBoss.GetHealthPercent();
            lastBossHealthSampleTime = Time.time;
        }
    }

    /// <summary>
    /// Ends the fight and persists the player's updated skill profile.
    /// </summary>
    public void EndFight(bool playerWon)
    {
        if (!fightActive)
            return;

        fightActive = false;

        CombatData fightData = CombatTracker.Instance != null
            ? CombatTracker.Instance.BuildCombatData()
            : default;

        fightData.fightDuration = fightStartTime < 0f ? 0f : Time.time - fightStartTime;
        if (fightData.finalSkillScore <= 0f && CombatTracker.Instance != null)
            fightData.finalSkillScore = CombatTracker.Instance.CurrentSnapshot.skillScore;

        playerSkillProfile.UpdateProfile(fightData);
        playerSkillProfile.SaveProfile();

        CombatEventSystem.RaiseFightEnd(playerWon, fightData.fightDuration, fightData.finalSkillScore);
    }

    private void RecalculateDifficulty()
    {
        float rawScore = CombatTracker.Instance != null && CombatTracker.Instance.CurrentSnapshot.skillScore > 0f
            ? CombatTracker.Instance.CurrentSnapshot.skillScore
            : playerSkillProfile.currentSkillScore;

        float skillScore = rawScore * playerSkillProfile.difficultyMultiplier;

        // ── Grace period: clamp score to a very low value for the first N seconds
        //    so every new fight starts gentle regardless of past history.
        bool inGrace = fightStartTime >= 0f && Time.time - fightStartTime < gracePeriodSeconds;
        if (inGrace)
            skillScore = Mathf.Min(skillScore, gracePeriodScoreOverride);

        // ── Boss death spiral: when boss is nearly dead, treat skill as very high
        //    so the difficulty engine pushes the boss to maximum aggression / speed.
        BossAIController activeBoss = BossAIController.ActiveBoss;
        if (activeBoss != null && activeBoss.GetHealthPercent() < deathSpiralThreshold)
        {
            skillScore = Mathf.Max(skillScore, deathSpiralScoreFloor);
            deathSpiralActive = true;
        }
        else
        {
            deathSpiralActive = false;
        }

        DifficultySettings targetSettings = DifficultyEngine.EvaluateSkillScore(
            Mathf.Clamp(skillScore, 0f, 100f),
            difficultySettingsAsset);

        if (hiddenAssistActive && !deathSpiralActive)  // don't soften the boss when it's at death's door
        {
            targetSettings.bossDamageMultiplier *= hiddenAssistDamageMultiplier;
            targetSettings.hiddenAssistMultiplier = hiddenAssistDamageMultiplier;
        }

        if (Time.time < heatModeEndTime)
            targetSettings.playerDamageMultiplier *= heatModeDamageMultiplier;

        // During death spiral use a faster blend so the escalation feels sudden.
        float blendFactor = deathSpiralActive ? Mathf.Min(liveBlendFactor * 2f, 0.85f) : liveBlendFactor;

        currentSettings = DifficultyEngine.Blend(currentSettings, targetSettings, blendFactor);
        CombatEventSystem.RaiseDifficultyAdjusted(currentSettings);

        Debug.Log($"[FPM] Recalc — rawScore={rawScore:F1}  eff={skillScore:F1}  " +
                  $"grace={inGrace}  deathSpiral={deathSpiralActive}  " +
                  $"bossDmg×{currentSettings.BossDamageMultiplier:F2}  " +
                  $"telScale={currentSettings.TelegraphScale:F2}");
    }

    private void BroadcastSettings(float blend)
    {
        // At fight start always treat the player as a beginner (grace period score)
        // so the first encounter is forgiving regardless of saved profile.
        DifficultySettings targetSettings = DifficultyEngine.EvaluateSkillScore(
            gracePeriodScoreOverride, difficultySettingsAsset);
        currentSettings = DifficultyEngine.Blend(currentSettings, targetSettings, blend);
        CombatEventSystem.RaiseDifficultyAdjusted(currentSettings);
    }

    private void UpdateHiddenAssist()
    {
        if (PlayerHealth.Instance == null)
            return;

        float healthPercent = PlayerHealth.Instance.MaxHealth <= 0f
            ? 1f
            : PlayerHealth.Instance.CurrentHealth / PlayerHealth.Instance.MaxHealth;

        if (healthPercent < strugglingHealthThreshold)
        {
            if (lowHealthStartTime < 0f)
                lowHealthStartTime = Time.time;

            if (!hiddenAssistActive && Time.time - lowHealthStartTime >= strugglingDuration)
            {
                hiddenAssistActive = true;
                RecalculateDifficulty();
            }
        }
        else
        {
            lowHealthStartTime = -1f;
            if (hiddenAssistActive)
            {
                hiddenAssistActive = false;
                RecalculateDifficulty();
            }
        }
    }

    private void UpdateHeatMode()
    {
        if (heatModeEndTime > 0f && Time.time >= heatModeEndTime)
        {
            heatModeEndTime = -1f;
            RecalculateDifficulty();
        }
    }

    private void UpdateBossPressure()
    {
        BossAIController activeBoss = BossAIController.ActiveBoss;
        if (activeBoss == null || Time.time - lastBossHealthSampleTime < bossPressureSampleSeconds)
            return;

        float currentHealthPercent = activeBoss.GetHealthPercent();
        float healthLoss = lastBossHealthPercent - currentHealthPercent;

        if (healthLoss >= fastBossLossThreshold)
            activeBoss.ForceAdaptivePhaseAdvance();

        lastBossHealthPercent = currentHealthPercent;
        lastBossHealthSampleTime = Time.time;
    }

    private void HandlePlayerParry(bool success, float timingPrecision)
    {
        if (!fightActive)
            return;

        if (success)
        {
            successfulParryStreak++;
            if (successfulParryStreak >= parriesForHeatMode)
            {
                successfulParryStreak = 0;
                heatModeEndTime = Time.time + heatModeDuration;
                RecalculateDifficulty();
            }
        }
        else
        {
            successfulParryStreak = 0;
        }
    }

    private void HandleBossDefeated()
    {
        EndFight(true);
    }

    private void HandlePlayerDefeated(bool playerWon)
    {
        EndFight(playerWon);
    }
}
