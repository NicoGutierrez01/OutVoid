using UnityEngine;

public class ObjetoFlotanteAltar : MonoBehaviour
{
    [Header("Rotación")]
    public float velocidadGiro = 60f;
    public Vector3 ejeRotacion = Vector3.up;

    [Header("Flotación (Arriba / Abajo)")]
    [Tooltip("Distancia que sube y baja respecto a su posición original")]
    public float amplitudFlotacion = 0.15f;
    public float frecuenciaFlotacion = 2f;

    [Header("Pulso de Brillo (Emission)")]
    public bool usarPulsoBrillo = true;
    public float intensidadMinima = 0.8f;
    public float intensidadMaxima = 2.5f;
    public float velocidadBrillo = 2.5f;

    private Vector3 posicionLocalInicial;
    private Renderer objRenderer;
    private Material objMaterial;
    private Color colorEmisionBase;

    void Start()
    {
        // Se guarda la posición LOCAL para que funcione con cualquier posición o escala del altar
        posicionLocalInicial = transform.localPosition;

        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            objMaterial = objRenderer.material;
            if (objMaterial.HasProperty("_EmissionColor"))
            {
                colorEmisionBase = objMaterial.GetColor("_EmissionColor");
                if (colorEmisionBase == Color.black) colorEmisionBase = Color.white;
                objMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    void Update()
    {
        // Giro continuo sobre su propio eje local
        transform.Rotate(ejeRotacion, velocidadGiro * Time.deltaTime, Space.Self);

        // Movimiento senoidal vertical suave en espacio local
        float offsetVertical = Mathf.Sin(Time.time * frecuenciaFlotacion) * amplitudFlotacion;
        transform.localPosition = new Vector3(
            posicionLocalInicial.x,
            posicionLocalInicial.y + offsetVertical,
            posicionLocalInicial.z
        );

        // Pulso de intensidad lumínica
        if (usarPulsoBrillo && objMaterial != null && objMaterial.HasProperty("_EmissionColor"))
        {
            float t = (Mathf.Sin(Time.time * velocidadBrillo) + 1f) * 0.5f;
            float factor = Mathf.Lerp(intensidadMinima, intensidadMaxima, t);
            objMaterial.SetColor("_EmissionColor", colorEmisionBase * factor);
        }
    }

    private void OnDestroy()
    {
        if (objMaterial != null)
        {
            Destroy(objMaterial);
        }
    }
}