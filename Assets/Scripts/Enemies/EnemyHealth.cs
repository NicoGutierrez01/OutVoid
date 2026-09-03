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
    [Tooltip("¿Mantener tamaño relativo a la distancia para que se distinga de lejos?")]
    public bool escalarConDistancia = true;
    public float multiplicadorEscalaLejos = 1.25f;
    [Tooltip("Tiempo en segundos antes de ocultar la barra si no recibe daño")]
    public float tiempoOcultarBarra = 5f; 

    [Range(0, 100)] public float probabilidadDrop = 35f; 

    public GameObject prefabDropVida;
    public GameObject prefabDropEscudo;
    public GameObject prefabDropBalas;

    [Header("Efecto de Explosión al Morir")]
    public GameObject prefabParticulasMuerte; 

    private PlayerStats playerScript; 
    private WeaponSystem weaponScript;
    private PlayerHUD playerHUDScript;
    public static bool healthPerKillActive = false; 

    private bool isDead = false;
    private Camera camaraPrincipal;
    private Vector3 escalaInicialCanvas;
    private Transform canvasTransform;
    private Coroutine corrutinaOcultarBarra;

    void Start()
    {
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        camaraPrincipal = Camera.main;
        
        maxHealth = maxHealth + ((MapManager.nivelBucle - 1) * 25f);
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerScript = playerObj.GetComponentInChildren<PlayerStats>();
            weaponScript = playerObj.GetComponentInChildren<WeaponSystem>();
            playerHUDScript = Object.FindFirstObjectByType<PlayerHUD>();
        }

        if (healthBar != null) 
        { 
            healthBar.direction = Slider.Direction.LeftToRight;
            healthBar.maxValue = maxHealth; 
            healthBar.value = currentHealth;

            canvasTransform = healthBar.transform.parent != null ? healthBar.transform.parent : healthBar.transform;

            Vector3 localScale = canvasTransform.localScale;
            localScale.x = Mathf.Abs(localScale.x);
            canvasTransform.localScale = localScale;
            escalaInicialCanvas = localScale;

            healthBar.gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (isDead || healthBar == null || !healthBar.gameObject.activeSelf || camaraPrincipal == null) return;

        canvasTransform.forward = camaraPrincipal.transform.forward;

        if (escalarConDistancia)
        {
            float distancia = Vector3.Distance(camaraPrincipal.transform.position, canvasTransform.position);
            float factor = Mathf.Max(1f, (distancia / 10f) * multiplicadorEscalaLejos);
            canvasTransform.localScale = escalaInicialCanvas * factor;
        }
    }

    IEnumerator FlashWhiteRoutine()
    {
        EncenderBrillo(Color.white * 5f); 
        yield return new WaitForSeconds(flashDuration);
        ApagarBrillo();
    }

    public void TakeDamage(float amount, bool esHeadshot = false)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
            healthBar.value = currentHealth;

            if (corrutinaOcultarBarra != null) StopCoroutine(corrutinaOcultarBarra);
            corrutinaOcultarBarra = StartCoroutine(RutinaOcultarBarraPorInactividad());
        }

        StartCoroutine(FlashWhiteRoutine());

        if (currentHealth > 0)
        {
            StartCoroutine(StunRoutine());
        }
        
        if (currentHealth <= 0)
        {
            CrosshairFeedbackManager crosshair = Object.FindFirstObjectByType<CrosshairFeedbackManager>();
            if (crosshair != null) crosshair.OnEnemyKill(esHeadshot);

            isDead = true;
            if (healthBar != null) healthBar.gameObject.SetActive(false);
            Die();
        }
    }

    IEnumerator RutinaOcultarBarraPorInactividad()
    {
        yield return new WaitForSeconds(tiempoOcultarBarra);
        if (healthBar != null && !isDead)
        {
            healthBar.gameObject.SetActive(false);
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
        if (corrutinaOcultarBarra != null) StopCoroutine(corrutinaOcultarBarra);

        if (agent != null) 
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false; 
        }

        Kamikaze kami = GetComponent<Kamikaze>();
        if (kami != null) { kami.estaVivo = false; kami.enabled = false; }

        Stalker stalker = GetComponent<Stalker>();
        if (stalker != null) stalker.enabled = false; 

        Artillero artillero = GetComponent<Artillero>();
        if (artillero != null) artillero.enabled = false; 

        Collider[] todosLosColliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in todosLosColliders) c.isTrigger = true;

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

        try
        {
            if (MapManager.Instance != null) MapManager.Instance.RegistrarMuerte();
        }
        catch (System.Exception e)
        {
            Debug.Log("Error en MapManager al registrar muerte: " + e.Message);
        }

        StartCoroutine(RutinaMuerteExplosiva());
    }

    IEnumerator RutinaMuerteExplosiva()
    {
        if (prefabParticulasMuerte != null)
        {
            GameObject fx = Instantiate(prefabParticulasMuerte, transform.position + Vector3.up * 1f, Quaternion.identity);
            Destroy(fx, 2.0f); 
        }

        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = false;
            }
        }

        if (healthBar != null) healthBar.gameObject.SetActive(false);

        yield return null;
        Destroy(gameObject);
    }
}