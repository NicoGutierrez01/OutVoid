using UnityEngine;
using Unity.Services.Analytics;
using static StaticVariables;
using static EventManager;

public class PlayerStats : MonoBehaviour
{
    [Header("Estadisticas")]
    public float currentHealth;
    public float maxHealth = 100f;
    public float currentShield = 0f;
    public bool isGhostMode = false;

    [Header("Regeneración de Vida")]
    public float regenRate = 5f;
    public float timeBeforeRegen = 4f;
    public float limitRegen = 45f;
    private float regenTimer;
    private bool muerto;

    [Header("Mejoras Pasivas")]
    public bool tieneEscudoEmergencia = false;


    private void Awake()
    {
        currentHealth = maxHealth;
    }


    public void Initialize()
    {

        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso progreso = AdministradorDeProgreso.Instancia;

            maxHealth = progreso.vidaMaximaGuardada;
            currentHealth = progreso.vidaActualGuardada;

            currentShield = progreso.escudoGuardado;

            tieneEscudoEmergencia = progreso.tieneEscudoEmergencia;

            EnemyHealth.healthPerKillActive = progreso.saludPorKill;

            Debug.Log(
                $"[PLAYER STATS] Estado restaurado -> " +
                $"HP: {currentHealth}/{maxHealth} | " +
                $"Escudo: {currentShield}"
            );
        }
        else
        {
            currentHealth = maxHealth;
            currentShield = 0f;
        }

        regenTimer = timeBeforeRegen;
    }


    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(
            currentHealth + amount,
            0,
            maxHealth
        );
    }


    // atacante: nombre que se muestra en GameOver si este golpe mata al jugador (KAMIKAZE, STALKER...).
    public void TakeDamage(float amount, string atacante = null)
    {
        if (muerto) return;
        if (isGhostMode) return;
        if (AdministradorDeProgreso.Instancia != null && AdministradorDeProgreso.Instancia.modoDios) return;

        regenTimer = 0f;

        if (MusicManager.Instance != null) MusicManager.Instance.PlayTakingDamage();

        if (currentShield > 0)
        {
            currentShield -= amount;

            if (currentShield < 0)
            {
                float sobrante = Mathf.Abs(currentShield);

                currentShield = 0;
                currentHealth -= sobrante;
            }
        }
        else
        {
            currentHealth -= amount;
        }

        if (
            tieneEscudoEmergencia &&
            currentHealth > 0 &&
            currentHealth < 30f
        )
        {
            currentShield += 60f;
            tieneEscudoEmergencia = false;

            CrosshairFeedbackManager crosshair = FindAnyObjectByType<CrosshairFeedbackManager>();
            if (crosshair != null) crosshair.ShowReward(CrosshairFeedbackManager.RewardType.Shield);
        }


        currentHealth = Mathf.Clamp(
            currentHealth,
            0,
            maxHealth
        );


        if (currentHealth <= 0)
        {
            Morir(atacante);
        }
    }


    private void Morir(string atacante)
    {
        // Una sola muerte: el láser del jefe o varias explosiones pueden pegar varias veces en el mismo frame.
        muerto = true;

        // Se guarda antes de cambiar de escena: GameOver no puede consultar objetos del mapa.
        if (AdministradorDeProgreso.Instancia != null)
            AdministradorDeProgreso.Instancia.enemigoAsesino = string.IsNullOrEmpty(atacante) ? "DESCONOCIDO" : atacante;

        Debug.Log(
            "El jugador ha muerto. Enviando evento GameOver y pasando a la pantalla..."
        );

        GameOverEvent gameOverEvent = new GameOverEvent
        {
            level = SessionData.level,
            time = Mathf.FloorToInt(GameTimer.tiempoTotal),
        };

        // Si Analytics no está inicializado (Play directo en un mapa) no debe impedir llegar al GameOver.
        try { AnalyticsService.Instance.RecordEvent(gameOverEvent); }
        catch (System.Exception e) { Debug.LogWarning("[PlayerStats] No se pudo enviar GameOverEvent: " + e.Message); }

        UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
    }


    private void Update()
    {
        if (
            currentHealth < limitRegen &&
            currentHealth > 0 &&
            regenTimer >= timeBeforeRegen
        )
        {
            currentHealth = Mathf.Clamp(
                currentHealth + regenRate * Time.deltaTime,
                0,
                limitRegen
            );
        }
        else
        {
            regenTimer += Time.deltaTime;
        }

        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso progreso =
                AdministradorDeProgreso.Instancia;

            progreso.vidaActualGuardada = currentHealth;
            progreso.vidaMaximaGuardada = maxHealth;
            progreso.escudoGuardado = currentShield;

            progreso.tieneEscudoEmergencia =
                tieneEscudoEmergencia;
        }
    }

    public void AumentarVidaMaxima(float cantidad)
    {
        maxHealth += cantidad;
        currentHealth += cantidad;

        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso.Instancia.vidaMaximaGuardada = maxHealth;
            AdministradorDeProgreso.Instancia.vidaActualGuardada = currentHealth;
        }
    }
}