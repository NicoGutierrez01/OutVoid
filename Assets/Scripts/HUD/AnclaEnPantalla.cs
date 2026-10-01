using UnityEngine;

// Ubica un objeto 3D (p. ej. un ParticleSystem) delante de un punto de un elemento de UI,
// para que siga al fondo o al logo en cualquier resolución o aspect ratio.
public class AnclaEnPantalla : MonoBehaviour
{
    [SerializeField] private Camera camara;
    [SerializeField] private RectTransform objetivo;
    [Tooltip("Punto dentro del objetivo: (0,0) abajo-izquierda, (1,1) arriba-derecha")]
    [SerializeField] private Vector2 puntoNormalizado = new Vector2(0.5f, 0.5f);
    [Tooltip("Distancia a la cámara; debe ser menor que el Plane Distance del canvas del fondo")]
    [SerializeField] private float distancia = 60f;

    private readonly Vector3[] esquinas = new Vector3[4];
    private Canvas canvasObjetivo;

    public void Configurar(Camera camara, RectTransform objetivo, Vector2 puntoNormalizado, float distancia)
    {
        this.camara = camara;
        this.objetivo = objetivo;
        this.puntoNormalizado = puntoNormalizado;
        this.distancia = distancia;
    }

    private void Start()
    {
        if (objetivo != null) canvasObjetivo = objetivo.GetComponentInParent<Canvas>().rootCanvas;
    }

    private void LateUpdate()
    {
        if (camara == null || objetivo == null || canvasObjetivo == null) return;

        // Esquinas: 0 abajo-izq, 1 arriba-izq, 3 abajo-der.
        objetivo.GetWorldCorners(esquinas);
        Vector3 punto = esquinas[0]
            + (esquinas[3] - esquinas[0]) * puntoNormalizado.x
            + (esquinas[1] - esquinas[0]) * puntoNormalizado.y;

        // En un canvas Overlay las coordenadas "de mundo" ya son píxeles de pantalla.
        Vector3 pantalla = canvasObjetivo.renderMode == RenderMode.ScreenSpaceOverlay
            ? punto
            : camara.WorldToScreenPoint(punto);

        transform.SetPositionAndRotation(
            camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, distancia)),
            camara.transform.rotation);
    }
}
