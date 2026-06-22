using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    public NavMeshAgent agent;
    public float maxHealth = 100f;
    private float currentHealth;
    
    [Header("Efectos Visuales")] 
    public float flashDuration = 0.1f;
    private SkinnedMeshRenderer[] renderers; 

    [Header("UI y Drops de Recursos")]
    public Slider healthBar;
    [Range(0, 100)] public float probabilidadDrop = 35f; 

    public GameObject prefabDropVida;
    public GameObject prefabDropEscudo;
    public GameObject prefabDropBalas;

    private PlayerStats playerScript; 
    private WeaponSystem weaponScript;
    private PlayerHUD playerHUDScript;
    public static bool healthPerKillActive = false; 

    private bool isDead = false;

    void Start()
    {
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        
        maxHealth = maxHealth + ((MapManager.nivelBucle - 1) * 25f);
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerScript = playerObj.GetComponentInChildren<PlayerStats>();
            weaponScript = playerObj.GetComponentInChildren<WeaponSystem>();
            playerHUDScript = Object.FindFirstObjectByType<PlayerHUD>();
        }

        if (healthBar != null) { healthBar.maxValue = maxHealth; healthBar.value = currentHealth; }
    }

    IEnumerator FlashWhiteRoutine()
    {
        EncenderBrillo(Color.white * 5f); 
        
        yield return new WaitForSeconds(flashDuration);

        ApagarBrillo();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;

        StartCoroutine(FlashWhiteRoutine());

        if (playerHUDScript != null) playerHUDScript.MostrarHitmarker();

        if (currentHealth > 0)
        {
            StartCoroutine(StunRoutine());
        }
        
        if (healthBar != null) healthBar.value = currentHealth;
        
        if (currentHealth <= 0)
        {
            isDead = true;
            if(healthBar != null) healthBar.gameObject.SetActive(false);
            Die();
        }
    }

    void EncenderBrillo(Color colorBase) 
    {
        foreach (var r in renderers) 
        {
            if (r != null) r.material.SetColor("_EmissionColor", colorBase);
        }
    }

    void ApagarBrillo() 
    {
        foreach (var r in renderers) 
        {
            if (r != null) r.material.SetColor("_EmissionColor", Color.black);
        }
    }

    IEnumerator StunRoutine()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null && !anim.GetBool("isDead")) anim.SetTrigger("Stun");
            
            yield return new WaitForSeconds(0.3f);

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = false;
        }
    }

    void Die()
    {
        if (agent != null) { agent.isStopped = true; agent.enabled = false; }
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) { anim.SetBool("isMoving", false); anim.SetTrigger("Die"); }

        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso.Instancia.enemigosMuertos++;
            AdministradorDeProgreso.Instancia.puntosTotales += Random.Range(120, 350); 
        }
        
        if (healthPerKillActive && playerScript != null) { playerScript.maxHealth += 1f; playerScript.Heal(1f); }

        if (Random.value * 100 <= probabilidadDrop)
        {
            Vector3 posicionSpawn = transform.position;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 500f))
                posicionSpawn = hit.point + Vector3.up * 0.5f; 

            float dropRoll = Random.Range(1, 101);
            GameObject recursoAElegir = null;

            bool vidaCritica = playerScript != null && playerScript.currentHealth < 30f;
            bool municionEscasa = weaponScript != null && weaponScript.balasReserva < 12;
            bool municionLlena = weaponScript != null && weaponScript.balasReserva >= 30;

            if (vidaCritica) {
                if (dropRoll <= 70) recursoAElegir = prefabDropVida;
                else if (dropRoll <= 90) recursoAElegir = prefabDropEscudo;
                else recursoAElegir = prefabDropBalas;
            }
            else if (municionLlena) {
                if (dropRoll <= 60) recursoAElegir = prefabDropEscudo;
                else recursoAElegir = prefabDropVida;
            }
            else if (municionEscasa) {
                if (dropRoll <= 70) recursoAElegir = prefabDropBalas;
                else if (dropRoll <= 90) recursoAElegir = prefabDropEscudo;
                else recursoAElegir = prefabDropVida;
            }
            else {
                if (dropRoll <= 50) recursoAElegir = prefabDropBalas;
                else if (dropRoll <= 80) recursoAElegir = prefabDropEscudo;
                else recursoAElegir = prefabDropVida;
            }

            if (recursoAElegir != null) Instantiate(recursoAElegir, posicionSpawn, Quaternion.identity);
        }

        if (MapManager.Instance != null) MapManager.Instance.RegistrarMuerte();

        StartCoroutine(EsperarYDestruir(2.0f));
    }

    IEnumerator EsperarYDestruir(float tiempo)
    {
        yield return new WaitForSeconds(tiempo);
        Destroy(gameObject);
    }
}