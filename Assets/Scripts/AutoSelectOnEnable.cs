using UnityEngine;
using UnityEngine.EventSystems;

public class AutoSelectOnEnable : MonoBehaviour
{
    public GameObject defaultSelection;

    private void OnEnable()
    {
        if (EventSystem.current == null) return; // el EventSystem puede habilitarse después al cargar la escena
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(defaultSelection);
    }
}