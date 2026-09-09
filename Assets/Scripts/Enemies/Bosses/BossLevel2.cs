using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossLevel2 : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform eyePoint;
    [SerializeField] private LineRenderer laserLine;

    [Header("Secuencia de Salida del Portal")]
    [SerializeField] private float exitPortalDistance = 4.5f; // Metros que avanza al salir
    [SerializeField] private float exitPortalSpeed = 3.0f;

    [Header("Combate Melee")]
    [SerializeField] private float attackRange = 3.2f;
    [SerializeField] private float meleeCooldown = 2.5f;
    [Range(0, 100)] [SerializeField] private int comboChance = 35;

    [Header("Ataque Láser")]
    [SerializeField] private float laserMinDistance = 10.0f;
    [SerializeField] private float laserCooldown = 8.0f;
    [SerializeField] private float laserDuration = 3.5f;
    [SerializeField] private float totalLaserDamage = 70f;
    [SerializeField] private float laserMaxRange = 60f;
    [SerializeField] private float laserTrackingSpeed = 1.4f;
    [SerializeField] private LayerMask hitLayers;

    private NavMeshAgent agent;
    private bool isBusy = true; // Arranca en true para bloquear combate mientras sale del portal
    private float nextMeleeTime = 0f;
    private float nextLaserTime = 0f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (laserLine != null) laserLine.enabled = false;
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        nextLaserTime = Time.time + 5f;

        // Inicia la caminata saliendo del portal
        StartCoroutine(ExitPortalRoutine());
    }

    private IEnumerator ExitPortalRoutine()
    {
        isBusy = true;
        agent.isStopped = false;
        agent.speed = exitPortalSpeed;

        // Punto unos metros hacia adelante según hacia dónde mira el portal/boss al nacer
        Vector3 targetExit = transform.position + (transform.forward * exitPortalDistance);

        // Si el punto cae sobre el NavMesh, fijamos destino
        if (NavMesh.SamplePosition(targetExit, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(targetExit);
        }

        // Camina hacia adelante hasta acercarse al punto o pasar 2.5 segundos
        float timer = 0f;
        while (timer < 2.5f && Vector3.Distance(transform.position, targetExit) > 0.8f)
        {
            timer += Time.deltaTime;
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
            yield return null;
        }

        // Habilita el combate normal
        isBusy = false;
    }

    private void Update()
    {
        if (playerTarget == null || isBusy) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance >= laserMinDistance && Time.time >= nextLaserTime)
        {
            StartCoroutine(LaserAttackRoutine());
            return;
        }

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        }
        else 
        {
            agent.isStopped = true;
            animator.SetFloat(SpeedHash, 0f);

            RotateTowardsPlayer(7f);

            if (Time.time >= nextMeleeTime)
            {
                DecideMeleePattern();
            }
        }
    }

    private void RotateTowardsPlayer(float turnSpeed)
    {
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * turnSpeed);
        }
    }

    private void DecideMeleePattern()
    {
        int roll = Random.Range(0, 100);

        if (roll < comboChance)
        {
            StartCoroutine(MeleeRoutine(2, 4.8f));
        }
        else
        {
            int quickAttack = Random.Range(0, 2);
            StartCoroutine(MeleeRoutine(quickAttack, 1.7f));
        }
    }

    private IEnumerator MeleeRoutine(int attackType, float duration)
    {
        isBusy = true;
        agent.isStopped = true;

        animator.SetInteger(AttackTypeHash, attackType);
        animator.SetTrigger(AttackTriggerHash);

        yield return new WaitForSeconds(duration);

        animator.SetInteger(AttackTypeHash, -1);
        nextMeleeTime = Time.time + meleeCooldown;
        isBusy = false;
    }

    private IEnumerator LaserAttackRoutine()
    {
        isBusy = true;
        
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        animator.SetFloat(SpeedHash, 0f);

        yield return new WaitForSeconds(0.6f);

        if (laserLine != null)
        {
            laserLine.gameObject.SetActive(true);
            laserLine.enabled = true;
        }

        float elapsed = 0f;
        float damagePerSecond = totalLaserDamage / laserDuration;

        Vector3 currentAimPoint = playerTarget.position + Vector3.up * 1.0f;

        while (elapsed < laserDuration)
        {
            elapsed += Time.deltaTime;
            agent.velocity = Vector3.zero;

            Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 2f;
            Vector3 targetAim = playerTarget.position + Vector3.up * 1.0f;

            currentAimPoint = Vector3.Lerp(currentAimPoint, targetAim, Time.deltaTime * laserTrackingSpeed);

            Vector3 bodyLookDir = (currentAimPoint - transform.position).normalized;
            bodyLookDir.y = 0;
            if (bodyLookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(bodyLookDir), Time.deltaTime * 2.0f);
            }

            Vector3 shootDirection = (currentAimPoint - origin).normalized;
            Vector3 laserEnd = origin + (shootDirection * laserMaxRange);

            if (Physics.Raycast(origin, shootDirection, out RaycastHit hit, laserMaxRange, hitLayers))
            {
                laserEnd = hit.point;

                PlayerStats playerStats = hit.collider.GetComponentInParent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(damagePerSecond * Time.deltaTime);
                }
            }

            if (laserLine != null)
            {
                laserLine.SetPosition(0, origin);
                laserLine.SetPosition(1, laserEnd);
            }

            yield return null;
        }

        if (laserLine != null)
        {
            laserLine.enabled = false;
            laserLine.gameObject.SetActive(false);
        }

        nextLaserTime = Time.time + laserCooldown;
        isBusy = false;
    }
}