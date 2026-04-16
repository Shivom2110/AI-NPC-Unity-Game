using UnityEngine;

/// <summary>
/// Place this on an empty GameObject with a Box Collider (Is Trigger = true)
/// covering the hall entrance. When the player walks in, the boss activates.
/// </summary>
public class BossEntranceTrigger : MonoBehaviour
{
    [SerializeField] private BossAIController boss;

    private bool _triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;
        if (boss != null)
            boss.TriggerEntrance();
    }
}
