using UnityEngine;
using System.Collections;

public class MiniCube : MonoBehaviour
{
    [Header("Estadísticas")]
    public float maxHealth = 25f;
    public float currentHealth;

    [Header("Efectos Visuales")]
    public Renderer minionRenderer; 
    public float flashDuration = 0.1f;
    private Color originalColor;
    private MaterialPropertyBlock propBlock;

    [Header("Ajustes de Combate")]
    public float cadenciaDisparo = 2f;
    public float fuerzaBala = 20f;
    
    [Header("Referencias")]
    public GameObject balaPrefab;
    public Transform puntoDeDisparo; 
    
    private Transform player;
    private float timerDisparo;

    void Awake() { propBlock = new MaterialPropertyBlock(); }

    void Start()
    {
        maxHealth = maxHealth + ((MapManager.nivelBucle - 1) * 10f);
        currentHealth = maxHealth;

        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        
        gameObject.tag = "MinionBoss";

        if (minionRenderer != null) originalColor = minionRenderer.sharedMaterial.color;
    }

    void Update()
    {
        if (player == null) return;

        transform.LookAt(player);

        timerDisparo += Time.deltaTime;
        if (timerDisparo >= cadenciaDisparo)
        {
            Disparar();
            timerDisparo = 0;
        }
    }

    void Disparar()
    {
        if (balaPrefab != null && puntoDeDisparo != null)
        {
            GameObject bala = Instantiate(balaPrefab, puntoDeDisparo.position, puntoDeDisparo.rotation);
            Rigidbody rb = bala.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(puntoDeDisparo.forward * fuerzaBala, ForceMode.Impulse);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (minionRenderer != null && gameObject.activeInHierarchy) StartCoroutine(FlashRed());
        if (currentHealth <= 0) Die();
    }

    public void Quemar() { StartCoroutine(EfectoFuego()); }

    IEnumerator EfectoFuego()
    {
        float damagePerTick = maxHealth * 0.25f / 3f; 
        for (int i = 0; i < 3; i++)
        {
            propBlock.SetColor("_Color", new Color(1f, 0.5f, 0f)); 
            minionRenderer.SetPropertyBlock(propBlock);
            TakeDamage(damagePerTick);
            yield return new WaitForSeconds(1f);
        }
        propBlock.SetColor("_Color", originalColor);
        minionRenderer.SetPropertyBlock(propBlock);
    }

    IEnumerator FlashRed()
    {
        propBlock.SetColor("_Color", Color.red);
        minionRenderer.SetPropertyBlock(propBlock);
        yield return new WaitForSeconds(flashDuration);
        propBlock.SetColor("_Color", originalColor);
        minionRenderer.SetPropertyBlock(propBlock);
    }

    void Die()
    {
        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso.Instancia.enemigosMuertos++;
            AdministradorDeProgreso.Instancia.puntosTotales += Random.Range(50, 76);
        }
        
        Destroy(gameObject);
    }
}