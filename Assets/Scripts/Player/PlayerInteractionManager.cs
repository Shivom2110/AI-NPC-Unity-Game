using UnityEngine;

/// <summary>
/// Detects nearby non-boss NPCs, shows a proximity prompt, and opens/closes
/// the <see cref="ChatDialogueUI"/> on E key press.
/// </summary>
public class PlayerInteractionManager : MonoBehaviour
{
    [SerializeField] private float   interactionRadius = 4f;
    [SerializeField] private KeyCode interactKey       = KeyCode.T;

    private NPCController _nearestNpc;

    private void Update()
    {
        // Don't scan while chat is open — let ChatDialogueUI handle its own input.
        if (ChatDialogueUI.Instance != null && ChatDialogueUI.Instance.IsOpen) return;

        FindNearestNpc();

        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log($"[PIM] T pressed. Instance={ChatDialogueUI.Instance != null}, NPC={_nearestNpc?.name ?? "none"}");
            if (_nearestNpc != null)
                ChatDialogueUI.Instance?.OpenChat(_nearestNpc);
        }
    }

    private void FindNearestNpc()
    {
        NPCController[] all  = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        NPCController   best = null;
        float bestDist       = float.MaxValue;

        foreach (var npc in all)
        {
            if (npc == null || npc.IsBoss()) continue;
            float d = Vector3.Distance(transform.position, npc.transform.position);
            if (d <= interactionRadius && d < bestDist) { best = npc; bestDist = d; }
        }

        _nearestNpc = best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
