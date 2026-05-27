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
        var movement = player.GetComponent<PlayerMovement>();
        var weapon = player.GetComponent<WeaponSystem>();
        var hud = Object.FindAnyObjectByType<PlayerHUD>(); 

        switch (tipo)
        {
            case TipoRecurso.Vida:
                movement.Heal(cantidadVida);
                if(hud != null) hud.MostrarItemRecogido("+ VIDA");
                break;
                
            case TipoRecurso.Escudo:
                movement.currentShield += cantidadEscudo;
                if(hud != null) hud.MostrarItemRecogido("+ ESCUDO");
                break;
                
            case TipoRecurso.Balas:
                if (weapon != null) weapon.AddAmmo(cantidadBalas);
                if(hud != null) hud.MostrarItemRecogido("+ " + cantidadBalas + " BALAS");
                break;
        }
    }
}