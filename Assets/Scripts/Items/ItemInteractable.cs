using UnityEngine;
using Unity.Services.Analytics;
using static StaticVariables;
using static EventManager;

public class ItemInteractable : MonoBehaviour
{
    public ItemMejoraData data;
    [HideInInspector] public string idGrupoDrop; 

    public void Configurar(ItemMejoraData nuevaData, string idGrupo)
    {
        data = nuevaData;
        idGrupoDrop = idGrupo;
        
        Renderer render = GetComponent<Renderer>();
        if (render != null) render.material.color = data.colorProvisorio;
    }

    public void Recoger(GameObject jugador)
    {
        PlayerInventario inventario = jugador.GetComponent<PlayerInventario>();
        if (inventario != null)
        {
            inventario.AplicarMejora(data);
        }

        SessionData.itemName = data.name; 
        
        Debug.Log($"Evento 'ItemPick' - El jugador eligió el ítem: {SessionData.itemName}");

        ItemPickEvent itemPickEvent = new ItemPickEvent
        {
            itemName = SessionData.itemName
        };

        AnalyticsService.Instance.RecordEvent(itemPickEvent);

        ItemInteractable[] todosLosItems = Object.FindObjectsByType<ItemInteractable>(FindObjectsSortMode.None);
        foreach (ItemInteractable item in todosLosItems)
        {
            if (item.idGrupoDrop == this.idGrupoDrop)
            {
                Destroy(item.gameObject);
            }
        }

        if (MapManager.Instance != null)
        {
            MapManager.Instance.ColapsarMapa();
        }
    }
}