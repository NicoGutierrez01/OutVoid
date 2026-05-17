using UnityEngine;

public class AdministradorDeProgreso : MonoBehaviour
{
    public static AdministradorDeProgreso Instancia;

    [Header("Mejoras de la Partida")]
    public bool tieneEscudoEmergencia = false;
    public bool saludPorKill = false;
    public float multiplicadorVelocidad = 1f;
    public int saltosAdicionales = 0;
    public float multiplicadorDashCooldown = 1f;
    public float multiplicadorDinamitaCooldown = 1f;
    public float multiplicadorDaño = 1f;
    public bool balasDeFuego = false;
    public float multiplicadorRecarga = 1f;
    public float probabilidadDropExtra = 0f;

    [Header("Estadísticas de Vida")]
    public float vidaMaximaGuardada = 100f;
    public float vidaActualGuardada = 100f;

    [Header("Datos para el Game Over")]
    public int enemigosMuertos = 0;
    public int puntosTotales = 0;
    public int mejorasRecogidas = 0;

    private void Awake()
    {
        if (Instancia == null) { Instancia = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void ReiniciarProgreso()
    {
        vidaMaximaGuardada = 100f;
        vidaActualGuardada = 100f;
        tieneEscudoEmergencia = false;
        saludPorKill = false;
        multiplicadorVelocidad = 1f;
        saltosAdicionales = 0;
        multiplicadorDashCooldown = 1f;
        multiplicadorDinamitaCooldown = 1f;
        multiplicadorDaño = 1f;
        balasDeFuego = false;
        multiplicadorRecarga = 1f;
        probabilidadDropExtra = 0f;
        
        enemigosMuertos = 0;
        puntosTotales = 0;
        mejorasRecogidas = 0;
    }
}