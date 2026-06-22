using UnityEngine;
using UnityEngine.EventSystems;

public class AutoSelectOnEnable : MonoBehaviour
{
    public GameObject defaultSelection;

    private void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(defaultSelection);
    }
}