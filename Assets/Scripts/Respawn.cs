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
                PlayerMovement pm = other.GetComponent<PlayerMovement>();
                if (pm != null)
                {
                    float danioPorCaida = pm.maxHealth * 0.30f;
                    
                    pm.TakeDamage(danioPorCaida);
                    
                    pm.controller.enabled = false;
                    other.transform.position = puntoDeReaparicion;
                    pm.controller.enabled = true;

                    Debug.Log("¡El jugador se cayó al vacío! Reapareciendo con -30% de vida.");
                }
            }
        }
    }
}