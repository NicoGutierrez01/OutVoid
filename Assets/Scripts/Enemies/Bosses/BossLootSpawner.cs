using UnityEngine;

public class BossLootSpawner : MonoBehaviour
{
    [Header("Configuración de Drops")]
    public GameObject prefabCofre;
    [Tooltip("Capa del suelo para asegurar que el cofre caiga en la tierra y no arriba de otro boss o portal")]
    public LayerMask capaSuelo = ~0;

    public void SpawnearRecompensas()
    {
        if (prefabCofre == null)
        {
            Debug.LogWarning("No se asignó el prefabCofre en BossLootSpawner.");
            return;
        }

        Vector3 posSpawn = transform.position;

        LayerMask mascara = (MapManager.Instance != null && MapManager.Instance.datosNivelActual != null) 
                            ? MapManager.Instance.datosNivelActual.capaSuelo 
                            : capaSuelo;

        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 50f, mascara))
        {
            posSpawn = hit.point + Vector3.up * 0.5f;
        }
        else
        {
            posSpawn = new Vector3(transform.position.x, 0.5f, transform.position.z);
        }

        GameObject cofreObj = Instantiate(prefabCofre, posSpawn, Quaternion.identity);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 target = new Vector3(player.transform.position.x, cofreObj.transform.position.y, player.transform.position.z);
            cofreObj.transform.LookAt(target);
        }

        CofreRecompensa scriptCofre = cofreObj.GetComponent<CofreRecompensa>();
        if (scriptCofre != null)
        {
            scriptCofre.esCofreBoss = true;
        }
    }
}