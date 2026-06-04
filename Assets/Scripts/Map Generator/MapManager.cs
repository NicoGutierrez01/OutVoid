using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Analytics;
using static StaticVariables;
using static EventManager;

public class MapManager : MonoBehaviour
{
    [Header("Ajustes del Mapa")]
    public int gridWidth = 5; 
    public int gridheaight = 5;
    public float tileSize = 50f;

    [Header("Prefabs de Suelo Base")]
    public GameObject tileBaseLiso;
    public GameObject tileBaseRugoso;

    [Header("Lista de Tiles")]
    public List<GameObject> tilesDunes;
    public List<GameObject> tilesRifts;

    [Header("Prefab de Montañas Único")]
    public GameObject prefabAnilloMontañas;
    public Vector3 positionMontains;

    [Header("Probabilidades (0 a 1)")]
    [Range(0f, 1f)] public float chanceRugoso = 0.8f;
    [Range(0f, 1f)] public float chanceDunes = 0.4f;
    [Range(0f, 1f)] public float chanceRifts = 0.1f;

    [Header("Player & Enemys")]
    public GameObject playerPrefab;
    public GameObject portalEnemigoPrefab;
    public LayerMask capaSuelo;

    [Header("Configuración del Boss")]
    public GameObject lapidaPrefab;
    public int enemigosParaJefe = 15;
    public int contadorMuertes = 0;
    private GameObject lapidaInstanciada;
    private Vector3 posicionLapida = new Vector3(95f, 0.2f, 100f);

    [Header("Boss Portal Settings")]
    public GameObject portalBossPrefab; 
    public Vector3 alturaPortalBoss = new Vector3(95f, 40f, 100f);

    [Header("Gestión de Portales")]
    private List<GameObject> portalesActivos = new List<GameObject>();

    [Header("Ajustes del Boss")]
    public GameObject bossPrefab; 
    public float retrasoSpawnBoss = 2f;

    [Header("Sistema de Bucle")]
    public static int nivelBucle = 1; 
    public static bool bossDerrotado = false;

    [Header("UI y Progreso")]
    public TextMeshProUGUI textoProgresoMuertes;
    public int enemigosMuertosActuales = 0;

    [Header("UI de Objetivos (Pop-ups temporizados)")]
    public GameObject popupInstrucciones;  
    public GameObject popupLapidaInvocada;

    [Header("Sistema de Rondas")]
    public int rondaActual = 1;
    public int maxRondas = 3;

    public static MapManager Instance;
    private bool[,] mapaDeGrietas;
    private NavMeshSurface navSurface;

    void Start()
    {
        bossDerrotado = false;
        rondaActual = 1; 
        
        SessionData.level = nivelBucle;
        SessionData.round = rondaActual;

        navSurface = GetComponent<NavMeshSurface>();
        mapaDeGrietas = new bool[gridWidth, gridheaight];

        if (nivelBucle >= 4)
        {
            enemigosParaJefe = 0;
            enemigosMuertosActuales = 0;
            ActualizarTextoProgreso();
            
            GenerarMapa();
            ActualizarNavMesh();

            Vector3 posPlayer = new Vector3(2 * tileSize, 2f, 2 * tileSize);
            Instantiate(playerPrefab, posPlayer, Quaternion.identity);
            SpawnearPortalBoss(); 
            
            Debug.Log("¡Nivel 4 alcanzado! Batalla final iniciada.");
        }
        else
        {
            ConfigurarRonda(); 
            GenerarMapa();
            ActualizarNavMesh();
            SpawnearElementosDeJuego();
        }
        
        Debug.Log($"Evento 'LevelStart' - Iniciando Nivel {SessionData.level}, Ronda {SessionData.round}");

        LevelStartEvent levelStart = new LevelStartEvent
        {
            level = SessionData.level,
            round = SessionData.round
        };

        AnalyticsService.Instance.RecordEvent(levelStart);

        if (nivelBucle == 1 && popupInstrucciones != null) 
        {
            StartCoroutine(ManejarPopupInstrucciones(5f));
        }
        else if (popupInstrucciones != null) 
        {
            popupInstrucciones.SetActive(false);
        }

        if (popupLapidaInvocada != null) popupLapidaInvocada.SetActive(false);
    }

