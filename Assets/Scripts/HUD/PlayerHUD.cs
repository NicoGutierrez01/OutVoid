using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class PlayerHUD : MonoBehaviour
{
    [Header("Scripts")]
    public PlayerStats player;
    public WeaponSystem weapon;

    [Header("UI Vida Dinámica (Diseño Sliced)")]
    public TextMeshProUGUI healthText;
    public RectTransform rtForeground; 
    public RectTransform rtShield;   
    private Image imgForeground;      

    [Header("Configuración de Colores")]
    public Color colorVidaNormal = Color.green;
    public Color colorVidaBaja = Color.red;

    [Header("UI Munición")]
    public TextMeshProUGUI ammoText;
    public Image hit;
    public Image hit2;
    public Image hit3;
    public Image bulletYellow;
    public float durationHit = 0.1f;

    [Header("UI Damage")]
    public Image imageDamage;
    [Range(0f, 1f)]
    public float alfaMaximo = 0.8f;
    public float umbralVida = 45f;

    [Header("Objetivos (Dividido en dos componentes)")]
    public TextMeshProUGUI textoRonda;      
    public TextMeshProUGUI textoDescripcion; 

    [Header("Pop-up de Items")]
    public GameObject popupObjeto; 
    public TextMeshProUGUI popupTexto;
    public float tiempoVisible = 2f;

    void Start()
    {
        if (rtForeground != null)
        {
            imgForeground = rtForeground.GetComponent<Image>();
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

        if (rtForeground != null)
        {
            float porcentajeVida = player.currentHealth / player.maxHealth;
            porcentajeVida = Mathf.Clamp01(porcentajeVida);

            rtForeground.anchorMin = new Vector2(0, 0);
            rtForeground.anchorMax = new Vector2(porcentajeVida, 1);
            rtForeground.offsetMin = Vector2.zero;
            rtForeground.offsetMax = Vector2.zero;

            if (imgForeground != null)
            {
                if (player.currentHealth <= 50f)
                {
                    imgForeground.color = colorVidaBaja;
                }
                else
                {
                    imgForeground.color = colorVidaNormal;
                }
            }
        }

        if (rtShield != null)
        {
            if (player.currentShield > 0)
            {
                rtShield.gameObject.SetActive(true);

                float porcentajeEscudo = player.currentShield / player.maxHealth;
                float puntoIzquierdo = 1f - porcentajeEscudo;

                rtShield.anchorMin = new Vector2(Mathf.Clamp01(puntoIzquierdo), 0); 
                rtShield.anchorMax = new Vector2(1f, 1);

                rtShield.offsetMin = Vector2.zero;
                rtShield.offsetMax = Vector2.zero;
            }
            else
            {
                rtShield.gameObject.SetActive(false);
            }
        }

        float vidaTotal = player.currentHealth + player.currentShield;
        if (healthText != null)
        {
            healthText.text = " " + vidaTotal.ToString("F0");
            
            if (vidaTotal < 30) healthText.color = Color.red;
            else healthText.color = Color.white;
        }

        if (weapon.recargando)
        {
            ammoText.text = "0" +  " / " + weapon.balasReserva;
        }
        else
        {
            ammoText.text = weapon.balasActuales + " / " + weapon.balasReserva;
            if (weapon.balasActuales == 0 && weapon.balasReserva == 0) ammoText.color = Color.red;
            else ammoText.color = Color.white;
        }

        if (MapManager.Instance != null)
        {
            int ronda = MapManager.Instance.rondaActual;

            if (MapManager.nivelBucle >= 4)
            {
                if (textoRonda != null)
                {
                    textoRonda.text = "¡ALERTA!";
                    textoRonda.color = Color.red;
                }
                if (textoDescripcion != null)
                {
                    textoDescripcion.text = "¡BATALLA FINAL!\nAcaba con el Jefe.";
                    textoDescripcion.color = Color.red;
                }
            }
            else if (ronda >= 4)
            {
                if (textoRonda != null)
                {
                    textoRonda.text = "¡JEFE!";
                    textoRonda.color = Color.red;
                }
                if (textoDescripcion != null)
                {
                    textoDescripcion.text = "Busca la tumba\ny sobrevive.";
                    textoDescripcion.color = Color.red;
                }
            }
            else
            {
                if (textoRonda != null)
                {
                    textoRonda.text = $"Ronda {ronda}/3";
                    textoRonda.color = Color.white;
                }
                if (textoDescripcion != null)
                {
                    int muertos = MapManager.Instance.enemigosMuertosActuales;
                    int meta = MapManager.Instance.enemigosParaJefe;

                    textoDescripcion.text = $"Mata enemigos\n{muertos} / {meta}";
                    textoDescripcion.color = Color.white;
                }
            }
        }

        if (imageDamage != null && player != null)
            {
                if (player.maxHealth > 0 && player.currentHealth <= player.maxHealth)
                {
                    if (player.currentHealth >= umbralVida)
                    {
                        Color c = imageDamage.color;
                        if (c.a != 0f) 
                        {
                            c.a = 0f;
                            imageDamage.color = c;
                        }
                    }
                    else 
                    {
                        float cercaniaAMuerte = 1f - (player.currentHealth / umbralVida);
                        float alphaFinal = cercaniaAMuerte * alfaMaximo;

                        Color c = imageDamage.color;
                        c.a = Mathf.Clamp01(alphaFinal);
                        imageDamage.color = c;
                    }
                }
            }
    }

    public void MostrarItemRecogido(string nombreItem)
    {
        StopCoroutine(nameof(EfectoPopup));
        StartCoroutine(EfectoPopup(nombreItem));
    }

    System.Collections.IEnumerator EfectoPopup(string nombreItem)
    {
        popupTexto.text = "¡" + nombreItem + "!";
        popupObjeto.SetActive(true);
        
        yield return new WaitForSeconds(tiempoVisible);
        
        popupObjeto.SetActive(false);
    }

    public void MostrarHitmarker()
    {
        StopCoroutine(nameof(EfectoHitmarker)); 
        StartCoroutine(EfectoHitmarker());
    }

    private System.Collections.IEnumerator EfectoHitmarker()
    {
        if (hit != null) hit.gameObject.SetActive(true);
        if (hit2 != null) hit2.gameObject.SetActive(true);
        if (hit3 != null) hit3.gameObject.SetActive(true);
        if (bulletYellow != null) bulletYellow.gameObject.SetActive(true);

        yield return new WaitForSeconds(durationHit);

        if (hit != null) hit.gameObject.SetActive(false);
        if (hit2 != null) hit2.gameObject.SetActive(false);
        if (hit3 != null) hit3.gameObject.SetActive(false);
        if (bulletYellow != null) bulletYellow.gameObject.SetActive(false);
    }
}