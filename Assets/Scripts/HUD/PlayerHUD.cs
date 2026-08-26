using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    [Header("Objetivos (Panel Morado)")]
    public TextMeshProUGUI textoRonda;      
    public TextMeshProUGUI textoDescripcion; 

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

        if (imgForeground != null)
        {
            float porcentajeVida = Mathf.Clamp01(player.currentHealth / player.maxHealth);
            imgForeground.fillAmount = porcentajeVida;

            imgForeground.color = gradienteVida.Evaluate(porcentajeVida);
        }

        if (imgShield != null)
        {
            if (player.currentShield > 0)
            {
                imgShield.gameObject.SetActive(true);
                imgShield.fillAmount = Mathf.Clamp01(player.currentShield / player.maxHealth);
            }
            else
            {
                imgShield.gameObject.SetActive(false);
            }
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