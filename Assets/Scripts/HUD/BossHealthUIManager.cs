using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthUIManager : MonoBehaviour
{
    public static BossHealthUIManager Instance;

    [Header("Barra para 1 solo Boss (Centro)")]
    public Slider sliderCentro;

    [Header("Barras para 2 Bosses (Lado a Lado)")]
    public Slider sliderIzquierda;
    public Slider sliderDerecha;

    private TextMeshProUGUI textoCentro;
    private TextMeshProUGUI textoIzq;
    private TextMeshProUGUI textoDer;

    private void Awake()
    {
        Instance = this;

        if (sliderCentro != null)
        {
            textoCentro = sliderCentro.GetComponentInChildren<TextMeshProUGUI>();
            sliderCentro.gameObject.SetActive(false);
        }
        if (sliderIzquierda != null)
        {
            textoIzq = sliderIzquierda.GetComponentInChildren<TextMeshProUGUI>();
            sliderIzquierda.gameObject.SetActive(false);
        }
        if (sliderDerecha != null)
        {
            textoDer = sliderDerecha.GetComponentInChildren<TextMeshProUGUI>();
            sliderDerecha.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Activa la barra correspondiente según si es reto doble o boss individual
    /// </summary>
    public void RegistrarBoss(float vidaMaxima, out int slotAsignado)
    {
        bool esDoble = (MapManager.Instance != null && MapManager.Instance.retoDobleBossActivo);

        if (!esDoble)
        {
            slotAsignado = 0;
            if (sliderCentro != null)
            {
                sliderCentro.maxValue = vidaMaxima;
                sliderCentro.value = vidaMaxima;
                sliderCentro.gameObject.SetActive(true);
                ActualizarTexto(textoCentro, vidaMaxima, vidaMaxima);
            }
        }
        else
        {
            if (sliderIzquierda != null && !sliderIzquierda.gameObject.activeSelf)
            {
                slotAsignado = 1;
                sliderIzquierda.maxValue = vidaMaxima;
                sliderIzquierda.value = vidaMaxima;
                sliderIzquierda.gameObject.SetActive(true);
                ActualizarTexto(textoIzq, vidaMaxima, vidaMaxima);
            }
            else
            {
                slotAsignado = 2;
                if (sliderDerecha != null)
                {
                    sliderDerecha.maxValue = vidaMaxima;
                    sliderDerecha.value = vidaMaxima;
                    sliderDerecha.gameObject.SetActive(true);
                    ActualizarTexto(textoDer, vidaMaxima, vidaMaxima);
                }
            }
        }
    }

    public void ActualizarVidaBoss(int slot, float vidaActual, float vidaMaxima)
    {
        if (slot == 0 && sliderCentro != null)
        {
            sliderCentro.value = vidaActual;
            ActualizarTexto(textoCentro, vidaActual, vidaMaxima);
        }
        else if (slot == 1 && sliderIzquierda != null)
        {
            sliderIzquierda.value = vidaActual;
            ActualizarTexto(textoIzq, vidaActual, vidaMaxima);
        }
        else if (slot == 2 && sliderDerecha != null)
        {
            sliderDerecha.value = vidaActual;
            ActualizarTexto(textoDer, vidaActual, vidaMaxima);
        }
    }

    public void DesactivarBarraBoss(int slot)
    {
        if (slot == 0 && sliderCentro != null) sliderCentro.gameObject.SetActive(false);
        if (slot == 1 && sliderIzquierda != null) sliderIzquierda.gameObject.SetActive(false);
        if (slot == 2 && sliderDerecha != null) sliderDerecha.gameObject.SetActive(false);
    }

    private void ActualizarTexto(TextMeshProUGUI texto, float vidaActual, float vidaMax)
    {
        if (texto != null)
        {
            int actual = Mathf.Max(0, Mathf.CeilToInt(vidaActual));
            int max = Mathf.CeilToInt(vidaMax);
            texto.text = $"{actual} / {max}";
        }
    }
}