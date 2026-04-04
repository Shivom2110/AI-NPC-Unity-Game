using UnityEngine;
using UnityEngine.UI;

public class BossDebugUIController : MonoBehaviour
{
    [SerializeField] private BossAIController targetBoss;
    [SerializeField] private Text bossNameText;
    [SerializeField] private Text bossHealthText;
    [SerializeField] private Text comboText;
    [SerializeField] private Text counterText;
    [SerializeField] private Text phaseText;

    private void Update()
    {
        if (targetBoss == null)
        {
            targetBoss = FindObjectOfType<BossAIController>();
            if (targetBoss == null) return;
        }

        if (bossNameText != null)
            bossNameText.text = targetBoss.GetBossName();

        if (bossHealthText != null)
            bossHealthText.text = $"HP: {targetBoss.GetCurrentHealth():0}/{targetBoss.GetMaxHealth():0}";

        if (comboText != null)
            comboText.text = $"Observed Combo: {targetBoss.GetLastObservedCombo()}";

        if (counterText != null)
            counterText.text = $"Counter: {targetBoss.GetLastCounterUsed()}";

        if (phaseText != null)
            phaseText.text = $"Phase: {targetBoss.GetDifficultyPhase()}";
    }
}
