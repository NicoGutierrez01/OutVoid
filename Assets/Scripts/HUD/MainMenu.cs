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
    public GameObject panelCarga;
    public GameObject panelConsentimiento; // <-- Asegurate de tener esta variable

    [Header("Pantalla de Carga")]
    public Slider barraDeCarga;
    public string nombreEscenaJuego = "Desert"; 

    [Header("Opciones - Sensibilidad")]
    public Slider sliderSensibilidad;
    public TextMeshProUGUI textoSensibilidad;

    [Header("Botón Salir")]
    public GameObject botonSalir;

    void Start()
    {
        // 1. INVERTIMOS EL ORDEN INICIAL: Encendemos el consentimiento y apagamos el resto
        if (panelConsentimiento != null) panelConsentimiento.SetActive(true);
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelCarga != null) panelCarga.SetActive(false);

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

    // 2. NUEVO MÉTODO: Se llama cuando el jugador responde Sí o No
    public void MostrarMenuPrincipal()
    {
        panelConsentimiento.SetActive(false);
        panelPrincipal.SetActive(true);
    }

    // 3. Este vuelve a ser exclusivo del botón "Play"
    public void EmpezarJuego()
    {
        panelPrincipal.SetActive(false);
        panelCarga.SetActive(true);
        
        StartCoroutine(CargarEscenaAsincrona());
    }

    IEnumerator CargarEscenaAsincrona()
    {
        AsyncOperation operacion = SceneManager.LoadSceneAsync(nombreEscenaJuego);

        operacion.allowSceneActivation = false; 

        float progresoVisual = 0f;

        while (!operacion.isDone)
        {
            float progresoReal = Mathf.Clamp01(operacion.progress / 0.9f);

            progresoVisual = Mathf.MoveTowards(progresoVisual, progresoReal, 0.5f * Time.deltaTime);
            
            if (barraDeCarga != null)
            {
                barraDeCarga.value = progresoVisual;
            }

            if (progresoVisual >= 1f)
            {
                operacion.allowSceneActivation = true;
            }

            yield return null; 
        }
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