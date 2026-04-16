using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatController : MonoBehaviour
{
    public struct HudMoveData
    {
        public string DisplayName;
        public string Keybind;
        public string IconLabel;
        public float Damage;
        public float Cooldown;
        public float RemainingCooldown;
        public bool RequiresDrawnWeapon;
        public bool IsUsable;
        public Color AccentColor;
    }

    [Header("References")]
    [SerializeField] private Animator           animator;
    [SerializeField] private SwordManager       swordManager;
    [SerializeField] private CharacterController controller;

    [Header("Targeting")]
    [SerializeField] private float attackRange      = 5f;
    [SerializeField] private float bossSearchRadius = 15f;

    [Header("Roll Settings")]
    [SerializeField] private float doubleTapWindow  = 0.35f;

    // Animator hashes
    private static readonly int LightAttackHash  = Animator.StringToHash("LightAttack");
    private static readonly int HeavyAttackHash  = Animator.StringToHash("HeavyAttack");
    private static readonly int ParryHash        = Animator.StringToHash("Parry");
    private static readonly int FlashyAttackHash = Animator.StringToHash("FlashyAttack");
    private static readonly int RollHash         = Animator.StringToHash("Roll");
    private static readonly int UltimateHash     = Animator.StringToHash("Ultimate");

    // Attack damage
    private readonly Dictionary<PlayerAttackType, float> damages       = new();
    private readonly Dictionary<PlayerAttackType, float> cooldowns     = new();
    private readonly Dictionary<PlayerAttackType, float> lastUsedTimes = new();

    // Ability cooldowns
    private const float ParryCooldown    = 0.3f;   // short — mistiming shouldn't lock you out
    private const float FlashyCooldown   = 4f;
    private const float RollCooldown     = 1f;
    private const float UltimateCooldown = 15f;

    private float _lastParryTime    = -999f;
    private float _lastFlashyTime   = -999f;
    private float _lastRollTime     = -999f;
    private float _lastUltimateTime = -999f;

    // Double-tap space
    private float _lastSpaceTapTime = -999f;

    private BossAIController currentBoss;
    private DifficultySettings _currentDifficulty;

    void Awake()
    {
        if (GetComponent<PlayerHealth>() == null)
            gameObject.AddComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        CombatEventSystem.OnDifficultyAdjusted += HandleDifficultyAdjusted;
    }

    void OnDisable()
    {
        CombatEventSystem.OnDifficultyAdjusted -= HandleDifficultyAdjusted;
    }

    // ── Init ──────────────────────────────────────────────────────
    void Start()
    {
        if (animator     == null) animator     = GetComponentInChildren<Animator>();
        if (swordManager == null) swordManager = GetComponent<SwordManager>();
        if (controller   == null) controller   = GetComponent<CharacterController>();

        damages[PlayerAttackType.AutoAttack] = 10f;
        damages[PlayerAttackType.Attack2]    = 50f;
        damages[PlayerAttackType.Attack3]    = 200f;
        damages[PlayerAttackType.Attack4]    = 150f;
        damages[PlayerAttackType.Ultimate]   = 300f;

        cooldowns[PlayerAttackType.AutoAttack] = 0.6f;  // prevent spam
        cooldowns[PlayerAttackType.Attack2]    = 1.5f;
        cooldowns[PlayerAttackType.Attack3]    = FlashyCooldown;
        cooldowns[PlayerAttackType.Attack4]    = 7f;
        cooldowns[PlayerAttackType.Ultimate]   = UltimateCooldown;

        foreach (PlayerAttackType attack in damages.Keys)
            lastUsedTimes[attack] = -999f;

        _currentDifficulty = FightProgressionManager.Instance != null
            ? FightProgressionManager.Instance.CurrentSettings
            : DifficultyEngine.EvaluateSkillScore(50f, null);
    }

    // ── Update ────────────────────────────────────────────────────
    void Update()
    {
        FindNearestBoss();
        HandleInput();
    }

    void HandleInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null)
            return;

        bool canAct = swordManager != null && swordManager.IsDrawn;

        if (canAct)
        {
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)  TryAttack(PlayerAttackType.AutoAttack, triggerLight: true);
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) TryAttack(PlayerAttackType.Attack2,    triggerHeavy: true);
            if (keyboard.qKey.wasPressedThisFrame)                       TryParry();
            if (keyboard.eKey.wasPressedThisFrame)                       TryFlashyAttack();
            if (keyboard.rKey.wasPressedThisFrame)                       TryUltimate();
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
            HandleSpaceTap();
    }

    // ── Parry (Q) ─────────────────────────────────────────────────
    private const float ParryCounterDamage = 25f;  // damage dealt to boss on perfect parry

    void TryParry()
    {
        if (Time.time - _lastParryTime < ParryCooldown) return;
        _lastParryTime = Time.time;

        if (animator != null)
        {
            animator.ResetTrigger(ParryHash);
            animator.SetTrigger(ParryHash);
        }

        bool parried = false;
        ParryResolution resolution = default;

        if (currentBoss != null)
            parried = currentBoss.TryParry(out resolution);
        else if (ParryWindow.Instance != null)
        {
            resolution = ParryWindow.Instance.ResolveParryAttempt();
            parried = resolution.success;
        }

        ComboTracker.Instance?.ReportParry(parried);

        if (parried)
        {
            float counterDamage = GetScaledParryDamage(resolution.counterDamageMultiplier);
            if (currentBoss != null)
                currentBoss.TakeDamage(counterDamage);

            CombatEventSystem.RaisePlayerAttack("parry_counter", true, counterDamage);
            Debug.Log($"[Player] PERFECT PARRY — dealt {counterDamage:F1} counter damage!");
        }
        else
        {
            Debug.Log(resolution.grade == ParryTimingGrade.Late
                ? "[Player] Late parry — reduced damage only"
                : "[Player] Parry (no attack to parry)");
        }
    }

    // ── Flashy Attack (E) ─────────────────────────────────────────
    void TryFlashyAttack()
    {
        if (Time.time - _lastFlashyTime < FlashyCooldown)
        {
            Debug.Log($"[Player] E on cooldown: {(FlashyCooldown - (Time.time - _lastFlashyTime)):F1}s");
            return;
        }

        _lastFlashyTime = Time.time;

        if (animator != null)
        {
            animator.ResetTrigger(FlashyAttackHash);
            animator.SetTrigger(FlashyAttackHash);
        }

        ExecuteAttack(PlayerAttackType.Attack3, "flash", damages[PlayerAttackType.Attack3]);

        Debug.Log("[Player] Flashy Attack!");
    }

    // ── Ultimate (R) ──────────────────────────────────────────────
    void TryUltimate()
    {
        if (Time.time - _lastUltimateTime < UltimateCooldown)
        {
            Debug.Log($"[Player] R on cooldown: {(UltimateCooldown - (Time.time - _lastUltimateTime)):F1}s");
            return;
        }

        _lastUltimateTime = Time.time;

        if (animator != null)
        {
            animator.ResetTrigger(UltimateHash);
            animator.SetTrigger(UltimateHash);
        }

        ExecuteAttack(PlayerAttackType.Ultimate, "ultimate", damages[PlayerAttackType.Ultimate]);

        Debug.Log("[Player] ULTIMATE!");
    }

    // ── Roll (Double Space) ───────────────────────────────────────
    void HandleSpaceTap()
    {
        float timeSinceLast = Time.time - _lastSpaceTapTime;

        if (timeSinceLast <= doubleTapWindow)
        {
            TryRoll();
            _lastSpaceTapTime = -999f;
        }
        else
        {
            _lastSpaceTapTime = Time.time;
        }
    }

    void TryRoll()
    {
        if (Time.time - _lastRollTime < RollCooldown) return;

        RollResolution resolution = default;
        bool startedRoll = RollSystem.Instance == null || RollSystem.Instance.TryStartRoll(out resolution);

        if (!startedRoll)
        {
            Debug.Log("[Player] Not enough stamina to roll.");
            return;
        }

        _lastRollTime = Time.time;

        if (animator != null)
        {
            animator.ResetTrigger(RollHash);
            animator.SetTrigger(RollHash);
        }

        bool dodged = RollSystem.Instance != null ? resolution.success : true;
        ComboTracker.Instance?.ReportDodge(dodged);
        Debug.Log(dodged ? "[Player] Perfect roll timing!" : "[Player] Roll started.");
    }

    // ── Standard Attacks (LMB / RMB) ─────────────────────────────
    void TryAttack(PlayerAttackType attackType,
                   bool triggerLight = false,
                   bool triggerHeavy = false)
    {
        float remaining = GetRemainingCooldown(attackType);
        if (remaining > 0f) return;

        lastUsedTimes[attackType] = Time.time;

        if (triggerLight && animator != null)
        {
            animator.ResetTrigger(LightAttackHash);
            animator.SetTrigger(LightAttackHash);
        }

        if (triggerHeavy && animator != null)
        {
            animator.ResetTrigger(HeavyAttackHash);
            animator.SetTrigger(HeavyAttackHash);
        }

        ExecuteAttack(attackType, ComboHitSystem.ToActionToken(attackType), damages[attackType]);
    }

    float GetRemainingCooldown(PlayerAttackType attackType)
    {
        float elapsed = Time.time - lastUsedTimes[attackType];
        return Mathf.Max(0f, cooldowns[attackType] - elapsed);
    }

    // ── Boss Detection ────────────────────────────────────────────
    void FindNearestBoss()
    {
        BossAIController best = null;
        float bestDist = float.MaxValue;

        if (IsBossTargetable(BossAIController.ActiveBoss, out float activeBossDist))
        {
            best = BossAIController.ActiveBoss;
            bestDist = activeBossDist;
        }

        if (best == null)
        {
            BossAIController[] bosses = FindObjectsOfType<BossAIController>();
            foreach (BossAIController boss in bosses)
            {
                if (!IsBossTargetable(boss, out float dist))
                    continue;

                if (dist < bestDist)
                {
                    best = boss;
                    bestDist = dist;
                }
            }
        }

        currentBoss = best;
    }

    // ── Cooldown Getters for HUD ──────────────────────────────────
    public BossAIController CurrentBoss => currentBoss;
    public bool IsWeaponDrawn => swordManager != null && swordManager.IsDrawn;

    public int GetHudMoveCount() => 6;

    public bool TryGetHudMoveData(int index, out HudMoveData moveData)
    {
        switch (index)
        {
            case 0:
                moveData = BuildHudMoveData(
                    "Slash", "LMB", "SL",
                    GetDisplayedDamage(PlayerAttackType.AutoAttack),
                    cooldowns[PlayerAttackType.AutoAttack],
                    GetRemainingCooldown(PlayerAttackType.AutoAttack),
                    requiresDrawnWeapon: true,
                    new Color(0.83f, 0.67f, 0.25f));
                return true;

            case 1:
                moveData = BuildHudMoveData(
                    "Heavy", "RMB", "HV",
                    GetDisplayedDamage(PlayerAttackType.Attack2),
                    cooldowns[PlayerAttackType.Attack2],
                    GetRemainingCooldown(PlayerAttackType.Attack2),
                    requiresDrawnWeapon: true,
                    new Color(0.77f, 0.42f, 0.23f));
                return true;

            case 2:
                moveData = BuildHudMoveData(
                    "Parry", "Q", "PR",
                    GetScaledParryDamage(1f),
                    ParryCooldown,
                    GetTimedCooldownRemaining(_lastParryTime, ParryCooldown),
                    requiresDrawnWeapon: true,
                    new Color(0.33f, 0.7f, 0.67f));
                return true;

            case 3:
                moveData = BuildHudMoveData(
                    "Flash", "E", "FL",
                    GetDisplayedDamage(PlayerAttackType.Attack3),
                    FlashyCooldown,
                    GetFlashyCooldownRemaining(),
                    requiresDrawnWeapon: true,
                    new Color(0.79f, 0.28f, 0.36f));
                return true;

            case 4:
                moveData = BuildHudMoveData(
                    "Roll", "2x SPACE", "RL",
                    0f,
                    RollCooldown,
                    GetTimedCooldownRemaining(_lastRollTime, RollCooldown),
                    requiresDrawnWeapon: false,
                    new Color(0.41f, 0.58f, 0.86f));
                return true;

            case 5:
                moveData = BuildHudMoveData(
                    "Ultimate", "R", "ULT",
                    GetDisplayedDamage(PlayerAttackType.Ultimate),
                    UltimateCooldown,
                    GetUltimateCooldownRemaining(),
                    requiresDrawnWeapon: true,
                    new Color(0.76f, 0.23f, 0.23f));
                return true;
        }

        moveData = default;
        return false;
    }

    public float GetFlashyCooldownRemaining()   => Mathf.Max(0f, FlashyCooldown   - (Time.time - _lastFlashyTime));
    public float GetUltimateCooldownRemaining() => Mathf.Max(0f, UltimateCooldown - (Time.time - _lastUltimateTime));

    private HudMoveData BuildHudMoveData(
        string displayName,
        string keybind,
        string iconLabel,
        float damage,
        float cooldown,
        float remainingCooldown,
        bool requiresDrawnWeapon,
        Color accentColor)
    {
        bool isUsable = !requiresDrawnWeapon || IsWeaponDrawn;

        return new HudMoveData
        {
            DisplayName = displayName,
            Keybind = keybind,
            IconLabel = iconLabel,
            Damage = damage,
            Cooldown = cooldown,
            RemainingCooldown = remainingCooldown,
            RequiresDrawnWeapon = requiresDrawnWeapon,
            IsUsable = isUsable,
            AccentColor = accentColor
        };
    }

    private float GetTimedCooldownRemaining(float lastUseTime, float cooldown)
    {
        return Mathf.Max(0f, cooldown - (Time.time - lastUseTime));
    }

    private void ExecuteAttack(PlayerAttackType attackType, string attackLabel, float baseDamage)
    {
        if (ComboTracker.Instance != null)
            ComboTracker.Instance.AddAttack(attackType, Time.time);

        string comboSignature = ComboHitSystem.ToActionToken(attackType);
        float comboMultiplier = 1f;
        if (ComboHitSystem.Instance != null)
            comboMultiplier = ComboHitSystem.Instance.RegisterAttack(attackType, Time.time, out comboSignature);

        bool landed = false;
        float finalDamage = GetScaledDamage(baseDamage, comboMultiplier);

        if (currentBoss != null)
        {
            currentBoss.OnPlayerCombatAction(attackType, Time.time);
            if (IsInRange(currentBoss.transform))
            {
                currentBoss.TakeDamage(finalDamage);
                landed = true;
            }
        }

        CombatEventSystem.RaisePlayerAttack(attackLabel, landed, landed ? finalDamage : 0f);
        ComboHitSystem.Instance?.ResolveCombo(comboSignature, landed);
    }

    private float GetDisplayedDamage(PlayerAttackType attackType)
    {
        return GetScaledDamage(damages[attackType], 1f);
    }

    private float GetScaledDamage(float baseDamage, float comboMultiplier)
    {
        float playerMultiplier = Mathf.Max(0.1f, _currentDifficulty.playerDamageMultiplier);
        return baseDamage * playerMultiplier * comboMultiplier;
    }

    private float GetScaledParryDamage(float counterMultiplier)
    {
        float safeMultiplier = Mathf.Max(1f, counterMultiplier);
        return GetScaledDamage(ParryCounterDamage, safeMultiplier);
    }

    private void HandleDifficultyAdjusted(DifficultySettings settings)
    {
        _currentDifficulty = settings;
    }

    bool IsInRange(Transform target) =>
        GetHorizontalDistance(target) <= attackRange;

    bool IsBossTargetable(BossAIController boss, out float distance)
    {
        distance = float.MaxValue;

        if (boss == null || !boss.gameObject.activeInHierarchy)
            return false;

        distance = GetHorizontalDistance(boss.transform);
        return distance <= bossSearchRadius;
    }

    float GetHorizontalDistance(Transform target)
    {
        if (target == null)
            return float.MaxValue;

        Vector3 from = transform.position;
        Vector3 to = target.position;
        from.y = 0f;
        to.y = 0f;
        return Vector3.Distance(from, to);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bossSearchRadius);
    }
}
