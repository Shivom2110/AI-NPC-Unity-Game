using System.Collections.Generic;
using UnityEngine;

public class BossAIController : MonoBehaviour
{
    [Header("Boss Identity")]
    [SerializeField] private string bossName = "Boss 1";

    [Header("Boss Settings")]
    [SerializeField] private float maxHealth           = 2000f;
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
    private float _nextDifficultyUpdate  = 0f;
    private int   _lastKnownSkillLevel   = 1;

    // Parry / dodge windows
    private bool  _parryWindowOpen = false;
    private bool  _dodgeWindowOpen = false;
    private float _parryWindowEnd  = 0f;
    private float _dodgeWindowEnd  = 0f;
    private float _pendingDamage   = 0f;

    // Roar state
    private float _roarEndTime = 0f;

    public bool ParryWindowOpen => _parryWindowOpen;
    public bool DodgeWindowOpen => _dodgeWindowOpen;

    private enum BossState { Hidden, WalkingIn, Roaring, Combat, Dead }
    private BossState _state = BossState.Hidden;

    // ── Lifecycle ─────────────────────────────────────────────────
    void Start()
    {
        currentHealth = maxHealth;

        if (bossAnimator == null)
            bossAnimator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        transform.position = spawnPosition;
        ApplySkillScaling(1); // start at skill 1
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (_state == BossState.WalkingIn) HandleEntrance();
        if (_state == BossState.Roaring)   HandleRoar();
        if (_state == BossState.Combat)
        {
            HandleAdaptiveDifficulty();
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

            if (bossAnimator != null)
            {
                bossAnimator.SetFloat(SpeedHash, 0f);
                bossAnimator.ResetTrigger(RoarHash);
                bossAnimator.SetTrigger(RoarHash);
            }

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
        if (_parryWindowOpen || _dodgeWindowOpen) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > preferredDistance)
        {
            Vector3 dir        = (player.position - transform.position).normalized;
            dir.y              = 0f;
            transform.position += dir * combatMoveSpeed * Time.deltaTime;

            if (bossAnimator != null)
                bossAnimator.SetFloat(SpeedHash, 1f);
        }
        else
        {
            if (bossAnimator != null)
                bossAnimator.SetFloat(SpeedHash, 0f);
        }

        FacePlayer();
    }

    // ── Combat AI ─────────────────────────────────────────────────
    void HandleCombatAI()
    {
        if (Time.time >= nextAttackTime && !_parryWindowOpen && !_dodgeWindowOpen)
        {
            if (player != null &&
                Vector3.Distance(transform.position, player.position) <= preferredDistance + 2f)
            {
                TriggerRandomAttack();
                nextAttackTime = Time.time + Random.Range(_scaledMinInterval, _scaledMaxInterval);
            }
        }
    }

    void TriggerRandomAttack()
    {
        if (bossAnimator == null) return;

        float heavyChance = Mathf.Clamp01(currentDifficultyPhase * 0.1f);
        bool  useHeavy    = Random.value < heavyChance;

        int roll;
        if (useHeavy)
            roll = Random.Range(2, 4);
        else
            roll = Random.Range(0, 2);

        switch (roll)
        {
            case 0:
                bossAnimator.ResetTrigger(AttackHash);
                bossAnimator.SetTrigger(AttackHash);
                OpenParryWindow(_scaledLightDamage);
                Debug.Log($"[{bossName}] LIGHT ATTACK ({_scaledLightDamage:F1} dmg) — parry with Q!");
                break;
            case 1:
                bossAnimator.ResetTrigger(KickHash);
                bossAnimator.SetTrigger(KickHash);
                OpenParryWindow(_scaledLightDamage);
                Debug.Log($"[{bossName}] KICK ({_scaledLightDamage:F1} dmg) — parry with Q!");
                break;
            case 2:
                bossAnimator.ResetTrigger(HeavyAttackHash);
                bossAnimator.SetTrigger(HeavyAttackHash);
                OpenDodgeWindow(_scaledHeavyDamage);
                Debug.Log($"[{bossName}] HEAVY ATTACK ({_scaledHeavyDamage:F1} dmg) — dodge with double Space!");
                break;
            case 3:
                bossAnimator.ResetTrigger(JumpAttackHash);
                bossAnimator.SetTrigger(JumpAttackHash);
                OpenDodgeWindow(_scaledHeavyDamage);
                Debug.Log($"[{bossName}] JUMP ATTACK ({_scaledHeavyDamage:F1} dmg) — dodge with double Space!");
                break;
        }
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
        if (_parryWindowOpen && Time.time > _parryWindowEnd)
        {
            _parryWindowOpen = false;
            DamagePlayer(_pendingDamage);
            Debug.Log($"[{bossName}] attack landed — {_pendingDamage} dmg to player!");
        }

        if (_dodgeWindowOpen && Time.time > _dodgeWindowEnd)
        {
            _dodgeWindowOpen = false;
            DamagePlayer(_pendingDamage);
            Debug.Log($"[{bossName}] attack landed — {_pendingDamage} dmg to player!");
        }
    }

