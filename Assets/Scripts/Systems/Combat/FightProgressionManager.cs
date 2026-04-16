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
    [SerializeField] private float liveAdjustmentInterval = 30f;
    [SerializeField] [Range(0f, 1f)] private float liveBlendFactor = 0.45f;

    [Header("Hidden Assist")]
    [SerializeField] [Range(0.1f, 0.5f)] private float strugglingHealthThreshold = 0.3f;
    [SerializeField] private float strugglingDuration = 20f;
    [SerializeField] [Range(0.5f, 1f)] private float hiddenAssistDamageMultiplier = 0.85f;

    [Header("Heat Mode")]
    [SerializeField] private int parriesForHeatMode = 3;
    [SerializeField] private float heatModeDuration = 10f;
    [SerializeField] [Range(1f, 2f)] private float heatModeDamageMultiplier = 1.25f;

    [Header("Boss Pressure")]
    [SerializeField] [Range(0.05f, 0.5f)] private float fastBossLossThreshold = 0.2f;
    [SerializeField] private float bossPressureSampleSeconds = 10f;

    private DifficultySettings currentSettings;
    private bool fightActive;
    private float fightStartTime = -1f;
    private float nextAdjustmentTime;
    private float lowHealthStartTime = -1f;
    private float heatModeEndTime = -1f;
    private int successfulParryStreak;
    private bool hiddenAssistActive;
    private float lastBossHealthPercent = 1f;
    private float lastBossHealthSampleTime;

    /// <summary>Returns the live difficulty settings currently applied to combat.</summary>
    public DifficultySettings CurrentSettings => currentSettings;

    /// <summary>Returns the runtime persistent player skill profile.</summary>
    public PlayerSkillProfile Profile => playerSkillProfile;

    /// <summary>Returns the player's current damage multiplier.</summary>
    public float CurrentPlayerDamageMultiplier => currentSettings.playerDamageMultiplier;

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
        float skillScore = CombatTracker.Instance != null && CombatTracker.Instance.CurrentSnapshot.skillScore > 0f
            ? CombatTracker.Instance.CurrentSnapshot.skillScore
            : playerSkillProfile.currentSkillScore;

        DifficultySettings targetSettings = DifficultyEngine.EvaluateSkillScore(
            Mathf.Clamp(skillScore * playerSkillProfile.difficultyMultiplier, 0f, 100f),
            difficultySettingsAsset);

        if (hiddenAssistActive)
        {
            targetSettings.bossDamageMultiplier *= hiddenAssistDamageMultiplier;
            targetSettings.hiddenAssistMultiplier = hiddenAssistDamageMultiplier;
        }

        if (Time.time < heatModeEndTime)
            targetSettings.playerDamageMultiplier *= heatModeDamageMultiplier;

        currentSettings = DifficultyEngine.Blend(currentSettings, targetSettings, liveBlendFactor);
        CombatEventSystem.RaiseDifficultyAdjusted(currentSettings);
    }

    private void BroadcastSettings(float blend)
    {
        DifficultySettings targetSettings = playerSkillProfile.GetDifficultyRecommendation();
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
