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
    public float minTeleportCooldown = 8f;
    public float maxTeleportCooldown = 15f;
    private float teleportCooldown;
    private float teleportTimer = 0f;
    
    [Header("Attack Settings")]
    public float attackRange = 2.5f;
    public float damageAmount = 25f;
    public float attackCooldown = 1.5f;
    private float attackTimer = 0f;

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
        anim = GetComponentInChildren<Animator>();

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

        switch (currentState)
        {
            case State.Chase:
                ChaseBehavior();
                break;
            case State.Teleport:
                TeleportBehavior();
                break;
            case State.Attack:
                AttackBehavior();
                break;
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

        agent.SetDestination(playerTransform.position);
        anim.SetBool("isMoving", agent.velocity.sqrMagnitude > 0.1f);

        if (dist <= attackRange)
        {
            currentState = State.Attack;
        }
        else if (dist > teleportDistance && teleportTimer >= teleportCooldown)
        {
            currentState = State.Teleport;
        }
    }

    void AttackBehavior()
    {
        agent.isStopped = true;
        anim.SetBool("isMoving", false);
        anim.SetBool("isAttacking", true);

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
        {
            if (playerScript != null) playerScript.TakeDamage(damageAmount);
            attackTimer = 0f;
        }

        if (Vector3.Distance(transform.position, playerTransform.position) > attackRange + 1f)
        {
            anim.SetBool("isAttacking", false);
            agent.isStopped = false;
            currentState = State.Chase;
        }
    }

    void TeleportBehavior()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 10f + playerTransform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 10.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        
        teleportTimer = 0f;
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