using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Artillero : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform playerTransform;

    [Header("Configuración de Movimiento")]
    public float attackRange = 15f;  
    public float retreatRange = 7f;  // Aumenté un poquito para que empiece a retroceder antes
    public float velocidadAvance = 3.5f;
    public float velocidadRetroceso = 2.5f; // Un poco más lento al ir hacia atrás

    [Header("Configuración de Disparo")]
    public GameObject projectilePrefab; 
    public Transform firePoint;        
    public float timeBetweenShots = 2.2f; 
    public float delayDisparoAnimacion = 0.18f; 
    private float nextShotTime;

    [Header("Seguridad NavMesh")]
    public LayerMask capaObstaculos; 
    private float tiempoTrabado = 0f;

    [Header("Sistema de Visión Avanzado")]
    public float anguloVision = 90f;      
    public float rangoCercaniaCiega = 3.5f; 
    public LayerMask capaObstaculosVision;  

    [Header("Patrulla Pasiva (Wander)")]
    public float radioPatrulla = 10f;      
    public float tiempoEntrePuntos = 5f;    
    private float timerPatrulla = 0f;

    private bool haAterrizado = false;
    private bool isAlerted = false; 
    private bool estaDisparando = false;
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
            if (!isAlerted)
            {
                if (CheckVisionYProximidad())
                {
                    isAlerted = true;
                    SetAnimState(false, true); 
                }
                else
                {
                    // Si no está en combate, que el NavMesh controle a dónde mira
                    agent.updateRotation = true;
                    agent.speed = velocidadAvance;
                    ManejarPatrullaPasiva();
                    return; 
                }
            }

            // EN COMBATE: Desactivamos la rotación del NavMesh para controlar la mirada manualmente
            agent.updateRotation = false;

            // Detección de atasco
            if (agent.velocity.sqrMagnitude < 0.3f && !agent.isStopped)
            {
                tiempoTrabado += Time.deltaTime;
                if (tiempoTrabado > 10f) Destroy(gameObject);
            }
            else { tiempoTrabado = 0f; }

            float distance = Vector3.Distance(transform.position, playerTransform.position);

            // Si está reproduciendo la animación de disparo, lo frenamos un instante
            if (estaDisparando)
            {
                agent.isStopped = true;
                MirarAlJugador(15f);
                return;
            }

            // Lógica Global de Disparo (Puede disparar estando lejos, cerca o huyendo)
            if (distance <= attackRange && Time.time >= nextShotTime)
            {
                if (ShotManager.Instance == null || ShotManager.Instance.SolicitarPermisoParaDisparar())
                {
                    StartCoroutine(RutinaDisparoYRecarga());
                }
            }

            // 1. Si está muy lejos: Avanzar hacia el jugador
            if (distance > attackRange)
            {
                agent.isStopped = false;
                agent.speed = velocidadAvance;
                agent.SetDestination(playerTransform.position);
                MirarAlJugador(10f);
                
                if (estadoActualAnim != 0)
                {
                    SetAnimState(true, false); 
                    estadoActualAnim = 0;
                }
            }
            // 2. Si está demasiado cerca: Retroceder (Kiting) mirándote de frente
            else if (distance < retreatRange)
            {
                Vector3 direccionAlejarse = (transform.position - playerTransform.position).normalized;
                direccionAlejarse.y = 0;
                // Calculamos un punto unos metros detrás de él
                Vector3 destinoRetroceso = transform.position + direccionAlejarse * 3f; 

                agent.isStopped = false;
                agent.speed = velocidadRetroceso;
                agent.SetDestination(destinoRetroceso);
                
                MirarAlJugador(12f);

                if (estadoActualAnim != 2) // Usamos otro estado para forzar la animación
                {
                    SetAnimState(true, true); 
                    estadoActualAnim = 2;
                }
            }
            // 3. En rango ideal de tiro: Frenar y Apuntar
            else
            {
                agent.isStopped = true;
                MirarAlJugador(12f);
                
                if (estadoActualAnim != 1)
                {
                    SetAnimState(false, true); 
                    estadoActualAnim = 1;
                }
            }
        }
    }

    IEnumerator RutinaDisparoYRecarga()
    {
        estaDisparando = true;
        nextShotTime = Time.time + timeBetweenShots;

        if (anim != null) anim.SetTrigger("Shoot");

        yield return new WaitForSeconds(delayDisparoAnimacion);

        Disparar();

        yield return new WaitForSeconds(0.2f);

        if (anim != null) anim.SetTrigger("Recharge");

        estaDisparando = false;
    }

    void ManejarPatrullaPasiva()
    {
        timerPatrulla += Time.deltaTime;

        if (timerPatrulla >= tiempoEntrePuntos || agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            Vector3 puntoAleatorio = Random.insideUnitSphere * radioPatrulla + transform.position;
            NavMeshHit hit;

            if (NavMesh.SamplePosition(puntoAleatorio, out hit, radioPatrulla, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
            timerPatrulla = 0f;
        }

        bool moviendose = agent.velocity.sqrMagnitude > 0.1f;
        SetAnimState(moviendose, false); 
        
        if (moviendose) estadoActualAnim = 0;
        else estadoActualAnim = -1;
    }

    bool CheckVisionYProximidad()
    {
        if (playerTransform == null) return false;

        float distancia = Vector3.Distance(transform.position, playerTransform.position);

        if (distancia <= rangoCercaniaCiega) return true;

        if (distancia <= attackRange)
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

    void SetAnimState(bool walking, bool aiming)
    {
        if (anim != null)
        {
            anim.SetBool("isWalking", walking);
            anim.SetBool("isAiming", aiming);
        }
    }

    void MirarAlJugador(float velocidadRotacion = 8f)
    {
        if (playerTransform == null) return;
        Vector3 direccion = (playerTransform.position - transform.position).normalized;
        direccion.y = 0;
        if (direccion != Vector3.zero)
        {
            Quaternion rotacionMirar = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionMirar, Time.deltaTime * velocidadRotacion);
        }
    }

    void Disparar() 
    {
        if (playerTransform != null && firePoint != null)
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