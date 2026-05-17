using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Kamikaze : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform playerTransform;
    public PlayerMovement playerScript;

    [Header("Configuración de Ataque")]
    public float damgeAmount = 30f;
    public float attackRange = 2.5f;
    public float explosionRadius = 4f;
    public float tiempoParaExplotar = 3f;

    [Header("Efectos Visuales")]
    public Renderer kamikazeRenderer; 
    public GameObject particulasExplosion; 
    private MaterialPropertyBlock propBlock;
    private Color originalColor;

    [Header("Seguridad NavMesh")]
    public LayerMask capaObstaculos; 
    private float tiempoTrabado = 0f;

    private bool haAterrizado = false;
    private bool estaExplotando = false; 

    void Awake() 
    { 
        propBlock = new MaterialPropertyBlock(); 
    }

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerTransform = p.transform;
            playerScript = p.GetComponent<PlayerMovement>();
        }

        if (kamikazeRenderer != null) 
            originalColor = kamikazeRenderer.sharedMaterial.color;
    }

    void Update()
    {
        if (estaExplotando) return;

        if (haAterrizado && agent.enabled && agent.isOnNavMesh && playerTransform != null)
        {
            if (agent.velocity.sqrMagnitude < 0.2f)
            {
                tiempoTrabado += Time.deltaTime;
                if (tiempoTrabado > 10f) Destroy(gameObject);
            }
            else { tiempoTrabado = 0f; }

            agent.SetDestination(playerTransform.position);

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= attackRange) EmpezarExplosion();
        }
    }

    void EmpezarExplosion()
    {
        estaExplotando = true;
        agent.isStopped = true;
        StartCoroutine(RutinaExplosion());
    }

    IEnumerator RutinaExplosion()
    {
        float tiempoPasado = 0f;
        float velocidadParpadeo = 0.3f; 
        bool colorBlanco = false;

        while (tiempoPasado < tiempoParaExplotar)
        {
            if (kamikazeRenderer != null)
            {
                propBlock.SetColor("_Color", colorBlanco ? originalColor : Color.white);
                kamikazeRenderer.SetPropertyBlock(propBlock);
            }
            colorBlanco = !colorBlanco; 

            yield return new WaitForSeconds(velocidadParpadeo);
            tiempoPasado += velocidadParpadeo;

            velocidadParpadeo = Mathf.Max(0.05f, velocidadParpadeo * 0.85f);
        }

        if (particulasExplosion != null)
        {
            Instantiate(particulasExplosion, transform.position, Quaternion.identity);
        }

        Collider[] objetosAlrededor = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in objetosAlrededor)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerMovement player = hit.GetComponent<PlayerMovement>();
                if (player != null) player.TakeDamage(damgeAmount);
            }
        }
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<Kamikaze>() != null || collision.gameObject.GetComponent<Artillero>() != null) return;

        if (!haAterrizado && collision.gameObject.CompareTag("Suelo"))
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 4.0f, NavMesh.AllAreas))
            {
                if (!Physics.CheckSphere(hit.position, 0.8f, capaObstaculos))
                {
                    haAterrizado = true;
                    agent.enabled = true;
                    agent.Warp(hit.position);

                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                }
            }
        }
    }
}