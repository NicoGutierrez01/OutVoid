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

    [Header("Imán hacia el jugador")]
    [Tooltip("Velocidad con la que arranca a viajar hacia el jugador")]
    [SerializeField] private float velocidadInicial = 4f;

    [Tooltip("Cuánto acelera por segundo mientras viaja")]
    [SerializeField] private float aceleracion = 25f;

    [SerializeField] private float velocidadMaxima = 40f;

    [Tooltip("Altura sobre los pies del jugador a la que apunta (centro de la cápsula)")]
    [SerializeField] private float alturaObjetivo = 1f;

    [Tooltip("Distancia a la que se recoge aunque el trigger no haya saltado")]
    [SerializeField] private float distanciaRecogida = 0.5f;

    // Compartido por todos los orbes: se busca una sola vez por escena
    private static Transform jugador;

    private Vector3 posicionBase;
    private float velocidadActual;
    private bool recogido;

    private void Start()
    {
        // Se eleva inicialmente respecto a donde spawneó para no quedar pegado al suelo
        posicionBase = transform.position + Vector3.up * alturaMinimaSuelo;
        transform.position = posicionBase;
        velocidadActual = velocidadInicial;

        // Root y Character tienen tag Player; el que se mueve es Character (PlayerCharacter)
        if (jugador == null)
        {
            PlayerCharacter pc = FindFirstObjectByType<PlayerCharacter>();
            if (pc != null)
                jugador = pc.transform;
        }
    }

    private void Update()
    {
        transform.Rotate(
            Vector3.up,
            velocidadGiro * Time.deltaTime,
            Space.World
        );

        if (jugador == null)
        {
            // Sin jugador: flota en el lugar como antes
            float offsetY =
                Mathf.Sin(Time.time * frecuenciaFlotacion)
                * amplitudFlotacion;

            transform.position = new Vector3(
                posicionBase.x,
                posicionBase.y + offsetY,
                posicionBase.z
            );
            return;
        }

        // Viaja hacia el jugador acelerando hasta la velocidad máxima
        Vector3 objetivo = jugador.position + Vector3.up * alturaObjetivo;
        velocidadActual = Mathf.Min(velocidadActual + aceleracion * Time.deltaTime, velocidadMaxima);
        transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidadActual * Time.deltaTime);

        if ((transform.position - objetivo).sqrMagnitude <= distanciaRecogida * distanciaRecogida)
            Recoger();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            Recoger();
    }

    private void Recoger()
    {
        // Evita sumar dos veces si tocan varios colliders del jugador en el mismo frame
        if (recogido)
            return;
        recogido = true;

        if (ExperienceManager.Instancia != null)
        {
            ExperienceManager.Instancia.AgregarExperiencia(
                cantidadExperiencia
            );
        }

        Destroy(gameObject);
    }
}
