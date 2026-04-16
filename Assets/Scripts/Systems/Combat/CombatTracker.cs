using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks rolling combat performance metrics and exposes a live skill snapshot.
/// </summary>
public class CombatTracker : MonoBehaviour
{
    public static CombatTracker Instance { get; private set; }

    [Header("Rolling Window")]
    [SerializeField] private float rollingWindowSeconds = 60f;
    [SerializeField] private float recalculateInterval = 5f;
    [SerializeField] private int reactionSampleCount = 10;

    [Header("Skill Weights")]
    [SerializeField] [Range(0f, 1f)] private float parryWeight = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float reactionWeight = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float damageRatioWeight = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float comboVarietyWeight = 0.25f;

    private readonly List<AttackSample> attackSamples = new List<AttackSample>();
    private readonly List<PrecisionSample> parrySamples = new List<PrecisionSample>();
    private readonly List<PrecisionSample> dodgeSamples = new List<PrecisionSample>();
    private readonly List<DamageSample> damageDealtSamples = new List<DamageSample>();
    private readonly List<DamageSample> damageTakenSamples = new List<DamageSample>();
    private readonly List<ActivitySample> attackActivitySamples = new List<ActivitySample>();
    private readonly List<ActivitySample> dodgeActivitySamples = new List<ActivitySample>();
    private readonly List<ActivitySample> blockActivitySamples = new List<ActivitySample>();
    private readonly List<ComboSample> comboSamples = new List<ComboSample>();
    private readonly List<AdaptationSample> adaptationSamples = new List<AdaptationSample>();
    private readonly List<string> fullFightComboHistory = new List<string>();
    private readonly Queue<float> reactionTimes = new Queue<float>();

    private CombatAnalyticsSnapshot currentSnapshot;
    private float fightStartTime = -1f;
    private float nextRecalculationTime;
    private float totalDamageDealt;
    private float totalDamageTaken;
    private int totalParryAttempts;
    private int totalParrySuccesses;
    private int totalDodgeAttempts;
    private int totalDodgeSuccesses;
    private string lastComboSignature = "None";
    private string comboAtLastPhaseShift = "None";
    private float lastBossTelegraphTime = -1f;

