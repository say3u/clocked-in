using UnityEngine;
using UnityEngine.EventSystems;

// Put on each menu button. Shows the ">>" marker when the button is selected,
// whether by keyboard navigation or by the mouse hovering over it.
public class MenuButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [SerializeField] GameObject marker; // child text object containing ">>"

    void Awake()
    {
        if (marker != null) marker.SetActive(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (marker != null) marker.SetActive(true);
        // TODO (audio): play hover/move sound here
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (marker != null) marker.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Hovering selects, so mouse and keyboard share one "selected" button
        EventSystem.current.SetSelectedGameObject(gameObject);
    }
}
