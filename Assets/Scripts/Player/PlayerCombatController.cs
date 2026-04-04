using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float bossSearchRadius = 15f;

    [Header("Keys")]
    [SerializeField] private KeyCode autoAttackKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode attack2Key = KeyCode.Mouse1;
    [SerializeField] private KeyCode attack3Key = KeyCode.Q;
    [SerializeField] private KeyCode attack4Key = KeyCode.R;
    [SerializeField] private KeyCode ultimateKey = KeyCode.F;

    private readonly Dictionary<PlayerAttackType, float> damages = new Dictionary<PlayerAttackType, float>();
    private readonly Dictionary<PlayerAttackType, float> cooldowns = new Dictionary<PlayerAttackType, float>();
    private readonly Dictionary<PlayerAttackType, float> lastUsedTimes = new Dictionary<PlayerAttackType, float>();

    private BossAIController currentBoss;

    private void Start()
    {
        damages[PlayerAttackType.AutoAttack] = 10f;
        damages[PlayerAttackType.Attack2] = 50f;
        damages[PlayerAttackType.Attack3] = 100f;
        damages[PlayerAttackType.Attack4] = 150f;
        damages[PlayerAttackType.Ultimate] = 300f;

        cooldowns[PlayerAttackType.AutoAttack] = 0f;
        cooldowns[PlayerAttackType.Attack2] = 3f;
        cooldowns[PlayerAttackType.Attack3] = 5f;
        cooldowns[PlayerAttackType.Attack4] = 7f;
        cooldowns[PlayerAttackType.Ultimate] = 10f;

        foreach (PlayerAttackType attack in damages.Keys)
        {
            lastUsedTimes[attack] = -999f;
        }
    }

    private void Update()
    {
        FindNearestBoss();
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(autoAttackKey)) TryAttack(PlayerAttackType.AutoAttack);
        if (Input.GetKeyDown(attack2Key)) TryAttack(PlayerAttackType.Attack2);
        if (Input.GetKeyDown(attack3Key)) TryAttack(PlayerAttackType.Attack3);
        if (Input.GetKeyDown(attack4Key)) TryAttack(PlayerAttackType.Attack4);
        if (Input.GetKeyDown(ultimateKey)) TryAttack(PlayerAttackType.Ultimate);
    }

    private void TryAttack(PlayerAttackType attackType)
    {
        float now = Time.time;
        float remainingCooldown = GetRemainingCooldown(attackType);

        if (remainingCooldown > 0f)
        {
            Debug.Log($"[Player] {attackType} on cooldown: {remainingCooldown:F1}s left");
            return;
        }

        lastUsedTimes[attackType] = now;

        if (ComboTracker.Instance != null)
        {
            ComboTracker.Instance.AddAttack(attackType, now);
        }

        if (currentBoss != null)
        {
            currentBoss.OnPlayerCombatAction(attackType, now);

            if (IsInRange(currentBoss.transform))
            {
                currentBoss.TakeDamage(damages[attackType]);
            }
        }

        Debug.Log($"[Player] Used {attackType} | damage={damages[attackType]} | cooldown={cooldowns[attackType]}");
    }

    private float GetRemainingCooldown(PlayerAttackType attackType)
    {
        float elapsed = Time.time - lastUsedTimes[attackType];
        return Mathf.Max(0f, cooldowns[attackType] - elapsed);
    }

    private void FindNearestBoss()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, bossSearchRadius);

        BossAIController best = null;
        float bestDistance = float.MaxValue;

        foreach (Collider col in nearby)
        {
            BossAIController boss = col.GetComponent<BossAIController>();
            if (boss == null) continue;

            float distance = Vector3.Distance(transform.position, boss.transform.position);
            if (distance < bestDistance)
            {
                best = boss;
                bestDistance = distance;
            }
        }

        currentBoss = best;
    }

    private bool IsInRange(Transform target)
    {
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bossSearchRadius);
    }
}
