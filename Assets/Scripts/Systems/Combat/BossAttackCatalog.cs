using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject catalog for boss attacks.
/// </summary>
[CreateAssetMenu(fileName = "BossAttackCatalog", menuName = "Combat/Boss Attack Catalog")]
public class BossAttackCatalog : ScriptableObject
{
    [Header("Attack Definitions")]
    [SerializeField] private List<BossAttack> attacks = new List<BossAttack>();

    /// <summary>Returns the full boss attack list.</summary>
    public IReadOnlyList<BossAttack> GetAttacks()
    {
        return attacks;
    }

    /// <summary>Returns attacks available in the provided phase.</summary>
    public List<BossAttack> GetAttacksForPhase(BossCombatPhase phase)
    {
        List<BossAttack> phaseAttacks = new List<BossAttack>();
        int phaseValue = (int)phase;

        for (int i = 0; i < attacks.Count; i++)
        {
            BossAttack attack = attacks[i];
            if (attack.minPhase <= phaseValue && attack.maxPhase >= phaseValue)
                phaseAttacks.Add(attack);
        }

        return phaseAttacks;
    }

    /// <summary>Creates a runtime fallback catalog when no asset is assigned.</summary>
    public static BossAttackCatalog CreateRuntimeDefault()
    {
        BossAttackCatalog catalog = CreateInstance<BossAttackCatalog>();
        catalog.hideFlags = HideFlags.HideAndDontSave;
        catalog.attacks = new List<BossAttack>
        {
            new BossAttack
            {
                id = BossAttackId.QuickSlash,
                name = "QuickSlash",
                damage = 16f,
                isParryable = true,
                telegraphDuration = 0.4f,
                attackType = "physical",
                minPhase = 1,
                maxPhase = 4,
                comboChainLength = 1
            },
            new BossAttack
            {
                id = BossAttackId.HeavySlam,
                name = "HeavySlam",
                damage = 28f,
                isParryable = true,
                telegraphDuration = 0.7f,
                attackType = "physical",
                minPhase = 1,
                maxPhase = 4,
                comboChainLength = 1
            },
            new BossAttack
            {
                id = BossAttackId.SpinAttack,
                name = "SpinAttack",
                damage = 24f,
                isParryable = false,
                telegraphDuration = 0.3f,
                attackType = "physical",
                minPhase = 2,
                maxPhase = 4,
                isUnblockable = true,
                comboChainLength = 1
            },
            new BossAttack
            {
                id = BossAttackId.GrabAttack,
                name = "GrabAttack",
                damage = 36f,
                isParryable = false,
                telegraphDuration = 0.2f,
                attackType = "grab",
                minPhase = 2,
                maxPhase = 4,
                isUnblockable = true,
                comboChainLength = 1
            },
            new BossAttack
            {
                id = BossAttackId.DelayedHeavy,
                name = "DelayedHeavy",
                damage = 32f,
                isParryable = true,
                telegraphDuration = 1.2f,
                attackType = "physical",
                minPhase = 2,
                maxPhase = 4,
                comboChainLength = 1
            },
            new BossAttack
            {
                id = BossAttackId.ComboString,
                name = "ComboString",
                damage = 22f,
                isParryable = false,
                telegraphDuration = 0.35f,
                attackType = "combo",
                minPhase = 2,
                maxPhase = 4,
                comboChainLength = 4
            },
            new BossAttack
            {
                id = BossAttackId.UnstoppableRush,
                name = "UnstoppableRush",
                damage = 44f,
                isParryable = false,
                telegraphDuration = 0.1f,
                attackType = "special",
                minPhase = 4,
                maxPhase = 4,
                isUnblockable = true,
                guaranteedNoCounter = true,
                comboChainLength = 1
            },
            new BossAttack
            {
                id = BossAttackId.LastResort,
                name = "LastResort",
                damage = 0.6f,
                isParryable = false,
                telegraphDuration = 0f,
                attackType = "special",
                minPhase = 4,
                maxPhase = 4,
                isUnblockable = true,
                isUndodgeable = true,
                guaranteedNoCounter = true,
                comboChainLength = 1
            }
        };

        return catalog;
    }
}
