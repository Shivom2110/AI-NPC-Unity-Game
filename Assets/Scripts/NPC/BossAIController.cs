using System.Collections.Generic;
using UnityEngine;

public class BossAIController : MonoBehaviour
{
    public static BossAIController ActiveBoss { get; private set; }

    [Header("Boss Identity")]
    [SerializeField] private string bossName = "Boss 1";

    [Header("Boss Settings")]
    [SerializeField] private float maxHealth           = 2400f;
    [SerializeField] private int   comboReadDepth      = 2;
    [SerializeField] private float baseCounterCooldown = 1.2f;

    [Header("Entrance")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(38f, -1f, -14f);
    [SerializeField] private Vector3 arenaPosition = new Vector3(26f,  0f, -12f);
    [SerializeField] private float   entranceSpeed  = 2f;
    [SerializeField] private float   rotationSpeed  = 6f;
    [SerializeField] private float   roarDuration   = 2.5f;  // how long the roar anim lasts

    [Header("Combat Movement")]
    [SerializeField] private float combatMoveSpeed   = 1.5f;
    [SerializeField] private float preferredDistance = 3f;

    [Header("Attack Settings (base values — scaled by player skill)")]
    [SerializeField] private float minAttackInterval   = 2f;
    [SerializeField] private float maxAttackInterval   = 4f;
    [SerializeField] private float parryWindowDuration = 0.6f;
    [SerializeField] private float dodgeWindowDuration = 0.8f;

    [Header("Damage to Player (base values — scaled by player skill)")]
    [SerializeField] private float lightAttackDamage = 6f;
    [SerializeField] private float heavyAttackDamage = 12f;

    [Header("Adaptive Difficulty")]
    [SerializeField] private float difficultyUpdateInterval = 3f;
    // Multiplier range across skill levels 1–5
    [SerializeField] private float damageAtSkill1   = 0.5f;   // x base damage
    [SerializeField] private float damageAtSkill5   = 1.6f;
    [SerializeField] private float parryWindowSkill1 = 1.0f;  // seconds
    [SerializeField] private float parryWindowSkill5 = 0.3f;
    [SerializeField] private float dodgeWindowSkill1 = 1.2f;
    [SerializeField] private float dodgeWindowSkill5 = 0.4f;
    [SerializeField] private float minIntervalSkill1 = 3.5f;
    [SerializeField] private float minIntervalSkill5 = 1.0f;
    [SerializeField] private float maxIntervalSkill1 = 5.5f;
    [SerializeField] private float maxIntervalSkill5 = 2.0f;

    [Header("References")]
    [SerializeField] private Animator  bossAnimator;
    [SerializeField] private Transform player;

    [Header("Adaptive Combat")]
    [SerializeField] private AdaptiveBossController adaptiveBoss;
    [SerializeField] private bool debugCombatLogs = true;

    // Animator hashes
    private static readonly int SpeedHash       = Animator.StringToHash("Speed");
    private static readonly int IsAliveHash     = Animator.StringToHash("IsAlive");
    private static readonly int RoarHash        = Animator.StringToHash("Roar");
    private static readonly int AttackHash      = Animator.StringToHash("Attack");
    private static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");
    private static readonly int JumpAttackHash  = Animator.StringToHash("JumpAttack");
    private static readonly int KickHash        = Animator.StringToHash("Kick");
    private static readonly int IsHurtHash      = Animator.StringToHash("IsHurt");

    private float  currentHealth;
    private float  lastCounterTime        = -999f;
    private float  nextAttackTime         = 0f;
    private int    currentDifficultyPhase = 1;
    private string lastObservedCombo      = "None";
    private string lastCounterUsed        = "Idle";

    // Runtime-scaled values (updated from player skill level)
    private float _scaledLightDamage;
    private float _scaledHeavyDamage;
    private float _scaledParryWindow;
    private float _scaledDodgeWindow;
    private float _scaledMinInterval;
    private float _scaledMaxInterval;
    private DifficultySettings _difficultySettings;
    private float _nextDifficultyUpdate  = 0f;
    private int   _lastKnownSkillLevel   = 1;

    // Parry / dodge windows
    private bool  _parryWindowOpen = false;
    private bool  _dodgeWindowOpen = false;
    private float _parryWindowEnd  = 0f;
    private float _dodgeWindowEnd  = 0f;
    private float _pendingDamage   = 0f;
    private float _pendingDamageScale = 1f;
    private BossAttack _activeAttack;
    private bool _attackActive = false;
    private bool _attackFullyCountered = false;
    private bool _hitboxOpened = false;
    private float _attackResolveTime = 0f;

    // Roar state
    private float _roarEndTime = 0f;

    // Animation / feel
    private float _bossSpeed     = 0f;   // smoothed Speed parameter
    private float _staggerEndTime = 0f;  // blocks movement and attacks during hurt recovery

    [Header("Hit Reaction Timing")]
    [SerializeField] private float hitStaggerDuration       = 0.40f;
    [SerializeField] private float interruptStaggerDuration = 0.85f;

    public bool ParryWindowOpen => _parryWindowOpen;
    public bool DodgeWindowOpen => _dodgeWindowOpen;

    private enum BossState { Hidden, WalkingIn, Roaring, Combat, Dead }
    private BossState _state = BossState.Hidden;

    void OnEnable()
    {
        ActiveBoss = this;
        CombatEventSystem.OnDifficultyAdjusted += HandleDifficultyAdjusted;
    }

    void OnDisable()
    {
        CombatEventSystem.OnDifficultyAdjusted -= HandleDifficultyAdjusted;

        if (ActiveBoss == this)
            ActiveBoss = null;
    }

    // ── Lifecycle ─────────────────────────────────────────────────
    void Start()
    {
        currentHealth = maxHealth;

        if (bossAnimator == null)
            bossAnimator = GetComponentInChildren<Animator>();

        if (adaptiveBoss == null)
            adaptiveBoss = GetComponent<AdaptiveBossController>();

        if (adaptiveBoss == null)
            adaptiveBoss = gameObject.AddComponent<AdaptiveBossController>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        transform.position = spawnPosition;
        ApplySkillScaling(1);
        ApplyDifficultySettings(FightProgressionManager.Instance != null
            ? FightProgressionManager.Instance.CurrentSettings
            : DifficultyEngine.EvaluateSkillScore(50f, null));
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (_state == BossState.WalkingIn) HandleEntrance();
        if (_state == BossState.Roaring)   HandleRoar();
        if (_state == BossState.Combat)
        {
            HandleCombatMovement();
            HandleCombatAI();
            CheckAttackWindows();
        }
    }

    void HandleAdaptiveDifficulty()
    {
        if (Time.time < _nextDifficultyUpdate) return;
        _nextDifficultyUpdate = Time.time + difficultyUpdateInterval;

        int skill = ComboTracker.Instance != null ? ComboTracker.Instance.SkillLevel : 1;
        if (skill == _lastKnownSkillLevel) return;

        _lastKnownSkillLevel = skill;
        ApplySkillScaling(skill);
        Debug.Log($"[{bossName}] Adaptive difficulty updated — player skill {skill}/5 | " +
                  $"LightDmg={_scaledLightDamage:F1} HeavyDmg={_scaledHeavyDamage:F1} " +
                  $"ParryWin={_scaledParryWindow:F2}s DodgeWin={_scaledDodgeWindow:F2}s " +
                  $"Interval={_scaledMinInterval:F1}-{_scaledMaxInterval:F1}s");
    }

    void ApplySkillScaling(int skill)
    {
        float t = (Mathf.Clamp(skill, 1, 5) - 1) / 4f; // 0 at skill 1, 1 at skill 5

        _scaledLightDamage  = lightAttackDamage * Mathf.Lerp(damageAtSkill1,    damageAtSkill5,    t);
        _scaledHeavyDamage  = heavyAttackDamage * Mathf.Lerp(damageAtSkill1,    damageAtSkill5,    t);
        _scaledParryWindow  = Mathf.Lerp(parryWindowSkill1, parryWindowSkill5, t);
        _scaledDodgeWindow  = Mathf.Lerp(dodgeWindowSkill1, dodgeWindowSkill5, t);
        _scaledMinInterval  = Mathf.Lerp(minIntervalSkill1, minIntervalSkill5, t);
        _scaledMaxInterval  = Mathf.Lerp(maxIntervalSkill1, maxIntervalSkill5, t);
    }

    // ── Entrance ──────────────────────────────────────────────────
    public void TriggerEntrance()
    {
        gameObject.SetActive(true);
        transform.position = spawnPosition;
        _state             = BossState.WalkingIn;

        if (bossAnimator != null)
        {
            bossAnimator.SetBool(IsAliveHash, true);
            bossAnimator.SetFloat(SpeedHash, 1f);
        }

        Debug.Log($"[{bossName}] entering the arena!");
    }

    void HandleEntrance()
    {
        Vector3 target = new Vector3(arenaPosition.x, transform.position.y, arenaPosition.z);
        float   dist   = Vector3.Distance(transform.position, target);

        if (dist > 0.3f)
        {
            Vector3 dir        = (target - transform.position).normalized;
            transform.position += dir * entranceSpeed * Time.deltaTime;
            transform.rotation  = Quaternion.Slerp(transform.rotation,
                                    Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Reached arena — snap to position and ROAR
            transform.position = arenaPosition;
            _state             = BossState.Roaring;
            _roarEndTime       = Time.time + roarDuration;
            _bossSpeed         = 0f;

            if (bossAnimator != null)
            {
                bossAnimator.SetFloat(SpeedHash, 0f);
                bossAnimator.ResetTrigger(RoarHash);
                bossAnimator.SetTrigger(RoarHash);
            }

            CombatEventBus.FireBossRoar();
            FacePlayer();
            Debug.Log($"[{bossName}] ROAR!");
        }
    }

    // ── Roar → Combat transition ──────────────────────────────────
    void HandleRoar()
    {
        if (Time.time >= _roarEndTime)
        {
            _state         = BossState.Combat;
            nextAttackTime = Time.time + Random.Range(_scaledMinInterval, _scaledMaxInterval);
            Debug.Log($"[{bossName}] combat start!");
        }
    }

    // ── Combat Movement ───────────────────────────────────────────
    void HandleCombatMovement()
    {
        if (player == null) return;
        if (_attackActive) return;

        float dist        = Vector3.Distance(transform.position, player.position);
        float targetSpeed = 0f;

        if (dist > preferredDistance)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0f;
            transform.position += dir * combatMoveSpeed * Time.deltaTime;
            targetSpeed = 1f;
        }

        _bossSpeed = Mathf.Lerp(_bossSpeed, targetSpeed, 8f * Time.deltaTime);
        if (bossAnimator != null)
            bossAnimator.SetFloat(SpeedHash, _bossSpeed);

        FacePlayer();
    }

    // ── Combat AI ─────────────────────────────────────────────────
    void HandleCombatAI()
    {
        if (_attackActive) return;
        if (Time.time < _staggerEndTime) return;

        if (Time.time >= nextAttackTime)
        {
            if (player != null &&
                Vector3.Distance(transform.position, player.position) <= preferredDistance + 2f)
            {
                TriggerAdaptiveAttack();
                nextAttackTime = Time.time + Random.Range(_scaledMinInterval, _scaledMaxInterval);
            }
        }
    }

    void TriggerAdaptiveAttack()
    {
        if (bossAnimator == null || adaptiveBoss == null) return;

        float playerMaxHealth = PlayerHealth.Instance != null ? PlayerHealth.Instance.MaxHealth : 200f;
        _activeAttack = adaptiveBoss.SelectNextAttack(GetHealthPercent(), playerMaxHealth);
        _pendingDamageScale = 1f;
        _attackFullyCountered = false;
        _attackActive = true;

        _pendingDamage = _activeAttack.damage;
        _parryWindowOpen = _activeAttack.isParryable && !_activeAttack.guaranteedNoCounter;
        _dodgeWindowOpen = !_activeAttack.isParryable || _activeAttack.isUndodgeable;
        _parryWindowEnd = Time.time + _activeAttack.telegraphDuration;
        _dodgeWindowEnd = _parryWindowEnd;
        _hitboxOpened = false;
        _attackResolveTime = _parryWindowEnd + Mathf.Max(0.14f, _scaledParryWindow, _scaledDodgeWindow);

        switch (_activeAttack.id)
        {
            case BossAttackId.QuickSlash:
                bossAnimator.ResetTrigger(AttackHash);
                bossAnimator.SetTrigger(AttackHash);
                break;
            case BossAttackId.HeavySlam:
            case BossAttackId.DelayedHeavy:
                bossAnimator.ResetTrigger(HeavyAttackHash);
                bossAnimator.SetTrigger(HeavyAttackHash);
                break;
            case BossAttackId.SpinAttack:
                bossAnimator.ResetTrigger(KickHash);
                bossAnimator.SetTrigger(KickHash);
                break;
            case BossAttackId.GrabAttack:
            case BossAttackId.UnstoppableRush:
                bossAnimator.ResetTrigger(JumpAttackHash);
                bossAnimator.SetTrigger(JumpAttackHash);
                break;
            case BossAttackId.ComboString:
            case BossAttackId.LastResort:
                bossAnimator.ResetTrigger(AttackHash);
                bossAnimator.SetTrigger(AttackHash);
                break;
        }

        CombatEventSystem.RaiseBossAttackStart(_activeAttack, _activeAttack.telegraphDuration);
        CombatEventBus.FireBossAttackTelegraph(_activeAttack, _activeAttack.telegraphDuration);

        if (debugCombatLogs)
            Debug.Log($"[{bossName}] {_activeAttack.name} telegraphed for {_activeAttack.telegraphDuration:F2}s ({_activeAttack.damage:F1} dmg)");
    }

    // ── Parry / Dodge Windows ─────────────────────────────────────
    void OpenParryWindow(float damage)
    {
        _parryWindowOpen = true;
        _parryWindowEnd  = Time.time + _scaledParryWindow;
        _pendingDamage   = damage;
    }

    void OpenDodgeWindow(float damage)
    {
        _dodgeWindowOpen = true;
        _dodgeWindowEnd  = Time.time + _scaledDodgeWindow;
        _pendingDamage   = damage;
    }

    void CheckAttackWindows()
    {
        if (_attackActive && !_hitboxOpened && Time.time >= _parryWindowEnd)
        {
            _hitboxOpened = true;
            CombatEventBus.FireBossAttackHitbox(_activeAttack);
        }

        if (_attackActive && Time.time >= _attackResolveTime)
        {
            if (!_attackFullyCountered)
            {
                float appliedDamage = _pendingDamage * _pendingDamageScale;
                if (appliedDamage > 0f)
                {
                    DamagePlayer(appliedDamage, _activeAttack.attackType);
                    if (debugCombatLogs)
                        Debug.Log($"[{bossName}] attack landed — {appliedDamage:F1} dmg to player!");
                }
            }

            ClearActiveAttack();
        }
    }

    // ── Player Reactions ──────────────────────────────────────────
    public bool TryParry()
    {
        return TryParry(out _);
    }

    public bool TryDodge()
    {
        return RollSystem.Instance != null && RollSystem.Instance.IsInvincible;
    }

    public bool TryParry(out ParryResolution resolution)
    {
        resolution = ParryWindow.Instance != null
            ? ParryWindow.Instance.ResolveParryAttempt()
            : default;

        if (!_attackActive)
            return resolution.success;

        _pendingDamageScale = Mathf.Min(_pendingDamageScale, resolution.playerDamageScale <= 0f ? 0f : resolution.playerDamageScale);

        if (resolution.grade == ParryTimingGrade.Perfect)
        {
            _attackFullyCountered = true;
            TriggerBossHitReaction(interruptAttack: _activeAttack.isParryable || _activeAttack.IsParryable);
            nextAttackTime = Time.time + Random.Range(_scaledMinInterval, _scaledMaxInterval) + 1.5f;
            ClearActiveAttack();
        }
        else if (resolution.grade == ParryTimingGrade.Good)
        {
            TriggerBossHitReaction(interruptAttack: false);
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.6f);
        }

        return resolution.success;
    }

