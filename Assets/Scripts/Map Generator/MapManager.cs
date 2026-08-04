using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Analytics;
using static StaticVariables;
using static EventManager;

public enum TipoObjetivo
{
    EliminarEnemigos,
    DefenderZona
}

public class MapManager : MonoBehaviour
{
    #region Variables y Referencias
    [Header("Oredn de Escenas")]
    public string[] ordenDeNiveles = new string[] { "Desert", "Forest"};

    [Header("Datos del Nivel (el cartucho)")]
    public DatosDeNivel datosNivelActual;

    [Header("Sistema de Objetivos")]
    public TipoObjetivo objetivoActual;

    [Header("Logica Objetivo 1: Eliminar Enemigos")]
    public int enemigosParaJefe = 15;
    public int enemigosMuertosActuales = 0;

    [Header("Logica Objetivo 2: Defender Zona")]
    public float tiempoDefensa = 60f;
    public float tiempoDefensaActual = 0f;

    [Header("Sistema de Rondas y Bucles")]
    public static int nivelBucle = 1;
    public static bool bossDerrotado = false;
    public int rondaActual = 1;
    public int maxRondas = 4;
    public float retrasoSpawnBoss = 2f;

    [Header("UI y Pantallas de Carga")]
    public GameObject panelCargaEscena;
    public UnityEngine.UI.Slider barraCargaEscena;
    public TextMeshProUGUI textoProgresoMuertes;
    public GameObject popupInstrucciones;
    public GameObject popupLapidaInvocada;

    [Header("Recompensas Globales")]
    public GameObject prefabContenedorMejora;
    public MejoraData[] mejorasComunes;
    public MejoraData[] mejorasRaras;
    public MejoraData[] mejorasEpicas;

    private GameObject zonaDefensaInstanciada;
    private GameObject lapidaInstanciada;
    private Vector3 alturaPortalBossDinamica; 
    private List<GameObject> portalesActivos = new List<GameObject>();
    private NavMeshSurface navSurface;
    
    public static MapManager Instance;
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

        if (nivelBucle == 1)
        {
            objetivoActual = TipoObjetivo.EliminarEnemigos;
        }
        else if (nivelBucle == 2)
        {
            objetivoActual = TipoObjetivo.DefenderZona;
        }

        if (panelCargaEscena != null) panelCargaEscena.SetActive(true);
        if (barraCargaEscena != null) barraCargaEscena.value = 0;

        StartCoroutine(SecuenciaDeGeneracionAsincrona());

