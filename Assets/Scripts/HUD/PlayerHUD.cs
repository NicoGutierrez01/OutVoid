using UnityEngine;
using TMPro;
using UnityEngine.UI; 

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
            ammoText.text = "Recargando";
            ammoText.color = Color.yellow;
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
                // Estado normal reflejado en la imagen image_6999e1.png
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
    }

    public void MostrarItemRecogido(string nombreItem)
    {
        StopAllCoroutines(); 
        StartCoroutine(EfectoPopup(nombreItem));
    }

    System.Collections.IEnumerator EfectoPopup(string nombreItem)
    {
        popupTexto.text = "¡" + nombreItem + "!";
        popupObjeto.SetActive(true);
        
        yield return new WaitForSeconds(tiempoVisible);
        
        popupObjeto.SetActive(false);
    }
}