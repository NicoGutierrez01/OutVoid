using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections; // Necesario para las corrutinas

public class InteraccionLapida : MonoBehaviour
{
    public float radioInteraccion = 3f;
    private bool jugadorCerca = false;
    private bool activada = false;

    [Header("UI Flotante")]
    public GameObject canvasFlotante; 
    private Transform camaraJugador; 

    [Header("Configuración Camera Shake")]
    public float duracionSacudida = 2f;
    public float intensidadSacudida = 0.2f;

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

        // Disparamos el terremoto en la cámara
        StartCoroutine(RutinaCameraShake());

        MapManager.Instance.SpawnearPortalBoss();

        // Aumenté ligeramente el tiempo de destrucción para que coincida con el shake si lo deseas, 
        // o se destruye la lápida mientras la cámara vibra.
        Destroy(gameObject, duracionSacudida); 
    }

   System.Collections.IEnumerator RutinaCameraShake()
{
    if (camaraJugador == null) yield break;

    float tiempoTranscurrido = 0f;

    while (tiempoTranscurrido < duracionSacudida)
    {
        // Generamos un desplazamiento aleatorio pequeño en X e Y
        float offsetX = Random.Range(-1f, 1f) * intensidadSacudida;
        float offsetY = Random.Range(-1f, 1f) * intensidadSacudida;

        // Movemos la cámara sumándole el temblor a su posición local actual (respetando el movimiento del jugador)
        camaraJugador.localPosition += new Vector3(offsetX, offsetY, 0f);

        tiempoTranscurrido += Time.deltaTime;
        yield return null;

        // Al terminar el frame, revertimos inmediatamente ese desplazamiento aleatorio 
        // para que la lógica de la cámara en primera persona del jugador no pierda su referencia real.
        camaraJugador.localPosition -= new Vector3(offsetX, offsetY, 0f);
    }
}
}