    // ── Take Damage ───────────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        if (_state == BossState.Dead) return;

        float nextHealth = currentHealth - damage;
        float nextHealthPercent = maxHealth <= 0f ? 0f : nextHealth / maxHealth;

        if (adaptiveBoss != null && adaptiveBoss.TryTriggerSecondWind(nextHealthPercent, out float healPercent))
        {
            currentHealth = Mathf.Max(1f, maxHealth * healPercent);
            ForceAdaptivePhaseAdvanceTo(BossCombatPhase.Phase4);
            TriggerBossHitReaction();
            CombatEventBus.FireBossSecondWind();

            if (debugCombatLogs)
                Debug.Log($"[{bossName}] triggered second wind and healed to {currentHealth:F1}/{maxHealth:F1}");

            return;
        }

        if (nextHealth <= 0f && adaptiveBoss != null && adaptiveBoss.PreventTrueDeath)
        {
            currentHealth = Mathf.Max(1f, maxHealth * 0.03f);
            ForceAdaptivePhaseAdvanceTo(BossCombatPhase.Phase4);
            TriggerBossHitReaction();

            if (debugCombatLogs)
                Debug.Log($"[{bossName}] refused defeat and clung to the last phase.");

            return;
        }

        currentHealth = Mathf.Max(0f, nextHealth);
        UpdateDifficultyPhase();

