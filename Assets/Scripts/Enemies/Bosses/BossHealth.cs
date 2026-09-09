using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealth : MonoBehaviour
{
    [Header("Estadísticas")]
    public float maxHealth = 1200f;
    public float currentHealth;

    [Header("Efectos Visuales")]
    [SerializeField] private Renderer bossRenderer;
    [SerializeField] private float flashDuration = 0.1f;
    private Color originalColor;
    private MaterialPropertyBlock propBlock;

    [Header("Muerte y Recompensas")]
    [SerializeField] private Animator anim;
    [SerializeField] private GameObject prefabExplosionFinal;
    [SerializeField] private float tiempoAnimacionMuerte = 2.5f;

    private Slider bossHealthBarGlobal;
    private TextMeshProUGUI textoVidaBoss;
    public bool isDead { get; private set; } = false;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (MapManager.nivelBucle > 0)
        {
            maxHealth += ((MapManager.nivelBucle - 1) * 1300f);
        }
        currentHealth = maxHealth;

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayBossMusic();

        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (bossRenderer != null && bossRenderer.sharedMaterial != null)
            originalColor = bossRenderer.sharedMaterial.color;

        // Búsqueda del slider deshabilitado en UIScene
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
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"[BOSS DAMAGE] Daño recibido: {amount} | Vida restante real: {currentHealth}");

        if (bossHealthBarGlobal != null)
            bossHealthBarGlobal.value = currentHealth;

        ActualizarTextoVida();

        if (bossRenderer != null && gameObject.activeInHierarchy)
            StartCoroutine(FlashRed());

        if (currentHealth <= 0)
            Die();
    }

    public void Quemar()
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(EfectoFuego());
    }

    private IEnumerator EfectoFuego()
    {
        float damagePerTick = maxHealth * 0.05f;
        for (int i = 0; i < 3; i++)
        {
            if (isDead) break;
            propBlock.SetColor("_Color", new Color(1f, 0.5f, 0f));
            if (bossRenderer != null) bossRenderer.SetPropertyBlock(propBlock);
            TakeDamage(damagePerTick);
            yield return new WaitForSeconds(1f);
        }
        if (!isDead && bossRenderer != null)
        {
            propBlock.SetColor("_Color", originalColor);
            bossRenderer.SetPropertyBlock(propBlock);
        }
    }

    private void ActualizarTextoVida()
    {
        if (textoVidaBoss != null)
        {
            int vidaActualMostrada = Mathf.Max(0, Mathf.CeilToInt(currentHealth));
            int vidaMaximaMostrada = Mathf.CeilToInt(maxHealth);
            textoVidaBoss.text = $"{vidaActualMostrada} / {vidaMaximaMostrada}";
        }
    }

    private IEnumerator FlashRed()
    {
        propBlock.SetColor("_Color", Color.red);
        bossRenderer.SetPropertyBlock(propBlock);
        yield return new WaitForSeconds(flashDuration);
        propBlock.SetColor("_Color", originalColor);
        bossRenderer.SetPropertyBlock(propBlock);
    }

    private void Die()
    {
        isDead = true;

        if (MusicManager.Instance != null)
            MusicManager.Instance.StopBossMusic();

        if (bossHealthBarGlobal != null)
            bossHealthBarGlobal.gameObject.SetActive(false);

        // Desactivar IA del Boss 2 si existe
        var boss2Controller = GetComponent<BossLevel2>();
        if (boss2Controller != null) boss2Controller.enabled = false;

        // Desactivar NavMeshAgent
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (anim != null) anim.SetTrigger("Dead");

        StartCoroutine(RutinaMuerteBoss());
    }

    private IEnumerator RutinaMuerteBoss()
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
        else if (MapManager.Instance != null)
        {
            MapManager.Instance.AvanzarSiguienteNivel();
        }

        if (MapManager.Instance == null)
        {
            AnalyticsBridge.EnviarLevelComplete(MapManager.nivelBucle, 4);
        }

        Destroy(gameObject);
    }
}