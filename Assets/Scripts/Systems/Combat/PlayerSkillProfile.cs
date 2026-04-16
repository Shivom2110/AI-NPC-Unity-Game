using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Persistent skill profile shared across fights.
/// </summary>
[CreateAssetMenu(fileName = "PlayerSkillProfile", menuName = "Combat/Player Skill Profile")]
public class PlayerSkillProfile : ScriptableObject
{
    [Header("Runtime Difficulty Source")]
    [SerializeField] private DifficultySettingsAsset difficultySettingsAsset;

    [Header("Persistent Stats")]
    [Range(0f, 100f)] public float currentSkillScore = 50f;
    public int sessionCount;
    public float averageReactionTime = 350f;
    [Range(0f, 1f)] public float parrySuccessRate = 0.5f;
    [Range(0f, 1f)] public float dodgeSuccessRate = 0.5f;
    public string favoriteCombo = "None";
    public float comboVarietyScore = 0.5f;
    public float damageDealtAverage = 0f;
    public float damageTakenAverage = 0f;
    public float fightDuration = 0f;
    public float skillTrend = 0f;
    public float difficultyMultiplier = 1f;

    private const string SaveFileName = "adaptive_combat_profile.json";

    /// <summary>
    /// Creates a runtime profile and loads persisted data when available.
    /// </summary>
    public static PlayerSkillProfile CreateRuntimeProfile()
    {
        PlayerSkillProfile profile = CreateInstance<PlayerSkillProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        profile.LoadProfile();
        return profile;
    }

    /// <summary>
    /// Updates the profile from fight-end combat data.
    /// </summary>
    public void UpdateProfile(CombatData data)
    {
        float previousScore = currentSkillScore;
        sessionCount = Mathf.Max(1, sessionCount + 1);

        averageReactionTime = RunningAverage(averageReactionTime, data.averageReactionTime, sessionCount);
        parrySuccessRate = RunningAverage(parrySuccessRate, data.parrySuccessRate, sessionCount);
        dodgeSuccessRate = RunningAverage(dodgeSuccessRate, data.dodgeSuccessRate, sessionCount);
        comboVarietyScore = RunningAverage(comboVarietyScore, GetComboVariety(data), sessionCount);
        damageDealtAverage = RunningAverage(damageDealtAverage, data.damageDealtTotal, sessionCount);
        damageTakenAverage = RunningAverage(damageTakenAverage, data.damageTakenTotal, sessionCount);
        fightDuration = RunningAverage(fightDuration, data.fightDuration, sessionCount);

        currentSkillScore = Mathf.Clamp(RunningAverage(previousScore, data.finalSkillScore, sessionCount), 0f, 100f);
        skillTrend = data.finalSkillScore - previousScore;
        difficultyMultiplier = Mathf.Clamp(
            Mathf.Lerp(difficultyMultiplier, 1f + (skillTrend / 250f), 0.5f),
            0.85f,
            1.25f);

        favoriteCombo = FindFavoriteCombo(data.comboHistory);
        SaveProfile();
    }

    /// <summary>
    /// Returns a live difficulty recommendation using the current profile.
    /// </summary>
    public DifficultySettings GetDifficultyRecommendation()
    {
        DifficultySettingsAsset source = difficultySettingsAsset != null
            ? difficultySettingsAsset
            : DifficultySettingsAsset.CreateRuntimeDefault();

        return DifficultyEngine.EvaluateProfile(this, source);
    }

    /// <summary>
    /// Saves the profile to persistent storage.
    /// </summary>
    public void SaveProfile()
    {
        ProfileSaveData saveData = new ProfileSaveData
        {
            currentSkillScore = currentSkillScore,
            sessionCount = sessionCount,
            averageReactionTime = averageReactionTime,
            parrySuccessRate = parrySuccessRate,
            dodgeSuccessRate = dodgeSuccessRate,
            favoriteCombo = favoriteCombo,
            comboVarietyScore = comboVarietyScore,
            damageDealtAverage = damageDealtAverage,
            damageTakenAverage = damageTakenAverage,
            fightDuration = fightDuration,
            skillTrend = skillTrend,
            difficultyMultiplier = difficultyMultiplier
        };

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(GetSavePath(), json);
    }

    /// <summary>
    /// Loads the profile from persistent storage.
    /// </summary>
    public void LoadProfile()
    {
        string savePath = GetSavePath();
        if (!File.Exists(savePath))
            return;

        ProfileSaveData saveData = JsonUtility.FromJson<ProfileSaveData>(File.ReadAllText(savePath));
        currentSkillScore = saveData.currentSkillScore;
        sessionCount = saveData.sessionCount;
        averageReactionTime = saveData.averageReactionTime;
        parrySuccessRate = saveData.parrySuccessRate;
        dodgeSuccessRate = saveData.dodgeSuccessRate;
        favoriteCombo = saveData.favoriteCombo;
        comboVarietyScore = saveData.comboVarietyScore;
        damageDealtAverage = saveData.damageDealtAverage;
        damageTakenAverage = saveData.damageTakenAverage;
        fightDuration = saveData.fightDuration;
        skillTrend = saveData.skillTrend;
        difficultyMultiplier = Mathf.Clamp(saveData.difficultyMultiplier, 0.85f, 1.25f);
    }

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    private float RunningAverage(float currentValue, float newValue, int count)
    {
        if (count <= 1)
            return newValue;

        return ((currentValue * (count - 1)) + newValue) / count;
    }

    private float GetComboVariety(CombatData data)
    {
        if (data.comboHistory == null || data.comboHistory.Count == 0)
            return 0f;

        HashSet<string> unique = new HashSet<string>(data.comboHistory);
        return Mathf.Clamp01((float)unique.Count / Mathf.Max(1, data.comboHistory.Count));
    }

    private string FindFavoriteCombo(List<string> comboHistory)
    {
        if (comboHistory == null || comboHistory.Count == 0)
            return "None";

        Dictionary<string, int> counts = new Dictionary<string, int>();
        string bestCombo = "None";
        int bestCount = 0;

        for (int i = 0; i < comboHistory.Count; i++)
        {
            string combo = string.IsNullOrWhiteSpace(comboHistory[i]) ? "None" : comboHistory[i];
            counts.TryGetValue(combo, out int existing);
            existing++;
            counts[combo] = existing;

            if (existing > bestCount)
            {
                bestCount = existing;
                bestCombo = combo;
            }
        }

        return bestCombo;
    }

    [Serializable]
    private struct ProfileSaveData
    {
        public float currentSkillScore;
        public int sessionCount;
        public float averageReactionTime;
        public float parrySuccessRate;
        public float dodgeSuccessRate;
        public string favoriteCombo;
        public float comboVarietyScore;
        public float damageDealtAverage;
        public float damageTakenAverage;
        public float fightDuration;
        public float skillTrend;
        public float difficultyMultiplier;
    }
}
