using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PortalSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject[] posiblesEnemigos;

    [Header("Configuración de Oleada")]
    public int cantidadEnemigosNivel1 = 5; 
    public int cantidadEnemigosNivel2 = 2; 
    public float tiempoEntreSpawns = 2f;
    public float radioDispersion = 2f;
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
            }

            if (MapManager.Instance != null)
            {
                Vector3 nuevaPos = MapManager.Instance.ObtenerPosicionAleatoriaPortal(transform.position);
                transform.position = nuevaPos;

                if (MapManager.nivelBucle == 2)
                {
                    Vector3 dirHaciaCentro = (Vector3.zero - nuevaPos).normalized;
                    dirHaciaCentro.y = 0;
                    if (dirHaciaCentro != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(dirHaciaCentro) * Quaternion.Euler(-90f, 0f, 0f);
                    }
                }
            }

            CambiarEstadoPortal(true);
            yield return new WaitForSeconds(1f);

            int totalEnemigos = (MapManager.nivelBucle == 2) ? cantidadEnemigosNivel2 : cantidadEnemigosNivel1;

            for (int i = 0; i < totalEnemigos; i++)
            {
                Vector3 posicionFinal;
                Quaternion rotacionFinal = Quaternion.identity;

                if (MapManager.nivelBucle == 1)
                {
                    Vector3 spawnOffset = new Vector3(
                        Random.Range(-radioDispersion, radioDispersion),
                        -distanciaCaida,
                        Random.Range(-radioDispersion, radioDispersion)
                    );
                    posicionFinal = transform.position + spawnOffset;
                }
                else
                {
                    Vector3 direccionSalida = (Vector3.zero - transform.position).normalized;
                    direccionSalida.y = 0;

                    Vector3 lateral = Vector3.Cross(Vector3.up, direccionSalida);
                    Vector3 offsetFrontal = (direccionSalida * 1.5f) + (lateral * Random.Range(-radioDispersion * 0.5f, radioDispersion * 0.5f));
                    
                    posicionFinal = transform.position + offsetFrontal;

                    if (NavMesh.SamplePosition(posicionFinal, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
                    {
                        posicionFinal = hit.position;
                    }

                    if (direccionSalida != Vector3.zero)
                    {
                        rotacionFinal = Quaternion.LookRotation(direccionSalida);
                    }
                }

                if (enemigoDeEstePortal != null)
                {
                    GameObject nuevoEnemigo = Instantiate(enemigoDeEstePortal, posicionFinal, rotacionFinal);

                    NavMeshAgent agent = nuevoEnemigo.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.Warp(posicionFinal);
                    }
                }

                yield return new WaitForSeconds(tiempoEntreSpawns);
            }

            CambiarEstadoPortal(false);
            yield return new WaitForSeconds(tiempoEntreOleadas);
        }
    }

    private void OnDisable()
    {
        if (MapManager.Instance != null)
        {
            MapManager.Instance.LiberarPosicionPortal(transform.position);
        }
    }
}