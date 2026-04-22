using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Sends player messages to the Groq chat completions API and returns AI-generated NPC replies.
/// Reads the API key from StreamingAssets/groq_config.txt or the GROQ_API_KEY environment variable.
/// Falls back gracefully when the key is missing or the request fails.
/// </summary>
public class GroqNPCResponder : MonoBehaviour
{
    public static GroqNPCResponder Instance { get; private set; }

    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string Model = "llama-3.1-8b-instant";
    private const int MaxTok = 180;
    private const float Temperature = 0.4f;

    private string _apiKey = "";

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_apiKey) &&
                               !_apiKey.Equals("YOUR_GROQ_API_KEY_HERE", StringComparison.OrdinalIgnoreCase);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<GroqNPCResponder>() != null) return;
        new GameObject("GroqNPCResponder").AddComponent<GroqNPCResponder>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadKey();
    }

    private void LoadKey()
    {
        try
        {
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "groq_config.txt");
            if (File.Exists(streamingPath))
            {
                _apiKey = File.ReadAllText(streamingPath).Trim();
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")?.Trim() ?? "";
            }

            if (IsAvailable)
            {
                Debug.Log("[GroqNPC] API key loaded. AI responses active.");
            }
            else
            {
                Debug.Log("[GroqNPC] No Groq key found. Using local NPC fallback responses.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GroqNPC] Could not load key: {e.Message}");
        }
    }

    public void Ask(string npcName, string playerMessage,
                    Action<string> onReply, Action onFallback = null)
    {
        if (!IsAvailable) { onFallback?.Invoke(); return; }
        StartCoroutine(SendRequest(npcName, playerMessage, onReply, onFallback));
    }

    private IEnumerator SendRequest(string npcName, string playerMessage,
                                    Action<string> onReply, Action onFallback)
    {
        string system = BuildSystem(npcName);
        string body = BuildBody(system, playerMessage);
        byte[] raw = Encoding.UTF8.GetBytes(body);

        using (UnityWebRequest req = new UnityWebRequest(ApiUrl, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(raw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
            req.timeout = 15;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[GroqNPC] Request failed: {req.error}\nBody: {req.downloadHandler.text}");
                onFallback?.Invoke();
                yield break;
            }

            string text = ExtractText(req.downloadHandler.text);
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning($"[GroqNPC] Empty response from API. Raw: {req.downloadHandler.text}");
                onFallback?.Invoke();
            }
            else
            {
                onReply(text.Trim());
            }
        }
    }

    private static string BuildSystem(string npcName)
    {
        return
            $"You are {npcName}, a wise NPC in a fantasy dungeon action game. " +
            "The player battles a powerful boss named Shaan who has four phases. " +
            "You know every game mechanic: " +
            "F = draw or sheathe sword, " +
            "WASD = move, " +
            "LMB = light attack, RMB = heavy attack, " +
            "Q = parry (press before the hit, perfect timing does double counter damage), " +
            "double tap SPACE = dodge roll (costs 20 stamina, regenerates at 10 per second), " +
            "E = Flashy Attack (100 damage, 4 second cooldown, interrupts boss heavy wind-ups), " +
            "R = Ultimate (300 damage, 15 second cooldown, also interrupts). " +
            "Boss phases: Phase 1 above 70 percent HP, Phase 2 from 40 to 70 percent, " +
            "Phase 3 from 15 to 40 percent, Phase 4 below 15 percent HP. " +
            "Keep every reply to 1 to 3 short sentences. Stay in character, wise and slightly mysterious. " +
            "Never mention APIs, code, engines, or that you are an AI.";
    }

    private static string BuildBody(string system, string userMsg)
    {
        var payload = new GroqChatRequest
        {
            model = Model,
            max_tokens = MaxTok,
            temperature = Temperature,
            messages = new[]
            {
                new GroqMessage { role = "system", content = system },
                new GroqMessage { role = "user", content = userMsg }
            }
        };

        return JsonUtility.ToJson(payload);
    }

    private static string ExtractText(string json)
    {
        try
        {
            GroqChatResponse response = JsonUtility.FromJson<GroqChatResponse>(json);
            if (response?.choices == null || response.choices.Length == 0) return null;
            return response.choices[0]?.message?.content;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GroqNPC] Response parse failed: {e.Message}");
            return null;
        }
    }

    [Serializable]
    private class GroqChatRequest
    {
        public string model;
        public int max_tokens;
        public float temperature;
        public GroqMessage[] messages;
    }

    [Serializable]
    private class GroqChatResponse
    {
        public GroqChoice[] choices;
    }

    [Serializable]
    private class GroqChoice
    {
        public GroqMessage message;
    }

    [Serializable]
    private class GroqMessage
    {
        public string role;
        public string content;
    }
}
