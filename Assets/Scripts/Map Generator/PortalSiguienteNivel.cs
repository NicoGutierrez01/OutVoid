using UnityEngine;
using UnityEngine.InputSystem;

public class PortalSiguienteNivel : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject canvasInteractuar;
    public bool requiereTeclaF = true;

    private bool jugadorEnZona = false;

    void Start()
    {
        if (canvasInteractuar != null) canvasInteractuar.SetActive(false);
    }

    void Update()
    {
        if (jugadorEnZona && requiereTeclaF)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                Viajar();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnZona = true;

            if (requiereTeclaF)
            {
                if (canvasInteractuar != null) canvasInteractuar.SetActive(true);
            }
            else
            {
                Viajar();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnZona = false;
            if (canvasInteractuar != null) canvasInteractuar.SetActive(false);
        }
    }

    void Viajar()
    {
        if (canvasInteractuar != null) canvasInteractuar.SetActive(false);

        if (MapManager.Instance != null)
        {
            MapManager.Instance.ColapsarMapa();
        }
    }
}