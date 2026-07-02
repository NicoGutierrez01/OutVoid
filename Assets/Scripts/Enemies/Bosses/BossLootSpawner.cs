using UnityEngine;
using System.Collections.Generic;

public class BossLootSpawner : MonoBehaviour
{
    [Header("Configuración de Drops")]
    public GameObject prefabItemBase;

    public List<ItemMejoraData> poolItemsLegendarios; 

    public void SpawnearRecompensas()
    {
        if (poolItemsLegendarios.Count < 3)
        {
            Debug.LogWarning("No hay suficientes ítems en la pool para spawnear 3 opciones. Creá más Scriptable Objects.");
            return;
        }

        List<ItemMejoraData> itemsDisponibles = new List<ItemMejoraData>(poolItemsLegendarios);
        for (int i = 0; i < itemsDisponibles.Count; i++)
        {
            ItemMejoraData temp = itemsDisponibles[i];
            int randomIndex = Random.Range(i, itemsDisponibles.Count);
            itemsDisponibles[i] = itemsDisponibles[randomIndex];
            itemsDisponibles[randomIndex] = temp;
        }

        string idUnicoDrop = System.Guid.NewGuid().ToString();

        Vector3[] posiciones = new Vector3[3];
        posiciones[0] = transform.position + transform.forward * 2.5f; 
        posiciones[1] = transform.position - transform.forward * 1.5f + transform.right * 2f; 
        posiciones[2] = transform.position - transform.forward * 1.5f - transform.right * 2f; 

        for (int i = 0; i < 3; i++)
        {
            Vector3 posSpawn = posiciones[i];

            if (Physics.Raycast(posSpawn + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 500f))
            {
                posSpawn = hit.point + Vector3.up * 0.5f;
            }

            GameObject nuevoItem = Instantiate(prefabItemBase, posSpawn, Quaternion.identity);
            ItemInteractable scriptInteractable = nuevoItem.GetComponent<ItemInteractable>();
            
            if (scriptInteractable != null)
            {
                scriptInteractable.Configurar(itemsDisponibles[i], idUnicoDrop);
            }
        }
    }
}