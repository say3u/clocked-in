using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Put on the root of PauseCanvas (must stay active). pausePanel is a child that starts hidden.
// Uses the Escape key directly, like PlayerMovement does, so no input-map change is needed yet.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;       // the visible pause UI
    [SerializeField] GameObject firstSelected;    // the Resume button
    [SerializeField] MonoBehaviour[] extraScriptsToDisable; // any other scripts that read input (interact script later, etc.)
    [SerializeField] string mainMenuSceneName = "MainMenu";

    public static bool IsPaused { get; private set; }

    readonly List<MonoBehaviour> disabledByPause = new List<MonoBehaviour>();

    void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        pausePanel.SetActive(false);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        // No pausing on the game-over screen
        if (GameOverScreen.IsShown) return;

        // If Instructions/Settings is open (or was just closed this frame), Escape belongs to that panel
        if (MenuPanel.OpenCount > 0 || MenuPanel.LastClosedFrame == Time.frameCount) return;

        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true; // UI sounds later: tick "Ignore Listener Pause" on their AudioSources

        // Stop the player from looking around / using the flashlight while paused
        disabledByPause.Clear();
        foreach (var script in ScriptsToDisable())
        {
            if (script != null && script.enabled)
            {
                script.enabled = false;
                disabledByPause.Add(script);
            }
        }

        pausePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void Resume()
    {
        IsPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;

        foreach (var script in disabledByPause)
        {
            if (script != null) script.enabled = true;
        }
        disabledByPause.Clear();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GoToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerable<MonoBehaviour> ScriptsToDisable()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        if (player != null) yield return player;

        var flashlight = FindFirstObjectByType<Flashlight>();
        if (flashlight != null) yield return flashlight;

        foreach (var script in extraScriptsToDisable) yield return script;
    }
}