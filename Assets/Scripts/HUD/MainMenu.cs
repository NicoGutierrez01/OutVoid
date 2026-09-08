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

    [Header("Opciones - Sensibilidad Mouse")]
    public Slider sliderSensibilidadMouse;
    public TextMeshProUGUI textoSensibilidadMouse;

    [Header("Opciones - Sensibilidad Joystick")]
    public Slider sliderSensibilidadGamepad;
    public TextMeshProUGUI textoSensibilidadGamepad;

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

        // Cargar sensibilidad Mouse
        float sensMouse = PlayerPrefs.GetFloat("SensibilidadMouse", 1f);
        if (sliderSensibilidadMouse != null)
        {
            sliderSensibilidadMouse.minValue = 0.1f;
            sliderSensibilidadMouse.maxValue = 15f;
            sliderSensibilidadMouse.value = sensMouse;
            ActualizarTextoMouse(sensMouse);
        }

        // Cargar sensibilidad Joystick
        float sensGamepad = PlayerPrefs.GetFloat("SensibilidadGamepad", 5f);
        if (sliderSensibilidadGamepad != null)
        {
            sliderSensibilidadGamepad.minValue = 0.5f;
            sliderSensibilidadGamepad.maxValue = 15f;
            sliderSensibilidadGamepad.value = sensGamepad;
            ActualizarTextoGamepad(sensGamepad);
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

    public void CambiarSensibilidadMouse(float valor)
    {
        PlayerPrefs.SetFloat("SensibilidadMouse", valor);
        PlayerPrefs.Save();
        ActualizarTextoMouse(valor);
    }

    public void CambiarSensibilidadGamepad(float valor)
    {
        PlayerPrefs.SetFloat("SensibilidadGamepad", valor);
        PlayerPrefs.Save();
        ActualizarTextoGamepad(valor);
    }

    void ActualizarTextoMouse(float valor)
    {
        if (textoSensibilidadMouse != null)
            textoSensibilidadMouse.text = valor.ToString("F2");
    }

    void ActualizarTextoGamepad(float valor)
    {
        if (textoSensibilidadGamepad != null)
            textoSensibilidadGamepad.text = valor.ToString("F2");
    }

    public void SalirDelJuego()
    {
        Debug.Log("Cerrando Out-Void...");
        Application.Quit();
    }
}