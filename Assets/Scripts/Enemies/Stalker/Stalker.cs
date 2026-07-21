using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Stalker : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform playerTransform;
    public Animator anim;
    public PlayerStats playerScript;

    private enum State { Chase, Teleport, Attack };
    private State currentState = State.Chase;

    [Header("Behavior Settings")]
    public float agroRange = 15f;
    private bool isAlerted = false;

    [Header("Teleport Settings")]
    public float teleportDistance = 10f;
    public float minTeleportCooldown = 6f;
    public float maxTeleportCooldown = 12f;
    public GameObject particulasTeleport; 
    private float teleportCooldown;
    private float teleportTimer = 0f;
    private bool estaHaciendoTeleport = false;

    [Header("Attack Settings")]
    public float attackRange = 2.2f;
    public float damageAmount = 25f;
    public float attackCooldown = 1.8f;
    private bool estaAtacando = false;
    private float proximoAtaqueTime = 0f;

    [Header("Sistema de Visión Avanzado")]
    public float anguloVision = 90f;        
    public float rangoCercaniaCiega = 3.5f; 
    public LayerMask capaObstaculosVision;  

    [Header("Sistema de Patrulla Pasiva")]
    public float radioPatrulla = 12f;       
    private float tiempoSiguientePunto = 0f;
    private float esperaEnPunto = 2f;       

    private bool hasLanded = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        teleportCooldown = Random.Range(minTeleportCooldown, maxTeleportCooldown);

        agent.enabled = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerScript = playerObj.GetComponentInChildren<PlayerStats>();
        }
    }

    void Update()
    {
        if (!hasLanded || agent == null || !agent.enabled) return;

        if (!isAlerted)
        {
            if (CheckVisionYProximidad())
            {
                isAlerted = true;
                agent.isStopped = false;
            }
            else
            {
                ManejarPatrullaPasiva();
                return; 
            }
        }

        teleportTimer += Time.deltaTime;

        if (estaAtacando || estaHaciendoTeleport) return;

        switch (currentState)
        {
            case State.Chase:
                ChaseBehavior();
                break;
            case State.Teleport:
                StartCoroutine(RutinaTeleport());
                break;
            case State.Attack:
                StartCoroutine(RutinaAtaque());
                break;
        }
    }

    void MirarSuaveAlJugador(float velocidad = 8f)
    {
        if (playerTransform == null) return;
        Vector3 direccion = (playerTransform.position - transform.position).normalized;
        direccion.y = 0;
        if (direccion != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * velocidad);
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
                tiempoSiguientePunto = Time.time + esperaEnPunto + Random.Range(0f, 1.5f);
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

        if (distancia <= agroRange)
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

    void ChaseBehavior()
    {
        if (playerTransform == null || agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        agent.isStopped = false;
        agent.SetDestination(playerTransform.position);
        if (anim != null) anim.SetBool("isMoving", agent.velocity.sqrMagnitude > 0.1f);

        if (dist <= attackRange && Time.time >= proximoAtaqueTime)
        {
            currentState = State.Attack;
        }
        else if (dist > teleportDistance && teleportTimer >= teleportCooldown)
        {
            currentState = State.Teleport;
        }
    }

    IEnumerator RutinaAtaque()
    {
        estaAtacando = true;
        agent.isStopped = true;

        if (anim != null)
        {
            anim.SetBool("isMoving", false);
            anim.SetBool("isAttacking", true);
        }

        float tiempoOrientacion = 0.3f;
        float timer = 0f;
        while (timer < tiempoOrientacion)
        {
            MirarSuaveAlJugador(12f);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.15f);

        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= attackRange + 0.8f && playerScript != null)
            {
                playerScript.TakeDamage(damageAmount);
            }
        }

        yield return new WaitForSeconds(attackCooldown - 0.45f);

        if (anim != null) anim.SetBool("isAttacking", false);
        
        proximoAtaqueTime = Time.time + attackCooldown;
        estaAtacando = false;
        agent.isStopped = false;
        currentState = State.Chase;
    }

    IEnumerator RutinaTeleport()
    {
        estaHaciendoTeleport = true;
        agent.isStopped = true;

        if (particulasTeleport != null)
        {
            Instantiate(particulasTeleport, transform.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(0.2f);

        Vector3 posicionAtrasPlayer = playerTransform.position - playerTransform.forward * Random.Range(3f, 5f);
        Vector3 puntoTeleport = posicionAtrasPlayer + Random.insideUnitSphere * 2f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoTeleport, out hit, 6.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else if (NavMesh.SamplePosition(playerTransform.position, out hit, 6.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        Vector3 dirAlJugador = (playerTransform.position - transform.position).normalized;
        dirAlJugador.y = 0;
        if (dirAlJugador != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dirAlJugador);
        }

        if (particulasTeleport != null)
        {
            Instantiate(particulasTeleport, transform.position, Quaternion.identity);
        }

        teleportTimer = 0f;
        teleportCooldown = Random.Range(minTeleportCooldown, maxTeleportCooldown);

        yield return new WaitForSeconds(0.2f);

        estaHaciendoTeleport = false;
        agent.isStopped = false;
        currentState = State.Chase;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasLanded && collision.gameObject.CompareTag("Suelo"))
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 4.0f, NavMesh.AllAreas))
            {
                hasLanded = true;
                agent.enabled = true;
                agent.Warp(hit.position);
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }
        }
    }
}