using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Dynamite : MonoBehaviour
{
    [Header("Explosión")]
    public GameObject prefabExplosion;
    public float tiempoParaExplotar = 3f;
    [Tooltip("Radio del área de daño de la explosión")]
    public float radioExplosion = 8.5f; 
    public float dañoExplosion = 110f;

    [Header("Física de Impacto")]
    public float amortiguacionImpacto = 0.5f;

    private Rigidbody rb;
    private bool yaExploto = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Start()
    {
        Invoke(nameof(Explotar), tiempoParaExplotar);
    }

    public void InicializarLanzamiento(Vector3 direccionMirada, float fuerzaFrontal, float fuerzaArriba, Collider[] collidersPlayer = null)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        Collider propioCollider = GetComponent<Collider>();
        if (propioCollider != null && collidersPlayer != null)
        {
            foreach (Collider c in collidersPlayer)
            {
                if (c != null) Physics.IgnoreCollision(propioCollider, c, true);
            }
        }

        Vector3 impulso = (direccionMirada.normalized * fuerzaFrontal) + (Vector3.up * fuerzaArriba);
        rb.linearVelocity = impulso;
        rb.AddTorque(transform.right * 8f, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) return;

        rb.linearVelocity *= amortiguacionImpacto;
        rb.angularVelocity *= amortiguacionImpacto;
    }

    public void RecibirDisparo()
    {
        if (yaExploto) return;
        Explotar();
    }

    public void Explotar()
    {
        if (yaExploto) return;
        yaExploto = true;

        CancelInvoke(nameof(Explotar));

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayExplosion();
        }

        Collider[] objetosAlrededor = Physics.OverlapSphere(transform.position, radioExplosion);
        System.Collections.Generic.HashSet<GameObject> golpeados = new System.Collections.Generic.HashSet<GameObject>();

        foreach (Collider hit in objetosAlrededor)
        {
            if (hit.CompareTag("Enemigo") || (hit.transform.root != null && hit.transform.root.CompareTag("Enemigo")))
            {
                EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy != null && !golpeados.Contains(enemy.gameObject))
                {
                    golpeados.Add(enemy.gameObject);
                    enemy.TakeDamage(dañoExplosion, false);
                }

                Boss boss = hit.GetComponentInParent<Boss>();
                if (boss != null && !golpeados.Contains(boss.gameObject))
                {
                    golpeados.Add(boss.gameObject);
                    boss.TakeDamage(dañoExplosion);
                }

                MiniCube minion = hit.GetComponentInParent<MiniCube>();
                if (minion != null && !golpeados.Contains(minion.gameObject))
                {
                    golpeados.Add(minion.gameObject);
                    minion.TakeDamage(dañoExplosion);
                }
            }

            if (hit.CompareTag("Player"))
            {
                PlayerStats player = hit.GetComponentInParent<PlayerStats>();
                if (player != null)
                {
                    player.TakeDamage(dañoExplosion * 0.5f); 
                }
            }
        }

        if (prefabExplosion != null)
        {
            GameObject fx = Instantiate(prefabExplosion, transform.position, Quaternion.identity);
            fx.transform.localScale = Vector3.one * (radioExplosion / 4f);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioExplosion);
    }
}