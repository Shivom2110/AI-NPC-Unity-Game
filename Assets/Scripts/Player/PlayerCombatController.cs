using UnityEngine;
using AINPC.Systems.Events;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Input Keys")]
    [SerializeField] private KeyCode lightAttackKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode heavyAttackKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode dodgeKey = KeyCode.Space;
    [SerializeField] private KeyCode blockKey = KeyCode.LeftShift;

    private float lastAttackTime;
    private BossAIController currentBoss;
    private string lastAction = "";

    private void PublishCombatEvent(PlayerEventType type, string meta = "")
    {
        string bossId = currentBoss != null ? currentBoss.gameObject.name : "";
        EventBus.Publish(new PlayerEvent(type, npcId: "", bossId: bossId, meta: meta));
    }

    void Update()
    {
        // Find nearby boss
        if (currentBoss == null)
        {
            FindNearbyBoss();
        }

        HandleCombatInput();
    }

    private void FindNearbyBoss()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, 15f);
        foreach (Collider col in nearbyObjects)
        {
            BossAIController boss = col.GetComponent<BossAIController>();
            if (boss != null)
            {
                currentBoss = boss;
                Debug.Log("Boss detected!");
                break;
            }
        }
    }

    private void HandleCombatInput()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        // Light Attack
        if (Input.GetKeyDown(lightAttackKey))
        {
            PerformLightAttack();
        }
        // Heavy Attack
        else if (Input.GetKeyDown(heavyAttackKey))
        {
            PerformHeavyAttack();
        }
        // Dodge
        else if (Input.GetKeyDown(dodgeKey))
        {
            PerformDodge();
        }
        // Block
        else if (Input.GetKey(blockKey))
        {
            PerformBlock();
        }
    }

    private void PerformLightAttack()
    {
        Debug.Log("Player: Light Attack!");
        lastAction = "light_attack";
        lastAttackTime = Time.time;

        PublishCombatEvent(PlayerEventType.AttackMid, "light");

        // Try to hit boss
        if (currentBoss != null && IsInRange(currentBoss.transform))
        {
            currentBoss.TakeDamage(attackDamage);
            currentBoss.OnPlayerCombatAction("light_attack");
        }

        // TODO: Play attack animation
    }

    private void PerformHeavyAttack()
    {
        Debug.Log("Player: Heavy Attack!");
        lastAction = "heavy_attack";
        lastAttackTime = Time.time;

        PublishCombatEvent(PlayerEventType.AttackHigh, "heavy");

        // Try to hit boss
        if (currentBoss != null && IsInRange(currentBoss.transform))
        {
            currentBoss.TakeDamage(attackDamage * 2);
            currentBoss.OnPlayerCombatAction("heavy_attack");
        }

        // TODO: Play heavy attack animation
    }

    private void PerformDodge()
    {
        Debug.Log("Player: Dodge!");
        lastAction = "dodge";

        // Determine dodge direction
        float horizontal = Input.GetAxis("Horizontal");
        string dodgeDirection = horizontal < 0 ? "dodge_left" : (horizontal > 0 ? "dodge_right" : "dodge_back");

        if (dodgeDirection == "dodge_left") PublishCombatEvent(PlayerEventType.DodgeLeft);
        else if (dodgeDirection == "dodge_right") PublishCombatEvent(PlayerEventType.DodgeRight);
        else PublishCombatEvent(PlayerEventType.DodgeBack);

        if (currentBoss != null)
        {
            currentBoss.OnPlayerCombatAction(dodgeDirection);
        }

        // TODO: Play dodge animation and grant i-frames
    }

    private void PerformBlock()
    {
        if (lastAction != "block") // Only log once
        {
            Debug.Log("Player: Blocking!");
            lastAction = "block";

            PublishCombatEvent(PlayerEventType.Block);

            if (currentBoss != null)
            {
                currentBoss.OnPlayerCombatAction("block");
            }
        }

        // TODO: Activate blocking state
    }

    private bool IsInRange(Transform target)
    {
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }

    // Called when player takes damage (call this from your damage system)
    public void TakeDamage(float damage)
    {
        // TODO: Reduce health, play hurt animation
        Debug.Log($"Player took {damage} damage!");
    }

    // Track player's current combat style for boss AI
    public string GetCombatStyle()
    {
        // Simple analysis - you can make this more sophisticated
        return lastAction;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}