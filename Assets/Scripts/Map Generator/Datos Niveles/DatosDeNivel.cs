using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NuevoDatosDeNivel", menuName = "Configuracion/Datos de Nivel")]
public class DatosDeNivel : ScriptableObject
{
    [Header("Escenario y Entorno")]
    public GameObject mapaPrefab;
    public LayerMask capaSuelo;

    [Header("Jugador")]
    public GameObject playerPrefab;
    public Vector3[] spawnPointsPlayer;

    [Header("Spawns y Enemigos del Nivel")]
    public GameObject portalEnemigoPrefab;
    public Vector3[] spawnPointsPortales;

    [Tooltip("Lista de posiciones donde puede aparecer la lapida del boss")]
    public Vector3[] spawnPointsLapida;

    [Header("Jefe del Nivel")]
    public GameObject bossPrefab;
    public GameObject portalBossPrefab;
    public GameObject lapidaPrefab;
    public Vector3[] spawnPointsPortalBoss;

    [Header("Magneto")]
    public GameObject magnetoPrefab;
    public Vector3[] spawnPointsMagneto;

    [Header("Decoración del Nivel")]
    public List<GameObject> prefabsDecoracionPrincipal;
    public int cantidadDecoracionPrincipal = 50; 

    [Header("Objetivos Especiales")]
    public GameObject zonaDefensaPrefab;
    public Vector3[] spawnPointsZonas;

    [Header("Portal de Siguiente Nivel")]
    public GameObject portalSiguienteNivelPrefab;
    [Tooltip("Posibles posiciones donde puede aparecer el portal de salida (se elige una al azar)")]
    public Vector3[] spawnPointsPortalSiguienteNivel;

    [Header("Altar Doble Boss (Opcional si tiene spawns definidos)")]
    [Tooltip("Prefab del altar interactuable para activar el segundo boss")]
    public GameObject altarPrefab;
    public Vector3[] spawnPointsAltar;

    public List<GameObject> prefabsDecoracionSecundaria;
    public int cantidadDecoracionSecundaria = 75;

    public GameObject prefabDecoracionSuelo;
    public int cantidadDecoracionSuelo = 150;
}