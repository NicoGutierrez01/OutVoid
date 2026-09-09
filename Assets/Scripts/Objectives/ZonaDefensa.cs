using System.Collections;
using UnityEngine;

public class ZonaDefensa : MonoBehaviour
{
    public static bool jugadorEnZona = false;

    [Header("Efecto Visual (Point Light)")]
    [SerializeField] private Light luzZona;
    [SerializeField] private float intensidadLuzActiva = 4.0f;

    [Header("Efecto Visual (Emisión Material)")]
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private int indiceMaterial = 0;
    [ColorUsage(true, true)]
    [SerializeField] private Color colorEmisionActivo = new Color(0f, 0.8f, 1f) * 2.5f;

    [Header("Suavizado")]
    [SerializeField] private float velocidadTransicion = 5.0f;

    private Color colorEmisionApagado = Color.black;
    private MaterialPropertyBlock propBlock;
    private Coroutine rutinaVisual;

    private void Awake()
    {
        jugadorEnZona = false;
        propBlock = new MaterialPropertyBlock();

        if (luzZona != null)
        {
            luzZona.intensity = 0f;
        }

        AplicarEmision(colorEmisionApagado);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (EsJugador(other))
        {
            jugadorEnZona = true;
            IniciarTransicion(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (EsJugador(other))
        {
            jugadorEnZona = false;
            IniciarTransicion(false);
        }
    }

    private bool EsJugador(Collider col)
    {
        return col.CompareTag("Player") || (col.transform.root != null && col.transform.root.CompareTag("Player"));
    }

    private void IniciarTransicion(bool encender)
    {
        if (rutinaVisual != null) StopCoroutine(rutinaVisual);
        rutinaVisual = StartCoroutine(TransicionEfectos(encender));
    }

    private IEnumerator TransicionEfectos(bool encender)
    {
        float duracion = 0.4f;
        float elapsed = 0f;

        float startIntensity = luzZona != null ? luzZona.intensity : 0f;
        float targetIntensity = encender ? intensidadLuzActiva : 0f;

        Color startColor = encender ? colorEmisionApagado : colorEmisionActivo;
        Color targetColor = encender ? colorEmisionActivo : colorEmisionApagado;

        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duracion;

            if (luzZona != null)
            {
                luzZona.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            }

            Color c = Color.Lerp(startColor, targetColor, t);
            AplicarEmision(c);

            yield return null;
        }

        if (luzZona != null) luzZona.intensity = targetIntensity;
        AplicarEmision(targetColor);
    }

    private void AplicarEmision(Color col)
    {
        if (meshRenderer != null)
        {
            meshRenderer.GetPropertyBlock(propBlock, indiceMaterial);
            propBlock.SetColor("_EmissionColor", col);
            meshRenderer.SetPropertyBlock(propBlock, indiceMaterial);
        }
    }

    private void OnDisable()
    {
        jugadorEnZona = false;
        if (luzZona != null) luzZona.intensity = 0f;
        AplicarEmision(colorEmisionApagado);
    }

    private void OnDestroy()
    {
        jugadorEnZona = false;
    }
}