using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] string gameSceneName = "SampleScene"; // must match the team's scene name exactly
    [SerializeField] GameObject firstSelected;             // drag the Start Game button here
    [SerializeField] GameObject settingsPanel;             // optional, leave empty for now

    GameObject lastSelected;

    void Start()
    {
        // In case we arrive from a paused game that locked the cursor / froze time
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    void Update()
    {
        // Clicking empty space clears the selection, which breaks keyboard navigation.
        // If that happens, any key press restores the last selected button.
        var es = EventSystem.current;
        if (es.currentSelectedGameObject != null)
        {
            lastSelected = es.currentSelectedGameObject;
        }
        else if (lastSelected != null && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            es.SetSelectedGameObject(lastSelected);
        }
    }

    public void StartGame()
    {
        // TODO (audio): click sound / stop menu music
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        else Debug.Log("Settings not built yet.");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
