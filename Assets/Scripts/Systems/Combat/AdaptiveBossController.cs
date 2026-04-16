using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adaptive boss brain that predicts player habits and selects counter-attacks.
/// </summary>
[DisallowMultipleComponent]
public class AdaptiveBossController : MonoBehaviour
{
    [Header("Boss Data")]
    [SerializeField] private BossAttackCatalog attackCatalog;
    [SerializeField] private DifficultySettingsAsset difficultySettingsAsset;

    [Header("Pattern Memory")]
    [SerializeField] [Range(6, 20)] private int playerActionWindow = 20;
    [SerializeField] [Range(2, 6)] private int dominantPatternThreshold = 3;

    [Header("Phases")]
    [SerializeField] [Range(0.1f, 1f)] private float phase1Threshold = 0.7f;
    [SerializeField] [Range(0.05f, 1f)] private float phase2Threshold = 0.4f;
    [SerializeField] [Range(0.01f, 1f)] private float phase3Threshold = 0.15f;
    [SerializeField] [Range(0.01f, 1f)] private float secondWindThreshold = 0.1f;
    [SerializeField] [Range(0.01f, 1f)] private float secondWindHealPercent = 0.15f;
    [SerializeField] [Range(0.01f, 1f)] private float lastResortThreshold = 0.05f;

    [Header("Edge")]
    [SerializeField] private bool preventTrueDeath = true;
    [SerializeField] private bool guaranteedEdgeEnabled = true;
    [SerializeField] private float highSkillEdgeScore = 85f;
    [SerializeField] private float highSkillDamageBonus = 1.2f;
    [SerializeField] private float highSkillTelegraphPenalty = 0.7f;
    [SerializeField] private float guaranteedNoCounterInterval = 30f;

    private readonly Queue<string> recentPlayerActions = new Queue<string>();
    private readonly Dictionary<string, int> trigramCounts = new Dictionary<string, int>();

    private DifficultySettings currentSettings;
    private BossCombatPhase currentPhase = BossCombatPhase.Phase1;
    private BossAttackId lastAttackId = BossAttackId.QuickSlash;
    private string memorizedSpamCombo = string.Empty;
    private string lastResolvedCombo = string.Empty;
    private int spamComboStreak;
    private float nextGuaranteedAttackTime;
    private bool secondWindUsed;
    private bool lastResortUsed;

    /// <summary>Returns the live boss phase.</summary>
    public BossCombatPhase CurrentPhase => currentPhase;

    /// <summary>Returns the currently applied difficulty settings.</summary>
    public DifficultySettings CurrentSettings => currentSettings;

    /// <summary>Returns whether the boss can truly die.</summary>
    public bool PreventTrueDeath => preventTrueDeath;

    private void Awake()
    {
        if (attackCatalog == null)
            attackCatalog = BossAttackCatalog.CreateRuntimeDefault();

        if (difficultySettingsAsset == null)
            difficultySettingsAsset = DifficultySettingsAsset.CreateRuntimeDefault();

        currentSettings = DifficultyEngine.EvaluateSkillScore(50f, difficultySettingsAsset);
    }

    private void OnEnable()
    {
        CombatEventSystem.OnPlayerActionToken += HandlePlayerActionToken;
        CombatEventSystem.OnComboResolved += HandleComboResolved;
        CombatEventSystem.OnDifficultyAdjusted += HandleDifficultyAdjusted;
    }

    private void OnDisable()
    {
        CombatEventSystem.OnPlayerActionToken -= HandlePlayerActionToken;
        CombatEventSystem.OnComboResolved -= HandleComboResolved;
        CombatEventSystem.OnDifficultyAdjusted -= HandleDifficultyAdjusted;
    }

    /// <summary>
    /// Applies a new difficulty snapshot to the boss brain.
    /// </summary>
    public void ApplyDifficulty(DifficultySettings settings)
    {
        currentSettings = settings;
    }

    /// <summary>
    /// Evaluates the current phase from normalized health.
    /// </summary>
    public BossCombatPhase EvaluatePhase(float healthPercent)
    {
        if (healthPercent > phase1Threshold)
            return BossCombatPhase.Phase1;

        if (healthPercent > phase2Threshold)
            return BossCombatPhase.Phase2;

        if (healthPercent > phase3Threshold)
            return BossCombatPhase.Phase3;

        return BossCombatPhase.Phase4;
    }

