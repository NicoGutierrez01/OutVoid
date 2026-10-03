using UnityEngine;

public class Magnet : MonoBehaviour
{
    [Header("Atracción")]
    [Tooltip("Velocidad inicial con la que cada recurso empieza a moverse hacia el jugador")]
    [SerializeField] private float velocidadAtraccion = 20f;
    [Tooltip("Cuánto aumenta la velocidad de cada recurso por segundo mientras es atraído")]
    [SerializeField] private float aceleracion = 40f;
    [Tooltip("Distancia a la que el recurso se pega al jugador para recogerse")]
    [SerializeField] private float distanciaRecogida = 1.5f;

    [Header("Duración")]
    // Campo nuevo (antes duracionMagneto = 3 en el prefab): toma el default 7.
    [Tooltip("Segundos que dura el efecto: todos los recursos del mapa van hacia el jugador")]
    [SerializeField] private float duracionEfecto = 7f;

    [Header("Efectos Visuales (Estilo San Andreas)")]
    public float velocidadGiro = 90f;

    private bool activo = false;
    private float tiempoRestante;

    // Referencias para ocultar el pickup mientras está activo
    private MeshRenderer[] renderers;
    private Collider col;

    private void Awake()
    {
        renderers = GetComponentsInChildren<MeshRenderer>();
        TryGetComponent(out col);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activo)
            return;

        PlayerCharacter pc = other.GetComponentInParent<PlayerCharacter>();
        if (pc == null && !other.CompareTag("Player"))
            return;

        Transform jugador = pc != null ? pc.transform : other.transform;

        activo = true;
        tiempoRestante = duracionEfecto;
        SetVisuales(false);

        // El movimiento lo hace cada ItemRecurso según el estado global (incluye los que aparezcan después).
        ItemRecurso.ActivarMagneto(jugador, duracionEfecto, velocidadAtraccion, aceleracion, distanciaRecogida);
    }

    private void Update()
    {
        if (!activo)
        {
            transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);
            return;
        }

        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante <= 0f)
            DesactivarYReubicarMagneto();
    }

    private void DesactivarYReubicarMagneto()
    {
        activo = false;

        // Obtener nueva posición desde MapManager
        if (MapManager.Instance != null)
        {
            transform.position = MapManager.Instance.ObtenerNuevaPosicionMagneto(transform.position);
        }

        // Volver a mostrarlo para que el jugador lo encuentre en su nuevo punto
        SetVisuales(true);
    }

    private void SetVisuales(bool visible)
    {
        if (col != null) col.enabled = visible;

        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}
