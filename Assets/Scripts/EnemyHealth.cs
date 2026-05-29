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

    [Header("UI y Drop")]
    public Slider healthBar;
    public GameObject itemPrefab; 
    [Range(0, 100)] public float probabilidadDrop = 30f;

    private PlayerStats playerScript; 
    public static bool healthPerKillActive = false; 

    void Awake() { propBlock = new MaterialPropertyBlock(); }

    void Start()
    {
        maxHealth = maxHealth + ((MapManager.nivelBucle - 1) * 25f);
        currentHealth = maxHealth;
        
        currentHealth = maxHealth;
        playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();
        if (healthBar != null) { healthBar.maxValue = maxHealth; healthBar.value = currentHealth; }
        if (enemyRenderer != null) originalColor = enemyRenderer.sharedMaterial.color;
    }

    public void Quemar() { StartCoroutine(EfectoFuego()); }

    IEnumerator EfectoFuego()
    {
        float damagePerTick = maxHealth * 0.25f / 3f; 
        for (int i = 0; i < 3; i++)
        {
            propBlock.SetColor("_Color", new Color(1f, 0.5f, 0f)); 
            enemyRenderer.SetPropertyBlock(propBlock);
            TakeDamage(damagePerTick);
            yield return new WaitForSeconds(1f);
        }
        propBlock.SetColor("_Color", originalColor);
        enemyRenderer.SetPropertyBlock(propBlock);
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

        if (Random.value * 100 <= probabilidadDrop)
        {
            Vector3 posicionSpawn = transform.position;
            RaycastHit hit;

            if (Physics.Raycast(transform.position, Vector3.down, out hit, 5f))
            {
                posicionSpawn = hit.point + Vector3.up * 0.5f; 
            }

            Instantiate(itemPrefab, posicionSpawn, Quaternion.identity);
        }

        if (MapManager.Instance != null)
            {
                MapManager.Instance.RegistrarMuerte();
            }
        
        Destroy(gameObject);
    }
}