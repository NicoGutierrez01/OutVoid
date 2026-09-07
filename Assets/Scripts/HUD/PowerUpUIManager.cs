using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct ProbabilidadesPorRonda
{
    public string nombreRonda;
    [Range(0, 100)] public float probComun;
    [Range(0, 100)] public float probRara;
    [Range(0, 100)] public float probEpica;
}

public class PowerUpUIManager : MonoBehaviour
{
    public static PowerUpUIManager Instancia;

    [Header("Lista Total de Mejoras")]
    public List<PowerUpsChest> poolDeMejoras;

    [Header("Escalado de Probabilidades por Ronda")]
    [Tooltip("Definí la tabla de probabilidades para cada ronda de combate")]
    public ProbabilidadesPorRonda[] probabilidadesRondas = new ProbabilidadesPorRonda[]
    {
        new ProbabilidadesPorRonda { nombreRonda = "Ronda 1", probComun = 60f, probRara = 30f, probEpica = 10f },
        new ProbabilidadesPorRonda { nombreRonda = "Ronda 2", probComun = 40f, probRara = 35f, probEpica = 25f },
        new ProbabilidadesPorRonda { nombreRonda = "Ronda 3", probComun = 20f, probRara = 35f, probEpica = 45f }
    };

    [Header("UI del Panel")]
    public GameObject panelPowerUps;

    [Header("Elementos de las 3 Cartas (Botones)")]
    public Button[] botonesCartas;
    public Image[] marcosCartas; 
    public Image[] iconosCartas;
    public TextMeshProUGUI[] titulosCartas;
    public TextMeshProUGUI[] descripcionesCartas;

    private GameObject cofreActivo;
    private bool eraCofreBoss = false;

