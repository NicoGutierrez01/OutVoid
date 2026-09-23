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

    [Header("Avatar y Estado Crítico")]
    [Tooltip("Imagen del avatar circular del vaquero")]
    public Image imgAvatarCowboy;
    [Tooltip("Porcentaje de vida considerado crítico (0.25 = 25%)")]
    [Range(0.05f, 0.5f)] public float umbralVidaCritica = 0.25f;
    [Tooltip("Velocidad de la pulsación roja")]
    public float velocidadParpadeoCritico = 3f;
    public Color colorCritico = new Color(1f, 0.2f, 0.2f, 1f);

    private Color colorOriginalAvatar = Color.white;
    private bool enVidaCritica = false;

    [Header("Configuración de Colores")]
    public Gradient gradienteVida; 

    [Header("UI Munición y Estados del Arma")]
    public TextMeshProUGUI ammoText;
    [Tooltip("Ícono triangular de advertencia sobre el arma")]
    public GameObject iconoAlertaPocasBalas;
    [Tooltip("Imagen del marco/borde del arma para teñir en rojo")]
    public Image imgMarcoArma;
    [Tooltip("Imagen del revólver")]
    public Image imgIconoArma;

    [Header("Configuración Parpadeo Alerta Sin Balas")]
    public float duracionParpadeoAlerta = 2.0f;
    public float intervaloParpadeo = 0.15f;

    private Color colorOriginalMarco = Color.white;
    private Color colorOriginalIcono = Color.white;
    private bool estabaSinBalas = false;
    private Coroutine corrutinaParpadeoAlerta;

    [Header("UI Damage")]
    public Image imageDamage;
    [Range(0f, 1f)]
    public float alfaMaximo = 0.8f;
    public float umbralVida = 45f;

    [Header("Efecto Dash Espectral")]
    public Image imgGhostDash;
    [Range(0f, 1f)] public float alfaMaximoGhost = 0.9f;
    public float tiempoFadeInGhost = 0.25f; 
    public float tiempoFadeOutGhost = 0.4f;
    private Coroutine ghostCoroutine;

    [Header("Objetivos (Top / Superior)")]
    public TextMeshProUGUI textoRonda;      
    public TextMeshProUGUI textoDescripcion;

    [Header("Animación de Objetivos")]
    [Tooltip("RectTransform del contenedor principal 'Objective' que se anima")]
    public RectTransform rectObjetivo;
    [Tooltip("Distancia en píxeles que baja hacia el centro de la pantalla")]
    public float distanciaBajada = 140f;
    [Tooltip("Multiplicador de tamaño en el centro")]
    public float escalaAumento = 1.25f;
    [Tooltip("Duración total de la animación")]
    public float duracionAnimacion = 1.3f;

    private Vector2 posOriginalObjetivo;
    private Vector3 escalaOriginalObjetivo;
    private Coroutine corrutinaAnimObjetivo;
    private int ultimaRondaDetectada = -1;
    private int ultimoTipoObjetivoDetectado = -1;

    void Start()
    {
        if (imgGhostDash != null)
        {
            Color c = imgGhostDash.color;
            c.a = 0f;
            imgGhostDash.color = c;
            imgGhostDash.gameObject.SetActive(false);
        }

        if (imgMarcoArma != null) colorOriginalMarco = imgMarcoArma.color;
        if (imgIconoArma != null) colorOriginalIcono = imgIconoArma.color;
        if (iconoAlertaPocasBalas != null) iconoAlertaPocasBalas.SetActive(false);

        if (imgAvatarCowboy != null)
        {
            colorOriginalAvatar = imgAvatarCowboy.color;
        }

        if (rectObjetivo != null)
        {
            posOriginalObjetivo = rectObjetivo.anchoredPosition;
            escalaOriginalObjetivo = rectObjetivo.localScale;
            
            AnimarCambioObjetivo();
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

        ActualizarVida();
        ActualizarEstadoArmaYMunicion();
        ActualizarObjetivos();
        ActualizarVignetteDano();
    }

    private void ActualizarVida()
    {
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

        ActualizarAvatarCritico();
    }

    private void ActualizarAvatarCritico()
    {
        if (imgAvatarCowboy == null || player.maxHealth <= 0) return;

        float ratioVida = player.currentHealth / player.maxHealth;

        if (ratioVida <= umbralVidaCritica && player.currentHealth > 0)
        {
            enVidaCritica = true;
            float lerp = (Mathf.Sin(Time.time * velocidadParpadeoCritico) + 1f) * 0.5f;
            imgAvatarCowboy.color = Color.Lerp(colorOriginalAvatar, colorCritico, lerp);
        }
        else if (enVidaCritica)
        {
            enVidaCritica = false;
            imgAvatarCowboy.color = colorOriginalAvatar;
        }
    }

    private void ActualizarEstadoArmaYMunicion()
    {
        int balas = weapon.balasActuales;
        int reserva = weapon.balasReserva;

        if (weapon.recargando)
        {
            ammoText.text = "0 / " + reserva;
        }
        else
        {
            ammoText.text = balas + " / " + reserva;
        }

        bool sinBalas = (balas == 0 && reserva == 0);

        if (sinBalas)
        {
            ammoText.color = Color.red;
            if (imgMarcoArma != null) imgMarcoArma.color = Color.red;
            if (imgIconoArma != null) imgIconoArma.color = Color.red;

            if (!estabaSinBalas)
            {
                estabaSinBalas = true;
                if (corrutinaParpadeoAlerta != null) StopCoroutine(corrutinaParpadeoAlerta);
                corrutinaParpadeoAlerta = StartCoroutine(RutinaParpadeoAlerta());
            }
        }
        else
        {
            if (estabaSinBalas)
            {
                estabaSinBalas = false;
                if (corrutinaParpadeoAlerta != null)
                {
                    StopCoroutine(corrutinaParpadeoAlerta);
                    corrutinaParpadeoAlerta = null;
                }
                if (iconoAlertaPocasBalas != null) iconoAlertaPocasBalas.SetActive(false);
            }

            ammoText.color = Color.white;
            if (imgMarcoArma != null) imgMarcoArma.color = colorOriginalMarco;
            if (imgIconoArma != null) imgIconoArma.color = colorOriginalIcono;
        }
    }

    private IEnumerator RutinaParpadeoAlerta()
    {
        if (iconoAlertaPocasBalas == null) yield break;

        float tiempoTranscurrido = 0f;
        bool visible = true;

        while (tiempoTranscurrido < duracionParpadeoAlerta)
        {
            iconoAlertaPocasBalas.SetActive(visible);
            visible = !visible;

            yield return new WaitForSeconds(intervaloParpadeo);
            tiempoTranscurrido += intervaloParpadeo;
        }

        iconoAlertaPocasBalas.SetActive(false);
        corrutinaParpadeoAlerta = null;
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
        int tipoActual = (int)MapManager.Instance.objetivoActual;

        if (ronda != ultimaRondaDetectada || tipoActual != ultimoTipoObjetivoDetectado)
        {
            ultimaRondaDetectada = ronda;
            ultimoTipoObjetivoDetectado = tipoActual;
            AnimarCambioObjetivo();
        }

        if (MapManager.nivelBucle >= 4)
        {
            if (textoRonda != null) textoRonda.text = "¡ALERTA!";
            if (textoDescripcion != null) textoDescripcion.text = "¡BATALLA FINAL!\nACABA CON EL JEFE.";
        }
        else if (ronda >= 4)
        {
            if (textoRonda != null) textoRonda.text = "¡JEFE!";
            if (textoDescripcion != null) textoDescripcion.text = "BUSCA LA TUMBA\nY SOBREVIVE.";
        }
        else
        {
            if (textoRonda != null) textoRonda.text = "OBJETIVO"; 
            if (textoDescripcion != null)
            {
                if (MapManager.Instance.objetivoActual == TipoObjetivo.EliminarEnemigos)
                {
                    int muertos = MapManager.Instance.enemigosMuertosActuales;
                    int meta = MapManager.Instance.enemigosParaJefe;
                    textoDescripcion.text = $"MATA ENEMIGOS\n{muertos} / {meta}";
                }
                else if (MapManager.Instance.objetivoActual == TipoObjetivo.DefenderZona)
                {
                    int tiempo = Mathf.CeilToInt(MapManager.Instance.tiempoDefensaActual);
                    textoDescripcion.text = $"DEFIENDE LA ZONA\n{tiempo}S";
                }
            }
        }
    }

    public void AnimarCambioObjetivo()
    {
        if (rectObjetivo == null) return;
        if (corrutinaAnimObjetivo != null) StopCoroutine(corrutinaAnimObjetivo);
        corrutinaAnimObjetivo = StartCoroutine(RutinaAnimarObjetivo());
    }

    private IEnumerator RutinaAnimarObjetivo()
    {
        Vector2 posBaja = posOriginalObjetivo + new Vector2(0f, -distanciaBajada);
        Vector3 escalaAumentada = escalaOriginalObjetivo * escalaAumento;

        float tiempoBajada = duracionAnimacion * 0.35f;
        float tiempoRetorno = duracionAnimacion * 0.45f;

        float t = 0f;
        while (t < tiempoBajada)
        {
            t += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, t / tiempoBajada);
            rectObjetivo.anchoredPosition = Vector2.Lerp(posOriginalObjetivo, posBaja, f);
            rectObjetivo.localScale = Vector3.Lerp(escalaOriginalObjetivo, escalaAumentada, f);
            yield return null;
        }

        yield return new WaitForSeconds(0.25f);

        t = 0f;
        while (t < tiempoRetorno)
        {
            t += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, t / tiempoRetorno);
            rectObjetivo.anchoredPosition = Vector2.Lerp(posBaja, posOriginalObjetivo, f);
            rectObjetivo.localScale = Vector3.Lerp(escalaAumentada, escalaOriginalObjetivo, f);
            yield return null;
        }

        rectObjetivo.anchoredPosition = posOriginalObjetivo;
        rectObjetivo.localScale = escalaOriginalObjetivo;
        corrutinaAnimObjetivo = null;
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