    // ── Player Reactions ──────────────────────────────────────────
    public bool TryParry()
    {
        if (!_parryWindowOpen) return false;

        _parryWindowOpen = false;

        if (bossAnimator != null)
        {
            bossAnimator.ResetTrigger(IsHurtHash);
            bossAnimator.SetTrigger(IsHurtHash);
        }

        nextAttackTime = Time.time + Random.Range(minAttackInterval, maxAttackInterval) + 1.5f;
        Debug.Log($"[{bossName}] PARRIED! Boss staggered.");
        return true;
    }

    public bool TryDodge()
    {
        if (!_dodgeWindowOpen) return false;

        _dodgeWindowOpen = false;
        Debug.Log($"[{bossName}] attack DODGED!");
        return true;
    }

    // ── Take Damage ───────────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        if (_state == BossState.Dead) return;

        currentHealth -= damage;
        currentHealth  = Mathf.Max(0f, currentHealth);
        UpdateDifficultyPhase();

        if (bossAnimator != null)
        {
            bossAnimator.ResetTrigger(IsHurtHash);
            bossAnimator.SetTrigger(IsHurtHash);
        }

        Debug.Log($"[{bossName}] took {damage} dmg. HP: {currentHealth}/{maxHealth} | Phase {currentDifficultyPhase}");

        if (currentHealth <= 0f) Die();
    }

    // ── Death ─────────────────────────────────────────────────────
    void Die()
    {
        _state = BossState.Dead;
        if (bossAnimator != null) bossAnimator.SetBool(IsAliveHash, false);
        Debug.Log($"[{bossName}] defeated.");
        Destroy(gameObject, 3f);
    }

    // ── Helpers ───────────────────────────────────────────────────
    void DamagePlayer(float damage)
    {
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.TakeDamage(damage);
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
        float lostRatio        = (maxHealth - currentHealth) / maxHealth;
        currentDifficultyPhase = Mathf.Clamp(1 + Mathf.FloorToInt(lostRatio * 10f), 1, 10);
    }

    float GetCurrentCounterCooldown() =>
        Mathf.Max(0.35f, baseCounterCooldown - (currentDifficultyPhase - 1) * 0.05f);

    public void OnPlayerCombatAction(PlayerAttackType attackType, float timeStamp)
    {
        if (_state != BossState.Combat) return;
        if (ComboTracker.Instance == null) return;

        List<AttackEvent> comboSlice = ComboTracker.Instance.GetLastComboSlice(comboReadDepth);
        lastObservedCombo            = ComboTracker.Instance.BuildSignature(comboSlice);

        if (Time.time - lastCounterTime < GetCurrentCounterCooldown()) return;

        lastCounterUsed = BossCounterLibrary.GetCounter(comboSlice);
        lastCounterTime = Time.time;
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
