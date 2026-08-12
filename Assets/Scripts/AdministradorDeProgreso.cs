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


    [Header("Estado del Jugador")]
    public float escudoGuardado = 0f;

    // Balas actualmente dentro del tambor
    public int balasActualesGuardadas = 6;

    // Balas disponibles en reserva
    public int balasReservaGuardadas = 24;


    [Header("Datos para el Game Over")]
    public int enemigosMuertos = 0;
    public int puntosTotales = 0;
    public int mejorasRecogidas = 0;


    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    // =========================================================
    // GUARDAR ESTADO DEL JUGADOR
    // =========================================================

    public void GuardarEstadoJugador(GameObject jugador)
    {
        if (jugador == null)
        {
            Debug.LogError("[PROGRESO] No se pudo guardar el estado: jugador es NULL.");
            return;
        }

        PlayerStats stats = jugador.GetComponentInChildren<PlayerStats>();
        WeaponSystem weapon = jugador.GetComponentInChildren<WeaponSystem>();


        // =====================================================
        // VIDA Y ESCUDO
        // =====================================================

        if (stats != null)
        {
            vidaMaximaGuardada = stats.maxHealth;
            vidaActualGuardada = stats.currentHealth;
            escudoGuardado = stats.currentShield;
        }
        else
        {
            Debug.LogWarning("[PROGRESO] No se encontró PlayerStats.");
        }


        // =====================================================
        // MUNICIÓN
        // =====================================================

        if (weapon != null)
        {
            balasActualesGuardadas = weapon.balasActuales;
            balasReservaGuardadas = weapon.balasReserva;
        }
        else
        {
            Debug.LogWarning("[PROGRESO] No se encontró WeaponSystem.");
        }


        Debug.Log(
            $"[PROGRESO] Estado guardado -> " +
            $"HP: {vidaActualGuardada}/{vidaMaximaGuardada} | " +
            $"Escudo: {escudoGuardado} | " +
            $"Balas: {balasActualesGuardadas}/{balasReservaGuardadas} | " +
            $"Saltos extra: {saltosAdicionales}"
        );
    }


    // =========================================================
    // REINICIAR TODA LA PARTIDA
    // =========================================================

    public void ReiniciarProgreso()
    {
        // Vida
        vidaMaximaGuardada = 100f;
        vidaActualGuardada = 100f;

        // Estado
        escudoGuardado = 0f;

        // Munición inicial
        balasActualesGuardadas = 6;
        balasReservaGuardadas = 24;


        // Mejoras
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


        // Estadísticas
        enemigosMuertos = 0;
        puntosTotales = 0;
        mejorasRecogidas = 0;


        Debug.Log("[PROGRESO] Progreso reiniciado.");
    }
}