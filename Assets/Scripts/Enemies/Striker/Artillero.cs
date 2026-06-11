using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Artillero : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform playerTransform;

    [Header("Configuración de Movimiento")]
    public float attackRange = 15f;  
    
    [Header("Configuración de Disparo")]
    public GameObject projectilePrefab; 
    public Transform firePoint;        
    public float timeBetweenShots = 2f; 
    private float nextShotTime;
    private bool isReloading = false;

    [Header("Seguridad NavMesh")]
    public LayerMask capaObstaculos; 
    private bool haAterrizado = false;
    private Animator anim;

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
        if (!haAterrizado || !agent.enabled || playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance > attackRange)
        {
            // Movimiento
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
            anim.SetBool("isMoving", true);
        }
        else
        {
            // Combate
            agent.isStopped = true;
            anim.SetBool("isMoving", false);
            MirarAlJugador();
            
            if (Time.time >= nextShotTime && !isReloading)
            {
                StartCoroutine(SecuenciaAtaque());
                nextShotTime = Time.time + timeBetweenShots;
            }
        }
    }

    IEnumerator SecuenciaAtaque()
    {
        isReloading = true;
        anim.SetTrigger("isShooting");
        
        // Disparo
        Vector3 puntoDeMira = playerTransform.position + Vector3.up * 0.5f;
        firePoint.LookAt(puntoDeMira);
        Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        yield return new WaitForSeconds(0.8f); 
        
        anim.SetTrigger("isRecharge");
        yield return new WaitForSeconds(1.2f); 
        
        isReloading = false;
    }

    void MirarAlJugador()
    {
        Vector3 direccion = (playerTransform.position - transform.position).normalized;
        direccion.y = 0; 
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccion), Time.deltaTime * 5f);
    }

    public void Morir()
    {
        anim.SetTrigger("Die");
        agent.enabled = false;
        Destroy(gameObject, 2.0f); 
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!haAterrizado && collision.gameObject.CompareTag("Suelo"))
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 4.0f, NavMesh.AllAreas))
            {
                haAterrizado = true;
                agent.enabled = true;
                agent.Warp(hit.position);
                GetComponent<Rigidbody>().isKinematic = true;
            }
        }
    }
}