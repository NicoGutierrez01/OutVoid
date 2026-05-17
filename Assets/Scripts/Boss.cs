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
    
    private Transform player;
    private float timerAtaque;
    private float timerSpawn;
    
    private Slider bossHealthBarGlobal;
    private TextMeshProUGUI textoVidaBoss; 

    void Awake() { propBlock = new MaterialPropertyBlock(); }

    void Start()
    {
        maxHealth = maxHealth + ((MapManager.nivelBucle - 1) * 1300f);
        currentHealth = maxHealth;
        
        posicionInicial = transform.position;
        
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (bossRenderer != null) originalColor = bossRenderer.sharedMaterial.color;

        Slider[] todosLosSliders = Resources.FindObjectsOfTypeAll<Slider>();
        
        foreach (Slider s in todosLosSliders)
        {
            if (s.name == "BossHealthBar" && s.gameObject.scene.isLoaded)
            {
                bossHealthBarGlobal = s;
                bossHealthBarGlobal.maxValue = maxHealth;
                bossHealthBarGlobal.value = currentHealth;
                
                bossHealthBarGlobal.gameObject.SetActive(true); 
                
                textoVidaBoss = bossHealthBarGlobal.GetComponentInChildren<TextMeshProUGUI>();
                ActualizarTextoVida();

                break;
            }
        }

        if (bossHealthBarGlobal == null)
        {
            Debug.LogError("No se encontró el BossHealthBar deshabilitado. Verificá que el nombre sea exacto.");
        }
    }

    void Update()
    {
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
        currentHealth -= amount;
        
        if (bossHealthBarGlobal != null)
            bossHealthBarGlobal.value = currentHealth;

        ActualizarTextoVida();

        if (bossRenderer != null && gameObject.activeInHierarchy) StartCoroutine(FlashRed());
        if (currentHealth <= 0) Die();
    }

    void ActualizarTextoVida()
    {
        if (textoVidaBoss != null)
        {
            int vidaActualMostrada = Mathf.Max(0, Mathf.CeilToInt(currentHealth));
            int vidaMaximaMostrada = Mathf.CeilToInt(maxHealth);
            textoVidaBoss.text = vidaActualMostrada.ToString() + " / " + vidaMaximaMostrada.ToString();
        }
    }

    public void Quemar() { StartCoroutine(EfectoFuego()); }

    IEnumerator EfectoFuego()
    {
        float damagePerTick = maxHealth * 0.05f; 
        for (int i = 0; i < 3; i++)
        {
            propBlock.SetColor("_Color", new Color(1f, 0.5f, 0f)); 
            bossRenderer.SetPropertyBlock(propBlock);
            TakeDamage(damagePerTick);
            yield return new WaitForSeconds(1f);
        }
        propBlock.SetColor("_Color", originalColor);
        bossRenderer.SetPropertyBlock(propBlock);
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
        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso.Instancia.enemigosMuertos++;
            AdministradorDeProgreso.Instancia.puntosTotales += Random.Range(750, 1200);
        }

        if (bossHealthBarGlobal != null) bossHealthBarGlobal.gameObject.SetActive(false);

        if (MapManager.Instance != null)
        {
            MapManager.Instance.ColapsarMapa();
        }

        Destroy(gameObject);
    }
}