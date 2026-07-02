using UnityEngine;

public class ItemRecurso : MonoBehaviour
{
    public enum TipoRecurso { Vida, Escudo, Balas }
    public TipoRecurso tipo;

    [Header("Cantidades Base")]
    public float cantidadVida = 25f; 
    public float cantidadEscudo = 20f; 
    public int cantidadBalas = 12; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AplicarRecurso(other.gameObject);
            Destroy(gameObject);
        }
    }

    void AplicarRecurso(GameObject player)
    {
        var stats = player.GetComponent<PlayerStats>();
        var weapon = player.GetComponent<WeaponSystem>();

        switch (tipo)
        {
            case TipoRecurso.Vida:
                if (stats != null) stats.Heal(cantidadVida);
                break;
                
            case TipoRecurso.Escudo:
                if (stats != null) stats.currentShield += cantidadEscudo;
                break;
                
            case TipoRecurso.Balas:
                if (weapon != null) weapon.AddAmmo(cantidadBalas);
                break;
        }
    }
}