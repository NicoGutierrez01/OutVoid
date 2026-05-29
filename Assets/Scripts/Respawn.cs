using UnityEngine;
using UnityEngine.SceneManagement;

public class Respawn : MonoBehaviour
{
    [Header("Configuración")]
    public Vector3 puntoDeReaparicion = new Vector3(100f, 5f, 100f); 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (MapManager.bossDerrotado)
            {
                Debug.Log("Transición al siguiente bucle...");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                PlayerStats stats = other.GetComponent<PlayerStats>();

                Player playerRoot = other.GetComponentInParent<Player>();

                if (stats != null && playerRoot != null)
                {
                    float danioPorCaida = stats.maxHealth * 0.30f;
                    stats.TakeDamage(danioPorCaida);

                    playerRoot.Teleport(puntoDeReaparicion);

                    Debug.Log("¡El jugador se cayó al vacío! Reapareciendo con -30% de vida.");
                }
            }
        }
    }
}