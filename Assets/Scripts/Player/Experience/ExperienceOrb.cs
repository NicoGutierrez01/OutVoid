using UnityEngine;

public class ExperienceOrb : MonoBehaviour
{
    [Header("Experiencia")]
    public float cantidadExperiencia = 10f;

    [Header("Movimiento (Estilo San Andreas)")]
    public float velocidadGiro = 120f;

    [Tooltip("Distancia extra que se eleva desde el suelo para que nunca lo toque")]
    public float alturaMinimaSuelo = 0.35f;

    [Tooltip("Cuánto sube y baja flotando por encima de la altura mínima")]
    public float amplitudFlotacion = 0.15f;
    public float frecuenciaFlotacion = 3f;

    private Vector3 posicionBase;

    private void Start()
    {
        // Se eleva inicialmente respecto a donde spawneó para no quedar pegado al suelo
        posicionBase = transform.position + Vector3.up * alturaMinimaSuelo;
        transform.position = posicionBase;
    }

    private void Update()
    {
        transform.Rotate(
            Vector3.up,
            velocidadGiro * Time.deltaTime,
            Space.World
        );

        // Oscilación suave basada en la posición base elevada
        float offsetY =
            Mathf.Sin(Time.time * frecuenciaFlotacion)
            * amplitudFlotacion;

        transform.position = new Vector3(
            posicionBase.x,
            posicionBase.y + offsetY,
            posicionBase.z
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (ExperienceManager.Instancia != null)
        {
            ExperienceManager.Instancia.AgregarExperiencia(
                cantidadExperiencia
            );
        }

        Destroy(gameObject);
    }
}