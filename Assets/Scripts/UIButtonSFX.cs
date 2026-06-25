using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSFX : MonoBehaviour,
    IPointerClickHandler,
    ISelectHandler
{
    private static bool menuInitialized = false;
    private static GameObject lastSelected;

    private void Start()
    {
        Invoke(nameof(EnableSelectionSounds), 0.2f);
    }

    void EnableSelectionSounds()
    {
        menuInitialized = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        MusicManager.Instance?.PlayUIClick();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!menuInitialized)
            return;

        if (lastSelected == gameObject)
            return;

        lastSelected = gameObject;

        MusicManager.Instance?.PlayUIClick();
    }
}