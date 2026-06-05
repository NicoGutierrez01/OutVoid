using UnityEngine;

public class ItemMejoraDinamica : MonoBehaviour
{
    private MejoraData data;

    [Header("Referencias Internas")]
    public MeshFilter meshFilter;
    public Light luzBaliza; // Si usas una luz real para el color de la rareza

    // Esta función la llama el MapManager al spawnearlo
    public void ConfigurarItem(MejoraData datosAsignados)
    {
        data = datosAsignados;

        // Se disfraza con la mesh que dice la tarjetita
        if (meshFilter != null && data.malla != null)
        {
            meshFilter.mesh = data.malla;
        }

        // Cambia el color de la luz según la rareza
        if (luzBaliza != null)
        {
            luzBaliza.color = data.colorBaliza;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AplicarMejora(other.gameObject);
            MostrarMensaje();
            Destroy(gameObject);
        }
    }

    private void AplicarMejora(GameObject jugador)
    {
        PlayerStats stats = jugador.GetComponentInChildren<PlayerStats>();
        if (stats != null)
        {
            // Leemos qué estadística toca mejorar según el ScriptableObject
            switch (data.statAMejorar)
            {
                case TipoStat.VidaMaxima:
                    stats.maxHealth += data.valorSuma;
                    stats.Heal(data.valorSuma); // Le curamos esa vida extra
                    break;
                case TipoStat.Armadura:
                    // stats.armadura += data.valorSuma; (Ejemplo futuro)
                    break;
            }
        }
    }

    private void MostrarMensaje()
    {
        // Acá buscamos tu HUD y le mandamos el texto armado
        PlayerHUD hud = FindAnyObjectByType<PlayerHUD>();
        if (hud != null)
        {
            // Va a mostrar algo como: "Pólvora Pesada (Rara) \n +10 Daño"
            hud.MostrarItemRecogido($"{data.nombreMejora} ({data.rareza})\n{data.descripcion}");
        }
    }
}