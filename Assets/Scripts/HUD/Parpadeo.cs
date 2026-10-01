using UnityEngine;
using UnityEngine.UI;

// Varía alfa y escala de un Graphic con ruido Perlin: titileo de fuego (velocidad alta) o pulso de halo (baja).
public class Parpadeo : MonoBehaviour
{
    [SerializeField] private Graphic grafico;
    [SerializeField] private float alfaMinimo = 0.35f;
    [SerializeField] private float alfaMaximo = 0.6f;
    [SerializeField] private float variacionEscala = 0.06f;
    [SerializeField] private float velocidad = 6f;

    private float semilla;

    public void Configurar(Graphic grafico, float alfaMinimo, float alfaMaximo, float variacionEscala, float velocidad)
    {
        this.grafico = grafico;
        this.alfaMinimo = alfaMinimo;
        this.alfaMaximo = alfaMaximo;
        this.variacionEscala = variacionEscala;
        this.velocidad = velocidad;
    }

    private void Awake()
    {
        semilla = Random.value * 100f;
    }

    private void Update()
    {
        if (grafico == null) return;

        float ruido = Mathf.PerlinNoise(semilla, Time.unscaledTime * velocidad);

        Color c = grafico.color;
        c.a = Mathf.Lerp(alfaMinimo, alfaMaximo, ruido);
        grafico.color = c;

        transform.localScale = Vector3.one * (1f + (ruido - 0.5f) * variacionEscala);
    }
}
