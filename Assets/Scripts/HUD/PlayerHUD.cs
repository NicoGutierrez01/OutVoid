using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PlayerHUD : MonoBehaviour
{
    [Header("Scripts")]
    public PlayerStats player;
    public WeaponSystem weapon;

    [Header("UI Vida Dinámica")]
    public TextMeshProUGUI healthText;
    public Image imgForeground; 
    public Image imgShield;     

    [Header("Configuración de Colores")]
    public Gradient gradienteVida; 

    [Header("UI Munición")]
    public TextMeshProUGUI ammoText;

    [Header("UI Damage")]
    public Image imageDamage;
    [Range(0f, 1f)]
    public float alfaMaximo = 0.8f;
    public float umbralVida = 45f;

    [Header("Efecto Dash Espectral")]
    public Image imgGhostDash;
    [Range(0f, 1f)] public float alfaMaximoGhost = 0.9f;
    [Tooltip("Tiempo que tarda en aparecer gradualmente")]
    public float tiempoFadeInGhost = 0.25f; 
    [Tooltip("Tiempo que tarda en desvanecerse")]
    public float tiempoFadeOutGhost = 0.4f;
    private Coroutine ghostCoroutine;

    [Header("Objetivos (Panel Morado)")]
    public TextMeshProUGUI textoRonda;      
    public TextMeshProUGUI textoDescripcion; 

    void Start()
    {
        if (imgGhostDash != null)
        {
            Color c = imgGhostDash.color;
            c.a = 0f;
            imgGhostDash.color = c;
            imgGhostDash.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (player == null || weapon == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.GetComponentInChildren<PlayerStats>();
                weapon = p.GetComponentInChildren<WeaponSystem>();
            }
            if (player == null || weapon == null) return; 
        }

        float limiteVisualBarra = Mathf.Max(player.maxHealth, player.currentHealth + player.currentShield);

        if (imgShield != null)
        {
            if (player.currentShield > 0)
            {
                imgShield.gameObject.SetActive(true);
                imgShield.fillAmount = (player.currentHealth + player.currentShield) / limiteVisualBarra;
            }
            else
            {
                imgShield.gameObject.SetActive(false);
            }
        }

        if (imgForeground != null)
        {
            imgForeground.fillAmount = player.currentHealth / limiteVisualBarra;

            float porcentajeColor = Mathf.Clamp01(player.currentHealth / player.maxHealth);
            imgForeground.color = gradienteVida.Evaluate(porcentajeColor);
        }

        if (healthText != null)
        {
            float vidaActualTotal = player.currentHealth + player.currentShield;
            string colorPrincipal = vidaActualTotal < 30 ? "<color=#FF0000>" : "<color=#FFFFFF>";
            string formatoMaximo = $"<size=75%><color=#FFFFFF80>| {player.maxHealth.ToString("F0")}</color></size>";
            healthText.text = $"{colorPrincipal}{vidaActualTotal.ToString("F0")}</color> {formatoMaximo}";
        }

        if (weapon.recargando)
        {
            ammoText.text = "0 / " + weapon.balasReserva;
        }
        else
        {
            ammoText.text = weapon.balasActuales + " / " + weapon.balasReserva;
            ammoText.color = (weapon.balasActuales == 0 && weapon.balasReserva == 0) ? Color.red : Color.white;
        }

        ActualizarObjetivos();
        
        ActualizarVignetteDano();
    }

    public void TriggerGhostOverlay(float duracionHabilidad)
    {
        if (imgGhostDash == null) return;

        if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);
        ghostCoroutine = StartCoroutine(RutinaGhostOverlay(duracionHabilidad));
    }

    private IEnumerator RutinaGhostOverlay(float duracionHabilidad)
    {
        imgGhostDash.gameObject.SetActive(true);

        Color c = imgGhostDash.color;
        c.a = 0f;
        imgGhostDash.color = c;

        float tIn = 0f;
        while (tIn < tiempoFadeInGhost)
        {
            tIn += Time.deltaTime;
            c.a = Mathf.Lerp(0f, alfaMaximoGhost, tIn / tiempoFadeInGhost);
            imgGhostDash.color = c;
            yield return null;
        }

        c.a = alfaMaximoGhost;
        imgGhostDash.color = c;

        float tiempoSostenido = Mathf.Max(0f, duracionHabilidad - tiempoFadeInGhost - tiempoFadeOutGhost);
        yield return new WaitForSeconds(tiempoSostenido);

        float tOut = 0f;
        while (tOut < tiempoFadeOutGhost)
        {
            tOut += Time.deltaTime;
            c.a = Mathf.Lerp(alfaMaximoGhost, 0f, tOut / tiempoFadeOutGhost);
            imgGhostDash.color = c;
            yield return null;
        }

        c.a = 0f;
        imgGhostDash.color = c;
        imgGhostDash.gameObject.SetActive(false);
    }

    private void ActualizarObjetivos()
    {
        if (MapManager.Instance == null) return;

        int ronda = MapManager.Instance.rondaActual;

        if (MapManager.nivelBucle >= 4)
        {
            if (textoRonda != null) textoRonda.text = "¡ALERTA!";
            if (textoDescripcion != null) textoDescripcion.text = "¡BATALLA FINAL!\nAcaba con el Jefe.";
        }
        else if (ronda >= 4)
        {
            if (textoRonda != null) textoRonda.text = "¡JEFE!";
            if (textoDescripcion != null) textoDescripcion.text = "Busca la tumba\ny sobrevive.";
        }
        else
        {
            if (textoRonda != null) textoRonda.text = $"OBJETIVO"; 
            if (textoDescripcion != null)
            {
                if (MapManager.Instance.objetivoActual == TipoObjetivo.EliminarEnemigos)
                {
                    int muertos = MapManager.Instance.enemigosMuertosActuales;
                    int meta = MapManager.Instance.enemigosParaJefe;
                    textoDescripcion.text = $"Mata enemigos\n{muertos} / {meta}";
                }
                else if (MapManager.Instance.objetivoActual == TipoObjetivo.DefenderZona)
                {
                    int tiempo = Mathf.CeilToInt(MapManager.Instance.tiempoDefensaActual);
                    textoDescripcion.text = $"Defiende la zona\n{tiempo}s";
                }
            }
        }
    }

    private void ActualizarVignetteDano()
    {
        if (imageDamage == null || player == null) return;

        if (player.maxHealth > 0 && player.currentHealth <= player.maxHealth)
        {
            Color c = imageDamage.color;
            if (player.currentHealth >= umbralVida)
            {
                if (c.a != 0f) { c.a = 0f; imageDamage.color = c; }
            }
            else 
            {
                float cercaniaAMuerte = 1f - (player.currentHealth / umbralVida);
                c.a = Mathf.Clamp01(cercaniaAMuerte * alfaMaximo);
                imageDamage.color = c;
            }
        }
    }
}