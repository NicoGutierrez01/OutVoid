using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 
using UnityEngine.UI; 
using TMPro;          

public class PauseMenu : MonoBehaviour
{
    [Header("UI del Menú de Pausa")]
    public GameObject ventanaPausa; 

    [Header("Opciones - Sensibilidad Mouse")]
    public Slider sliderSensibilidadMouse;
    public TextMeshProUGUI textoSensibilidadMouse;

    [Header("Opciones - Sensibilidad Joystick")]
    public Slider sliderSensibilidadGamepad;
    public TextMeshProUGUI textoSensibilidadGamepad;

    [Header("Referencias")]
    public PlayerCamera playerCamera; 

    private bool estaPausado = false;

    void Start()
    {
        ventanaPausa.SetActive(false);
        Time.timeScale = 1f;

        float sensMouse = PlayerPrefs.GetFloat("SensibilidadMouse", 1f);
        if (sliderSensibilidadMouse != null)
        {
            sliderSensibilidadMouse.minValue = 0.1f;
            sliderSensibilidadMouse.maxValue = 15f;
            sliderSensibilidadMouse.value = sensMouse;
            ActualizarTextoMouse(sensMouse);
        }

        float sensGamepad = PlayerPrefs.GetFloat("SensibilidadGamepad", 5f);
        if (sliderSensibilidadGamepad != null)
        {
            sliderSensibilidadGamepad.minValue = 0.5f;
            sliderSensibilidadGamepad.maxValue = 15f;
            sliderSensibilidadGamepad.value = sensGamepad;
            ActualizarTextoGamepad(sensGamepad);
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

        if (Time.timeScale == 0f) return;
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

    public void CambiarSensibilidadMouse(float valor)
    {
        PlayerPrefs.SetFloat("SensibilidadMouse", valor);
        PlayerPrefs.Save();
        ActualizarTextoMouse(valor);
        NotificarCamara();
    }

    public void CambiarSensibilidadGamepad(float valor)
    {
        PlayerPrefs.SetFloat("SensibilidadGamepad", valor);
        PlayerPrefs.Save();
        ActualizarTextoGamepad(valor);
        NotificarCamara();
    }

    private void NotificarCamara()
    {
        if (playerCamera == null)
            playerCamera = FindAnyObjectByType<PlayerCamera>();

        if (playerCamera != null)
            playerCamera.ActualizarSensibilidad();
    }

    private void ActualizarTextoMouse(float valor)
    {
        if (textoSensibilidadMouse != null)
            textoSensibilidadMouse.text = valor.ToString("F2");
    }

    private void ActualizarTextoGamepad(float valor)
    {
        if (textoSensibilidadGamepad != null)
            textoSensibilidadGamepad.text = valor.ToString("F2");
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1f; 
        PlayerPrefs.Save(); 
        SceneManager.LoadScene("MainMenu"); 
    }
}