    void ConfigurarRonda()
    {
        enemigosMuertosActuales = 0;

        int enemigosBase = 15 + ((nivelBucle - 1) * 10);
        enemigosParaJefe = enemigosBase * rondaActual; 
        
        ActualizarTextoProgreso();
        Debug.Log($"Iniciando Ronda {rondaActual}. Objetivo: {enemigosParaJefe} enemigos.");
    }

    void AvanzarRonda()
    {
        LevelCompleteEvent levelComplete = new LevelCompleteEvent
        {
            level = SessionData.level,
            round = SessionData.round,
            time = Mathf.FloorToInt(GameTimer.tiempoTotal)
        };
        AnalyticsService.Instance.RecordEvent(levelComplete);
        Debug.Log($"[Analytics] Ronda Completada - Nivel {SessionData.level}, Ronda {SessionData.round}");

        rondaActual++;
        SessionData.round = rondaActual;

        ConfigurarRonda();

        LevelStartEvent levelStart = new LevelStartEvent
        {
            level = SessionData.level,
            round = SessionData.round
        };
        AnalyticsService.Instance.RecordEvent(levelStart);
        Debug.Log($"[Analytics] Nueva Ronda Iniciada - Nivel {SessionData.level}, Ronda {SessionData.round}");

        if (popupInstrucciones != null) 
        {
            StartCoroutine(ManejarPopupInstrucciones(3f));
        }
    }

    System.Collections.IEnumerator ManejarPopupInstrucciones(float tiempo)
    {
        popupInstrucciones.SetActive(true);
        yield return new WaitForSeconds(tiempo);
        popupInstrucciones.SetActive(false);
    }

    System.Collections.IEnumerator ManejarPopupLapida(float tiempo)
    {
        popupLapidaInvocada.SetActive(true);
        yield return new WaitForSeconds(tiempo);
        popupLapidaInvocada.SetActive(false);
    }

    public void RegistrarMuerte()
    {
        enemigosMuertosActuales++;
        ActualizarTextoProgreso();

        if (textoProgresoMuertes != null)
        {
            StopCoroutine("AnimacionPopUp"); 
            StartCoroutine("AnimacionPopUp");
        }

        if (enemigosMuertosActuales >= enemigosParaJefe)
        {
            if (rondaActual < maxRondas)
            {
                AvanzarRonda();
            }
            else if (lapidaInstanciada == null) 
            {
                SpawnearLapida();
            }
        }
    }

    void ActualizarTextoProgreso()
    {
        if (textoProgresoMuertes != null)
        {
            int muertesVisuales = Mathf.Min(enemigosMuertosActuales, enemigosParaJefe);
            textoProgresoMuertes.text = "Matados: " + muertesVisuales + " / " + enemigosParaJefe;
        }
    }

    void SpawnearLapida()
    {
        if (lapidaPrefab != null)
        {
            lapidaInstanciada = Instantiate(lapidaPrefab, posicionLapida, Quaternion.identity);
            DesactivarPortalesComunes(); 

            if (popupInstrucciones != null) popupInstrucciones.SetActive(false);
            if (popupLapidaInvocada != null) 
            {
                StartCoroutine(ManejarPopupLapida(2f));
            }
        }
    }

    public void SpawnearPortalBoss()
    {
        if (portalBossPrefab != null)
        {
            if (popupLapidaInvocada != null) popupLapidaInvocada.SetActive(false);

            GameObject portal = Instantiate(portalBossPrefab, alturaPortalBoss, Quaternion.identity);
            portal.transform.localScale = Vector3.one * 5f; 
            StartCoroutine(SecuenciaSpawnBoss(portal));
        }
    }

