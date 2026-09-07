using UnityEngine;
using System.Collections.Generic;

public class PlayerInventario : MonoBehaviour
{
    // Diccionario para contar cuántas veces se eligió cada stat o tipo de mejora
    public Dictionary<StatModificado, int> nivelesMejoras = new Dictionary<StatModificado, int>();

    private WeaponSystem weapon;
    private PlayerStats stats;

    void Awake()
    {
        weapon = GetComponentInChildren<WeaponSystem>();
        stats = GetComponentInChildren<PlayerStats>();
    }

    public void RegistrarYAplicarPowerUp(PowerUpsChest powerUp)
    {
        if (powerUp == null) return;

        if (weapon == null) weapon = GetComponentInChildren<WeaponSystem>();
        if (stats == null) stats = GetComponentInChildren<PlayerStats>();

        // 1. Seguimiento de niveles
        if (nivelesMejoras.ContainsKey(powerUp.statAMejorar))
        {
            nivelesMejoras[powerUp.statAMejorar]++;
        }
        else
        {
            nivelesMejoras.Add(powerUp.statAMejorar, 1);
        }

        // 2. Aplicar el efecto mecánico
        EjecutarEfecto(powerUp);

        // 3. Registrar en el Administrador de Progreso para el GameOver
        if (AdministradorDeProgreso.Instancia != null)
        {
            AdministradorDeProgreso.Instancia.mejorasRecogidas++;
        }

        Debug.Log($"[INVENTARIO] PowerUp aplicado: {powerUp.nombrePowerUp} (Nivel acumulado: {nivelesMejoras[powerUp.statAMejorar]} | Total Run: {(AdministradorDeProgreso.Instancia != null ? AdministradorDeProgreso.Instancia.mejorasRecogidas : 0)})");
    }

    private void EjecutarEfecto(PowerUpsChest mejora)
    {
        AdministradorDeProgreso admin = AdministradorDeProgreso.Instancia;

        switch (mejora.statAMejorar)
        {
            case StatModificado.VidaMaxima:
                if (stats != null)
                {
                    stats.maxHealth += mejora.valorSuma;
                    stats.currentHealth += mejora.valorSuma;
                }
                break;

            case StatModificado.EscudoMaximo:
                if (stats != null)
                {
                    stats.currentShield += mejora.valorSuma;
                }
                break;

            case StatModificado.DanoArma:
                if (weapon != null)
                {
                    weapon.damage += mejora.valorSuma;
                }
                if (admin != null)
                {
                    admin.multiplicadorDaño *= 1.15f;
                }
                break;

            case StatModificado.VelocidadRecarga:
                if (weapon != null)
                {
                    weapon.tiempoRecarga = Mathf.Max(0.2f, weapon.tiempoRecarga - mejora.valorSuma);
                }
                if (admin != null)
                {
                    admin.multiplicadorRecarga += 0.15f;
                }
                break;

            case StatModificado.BalasDeFuego:
                if (weapon != null) weapon.tieneFuego = true;
                if (admin != null) admin.balasDeFuego = true;
                break;

            case StatModificado.BalasPenetrantes:
                if (weapon != null) weapon.balasPenetrantes = true;
                if (admin != null) admin.balasPenetrantes = true;
                break;

            case StatModificado.DisparoTriple:
                if (weapon != null) weapon.disparoTriple = true;
                if (admin != null) admin.disparoTriple = true;
                break;

            case StatModificado.BalasExplosivas:
                if (weapon != null) weapon.balasExplosivas = true;
                if (admin != null) admin.balasExplosivas = true;
                break;

            // Rara: Velocidad de movimiento
            case StatModificado.VelocidadMovimiento:
                if (admin != null) admin.multiplicadorVelocidad += mejora.valorSuma;
                PlayerCharacter pChar = GetComponentInChildren<PlayerCharacter>();
                if (pChar != null)
                {
                    pChar.AumentarVelocidad(mejora.valorSuma);
                }
                break;

            // Legendaria: Vampirismo por muerte
            case StatModificado.SaludPorBaja:
                EnemyHealth.healthPerKillActive = true;
                if (admin != null) admin.saludPorKill = true;
                break;

            // Epica: Escudo de emergencia
            case StatModificado.EscudoEmergencia:
                if (stats != null) stats.tieneEscudoEmergencia = true;
                if (admin != null) admin.tieneEscudoEmergencia = true;
                break;

            // Legendaria: Potenciador de Dinamita
            case StatModificado.DinamitaDevastadora:
                if (admin != null) admin.multiplicadorDinamitaCooldown *= 0.5f;
                PlayerAbilities abilities = GetComponentInChildren<PlayerAbilities>();
                if (abilities != null)
                {
                    abilities.PotenciarDinamita(mejora.valorSuma);
                }
                break;
        }
    }

    public int ObtenerNivelMejora(StatModificado stat)
    {
        return nivelesMejoras.ContainsKey(stat) ? nivelesMejoras[stat] : 0;
    }
}