using UnityEngine;

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

        ItemInteractable[] todosLosItems = Object.FindObjectsByType<ItemInteractable>(FindObjectsSortMode.None);
        foreach (ItemInteractable item in todosLosItems)
        {
            if (item.idGrupoDrop == this.idGrupoDrop)
            {
                Destroy(item.gameObject);
            }
        }
    }
}