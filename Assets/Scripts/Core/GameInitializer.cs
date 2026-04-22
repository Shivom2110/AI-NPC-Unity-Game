using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    public static GameInitializer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureComponent<NPCMemoryManager>();
        EnsureComponent<ComboTracker>();
        EnsureComponent<DifficultyEngine>();
        EnsureComponent<FightProgressionManager>();

        Debug.Log("[GameInitializer] Systems ready.");
    }

    private void EnsureComponent<T>() where T : Component
    {
        T existing = FindObjectOfType<T>();
        if (existing == null)
        {
            gameObject.AddComponent<T>();
        }
    }
}
