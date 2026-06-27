using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sends the player back to the main menu. Wired to the in-game MENU button's
/// onClick (baked by the "Add Main Menu Button" tool), and also fires on the
/// Escape key so you can bail during editor testing without precise clicking.
///
/// A normal gameplay component — not a runtime auto-apply hook.
/// </summary>
public class ReturnToMenu : MonoBehaviour
{
    [Tooltip("Scene to load. Must be in Build Settings.")]
    public string mainMenuScene = "MainMenu";

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;                                   // in case a panel paused time
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        SceneManager.LoadScene(mainMenuScene);
    }

    void Update()
    {
        // Editor/keyboard convenience — harmless on a cabinet (no Escape key).
        if (Input.GetKeyDown(KeyCode.Escape)) GoToMainMenu();
    }
}
