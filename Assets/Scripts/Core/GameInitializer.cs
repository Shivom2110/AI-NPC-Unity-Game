using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;

public class GameInitializer : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string openAIApiKey = ""; // Set in inspector or load from config file
    
    private bool isInitialized = false;

    async void Start()
    {
        await InitializeFirebase();
        InitializeOpenAI();
        InitializeNPCSystem();
    }

    private async System.Threading.Tasks.Task InitializeFirebase()
    {
        Debug.Log("Initializing Firebase...");

        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                var app = FirebaseApp.DefaultInstance;
                Debug.Log("Firebase initialized successfully!");

                // Initialize the NPCMemoryManager
                NPCMemoryManager.Instance.Initialize();
            }
            else
            {
                Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Firebase initialization error: {e.Message}");
        }
    }

    private void InitializeOpenAI()
    {
        Debug.Log("Initializing OpenAI Service...");
        
        // OpenAI service initializes automatically as singleton
        // But we can configure it here if needed
        if (!string.IsNullOrEmpty(openAIApiKey))
        {
            // Set API key (you might want to do this more securely)
            // OpenAIService.Instance.SetApiKey(openAIApiKey);
            Debug.Log("OpenAI Service ready!");
        }
        else
        {
            Debug.LogWarning("OpenAI API key not set! Please add it in the inspector or config file.");
        }
    }

    private void InitializeNPCSystem()
    {
        Debug.Log("NPC System initialized!");
        isInitialized = true;
    }

    public bool IsReady()
    {
        return isInitialized;
    }
}
