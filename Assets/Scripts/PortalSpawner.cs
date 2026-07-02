using System.Collections;
using UnityEngine;

public class PortalSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject[] posiblesEnemigos; 

    [Header("Configuración de Oleada")]
    public int cantidadEnemigos = 5;   
    public float tiempoEntreSpawns = 2f; 
    public float radioDispersion = 3f;  
    public float distanciaCaida = 3f; 

    [Header("Tiempos de Juego")]
    public float tiempoEntreOleadas = 20f; 

    private GameObject enemigoDeEstePortal;
    
    private Renderer[] componentesVisuales;
    private Collider[] componentesFisicos;

    void Start()
    {
        componentesVisuales = GetComponentsInChildren<Renderer>();
        componentesFisicos = GetComponentsInChildren<Collider>();

        StartCoroutine(CicloDeOleadas());
    }

    void CambiarEstadoPortal(bool estado)
    {
        foreach (Renderer r in componentesVisuales) r.enabled = estado;
        foreach (Collider c in componentesFisicos) c.enabled = estado;
    }

IEnumerator CicloDeOleadas()
{
    yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("Player") != null);
    yield return new WaitForSeconds(1f);
        while (true)
        {
            if (posiblesEnemigos != null && posiblesEnemigos.Length > 0)
            {
                int indiceAlAzar = Random.Range(0, posiblesEnemigos.Length);
                enemigoDeEstePortal = posiblesEnemigos[indiceAlAzar];
                
                Debug.Log("El portal va a lanzar una oleada de: " + enemigoDeEstePortal.name);
            }

            CambiarEstadoPortal(true);
            yield return new WaitForSeconds(1f);

            for (int i = 0; i < cantidadEnemigos; i++)
            {
                Vector3 spawnOffset = new Vector3(
                    Random.Range(-radioDispersion, radioDispersion),
                    -distanciaCaida,
                    Random.Range(-radioDispersion, radioDispersion)
                );

                Vector3 posicionFinal = transform.position + spawnOffset;

                if (enemigoDeEstePortal != null)
                {
                    Instantiate(enemigoDeEstePortal, posicionFinal, Quaternion.identity);
                }

                yield return new WaitForSeconds(tiempoEntreSpawns);
            }

            CambiarEstadoPortal(false);

            yield return new WaitForSeconds(tiempoEntreOleadas);
        }
    }
}