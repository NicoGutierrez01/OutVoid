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

    [Header("Mejoras Pasivas")]
    public bool tieneEscudoEmergencia = false;

    private PlayerHUD _hud;

    public void Initialize()
    {
        _hud = Object.FindAnyObjectByType<PlayerHUD>();

        if (AdministradorDeProgreso.Instancia != null)
        {
            maxHealth = AdministradorDeProgreso.Instancia.vidaMaximaGuardada;
            currentHealth = AdministradorDeProgreso.Instancia.vidaActualGuardada;
            tieneEscudoEmergencia = AdministradorDeProgreso.Instancia.tieneEscudoEmergencia;
            EnemyHealth.healthPerKillActive = AdministradorDeProgreso.Instancia.saludPorKill;
        }
        else
        {
            currentHealth = maxHealth;
        }

        regenTimer = timeBeforeRegen;
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isGhostMode) return;

        regenTimer = 0f;

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

        if (tieneEscudoEmergencia && currentHealth > 0 && currentHealth < 30f)
        {
            currentShield += 60f;
            tieneEscudoEmergencia = false;
            _hud?.MostrarItemRecogido("¡ESCUDO DE EMERGENCIA ACTIVADO!");
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (currentHealth <= 0) Morir();
    }

    private void Morir()
    {
        Debug.Log("El jugador ha muerto. Enviando evento GameOver y pasando a la pantalla...");
        
        // Asignamos que perdió
        SessionData.win = false; 
        
        // Empaquetamos los parámetros
        GameOverEvent gameOverEvent = new GameOverEvent
        {
            level = SessionData.level,
            // Sacamos el tiempo final directamente de tu GameTimer
            time = Mathf.FloorToInt(GameTimer.tiempoTotal), 
            win = SessionData.win,
            chara = SessionData.chara,
            weapon = SessionData.weapon,
            // Asumiendo que tenés la variable enemy en tu StaticVariables.SessionData
            enemy = SessionData.enemy 
        };

        // Grabamos el evento
        AnalyticsService.Instance.RecordEvent(gameOverEvent);

        UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
    }
    
    private void Update()
    {
        if (currentHealth < limitRegen && currentHealth > 0 && regenTimer >= timeBeforeRegen)
        {
            currentHealth = Mathf.Clamp(currentHealth + regenRate * Time.deltaTime, 0, limitRegen);
        }
        else{
            regenTimer += Time.deltaTime;
        }
        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso.Instancia.vidaActualGuardada = currentHealth;
            AdministradorDeProgreso.Instancia.vidaMaximaGuardada = maxHealth;
        }
    }

}