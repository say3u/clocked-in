using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Put on the root of GameOverCanvas (must stay active). panel is a child that starts hidden.
// MonsterFollow calls GameOverScreen.Show() when it catches the player.
public class GameOverScreen : MonoBehaviour
{
    [SerializeField] GameObject panel;          // the visible game-over UI (starts hidden)
    [SerializeField] CanvasGroup panelGroup;    // CanvasGroup on that same panel, used for the fade-in
    [SerializeField] GameObject firstSelected;  // the Retry button
    [SerializeField] AudioSource sting;         // optional: sound played when you are caught
    [SerializeField] float fadeTime = 0.6f;
    [SerializeField] string mainMenuSceneName = "MainMenu";

    static GameOverScreen instance;
    public static bool IsShown { get; private set; }

    void Awake()
    {
        instance = this;
        IsShown = false;
    }

    void Start()
    {
        panel.SetActive(false);
    }

    public static void Show()
    {
        if (instance == null)
        {
            Debug.LogWarning("GameOverScreen: there is no GameOverCanvas in this scene.");
            return;
        }
        if (IsShown) return; // the monster calls this every frame while touching you

        IsShown = true;
        instance.StartCoroutine(instance.ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        // Freeze the game and stop the player moving / looking
        Time.timeScale = 0f;
        foreach (var script in ScriptsToDisable())
        {
            if (script != null) script.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (sting != null) sting.Play();

        panel.SetActive(true);
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;

        // Fade in using unscaled time, because the game is frozen
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            panelGroup.alpha = t / fadeTime;
            yield return null;
        }

        panelGroup.alpha = 1f;
        panelGroup.interactable = true;
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    IEnumerable<MonoBehaviour> ScriptsToDisable()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        if (player != null) yield return player;

        var flashlight = FindFirstObjectByType<Flashlight>();
        if (flashlight != null) yield return flashlight;
    }
}