    /// <summary>
    /// Selects and scales the next boss attack.
    /// </summary>
    public BossAttack SelectNextAttack(float healthPercent, float playerMaxHealth)
    {
        currentPhase = EvaluatePhase(healthPercent);

        if (currentPhase == BossCombatPhase.Phase4 && !lastResortUsed && healthPercent <= lastResortThreshold)
        {
            lastResortUsed = true;
            BossAttack lastResort = FindAttack(BossAttackId.LastResort);
            return ScaleAttack(lastResort, playerMaxHealth, true);
        }

        if (ShouldUseGuaranteedEdgeAttack())
        {
            BossAttack edgeAttack = FindAttack(BossAttackId.UnstoppableRush);
            nextGuaranteedAttackTime = Time.time + guaranteedNoCounterInterval;
            return ScaleAttack(edgeAttack, playerMaxHealth, true);
        }

        BossAttackId preferredCounter = PredictCounterAttack();
        List<BossAttack> phaseAttacks = attackCatalog.GetAttacksForPhase(currentPhase);
        BossAttack selectedAttack = FindPhaseAttack(phaseAttacks, preferredCounter);

        if (currentPhase == BossCombatPhase.Phase4 && selectedAttack.id == lastAttackId)
            selectedAttack = FindFirstDifferentAttack(phaseAttacks, lastAttackId);

        lastAttackId = selectedAttack.id;
        return ScaleAttack(selectedAttack, playerMaxHealth, false);
    }

    /// <summary>
    /// Attempts the boss second-wind recovery when the player is close to winning.
    /// </summary>
    public bool TryTriggerSecondWind(float healthPercent, out float healPercent)
    {
        healPercent = secondWindHealPercent;

        if (secondWindUsed || healthPercent > secondWindThreshold || currentPhase == BossCombatPhase.Phase4)
            return false;

        secondWindUsed = true;
        currentPhase = BossCombatPhase.Phase4;
        return true;
    }

    /// <summary>
    /// Forces the boss into the next phase early.
    /// </summary>
    public BossCombatPhase ForcePhaseAdvance()
    {
        int nextPhase = Mathf.Clamp((int)currentPhase + 1, (int)BossCombatPhase.Phase1, (int)BossCombatPhase.Phase4);
        currentPhase = (BossCombatPhase)nextPhase;
        return currentPhase;
    }

    private void HandlePlayerActionToken(string actionToken, float value)
    {
        string canonical = CanonicalizeToken(actionToken);

        recentPlayerActions.Enqueue(canonical);
        while (recentPlayerActions.Count > playerActionWindow)
            recentPlayerActions.Dequeue();

        if (recentPlayerActions.Count < 3)
            return;

        string[] array = recentPlayerActions.ToArray();
        int lastIndex = array.Length - 1;
        string trigram = $"{array[lastIndex - 2]}|{array[lastIndex - 1]}|{array[lastIndex]}";

        trigramCounts.TryGetValue(trigram, out int count);
        trigramCounts[trigram] = count + 1;
    }

    private void HandleComboResolved(string comboSignature, bool landed)
    {
        if (!landed)
            return;

        if (string.IsNullOrWhiteSpace(comboSignature) || comboSignature == "None")
            return;

        if (comboSignature == lastResolvedCombo)
            spamComboStreak++;
        else
            spamComboStreak = 1;

        lastResolvedCombo = comboSignature;
        memorizedSpamCombo = spamComboStreak >= 3 ? comboSignature : string.Empty;
    }

    private void HandleDifficultyAdjusted(DifficultySettings settings)
    {
        currentSettings = settings;
    }

    private bool ShouldUseGuaranteedEdgeAttack()
    {
        if (!guaranteedEdgeEnabled)
            return false;

        float skillScore = CombatTracker.Instance != null ? CombatTracker.Instance.CurrentSnapshot.skillScore : 50f;
        return skillScore > highSkillEdgeScore && Time.time >= nextGuaranteedAttackTime;
    }

    private BossAttackId PredictCounterAttack()
    {
        if (!string.IsNullOrEmpty(memorizedSpamCombo))
            return BossAttackId.GrabAttack;

        string dominantTrigram = string.Empty;
        int dominantCount = 0;

        foreach (KeyValuePair<string, int> pair in trigramCounts)
        {
            if (pair.Value > dominantCount)
            {
                dominantCount = pair.Value;
                dominantTrigram = pair.Key;
            }
        }

        if (dominantCount < dominantPatternThreshold)
            return GetPhaseDefaultAttack();

        if (dominantTrigram.Contains("dodge|dodge|attack") || dominantTrigram.Contains("dodge|dodge|light"))
            return BossAttackId.GrabAttack;

        if (dominantTrigram.Contains("light|light|heavy"))
            return BossAttackId.QuickSlash;

        if (dominantTrigram.Contains("block|block|dodge") || dominantTrigram.Contains("parry_fail|parry_fail|dodge"))
            return BossAttackId.DelayedHeavy;

        if (dominantTrigram.Contains("parry|parry|parry"))
            return BossAttackId.ComboString;

        return GetPhaseDefaultAttack();
    }

