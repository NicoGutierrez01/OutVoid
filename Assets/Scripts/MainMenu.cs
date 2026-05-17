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
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
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