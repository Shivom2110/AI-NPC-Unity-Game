using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns floating damage numbers in world space above the boss and player.
/// Self-bootstraps — no Inspector wiring needed.
/// </summary>
public class DamageNumbers : MonoBehaviour
{
    public static DamageNumbers Instance { get; private set; }

    private Canvas _worldCanvas;
    private Font   _font;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<DamageNumbers>() != null) return;
        new GameObject("DamageNumbers").AddComponent<DamageNumbers>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // World-space canvas so numbers float in 3D
        GameObject canvasGO = new GameObject("DamageNumbersCanvas");
        canvasGO.transform.SetParent(transform);
        _worldCanvas = canvasGO.AddComponent<Canvas>();
        _worldCanvas.renderMode   = RenderMode.WorldSpace;
        _worldCanvas.sortingOrder = 5;
        canvasGO.AddComponent<GraphicRaycaster>();
    }

    private void OnEnable()
    {
        CombatEventBus.OnPlayerAttack     += OnPlayerAttack;
        CombatEventBus.OnBossAttackLanded += OnBossAttackLanded;
    }

    private void OnDisable()
    {
        CombatEventBus.OnPlayerAttack     -= OnPlayerAttack;
        CombatEventBus.OnBossAttackLanded -= OnBossAttackLanded;
    }

    // ── Events ─────────────────────────────────────────────────────────────────

    private void OnPlayerAttack(PlayerAttackType type, bool hitBoss, float damage)
    {
        if (!hitBoss || damage <= 0f) return;

        BossAIController boss = BossAIController.ActiveBoss;
        if (boss == null) return;

        Vector3 pos = boss.transform.position + Vector3.up * 2.5f
                      + Random.insideUnitSphere * 0.6f;
        pos.y = boss.transform.position.y + 2.5f + Random.Range(-0.3f, 0.5f);

        // Bigger number for bigger hits
        bool isCrit = damage > 80f;
        Color col = isCrit ? new Color(1f, 0.85f, 0.1f) : new Color(0.95f, 0.95f, 0.95f);
        int   sz  = isCrit ? 28 : 22;

        SpawnNumber($"{damage:0}", pos, col, sz, 1.0f);
    }

    private void OnBossAttackLanded(float damage)
    {
        if (damage <= 0f) return;

        PlayerHealth ph = PlayerHealth.Instance;
        if (ph == null) return;

        Vector3 pos = ph.transform.position + Vector3.up * 2f
                      + Random.insideUnitSphere * 0.3f;

        SpawnNumber($"-{damage:0}", pos, new Color(1f, 0.25f, 0.25f), 24, 0.9f);
    }

    // ── Core ───────────────────────────────────────────────────────────────────

    private void SpawnNumber(string text, Vector3 worldPos, Color color, int fontSize, float duration)
    {
        GameObject go = new GameObject("DmgNum");
        go.transform.SetParent(_worldCanvas.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 40f);
        go.transform.position = worldPos;
        // Scale canvas text to be legible in world space
        go.transform.localScale = Vector3.one * 0.012f;

        Text t = go.AddComponent<Text>();
        t.font      = _font;
        t.text      = text;
        t.fontSize  = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = color;

        Outline o = go.AddComponent<Outline>();
        o.effectColor    = new Color(0f, 0f, 0f, 0.9f);
        o.effectDistance = new Vector2(1.5f, -1.5f);

        StartCoroutine(AnimateNumber(go, t, worldPos, duration));
    }

    private static IEnumerator AnimateNumber(GameObject go, Text text,
                                             Vector3 startPos, float duration)
    {
        float elapsed = 0f;
        Vector3 drift = new Vector3(Random.Range(-0.3f, 0.3f), 1.8f, 0f);
        Color   startColor = text.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            go.transform.position = startPos + drift * t;

            // Fade out in last 40%
            float alpha = t > 0.6f ? Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f) : 1f;
            text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            // Face camera
            Camera cam = Camera.main;
            if (cam != null)
                go.transform.rotation = cam.transform.rotation;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(go);
    }
}
