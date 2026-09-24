using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ExperienceUI : MonoBehaviour
{
    [Header("Referencias UI Normales")]
    public Image barraBlancaExperiencia;  // Arrastra "Barra Blanca" (debe tener Image Type: Filled)
    public TextMeshProUGUI textoLevel;    // Arrastra "Level"

    [Header("Referencias UI de Nivel Completado (Efectos)")]
    public GameObject objetoBarraVerde;   // Arrastra "Barra Verde" (desactivado por defecto)
    public GameObject objetoRomboVerde;   // Arrastra "Rombo Verde" (desactivado por defecto)
    public float duracionEfectoVerde = 1.5f; 

    private Coroutine corrutinaEfectoNivel;

    private void Start()
    {
        if (ExperienceManager.Instancia == null)
            return;

        ExperienceManager.Instancia.OnExperienciaCambiada += ActualizarBarra;
        ExperienceManager.Instancia.OnSubioDeNivel += ManejarSubidaDeNivel;

        ActualizarBarra(
            ExperienceManager.Instancia.experienciaActual,
            ExperienceManager.Instancia.experienciaNecesaria
        );

        ActualizarNivelVisual(
            ExperienceManager.Instancia.nivelActual
        );

        if (objetoBarraVerde != null) objetoBarraVerde.SetActive(false);
        if (objetoRomboVerde != null) objetoRomboVerde.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ExperienceManager.Instancia == null)
            return;

        ExperienceManager.Instancia.OnExperienciaCambiada -= ActualizarBarra;
        ExperienceManager.Instancia.OnSubioDeNivel -= ManejarSubidaDeNivel;
    }

    private void ActualizarBarra(float actual, float necesaria)
    {
        if (barraBlancaExperiencia != null)
        {
            // Calcula el porcentaje (0 a 1) y lo aplica al llenado de la imagen
            barraBlancaExperiencia.fillAmount = actual / necesaria;
        }
    }

    private void ActualizarNivelVisual(int nivel)
    {
        if (textoLevel != null)
        {
            textoLevel.text = nivel.ToString();
        }
    }

    private void ManejarSubidaDeNivel(int nuevoNivel)
    {
        ActualizarNivelVisual(nuevoNivel);

        if (corrutinaEfectoNivel != null)
        {
            StopCoroutine(corrutinaEfectoNivel);
        }
        corrutinaEfectoNivel = StartCoroutine(RutinaEfectoNivelVerde());
    }

    private IEnumerator RutinaEfectoNivelVerde()
    {
        if (objetoBarraVerde != null) objetoBarraVerde.SetActive(true);
        if (objetoRomboVerde != null) objetoRomboVerde.SetActive(true);

        yield return new WaitForSeconds(duracionEfectoVerde);

        if (objetoBarraVerde != null) objetoBarraVerde.SetActive(false);
        if (objetoRomboVerde != null) objetoRomboVerde.SetActive(false);

        corrutinaEfectoNivel = null;
    }
}