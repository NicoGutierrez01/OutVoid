using UnityEngine;
using System.Collections.Generic;

public class Magnet : MonoBehaviour
{
    [Header("Atracción")]
    [SerializeField] private float radioAtraccion = 50f;
    [SerializeField] private float velocidadAtraccion = 20f;
    [SerializeField] private float aceleracion = 40f;
    [SerializeField] private float distanciaRecogida = 1.5f;

    [Header("Duración")]
    [SerializeField] private float duracionMagneto = 8f;
    [Header("Efectos Visuales (Estilo San Andreas)")]
    public float velocidadGiro = 90f;

    private Transform jugador;
    private bool activo = false;
    private float tiempoRestante;

    private readonly List<ItemRecurso> recursosAtraidos = new List<ItemRecurso>();

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[MAGNET] Trigger detectado con: {other.name} | Tag: {other.tag}");

        if (activo)
            return;

        // Buscamos el Player aunque el collider que entre
        // pertenezca a un hijo del jugador.
        if (other.CompareTag("Player") || other.GetComponentInParent<Player>() != null)
        {
            jugador = other.GetComponentInParent<Player>()?.transform;

            if (jugador == null)
                jugador = other.transform;

            ActivarMagneto();
        }
    }

    private void ActivarMagneto()
    {
        activo = true;
        tiempoRestante = duracionMagneto;

        Debug.Log("[MAGNET] ¡¡¡MAGNETO ACTIVADO!!!");

        BuscarRecursos();
    }

    private void Update()
    {
     
        transform.Rotate(
            Vector3.up,
            velocidadGiro * Time.deltaTime,
            Space.World
        );
        if (!activo)
            return;

        if (jugador == null)
        {
            Debug.LogWarning("[MAGNET] Se perdió la referencia del jugador.");
            return;
        }

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
        {
            DesactivarMagneto();
            return;
        }

        BuscarRecursos();
        MoverRecursos();
    }

    private void BuscarRecursos()
    {
        ItemRecurso[] recursos = FindObjectsByType<ItemRecurso>(
            FindObjectsSortMode.None
        );

        foreach (ItemRecurso recurso in recursos)
        {
            if (recurso == null)
                continue;

            if (recursosAtraidos.Contains(recurso))
                continue;

            float distancia = Vector3.Distance(
                jugador.position,
                recurso.transform.position
            );

            if (distancia <= radioAtraccion)
            {
                recurso.ActivarAtraccion();

recursosAtraidos.Add(recurso);

Debug.Log(
    $"[MAGNET] Atrayendo recurso: {recurso.name} | " +
    $"Distancia: {distancia:F1}m"
);
            }
        }
    }

    private void MoverRecursos()
    {
        for (int i = recursosAtraidos.Count - 1; i >= 0; i--)
        {
            ItemRecurso recurso = recursosAtraidos[i];

            if (recurso == null)
            {
                recursosAtraidos.RemoveAt(i);
                continue;
            }

            Vector3 direccion =
                (jugador.position - recurso.transform.position).normalized;

            float distancia = Vector3.Distance(
                jugador.position,
                recurso.transform.position
            );

            float factorDistancia = 1f -
                Mathf.Clamp01(distancia / radioAtraccion);

            float velocidad =
                velocidadAtraccion +
                (velocidadAtraccion * 2f * factorDistancia);

            recurso.transform.position +=
                direccion * velocidad * Time.deltaTime;

            // Cuando está suficientemente cerca,
            // lo llevamos al centro del Player.
            if (distancia <= distanciaRecogida)
            {
                recurso.transform.position = jugador.position;
            }
        }
    }

    private void DesactivarMagneto()
    {
        activo = false;
        recursosAtraidos.Clear();

        Debug.Log("[MAGNET] Magneto terminado.");

        Destroy(gameObject);
    }
}