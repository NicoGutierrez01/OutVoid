using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossLevel2 : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform playerTarget;

    [Header("Combate")]
    [SerializeField] private float attackRange = 2.8f;
    [SerializeField] private float cooldownBetweenAttacks = 2.5f;
    [Range(0, 100)] [SerializeField] private int comboChance = 35; // % de probabilidad de tirar el combo pesado

    private NavMeshAgent agent;
    private bool isAttacking = false;
    private float nextAttackTime = 0f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }
    }

    private void Update()
    {
        if (playerTarget == null || isAttacking) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        }
        else
        {
            // Freno y rotación suave hacia el jugador en rango
            agent.isStopped = true;
            animator.SetFloat(SpeedHash, 0f);

            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 7f);
            }

            if (Time.time >= nextAttackTime)
            {
                DecideAttackPattern();
            }
        }
    }

    private void DecideAttackPattern()
    {
        int roll = Random.Range(0, 100);

        if (roll < comboChance)
        {
            // Combo de 3 golpes: Attack -> Attack2 -> Attack3 (Fuerte)
            StartCoroutine(ExecuteAttack(2, 4.8f)); // Duración total aproximada de los 3 clips
        }
        else
        {
            // Ataque rápido: 0 (Brazo 1) o 1 (Brazo 2)
            int quickAttack = Random.Range(0, 2);
            StartCoroutine(ExecuteAttack(quickAttack, 1.7f)); // Duración de 1 clip individual
        }
    }

    private IEnumerator ExecuteAttack(int attackType, float duration)
    {
        isAttacking = true;
        agent.isStopped = true;

        animator.SetInteger(AttackTypeHash, attackType);
        animator.SetTrigger(AttackTriggerHash);

        yield return new WaitForSeconds(duration);

        // Reseteo para el próximo ciclo
        animator.SetInteger(AttackTypeHash, -1);
        nextAttackTime = Time.time + cooldownBetweenAttacks;
        isAttacking = false;
    }
}