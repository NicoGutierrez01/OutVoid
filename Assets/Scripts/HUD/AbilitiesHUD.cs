using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; 

// Corre después del Player: el estado de la ulti ya está actualizado en el frame en que se aprieta Q
[DefaultExecutionOrder(100)]
public class AbilitiesHUD : MonoBehaviour
{
    [Header("Referencias del Player")]
    public PlayerAbilities playerAbilities;

    [Serializable]
    public struct HabilidadUI
    {
        public GameObject slotBase;       
        public GameObject objetoActivo; 
        public GameObject objetoDesactivado;
        public TextMeshProUGUI txtCooldown; 
    }

    [Serializable]
    public struct UltiUI
    {
        [Tooltip("Objeto que se muestra cuando la ulti está lista (On)")]
        public GameObject objetoOn;
        [Tooltip("Objeto que se muestra durante cooldown (Off)")]
        public GameObject objetoOff;
        [Tooltip("Texto en el medio para el porcentaje")]
        public TextMeshProUGUI txtPorcentaje;
        [Tooltip("Respaldo: solo se usa si PlayerAbilities.ultCooldown es 0")]
        public float cooldownTotalBase;
    }

    [Header("Configuración de Slots")]
    public HabilidadUI UI_Dash;
    public HabilidadUI UI_Bomba;
    public UltiUI UI_Ulti;

    [Header("Efecto Ulti lista (partículas UI)")]
    [Tooltip("Opcional (debe ser claro, se tiñe con el color). Vacío = brillo radial blanco generado por código")]
    [SerializeField] private Sprite spriteParticula;
    [Tooltip("Cantidad de partículas orbitando el ícono")]
    [SerializeField] private int cantidadParticulas = 14;
    [Tooltip("Radio medio de la órbita (unidades del slot); ~205 queda por fuera del anillo del ícono")]
    [SerializeField] private float radioOrbita = 205f;
    [Tooltip("Variación aleatoria del radio por partícula")]
    [SerializeField] private float variacionRadio = 12f;
    [Tooltip("Velocidad angular mínima y máxima (grados/seg)")]
    [SerializeField] private Vector2 velocidadAngular = new Vector2(35f, 65f);
    [Tooltip("Amplitud de la oscilación radial (unidades del slot)")]
    [SerializeField] private float oscilacionRadial = 6f;
    [Tooltip("Tamaño mínimo y máximo de cada partícula (unidades del slot)")]
    [SerializeField] private Vector2 tamanioParticula = new Vector2(20f, 34f);
    [Tooltip("Velocidad del titileo de alfa y tamaño")]
    [SerializeField] private float velocidadTitileo = 3f;
    [Tooltip("Alfa máximo de las partículas")]
    [Range(0f, 1f)] [SerializeField] private float alfaMaximo = 0.9f;
    [SerializeField] private Color colorParticulaA = new Color32(0xC4, 0xB5, 0xFD, 0xFF);
    [SerializeField] private Color colorParticulaB = new Color32(0xA7, 0x8B, 0xFA, 0xFF);

    // Pulso de la ulti activa
    private Vector3 escalaBaseOn = Vector3.one;
    private bool pulsando;

    // Partículas (arrays creados en Start, sin allocations por frame)
    private GameObject contenedorParticulas;
    private RectTransform[] particulasRT;
    private Image[] particulasImg;
    private float[] radios, velocidades, angulosIniciales, fases, tamanios;
    private bool efectoVisible;
    private float inicioEfecto;
    private Texture2D texturaGenerada;
    private Sprite spriteGenerado;

    void Start()
    {
        if (UI_Ulti.objetoOn == null) return;
        escalaBaseOn = UI_Ulti.objetoOn.transform.localScale;
        CrearParticulas();
    }

