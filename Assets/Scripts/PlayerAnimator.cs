using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimator : MonoBehaviour
{
    public static PlayerAnimator Instance { get; private set; }

    private Animator     _animator;
    private SwordManager _swordManager;
    private float        _currentSpeed;

    private static readonly int IsHurtHash       = Animator.StringToHash("IsHurt");
    private static readonly int IsDeadHash       = Animator.StringToHash("IsDead");
    private static readonly int LightAttackHash  = Animator.StringToHash("LightAttack");
    private static readonly int HeavyAttackHash  = Animator.StringToHash("HeavyAttack");
    private static readonly int FlashyAttackHash = Animator.StringToHash("FlashyAttack");
    private static readonly int UltimateHash     = Animator.StringToHash("Ultimate");
    private static readonly int ParryHash        = Animator.StringToHash("Parry");
    private static readonly int RollHash         = Animator.StringToHash("Roll");

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _animator     = GetComponentInChildren<Animator>();
        _swordManager = GetComponent<SwordManager>();

        if (_animator == null)
            Debug.LogError("[PlayerAnimator] No Animator found.");
    }

    void Update()
    {
        if (_animator == null) return;

        // Stop all animation updates when dead
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead) return;

        UpdateSpeed();
        UpdateCombatIdle();
    }

    // ── Speed ─────────────────────────────────────────────────────
    void UpdateSpeed()
    {
        float h = 0f, v = 0f;

        if (Keyboard.current.wKey.isPressed) v =  1f;
        if (Keyboard.current.sKey.isPressed) v = -1f;
        if (Keyboard.current.aKey.isPressed) h = -1f;
        if (Keyboard.current.dKey.isPressed) h =  1f;

        float inputAmount = new Vector2(h, v).magnitude;
        bool  isRunning   = Keyboard.current.leftShiftKey.isPressed;

        float targetSpeed = 0f;
        if (inputAmount > 0.1f)
            targetSpeed = isRunning ? 6f : 3f;

        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, 10f * Time.deltaTime);
        _animator.SetFloat("Speed", _currentSpeed);
    }

    // ── Combat Idle ───────────────────────────────────────────────
    void UpdateCombatIdle()
    {
        bool drawn = _swordManager != null && _swordManager.IsDrawn;
        _animator.SetBool("IsDrawn", drawn);
    }

    // ── Hit Reaction ──────────────────────────────────────────────
    public void TriggerHit()
    {
        if (_animator == null) return;

        // Cancel every queued attack trigger
        _animator.ResetTrigger(LightAttackHash);
        _animator.ResetTrigger(HeavyAttackHash);
        _animator.ResetTrigger(FlashyAttackHash);
        _animator.ResetTrigger(UltimateHash);
        _animator.ResetTrigger(ParryHash);
        _animator.ResetTrigger(RollHash);

        _animator.ResetTrigger(IsHurtHash);
        _animator.SetTrigger(IsHurtHash);
    }

    // ── Death ─────────────────────────────────────────────────────
    public void TriggerDeath()
    {
        if (_animator == null) return;

        // Clear everything then lock into death state
        _animator.ResetTrigger(LightAttackHash);
        _animator.ResetTrigger(HeavyAttackHash);
        _animator.ResetTrigger(FlashyAttackHash);
        _animator.ResetTrigger(UltimateHash);
        _animator.ResetTrigger(ParryHash);
        _animator.ResetTrigger(RollHash);
        _animator.ResetTrigger(IsHurtHash);

        _animator.SetFloat("Speed", 0f);
        _animator.SetBool("IsDrawn", false);
        _animator.SetBool(IsDeadHash, true);
    }

    // Root motion handled by RootMotionBlocker on Animator child.
    void OnAnimatorMove() { }
}
