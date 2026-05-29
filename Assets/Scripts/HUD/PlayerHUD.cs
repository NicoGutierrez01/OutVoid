using UnityEngine;
using TMPro;
using UnityEngine.UI; 

public class PlayerHUD : MonoBehaviour
{
    [Header("Scripts")]
    public PlayerStats player;
    public WeaponSystem weapon;

    [Header("UI Vida y Escudo")]
    public TextMeshProUGUI healthText;
    public Slider healthSlider;       
    public RectTransform shieldBar;    

    [Header("UI Munición")]
    public TextMeshProUGUI ammoText;

    [Header("Pop-up de Items")]
    public GameObject popupObjeto; 
    public TextMeshProUGUI popupTexto;
    public float tiempoVisible = 2f;

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

        if (healthSlider != null)
        {
            healthSlider.maxValue = player.maxHealth;
            healthSlider.value = player.currentHealth;
        }

        healthText.text = " " + player.currentHealth.ToString("F0");
        if (player.currentHealth < 30) healthText.color = Color.red;
        else healthText.color = Color.white;

        ActualizarEscudoVisual();

        if (weapon.recargando)
        {
            ammoText.text = "Recargando";
            ammoText.color = Color.yellow;
        }
        else
        {
            ammoText.text = weapon.balasActuales + " / " + weapon.balasMaximas;
            if (weapon.balasActuales == 0) ammoText.color = Color.red;
            else ammoText.color = Color.white;
        }
    }

    void ActualizarEscudoVisual()
    {
        if (shieldBar == null || healthSlider == null) return;


        float totalActual = player.currentHealth + player.currentShield;
        

        healthSlider.maxValue = player.maxHealth + player.currentShield;
        healthSlider.value = player.currentHealth;

        healthText.text = " " + totalActual.ToString("F0");

        if (player.currentShield > 0)
        {
            shieldBar.gameObject.SetActive(true);

            float totalBarra = player.maxHealth + player.currentShield;
            float inicioEscudo = player.currentHealth / totalBarra;
            float finEscudo = (player.currentHealth + player.currentShield) / totalBarra;

            shieldBar.anchorMin = new Vector2(inicioEscudo, 0); 
            shieldBar.anchorMax = new Vector2(finEscudo, 1);

            shieldBar.offsetMin = Vector2.zero;
            shieldBar.offsetMax = Vector2.zero;
        }
        else
        {
            healthSlider.maxValue = player.maxHealth;
            shieldBar.gameObject.SetActive(false);
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