    void GenerarMapa()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridheaight; z++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize);
                GameObject prefabAInstanciar = SeleccionarTileInterno(x, z);
                
                float[] rotaciones = { 0f, 90f, 180f, 270f };
                float rotY = rotaciones[Random.Range(0, rotaciones.Length)];

                GameObject nuevoTile = Instantiate(prefabAInstanciar, pos, Quaternion.identity);
                nuevoTile.transform.localRotation = Quaternion.Euler(-90, rotY, 0);
                nuevoTile.transform.parent = this.transform;
            }
        }

        if (prefabAnilloMontañas != null)
        {
            GameObject limites = Instantiate(prefabAnilloMontañas, positionMontains, Quaternion.identity);
            limites.transform.parent = this.transform;
        }
    }

    GameObject SeleccionarTileInterno(int x, int z)
    {
        float valorAleatorio = Random.value;
        bool esCentroPlayer = (x == 2 && z == 2);
        bool esBajoPortal = (x == 1 && z == 1) || (x == 4 && z == 1) || (x == 1 && z == 4) || (x == 4 && z == 4);

        if (esCentroPlayer || esBajoPortal)
        {
            return (Random.value < chanceRugoso) ? tileBaseRugoso : tileBaseLiso;
        }

        if (valorAleatorio < chanceRifts && !HayGrietaCerca(x, z))
        {
            mapaDeGrietas[x, z] = true;
            return tilesRifts[Random.Range(0, tilesRifts.Count)];
        }
        else if (valorAleatorio < chanceDunes + chanceRifts)
        {
            return tilesDunes[Random.Range(0, tilesDunes.Count)];
        }
        else
        {
            return (Random.value < chanceRugoso) ? tileBaseRugoso : tileBaseLiso;
        }
    }

    bool HayGrietaCerca(int x, int z)
    {
        if (x > 0 && mapaDeGrietas[x - 1, z]) return true;
        if (z > 0 && mapaDeGrietas[x, z - 1]) return true;
        if (x > 0 && z > 0 && mapaDeGrietas[x - 1, z - 1]) return true;
        return false;
    }

    void ActualizarNavMesh()
    {
        if (navSurface != null) navSurface.BuildNavMesh();
    }

    void SpawnearElementosDeJuego()
    {
        Vector3 posPlayer = new Vector3(2* tileSize, 2f, 2 * tileSize);
        Instantiate(playerPrefab, posPlayer, Quaternion.identity);
        SpawnearPortales();
    }

    void SpawnearPortales()
    {
        Vector3[] puntosExactos = new Vector3[]
        {
            new Vector3(25f, 15f, 25f),   
            new Vector3(25f, 15f, 175f),  
            new Vector3(175f, 15f, 175f), 
            new Vector3(175f, 15f, 25f)   
        };

        foreach (Vector3 pos in puntosExactos)
        {
            GameObject nuevoPortal = Instantiate(portalEnemigoPrefab, pos, Quaternion.identity);
            portalesActivos.Add(nuevoPortal); 
        }
    }

    public void DesactivarPortalesComunes()
    {
        foreach (GameObject portal in portalesActivos)
        {
            if (portal != null) portal.SetActive(false); 
        }
        portalesActivos.Clear(); 
    }

    void Awake()
    {
        Instance = this; 
    }

    System.Collections.IEnumerator AnimacionPopUp()
    {
        Vector3 escalaOriginal = Vector3.one;
        textoProgresoMuertes.transform.localScale = Vector3.one * 1.4f; 
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f; 
            textoProgresoMuertes.transform.localScale = Vector3.Lerp(Vector3.one * 1.4f, escalaOriginal, t);
            yield return null;
        }
    }

    System.Collections.IEnumerator SecuenciaSpawnBoss(GameObject portal)
    {
        yield return new WaitForSeconds(retrasoSpawnBoss);
        if (bossPrefab != null)
        {
            Vector3 posBoss = alturaPortalBoss + Vector3.down * 2f;
            Instantiate(bossPrefab, posBoss, Quaternion.identity);
        }
        yield return new WaitForSeconds(1f);
        Destroy(portal); 
    }

    public void ColapsarMapa()
    {
        bossDerrotado = true; 

        LevelCompleteEvent levelComplete = new LevelCompleteEvent
        {
            level = SessionData.level,
            round = SessionData.round,

            time = Mathf.FloorToInt(GameTimer.tiempoTotal) 
        };

        AnalyticsService.Instance.RecordEvent(levelComplete);
        Debug.Log($"Evento 'LevelComplete' enviado. Nivel: {SessionData.level}, Tiempo: {Mathf.FloorToInt(GameTimer.tiempoTotal)}s");

        nivelBucle++; 
        Debug.Log($"Avanzando al Nivel/Bucle {nivelBucle}. Recargando mapa...");

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}