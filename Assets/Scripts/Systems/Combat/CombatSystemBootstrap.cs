using UnityEngine;

/// <summary>
/// Ensures the adaptive combat systems exist in scenes that contain combat.
/// </summary>
public class CombatSystemBootstrap : MonoBehaviour
{
    private const string BootstrapName = "__AdaptiveCombatSystems";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrap()
    {
        CombatSystemBootstrap existing = Object.FindObjectOfType<CombatSystemBootstrap>();
        if (existing != null)
            return;

        GameObject root = new GameObject(BootstrapName);
        DontDestroyOnLoad(root);
        root.AddComponent<CombatSystemBootstrap>();
        root.AddComponent<ComboTracker>();
        root.AddComponent<CombatTracker>();
        root.AddComponent<ComboHitSystem>();
        root.AddComponent<ParryWindow>();
        root.AddComponent<RollSystem>();
        root.AddComponent<FightProgressionManager>();
    }
}
