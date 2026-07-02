using UnityEngine;

public class ItemMejoraDinamica : MonoBehaviour
{
    private MejoraData data;

    [Header("Referencias Internas")]
    public Transform puntoVisual;
    public Light luzBaliza;

    private GameObject visualInstanciado;

    public void ConfigurarItem(MejoraData datosAsignados)
    {
        data = datosAsignados;

        // Elimina el visual anterior si existe
        if (visualInstanciado != null)
        {
            Destroy(visualInstanciado);
        }

        // Instancia el prefab asignado a la mejora
        if (puntoVisual != null && data.prefabVisual != null)
        {
            visualInstanciado = Instantiate(data.prefabVisual, puntoVisual);

            visualInstanciado.transform.localPosition = Vector3.zero;
            visualInstanciado.transform.localRotation = Quaternion.identity;
            visualInstanciado.transform.localScale = Vector3.one;
        }

        // Cambia el color de la luz
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
            switch (data.statAMejorar)
            {
                case TipoStat.VidaMaxima:
                    stats.maxHealth += data.valorSuma;
                    stats.Heal(data.valorSuma);
                    break;

                case TipoStat.Armadura:
                    break;
            }
        }
    }

    private void MostrarMensaje()
    {
        PlayerHUD hud = FindAnyObjectByType<PlayerHUD>();

        if (hud != null)
        {
            hud.MostrarItemRecogido($"{data.nombreMejora} ({data.rareza})\n{data.descripcion}");
        }
    }
}