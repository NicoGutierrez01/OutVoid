using UnityEngine;

public class ItemMejora : MonoBehaviour
{
    public enum TipoMejora { SaludRegen, SaludKill, Velocidad, DobleSalto, Cooldown, Daño, Escudo, BalasFuego, RecargaRapida, Herradura }
    private TipoMejora mejoraAsignada;

    private void Start()
    {
        var movement = Object.FindAnyObjectByType<PlayerMovement>();
        int totalMejoras = System.Enum.GetValues(typeof(TipoMejora)).Length;

        mejoraAsignada = (TipoMejora)Random.Range(0, totalMejoras);

        if (mejoraAsignada == TipoMejora.DobleSalto && movement != null && movement.saltosAdicionalesMaximos >= 2)
        {
            while (mejoraAsignada == TipoMejora.DobleSalto)
            {
                mejoraAsignada = (TipoMejora)Random.Range(0, totalMejoras);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AplicarEfecto(other.gameObject);
            Destroy(gameObject);
        }
    }

    void AplicarEfecto(GameObject player)
    {
    var weapon = player.GetComponent<WeaponSystem>();
    var movement = player.GetComponent<PlayerMovement>();
    var abilities = player.GetComponent<PlayerAbilities>();
    
    PlayerHUD hud = Object.FindAnyObjectByType<PlayerHUD>();
    string textoPopup = "";

    AdministradorDeProgreso.Instancia.mejorasRecogidas++;

    switch (mejoraAsignada)
    {
        case TipoMejora.SaludRegen: 
            movement.tieneEscudoEmergencia = true; 
            AdministradorDeProgreso.Instancia.tieneEscudoEmergencia = true; 
            textoPopup = "PASIVA: SALVAVIDAS ADQUIRIDO"; 
            break;

        case TipoMejora.SaludKill: 
            EnemyHealth.healthPerKillActive = true; 
            AdministradorDeProgreso.Instancia.saludPorKill = true; 
            textoPopup = "+1 DE VIDA POR KILL"; break;

        case TipoMejora.Velocidad: 
            movement.speed *= 1.2f; 
            AdministradorDeProgreso.Instancia.multiplicadorVelocidad *= 1.2f; 
            textoPopup = "VELOCIDAD AUMENTADA"; break;

        case TipoMejora.DobleSalto: 
            if (movement.saltosAdicionalesMaximos < 2) 
            {
                movement.saltosAdicionalesMaximos++;
                AdministradorDeProgreso.Instancia.saltosAdicionales = movement.saltosAdicionalesMaximos; 
                textoPopup = (movement.saltosAdicionalesMaximos == 1) ? "DOBLE SALTO" : "TRIPLE SALTO";
            }
            break;

        case TipoMejora.Cooldown: 
            if (Random.value > 0.5f) {
                abilities.dashCooldown *= 0.8f;
                AdministradorDeProgreso.Instancia.multiplicadorDashCooldown *= 0.8f; 
                textoPopup = "COOLDOWN DE DASH MEJORADO";
            } else {
                abilities.dynamiteCooldown *= 0.8f;
                AdministradorDeProgreso.Instancia.multiplicadorDinamitaCooldown *= 0.8f; 
                textoPopup = "COOLDOWN DE DYNAMITE MEJORADO";
            } break;

        case TipoMejora.Daño: 
            weapon.damage *= 1.15f; 
            AdministradorDeProgreso.Instancia.multiplicadorDaño *= 1.15f; 
            textoPopup = "DAÑO +15%"; break;

        case TipoMejora.Escudo: 
            float esc = Random.Range(25f, 50f); 
            movement.currentShield += esc; 
            textoPopup = "ESCUDO AZUL +" + esc.ToString("F0"); break; 

        case TipoMejora.BalasFuego: 
            weapon.tieneFuego = true; 
            AdministradorDeProgreso.Instancia.balasDeFuego = true; 
            textoPopup = "BALAS DE FUEGO"; break;

        case TipoMejora.RecargaRapida: 
            weapon.tiempoRecarga *= 0.70f; 
            AdministradorDeProgreso.Instancia.multiplicadorRecarga *= 0.70f; 
            textoPopup = "RECARGA VELOZ"; break;
            
        case TipoMejora.Herradura: 
            AdministradorDeProgreso.Instancia.probabilidadDropExtra += 2f; 
            foreach(var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) 
                e.probabilidadDrop += 2f; 
            textoPopup = "MÁS PROBABILIDAD DE DROP"; break;
    }

    if (hud != null && textoPopup != "")
    {
        hud.MostrarItemRecogido(textoPopup);
    }

    Debug.Log("¡Obtuviste: " + mejoraAsignada + "!");
    }
}