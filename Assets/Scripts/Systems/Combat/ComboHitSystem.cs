using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Tracks combo strings and returns damage modifiers for variety or repetition.
/// </summary>
public class ComboHitSystem : MonoBehaviour
{
    public static ComboHitSystem Instance { get; private set; }

    [Header("Combo Detection")]
    [SerializeField] private float comboWindowSeconds = 2f;
    [SerializeField] private int maxComboLength = 5;

    [Header("Combo Rewards")]
    [SerializeField] private float newComboBonusMultiplier = 1.1f;
    [SerializeField] private float spamPenaltyMultiplier = 0.8f;
    [SerializeField] private int spamThreshold = 3;

    private readonly List<ComboEntry> currentCombo = new List<ComboEntry>();
    private readonly Dictionary<string, int> comboUsage = new Dictionary<string, int>();
    private readonly List<string> comboHistory = new List<string>();
    private readonly HashSet<string> uniqueCombosThisFight = new HashSet<string>();

    private string lastComboSignature = "None";
    private int repeatedComboCount = 0;

    /// <summary>Returns the current combo signature.</summary>
    public string CurrentComboSignature => BuildSignature();

    /// <summary>Returns the current fight combo history.</summary>
    public IReadOnlyList<string> ComboHistory => comboHistory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Registers an attack and returns the active combo signature plus any bonus or penalty multiplier.
    /// </summary>
    public float RegisterAttack(PlayerAttackType attackType, float timeStamp, out string comboSignature)
    {
        AdvanceComboWindow(timeStamp);

        currentCombo.Add(new ComboEntry
        {
            token = ToActionToken(attackType),
            timeStamp = timeStamp
        });

        while (currentCombo.Count > maxComboLength)
            currentCombo.RemoveAt(0);

        comboSignature = BuildSignature();
        comboHistory.Add(comboSignature);

        comboUsage.TryGetValue(comboSignature, out int usageCount);
        usageCount++;
        comboUsage[comboSignature] = usageCount;

        if (comboSignature == lastComboSignature)
            repeatedComboCount++;
        else
            repeatedComboCount = 1;

        lastComboSignature = comboSignature;

        float multiplier = 1f;

        if (!uniqueCombosThisFight.Contains(comboSignature))
        {
            uniqueCombosThisFight.Add(comboSignature);
            multiplier *= newComboBonusMultiplier;
        }

        if (repeatedComboCount >= spamThreshold)
            multiplier *= spamPenaltyMultiplier;

        return multiplier;
    }

    /// <summary>
    /// Publishes the resolved combo result after the attack lands or misses.
    /// </summary>
    public void ResolveCombo(string comboSignature, bool landed)
    {
        if (string.IsNullOrWhiteSpace(comboSignature))
            return;

        CombatEventSystem.RaiseComboResolved(comboSignature, landed);
    }

    /// <summary>
    /// Returns a 0-1 combo variety score for the current fight.
    /// </summary>
    public float GetComboVarietyScore()
    {
        if (comboHistory.Count == 0)
            return 0f;

        return Mathf.Clamp01((float)uniqueCombosThisFight.Count / comboHistory.Count);
    }

    /// <summary>
    /// Returns how repetitive the player's current combo use is.
    /// </summary>
    public float GetPatternRepetitionScore()
    {
        if (comboHistory.Count == 0)
            return 0f;

        int maxUsage = 0;
        foreach (KeyValuePair<string, int> pair in comboUsage)
        {
            if (pair.Value > maxUsage)
                maxUsage = pair.Value;
        }

        return Mathf.Clamp01((float)maxUsage / comboHistory.Count);
    }

    /// <summary>
    /// Clears all combo state for a new fight.
    /// </summary>
    public void ResetFight()
    {
        currentCombo.Clear();
        comboUsage.Clear();
        comboHistory.Clear();
        uniqueCombosThisFight.Clear();
        lastComboSignature = "None";
        repeatedComboCount = 0;
    }

    /// <summary>
    /// Converts a player attack enum into a normalized action token.
    /// </summary>
    public static string ToActionToken(PlayerAttackType attackType)
    {
        switch (attackType)
        {
            case PlayerAttackType.AutoAttack:
                return "light";
            case PlayerAttackType.Attack2:
                return "heavy";
            case PlayerAttackType.Attack3:
                return "flash";
            case PlayerAttackType.Attack4:
                return "special";
            case PlayerAttackType.Ultimate:
                return "ultimate";
            default:
                return attackType.ToString().ToLowerInvariant();
        }
    }

    private void AdvanceComboWindow(float timeStamp)
    {
        if (currentCombo.Count == 0)
            return;

        if (timeStamp - currentCombo[currentCombo.Count - 1].timeStamp > comboWindowSeconds)
            currentCombo.Clear();
    }

    private string BuildSignature()
    {
        if (currentCombo.Count == 0)
            return "None";

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < currentCombo.Count; i++)
        {
            if (i > 0)
                builder.Append("-");

            builder.Append(currentCombo[i].token);
        }

        return builder.ToString();
    }

    private struct ComboEntry
    {
        public string token;
        public float timeStamp;
    }
}
