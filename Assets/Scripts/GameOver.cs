using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; 

public class GameOver : MonoBehaviour
{
    [Header("Textos de Estadísticas")]
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoPuntos;
    public TextMeshProUGUI textoEnemigos;
    public TextMeshProUGUI textoMejoras;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        MostrarEstadisticas();
    }

    void MostrarEstadisticas()
    {
        // 1. Mostrar el Tiempo
        float t = GameTimer.tiempoTotal;
        int horas = (int)t / 3600;
        int minutos = (int)t / 60 % 60;
        int segundos = (int)t % 60;
        
        if (horas > 0) 
            textoTiempo.text = "TIEMPO: " + horas.ToString("00") + ":" + minutos.ToString("00") + ":" + segundos.ToString("00");
        else 
            textoTiempo.text = "TIEMPO: " + minutos.ToString("00") + ":" + segundos.ToString("00");

        if (AdministradorDeProgreso.Instancia != null)
        {
            textoPuntos.text = "PUNTOS: " + AdministradorDeProgreso.Instancia.puntosTotales.ToString();
            textoEnemigos.text = "ENEMIGOS ELIMINADOS: " + AdministradorDeProgreso.Instancia.enemigosMuertos.ToString();
            textoMejoras.text = "MEJORAS RECOLECTADAS: " + AdministradorDeProgreso.Instancia.mejorasRecogidas.ToString();
        }
        else
        {
            textoPuntos.text = "PUNTOS: 0";
            textoEnemigos.text = "ENEMIGOS ELIMINADOS: 0";
            textoMejoras.text = "MEJORAS RECOLECTADAS: 0";
        }
    }

    public void VolverAlMenu()
    {
        MapManager.nivelBucle = 1;
        GameTimer.tiempoTotal = 0f;

        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso.Instancia.ReiniciarProgreso();
        }

        SceneManager.LoadScene("MainMenu");
    }
}