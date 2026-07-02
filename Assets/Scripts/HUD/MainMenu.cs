using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; 

public class MainMenu : MonoBehaviour
{
    [Header("Paneles del Menú")]
    public GameObject panelPrincipal;
    public GameObject panelOpciones;
    public GameObject panelConsentimiento; 

    public Animator anim;

    [Header("Escena de Juego")]
    public string nombreEscenaJuego = "Desert"; 

    [Header("Opciones - Sensibilidad")]
    public Slider sliderSensibilidad;
    public TextMeshProUGUI textoSensibilidad;

    [Header("Botón Salir")]
    public GameObject botonSalir;

    void Start()
    {
        int aceptoAnalytics = PlayerPrefs.GetInt("AnalyticsConsent", 0);

        if (aceptoAnalytics == 1)
        {
            if (panelConsentimiento != null) panelConsentimiento.SetActive(false);
            if (panelPrincipal != null) panelPrincipal.SetActive(true);
        }
        else
        {
            if (panelConsentimiento != null) panelConsentimiento.SetActive(true);
            if (panelPrincipal != null) panelPrincipal.SetActive(false);
        }

        if (panelOpciones != null) panelOpciones.SetActive(false);

        float sensibilidadGuardada = PlayerPrefs.GetFloat("SensibilidadMouse", 1f);
        if (sliderSensibilidad != null)
        {
            sliderSensibilidad.value = sensibilidadGuardada;
            ActualizarTextoSensibilidad(sensibilidadGuardada);
        }

        #if UNITY_WEBGL
            if (botonSalir != null) botonSalir.SetActive(false);
        #endif
    }

    public void MostrarMenuPrincipal()
    {
        panelConsentimiento.SetActive(false);
        panelPrincipal.SetActive(true);
    }

    public void EmpezarJuego()
    {
        GameTimer.tiempoTotal = 0f; 
        MapManager.nivelBucle = 1;

        // Carga directa e inmediata de la escena del juego
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void AbrirOpciones()
    {
        panelPrincipal.SetActive(false);
        panelOpciones.SetActive(true);
    }

    public void CerrarOpciones()
    {
        PlayerPrefs.Save(); 
        panelOpciones.SetActive(false);
        panelPrincipal.SetActive(true);
    }

    public void CambiarSensibilidad(float valor)
    {
        PlayerPrefs.SetFloat("SensibilidadMouse", valor);
        ActualizarTextoSensibilidad(valor);
    }

    void ActualizarTextoSensibilidad(float valor)
    {
        if (textoSensibilidad != null)
        {
            textoSensibilidad.text = valor.ToString("F2"); 
        }
    }

    public void SalirDelJuego()
    {
        Debug.Log("Cerrando Out-Void...");
        Application.Quit();
    }
}