        TriggerBossHitReaction();

        Debug.Log($"[{bossName}] took {damage} dmg. HP: {currentHealth}/{maxHealth} | Phase {currentDifficultyPhase}");

        if (currentHealth <= 0f) Die();
    }

    // ── Death ─────────────────────────────────────────────────────
    void Die()
    {
        _state = BossState.Dead;
        if (bossAnimator != null) bossAnimator.SetBool(IsAliveHash, false);

        if (CombatFX.Instance != null)
            CombatFX.Instance.Hitstop(0.20f);

        CombatEventSystem.RaiseBossDefeated();
        CombatEventBus.FireBossDied();
        Debug.Log($"[{bossName}] defeated.");
        Destroy(gameObject, 3f);
    }

    // ── Helpers ───────────────────────────────────────────────────
    void DamagePlayer(float damage, string attackType)
    {
        if (PlayerHealth.Instance != null)
        {
            bool applied = PlayerHealth.Instance.TakeDamage(damage, attackType);
            if (applied)
            {
                CombatEventSystem.RaiseBossAttackLand(damage);
                CombatEventBus.FireBossAttackLanded(damage);
            }
        }
        else
            Debug.LogWarning("[Boss] PlayerHealth not found!");
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                   Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
    }

    void UpdateDifficultyPhase()
    {
        currentDifficultyPhase = FightProgressionManager.Instance != null
            ? FightProgressionManager.Instance.CurrentPhaseIndex
            : adaptiveBoss != null
                ? Mathf.Clamp((int)adaptiveBoss.EvaluatePhase(GetHealthPercent()), 1, 3)
                : 1;
    }

    float GetCurrentCounterCooldown() =>
        Mathf.Max(0.35f, baseCounterCooldown - (currentDifficultyPhase - 1) * 0.05f);

    public void OnPlayerCombatAction(PlayerAttackType attackType, float timeStamp)
    {
        if (_state != BossState.Combat) return;

        List<AttackEvent> comboSlice = ComboTracker.Instance != null
            ? ComboTracker.Instance.GetLastComboSlice(comboReadDepth)
            : new List<AttackEvent>();

        lastObservedCombo = ComboTracker.Instance != null
            ? ComboTracker.Instance.BuildSignature(comboSlice)
            : ComboHitSystem.Instance != null ? ComboHitSystem.Instance.CurrentComboSignature : "None";

        if (Time.time - lastCounterTime < GetCurrentCounterCooldown()) return;

        lastCounterUsed = adaptiveBoss != null ? adaptiveBoss.CurrentPhase.ToString() : BossCounterLibrary.GetCounter(comboSlice);
        lastCounterTime = Time.time;
    }

    public void ApplyDifficultySettings(DifficultySettings settings)
    {
        _difficultySettings = settings;
        adaptiveBoss?.ApplyDifficulty(settings);

        float healthPercent = currentHealth <= 0f || maxHealth <= 0f ? 1f : currentHealth / maxHealth;
        maxHealth = Mathf.Max(1f, settings.bossMaxHP);
        currentHealth = currentHealth <= 0f ? maxHealth : Mathf.Clamp(maxHealth * healthPercent, 1f, maxHealth);

        _scaledLightDamage = lightAttackDamage * settings.bossDamageMultiplier * settings.edgeMultiplier * settings.hiddenAssistMultiplier;
        _scaledHeavyDamage = heavyAttackDamage * settings.bossDamageMultiplier * settings.edgeMultiplier * settings.hiddenAssistMultiplier;
        _scaledParryWindow = settings.parryWindowSeconds;
        _scaledDodgeWindow = settings.dodgeWindowSeconds;
        _scaledMinInterval = settings.bossAttackIntervalMin;
        _scaledMaxInterval = settings.bossAttackIntervalMax;
        currentDifficultyPhase = FightProgressionManager.Instance != null
            ? FightProgressionManager.Instance.CurrentPhaseIndex
            : currentDifficultyPhase;
    }

    public void ForceAdaptivePhaseAdvance()
    {
        if (adaptiveBoss == null)
            return;

        ForceAdaptivePhaseAdvanceTo(adaptiveBoss.ForcePhaseAdvance());
    }

    public float GetHealthPercent()
    {
        return maxHealth <= 0f ? 0f : currentHealth / maxHealth;
    }

    private void ForceAdaptivePhaseAdvanceTo(BossCombatPhase phase)
    {
        currentDifficultyPhase = Mathf.Clamp((int)phase, 1, 3);
        nextAttackTime = Mathf.Min(nextAttackTime, Time.time + 0.5f);
    }

    private void TriggerBossHitReaction(bool interruptAttack = false)
    {
        if (bossAnimator == null)
            return;

        if (interruptAttack)
        {
            bossAnimator.ResetTrigger(AttackHash);
            bossAnimator.ResetTrigger(HeavyAttackHash);
            bossAnimator.ResetTrigger(JumpAttackHash);
            bossAnimator.ResetTrigger(KickHash);
            bossAnimator.SetFloat(SpeedHash, 0f);
            bossAnimator.Play(0, 0, 0f);
        }

        bossAnimator.ResetTrigger(IsHurtHash);
        bossAnimator.SetTrigger(IsHurtHash);

        _staggerEndTime = Time.time + (interruptAttack ? interruptStaggerDuration : hitStaggerDuration);

        if (interruptAttack && CombatFX.Instance != null)
            CombatFX.Instance.Hitstop(0.10f);
    }

    private void ClearActiveAttack()
    {
        _attackActive = false;
        _parryWindowOpen = false;
        _dodgeWindowOpen = false;
        _pendingDamage = 0f;
        _pendingDamageScale = 1f;
        _attackFullyCountered = false;
        _hitboxOpened = false;
        _attackResolveTime = 0f;
        CombatEventBus.FireBossAttackEnded();
    }

    private void HandleDifficultyAdjusted(DifficultySettings settings)
    {
        ApplyDifficultySettings(settings);
    }

    // ── Getters ───────────────────────────────────────────────────
    public string GetBossName()          => bossName;
    public float  GetCurrentHealth()     => currentHealth;
    public float  GetMaxHealth()         => maxHealth;
    public int    GetComboReadDepth()    => comboReadDepth;
    public string GetLastObservedCombo() => lastObservedCombo;
    public string GetLastCounterUsed()   => lastCounterUsed;
    public int    GetDifficultyPhase()   => currentDifficultyPhase;
    public bool   IsInCombat()           => _state == BossState.Combat;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(spawnPosition, 0.3f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(arenaPosition, 0.3f);
        Gizmos.DrawLine(spawnPosition, arenaPosition);
        Gizmos.color = Color.cyan;
        if (player != null)
            Gizmos.DrawWireSphere(transform.position, preferredDistance);
    }
}
