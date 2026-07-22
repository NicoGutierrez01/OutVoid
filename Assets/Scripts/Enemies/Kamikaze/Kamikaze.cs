using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Kamikaze : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform playerTransform;
    public PlayerStats playerScript;
    public Animator anim;

    [Header("Configuración de Movimiento y Velocidad")]
    public float velocidadPatrulla = 3.5f;   
    public float velocidadPersecucion = 7f;    

    [Header("Configuración de Ataque")]
    public float damgeAmount = 30f;
    public float attackRange = 2.5f;
    public float explosionRadius = 4f;
    [Tooltip("Tiempo total en segundos que tarda en explotar desde que entra en rango")]
    public float tiempoParaExplotar = 1.2f; 

    [Header("Efectos Visuales")]
    private SkinnedMeshRenderer[] renderers;
    public GameObject particulasExplosion; 

    [Header("Seguridad NavMesh")]
    public LayerMask capaObstaculos; 
    private float tiempoTrabado = 0f;

    [Header("Sistema de Visión Avanzado")]
    public float anguloVision = 90f;        
    public float rangoCercaniaCiega = 3.5f; 
    public LayerMask capaObstaculosVision;  

    [Header("Sistema de Patrulla Pasiva")]
    public float radioPatrulla = 15f;      
    private float tiempoSiguientePunto = 0f;
    private float esperaEnPunto = 3f;       

    private bool haAterrizado = false;
    private bool estaExplotando = false; 
    private bool isAlerted = false; 

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
            if (!isAlerted)
            {
                if (CheckVisionYProximidad())
                {
                    isAlerted = true;
                    agent.isStopped = false;
                    agent.speed = velocidadPersecucion;
                }
                else
                {
                    agent.speed = velocidadPatrulla;
                    ManejarPatrullaPasiva();
                    return; 
                }
            }

            if (agent.velocity.sqrMagnitude < 0.2f)
            {
                tiempoTrabado += Time.deltaTime;
                if (tiempoTrabado > 10f) Destroy(gameObject);
            }
            else { tiempoTrabado = 0f; }

            agent.SetDestination(playerTransform.position);
            
            if (anim != null)
            {
                anim.SetBool("isMoving", agent.velocity.sqrMagnitude > 0.1f);
            }

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= attackRange)
            {
                EmpezarExplosion();
            }
        }
    }

    void ManejarPatrullaPasiva()
    {
        if (anim != null) anim.SetBool("isMoving", agent.velocity.sqrMagnitude > 0.1f);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            if (Time.time >= tiempoSiguientePunto)
            {
                Vector3 puntoAleatorio = ObtenerPuntoAleatorioNavMesh(transform.position, radioPatrulla);
                agent.SetDestination(puntoAleatorio);
                tiempoSiguientePunto = Time.time + esperaEnPunto + Random.Range(0f, 2f); 
            }
        }
    }

    Vector3 ObtenerPuntoAleatorioNavMesh(Vector3 centro, float radio)
    {
        Vector3 dirAleatoria = Random.insideUnitSphere * radio;
        dirAleatoria += centro;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(dirAleatoria, out hit, radio, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return centro; 
    }

    bool CheckVisionYProximidad()
    {
        if (playerTransform == null) return false;

        float distancia = Vector3.Distance(transform.position, playerTransform.position);

        if (distancia <= rangoCercaniaCiega) return true;

        if (distancia <= 15f)
        {
            Vector3 direccionAlJugador = (playerTransform.position - transform.position).normalized;
            float angulo = Vector3.Angle(transform.forward, direccionAlJugador);

            if (angulo <= anguloVision / 2f)
            {
                RaycastHit hit;
                Vector3 origenRaycast = transform.position + Vector3.up * 1f;
                Vector3 destinoRaycast = playerTransform.position + Vector3.up * 1f;

                if (Physics.Linecast(origenRaycast, destinoRaycast, out hit, capaObstaculosVision))
                {
                    if (hit.transform.CompareTag("Player")) return true;
                }
                else
                {
                    return true;
                }
            }
        }
        return false;
    }

    void EmpezarExplosion()
    {
        estaExplotando = true;

        if (anim != null) anim.SetTrigger("Explode");

        Vector3 direccionCarga = transform.forward;
        Vector3 destinoCarga = transform.position + direccionCarga * 8f; 
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destinoCarga);
            agent.speed = velocidadPersecucion;
        }

        StartCoroutine(RutinaExplosion());
    }

    IEnumerator RutinaExplosion()
    {
        float tiempoPasado = 0f;
        float velocidadParpadeo = 0.15f; 
        bool brilloActivo = false;

        while (tiempoPasado < tiempoParaExplotar)
        {
            Color colorEmision = brilloActivo ? Color.white * 8f : Color.black;

            foreach (var r in renderers)
            {
                if (r != null && r.material.HasProperty("_EmissionColor"))
                {
                    r.material.SetColor("_EmissionColor", colorEmision);
                }
            }
            
            brilloActivo = !brilloActivo; 
            yield return new WaitForSeconds(velocidadParpadeo);
            tiempoPasado += velocidadParpadeo;
            velocidadParpadeo = Mathf.Max(0.03f, velocidadParpadeo * 0.8f);
        }

        foreach (var r in renderers)
        {
            if (r != null && r.material.HasProperty("_EmissionColor"))
            {
                r.material.SetColor("_EmissionColor", Color.black);
            }
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