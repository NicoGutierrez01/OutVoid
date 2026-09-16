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

    // Referencias para ocultar el pickup mientras está activo
    private MeshRenderer[] renderers;
    private Collider col;

    private void Awake()
    {
        renderers = GetComponentsInChildren<MeshRenderer>();
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activo)
            return;

        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerCharacter>() != null)
        {
            Transform foundPlayer = other.GetComponentInParent<PlayerCharacter>()?.transform;
            jugador = foundPlayer != null ? foundPlayer : other.transform;

            ActivarMagneto();
        }
    }

    private void ActivarMagneto()
    {
        activo = true;
        tiempoRestante = duracionMagneto;

        // Ocultar visualmente y apagar collider para que no se pueda interactuar mientras está activo
        SetVisuales(false);

        BuscarRecursos();
    }

    private void Update()
    {
        if (!activo)
        {
            transform.Rotate(
                Vector3.up,
                velocidadGiro * Time.deltaTime,
                Space.World
            );
            return;
        }

        if (jugador == null) return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
        {
            DesactivarYReubicarMagneto();
            return;
        }

        BuscarRecursos();
        MoverRecursos();
    }

    private void BuscarRecursos()
    {
        ItemRecurso[] recursos = FindObjectsByType<ItemRecurso>(FindObjectsSortMode.None);

        foreach (ItemRecurso recurso in recursos)
        {
            if (recurso == null || recursosAtraidos.Contains(recurso))
                continue;

            float distancia = Vector3.Distance(jugador.position, recurso.transform.position);

            if (distancia <= radioAtraccion)
            {
                recurso.ActivarAtraccion();
                recursosAtraidos.Add(recurso);
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

            Vector3 direccion = (jugador.position - recurso.transform.position).normalized;
            float distancia = Vector3.Distance(jugador.position, recurso.transform.position);
            float factorDistancia = 1f - Mathf.Clamp01(distancia / radioAtraccion);
            float velocidad = velocidadAtraccion + (velocidadAtraccion * 2f * factorDistancia);

            recurso.transform.position += direccion * velocidad * Time.deltaTime;

            if (distancia <= distanciaRecogida)
            {
                recurso.transform.position = jugador.position;
            }
        }
    }

    private void DesactivarYReubicarMagneto()
    {
        activo = false;
        recursosAtraidos.Clear();

        // Obtener nueva posición desde MapManager
        if (MapManager.Instance != null)
        {
            transform.position = MapManager.Instance.ObtenerNuevaPosicionMagneto(transform.position);
        }

        // Volver a mostrarlo para que el jugador lo encuentre en su nuevo punto
        SetVisuales(true);
    }

    private void SetVisuales(bool visible)
    {
        if (col != null) col.enabled = visible;

        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}