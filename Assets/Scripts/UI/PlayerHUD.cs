using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays player health bar and ability cooldowns for E (Flashy Attack) and R (Ultimate).
/// Attach to a Canvas GameObject. Assign references in Inspector.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image  healthFill;
    [SerializeField] private Color  healthFull    = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color  healthLow     = new Color(0.8f, 0.1f, 0.1f);

    [Header("Ability Cooldowns")]
    [SerializeField] private Image  eCooldownFill;   // radial fill image for E
    [SerializeField] private Text   eCooldownText;   // optional text showing seconds left
    [SerializeField] private Image  rCooldownFill;   // radial fill image for R
    [SerializeField] private Text   rCooldownText;

    [Header("Cooldown Settings")]
    [SerializeField] private float flashyCooldown  = 4f;
    [SerializeField] private float ultimateCooldown= 15f;

    private PlayerHealth         _playerHealth;
    private PlayerCombatController _combat;

    void Start()
    {
        _playerHealth = FindObjectOfType<PlayerHealth>();
        _combat       = FindObjectOfType<PlayerCombatController>();
    }

    void Update()
    {
        UpdateHealthBar();
        UpdateCooldowns();
    }

    void UpdateHealthBar()
    {
        if (_playerHealth == null || healthBar == null) return;

        float ratio = _playerHealth.CurrentHealth / _playerHealth.MaxHealth;
        healthBar.value = ratio;

        if (healthFill != null)
            healthFill.color = Color.Lerp(healthLow, healthFull, ratio);
    }

    void UpdateCooldowns()
    {
        if (_combat == null) return;

        float eRemaining = _combat.GetFlashyCooldownRemaining();
        float rRemaining = _combat.GetUltimateCooldownRemaining();

        // E cooldown
        if (eCooldownFill != null)
            eCooldownFill.fillAmount = eRemaining > 0f ? eRemaining / flashyCooldown : 0f;
        if (eCooldownText != null)
            eCooldownText.text = eRemaining > 0f ? eRemaining.ToString("F1") : "";

        // R cooldown
        if (rCooldownFill != null)
            rCooldownFill.fillAmount = rRemaining > 0f ? rRemaining / ultimateCooldown : 0f;
        if (rCooldownText != null)
            rCooldownText.text = rRemaining > 0f ? rRemaining.ToString("F1") : "";
    }
}
