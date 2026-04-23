using UnityEngine;

/// <summary>
/// Coordinates fight pacing, live difficulty updates, named phases, and post-fight profile persistence.
/// </summary>
public class FightProgressionManager : MonoBehaviour
{
    public enum TrainingPhase
    {
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3
    }

    public static FightProgressionManager Instance { get; private set; }

    [Header("Difficulty")]
    [SerializeField] private DifficultySettingsAsset difficultySettingsAsset;
    [SerializeField] private PlayerSkillProfile playerSkillProfile;
    [SerializeField] private float liveAdjustmentInterval = 10f;
    [SerializeField] [Range(0f, 1f)] private float liveBlendFactor = 0.40f;

    [Header("Grace Period")]
    [SerializeField] private float gracePeriodSeconds = 45f;
    [SerializeField] [Range(0f, 50f)] private float gracePeriodScoreOverride = 5f;

    [Header("Hidden Assist")]
    [SerializeField] [Range(0.1f, 0.5f)] private float strugglingHealthThreshold = 0.35f;
    [SerializeField] private float strugglingDuration = 8f;
    [SerializeField] [Range(0.5f, 1f)] private float hiddenAssistDamageMultiplier = 0.80f;

    [Header("Heat Mode")]
    [SerializeField] private int parriesForHeatMode = 3;
    [SerializeField] private float heatModeDuration = 10f;
    [SerializeField] [Range(1f, 2f)] private float heatModeDamageMultiplier = 1.25f;

    [Header("Boss Pressure")]
    [SerializeField] [Range(0.05f, 0.5f)] private float fastBossLossThreshold = 0.2f;
    [SerializeField] private float bossPressureSampleSeconds = 8f;

    [Header("Three-Phase Fight")]
    [SerializeField] [Range(0f, 100f)] private float phase2ScoreThreshold = 42f;
    [SerializeField] [Range(0f, 100f)] private float phase3ScoreThreshold = 73f;
    [SerializeField] [Range(0.1f, 1.5f)] private float phase1BossDamageScale = 0.78f;
    [SerializeField] [Range(0.1f, 2f)] private float phase1PlayerDamageScale = 1.22f;
    [SerializeField] [Range(0.1f, 2f)] private float phase2BossDamageScale = 1.0f;
    [SerializeField] [Range(0.1f, 2f)] private float phase2PlayerDamageScale = 0.92f;
    [SerializeField] [Range(0.1f, 2f)] private float phase3BossDamageScale = 1.18f;
    [SerializeField] [Range(0.1f, 2f)] private float phase3PlayerDamageScale = 0.68f;
    [SerializeField] [Range(0.1f, 2f)] private float phase1TelegraphScale = 1.25f;
    [SerializeField] [Range(0.1f, 2f)] private float phase2TelegraphScale = 0.95f;
    [SerializeField] [Range(0.1f, 2f)] private float phase3TelegraphScale = 0.65f;
    [SerializeField] [Range(0.1f, 1.5f)] private float phase1ParryWindow = 0.72f;
    [SerializeField] [Range(0.1f, 1.5f)] private float phase2ParryWindow = 0.42f;
    [SerializeField] [Range(0.1f, 1.5f)] private float phase3ParryWindow = 0.22f;
    [SerializeField] [Range(0.1f, 1.5f)] private float phase1DodgeWindow = 0.82f;
    [SerializeField] [Range(0.1f, 1.5f)] private float phase2DodgeWindow = 0.48f;
    [SerializeField] [Range(0.1f, 1.5f)] private float phase3DodgeWindow = 0.26f;
    [SerializeField] [Range(0.1f, 3f)] private float phase1AttackIntervalScale = 1.18f;
    [SerializeField] [Range(0.1f, 3f)] private float phase2AttackIntervalScale = 1.0f;
    [SerializeField] [Range(0.1f, 3f)] private float phase3AttackIntervalScale = 0.72f;

    [Header("Boss Desperation")]
    [SerializeField] [Range(0.05f, 0.4f)] private float deathSpiralThreshold = 0.20f;
    [SerializeField] [Range(0f, 100f)] private float deathSpiralScoreFloor = 90f;

    private DifficultySettings currentSettings;
    private TrainingPhase currentPhase = TrainingPhase.Phase1;
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