    private BossAttackId GetPhaseDefaultAttack()
    {
        switch (currentPhase)
        {
            case BossCombatPhase.Phase1:
                return Random.value <= currentSettings.parryableRatio ? BossAttackId.QuickSlash : BossAttackId.HeavySlam;
            case BossCombatPhase.Phase2:
                return Random.value <= 0.5f ? BossAttackId.DelayedHeavy : BossAttackId.SpinAttack;
            case BossCombatPhase.Phase3:
                return Random.value <= 0.5f ? BossAttackId.SpinAttack : BossAttackId.ComboString;
            case BossCombatPhase.Phase4:
                return Random.value <= 0.35f ? BossAttackId.UnstoppableRush : BossAttackId.GrabAttack;
            default:
                return BossAttackId.QuickSlash;
        }
    }

    private BossAttack FindPhaseAttack(List<BossAttack> phaseAttacks, BossAttackId preferredCounter)
    {
        for (int i = 0; i < phaseAttacks.Count; i++)
        {
            if (phaseAttacks[i].id == preferredCounter)
                return phaseAttacks[i];
        }

        if (phaseAttacks.Count > 0)
            return phaseAttacks[Random.Range(0, phaseAttacks.Count)];

        return FindAttack(BossAttackId.QuickSlash);
    }

    private BossAttack FindFirstDifferentAttack(List<BossAttack> phaseAttacks, BossAttackId disallowedAttack)
    {
        for (int i = 0; i < phaseAttacks.Count; i++)
        {
            if (phaseAttacks[i].id != disallowedAttack)
                return phaseAttacks[i];
        }

        return phaseAttacks.Count > 0 ? phaseAttacks[0] : FindAttack(BossAttackId.QuickSlash);
    }

    private BossAttack FindAttack(BossAttackId attackId)
    {
        IReadOnlyList<BossAttack> attacks = attackCatalog.GetAttacks();
        for (int i = 0; i < attacks.Count; i++)
        {
            if (attacks[i].id == attackId)
                return attacks[i];
        }

        return attacks.Count > 0 ? attacks[0] : default;
    }

    private BossAttack ScaleAttack(BossAttack attack, float playerMaxHealth, bool guaranteedNoCounter)
    {
        BossAttack scaled = attack;
        float telegraphScale = Mathf.Clamp(currentSettings.telegraphDuration / 0.8f, 0.15f, 1f);

        scaled.damage = attack.id == BossAttackId.LastResort
            ? playerMaxHealth * 0.6f
            : attack.damage * currentSettings.bossDamageMultiplier * currentSettings.edgeMultiplier;

        scaled.telegraphDuration = Mathf.Max(0f, attack.telegraphDuration * telegraphScale);

        if (currentPhase >= BossCombatPhase.Phase3 && Random.value > currentSettings.parryableRatio)
            scaled.isParryable = false;

        if (CombatTracker.Instance != null && CombatTracker.Instance.CurrentSnapshot.skillScore > highSkillEdgeScore)
        {
            scaled.damage *= highSkillDamageBonus;
            scaled.telegraphDuration *= highSkillTelegraphPenalty;
        }

        if (guaranteedNoCounter)
        {
            scaled.guaranteedNoCounter = true;
            scaled.isParryable = false;
            scaled.isUnblockable = true;
        }

        if (scaled.id == BossAttackId.ComboString)
            scaled.comboChainLength = currentPhase == BossCombatPhase.Phase4 ? 5 : 3 + (int)currentPhase - 1;

        return scaled;
    }

    private string CanonicalizeToken(string token)
    {
        string lower = token.ToLowerInvariant();

        if (lower.Contains("light") || lower.Contains("autoattack") || lower == "slash")
            return "light";

        if (lower.Contains("heavy") || lower.Contains("attack2"))
            return "heavy";

        if (lower.Contains("flash") || lower.Contains("attack3"))
            return "attack";

        if (lower.Contains("ultimate"))
            return "attack";

        if (lower.Contains("dodge"))
            return "dodge";

        if (lower.Contains("parry"))
            return "parry";

        if (lower.Contains("block"))
            return "block";

        return lower;
    }
}
