using UnityEngine;
using UnityEngine.InputSystem;

public class AltarDobleBoss : MonoBehaviour
{
    [Header("Detección")]
    [Tooltip("Distancia a la que el jugador puede interactuar")]
    public float radioInteraccion = 4f;

    [Header("Configuración Visual")]
    public GameObject textoInteractuar; 
    public ParticleSystem particulasActivo;

    private bool jugadorCerca = false;
    private bool yaActivado = false;
    private Transform camaraJugador;

    void Start()
    {
        if (textoInteractuar != null) textoInteractuar.SetActive(false);
        if (Camera.main != null) camaraJugador = Camera.main.transform;
    }

    void Update()
    {
        if (yaActivado) return;

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

        if (textoInteractuar != null)
        {
            if (textoInteractuar.activeSelf != jugadorCerca)
            {
                textoInteractuar.SetActive(jugadorCerca);
            }

            if (textoInteractuar.activeSelf && camaraJugador != null)
            {
                textoInteractuar.transform.LookAt(textoInteractuar.transform.position + camaraJugador.forward);
            }
        }

        if (jugadorCerca)
        {
            bool presionoF = (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) 
                             || Input.GetKeyDown(KeyCode.F);

            if (presionoF)
            {
                ActivarAltar();
            }
        }
    }

    void ActivarAltar()
    {
        yaActivado = true;

        if (MapManager.Instance != null)
        {
            MapManager.Instance.retoDobleBossActivo = true;
            Debug.Log("<color=green>[ALTAR ACTIVADO]</color> retoDobleBossActivo = TRUE. Aparecerán 2 bosses.");
        }
        else
        {
            Debug.LogError("[ALTAR ERROR] No se encontró MapManager.Instance");
        }

        if (textoInteractuar != null) textoInteractuar.SetActive(false);

        if (particulasActivo != null)
        {
            particulasActivo.transform.parent = null;
            particulasActivo.Play();
            Destroy(particulasActivo.gameObject, 3f);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radioInteraccion);
    }
}