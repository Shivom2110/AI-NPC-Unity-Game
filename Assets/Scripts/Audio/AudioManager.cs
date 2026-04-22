using UnityEngine;

/// <summary>
/// Central audio manager. Drag your audio clips into the Inspector slots —
/// the manager hooks up all events automatically.
///
/// Add the AudioManager component to any persistent GameObject (e.g. GameInitializer).
/// It will survive scene loads.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Player Combat")]
    public AudioClip sfxLightAttack;
    public AudioClip sfxHeavyAttack;
    public AudioClip sfxFlashyAttack;
    public AudioClip sfxUltimate;

    [Header("Parry / Dodge")]
    public AudioClip sfxParryPerfect;
    public AudioClip sfxParryGood;
    public AudioClip sfxParryFail;
    public AudioClip sfxDodgeRoll;
    public AudioClip sfxOutOfStamina;

    [Header("Player Health")]
    public AudioClip sfxPlayerHit;
    public AudioClip sfxPlayerDeath;

    [Header("Boss")]
    public AudioClip sfxBossRoar;
    public AudioClip sfxBossAttackLight;
    public AudioClip sfxBossAttackHeavy;
    public AudioClip sfxBossHit;
    public AudioClip sfxBossDeath;
    public AudioClip sfxBossPhase2;
    public AudioClip sfxBossPhase3;
    public AudioClip sfxBossFinalPhase;

    [Header("UI / Progression")]
    public AudioClip sfxHeatModeActivate;
    public AudioClip sfxVictory;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume    = 1f;

    private AudioSource _source;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake  = false;
        _source.spatialBlend = 0f;   // 2D
    }

    private void OnEnable()
    {
        CombatEventBus.OnPlayerAttack       += OnPlayerAttack;
        CombatEventBus.OnBossAttackLanded   += OnBossHitPlayer;
        CombatEventBus.OnPlayerDied         += OnPlayerDied;
        CombatEventBus.OnBossPhaseChanged   += OnBossPhase;
        CombatEventBus.OnBossDied           += OnBossDied;
        CombatEventSystem.OnPlayerParry     += OnPlayerParry;
        CombatEventSystem.OnFightEnd        += OnFightEnd;
    }

    private void OnDisable()
    {
        CombatEventBus.OnPlayerAttack       -= OnPlayerAttack;
        CombatEventBus.OnBossAttackLanded   -= OnBossHitPlayer;
        CombatEventBus.OnPlayerDied         -= OnPlayerDied;
        CombatEventBus.OnBossPhaseChanged   -= OnBossPhase;
        CombatEventBus.OnBossDied           -= OnBossDied;
        CombatEventSystem.OnPlayerParry     -= OnPlayerParry;
        CombatEventSystem.OnFightEnd        -= OnFightEnd;
    }

    // ── Event handlers ─────────────────────────────────────────────────────────

    private void OnPlayerAttack(PlayerAttackType type, bool hitBoss, float dmg)
    {
        switch (type)
        {
            case PlayerAttackType.AutoAttack: Play(sfxLightAttack); break;
            case PlayerAttackType.Attack2:    Play(sfxHeavyAttack); break;
            case PlayerAttackType.Attack3:    Play(sfxFlashyAttack); break;
            case PlayerAttackType.Ultimate:   Play(sfxUltimate); break;
        }

        if (hitBoss) Play(sfxBossHit);
    }

    private void OnPlayerParry(bool success, float timingMs)
    {
        if (success)
            Play(timingMs < 200f ? sfxParryPerfect : sfxParryGood);
        else
            Play(sfxParryFail);
    }

    private void OnBossHitPlayer(float dmg)  => Play(sfxPlayerHit);
    private void OnPlayerDied()               => Play(sfxPlayerDeath);
    private void OnBossDied()                 => Play(sfxBossDeath);

    private void OnBossPhase(int phase)
    {
        switch (phase)
        {
            case 2: Play(sfxBossPhase2);      break;
            case 3: Play(sfxBossPhase3);      break;
            case 4: Play(sfxBossFinalPhase);  break;
        }
    }

    private void OnFightEnd(bool playerWon, float dur, float skill)
    {
        if (playerWon) Play(sfxVictory);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _source == null) return;
        _source.PlayOneShot(clip, masterVolume * sfxVolume * volumeScale);
    }

    /// <summary>Call this directly for sounds not driven by events (e.g. UI clicks).</summary>
    public static void PlayClip(AudioClip clip, float volume = 1f)
        => Instance?.Play(clip, volume);
}
