using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Reusable overlay panel (Instructions, Settings, ...). Works in the main menu and the pause menu.
// Open() remembers which button opened it and Close() gives the selection back to that button.
public class MenuPanel : MonoBehaviour
{
    [SerializeField] GameObject firstSelected;   // the panel's Back button
    [SerializeField] CanvasGroup menuToDisable;  // CanvasGroup on the buttons behind the panel (optional)

    GameObject opener;

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
