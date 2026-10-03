using System.Collections.Generic;
using UnityEngine;

public class ItemRecurso : MonoBehaviour
{
    public enum TipoRecurso { Vida, Escudo, Balas }
    public TipoRecurso tipo;

    [Header("Cantidades Base")]
    public float cantidadVida = 25f;
    public float cantidadEscudo = 20f;
    public int cantidadBalas = 12;

    [Header("Efectos Visuales (Estilo San Andreas)")]
    public float velocidadGiro = 90f;

    [Tooltip("Distancia extra que se eleva desde el suelo para que nunca lo toque")]
    public float alturaMinimaSuelo = 0.35f;

    [Tooltip("Cuánto sube y baja flotando por encima de la altura mínima")]
    public float amplitudFlotacion = 0.2f;

    public float frecuenciaFlotacion = 2f;

    [Header("Brillo (Emission)")]
    public bool usarPulsoBrillo = true;
    public float intensidadMinima = 0.5f;
    public float intensidadMaxima = 2.0f;
    public float velocidadBrillo = 3f;

    [Header("Límite de drops")]
    [Tooltip("Máximo de recursos (vida+escudo+balas) activos a la vez. Al superarlo se elimina el más antiguo.")]
    [SerializeField] private int maxDropsActivos = 20;

    // Registro compartido de recursos activos, ordenado del más antiguo al más nuevo.
    private static readonly List<ItemRecurso> activos = new List<ItemRecurso>();
    public static IReadOnlyList<ItemRecurso> Activos => activos;

    // Estado global del Magnet: un solo temporizador compartido (dos Magnets no duplican la atracción).
    private static Transform objetivoMagneto;
    private static float finMagneto;
    private static float velocidadInicialMagneto;
    private static float aceleracionMagneto;
    private static float distanciaRecogidaMagneto;

    private static bool MagnetoActivo => objetivoMagneto != null && Time.time < finMagneto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearRegistro()
    {
        activos.Clear();
        objetivoMagneto = null;
        finMagneto = 0f;
    }

    // Todos los recursos activos (y los que aparezcan mientras dure) van hacia el jugador, sin importar la distancia.
    public static void ActivarMagneto(Transform jugador, float duracion, float velocidadInicial, float aceleracion, float distanciaRecogida)
    {
        objetivoMagneto = jugador;
        finMagneto = Mathf.Max(finMagneto, Time.time + duracion);
        velocidadInicialMagneto = velocidadInicial;
        aceleracionMagneto = aceleracion;
        distanciaRecogidaMagneto = distanciaRecogida;
    }

    private Vector3 posicionBase;
    private Renderer itemRenderer;
    private Material itemMaterial;
    private Color colorEmisionBase;

    private bool siendoAtraido = false;
    private float velocidadAtraccion;
    private bool recogido = false;

    void Awake()
    {
        // Estos recursos ya no desaparecen por tiempo: el límite de drops activos los controla.
        if (TryGetComponent(out ItemDespawnTemporal despawn))
            despawn.enabled = false;
    }

    void OnEnable()
    {
        activos.Add(this);

        while (activos.Count > Mathf.Max(1, maxDropsActivos))
        {
            ItemRecurso masAntiguo = activos[0];
            activos.RemoveAt(0);

            if (masAntiguo != null)
            {
                masAntiguo.gameObject.SetActive(false);
                Destroy(masAntiguo.gameObject);
            }
        }
    }

    void OnDisable()
    {
        activos.Remove(this);
    }

    void Start()
    {
        posicionBase = transform.position + Vector3.up * alturaMinimaSuelo;
        transform.position = posicionBase;

        itemRenderer = GetComponentInChildren<Renderer>();

        if (itemRenderer != null)
        {
            itemMaterial = itemRenderer.material;

            if (itemMaterial.HasProperty("_EmissionColor"))
            {
                colorEmisionBase = itemMaterial.GetColor("_EmissionColor");

                if (colorEmisionBase == Color.black)
                    colorEmisionBase = Color.white;

                itemMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    void Update()
    {
        transform.Rotate(
            Vector3.up,
            velocidadGiro * Time.deltaTime,
            Space.World
        );

        float oscilacionPositiva =
            (Mathf.Sin(Time.time * frecuenciaFlotacion) + 1f) * 0.5f;

        if (MagnetoActivo)
        {
            // Mientras el Magnet está activo no se aplica la flotación (evita que peleen).
            if (!siendoAtraido)
            {
                siendoAtraido = true;
                velocidadAtraccion = velocidadInicialMagneto;
            }

            velocidadAtraccion += aceleracionMagneto * Time.deltaTime;

            Vector3 destino = objetivoMagneto.position;
            if ((destino - transform.position).sqrMagnitude <= distanciaRecogidaMagneto * distanciaRecogidaMagneto)
                transform.position = destino;
            else
                transform.position = Vector3.MoveTowards(transform.position, destino, velocidadAtraccion * Time.deltaTime);
        }
        else
        {
            if (siendoAtraido)
            {
                // Terminó el Magnet: vuelve a flotar donde quedó, sin saltos.
                siendoAtraido = false;
                posicionBase = transform.position - Vector3.up * (oscilacionPositiva * amplitudFlotacion);
            }

            float nuevoY =
                posicionBase.y +
                (oscilacionPositiva * amplitudFlotacion);

            transform.position = new Vector3(
                posicionBase.x,
                nuevoY,
                posicionBase.z
            );
        }

        if (
            usarPulsoBrillo &&
            itemMaterial != null &&
            itemMaterial.HasProperty("_EmissionColor")
        )
        {
            float t =
                (Mathf.Sin(Time.time * velocidadBrillo) + 1f) * 0.5f;

            float factor =
                Mathf.Lerp(
                    intensidadMinima,
                    intensidadMaxima,
                    t
                );

            itemMaterial.SetColor(
                "_EmissionColor",
                colorEmisionBase * factor
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (recogido)
            return;

        if (other.CompareTag("Player"))
        {
            recogido = true;
            AplicarRecurso(other.gameObject);
            Destroy(gameObject);
        }
    }

    void AplicarRecurso(GameObject player)
    {
        var stats = player.GetComponent<PlayerStats>();
        var weapon = player.GetComponent<WeaponSystem>();

        CrosshairFeedbackManager crosshair =
            FindAnyObjectByType<CrosshairFeedbackManager>();

        switch (tipo)
        {
            case TipoRecurso.Vida:

                if (stats != null)
                    stats.Heal(cantidadVida);

                if (crosshair != null)
                    crosshair.ShowReward(
                        CrosshairFeedbackManager.RewardType.Health
                    );

                break;

            case TipoRecurso.Escudo:

                if (stats != null)
                    stats.currentShield += cantidadEscudo;

                if (crosshair != null)
                    crosshair.ShowReward(
                        CrosshairFeedbackManager.RewardType.Shield
                    );

                break;

            case TipoRecurso.Balas:

                if (weapon != null)
                    weapon.AddAmmo(cantidadBalas);

                break;
        }
    }

    private void OnDestroy()
    {
        if (itemMaterial != null)
        {
            Destroy(itemMaterial);
        }
    }
}