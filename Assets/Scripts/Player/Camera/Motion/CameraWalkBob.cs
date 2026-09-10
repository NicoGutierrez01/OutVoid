using UnityEngine;

public class CameraWalkBob : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerCharacter playerCharacter;

    [Header("Movimiento")]
    [SerializeField] private float frecuencia = 9f;
    [SerializeField] private float amplitudVertical = 0.025f;
    [SerializeField] private float amplitudHorizontal = 0.015f;

    [Header("Suavizado")]
    [SerializeField] private float velocidadEntrada = 12f;
    [SerializeField] private float velocidadSalida = 14f;

    private Vector3 posicionInicial;
    private float tiempoBob;
    private float intensidadActual;

    private void Start()
    {
        posicionInicial = transform.localPosition;

        if (playerCharacter == null)
        {
            playerCharacter = GetComponentInParent<PlayerCharacter>();
        }
    }

    private void LateUpdate()
    {
        if (playerCharacter == null)
            return;

        CharacterState state = playerCharacter.GetState();

        // Movimiento real del personaje sobre el suelo.
        Vector3 velocidadHorizontal = Vector3.ProjectOnPlane(
            state.Velocity,
            Vector3.up
        );

        bool estaCaminando =
            state.Grounded &&
            state.Stance == Stance.Stand &&
            velocidadHorizontal.sqrMagnitude > 0.5f;

        float intensidadObjetivo = estaCaminando ? 1f : 0f;

        float velocidadSuavizado =
            estaCaminando ? velocidadEntrada : velocidadSalida;

        intensidadActual = Mathf.MoveTowards(
            intensidadActual,
            intensidadObjetivo,
            velocidadSuavizado * Time.deltaTime
        );

        // Avanzamos el ciclo solamente cuando estamos caminando.
        if (intensidadActual > 0.01f)
        {
            tiempoBob += Time.deltaTime * frecuencia;
        }

        float bobVertical = Mathf.Sin(tiempoBob * 2f)
            * amplitudVertical;

        float bobHorizontal = Mathf.Cos(tiempoBob)
            * amplitudHorizontal;

        Vector3 movimientoBob = new Vector3(
            bobHorizontal,
            bobVertical,
            0f
        ) * intensidadActual;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            posicionInicial + movimientoBob,
            1f - Mathf.Exp(-15f * Time.deltaTime)
        );
    }
}