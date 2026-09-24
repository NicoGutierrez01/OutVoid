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

    [Serializable]
    public struct UltiUI
    {
        [Tooltip("Objeto que se muestra cuando la ulti está lista (On)")]
        public GameObject objetoOn;
        [Tooltip("Objeto que se muestra durante cooldown (Off)")]
        public GameObject objetoOff;
        [Tooltip("Texto en el medio para el porcentaje")]
        public TextMeshProUGUI txtPorcentaje;
        [Tooltip("Tiempo base de recarga de la ulti")]
        public float cooldownTotalBase;
    }

    [Header("Configuración de Slots")]
    public HabilidadUI UI_Dash;
    public HabilidadUI UI_Bomba;
    public UltiUI UI_Ulti;

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

        ActualizarUlti(playerAbilities.canUseQ, playerAbilities.ultCooldownTimer, UI_Ulti);
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

    void ActualizarUlti(bool listo, float tiempoRestante, UltiUI ui)
    {
        if (ui.objetoOn == null || ui.objetoOff == null) return;

        if (listo || tiempoRestante <= 0f)
        {
            ui.objetoOn.SetActive(true);
            ui.objetoOff.SetActive(false);

            if (ui.txtPorcentaje != null)
            {
                ui.txtPorcentaje.text = "";
            }
        }
        else
        {
            ui.objetoOn.SetActive(false);
            ui.objetoOff.SetActive(true);

            if (ui.txtPorcentaje != null)
            {
                if (ui.cooldownTotalBase > 0f)
                {
                    float progreso = Mathf.Clamp01(1f - (tiempoRestante / ui.cooldownTotalBase));
                    ui.txtPorcentaje.text = $"{Mathf.FloorToInt(progreso * 100f)}";
                }
                else
                {
                    ui.txtPorcentaje.text = $"{Mathf.CeilToInt(tiempoRestante)}s";
                }
            }
        }
    }
}