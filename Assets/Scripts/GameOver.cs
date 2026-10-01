using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Pantalla de fin de run (escena GameOver). La UI la arma Scripts/Editor/GameOverBuilder.cs.
// Solo lee datos que se guardaron antes del cambio de escena: AdministradorDeProgreso (DontDestroyOnLoad)
// y GameTimer.tiempoTotal (estático). No depende de objetos del mapa anterior.
// El reset de la run se hace recién al volver al menú, después de mostrar las estadísticas.
public class GameOver : MonoBehaviour
{
    [Header("Textos de Estadísticas")]
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [SerializeField] private TextMeshProUGUI etiquetaAsesino;
    [SerializeField] private TextMeshProUGUI textoAsesino;
    [SerializeField] private TextMeshProUGUI textoTiempo;
    [SerializeField] private TextMeshProUGUI textoEnemigos;
    [SerializeField] private TextMeshProUGUI textoMejoras;
    [SerializeField] private TextMeshProUGUI textoPuntos;
    [SerializeField] private TextMeshProUGUI textoNivel;
    [Tooltip("Se ocultan en victoria (p. ej. el ícono de calavera de 'TE MATÓ')")]
    [SerializeField] private GameObject[] objetosSoloDerrota;

    [Header("Animación de entrada (tiempo sin escalar, no bloquea el botón)")]
    [SerializeField] private CanvasGroup grupoTitulo;
    [SerializeField] private CanvasGroup grupoPanel;
    [Tooltip("Filas de estadísticas en el orden en que aparecen")]
    [SerializeField] private CanvasGroup[] gruposEstadisticas;
    [SerializeField] private float duracionFade = 0.45f;
    [SerializeField] private float retrasoPanel = 0.25f;
    [SerializeField] private float retrasoEntreEstadisticas = 0.08f;

    void Start()
    {
        // Si se murió con el juego congelado (cofre), el menú no debe quedar en timeScale 0.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Se llega acá por muerte (PlayerStats registra el asesino) o al terminar el último mapa (MapManager, sin asesino).
        // Antes se usaba nivelBucle >= 4, pero con 2 mapas la victoria llega con nivelBucle = 3.
        AdministradorDeProgreso p = AdministradorDeProgreso.Instancia;
        bool victoria = p != null && string.IsNullOrEmpty(p.enemigoAsesino);
        if (textoTitulo != null) textoTitulo.text = victoria ? "¡VICTORIA!" : "PERDISTE";
        if (victoria && objetosSoloDerrota != null)
            foreach (GameObject go in objetosSoloDerrota) if (go != null) go.SetActive(false);

        MostrarEstadisticas(p, victoria);
        StartCoroutine(Entrada());
    }

    void MostrarEstadisticas(AdministradorDeProgreso p, bool victoria)
    {
        string asesino = p != null && !string.IsNullOrEmpty(p.enemigoAsesino) ? p.enemigoAsesino : "DESCONOCIDO";
        if (victoria) Poner(etiquetaAsesino, "RESULTADO");
        Poner(textoAsesino, victoria ? "SOBREVIVISTE" : asesino);
        Poner(textoTiempo, FormatearTiempo(GameTimer.tiempoTotal));
        Poner(textoEnemigos, p != null ? p.enemigosMuertos.ToString() : "0");
        Poner(textoMejoras, p != null ? p.mejorasRecogidas.ToString() : "0");
        Poner(textoPuntos, p != null ? p.puntosTotales.ToString("N0") : "0");
        Poner(textoNivel, p != null ? p.nivelMaximoAlcanzado.ToString() : "1");
    }

    private static void Poner(TextMeshProUGUI texto, string valor)
    {
        if (texto != null) texto.text = valor;
    }

    private static string FormatearTiempo(float t)
    {
        int total = (int)t;
        int horas = total / 3600;
        return horas > 0
            ? $"{horas:00}:{total / 60 % 60:00}:{total % 60:00}"
            : $"{total / 60:00}:{total % 60:00}";
    }

    // Título, panel y filas aparecen escalonados. El botón no está en ningún grupo: se puede usar desde el primer frame.
    private IEnumerator Entrada()
    {
        if (grupoTitulo != null) StartCoroutine(Aparecer(grupoTitulo, 0f, 1.15f));
        if (grupoPanel != null) StartCoroutine(Aparecer(grupoPanel, retrasoPanel, 0.97f));

        if (gruposEstadisticas == null) yield break;
        for (int i = 0; i < gruposEstadisticas.Length; i++)
        {
            if (gruposEstadisticas[i] != null)
                StartCoroutine(Aparecer(gruposEstadisticas[i], retrasoPanel + 0.15f + i * retrasoEntreEstadisticas, 0.9f));
        }
    }

    private IEnumerator Aparecer(CanvasGroup grupo, float retraso, float escalaInicial)
    {
        Transform t = grupo.transform;
        grupo.alpha = 0f;
        t.localScale = Vector3.one * escalaInicial;

        if (retraso > 0f) yield return new WaitForSecondsRealtime(retraso);

        for (float x = 0f; x < 1f; x += Time.unscaledDeltaTime / duracionFade)
        {
            float k = Mathf.SmoothStep(0f, 1f, x);
            grupo.alpha = k;
            t.localScale = Vector3.one * Mathf.Lerp(escalaInicial, 1f, k);
            yield return null;
        }

        grupo.alpha = 1f;
        t.localScale = Vector3.one;
    }

    public void VolverAlMenu()
    {
        // Recién acá se borra la run: las estadísticas ya se mostraron.
        MapManager.nivelBucle = 1;
        GameTimer.tiempoTotal = 0f;

        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso.Instancia.ReiniciarProgreso();
        }

        SceneManager.LoadScene("MainMenu");
    }
}
