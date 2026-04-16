using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
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

    // ── Init ──────────────────────────────────────────────────────
    void Start()
    {
        if (animator     == null) animator     = GetComponentInChildren<Animator>();
        if (swordManager == null) swordManager = GetComponent<SwordManager>();
        if (controller   == null) controller   = GetComponent<CharacterController>();

        damages[PlayerAttackType.AutoAttack] = 10f;
        damages[PlayerAttackType.Attack2]    = 50f;
        damages[PlayerAttackType.Attack3]    = 100f;
        damages[PlayerAttackType.Attack4]    = 150f;
        damages[PlayerAttackType.Ultimate]   = 300f;

        cooldowns[PlayerAttackType.AutoAttack] = 0.6f;  // prevent spam
        cooldowns[PlayerAttackType.Attack2]    = 1.5f;
        cooldowns[PlayerAttackType.Attack3]    = 5f;
        cooldowns[PlayerAttackType.Attack4]    = 7f;
        cooldowns[PlayerAttackType.Ultimate]   = 10f;

        foreach (PlayerAttackType attack in damages.Keys)
            lastUsedTimes[attack] = -999f;
    }

    // ── Update ────────────────────────────────────────────────────
    void Update()
    {
        FindNearestBoss();
        HandleInput();
    }

    void HandleInput()
    {
        bool canAct = swordManager != null && swordManager.IsDrawn;

        if (canAct)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0)) TryAttack(PlayerAttackType.AutoAttack, triggerLight: true);
            if (Input.GetKeyDown(KeyCode.Mouse1)) TryAttack(PlayerAttackType.Attack2,    triggerHeavy: true);
            if (Input.GetKeyDown(KeyCode.Q))      TryParry();
            if (Input.GetKeyDown(KeyCode.E))      TryFlashyAttack();
            if (Input.GetKeyDown(KeyCode.R))      TryUltimate();
        }

        if (Input.GetKeyDown(KeyCode.Space))
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

        bool parried = currentBoss != null && currentBoss.TryParry();
        ComboTracker.Instance?.ReportParry(parried);

        if (parried)
        {
            currentBoss.TakeDamage(ParryCounterDamage);
            Debug.Log($"[Player] PERFECT PARRY — dealt {ParryCounterDamage} counter damage!");
        }
        else
        {
            Debug.Log("[Player] Parry (no attack to parry)");
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

        if (ComboTracker.Instance != null)
            ComboTracker.Instance.AddAttack(PlayerAttackType.Attack3, Time.time);

        if (currentBoss != null && IsInRange(currentBoss.transform))
            currentBoss.TakeDamage(200f);

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

        if (currentBoss != null && IsInRange(currentBoss.transform))
            currentBoss.TakeDamage(damages[PlayerAttackType.Ultimate]);

        if (ComboTracker.Instance != null)
            ComboTracker.Instance.AddAttack(PlayerAttackType.Ultimate, Time.time);

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
        _lastRollTime = Time.time;

        if (animator != null)
        {
            animator.ResetTrigger(RollHash);
            animator.SetTrigger(RollHash);
        }

        // Grant iframes for the duration of the roll
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.GrantIframes(0.6f);

        bool dodged = currentBoss != null && currentBoss.TryDodge();
        ComboTracker.Instance?.ReportDodge(dodged);
        Debug.Log(dodged ? "[Player] DODGE SUCCESS!" : "[Player] Roll");
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

        if (ComboTracker.Instance != null)
            ComboTracker.Instance.AddAttack(attackType, Time.time);

        if (currentBoss != null)
        {
            currentBoss.OnPlayerCombatAction(attackType, Time.time);
            if (IsInRange(currentBoss.transform))
                currentBoss.TakeDamage(damages[attackType]);
        }
    }

    float GetRemainingCooldown(PlayerAttackType attackType)
    {
        float elapsed = Time.time - lastUsedTimes[attackType];
        return Mathf.Max(0f, cooldowns[attackType] - elapsed);
    }

    // ── Boss Detection ────────────────────────────────────────────
    void FindNearestBoss()
    {
        Collider[] nearby   = Physics.OverlapSphere(transform.position, bossSearchRadius);
        BossAIController best = null;
        float bestDist      = float.MaxValue;

        foreach (Collider col in nearby)
        {
            BossAIController boss = col.GetComponentInParent<BossAIController>();
            if (boss == null) continue;

            float dist = Vector3.Distance(transform.position, boss.transform.position);
            if (dist < bestDist) { best = boss; bestDist = dist; }
        }

        currentBoss = best;
    }

    // ── Cooldown Getters for HUD ──────────────────────────────────
    public float GetFlashyCooldownRemaining()   => Mathf.Max(0f, FlashyCooldown   - (Time.time - _lastFlashyTime));
    public float GetUltimateCooldownRemaining() => Mathf.Max(0f, UltimateCooldown - (Time.time - _lastUltimateTime));

    bool IsInRange(Transform target) =>
        Vector3.Distance(transform.position, target.position) <= attackRange;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bossSearchRadius);
    }
}