    public DifficultySettings CurrentSettings => currentSettings;
    public PlayerSkillProfile Profile => playerSkillProfile;
    public float CurrentPlayerDamageMultiplier => currentSettings.playerDamageMultiplier;
    public bool IsHeatModeActive => fightActive && Time.time < heatModeEndTime;
    public bool IsHiddenAssistActive => fightActive && hiddenAssistActive;
    public TrainingPhase CurrentPhase => currentPhase;
    public int CurrentPhaseIndex => (int)currentPhase;
    public string CurrentPhaseName => GetPhaseName(currentPhase);
    public string CurrentPhaseSubtitle => GetPhaseSubtitle(currentPhase);

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
        currentPhase = TrainingPhase.Phase1;
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
        CombatEventBus.FireFightEnded(playerWon, fightData.fightDuration, fightData.finalSkillScore);
    }

    public static string GetPhaseName(TrainingPhase phase)
    {
        switch (phase)
        {
            case TrainingPhase.Phase1:
                return "Phase I - Trial of Instinct";
            case TrainingPhase.Phase2:
                return "Phase II - Crucible of Steel";
            case TrainingPhase.Phase3:
                return "Phase III - Edge of Ruin";
            default:
                return "Unknown Phase";
        }
    }

    public static string GetPhaseSubtitle(TrainingPhase phase)
    {
        switch (phase)
        {
            case TrainingPhase.Phase1:
                return "Reads dodge, parry, and combo habits with forgiving timing.";
            case TrainingPhase.Phase2:
                return "Hard but fair pressure where the boss shrugs off more damage.";
            case TrainingPhase.Phase3:
                return "A near-unbeatable gauntlet with razor-thin reaction windows.";
            default:
                return string.Empty;
        }
    }

    private void RecalculateDifficulty()
    {
        float rawScore = CombatTracker.Instance != null && CombatTracker.Instance.CurrentSnapshot.skillScore > 0f
            ? CombatTracker.Instance.CurrentSnapshot.skillScore
            : playerSkillProfile.currentSkillScore;

        float skillScore = EvaluateAdaptiveSkillScore(rawScore) * playerSkillProfile.difficultyMultiplier;

        bool inGrace = fightStartTime >= 0f && Time.time - fightStartTime < gracePeriodSeconds;
        if (inGrace)
            skillScore = Mathf.Min(skillScore, gracePeriodScoreOverride);

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

        TrainingPhase targetPhase = DeterminePhase(skillScore);
        ApplyPhaseTuning(ref targetSettings, targetPhase);

        if (hiddenAssistActive && !deathSpiralActive)
        {
            targetSettings.bossDamageMultiplier *= hiddenAssistDamageMultiplier;
            targetSettings.hiddenAssistMultiplier = hiddenAssistDamageMultiplier;
        }

        if (Time.time < heatModeEndTime)
            targetSettings.playerDamageMultiplier *= heatModeDamageMultiplier;

        float blendFactor = deathSpiralActive ? Mathf.Min(liveBlendFactor * 2f, 0.85f) : liveBlendFactor;

        currentSettings = DifficultyEngine.Blend(currentSettings, targetSettings, blendFactor);
        UpdatePhase(targetPhase);
        CombatEventSystem.RaiseDifficultyAdjusted(currentSettings);
        CombatEventBus.FireDifficultyAdjusted(currentSettings);

        Debug.Log(
            $"[FPM] Recalc raw={rawScore:F1} eff={skillScore:F1} phase={CurrentPhaseName} " +
            $"grace={inGrace} deathSpiral={deathSpiralActive} " +
            $"bossDmgx{currentSettings.BossDamageMultiplier:F2} telScale={currentSettings.TelegraphScale:F2}");
    }

    private void BroadcastSettings(float blend)
    {
        DifficultySettings targetSettings = DifficultyEngine.EvaluateSkillScore(
            gracePeriodScoreOverride, difficultySettingsAsset);

        TrainingPhase targetPhase = DeterminePhase(gracePeriodScoreOverride);
        ApplyPhaseTuning(ref targetSettings, targetPhase);

        currentSettings = DifficultyEngine.Blend(currentSettings, targetSettings, blend);
        UpdatePhase(targetPhase);
        CombatEventSystem.RaiseDifficultyAdjusted(currentSettings);
        CombatEventBus.FireDifficultyAdjusted(currentSettings);
    }

    private float EvaluateAdaptiveSkillScore(float rawScore)
    {
        CombatAnalyticsSnapshot snapshot = CombatTracker.Instance != null
            ? CombatTracker.Instance.CurrentSnapshot
            : default;

        float reactionScore = snapshot.averageReactionTime > 0f
            ? Mathf.InverseLerp(800f, 140f, snapshot.averageReactionTime)
            : 0f;
        float defenseScore = (snapshot.parrySuccessRate + snapshot.dodgeSuccessRate) * 0.5f;
        float comboScore = snapshot.comboVarietyScore;
        float aggressionScore = snapshot.aggressionIndex;

        float weighted = Mathf.Clamp01(
            reactionScore * 0.38f +
            defenseScore * 0.34f +
            comboScore * 0.18f +
            aggressionScore * 0.10f);

        return Mathf.Max(rawScore, weighted * 100f);
    }

    private TrainingPhase DeterminePhase(float skillScore)
    {
        if (skillScore >= phase3ScoreThreshold)
            return TrainingPhase.Phase3;

        if (skillScore >= phase2ScoreThreshold)
            return TrainingPhase.Phase2;

        return TrainingPhase.Phase1;
    }

    private void ApplyPhaseTuning(ref DifficultySettings settings, TrainingPhase phase)
    {
        switch (phase)
        {
            case TrainingPhase.Phase1:
                settings.bossDamageMultiplier *= phase1BossDamageScale;
                settings.playerDamageMultiplier *= phase1PlayerDamageScale;
                settings.telegraphScale = Mathf.Max(settings.telegraphScale, phase1TelegraphScale);
                settings.parryWindowSeconds = Mathf.Max(settings.parryWindowSeconds, phase1ParryWindow);
                settings.dodgeWindowSeconds = Mathf.Max(settings.dodgeWindowSeconds, phase1DodgeWindow);
                settings.bossAttackIntervalMin *= phase1AttackIntervalScale;
                settings.bossAttackIntervalMax *= phase1AttackIntervalScale;
                break;
            case TrainingPhase.Phase2:
                settings.bossDamageMultiplier *= phase2BossDamageScale;
                settings.playerDamageMultiplier *= phase2PlayerDamageScale;
                settings.telegraphScale = phase2TelegraphScale;
                settings.parryWindowSeconds = phase2ParryWindow;
                settings.dodgeWindowSeconds = phase2DodgeWindow;
                settings.bossAttackIntervalMin *= phase2AttackIntervalScale;
                settings.bossAttackIntervalMax *= phase2AttackIntervalScale;
                break;
            case TrainingPhase.Phase3:
                settings.bossDamageMultiplier *= phase3BossDamageScale;
                settings.playerDamageMultiplier *= phase3PlayerDamageScale;
                settings.telegraphScale = Mathf.Min(settings.telegraphScale, phase3TelegraphScale);
                settings.parryWindowSeconds = Mathf.Min(settings.parryWindowSeconds, phase3ParryWindow);
                settings.dodgeWindowSeconds = Mathf.Min(settings.dodgeWindowSeconds, phase3DodgeWindow);
                settings.bossAttackIntervalMin *= phase3AttackIntervalScale;
                settings.bossAttackIntervalMax *= phase3AttackIntervalScale;
                break;
        }

        settings.telegraphDuration = settings.telegraphScale;
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
                CombatEventBus.FireAssistModeChanged(true);
                RecalculateDifficulty();
            }
        }
        else
        {
            lowHealthStartTime = -1f;
            if (hiddenAssistActive)
            {
                hiddenAssistActive = false;
                CombatEventBus.FireAssistModeChanged(false);
                RecalculateDifficulty();
            }
        }
    }

    private void UpdateHeatMode()
    {
        if (heatModeEndTime > 0f && Time.time >= heatModeEndTime)
        {
            heatModeEndTime = -1f;
            CombatEventBus.FireHeatModeChanged(false);
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
                CombatEventBus.FireHeatModeChanged(true);
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

    private void UpdatePhase(TrainingPhase targetPhase)
    {
        if (currentPhase == targetPhase)
            return;

        currentPhase = targetPhase;
        CombatEventSystem.RaiseBossPhaseChange((int)currentPhase);
        CombatEventBus.FireBossPhaseChanged((int)currentPhase);
    }
}
