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
    public bool balasPenetrantes = false;
    public bool disparoTriple = false;

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

        // VIDA Y ESCUDO
        if (stats != null)
        {
            vidaMaximaGuardada = stats.maxHealth;
            vidaActualGuardada = stats.currentHealth;
            escudoGuardado = stats.currentShield;
        }

        // MUNICIÓN Y MEJORAS DE ARMA
        if (weapon != null)
        {
            balasActualesGuardadas = weapon.balasActuales;
            balasReservaGuardadas = weapon.balasReserva;
            balasDeFuego = weapon.tieneFuego;
            balasPenetrantes = weapon.balasPenetrantes;
            disparoTriple = weapon.disparoTriple;
        }

        Debug.Log(
            $"[PROGRESO] Estado guardado -> " +
            $"HP: {vidaActualGuardada}/{vidaMaximaGuardada} | " +
            $"Escudo: {escudoGuardado} | " +
            $"Balas: {balasActualesGuardadas}/{balasReservaGuardadas} | " +
            $"Penetrante: {balasPenetrantes} | Triple: {disparoTriple}"
        );
    }

    // =========================================================
    // REINICIAR TODA LA PARTIDA
    // =========================================================

    public void ReiniciarProgreso()
    {
        vidaMaximaGuardada = 100f;
        vidaActualGuardada = 100f;

        escudoGuardado = 0f;

        balasActualesGuardadas = 6;
        balasReservaGuardadas = 24;

        tieneEscudoEmergencia = false;
        saludPorKill = false;

        multiplicadorVelocidad = 1f;
        saltosAdicionales = 0;

        multiplicadorDashCooldown = 1f;
        multiplicadorDinamitaCooldown = 1f;

        multiplicadorDaño = 1f;
        balasDeFuego = false;
        balasPenetrantes = false;
        disparoTriple = false;

        multiplicadorRecarga = 1f;
        probabilidadDropExtra = 0f;

        enemigosMuertos = 0;
        puntosTotales = 0;
        mejorasRecogidas = 0;

        Debug.Log("[PROGRESO] Progreso reiniciado.");
    }
}