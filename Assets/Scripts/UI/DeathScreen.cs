using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attach to a full-screen Canvas (Sort Order: 10).
/// Assign the panel, buttons, and optional text in the Inspector.
/// The Canvas starts disabled — PlayerHealth.Die() calls Show().
/// </summary>
public class DeathScreen : MonoBehaviour
{
    public static DeathScreen Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;          // the dark overlay + "You Died" text
    [SerializeField] private Button     respawnButton;  // "Try Again"
    [SerializeField] private Button     mainMenuButton; // "Main Menu"

    [Header("Settings")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    void Start()
    {
        if (respawnButton  != null) respawnButton.onClick.AddListener(Respawn);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);

        // Unlock cursor so the player can click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Time.timeScale = 0.3f;  // slow-mo while the screen fades in, feels cinematic
    }

    void Respawn()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
