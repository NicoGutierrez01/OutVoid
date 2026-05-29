using UnityEngine;
using System.Collections.Generic;

public class PlayerInventario : MonoBehaviour
{
    public Dictionary<TipoEfecto, int> nivelesMejoras = new Dictionary<TipoEfecto, int>();

    private WeaponSystem weapon;
    private PlayerStats stats;

    void Start()
    {
        weapon = GetComponentInChildren<WeaponSystem>();
        stats = GetComponentInChildren<PlayerStats>();
    }

    public void AplicarMejora(ItemMejoraData item)
    {
        if (nivelesMejoras.ContainsKey(item.efecto))
        {
            if (nivelesMejoras[item.efecto] < item.nivelMaximo)
            {
                nivelesMejoras[item.efecto]++;
                Debug.Log($"¡{item.nombreItem} subió al nivel {nivelesMejoras[item.efecto]}!");
            }
            else
            {
                Debug.Log($"El ítem {item.nombreItem} ya está al nivel máximo. Reemplazando por curación extra.");
                stats.Heal(25f); 
                return; 
            }
        }
        else
        {
            nivelesMejoras.Add(item.efecto, 1);
            Debug.Log($"¡Agarraste {item.nombreItem} por primera vez!");
        }

        EjecutarEfecto(item.efecto);
    }

    private void EjecutarEfecto(TipoEfecto efecto)
    {
        AdministradorDeProgreso admin = AdministradorDeProgreso.Instancia;

        switch (efecto)
        {
            case TipoEfecto.Daño:
                weapon.damage *= 1.15f;
                if (admin != null) admin.multiplicadorDaño *= 1.15f;
                break;

            case TipoEfecto.BalasFuego:
                weapon.tieneFuego = true;
                if (admin != null) admin.balasDeFuego = true;
                break;

            case TipoEfecto.EscudoMax:
                stats.currentShield += 25f;
                break;
                
        }

        if (admin != null) admin.mejorasRecogidas++;
    }
}