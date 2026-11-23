using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Reusable overlay panel (Instructions, Settings, ...). Works in the main menu and the pause menu.
// Open() remembers which button opened it and Close() gives the selection back to that button.
// Panels must start hidden (unchecked in the Inspector).
public class MenuPanel : MonoBehaviour
{
    // Lets other scripts (PauseMenu) know a panel is open, so Escape closes the panel first
    public static int OpenCount { get; private set; }
    public static int LastClosedFrame { get; private set; } = -1;

    [SerializeField] GameObject firstSelected;   // the panel's first button or slider
    [SerializeField] CanvasGroup menuToDisable;  // CanvasGroup on the buttons behind the panel (optional)

    GameObject opener;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        OpenCount = 0;
        LastClosedFrame = -1;
    }

    void OnEnable()
    {
        OpenCount++;
    }

    void OnDisable()
    {
        OpenCount = Mathf.Max(0, OpenCount - 1);
        LastClosedFrame = Time.frameCount;
    }

    public void Open()
    {
        opener = EventSystem.current.currentSelectedGameObject;

        // Stop keyboard navigation from reaching the buttons behind the panel
        if (menuToDisable != null) menuToDisable.interactable = false;

        gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void Close()
    {
        gameObject.SetActive(false);

        if (menuToDisable != null) menuToDisable.interactable = true;
        if (opener != null) EventSystem.current.SetSelectedGameObject(opener);
    }

    void Update()
    {
        // Escape closes the panel (only runs while the panel is active)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }
}