    void CrearParticulas()
    {
        Transform slot = UI_Ulti.objetoOn.transform.parent;
        if (slot == null || cantidadParticulas <= 0) return;

        Sprite sprite = spriteParticula != null ? spriteParticula : CrearSpriteRadial();

        // Contenedor centrado en el ícono, justo encima de Background (detrás de On y del número)
        RectTransform onRT = (RectTransform)UI_Ulti.objetoOn.transform;
        contenedorParticulas = new GameObject("ParticulasUltiLista", typeof(RectTransform), typeof(Canvas));
        RectTransform cont = (RectTransform)contenedorParticulas.transform;
        cont.SetParent(slot, false);
        cont.SetSiblingIndex(onRT.GetSiblingIndex());
        cont.anchorMin = cont.anchorMax = cont.pivot = new Vector2(0.5f, 0.5f);
        cont.sizeDelta = Vector2.zero;
        cont.localPosition = onRT.localPosition + (Vector3)onRT.rect.center;

        int n = cantidadParticulas;
        particulasRT = new RectTransform[n];
        particulasImg = new Image[n];
        radios = new float[n]; velocidades = new float[n]; angulosIniciales = new float[n];
        fases = new float[n]; tamanios = new float[n];

        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Particula", typeof(RectTransform), typeof(Image));
            particulasRT[i] = (RectTransform)go.transform;
            particulasRT[i].SetParent(cont, false);
            go.TryGetComponent(out particulasImg[i]);
            particulasImg[i].sprite = sprite;
            particulasImg[i].raycastTarget = false;

            radios[i] = radioOrbita + UnityEngine.Random.Range(-variacionRadio, variacionRadio);
            // Sentido alternado para que no giren todas juntas
            velocidades[i] = UnityEngine.Random.Range(velocidadAngular.x, velocidadAngular.y) * Mathf.Deg2Rad * (i % 3 == 0 ? -1f : 1f);
            tamanios[i] = UnityEngine.Random.Range(tamanioParticula.x, tamanioParticula.y);
        }
        contenedorParticulas.SetActive(false);
    }

    // Brillo radial blanco (alfa (1-r)^2, como brillo_radial.png del Main Menu): se tiñe con Image.color
    Sprite CrearSpriteRadial()
    {
        const int lado = 64;
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[lado * lado];
        for (int y = 0; y < lado; y++)
            for (int x = 0; x < lado; x++)
            {
                float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(lado, lado) * 0.5f) / (lado * 0.5f);
                float a = Mathf.Clamp01(1f - r);
                px[y * lado + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        texturaGenerada = tex;
        spriteGenerado = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f));
        return spriteGenerado;
    }

    // UIScene se recarga en cada mapa: liberar sprite y textura generados para no acumularlos.
    // El contenedor también se destruye por si el componente se elimina sin la escena.
    void OnDestroy()
    {
        if (contenedorParticulas != null) Destroy(contenedorParticulas);
        if (spriteGenerado != null) Destroy(spriteGenerado);
        if (texturaGenerada != null) Destroy(texturaGenerada);
    }

    void Update()
    {
        if (playerAbilities == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerAbilities = p.GetComponentInChildren<PlayerAbilities>();

            if (playerAbilities == null) { SetEfectoVisible(false); return; }
        }

        ActualizarSlot(playerAbilities.canDash, playerAbilities.dashCooldownTimer, UI_Dash);
        ActualizarSlot(playerAbilities.canUseE, playerAbilities.dynamiteCooldownTimer, UI_Bomba);

        ActualizarUlti(playerAbilities.canUseQ, playerAbilities.ultCooldownTimer, UI_Ulti);

        // Oculto durante el dash; al terminar vuelve con el fade-in si la ulti sigue lista
        SetEfectoVisible(playerAbilities.canUseQ && !playerAbilities.isUltActive && playerAbilities.ultCooldownTimer <= 0f && !playerAbilities.DashActivo);
        // Tiempo escalado: en pausa/DevConsole (timeScale 0) el efecto queda congelado
        if (efectoVisible && Time.timeScale > 0f) AnimarParticulas();
    }

    // Solo actúa en el cambio de estado
    void SetEfectoVisible(bool visible)
    {
        if (visible == efectoVisible || contenedorParticulas == null) return;
        efectoVisible = visible;
        contenedorParticulas.SetActive(visible);
        if (!visible) return;

        // Reiniciar fases al volver a estar lista
        inicioEfecto = Time.time;
        int n = particulasRT.Length;
        for (int i = 0; i < n; i++)
        {
            angulosIniciales[i] = (i * Mathf.PI * 2f / n) + UnityEngine.Random.Range(-0.3f, 0.3f);
            fases[i] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }
        AnimarParticulas();
    }

    void AnimarParticulas()
    {
        float t = Time.time - inicioEfecto;
        for (int i = 0; i < particulasRT.Length; i++)
        {
            float ang = angulosIniciales[i] + velocidades[i] * t;
            float r = radios[i] + oscilacionRadial * Mathf.Sin(t * 1.7f + fases[i]);
            float titileo = Mathf.Sin(t * velocidadTitileo + fases[i]);

            particulasRT[i].anchoredPosition = new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
            float tam = tamanios[i] * (0.85f + 0.15f * titileo);
            particulasRT[i].sizeDelta = new Vector2(tam, tam);

            Color c = (i % 2 == 0) ? colorParticulaA : colorParticulaB;
            // Fade-in de 0.3 s al aparecer para que no salten de golpe
            c.a = alfaMaximo * (0.55f + 0.45f * titileo) * Mathf.Clamp01(t / 0.3f);
            particulasImg[i].color = c;
        }
    }

    void ActualizarSlot(bool listo, float tiempoRestante, HabilidadUI ui)
    {
        if (ui.objetoActivo == null || ui.objetoDesactivado == null) return;

        if (listo)
        {
            ui.objetoActivo.SetActive(true);
            ui.objetoDesactivado.SetActive(false);
            
            if (ui.txtCooldown != null)
            {
                ui.txtCooldown.text = ""; 
            }
        }
        else
        {
            ui.objetoActivo.SetActive(false);
            ui.objetoDesactivado.SetActive(true);
            
            if (ui.txtCooldown != null)
            {
                // Durante el dash el timer aún no arrancó (0): no mostrar "0"
                ui.txtCooldown.text = tiempoRestante > 0f ? Mathf.CeilToInt(tiempoRestante).ToString() : "";
            }
        }
    }

    void ActualizarUlti(bool listo, float tiempoRestante, UltiUI ui)
    {
        if (ui.objetoOn == null || ui.objetoOff == null) return;

        // Ulti en curso: icono encendido con pulso sutil (tiempo escalado: se congela en pausa)
        if (playerAbilities.isUltActive)
        {
            ui.objetoOn.SetActive(true);
            ui.objetoOff.SetActive(false);
            ui.objetoOn.transform.localScale = escalaBaseOn * (1f + 0.06f * Mathf.Sin(Time.time * 8f));
            pulsando = true;
            if (ui.txtPorcentaje != null) ui.txtPorcentaje.text = "";
            return;
        }
        if (pulsando)
        {
            ui.objetoOn.transform.localScale = escalaBaseOn;
            pulsando = false;
        }

        if (listo || tiempoRestante <= 0f)
        {
            ui.objetoOn.SetActive(true);
            ui.objetoOff.SetActive(false);

            if (ui.txtPorcentaje != null)
            {
                ui.txtPorcentaje.text = "";
            }
        }
        else
        {
            ui.objetoOn.SetActive(false);
            ui.objetoOff.SetActive(true);

            if (ui.txtPorcentaje != null)
            {
                // Cooldown real del jugador; cooldownTotalBase queda como respaldo
                float total = playerAbilities.ultCooldown > 0f ? playerAbilities.ultCooldown : ui.cooldownTotalBase;
                if (total > 0f)
                {
                    float progreso = Mathf.Clamp01(1f - (tiempoRestante / total));
                    ui.txtPorcentaje.text = $"{Mathf.FloorToInt(progreso * 100f)}%";
                }
                else
                {
                    ui.txtPorcentaje.text = $"{Mathf.CeilToInt(tiempoRestante)}s";
                }
            }
        }
    }
}