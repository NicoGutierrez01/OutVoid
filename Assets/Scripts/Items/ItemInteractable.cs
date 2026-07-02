using UnityEngine;
using UnityEngine.SceneManagement; 

public class ItemInteractable : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform puntoVisual; 

    [Header("Datos del Ítem")]
    [SerializeField] private ItemMejoraData dataItem; 

    public ItemMejoraData data => dataItem;

    private GameObject visualActual;
    private string idDropOrigen; 

    private void Start()
    {
        ActualizarVisual();
    }

    public void ActualizarVisual()
    {
        if (puntoVisual == null)
        {
            Debug.LogWarning($"Falta asignar el punto visual en {gameObject.name}");
            return;
        }

        if (dataItem == null)
        {
            return; 
        }

        if (visualActual != null)
        {
            Destroy(visualActual);
        }
        else
        {
            foreach (Transform hijo in puntoVisual)
            {
                Destroy(hijo.gameObject);
            }
        }

        if (dataItem.mallaEspecial != null)
        {
            visualActual = Instantiate(dataItem.mallaEspecial, puntoVisual);
            
            visualActual.transform.localPosition = Vector3.zero;
            visualActual.transform.localRotation = Quaternion.identity;
            visualActual.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.LogWarning($"El ítem {dataItem.nombreItem} no tiene una mallaEspecial asignada en su Data.");
        }
    }

    public void Configurar(ItemMejoraData nuevaData, string idDrop)
    {
        dataItem = nuevaData;
        idDropOrigen = idDrop; 
        ActualizarVisual(); 
    }

    public void Recoger(GameObject jugador)
    {
        Debug.Log($"¡Ítem {dataItem.nombreItem} recogido por el jugador!");

        StaticVariables.SessionData.round = 1; 

        if (MapManager.Instance != null)
        {
            MapManager.Instance.ColapsarMapa();
        }
        else
        {
            Debug.LogError("No se encontró una instancia activa de MapManager en la escena.");
        }

        Destroy(gameObject);
    }
}