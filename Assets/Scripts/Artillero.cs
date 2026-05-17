using UnityEngine;
using UnityEngine.AI;

public class Artillero : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform playerTransform;

    [Header("Configuración de Movimiento")]
    public float attackRange = 15f;  
    public float retreatRange = 5f;  
    
    [Header("Configuración de Disparo")]
    public GameObject projectilePrefab; 
    public Transform firePoint;        
    public float timeBetweenShots = 2f; 
    private float nextShotTime;

    [Header("Seguridad NavMesh")]
    public LayerMask capaObstaculos; 
    private float tiempoTrabado = 0f;

    private bool haAterrizado = false;
    private Animator anim;

    private int estadoActualAnim = -1; 

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (haAterrizado && agent.enabled && agent.isOnNavMesh && playerTransform != null)
        {
            if (agent.velocity.sqrMagnitude < 0.2f && !agent.isStopped)
            {
                tiempoTrabado += Time.deltaTime;
                if (tiempoTrabado > 10f) Destroy(gameObject);
            }
            else { tiempoTrabado = 0f; }

            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance > attackRange)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);
                
                if (estadoActualAnim != 0)
                {
                    SetAnimState(true, false);
                    estadoActualAnim = 0;
                }
            }
            else
            {
                agent.isStopped = true;
                MirarAlJugador();
                
                if (estadoActualAnim != 1)
                {
                    SetAnimState(false, true);
                    estadoActualAnim = 1;
                }

                if (Time.time >= nextShotTime)
                {
                    Disparar();
                    nextShotTime = Time.time + timeBetweenShots;
                }
            }
        }
    }

    void SetAnimState(bool walking, bool aiming)
    {
        if (anim != null)
        {
            anim.SetBool("isWalking", walking);
            anim.SetBool("isAiming", aiming);
        }
    }

    void MirarAlJugador()
    {
        Vector3 direccion = (playerTransform.position - transform.position).normalized;
        direccion.y = 0; 
        Quaternion rotacionMirar = Quaternion.LookRotation(direccion);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionMirar, Time.deltaTime * 5f);
    }

    void Disparar() 
    {
        if (playerTransform != null)
        {
            Vector3 puntoDeMira = playerTransform.position + Vector3.up * 0.5f;
            firePoint.LookAt(puntoDeMira);
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        }
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