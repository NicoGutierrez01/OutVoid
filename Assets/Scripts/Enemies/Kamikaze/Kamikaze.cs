using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Kamikaze : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform playerTransform;
    public PlayerStats playerScript;
    public Animator anim;

    [Header("Configuración de Ataque")]
    public float damgeAmount = 30f;
    public float attackRange = 2.5f;
    public float explosionRadius = 4f;
    public float tiempoParaExplotar = 3f;

    [Header("Efectos Visuales")]
    private SkinnedMeshRenderer[] renderers;
    public GameObject particulasExplosion; 

    [Header("Seguridad NavMesh")]
    public LayerMask capaObstaculos; 
    private float tiempoTrabado = 0f;

    private bool haAterrizado = false;
    private bool estaExplotando = false; 

    public bool estaVivo = true;


    void Start()
    {
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerTransform = p.transform;
            playerScript = p.GetComponentInParent<PlayerStats>();
        }
    }

    void Update()
    {
        if (!estaVivo || estaExplotando) return;

        if (haAterrizado && agent.enabled && agent.isOnNavMesh && playerTransform != null)
        {
            if (agent.velocity.sqrMagnitude < 0.2f)
            {
                tiempoTrabado += Time.deltaTime;
                if (tiempoTrabado > 10f) Destroy(gameObject);
            }
            else { tiempoTrabado = 0f; }

            agent.SetDestination(playerTransform.position);

            anim.SetBool("isMoving", agent.velocity.sqrMagnitude > 0.1f);

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= attackRange) EmpezarExplosion();
        }
    }

    void EmpezarExplosion()
    {
        estaExplotando = true;
        agent.isStopped = true;
        anim.SetTrigger("Explode");
        StartCoroutine(RutinaExplosion());
    }

    IEnumerator RutinaExplosion()
    {
        float tiempoPasado = 0f;
        float velocidadParpadeo = 0.3f; 
        bool brilloActivo = false;

        while (tiempoPasado < tiempoParaExplotar)
        {
            Color colorEmision = brilloActivo ? Color.white * 8f : Color.black;

            foreach (var r in renderers)
            {
                r.material.SetColor("_EmissionColor", colorEmision);
            }
            
            brilloActivo = !brilloActivo; 
            yield return new WaitForSeconds(velocidadParpadeo);
            tiempoPasado += velocidadParpadeo;
            velocidadParpadeo = Mathf.Max(0.05f, velocidadParpadeo * 0.85f);
        }

        foreach (var r in renderers) r.material.SetColor("_EmissionColor", Color.black);

        if (particulasExplosion != null)
        {
            Instantiate(particulasExplosion, transform.position, Quaternion.identity);
        }

        Collider[] objetosAlrededor = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in objetosAlrededor)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerStats player = hit.GetComponent<PlayerStats>();
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