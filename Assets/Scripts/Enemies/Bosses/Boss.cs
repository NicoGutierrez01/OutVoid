using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class Boss : MonoBehaviour
{
    [Header("Estadísticas")]
    public float maxHealth = 800f;
    public float currentHealth;
    
    [Header("Efectos Visuales")]
    public Renderer bossRenderer; 
    public float flashDuration = 0.1f;
    private Color originalColor;
    private MaterialPropertyBlock propBlock;

    [Header("Movimiento Lateral")]
    public float amplitud = 15f; 
    public float frecuencia = 0.5f; 
    private Vector3 posicionInicial;

    [Header("Ataque (Bombas)")]
    public GameObject bombaPrefab;
    public Transform puntoDisparo; 
    public float cadenciaFuego = 2.5f;
    public float fuerzaLanzamiento = 25f;

    [Header("Invocación de Minions")]
    public GameObject minionPrefab;
    public float cadenciaSpawn = 8f; 
    public int maxMinionsActivos = 4;

    [Header("Animaciones y Explosión")]
    public Animator anim;
    [Tooltip("Arrastrá acá las partículas de explosión épica para cuando termine de morir")]
    public GameObject prefabExplosionFinal;
    [Tooltip("Cuánto dura la animación de muerte antes de explotar (en segundos)")]
    public float tiempoAnimacionMuerte = 2.0f;
    
    private Transform player;
    private float timerAtaque;
    private float timerSpawn;
    
    private int slotBarraAsignado = 0;
    private bool isDead = false;

   void Awake()
{
    propBlock = new MaterialPropertyBlock();

    if (bossRenderer == null)
        bossRenderer = GetComponentInChildren<Renderer>();

    if (bossRenderer == null)
        Debug.LogError("Boss: No se encontró ningún Renderer en el Boss ni en sus hijos.");
}

    void Start()
    {
        maxHealth = maxHealth + ((MapManager.nivelBucle - 1) * 1300f);
        currentHealth = maxHealth;
        
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayBossMusic();

        posicionInicial = transform.position;
        
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (bossRenderer != null) originalColor = bossRenderer.sharedMaterial.color;

        // Registro con el nuevo sistema de 3 barras
        if (BossHealthUIManager.Instance != null)
        {
            BossHealthUIManager.Instance.RegistrarBoss(maxHealth, out slotBarraAsignado);
        }
    }

    void Update()
    {
        if (isDead) return;

        ManejarMovimientoOscilatorio();
        
        if (player != null)
        {
            transform.LookAt(player);
            ManejarAtaque();
        }
    }

    void ManejarMovimientoOscilatorio()
    {
        float desplazamientox = Mathf.Sin(Time.time * frecuencia) * amplitud;
        transform.position = posicionInicial + new Vector3(desplazamientox, 0, 0);
    }

    void ManejarAtaque()
    {
        timerAtaque += Time.deltaTime;
        if (timerAtaque >= cadenciaFuego)
        {
            DispararBomba();
            timerAtaque = 0;
        }

        timerSpawn += Time.deltaTime;
        if (timerSpawn >= cadenciaSpawn)
        {
            ManejarInvocacion();
            timerSpawn = 0;
        }
    }

    void DispararBomba()
    {
        if (bombaPrefab != null && puntoDisparo != null)
        {
            if (anim != null) anim.SetTrigger("Shoot");

            GameObject bomba = Instantiate(bombaPrefab, puntoDisparo.position, Quaternion.identity);
            Rigidbody rb = bomba.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Vector3 direccion = (player.position - puntoDisparo.position).normalized;
                Vector3 vectorFuerza = (direccion + Vector3.up * 0.3f) * fuerzaLanzamiento;
                rb.AddForce(vectorFuerza, ForceMode.Impulse);
            }
        }
    }

    void ManejarInvocacion()
    {
        int minionsActuales = GameObject.FindGameObjectsWithTag("MinionBoss").Length;

        if (minionsActuales < maxMinionsActivos && minionPrefab != null)
        {
            Vector3 posSpawn = transform.position + Vector3.down * 3f;
            GameObject minion = Instantiate(minionPrefab, posSpawn, Quaternion.identity);
            
            Rigidbody rb = minion.GetComponent<Rigidbody>();
            if(rb != null) rb.AddForce(transform.right * Random.Range(-5f, 5f), ForceMode.Impulse);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        
        if (BossHealthUIManager.Instance != null)
        {
            BossHealthUIManager.Instance.ActualizarVidaBoss(slotBarraAsignado, currentHealth, maxHealth);
        }

        if (bossRenderer != null && gameObject.activeInHierarchy) StartCoroutine(FlashRed());
        if (currentHealth <= 0) Die();
    }

    public void Quemar() { StartCoroutine(EfectoFuego()); }

    IEnumerator EfectoFuego()
    {
        float damagePerTick = maxHealth * 0.05f; 
        for (int i = 0; i < 3; i++)
        {
            if (isDead) break;
            propBlock.SetColor("_Color", new Color(1f, 0.5f, 0f)); 
            bossRenderer.SetPropertyBlock(propBlock);
            TakeDamage(damagePerTick);
            yield return new WaitForSeconds(1f);
        }
        if (!isDead)
        {
            propBlock.SetColor("_Color", originalColor);
            bossRenderer.SetPropertyBlock(propBlock);
        }
    }

    IEnumerator FlashRed()
    {
        propBlock.SetColor("_Color", Color.red);
        bossRenderer.SetPropertyBlock(propBlock);
        yield return new WaitForSeconds(flashDuration);
        propBlock.SetColor("_Color", originalColor);
        bossRenderer.SetPropertyBlock(propBlock);
    }

    void Die()
    {
        isDead = true;
        this.enabled = false; 

        if (BossHealthUIManager.Instance != null)
        {
            BossHealthUIManager.Instance.DesactivarBarraBoss(slotBarraAsignado);
        }

        if (anim != null) anim.SetTrigger("Dead");

        StartCoroutine(RutinaMuerteBoss());
    }

    IEnumerator RutinaMuerteBoss()
    {
        yield return new WaitForSeconds(tiempoAnimacionMuerte);

        if (prefabExplosionFinal != null)
        {
            GameObject fx = Instantiate(prefabExplosionFinal, transform.position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        if (bossRenderer != null) bossRenderer.enabled = false;

        BossLootSpawner lootSpawner = GetComponent<BossLootSpawner>();
        if (lootSpawner != null)
        {
            lootSpawner.SpawnearRecompensas();
        }

        if (MapManager.Instance != null)
        {
            MapManager.Instance.RegistrarMuerteBoss();
        }

        Destroy(gameObject);
    }
}