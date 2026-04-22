using UnityEngine;

/// <summary>
/// ScriptableObject containing adaptive difficulty tuning values.
/// </summary>
[CreateAssetMenu(fileName = "DifficultySettingsAsset", menuName = "Combat/Difficulty Settings Asset")]
public class DifficultySettingsAsset : ScriptableObject
{
    [Header("Tier Scores")]
    [SerializeField] private Vector4 scoreBands = new Vector4(20f, 40f, 60f, 80f);

    [Header("Boss Damage  (tier 0 = easiest / beginner, tier 4 = expert)")]
    [SerializeField] private float bossDamageTier0 = 0.45f;  // very gentle start
    [SerializeField] private float bossDamageTier1 = 0.75f;
    [SerializeField] private float bossDamageTier2 = 1.0f;
    [SerializeField] private float bossDamageTier3 = 1.4f;
    [SerializeField] private float bossDamageTier4 = 1.9f;   // punishing for experts

    [Header("Boss Attack Interval (seconds between attacks)")]
    [SerializeField] private float attackIntervalTier0 = 5.0f;  // lots of breathing room
    [SerializeField] private float attackIntervalTier1 = 3.5f;
    [SerializeField] private float attackIntervalTier2 = 2.2f;
    [SerializeField] private float attackIntervalTier3 = 1.3f;
    [SerializeField] private float attackIntervalTier4 = 0.6f;  // relentless for experts

    [Header("Parryable Ratio (0=all unparryable, 1=all parryable)")]
    [SerializeField] private float parryableTier0 = 0.90f;  // almost always parryable at start
    [SerializeField] private float parryableTier1 = 0.70f;
    [SerializeField] private float parryableTier2 = 0.45f;
    [SerializeField] private float parryableTier3 = 0.22f;
    [SerializeField] private float parryableTier4 = 0.05f;  // expert faces mostly unparryables

    [Header("Player Health")]
    [SerializeField] private float playerHpTier0 = 220f;   // generous HP to learn the game
    [SerializeField] private float playerHpTier1 = 185f;
    [SerializeField] private float playerHpTier2 = 150f;
    [SerializeField] private float playerHpTier3 = 120f;
    [SerializeField] private float playerHpTier4 = 100f;

    [Header("Boss Health")]
    [SerializeField] private float bossHpTier0 = 500f;
    [SerializeField] private float bossHpTier1 = 750f;
    [SerializeField] private float bossHpTier2 = 1000f;
    [SerializeField] private float bossHpTier3 = 1400f;
    [SerializeField] private float bossHpTier4 = 2000f;

    [Header("Player Damage Multiplier")]
    [SerializeField] private float playerDamageTier0 = 0.85f;  // slightly dampened to lengthen learning fights
    [SerializeField] private float playerDamageTier1 = 0.92f;
    [SerializeField] private float playerDamageTier2 = 1.0f;
    [SerializeField] private float playerDamageTier3 = 1.15f;
    [SerializeField] private float playerDamageTier4 = 1.40f;

    [Header("Telegraph Scale (multiplier on each attack's base wind-up time)")]
    // Higher = longer telegraph = more time to react (easier).
    // Tier 0 gives 2× base time; tier 4 gives 0.45× (very tight).
    [SerializeField] private float telegraphTier0 = 2.0f;
    [SerializeField] private float telegraphTier1 = 1.5f;
    [SerializeField] private float telegraphTier2 = 1.0f;
    [SerializeField] private float telegraphTier3 = 0.70f;
    [SerializeField] private float telegraphTier4 = 0.45f;

    [Header("Edge")]
    [SerializeField] private float edgeTier0 = 1.02f;
    [SerializeField] private float edgeTier1 = 1.05f;
    [SerializeField] private float edgeTier2 = 1.10f;
    [SerializeField] private float edgeTier3 = 1.18f;
    [SerializeField] private float edgeTier4 = 1.28f;

    [Header("Stamina")]
    [SerializeField] private float playerStaminaTier0 = 130f;  // more stamina to experiment at start
    [SerializeField] private float playerStaminaTier1 = 110f;
    [SerializeField] private float playerStaminaTier2 = 90f;
    [SerializeField] private float playerStaminaTier3 = 75f;
    [SerializeField] private float playerStaminaTier4 = 60f;

    public Vector4 ScoreBands => scoreBands;
    public float[] BossDamageTiers => new[] { bossDamageTier0, bossDamageTier1, bossDamageTier2, bossDamageTier3, bossDamageTier4 };
    public float[] AttackIntervalTiers => new[] { attackIntervalTier0, attackIntervalTier1, attackIntervalTier2, attackIntervalTier3, attackIntervalTier4 };
    public float[] ParryableTiers => new[] { parryableTier0, parryableTier1, parryableTier2, parryableTier3, parryableTier4 };
    public float[] PlayerHpTiers => new[] { playerHpTier0, playerHpTier1, playerHpTier2, playerHpTier3, playerHpTier4 };
    public float[] BossHpTiers => new[] { bossHpTier0, bossHpTier1, bossHpTier2, bossHpTier3, bossHpTier4 };
    public float[] PlayerDamageTiers => new[] { playerDamageTier0, playerDamageTier1, playerDamageTier2, playerDamageTier3, playerDamageTier4 };
    public float[] TelegraphTiers => new[] { telegraphTier0, telegraphTier1, telegraphTier2, telegraphTier3, telegraphTier4 };
    public float[] EdgeTiers => new[] { edgeTier0, edgeTier1, edgeTier2, edgeTier3, edgeTier4 };
    public float[] PlayerStaminaTiers => new[] { playerStaminaTier0, playerStaminaTier1, playerStaminaTier2, playerStaminaTier3, playerStaminaTier4 };

    /// <summary>Creates a runtime default settings asset when none is assigned.</summary>
    public static DifficultySettingsAsset CreateRuntimeDefault()
    {
        DifficultySettingsAsset asset = CreateInstance<DifficultySettingsAsset>();
        asset.hideFlags = HideFlags.HideAndDontSave;
        return asset;
    }
}
