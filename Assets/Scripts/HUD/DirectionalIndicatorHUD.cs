using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DirectionalIndicatorHUD : MonoBehaviour
{
    public static DirectionalIndicatorHUD Instance;

    [Header("Referencias")]
    [Tooltip("El contenedor centrado en la mira (Anchors 0.5, 0.5 con Pos X/Y = 0)")]
    public RectTransform contenedorCentro; 
    public GameObject prefabFlecha;

    [Header("Radio del Círculo")]
    [Tooltip("Radio del círculo en píxeles que envuelve todos los iconos de la mira")]
    public float radioOrbita = 90f; 

    [Header("Ajustes de Flecha de Impacto")]
    public float duracionImpacto = 1.6f;

    [Header("Radar de Proximidad (Enemigos cercanos)")]
    public bool detectarEnemigosCercanos = true;
    public float rangoDeteccion = 22f; // Rango ligeramente ampliado
    public float intervaloChequeo = 0.15f;

    public static bool estaBloqueado = false;

    private Transform camaraJugador;
    private Transform playerTransform;
    private List<IndicadorProximidad> flechasProximidad = new List<IndicadorProximidad>();

    private class IndicadorProximidad
    {
        public Transform enemigo;
        public RectTransform rect;
        public CanvasGroup canvasGroup;
    }

    void Awake()
    {
        Instance = this;
        estaBloqueado = false; // Reset obligatorio al iniciar escena
    }

    void Start()
    {
        ActualizarReferencias();

        if (detectarEnemigosCercanos)
        {
            StartCoroutine(RutinaActualizarEnemigosCercanos());
        }
    }

    void ActualizarReferencias()
    {
        if (camaraJugador == null && Camera.main != null) 
            camaraJugador = Camera.main.transform;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    public bool DebeEstarOculto()
    {
        return estaBloqueado || Time.timeScale <= 0f;
    }

    void LateUpdate()
    {
        if (DebeEstarOculto())
        {
            if (flechasProximidad.Count > 0) LimpiarTodasLasFlechas();
            return;
        }

        if (camaraJugador == null)
        {
            ActualizarReferencias();
            return;
        }

        for (int i = flechasProximidad.Count - 1; i >= 0; i--)
        {
            var item = flechasProximidad[i];
            if (item.enemigo == null || !item.enemigo.gameObject.activeInHierarchy)
            {
                Destroy(item.rect.gameObject);
                flechasProximidad.RemoveAt(i);
                continue;
            }

            ActualizarTransformFlecha(item.rect, item.enemigo.position);
        }
    }

    public static void PausarIndicadores(bool pausar)
    {
        estaBloqueado = pausar;

        if (pausar && Instance != null)
        {
            Instance.LimpiarTodasLasFlechas();
        }
    }

    public void LimpiarTodasLasFlechas()
    {
        for (int i = flechasProximidad.Count - 1; i >= 0; i--)
        {
            if (flechasProximidad[i].rect != null)
            {
                Destroy(flechasProximidad[i].rect.gameObject);
            }
        }
        flechasProximidad.Clear();
    }

    public void RegistrarImpacto(Vector3 posicionAgresor)
    {
        if (DebeEstarOculto()) return;
        if (prefabFlecha == null || contenedorCentro == null) return;
        
        ActualizarReferencias();
        if (camaraJugador == null) return;

        GameObject flecha = Instantiate(prefabFlecha, contenedorCentro);
        StartCoroutine(RutinaFlechaImpacto(flecha, posicionAgresor));
    }

    IEnumerator RutinaFlechaImpacto(GameObject flechaObj, Vector3 posicionAgresor)
    {
        RectTransform rect = flechaObj.GetComponent<RectTransform>();
        CanvasGroup cg = flechaObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = flechaObj.AddComponent<CanvasGroup>();

        float timer = 0f;

        while (timer < duracionImpacto)
        {
            if (DebeEstarOculto())
            {
                Destroy(flechaObj);
                yield break;
            }

            if (camaraJugador != null)
            {
                ActualizarTransformFlecha(rect, posicionAgresor);
            }

            timer += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, timer / duracionImpacto);
            yield return null;
        }

        Destroy(flechaObj);
    }

    void ActualizarTransformFlecha(RectTransform rect, Vector3 targetWorldPos)
    {
        Vector3 dirHaciaObjetivo = targetWorldPos - camaraJugador.position;
        dirHaciaObjetivo.y = 0;

        if (dirHaciaObjetivo.sqrMagnitude < 0.001f) return;

        Vector3 dirLocal = camaraJugador.InverseTransformDirection(dirHaciaObjetivo);
        dirLocal.y = 0;
        dirLocal.Normalize();

        float anguloPantalla = Mathf.Atan2(dirLocal.x, dirLocal.z) * Mathf.Rad2Deg;

        float anguloRad = (90f - anguloPantalla) * Mathf.Deg2Rad;
        float posX = Mathf.Cos(anguloRad) * radioOrbita;
        float posY = Mathf.Sin(anguloRad) * radioOrbita;
        rect.anchoredPosition = new Vector2(posX, posY);
        rect.localRotation = Quaternion.Euler(0f, 0f, -anguloPantalla + 180f);
    }

    IEnumerator RutinaActualizarEnemigosCercanos()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(intervaloChequeo);

            if (DebeEstarOculto())
            {
                if (flechasProximidad.Count > 0) LimpiarTodasLasFlechas();
                continue;
            }

            if (playerTransform == null)
            {
                ActualizarReferencias();
                continue;
            }

            Collider[] hits = Physics.OverlapSphere(playerTransform.position, rangoDeteccion);
            HashSet<Transform> enemigosDetectados = new HashSet<Transform>();

            foreach (var h in hits)
            {
                if (h.CompareTag("Enemigo") || (h.transform.root != null && h.transform.root.CompareTag("Enemigo")))
                {
                    Transform rootEnemy = h.transform.root;
                    if (!enemigosDetectados.Contains(rootEnemy))
                    {
                        enemigosDetectados.Add(rootEnemy);
                    }
                }
            }

            for (int i = flechasProximidad.Count - 1; i >= 0; i--)
            {
                if (!enemigosDetectados.Contains(flechasProximidad[i].enemigo))
                {
                    Destroy(flechasProximidad[i].rect.gameObject);
                    flechasProximidad.RemoveAt(i);
                }
            }

            foreach (Transform enemy in enemigosDetectados)
            {
                bool yaExiste = flechasProximidad.Exists(x => x.enemigo == enemy);
                if (!yaExiste && prefabFlecha != null && contenedorCentro != null)
                {
                    GameObject obj = Instantiate(prefabFlecha, contenedorCentro);
                    CanvasGroup cg = obj.GetComponent<CanvasGroup>();
                    if (cg == null) cg = obj.AddComponent<CanvasGroup>();
                    cg.alpha = 0.75f;

                    flechasProximidad.Add(new IndicadorProximidad
                    {
                        enemigo = enemy,
                        rect = obj.GetComponent<RectTransform>(),
                        canvasGroup = cg
                    });
                }
            }
        }
    }
}