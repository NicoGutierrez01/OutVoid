using UnityEngine;
using UnityEngine.InputSystem;

public class PortalSiguienteNivel : MonoBehaviour
{
    [Header("Detección de Proximidad")]
    [Tooltip("Distancia en metros para poder interactuar con el portal")]
    public float radioInteraccion = 4f;

    [Header("UI Flotante")]
    [Tooltip("Canvas o texto hijo que dice '[F] Entrar al Portal'")]
    public GameObject canvasInteractuar;

    private Transform camaraJugador;
    private bool jugadorCerca = false;
    private bool yaViajo = false;

    void Start()
    {
        if (canvasInteractuar != null) canvasInteractuar.SetActive(false);
        if (Camera.main != null) camaraJugador = Camera.main.transform;
    }

    void Update()
    {
        if (yaViajo) return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, radioInteraccion);
        bool detectado = false;

        foreach (var hit in colliders)
        {
            if (hit.CompareTag("Player"))
            {
                detectado = true;
                break;
            }
        }

        jugadorCerca = detectado;

        if (canvasInteractuar != null)
        {
            if (canvasInteractuar.activeSelf != jugadorCerca)
            {
                canvasInteractuar.SetActive(jugadorCerca);
            }

            if (canvasInteractuar.activeSelf && camaraJugador != null)
            {
                canvasInteractuar.transform.LookAt(canvasInteractuar.transform.position + camaraJugador.forward);
            }
        }

        if (jugadorCerca)
        {
            bool presionoF = (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                             || Input.GetKeyDown(KeyCode.F);

            if (presionoF)
            {
                Viajar();
            }
        }
    }

    void Viajar()
    {
        yaViajo = true;

        if (canvasInteractuar != null) canvasInteractuar.SetActive(false);

        if (MapManager.Instance != null)
        {
            Debug.Log("<color=cyan>[PORTAL]</color> Jugador interactuó con el portal. Colapsando mapa...");
            MapManager.Instance.ColapsarMapa();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioInteraccion);
    }
}