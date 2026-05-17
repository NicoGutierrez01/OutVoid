using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Referencias")]
    public CharacterController controller;
    public Transform cam;

    [Header("Estadisticas")]
    public float currentHealth;
    public float maxHealth = 100f;
    public float currentShield = 0f;
    public bool isGhostMode = false;

    [Header("Regeneración de Vida")]
    public float regenRate = 5f; 
    public float timeBeforeRegen = 4f; 
    private float regenTimer; 

    [Header("Ajustes de Movimiento")]
    public float speed = 10f;
    public float gravity = -19.62f;
    public float jumpHeight = 2f;
    public float dashSpeedMultiplier = 2f;

    [Header("Habilidades Desbloqueables")]
    public int saltosAdicionalesMaximos = 0;
    private int jumpCount = 0;

    [Header("Mejoras Pasivas")]
    public bool tieneEscudoEmergencia = false;

    private Vector3 velocity;
    private Vector2 moveInput;
    private bool isGrounded;

    void Start() {
        if (AdministradorDeProgreso.Instancia != null)
        {
            maxHealth = AdministradorDeProgreso.Instancia.vidaMaximaGuardada;
            currentHealth = AdministradorDeProgreso.Instancia.vidaActualGuardada;

            speed *= AdministradorDeProgreso.Instancia.multiplicadorVelocidad;
            saltosAdicionalesMaximos = AdministradorDeProgreso.Instancia.saltosAdicionales;
            tieneEscudoEmergencia = AdministradorDeProgreso.Instancia.tieneEscudoEmergencia;
            EnemyHealth.healthPerKillActive = AdministradorDeProgreso.Instancia.saludPorKill; 
        }
        else 
        {
            currentHealth = maxHealth;
        }

        regenTimer = timeBeforeRegen;
    }

    public void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            if (isGrounded)
            {
                Jump();
                jumpCount = 1;
            }

            else if (jumpCount <= saltosAdicionalesMaximos)
            {
                Jump();
                jumpCount++;
            }
        }
    }

    void Jump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
            Object.FindAnyObjectByType<PlayerHUD>().MostrarItemRecogido("¡ESCUDO DE EMERGENCIA ACTIVADO!");
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (currentHealth <= 0) Morir();
    }

    void Morir()
    {
        Debug.Log("El jugador ha muerto. Pasando a la pantalla de Game Over...");

        UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
            jumpCount = 0; 
        }

        if (currentHealth < maxHealth && currentHealth > 0 && regenTimer >= timeBeforeRegen)
            currentHealth = Mathf.Clamp(currentHealth + regenRate * Time.deltaTime, 0, maxHealth);
        else
            regenTimer += Time.deltaTime;

        transform.rotation = Quaternion.Euler(0f, cam.eulerAngles.y, 0f);
        float currentSpeed = isGhostMode ? speed * dashSpeedMultiplier : speed;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move.normalized * currentSpeed * Time.deltaTime);
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso.Instancia.vidaActualGuardada = currentHealth;
            AdministradorDeProgreso.Instancia.vidaMaximaGuardada = maxHealth;
        }
    }
}