    private void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (panelPowerUps != null) panelPowerUps.SetActive(false);
    }

    public void MostrarOpciones(GameObject cofreQueSeAbrio)
    {
        cofreActivo = cofreQueSeAbrio;
        eraCofreBoss = false;

        if (cofreActivo != null)
        {
            CofreRecompensa scriptCofre = cofreActivo.GetComponent<CofreRecompensa>();
            if (scriptCofre != null && scriptCofre.esCofreBoss)
            {
                eraCofreBoss = true;
            }
        }

        List<PowerUpsChest> cartasElegidas = new List<PowerUpsChest>();

        if (eraCofreBoss)
        {
            // --- COFRE DEL BOSS: SOLO LEGENDARIAS ---
            List<PowerUpsChest> poolLegendarias = poolDeMejoras.FindAll(p => p != null && p.rareza == RarezaPowerUp.Legendaria);

            if (poolLegendarias.Count < 3)
            {
                Debug.LogWarning("[PowerUpUIManager] Se necesitan al menos 3 mejoras Legendarias en poolDeMejoras para el Boss.");
            }

            List<PowerUpsChest> copiaLegendarias = new List<PowerUpsChest>(poolLegendarias);
            for (int i = 0; i < 3 && copiaLegendarias.Count > 0; i++)
            {
                int index = Random.Range(0, copiaLegendarias.Count);
                cartasElegidas.Add(copiaLegendarias[index]);
                copiaLegendarias.RemoveAt(index);
            }
        }
        else
        {
            // --- COFRE NORMAL: EXCLUYE LEGENDARIAS Y APLICA PROBABILIDAD DE LA RONDA ---
            List<PowerUpsChest> comunes = poolDeMejoras.FindAll(p => p != null && p.rareza == RarezaPowerUp.Comun);
            List<PowerUpsChest> raras = poolDeMejoras.FindAll(p => p != null && p.rareza == RarezaPowerUp.Rara);
            List<PowerUpsChest> epicas = poolDeMejoras.FindAll(p => p != null && p.rareza == RarezaPowerUp.Epica);

            List<PowerUpsChest> todasValidas = poolDeMejoras.FindAll(p => p != null && p.rareza != RarezaPowerUp.Legendaria);

            int rondaActual = (MapManager.Instance != null) ? MapManager.Instance.rondaActual : 1;

            for (int i = 0; i < 3; i++)
            {
                RarezaPowerUp rarezaTirada = SortearRarezaPorRonda(rondaActual);
                PowerUpsChest seleccion = ObtenerCartaNoRepetida(rarezaTirada, comunes, raras, epicas, cartasElegidas, todasValidas);

                if (seleccion != null)
                {
                    cartasElegidas.Add(seleccion);
                }
            }
        }

        // Asignar visualmente las 3 cartas en pantalla
        for (int i = 0; i < cartasElegidas.Count && i < botonesCartas.Length; i++)
        {
            PowerUpsChest mejoraActual = cartasElegidas[i];

            titulosCartas[i].text = mejoraActual.nombrePowerUp;
            descripcionesCartas[i].text = mejoraActual.descripcion;
            
            if (iconosCartas != null && i < iconosCartas.Length && iconosCartas[i] != null)
            {
                iconosCartas[i].sprite = mejoraActual.iconoUI;
                iconosCartas[i].enabled = mejoraActual.iconoUI != null;
            }

            if (marcosCartas != null && i < marcosCartas.Length && marcosCartas[i] != null)
            {
                if (mejoraActual.marcoUI != null)
                {
                    marcosCartas[i].sprite = mejoraActual.marcoUI;
                }
                marcosCartas[i].color = Color.white;
            }

            botonesCartas[i].onClick.RemoveAllListeners();
            botonesCartas[i].onClick.AddListener(() => SeleccionarMejora(mejoraActual));
        }

        panelPowerUps.SetActive(true);
    }

    private RarezaPowerUp SortearRarezaPorRonda(int ronda)
    {
        int indiceConfig = Mathf.Clamp(ronda - 1, 0, probabilidadesRondas.Length - 1);
        ProbabilidadesPorRonda config = probabilidadesRondas[indiceConfig];

        float roll = Random.Range(0f, 100f);

        if (roll < config.probComun)
        {
            return RarezaPowerUp.Comun;
        }
        else if (roll < (config.probComun + config.probRara))
        {
            return RarezaPowerUp.Rara;
        }
        else
        {
            return RarezaPowerUp.Epica;
        }
    }

    private PowerUpsChest ObtenerCartaNoRepetida(
        RarezaPowerUp rareza, 
        List<PowerUpsChest> comunes, 
        List<PowerUpsChest> raras, 
        List<PowerUpsChest> epicas, 
        List<PowerUpsChest> yaElegidas,
        List<PowerUpsChest> fallbackPool)
    {
        List<PowerUpsChest> listaObjetivo;

        switch (rareza)
        {
            case RarezaPowerUp.Epica:
                listaObjetivo = epicas;
                break;
            case RarezaPowerUp.Rara:
                listaObjetivo = raras;
                break;
            default:
                listaObjetivo = comunes;
                break;
        }

        List<PowerUpsChest> disponibles = listaObjetivo.FindAll(c => !yaElegidas.Contains(c));

        if (disponibles.Count > 0)
        {
            return disponibles[Random.Range(0, disponibles.Count)];
        }

        List<PowerUpsChest> disponiblesGlobal = fallbackPool.FindAll(c => !yaElegidas.Contains(c));
        if (disponiblesGlobal.Count > 0)
        {
            return disponiblesGlobal[Random.Range(0, disponiblesGlobal.Count)];
        }

        return fallbackPool.Count > 0 ? fallbackPool[Random.Range(0, fallbackPool.Count)] : null;
    }

    private void SeleccionarMejora(PowerUpsChest mejoraElegida)
    {
        AplicarEfecto(mejoraElegida);

        panelPowerUps.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cofreActivo != null)
        {
            Destroy(cofreActivo);
        }

        if (eraCofreBoss && MapManager.Instance != null)
        {
            MapManager.Instance.AvanzarSiguienteNivel();
        }
    }

    private void AplicarEfecto(PowerUpsChest mejora)
    {
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador == null) return;

        PlayerInventario inventario = jugador.GetComponentInChildren<PlayerInventario>();
        if (inventario == null)
        {
            inventario = jugador.AddComponent<PlayerInventario>();
        }

        inventario.RegistrarYAplicarPowerUp(mejora);
    }
}