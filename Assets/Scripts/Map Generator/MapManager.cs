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
    #region Variables y Referencias
    [Header("Nueva Pantalla de Carga Interna (Evita congelamientos)")]
    public GameObject panelCargaEscena;
    public UnityEngine.UI.Slider barraCargaEscena;
    [Header("Nuevo Sistema de Escenario")]
    public GameObject mapaPrefab; 

    [Header("Player & Enemigos")]
    public GameObject playerPrefab, portalEnemigoPrefab;
    public LayerMask capaSuelo;

    [Header("Boss Settings")]
    public GameObject lapidaPrefab, portalBossPrefab, bossPrefab;
    public int enemigosParaJefe = 15;
    public int enemigosMuertosActuales = 0;
    private GameObject lapidaInstanciada;
    private Vector3 posicionLapida = new Vector3(95f, 0.2f, 100f);
    public Vector3 alturaPortalBoss = new Vector3(95f, 40f, 100f);
    public float retrasoSpawnBoss = 2f;

    [Header("Sistema de Rondas y Bucle")]
    public static int nivelBucle = 1;
    public static bool bossDerrotado = false;
    public int rondaActual = 1;
    public int maxRondas = 3;

    [Header("UI")]
    public TextMeshProUGUI textoProgresoMuertes;
    public GameObject popupInstrucciones, popupLapidaInvocada;

    [Header("Recompensas")]
    public GameObject prefabContenedorMejora;
    public MejoraData[] mejorasComunes, mejorasRaras, mejorasEpicas;

    private List<GameObject> portalesActivos = new List<GameObject>();
    private NavMeshSurface navSurface;
    public static MapManager Instance;

    private Vector3[] spawnPointsPlayer = new Vector3[]
    {
        new Vector3(-50f, -13f, 65f),
        new Vector3(-210f, -13f, -60f),
        new Vector3(65f, -13f, -55f)
    };

    private Vector3[] spawnPointsPortales = new Vector3[]
    {
        new Vector3(-110f, 0f, -30f),
        new Vector3(-200f, 0f, -30f),
        new Vector3(-200f, 0f, -100f),
        new Vector3(-140f, 0f, 90f),
        new Vector3(-45f, 0f, 50f),
        new Vector3(-30f, 0f, -29f),
        new Vector3(90f, 0f, -45f),
        new Vector3(180f, 0f, -90f),
        new Vector3(55f, 0f, -120f),
        new Vector3(89f, 0f, 11f),
    };

    [Header("Sistema de Decoración del Desierto")]
    public List<GameObject> prefabsArboles;
    public List<GameObject> prefabsCactus;
    public GameObject prefabPasto;
    [Space(5)]
    public int cantidadArboles = 15;
    public int cantidadCactus = 25;
    public int cantidadPasto = 40;
    #endregion

    #region Ciclo de Vida
    void Awake() 
    { 
        Instance = this; 
    }

    void Start()
    {
        bossDerrotado = false;
        navSurface = GetComponent<NavMeshSurface>();

        if (panelCargaEscena != null) panelCargaEscena.SetActive(true);
        if (barraCargaEscena != null) barraCargaEscena.value = 0.1f;

        StartCoroutine(SecuenciaDeGeneracionAsincrona());

        AnalyticsBridge.EnviarLevelStart(SessionData.level, rondaActual);
    }
    #endregion

    #region Generación Asíncrona Controlada
    System.Collections.IEnumerator SecuenciaDeGeneracionAsincrona()
    {
        yield return new WaitForSeconds(0.1f); 

        if (nivelBucle >= 4) 
        {
            rondaActual = 4;
            SessionData.level = nivelBucle;
            SessionData.round = rondaActual;
            enemigosParaJefe = 0;
            
            GenerarMapa(); 
            if (barraCargaEscena != null) barraCargaEscena.value = 0.4f;
            yield return null;

            ActualizarNavMesh();
            if (barraCargaEscena != null) barraCargaEscena.value = 0.8f;
            yield return null;
            
            int randomIndexPlayer = Random.Range(0, spawnPointsPlayer.Length);
            Instantiate(playerPrefab, spawnPointsPlayer[randomIndexPlayer], Quaternion.identity);
            
            SpawnearPortalBoss();
        }
        else 
        {
            rondaActual = 1;
            SessionData.level = nivelBucle;
            SessionData.round = rondaActual;
            ConfigurarRonda();

            GenerarMapa(); 
            if (barraCargaEscena != null) barraCargaEscena.value = 0.3f;
            yield return null;

            ActualizarNavMesh();
            if (barraCargaEscena != null) barraCargaEscena.value = 0.6f;
            yield return null;

            SpawnearElementosDeJuego();
            if (barraCargaEscena != null) barraCargaEscena.value = 0.7f;
            yield return null;

            PoblarEscenarioConDecoracion();
            if (barraCargaEscena != null) barraCargaEscena.value = 0.95f;
            yield return new WaitForSeconds(0.2f); 
        }

        if (panelCargaEscena != null) panelCargaEscena.SetActive(false);
        
        if (popupInstrucciones != null) StartCoroutine(ManejarPopupInstrucciones(3f));
    }
    #endregion

    #region Logica del Loop de Juego
    public void RegistrarMuerte()
    {
        if (rondaActual >= 4) return; 

        enemigosMuertosActuales++;
        ActualizarTextoProgreso();

        if (enemigosMuertosActuales >= enemigosParaJefe)
        {
            AnalyticsBridge.EnviarLevelComplete(SessionData.level, rondaActual);
            
            if (rondaActual < 3)
            {
                AvanzarRonda();
            }
            else
            {
                SpawnearLapida();
            }
        }
    }

    private void AvanzarRonda()
    {
        SpawnearMejoraMenor();
        rondaActual++;
        SessionData.round = rondaActual;
        ConfigurarRonda();
        AnalyticsBridge.EnviarLevelStart(SessionData.level, rondaActual);
        if (popupInstrucciones != null) StartCoroutine(ManejarPopupInstrucciones(3f));
    }

    private void InicializarRondaNormal()
    {
        rondaActual = 1;
        SessionData.level = nivelBucle;
        SessionData.round = rondaActual;
        
        ConfigurarRonda();
        GenerarMapa(); 
        ActualizarNavMesh();
        SpawnearElementosDeJuego();
        PoblarEscenarioConDecoracion();
        
        if (popupInstrucciones != null) StartCoroutine(ManejarPopupInstrucciones(3f));
    }

    private void InicializarNivelBoss()
    {
        rondaActual = 4;
        SessionData.level = nivelBucle;
        SessionData.round = rondaActual;
        enemigosParaJefe = 0;
        
        GenerarMapa(); 
        ActualizarNavMesh();
        
        int randomIndexPlayer = Random.Range(0, spawnPointsPlayer.Length);
        Instantiate(playerPrefab, spawnPointsPlayer[randomIndexPlayer], Quaternion.identity);
        
        SpawnearPortalBoss();
    }

    public void AvanzarSiguienteNivel()
    {
        AnalyticsBridge.EnviarLevelComplete(SessionData.level, 4);

        nivelBucle++; 
        SessionData.level = nivelBucle;

        if (nivelBucle > 4) 
        {
            SceneManager.LoadScene("GameOver");
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void ColapsarMapa()
    {
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        AvanzarSiguienteNivel();
    }
    #endregion

    #region Generacion y Helpers
    void ConfigurarRonda() 
    { 
        enemigosMuertosActuales = 0; 
        enemigosParaJefe = (15 * rondaActual) + ((nivelBucle - 1) * 10); 
        ActualizarTextoProgreso();
    }                                               

    void SpawnearLapida()                                                           
    {
        if (lapidaInstanciada != null) return; 

        if (nivelBucle < 4) 
        {
            SpawnearMejoraMenor();
        }

        rondaActual = 4;
        SessionData.round = rondaActual;
        
        AnalyticsBridge.EnviarLevelStart(SessionData.level, 4);

        int randomIndex = Random.Range(0, spawnPointsPlayer.Length);
        Vector3 coordenadaElegida = spawnPointsPlayer[randomIndex];
        Vector3 puntoDesdeElCielo = new Vector3(coordenadaElegida.x, 50f, coordenadaElegida.z);
        Vector3 posicionLapidaSegura = coordenadaElegida; 

        RaycastHit hit;
        if (Physics.Raycast(puntoDesdeElCielo, Vector3.down, out hit, 100f))
        {
            posicionLapidaSegura = hit.point + Vector3.up * 0.1f; 
            alturaPortalBoss = hit.point + Vector3.up * 40f;
        }
        else
        {
            alturaPortalBoss = posicionLapidaSegura + Vector3.up * 40f;
        }

        lapidaInstanciada = Instantiate(lapidaPrefab, posicionLapidaSegura, Quaternion.identity);
        DesactivarPortalesComunes();
        
        if (popupLapidaInvocada != null) StartCoroutine(ManejarPopupLapida(4f));
    }

    void SpawnearMejoraMenor()
    {
        if (prefabContenedorMejora == null) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        for (int i = 0; i < 2; i++)
        {
            float offsetX = (i == 0) ? -2f : 2f;
            Vector3 posInicial = player.transform.position + player.transform.forward * 3f + player.transform.right * offsetX + Vector3.up * 5f;
            
            Vector3 spawnPos = posInicial; 
            RaycastHit hit;
            if (Physics.Raycast(posInicial, Vector3.down, out hit, 15f, capaSuelo))
            {
                spawnPos = hit.point + Vector3.up * 0.5f; 
            }

            int rng = Random.Range(1, 101);
            MejoraData data = null;

            if (rondaActual == 1) data = (rng <= 85) ? ObtenerMejoraRandom(mejorasComunes) : ObtenerMejoraRandom(mejorasRaras);
            else if (rondaActual == 2) 
            {
                if (rng <= 50) data = ObtenerMejoraRandom(mejorasComunes);
                else if (rng <= 90) data = ObtenerMejoraRandom(mejorasRaras);
                else data = ObtenerMejoraRandom(mejorasEpicas);
            }
            else 
            {
                if (rng <= 20) data = ObtenerMejoraRandom(mejorasComunes);
                else if (rng <= 70) data = ObtenerMejoraRandom(mejorasRaras);
                else data = ObtenerMejoraRandom(mejorasEpicas);
            }

            if (data != null)
            {
                GameObject item = Instantiate(prefabContenedorMejora, spawnPos, Quaternion.identity);
                item.GetComponent<ItemMejoraDinamica>().ConfigurarItem(data);
            }
        }
    }

    MejoraData ObtenerMejoraRandom(MejoraData[] lista)
    {
        return (lista != null && lista.Length > 0) ? lista[Random.Range(0, lista.Length)] : null;
    }

    void ActualizarNavMesh()
    {
        if (navSurface != null) navSurface.BuildNavMesh();
    }

    void SpawnearElementosDeJuego()
    {
        int randomIndexPlayer = Random.Range(0, spawnPointsPlayer.Length);
        Vector3 posPlayer = spawnPointsPlayer[randomIndexPlayer];
        Instantiate(playerPrefab, posPlayer, Quaternion.identity);

        GameObject camCarga = GameObject.Find("Camera_Carga");
        if (camCarga != null)
        {
            camCarga.SetActive(false);
        }
        
        SpawnearPortales();
    }

    void SpawnearPortales()
    {
        int cantidadAIntercalar = 4;
        List<int> indicesDisponibles = new List<int>();
        for (int i = 0; i < spawnPointsPortales.Length; i++) indicesDisponibles.Add(i);

        for (int i = 0; i < cantidadAIntercalar; i++)
        {
            if (indicesDisponibles.Count == 0) break;

            int randomIndexList = Random.Range(0, indicesDisponibles.Count);
            int portalIndexElegido = indicesDisponibles[randomIndexList];
            indicesDisponibles.RemoveAt(randomIndexList);

            Vector3 posPortal = spawnPointsPortales[portalIndexElegido];
            GameObject nuevoPortal = Instantiate(portalEnemigoPrefab, posPortal, Quaternion.identity);
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

    void GenerarMapa()
    {
        if (mapaPrefab != null)
        {
            GameObject nuevoMapa = Instantiate(mapaPrefab, Vector3.zero, Quaternion.identity);
            nuevoMapa.transform.parent = this.transform;
        }
    }

    void PoblarEscenarioConDecoracion()
    {
        SpawnearObjetoDecorativo(prefabsArboles, cantidadArboles, "Árbol");
        SpawnearObjetoDecorativo(prefabsCactus, cantidadCactus, "Cactus");

        if (prefabPasto != null)
        {
            List<GameObject> listaPasto = new List<GameObject> { prefabPasto };
            SpawnearObjetoDecorativo(listaPasto, cantidadPasto, "Pasto");
        }
    }

    void SpawnearObjetoDecorativo(List<GameObject> poolPrefabs, int cantidad, string tipoNombre)
    {
        if (poolPrefabs == null || poolPrefabs.Count == 0) return;

        GameObject objetoSuelo = GameObject.Find("Ground_Baked.001");
        if (objetoSuelo == null)
        {
            Debug.LogError("No se encontró 'Ground_Baked.001' para calcular los límites de decoración.");
            return;
        }

        MeshCollider sueloCollider = objetoSuelo.GetComponent<MeshCollider>();
        if (sueloCollider == null)
        {
            Debug.LogError("'Ground_Baked.001' no tiene un MeshCollider para calcular las dimensiones.");
            return;
        }

        Bounds limitesSuelo = sueloCollider.bounds;

        int creados = 0;
        int intentos = 0;
        int intentosMaximos = cantidad * 15; 

        while (creados < cantidad && intentos < intentosMaximos)
        {
            intentos++;

            float randomX = Random.Range(limitesSuelo.min.x, limitesSuelo.max.x);
            float randomZ = Random.Range(limitesSuelo.min.z, limitesSuelo.max.z);

            Vector3 origenRaycast = new Vector3(randomX, limitesSuelo.max.y + 30f, randomZ);

            RaycastHit hit;
            if (Physics.Raycast(origenRaycast, Vector3.down, out hit, 150f))
            {
                if (hit.collider.name == "Ground_Baked.001")
                {
                    GameObject prefabElegido = poolPrefabs[Random.Range(0, poolPrefabs.Count)];
                    
                    Vector3 rotacionOriginal = prefabElegido.transform.rotation.eulerAngles;
                    Quaternion rotacionFinal = Quaternion.Euler(rotacionOriginal.x, Random.Range(0f, 360f), rotacionOriginal.z);

                    GameObject deco = Instantiate(prefabElegido, hit.point, rotacionFinal);
                    deco.transform.parent = this.transform; 
                    
                    creados++;
                }
            }
        }
        Debug.Log($"[Decoración] Se instanciaron {creados} / {cantidad} objetos de tipo {tipoNombre} distribuidos por el mapa.");
    }
    #endregion

    #region Corrutinas y UI
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

    System.Collections.IEnumerator ManejarPopupInstrucciones(float tiempo)
    {
        if (popupInstrucciones == null) yield break;
        popupInstrucciones.SetActive(true);

        var textoComponente = popupInstrucciones.GetComponentInChildren<TextMeshProUGUI>();
        if (textoComponente != null)
        {
            textoComponente.text = $"LEVEL {nivelBucle} - RONDA {rondaActual}";
        }

        yield return new WaitForSeconds(tiempo);
        popupInstrucciones.SetActive(false);
    }

    System.Collections.IEnumerator ManejarPopupLapida(float tiempo)
    {
        if (popupLapidaInvocada == null) yield break;
        popupLapidaInvocada.SetActive(true);
        yield return new WaitForSeconds(tiempo);
        popupLapidaInvocada.SetActive(false);
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

    void ActualizarTextoProgreso()
    {
        if (textoProgresoMuertes != null)
        {
            if (rondaActual == 4)
            {
                textoProgresoMuertes.text = "¡Derrota al Jefe!";
            }
            else
            {
                int muertesVisuales = Mathf.Min(enemigosMuertosActuales, enemigosParaJefe);
                textoProgresoMuertes.text = $"Matados: {muertesVisuales} / {enemigosParaJefe}";
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
    #endregion
}