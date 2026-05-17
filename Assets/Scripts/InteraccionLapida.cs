using UnityEngine;
using UnityEngine.InputSystem; 

public class InteraccionLapida : MonoBehaviour
{
    public float radioInteraccion = 3f;
    private bool jugadorCerca = false;
    private bool activada = false;

    [Header("UI Flotante")]
    public GameObject canvasFlotante; 
    private Transform camaraJugador; 

    void Start()
    {
        if (canvasFlotante != null) canvasFlotante.SetActive(false);
        if (Camera.main != null) camaraJugador = Camera.main.transform;
    }

    void Update()
    {
        if (activada) return;

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

        if (canvasFlotante != null)
        {
            if (canvasFlotante.activeSelf != jugadorCerca)
            {
                canvasFlotante.SetActive(jugadorCerca);
            }

            if (canvasFlotante.activeSelf && camaraJugador != null)
            {
                canvasFlotante.transform.LookAt(canvasFlotante.transform.position + camaraJugador.forward);
            }
        }

        if (jugadorCerca && Keyboard.current.fKey.wasPressedThisFrame)
        {
            ActivarPortalBoss();
        }
    }

    void ActivarPortalBoss()
    {
        activada = true;

        if (canvasFlotante != null) canvasFlotante.SetActive(false);

        MapManager.Instance.SpawnearPortalBoss();

        Destroy(gameObject, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioInteraccion);
    }
}