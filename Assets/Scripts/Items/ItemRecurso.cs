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

    private Vector3 posicionBase;
    private Renderer itemRenderer;
    private Material itemMaterial;
    private Color colorEmisionBase;

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
                if (colorEmisionBase == Color.black) colorEmisionBase = Color.white;
                itemMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);

        float oscilacionPositiva = (Mathf.Sin(Time.time * frecuenciaFlotacion) + 1f) * 0.5f;
        float nuevoY = posicionBase.y + (oscilacionPositiva * amplitudFlotacion);
        
        transform.position = new Vector3(posicionBase.x, nuevoY, posicionBase.z);

        if (usarPulsoBrillo && itemMaterial != null && itemMaterial.HasProperty("_EmissionColor"))
        {
            float t = (Mathf.Sin(Time.time * velocidadBrillo) + 1f) * 0.5f;
            float factor = Mathf.Lerp(intensidadMinima, intensidadMaxima, t);
            itemMaterial.SetColor("_EmissionColor", colorEmisionBase * factor);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AplicarRecurso(other.gameObject);
            Destroy(gameObject);
        }
    }

    void AplicarRecurso(GameObject player)
    {
        var stats = player.GetComponent<PlayerStats>();
        var weapon = player.GetComponent<WeaponSystem>();
        
        CrosshairFeedbackManager crosshair = FindAnyObjectByType<CrosshairFeedbackManager>();

        switch (tipo)
        {
            case TipoRecurso.Vida:
                if (stats != null) stats.Heal(cantidadVida);
                if (crosshair != null) crosshair.ShowReward(CrosshairFeedbackManager.RewardType.Health);
                break;
                
            case TipoRecurso.Escudo:
                if (stats != null) stats.currentShield += cantidadEscudo;
                if (crosshair != null) crosshair.ShowReward(CrosshairFeedbackManager.RewardType.Shield);
                break;
                
            case TipoRecurso.Balas:
                if (weapon != null) weapon.AddAmmo(cantidadBalas);
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