using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    
    [Header("Efectos Visuales")]
    public Renderer enemyRenderer; 
    public float flashDuration = 0.1f;
    private Color originalColor;
    private MaterialPropertyBlock propBlock;

    [Header("UI y Drops de Recursos")]
    public Slider healthBar;
    
    [Range(0, 100)] public float probabilidadDrop = 40f; 

    // NUEVO: Separamos los prefabs de los drops
    public GameObject prefabDropVida;
    public GameObject prefabDropEscudo;
    public GameObject prefabDropBalas;

    private PlayerMovement playerScript; 
    public static bool healthPerKillActive = false; 

    void Awake() { propBlock = new MaterialPropertyBlock(); }

    void Start()
    {      
        currentHealth = maxHealth;
        
        playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        if (healthBar != null) { healthBar.maxValue = maxHealth; healthBar.value = currentHealth; }
        if (enemyRenderer != null) originalColor = enemyRenderer.sharedMaterial.color;
    }

    public void Quemar() { StartCoroutine(EfectoFuego()); }

    IEnumerator EfectoFuego()
    {
        float damagePerTick = maxHealth * 0.05f; 
        int ticks = 5; 
        for (int i = 0; i < ticks; i++)
        {
            TakeDamage(damagePerTick);
            yield return new WaitForSeconds(1f); 
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (healthBar != null) healthBar.value = currentHealth;
        if (enemyRenderer != null && gameObject.activeInHierarchy) StartCoroutine(FlashRed());
        if (currentHealth <= 0) Die();
    }

    IEnumerator FlashRed()
    {
        propBlock.SetColor("_Color", Color.red);
        enemyRenderer.SetPropertyBlock(propBlock);
        yield return new WaitForSeconds(flashDuration);
        propBlock.SetColor("_Color", originalColor);
        enemyRenderer.SetPropertyBlock(propBlock);
    }

    void Die()
    {
        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso.Instancia.enemigosMuertos++;
            AdministradorDeProgreso.Instancia.puntosTotales += Random.Range(120, 350); 
        }

        if (healthPerKillActive && playerScript != null) 
        {
            playerScript.maxHealth += 1f; 
            playerScript.Heal(1f); 
        }

        if (MapManager.Instance != null)
        {
            MapManager.Instance.RegistrarMuerte();
        }

        if (Random.value * 100 <= probabilidadDrop)
        {
            Vector3 posicionSpawn = transform.position;
            RaycastHit hit;

            if (Physics.Raycast(transform.position, Vector3.down, out hit, 5f))
            {
                posicionSpawn = hit.point + Vector3.up * 0.5f; 
            }

            float dropRoll = Random.Range(1, 101);
            GameObject recursoAElegir = null;

            if (dropRoll <= 65) 
            {
                recursoAElegir = prefabDropBalas;
            }
            else if (dropRoll <= 85) 
            {
                recursoAElegir = prefabDropEscudo;
            }
            else 
            {
                recursoAElegir = prefabDropVida;
            }

            if (recursoAElegir != null)
            {
                Instantiate(recursoAElegir, posicionSpawn, Quaternion.identity);
            }
        }

        Destroy(gameObject);
    }
}