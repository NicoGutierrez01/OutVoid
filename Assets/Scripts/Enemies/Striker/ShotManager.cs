using UnityEngine;
using System.Collections.Generic;

public class ShotManager : MonoBehaviour
{
    public static ShotManager Instance;

    [Header("Configuración de Cadencia Global")]
    [Tooltip("Tiempo mínimo obligatorio que debe pasar entre el disparo de un artillero y el siguiente")]
    public float cooldownEntreArtilleros = 0.4f; 
    
    private float tiempoSiguienteDisparoPermitido = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // El artillero llama a esta función antes de disparar. Devuelve true si tiene permiso.
    public bool SolicitarPermisoParaDisparar()
    {
        if (Time.time >= tiempoSiguienteDisparoPermitido)
        {
            // Otorgamos el permiso y bloqueamos el canal por un breve instante global
            tiempoSiguienteDisparoPermitido = Time.time + cooldownEntreArtilleros;
            return true;
        }
        return false; // Esperá tu turno, rey
    }
}