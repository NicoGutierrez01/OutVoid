using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpUIManager : MonoBehaviour
{
    public static PowerUpUIManager Instancia;

    [Header("Lista Total de Mejoras")]
    public List<PowerUpsChest> poolDeMejoras;

    [Header("UI del Panel")]
    public GameObject panelPowerUps;

    [Header("Elementos de las 3 Cartas (Botones)")]
    public Button[] botonesCartas;
    public Image[] marcosCartas; // Las imágenes con el sprite "power_ups_marco"
    public Image[] iconosCartas;
    public TextMeshProUGUI[] titulosCartas;
    public TextMeshProUGUI[] descripcionesCartas;

    [Header("Colores por Rareza")]
    public Color colorComun = new Color(0.3f, 0.9f, 0.3f, 1f);       // Verde
    public Color colorRara = new Color(0.2f, 0.6f, 1f, 1f);          // Azul
    public Color colorEpica = new Color(0.7f, 0.2f, 1f, 1f);         // Púrpura / Magenta
    public Color colorLegendaria = new Color(1f, 0.75f, 0.1f, 1f);   // Dorado / Naranja

    private GameObject cofreActivo;

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
        if (poolDeMejoras.Count < 3)
        {
            Debug.LogError("¡No hay suficientes PowerUps en la lista! Agregá al menos 3 en el Inspector.");
            return;
        }

        cofreActivo = cofreQueSeAbrio;

        List<PowerUpsChest> disponibles = new List<PowerUpsChest>(poolDeMejoras);
        List<PowerUpsChest> elegidas = new List<PowerUpsChest>();

        for (int i = 0; i < 3; i++)
        {
            int indexRandom = Random.Range(0, disponibles.Count);
            elegidas.Add(disponibles[indexRandom]);
            disponibles.RemoveAt(indexRandom); 
        }

        for (int i = 0; i < 3; i++)
        {
            PowerUpsChest mejoraActual = elegidas[i];

            titulosCartas[i].text = mejoraActual.nombrePowerUp;
            descripcionesCartas[i].text = mejoraActual.descripcion;
            iconosCartas[i].sprite = mejoraActual.iconoUI;

            // Cambiar el color del marco según su rareza
            if (marcosCartas != null && i < marcosCartas.Length && marcosCartas[i] != null)
            {
                marcosCartas[i].color = ObtenerColorPorRareza(mejoraActual.rareza);
            }

            botonesCartas[i].onClick.RemoveAllListeners();
            botonesCartas[i].onClick.AddListener(() => SeleccionarMejora(mejoraActual));
        }

        panelPowerUps.SetActive(true);
    }

    private Color ObtenerColorPorRareza(RarezaPowerUp rareza)
    {
        switch (rareza)
        {
            case RarezaPowerUp.Comun: return colorComun;
            case RarezaPowerUp.Rara: return colorRara;
            case RarezaPowerUp.Epica: return colorEpica;
            case RarezaPowerUp.Legendaria: return colorLegendaria;
            default: return Color.white;
        }
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
    }

    private void AplicarEfecto(PowerUpsChest mejora)
    {
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador == null) return;

        PlayerStats stats = jugador.GetComponentInChildren<PlayerStats>();
        WeaponSystem weapon = jugador.GetComponentInChildren<WeaponSystem>();

        switch (mejora.statAMejorar)
        {
            case StatModificado.VidaMaxima:
                if (stats != null) {
                    stats.maxHealth += mejora.valorSuma;
                    stats.currentHealth += mejora.valorSuma; 
                }
                break;

            case StatModificado.EscudoMaximo:
                if (stats != null) stats.currentShield += mejora.valorSuma;
                break;

            case StatModificado.DanoArma:
                if (weapon != null) weapon.damage += mejora.valorSuma;
                break;

            case StatModificado.VelocidadRecarga:
                if (weapon != null) weapon.tiempoRecarga -= mejora.valorSuma; 
                break;

            case StatModificado.BalasDeFuego:
                if (weapon != null) weapon.tieneFuego = true;
                if (AdministradorDeProgreso.Instancia != null) AdministradorDeProgreso.Instancia.balasDeFuego = true;
                break;

            case StatModificado.BalasPenetrantes:
                if (weapon != null) weapon.balasPenetrantes = true;
                if (AdministradorDeProgreso.Instancia != null) AdministradorDeProgreso.Instancia.balasPenetrantes = true;
                break;

            case StatModificado.DisparoTriple:
                if (weapon != null) weapon.disparoTriple = true;
                if (AdministradorDeProgreso.Instancia != null) AdministradorDeProgreso.Instancia.disparoTriple = true;
                break;
        }
    }
}