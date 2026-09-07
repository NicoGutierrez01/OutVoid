using UnityEngine;

public class BossLootSpawner : MonoBehaviour
{
    [Header("Configuración de Drops")]
    [Tooltip("Prefab del cofre (el mismo que usa MapManager)")]
    public GameObject prefabCofre;

    public void SpawnearRecompensas()
    {
        if (prefabCofre == null)
        {
            Debug.LogWarning("No se asignó el prefabCofre en BossLootSpawner.");
            return;
        }

        Vector3 posSpawn = transform.position;

        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 500f))
        {
            posSpawn = hit.point + Vector3.up * 0.8f;
        }

        GameObject cofreObj = Instantiate(prefabCofre, posSpawn, Quaternion.identity);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            cofreObj.transform.LookAt(new Vector3(player.transform.position.x, cofreObj.transform.position.y, player.transform.position.z));
        }

        CofreRecompensa scriptCofre = cofreObj.GetComponent<CofreRecompensa>();
        if (scriptCofre != null)
        {
            scriptCofre.esCofreBoss = true;
        }
    }
}