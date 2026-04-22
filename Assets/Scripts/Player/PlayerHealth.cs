using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth        = 200f;
    [SerializeField] private float deathScreenDelay = 2f;   // seconds after death before UI shows

    public static PlayerHealth Instance { get; private set; }

    private float _currentHealth;
    private float _iframeEnd = 0f;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth     => maxHealth;
    public bool  IsDead        => _currentHealth <= 0f;
    public bool  IsInvincible  => Time.time < _iframeEnd;

    void Awake()
    {
        Instance       = this;
        _currentHealth = maxHealth;
    }

    void OnEnable()
    {
        CombatEventSystem.OnDifficultyAdjusted += HandleDifficultyAdjusted;
    }

    void OnDisable()
    {
        CombatEventSystem.OnDifficultyAdjusted -= HandleDifficultyAdjusted;

        if (Instance == this)
            Instance = null;
    }

    public void GrantIframes(float duration)
    {
        _iframeEnd = Time.time + duration;
    }

    public void SetMaxHealth(float newMaxHealth, bool preserveHealthPercentage)
    {
        float clampedMaxHealth = Mathf.Max(1f, newMaxHealth);
        float currentPercent = maxHealth <= 0f ? 1f : _currentHealth / maxHealth;

        maxHealth = clampedMaxHealth;
        _currentHealth = preserveHealthPercentage
            ? maxHealth * currentPercent
            : Mathf.Min(_currentHealth, maxHealth);
    }

    public void RestoreToPercent(float healthPercent)
    {
        _currentHealth = Mathf.Clamp01(healthPercent) * maxHealth;
    }

    public bool TakeDamage(float damage, string attackType = "boss")
    {
        if (IsDead) return false;

        if (IsInvincible)
        {
            Debug.Log("[Player] damage blocked by iframes!");
            return false;
        }

        _currentHealth -= damage;
        _currentHealth  = Mathf.Max(0f, _currentHealth);

        CombatEventSystem.RaisePlayerDamaged(damage, attackType);

        Debug.Log($"[Player] took {damage} dmg. HP: {_currentHealth}/{maxHealth}");

        if (_currentHealth <= 0f)
            Die();
        else
            PlayerAnimator.Instance?.TriggerHit();

        return true;
    }

    private void Die()
    {
        CombatEventBus.FirePlayerDied();
        Debug.Log("[Player] defeated.");

        // Play death animation
        PlayerAnimator.Instance?.TriggerDeath();

        // Disable movement and combat so player can't act while dying
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        PlayerCombatController pcc = GetComponent<PlayerCombatController>();
        if (pcc != null) pcc.enabled = false;

        // Show death screen after the animation has time to play
        Invoke(nameof(ShowDeathScreen), deathScreenDelay);
        CombatEventSystem.RaisePlayerDefeated(false);
    }

    private void ShowDeathScreen()
    {
        if (DeathScreen.Instance != null)
            DeathScreen.Instance.Show();
    }

    private void HandleDifficultyAdjusted(DifficultySettings settings)
    {
        SetMaxHealth(settings.playerMaxHP, true);
    }
}
