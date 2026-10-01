using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Resaltado de los botones del menú principal: fade del marco, color + resplandor del texto y escala leve.
// El hover selecciona el botón, así mouse y teclado/joystick comparten un único resaltado.
// Usa tiempo sin escalar para animar aunque el juego esté pausado.
public class BotonMenuFX : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    private static readonly int idColorResplandor = Shader.PropertyToID("_UnderlayColor");
    private static readonly int idSuavidadResplandor = Shader.PropertyToID("_UnderlaySoftness");
    private static readonly int idDilatacionResplandor = Shader.PropertyToID("_UnderlayDilate");

    [SerializeField] private Graphic marco;
    [SerializeField] private TMP_Text texto;

    [Header("Colores")]
    [SerializeField] private Color colorMarco = new Color32(0x9B, 0x5D, 0xE5, 0xFF);
    [SerializeField] private Color colorTextoNormal = new Color32(0xC4, 0xB5, 0xFD, 0xFF);
    [SerializeField] private Color colorTextoSeleccionado = new Color32(0xEE, 0xE5, 0xFF, 0xFF);
    [SerializeField] private Color colorResplandor = new Color32(0xB3, 0x88, 0xFF, 0xFF);

    [Header("Animación")]
    [SerializeField] private float escalaSeleccionado = 1.05f;
    [Tooltip("Inversa de la duración de la transición (10 = 0.1 s)")]
    [SerializeField] private float velocidad = 10f;

    private bool seleccionado;
    private float progreso;
    private Material materialTexto;

    public void Configurar(Graphic marco, TMP_Text texto)
    {
        this.marco = marco;
        this.texto = texto;
    }

    private void Awake()
    {
        if (texto == null) return;

        // Instancia propia del material para que cada botón tenga su resplandor.
        materialTexto = texto.fontMaterial;
        materialTexto.EnableKeyword("UNDERLAY_ON");
        materialTexto.SetFloat(idSuavidadResplandor, 0.8f);
        materialTexto.SetFloat(idDilatacionResplandor, 0.5f);
    }

    private void OnEnable()
    {
        seleccionado = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        progreso = seleccionado ? 1f : 0f;
        Aplicar();
    }

    private void Update()
    {
        float objetivo = seleccionado ? 1f : 0f;
        if (progreso == objetivo) return;

        progreso = Mathf.MoveTowards(progreso, objetivo, Time.unscaledDeltaTime * velocidad);
        Aplicar();
    }

    public void OnSelect(BaseEventData eventData) => seleccionado = true;

    public void OnDeselect(BaseEventData eventData) => seleccionado = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    private void Aplicar()
    {
        float k = Mathf.SmoothStep(0f, 1f, progreso);

        transform.localScale = Vector3.one * Mathf.Lerp(1f, escalaSeleccionado, k);

        if (marco != null) marco.color = new Color(colorMarco.r, colorMarco.g, colorMarco.b, k);
        if (texto != null) texto.color = Color.Lerp(colorTextoNormal, colorTextoSeleccionado, k);
        if (materialTexto != null)
            materialTexto.SetColor(idColorResplandor, new Color(colorResplandor.r, colorResplandor.g, colorResplandor.b, 0.85f * k));
    }
}
