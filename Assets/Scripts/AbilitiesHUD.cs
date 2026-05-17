using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AbilitiesHUD : MonoBehaviour
{
    [Header("Referencias del Player")]
    public PlayerAbilities playerAbilities;

    [Header("UI Dash")]
    public Image imgDash;
    public TextMeshProUGUI txtDash;

    [Header("UI Bomb")]
    public Image imgBomba;
    public TextMeshProUGUI txtBomba;

    [Header("UI Ulti")]
    public Image imgUlti;
    public TextMeshProUGUI txtUlti;

    private Color colorListo = Color.white;
    private Color colorCooldown = new Color(0.3f, 0.3f, 0.3f, 0.7f);

    void Update()
    {
        if (playerAbilities == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerAbilities = p.GetComponent<PlayerAbilities>();

            if (playerAbilities == null) return; 
        }

        ActualizarSlot(playerAbilities.canDash, playerAbilities.dashCooldownTimer, imgDash, txtDash, "Dash", "Shift");
        ActualizarSlot(playerAbilities.canUseE, playerAbilities.dynamiteCooldownTimer, imgBomba, txtBomba, "Bomb", "E");
        ActualizarSlot(playerAbilities.canUseQ, playerAbilities.ultCooldownTimer, imgUlti, txtUlti, "Ulti", "Q");
    }

    void ActualizarSlot(bool listo, float tiempoRestante, Image img, TextMeshProUGUI texto, string nombreHabilidad, string tecla)
    {
        if (listo)
        {
            img.color = colorListo;
            // Usamos \n para hacer el salto de línea y agregamos la tecla entre paréntesis
            texto.text = nombreHabilidad + "\n(" + tecla + ")";
        }
        else
        {
            img.color = colorCooldown;
            // Durante el cooldown mostramos el número, hacemos salto de línea y mantenemos la tecla visible
            texto.text = Mathf.CeilToInt(tiempoRestante).ToString() + "\n(" + tecla + ")";
        }
    }
}