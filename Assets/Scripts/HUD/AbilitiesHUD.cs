using UnityEngine;
using TMPro;
using System; 

public class AbilitiesHUD : MonoBehaviour
{
    [Header("Referencias del Player")]
    public PlayerAbilities playerAbilities;

    [Serializable]
    public struct HabilidadUI
    {
        public GameObject slotBase;       
        public GameObject objetoActivo; 
        public GameObject objetoDesactivado;
        public TextMeshProUGUI txtCooldown; 
    }

    [Header("Configuración de Slots")]
    public HabilidadUI UI_Dash;
    public HabilidadUI UI_Bomba;
    public HabilidadUI UI_Ulti;

    void Update()
    {
        if (playerAbilities == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerAbilities = p.GetComponentInChildren<PlayerAbilities>();

            if (playerAbilities == null) return; 
        }

        ActualizarSlot(playerAbilities.canDash, playerAbilities.dashCooldownTimer, UI_Dash);
        ActualizarSlot(playerAbilities.canUseE, playerAbilities.dynamiteCooldownTimer, UI_Bomba);
        ActualizarSlot(playerAbilities.canUseQ, playerAbilities.ultCooldownTimer, UI_Ulti);
    }

    void ActualizarSlot(bool listo, float tiempoRestante, HabilidadUI ui)
    {
        if (ui.objetoActivo == null || ui.objetoDesactivado == null) return;

        if (listo)
        {
            ui.objetoActivo.SetActive(true);
            ui.objetoDesactivado.SetActive(false);
            
            if (ui.txtCooldown != null)
            {
                ui.txtCooldown.text = ""; 
            }
        }
        else
        {
            ui.objetoActivo.SetActive(false);
            ui.objetoDesactivado.SetActive(true);
            
            if (ui.txtCooldown != null)
            {
                ui.txtCooldown.text = Mathf.CeilToInt(tiempoRestante).ToString();
            }
        }
    }
}