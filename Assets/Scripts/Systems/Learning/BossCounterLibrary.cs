using System.Collections.Generic;

public static class BossCounterLibrary
{
    private static readonly Dictionary<PlayerAttackType, string> SingleAttackCounters =
        new Dictionary<PlayerAttackType, string>
        {
            { PlayerAttackType.AutoAttack, "Block" },
            { PlayerAttackType.Attack2, "Dodge" },
            { PlayerAttackType.Attack3, "HeavyCounter" },
            { PlayerAttackType.Attack4, "Interrupt" },
            { PlayerAttackType.Ultimate, "SpecialCounter" }
        };

    private static readonly Dictionary<string, string> ComboCounters =
        new Dictionary<string, string>
        {
            { "AutoAttack > AutoAttack > AutoAttack", "QuickPunish" },
            { "AutoAttack > Attack2", "BlockThenPunish" },
            { "Attack2 > Attack3", "DodgeThenStrike" },
            { "Attack2 > Attack3 > Attack4", "HeavyCounterChain" },
            { "Attack3 > Attack4 > Ultimate", "SpecialCounter" },
            { "AutoAttack > Attack2 > Attack3", "ShieldBreakPunish" },
            { "Attack2 > Attack3 > Attack4 > Ultimate", "FullComboCounter" }
        };

    /// <summary>
    /// Returns a descriptive counter name for the predicted next player action.
    /// Used by ComboTracker to label the boss's likely response.
    /// </summary>
    public static string GetCounterForPrediction(string predictedAction)
    {
        return predictedAction switch
        {
            "AutoAttack" => "QuickInterrupt",
            "Attack2"    => "DodgePunish",
            "Attack3"    => "GrabCounter",
            "Attack4"    => "HeavyInterrupt",
            "Ultimate"   => "UltimateInterrupt",
            _            => "AdaptiveResponse",
        };
    }

    public static string GetCounter(List<AttackEvent> comboSlice)
    {
        if (comboSlice == null || comboSlice.Count == 0)
            return "Idle";

        string signature = BuildSignature(comboSlice);
        if (ComboCounters.TryGetValue(signature, out string comboCounter))
            return comboCounter;

        PlayerAttackType lastAttack = comboSlice[comboSlice.Count - 1].AttackType;
        if (SingleAttackCounters.TryGetValue(lastAttack, out string singleCounter))
            return singleCounter;

        return "QuickAttack";
    }

    private static string BuildSignature(List<AttackEvent> comboSlice)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < comboSlice.Count; i++)
        {
            if (i > 0) sb.Append(" > ");
            sb.Append(comboSlice[i].AttackType);
        }

        return sb.ToString();
    }
}
