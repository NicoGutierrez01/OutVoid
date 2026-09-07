using System.Collections;
using UnityEngine;

public class ItemDespawnTemporal : MonoBehaviour
{
    [Header("Ajustes de Tiempo")]
    public float tiempoVidaNormal = 10f;
    public float tiempoParpadeo = 5f;
    public float velocidadParpadeo = 0.2f;

    private Renderer renderItem;

    void Start()
    {
        renderItem = GetComponent<Renderer>();

        if (renderItem == null) 
        {
            renderItem = GetComponentInChildren<Renderer>();
        }

        StartCoroutine(RutinaCicloDeVida());
    }

    System.Collections.IEnumerator RutinaCicloDeVida()
    {
        yield return new WaitForSeconds(tiempoVidaNormal);

        float tiempoRestante = tiempoParpadeo;
        bool estaVisible = true;

        while (tiempoRestante > 0)
        {
            if (renderItem != null)
            {
                estaVisible = !estaVisible; 
                renderItem.enabled = estaVisible;
            }

            yield return new WaitForSeconds(velocidadParpadeo);
            tiempoRestante -= velocidadParpadeo;
        }

        Destroy(gameObject);
    }
}