        AnalyticsBridge.EnviarLevelStart(SessionData.level, rondaActual);
    }

    void Update()
    {
        if (objetivoActual == TipoObjetivo.DefenderZona && rondaActual < maxRondas)
        {
            if (ZonaDefensa.jugadorEnZona)
            {
                tiempoDefensaActual -= Time.deltaTime; 

                if (tiempoDefensaActual < 0) 
                {
                    tiempoDefensaActual = 0;
                }

                ActualizarTextoProgreso();

                if (tiempoDefensaActual <= 0)
                {
                    ZonaDefensa.jugadorEnZona = false; 
                    
                    CompletarObjetivoRonda();
                }
            }
        }
    }
    #endregion

    #region Generacion Asíncrona Controlada
    System.Collections.IEnumerator SecuenciaDeGeneracionAsincrona()
    {
        yield return new WaitForSeconds(0.1f);

        if (rondaActual < maxRondas)
        {
            SessionData.level = nivelBucle;
            SessionData.round = rondaActual;

            ConfigurarRondaPorObjetivo();

            GenerarMapa();
            if (barraCargaEscena != null) barraCargaEscena.value = 0.3f;
            yield return null;

            ActualizarNavMesh();
            if (barraCargaEscena != null) barraCargaEscena.value = 0.5f;
            yield return null;

            SpawnearJugador();
            SpawnearPortales();

            if (barraCargaEscena != null) barraCargaEscena.value = 0.7f;
            yield return null;

            PoblarEscenarioConDecoracion();
            if (barraCargaEscena != null) barraCargaEscena.value = 0.95f;
            yield return new WaitForSeconds(0.2f);
        }

        else
        {
            SessionData.level = nivelBucle;
            SessionData.round = rondaActual;
            enemigosParaJefe = 0;

            GenerarMapa();
            if (barraCargaEscena != null) barraCargaEscena.value = 0.3f;
            yield return null;

            ActualizarNavMesh();
            if (barraCargaEscena != null) barraCargaEscena.value = 0.5f;
            yield return null;

            SpawnearJugador();
            SpawnearPortales();
        }

        if (panelCargaEscena != null) panelCargaEscena.SetActive(false);
        if (popupInstrucciones != null) StartCoroutine(ManejarPopupInstrucciones(3f));
    }
    #endregion

    #region Generacion y Helpers
    void GenerarMapa()
    {
        if (datosNivelActual != null && datosNivelActual.mapaPrefab != null)
        {
            GameObject nuevoMapa = Instantiate(datosNivelActual.mapaPrefab, Vector3.zero, Quaternion.identity);
            nuevoMapa.transform.parent = this.transform;
        }
    }

    void ActualizarNavMesh()
    {
        if (navSurface != null) navSurface.BuildNavMesh();
    }

    void SpawnearJugador()
    {
        if (datosNivelActual == null || datosNivelActual.spawnPointsPlayer.Length == 0) return;

        int randomIndex = Random.Range(0, datosNivelActual.spawnPointsPlayer.Length);
        Instantiate(datosNivelActual.playerPrefab, datosNivelActual.spawnPointsPlayer[randomIndex], Quaternion.identity);

        GameObject camCarga = GameObject.Find("Camera_Carga");
        if (camCarga != null) camCarga.SetActive(false);
    }

    void SpawnearPortales()
    {
        if (datosNivelActual == null || datosNivelActual.spawnPointsPortales.Length == 0) return;

        int cantidadAIntercalar = 4;
        List<int> indicesDisponibles = new List<int>();
        for (int i = 0; i < datosNivelActual.spawnPointsPortales.Length; i++) 
        {
            indicesDisponibles.Add(i);
        }

        for (int i = 0; i < cantidadAIntercalar; i++)
        {
            if (indicesDisponibles.Count == 0) break;

            int randomIndexList = Random.Range(0, indicesDisponibles.Count);
            int portalIndex = indicesDisponibles[randomIndexList];
            indicesDisponibles.RemoveAt(randomIndexList);

            Vector3 posPortal = datosNivelActual.spawnPointsPortales[portalIndex];
            GameObject nuevoPortal = Instantiate(datosNivelActual.portalEnemigoPrefab, posPortal, Quaternion.identity);
            portalesActivos.Add(nuevoPortal);
        }
    }

    void PoblarEscenarioConDecoracion()
    {
        if (datosNivelActual == null) return;

        SpawnearObjetoDecorativo(datosNivelActual.prefabsDecoracionPrincipal, datosNivelActual.cantidadDecoracionPrincipal);
        SpawnearObjetoDecorativo(datosNivelActual.prefabsDecoracionSecundaria, datosNivelActual.cantidadDecoracionSecundaria);

        if (datosNivelActual.prefabDecoracionSuelo != null)
        {
            List<GameObject> listaPasto = new List<GameObject> { datosNivelActual.prefabDecoracionSuelo };
            SpawnearObjetoDecorativo(listaPasto, datosNivelActual.cantidadDecoracionSuelo);
        }
    }

    void SpawnearObjetoDecorativo(List<GameObject> poolPrefabs, int cantidad)
    {
        if (poolPrefabs == null || poolPrefabs.Count == 0) return;

        GameObject objetoSuelo = GameObject.Find("Ground_Baked.001");
        if (objetoSuelo == null) return;

        MeshCollider sueloCollider = objetoSuelo.GetComponent<MeshCollider>();
        if (sueloCollider == null) return;

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
    }

    void ConfigurarRondaPorObjetivo()
    {
        if (objetivoActual == TipoObjetivo.EliminarEnemigos)
        {
            enemigosMuertosActuales = 0; 
            enemigosParaJefe = (15 * rondaActual) + ((nivelBucle - 1) * 10);
            
            if (zonaDefensaInstanciada != null) 
            {
                Destroy(zonaDefensaInstanciada);
            }
        }
        else if (objetivoActual == TipoObjetivo.DefenderZona)
        {
            tiempoDefensaActual = tiempoDefensa + ((rondaActual - 1) * 30f);
            
            if (zonaDefensaInstanciada == null && datosNivelActual != null && datosNivelActual.zonaDefensaPrefab != null)
            {
                Vector3 posicionZona = Vector3.zero;
                
                if (datosNivelActual.spawnPointsZonas != null && datosNivelActual.spawnPointsZonas.Length > 0)
                {
                    posicionZona = datosNivelActual.spawnPointsZonas[Random.Range(0, datosNivelActual.spawnPointsZonas.Length)];
                }
                
                zonaDefensaInstanciada = Instantiate(datosNivelActual.zonaDefensaPrefab, posicionZona, Quaternion.identity);
                zonaDefensaInstanciada.transform.parent = this.transform; 
            }
        }
        
        ActualizarTextoProgreso();
    }

    public void SpawnearPortalBoss()
    {
        if (datosNivelActual == null || datosNivelActual.portalBossPrefab == null) return;

        Vector3 posPortalBoss = Vector3.zero;
        if (datosNivelActual.spawnPointsPortalBoss.Length > 0)
        {
            posPortalBoss = datosNivelActual.spawnPointsPortalBoss[Random.Range(0, datosNivelActual.spawnPointsPortalBoss.Length)];
        }

        if (popupLapidaInvocada != null) popupLapidaInvocada.SetActive(false);

        GameObject portal = Instantiate(datosNivelActual.portalBossPrefab, posPortalBoss, Quaternion.identity);
        portal.transform.localScale = Vector3.one * 5f; 
        
        StartCoroutine(SecuenciaSpawnBoss(portal, posPortalBoss));
    }

    System.Collections.IEnumerator SecuenciaSpawnBoss(GameObject portal, Vector3 posicionPortal)
    {
        yield return new WaitForSeconds(retrasoSpawnBoss);
        if (datosNivelActual.bossPrefab != null)
        {
            Vector3 posBoss = posicionPortal + Vector3.down * 2f;
            Instantiate(datosNivelActual.bossPrefab, posBoss, Quaternion.identity);
        }
        yield return new WaitForSeconds(1f);
        Destroy(portal); 
    }
    #endregion

    #region Lógica del Loop de Juego y Recompensas
    public void RegistrarMuerte()
    {
        if (rondaActual >= maxRondas) return; 

        if (objetivoActual == TipoObjetivo.EliminarEnemigos)
        {
            enemigosMuertosActuales++;
            ActualizarTextoProgreso();

            if (enemigosMuertosActuales >= enemigosParaJefe)
            {
                CompletarObjetivoRonda();
            }
        }
    }

    public void CompletarObjetivoRonda()
    {
        AnalyticsBridge.EnviarLevelComplete(SessionData.level, rondaActual);
            
        if (rondaActual < (maxRondas - 1)) 
        {
            AvanzarRonda();
        }
        else 
        {
            SpawnearLapida();
        }
    }

    private void AvanzarRonda()
    {
        SpawnearMejoraMenor();
        rondaActual++;
        SessionData.round = rondaActual;
        
        ConfigurarRondaPorObjetivo();
        
        AnalyticsBridge.EnviarLevelStart(SessionData.level, rondaActual);
        if (popupInstrucciones != null) StartCoroutine(ManejarPopupInstrucciones(3f));
    }

    public void AvanzarSiguienteNivel()
    {
        AnalyticsBridge.EnviarLevelComplete(SessionData.level, maxRondas);

        nivelBucle++; 
        SessionData.level = nivelBucle;

        int indiceSiguienteMapa = nivelBucle - 1;

        if (indiceSiguienteMapa >= ordenDeNiveles.Length) 
        {
            SceneManager.LoadScene("GameOver");
        }
        else
        {
            SceneManager.LoadScene(ordenDeNiveles[indiceSiguienteMapa]);
        }
    }

    public void ColapsarMapa()
    {
        foreach (Transform child in transform) 
        {
            Destroy(child.gameObject);
        }
        AvanzarSiguienteNivel();
    }

    void SpawnearLapida()                                                           
    {
        if (lapidaInstanciada != null || datosNivelActual == null) return; 

        if (nivelBucle < 4) 
        {
            SpawnearMejoraMenor();
        }

        rondaActual = maxRondas;
        SessionData.round = rondaActual;
        AnalyticsBridge.EnviarLevelStart(SessionData.level, maxRondas);

        Vector3 posicionLapida = Vector3.zero;
        if (datosNivelActual.spawnPointsLapida.Length > 0)
        {
            posicionLapida = datosNivelActual.spawnPointsLapida[Random.Range(0, datosNivelActual.spawnPointsLapida.Length)];
        }

        lapidaInstanciada = Instantiate(datosNivelActual.lapidaPrefab, posicionLapida, Quaternion.identity);
        DesactivarPortalesComunes();
        
        ActualizarTextoProgreso(); 
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

            if (Physics.Raycast(posInicial, Vector3.down, out hit, 15f, datosNivelActual.capaSuelo))
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

    public void DesactivarPortalesComunes()
    {
        foreach (GameObject portal in portalesActivos)
        {
            if (portal != null) portal.SetActive(false); 
        }
        portalesActivos.Clear(); 
    }
    #endregion

    #region Corrutinas de UI y Textos
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
            textoComponente.text = $"NIVEL {nivelBucle} - RONDA {rondaActual}";
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

    void ActualizarTextoProgreso()
    {
        if (textoProgresoMuertes != null)
        {
            if (rondaActual >= maxRondas) 
            {
                textoProgresoMuertes.text = "¡Derrota\nal Jefe!";
            }
            else if (objetivoActual == TipoObjetivo.EliminarEnemigos)
            {
                int muertesVisuales = Mathf.Min(enemigosMuertosActuales, enemigosParaJefe);
                textoProgresoMuertes.text = $"Mata enemigos\n{muertesVisuales} / {enemigosParaJefe}";
            }
            else if (objetivoActual == TipoObjetivo.DefenderZona)
            {
                textoProgresoMuertes.text = $"Defiende la zona\n{Mathf.CeilToInt(tiempoDefensaActual)}s";
            }
        }
    }
    #endregion
}