using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FirstSelectedButton : MonoBehaviour
{
    public Button firstButton;

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }
}