    /// <summary>Returns the latest rolling analytics snapshot.</summary>
    public CombatAnalyticsSnapshot CurrentSnapshot => currentSnapshot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        CombatEventSystem.OnPlayerAttack += HandlePlayerAttack;
        CombatEventSystem.OnPlayerParry += HandlePlayerParry;
        CombatEventSystem.OnPlayerDodge += HandlePlayerDodge;
        CombatEventSystem.OnPlayerDamaged += HandlePlayerDamaged;
        CombatEventSystem.OnBossAttackStart += HandleBossAttackStart;
        CombatEventSystem.OnComboResolved += HandleComboResolved;
        CombatEventSystem.OnBossPhaseChange += HandleBossPhaseChange;
    }

    private void OnDisable()
    {
        CombatEventSystem.OnPlayerAttack -= HandlePlayerAttack;
        CombatEventSystem.OnPlayerParry -= HandlePlayerParry;
        CombatEventSystem.OnPlayerDodge -= HandlePlayerDodge;
        CombatEventSystem.OnPlayerDamaged -= HandlePlayerDamaged;
        CombatEventSystem.OnBossAttackStart -= HandleBossAttackStart;
        CombatEventSystem.OnComboResolved -= HandleComboResolved;
        CombatEventSystem.OnBossPhaseChange -= HandleBossPhaseChange;
    }

    private void Update()
    {
        if (fightStartTime < 0f)
            return;

        if (Time.time >= nextRecalculationTime)
        {
            RecalculateSnapshot();
            nextRecalculationTime = Time.time + recalculateInterval;
        }
    }

    /// <summary>
    /// Explicitly starts a new fight tracking window.
    /// </summary>
    public void NotifyFightStarted()
    {
        if (fightStartTime >= 0f)
            return;

        fightStartTime = Time.time;
        nextRecalculationTime = Time.time + recalculateInterval;
    }

    /// <summary>
    /// Resets all tracked fight data.
    /// </summary>
    public void ResetFight()
    {
        attackSamples.Clear();
        parrySamples.Clear();
        dodgeSamples.Clear();
        damageDealtSamples.Clear();
        damageTakenSamples.Clear();
        attackActivitySamples.Clear();
        dodgeActivitySamples.Clear();
        blockActivitySamples.Clear();
        comboSamples.Clear();
        adaptationSamples.Clear();
        fullFightComboHistory.Clear();
        reactionTimes.Clear();

        currentSnapshot = default;
        fightStartTime = -1f;
        nextRecalculationTime = 0f;
        totalDamageDealt = 0f;
        totalDamageTaken = 0f;
        totalParryAttempts = 0;
        totalParrySuccesses = 0;
        totalDodgeAttempts = 0;
        totalDodgeSuccesses = 0;
        lastComboSignature = "None";
        comboAtLastPhaseShift = "None";
        lastBossTelegraphTime = -1f;
    }

    /// <summary>
    /// Builds a fight-end data summary.
    /// </summary>
    public CombatData BuildCombatData()
    {
        RecalculateSnapshot();

        return new CombatData
        {
            fightDuration = fightStartTime < 0f ? 0f : Time.time - fightStartTime,
            averageReactionTime = currentSnapshot.averageReactionTime,
            parrySuccessRate = totalParryAttempts <= 0 ? 0f : (float)totalParrySuccesses / totalParryAttempts,
            dodgeSuccessRate = totalDodgeAttempts <= 0 ? 0f : (float)totalDodgeSuccesses / totalDodgeAttempts,
            damageDealtTotal = totalDamageDealt,
            damageTakenTotal = totalDamageTaken,
            uniqueCombosUsed = GetUniqueComboCount(fullFightComboHistory),
            finalSkillScore = currentSnapshot.skillScore,
            comboHistory = new List<string>(fullFightComboHistory)
        };
    }

    private void HandlePlayerAttack(string attackType, bool landed, float damage)
    {
        EnsureFightStarted();

        attackSamples.Add(new AttackSample
        {
            timeStamp = Time.time,
            attackType = attackType,
            landed = landed,
            damage = damage
        });

        attackActivitySamples.Add(new ActivitySample
        {
            timeStamp = Time.time,
            duration = 0.35f
        });

        if (landed)
        {
            totalDamageDealt += damage;
            damageDealtSamples.Add(new DamageSample
            {
                timeStamp = Time.time,
                value = damage
            });
        }
    }

    private void HandlePlayerParry(bool success, float timingPrecision)
    {
        EnsureFightStarted();
        totalParryAttempts++;
        if (success)
            totalParrySuccesses++;

        parrySamples.Add(new PrecisionSample
        {
            timeStamp = Time.time,
            success = success,
            precisionMs = timingPrecision
        });

        blockActivitySamples.Add(new ActivitySample
        {
            timeStamp = Time.time,
            duration = 0.25f
        });

        RegisterReaction(timingPrecision);
    }

    private void HandlePlayerDodge(bool success, float timingPrecision)
    {
        EnsureFightStarted();
        totalDodgeAttempts++;
        if (success)
            totalDodgeSuccesses++;

        dodgeSamples.Add(new PrecisionSample
        {
            timeStamp = Time.time,
            success = success,
            precisionMs = timingPrecision
        });

        dodgeActivitySamples.Add(new ActivitySample
        {
            timeStamp = Time.time,
            duration = 0.4f
        });

        RegisterReaction(timingPrecision);
    }

    private void HandlePlayerDamaged(float damage, string attackType)
    {
        EnsureFightStarted();
        totalDamageTaken += damage;

        damageTakenSamples.Add(new DamageSample
        {
            timeStamp = Time.time,
            value = damage
        });
    }

    private void HandleBossAttackStart(BossAttack attack, float telegraphDuration)
    {
        EnsureFightStarted();
        lastBossTelegraphTime = Time.time;
    }

    private void HandleComboResolved(string comboSignature, bool landed)
    {
        EnsureFightStarted();
        lastComboSignature = comboSignature;

        comboSamples.Add(new ComboSample
        {
            timeStamp = Time.time,
            signature = comboSignature,
            landed = landed
        });
        fullFightComboHistory.Add(comboSignature);

        for (int i = adaptationSamples.Count - 1; i >= 0; i--)
        {
            if (adaptationSamples[i].resolved)
                continue;

            if (comboSignature != comboAtLastPhaseShift)
            {
                AdaptationSample sample = adaptationSamples[i];
                sample.resolved = true;
                sample.responseDelay = Time.time - sample.phaseShiftTime;
                adaptationSamples[i] = sample;
                break;
            }
        }
    }

    private void HandleBossPhaseChange(int newPhase)
    {
        EnsureFightStarted();
        comboAtLastPhaseShift = lastComboSignature;
        adaptationSamples.Add(new AdaptationSample
        {
            phaseShiftTime = Time.time,
            resolved = false,
            responseDelay = 10f
        });
    }

    private void RegisterReaction(float timingPrecision)
    {
        if (timingPrecision <= 0f || timingPrecision >= 999f)
            return;

        while (reactionTimes.Count >= reactionSampleCount)
            reactionTimes.Dequeue();

        reactionTimes.Enqueue(timingPrecision);
    }

    private void EnsureFightStarted()
    {
        if (fightStartTime >= 0f)
            return;

        NotifyFightStarted();
    }

    private void RecalculateSnapshot()
    {
        float cutoff = Time.time - rollingWindowSeconds;
        PruneOldEntries(cutoff);

        float oldSkillScore = currentSnapshot.skillScore;
        float avgReaction = AverageReactionTime();
        float parryPrecisionScore = GetParryScore();
        float reactionScore = Mathf.InverseLerp(800f, 120f, avgReaction <= 0f ? 800f : avgReaction);
        float damageRatio = SumDamage(damageDealtSamples) / Mathf.Max(1f, SumDamage(damageTakenSamples));
        float damageRatioScore = Mathf.InverseLerp(0.5f, 2.5f, damageRatio);
        float comboVarietyScore = GetComboVarietyScore();
        float patternRepetitionScore = GetPatternRepetitionScore();
        float aggressionIndex = GetAggressionIndex();
        float adaptationRate = GetAdaptationRate();
        float attackFrequency = rollingWindowSeconds <= 0f ? 0f : attackSamples.Count / rollingWindowSeconds;

        float skillScore = 100f * (
            parryPrecisionScore * parryWeight +
            reactionScore * reactionWeight +
            damageRatioScore * damageRatioWeight +
            comboVarietyScore * comboVarietyWeight);

        currentSnapshot = new CombatAnalyticsSnapshot
        {
            skillScore = Mathf.Clamp(skillScore, 0f, 100f),
            aggressionIndex = aggressionIndex,
            patternPredictability = Mathf.Clamp01((patternRepetitionScore * 0.7f) + ((1f - comboVarietyScore) * 0.3f)),
            adaptationRate = adaptationRate,
            averageReactionTime = avgReaction,
            parrySuccessRate = totalParryAttempts <= 0 ? 0f : (float)totalParrySuccesses / totalParryAttempts,
            dodgeSuccessRate = totalDodgeAttempts <= 0 ? 0f : (float)totalDodgeSuccesses / totalDodgeAttempts,
            damageRatio = damageRatio,
            comboVarietyScore = comboVarietyScore,
            patternRepetitionScore = patternRepetitionScore,
            attackFrequency = attackFrequency,
            attackTime = SumActivity(attackActivitySamples),
            dodgeTime = SumActivity(dodgeActivitySamples),
            blockTime = SumActivity(blockActivitySamples),
            favoriteCombo = GetFavoriteCombo()
        };

        if (!Mathf.Approximately(oldSkillScore, currentSnapshot.skillScore))
            CombatEventSystem.RaiseSkillScoreChanged(oldSkillScore, currentSnapshot.skillScore);
    }

    private void PruneOldEntries(float cutoff)
    {
        PruneList(attackSamples, cutoff, sample => sample.timeStamp);
        PruneList(parrySamples, cutoff, sample => sample.timeStamp);
        PruneList(dodgeSamples, cutoff, sample => sample.timeStamp);
        PruneList(damageDealtSamples, cutoff, sample => sample.timeStamp);
        PruneList(damageTakenSamples, cutoff, sample => sample.timeStamp);
        PruneList(attackActivitySamples, cutoff, sample => sample.timeStamp);
        PruneList(dodgeActivitySamples, cutoff, sample => sample.timeStamp);
        PruneList(blockActivitySamples, cutoff, sample => sample.timeStamp);
        PruneList(comboSamples, cutoff, sample => sample.timeStamp);
        PruneList(adaptationSamples, cutoff, sample => sample.phaseShiftTime);
    }

    private void PruneList<T>(List<T> list, float cutoff, System.Func<T, float> timeSelector)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (timeSelector(list[i]) < cutoff)
                list.RemoveAt(i);
        }
    }

    private float AverageReactionTime()
    {
        if (reactionTimes.Count == 0)
            return 0f;

        float total = 0f;
        foreach (float reaction in reactionTimes)
            total += reaction;

        return total / reactionTimes.Count;
    }

    private float GetParryScore()
    {
        if (parrySamples.Count == 0 && dodgeSamples.Count == 0)
            return 0.25f;

        float parryPrecision = AveragePrecision(parrySamples, 600f);
        float dodgePrecision = AveragePrecision(dodgeSamples, 600f);
        float parrySuccess = totalParryAttempts <= 0 ? 0f : (float)totalParrySuccesses / totalParryAttempts;
        float dodgeSuccess = totalDodgeAttempts <= 0 ? 0f : (float)totalDodgeSuccesses / totalDodgeAttempts;

        return Mathf.Clamp01((parryPrecision * 0.35f) + (dodgePrecision * 0.2f) + (parrySuccess * 0.3f) + (dodgeSuccess * 0.15f));
    }

    private float AveragePrecision(List<PrecisionSample> samples, float worstPrecisionMs)
    {
        if (samples.Count == 0)
            return 0f;

        float total = 0f;
        for (int i = 0; i < samples.Count; i++)
        {
            float clamped = Mathf.Clamp(samples[i].precisionMs, 0f, worstPrecisionMs);
            total += Mathf.InverseLerp(worstPrecisionMs, 0f, clamped);
        }

        return total / samples.Count;
    }

    private float SumDamage(List<DamageSample> samples)
    {
        float total = 0f;
        for (int i = 0; i < samples.Count; i++)
            total += samples[i].value;

        return total;
    }

    private float SumActivity(List<ActivitySample> samples)
    {
        float total = 0f;
        for (int i = 0; i < samples.Count; i++)
            total += samples[i].duration;

        return total;
    }

    private float GetComboVarietyScore()
    {
        if (comboSamples.Count == 0)
            return 0f;

        HashSet<string> unique = new HashSet<string>();
        for (int i = 0; i < comboSamples.Count; i++)
            unique.Add(comboSamples[i].signature);

        return Mathf.Clamp01((float)unique.Count / comboSamples.Count);
    }

    private float GetPatternRepetitionScore()
    {
        if (comboSamples.Count == 0)
            return 0f;

        Dictionary<string, int> counts = new Dictionary<string, int>();
        int bestCount = 0;

        for (int i = 0; i < comboSamples.Count; i++)
        {
            string signature = comboSamples[i].signature;
            counts.TryGetValue(signature, out int count);
            count++;
            counts[signature] = count;
            if (count > bestCount)
                bestCount = count;
        }

        return Mathf.Clamp01((float)bestCount / comboSamples.Count);
    }

    private float GetAggressionIndex()
    {
        float attackTime = SumActivity(attackActivitySamples);
        float dodgeTime = SumActivity(dodgeActivitySamples);
        float blockTime = SumActivity(blockActivitySamples);
        float total = attackTime + dodgeTime + blockTime;

        if (total <= 0.01f)
            return 0.5f;

        return Mathf.Clamp01(attackTime / total);
    }

    private float GetAdaptationRate()
    {
        if (adaptationSamples.Count == 0)
            return 0.5f;

        float total = 0f;
        for (int i = 0; i < adaptationSamples.Count; i++)
        {
            float delay = adaptationSamples[i].resolved
                ? adaptationSamples[i].responseDelay
                : Mathf.Min(10f, Time.time - adaptationSamples[i].phaseShiftTime);

            total += Mathf.Clamp01(1f - (delay / 10f));
        }

        return total / adaptationSamples.Count;
    }

    private string GetFavoriteCombo()
    {
        if (comboSamples.Count == 0)
            return "None";

        Dictionary<string, int> counts = new Dictionary<string, int>();
        string favorite = "None";
        int bestCount = 0;

        for (int i = 0; i < comboSamples.Count; i++)
        {
            string combo = comboSamples[i].signature;
            counts.TryGetValue(combo, out int count);
            count++;
            counts[combo] = count;

            if (count > bestCount)
            {
                bestCount = count;
                favorite = combo;
            }
        }

        return favorite;
    }

    private int GetUniqueComboCount(List<string> source)
    {
        HashSet<string> unique = new HashSet<string>();
        for (int i = 0; i < source.Count; i++)
            unique.Add(source[i]);

        return unique.Count;
    }

    private struct AttackSample
    {
        public float timeStamp;
        public string attackType;
        public bool landed;
        public float damage;
    }

    private struct PrecisionSample
    {
        public float timeStamp;
        public bool success;
        public float precisionMs;
    }

    private struct DamageSample
    {
        public float timeStamp;
        public float value;
    }

    private struct ActivitySample
    {
        public float timeStamp;
        public float duration;
    }

    private struct ComboSample
    {
        public float timeStamp;
        public string signature;
        public bool landed;
    }

    private struct AdaptationSample
    {
        public float phaseShiftTime;
        public bool resolved;
        public float responseDelay;
    }
}
