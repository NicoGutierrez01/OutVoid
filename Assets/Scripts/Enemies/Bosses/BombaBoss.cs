using UnityEngine;

public class BombaBoss : MonoBehaviour
{
    public float dañoExplosion = 40f;
    public float radioExplosion = 5f;
    public GameObject efectoExplosion; 

    private void OnCollisionEnter(Collision collision)
    {
        Explotar();
    }

    void Explotar()
    {
        Collider[] cercanos = Physics.OverlapSphere(transform.position, radioExplosion);
        foreach (var hit in cercanos)
        {
            if (hit.CompareTag("Player"))
            {
                if (hit.TryGetComponent(out PlayerStats player)) player.TakeDamage(dañoExplosion, "BOSS");
            }
        }

        if (efectoExplosion != null) Instantiate(efectoExplosion, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}