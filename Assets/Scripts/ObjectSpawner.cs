using UnityEngine;
using System.Collections; // ¡Clave para usar corrutinas!
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    public enum TipoTile { Base, Duna, Grieta }
    public TipoTile tipoDeEsteTile;

    [Header("Listas de Objetos")]
    public List<GameObject> cactus;
    public List<GameObject> piedras;
    public List<GameObject> arboles;
    public GameObject tronco;
    public GameObject pasto;
    public LayerMask capaSuelo;

    [Header("Configuración de Densidad")]
    public int intentosDeSpawn = 10; 
    public float radioTile = 24f;  

    void Start()
    {
        if (tipoDeEsteTile == TipoTile.Grieta) return;
        
        if (Mathf.Approximately(transform.position.x, 100f) && Mathf.Approximately(transform.position.z, 100f))
        {
            Debug.Log("Tile central despejado para el Player.");
            return; 
        }
        
        StartCoroutine(RutinaDeSpawn());
    }

    IEnumerator RutinaDeSpawn()
    {
        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < intentosDeSpawn; i++)
        {
            IntentarSpawnear();
        }
    }

    void IntentarSpawnear()
    {
        float randomX = Random.Range(-radioTile, radioTile);
        float randomZ = Random.Range(-radioTile, radioTile);

        Vector3 rayOrigin = transform.position + new Vector3(randomX, 50f, randomZ);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, capaSuelo))
        {
            float inclinacion = Vector3.Angle(hit.normal, Vector3.up);
            
            float seleccion = Random.value;

            if (seleccion < 0.4f) SpawnearObjeto(piedras, hit);
            else if (seleccion < 0.6f) SpawnearObjeto(cactus, hit);
            else if (seleccion < 0.7f) SpawnearPastoCluster(hit);
            else if (seleccion < 0.8f) SpawnearObjeto(arboles, hit);
            else if (seleccion < 0.85f && tipoDeEsteTile == TipoTile.Base && inclinacion < 15f) 
            {
                Instantiate(tronco, hit.point, Quaternion.Euler(-90, Random.Range(0, 360), 0), transform);
            }
        }
    }

    void SpawnearObjeto(List<GameObject> lista, RaycastHit hit)
    {
        if (lista.Count == 0) return;
        GameObject obj = Instantiate(lista[Random.Range(0, lista.Count)], hit.point, Quaternion.identity, transform);
        
        float rotX = (lista == arboles) ? 0f : -90f;

        obj.transform.rotation = Quaternion.Euler(rotX, Random.Range(0, 360), 0);
        
        if (lista == piedras) 
        {
            obj.transform.position += Vector3.down * 0.2f;
        }
    }

    void SpawnearPastoCluster(RaycastHit hitPrincipal)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 offset = (i == 0) ? Vector3.zero : new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
            Instantiate(pasto, hitPrincipal.point + offset, Quaternion.Euler(0, Random.Range(0, 360), 0), transform);
        }
    }
}