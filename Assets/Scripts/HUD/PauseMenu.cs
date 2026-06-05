using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 
using UnityEngine.UI; 
using TMPro;          

public class PauseMenu : MonoBehaviour
{
    [Header("UI del Menú de Pausa")]
    public GameObject ventanaPausa; 

    [Header("Opciones - Sensibilidad")]
    public Slider sliderSensibilidad;
    public TextMeshProUGUI textoSensibilidad;

    [Header("Referencias")]
    public PlayerCamera playerCamera; 

    private bool estaPausado = false;

    void Start()
    {
        ventanaPausa.SetActive(false);
        Time.timeScale = 1f;

        float sensibilidadGuardada = PlayerPrefs.GetFloat("SensibilidadMouse", 1f);
        if (sliderSensibilidad != null)
        {
            sliderSensibilidad.value = sensibilidadGuardada;
            ActualizarTextoSensibilidad(sensibilidadGuardada);
        }

        if (playerCamera == null)
        {
            playerCamera = FindAnyObjectByType<PlayerCamera>();
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (estaPausado) ReanudarJuego();
            else PausarJuego();
        }
    }

    public void PausarJuego()
    {
        estaPausado = true;
        ventanaPausa.SetActive(true);
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ReanudarJuego()
    {
        estaPausado = false;
        ventanaPausa.SetActive(false);
        Time.timeScale = 1f; 
        
        PlayerPrefs.Save(); 

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void CambiarSensibilidad(float valor)
    {
        PlayerPrefs.SetFloat("SensibilidadMouse", valor);
        PlayerPrefs.Save();
        ActualizarTextoSensibilidad(valor); // Ahora esto funciona

        if (playerCamera == null)
        {
            playerCamera = FindAnyObjectByType<PlayerCamera>();
        }

        if (playerCamera != null)
        {
            playerCamera.ActualizarSensibilidad();
        }
    }

    private void ActualizarTextoSensibilidad(float valor)
    {
        if (textoSensibilidad != null)
        {
            textoSensibilidad.text = valor.ToString("F2");
        }
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1f; 
        PlayerPrefs.Save(); 
        SceneManager.LoadScene("MainMenu"); 
    }
}