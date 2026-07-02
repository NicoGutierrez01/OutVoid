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

        if (visualInstanciado != null)
        {
            Destroy(visualInstanciado);
        }

        if (puntoVisual != null && data.prefabVisual != null)
        {
            visualInstanciado = Instantiate(data.prefabVisual, puntoVisual);

            visualInstanciado.transform.localPosition = Vector3.zero;
            visualInstanciado.transform.localRotation = Quaternion.identity;
            visualInstanciado.transform.localScale = Vector3.one;
        }

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