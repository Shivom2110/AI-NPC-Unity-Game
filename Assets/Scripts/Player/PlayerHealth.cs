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

    public void GrantIframes(float duration)
    {
        _iframeEnd = Time.time + duration;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        if (IsInvincible)
        {
            Debug.Log("[Player] damage blocked by iframes!");
            return;
        }

        _currentHealth -= damage;
        _currentHealth  = Mathf.Max(0f, _currentHealth);

        Debug.Log($"[Player] took {damage} dmg. HP: {_currentHealth}/{maxHealth}");

        if (_currentHealth <= 0f)
            Die();
        else
            PlayerAnimator.Instance?.TriggerHit();
    }

    private void Die()
    {
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
    }

    private void ShowDeathScreen()
    {
        if (DeathScreen.Instance != null)
            DeathScreen.Instance.Show();
    }
}
