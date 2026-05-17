using UnityEngine;

public class Dynamite : MonoBehaviour
{
    public GameObject prefabExplosion;
    public float tiempoParaExplotar = 3f;
    public float radioExplosion = 5f;
    public float dañoExplosion = 50f;

    void Start()
    {
        Invoke("Explotar", tiempoParaExplotar);
    }

    void Explotar()
    {
        Debug.Log("¡BOOM! La dinamita desaparece y hace daño en área.");

        Collider[] objetosAlrededor = Physics.OverlapSphere(transform.position, radioExplosion);

        foreach (Collider hit in objetosAlrededor)
        {
            EnemyHealth enemy = hit.gameObject.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(dañoExplosion);
            }

            if (hit.CompareTag("Player"))
            {
                PlayerMovement player = hit.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    player.TakeDamage(dañoExplosion);
                    Debug.Log("¡Cuidado con la onda expansiva!");
                }
            }
        }
        if (prefabExplosion != null) Instantiate(prefabExplosion, transform.position, Quaternion.identity);
        
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioExplosion);
    }
}