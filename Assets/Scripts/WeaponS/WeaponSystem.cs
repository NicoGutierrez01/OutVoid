using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Serialization;

public class WeaponSystem : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    public float damage = 20f;
    public float multiplicadorHeadshot = 2f; 
    private bool r2EstabaPresionado = false;
    public float range = 100f; 
    public Transform cam; 
    [Header("Daño por Distancia")]
public float distanciaDanioMaximo = 15f;
public float distanciaDanioMinimo = 40f;
[Range(0f, 1f)]
public float multiplicadorDanioLargoAlcance = 0.7f;

    [Header("Munición (Revólver)")]
    public int balasMaximas = 6;
    [HideInInspector] 
    public int balasActuales;
    public int balasReserva = 24; 
    public float tiempoRecarga = 1.5f;
    public bool recargando = false;

    private int limitePocasBalas = 12; 

    [Header("Animaciones")]
    public Animator gunAnim; 
    public Animator gunAnimIzquierda; 
    private bool dispararDerecha = true; 

    [HideInInspector] public bool isUltActive = false; 

    [Header("Cadencia de Tiro")]
    [Tooltip("Si la animación contiene 'Shoot', se sobreescribe automáticamente en Start().")]
    public float fireRate = 0.25f; 
    public bool sincronizarCadenciaConClip = true;
    private float proximoTiempoDisparo = 0f;
    public ParticleSystem muzzleFlash;
    public ParticleSystem muzzleFlashIzquierda;

    [Header("Efectos de Impacto (Partículas)")]
    public GameObject prefabImpactoRobot; 
    public GameObject prefabImpactoEntorno;

    [Header("Mejoras")]
    public bool tieneFuego = false;
    [Header("Debug - Tipos de Disparo")]
    public bool disparoTriple = false;
    public bool balasPenetrantes = false;

    [Tooltip("Ángulo entre el disparo central y cada disparo lateral.")]
    public float anguloDisparoTriple = 8f;

    private CrosshairFeedbackManager crosshairFeedback;

    [Header("Tracer Visual")]
    public bool mostrarTracer = true;
    public float duracionTracer = 0.08f;
    public float grosorTracer = 0.0025f;
    public Material materialTracer;

    [Header("Munición Explosiva")]
    public bool balasExplosivas = false;
    public float radioExplosionBala = 3.5f;
    public float porcentajeDanoExplosion = 0.6f;
    public GameObject prefabMicroExplosion;

    [Header("Reembolso de Balas")]
    [Range(0f, 1f)] public float chanceDevolverBalaKill = 0.35f;

    [Header("Uzi (reemplaza al revólver al subir de nivel)")]
    public int nivelDesbloqueoUzi = 5;
    public float uziDamage = 13f;
    public float uziFireRate = 0.08f;
    public float uziMultiplicadorHeadshot = 1.5f;
    public float uziDistanciaDanioMaximo = 8f;
    public float uziDistanciaDanioMinimo = 22f;
    [Range(0f, 1f)] public float uziMultiplicadorDanioLargoAlcance = 0.5f;
    public int uziBalasMaximas = 30;
    public float uziTiempoRecarga = 1.8f;
    [Tooltip("Cuántas balas de Uzi equivalen a una de revólver (drops, reembolsos, reserva).")]
    public float uziEscalaMunicion = 5f;
    [HideInInspector] public bool usandoUzi = false;
    [HideInInspector] public float escalaMunicion = 1f;

    [Header("Uzi - Dispersión (grados)")]
    public float dispersionBase = 0.6f;
    public float dispersionMaxima = 3.5f;
    public float dispersionPorDisparo = 0.35f;
    public float recuperacionDispersion = 6f;
    private float dispersionActual;

    [Header("Uzi - Retroceso Visual")]
    public float retrocesoCamaraVertical = 0.5f;
    public float retrocesoCamaraHorizontal = 0.25f;
    public float retrocesoCamaraMaximo = 3f;
    public float retrocesoManosDistancia = 0.03f;
    public float retrocesoManosAngulo = 4f;
    public float recuperacionRetroceso = 12f;
    private Vector2 retrocesoCamara;
    private float retrocesoManos;
    private Quaternion rotacionBaseCam;

    [Header("Uzi - Modelo y Animaciones")]
    [Tooltip("Hands-Uzi dentro del prefab (inactivo, ya ubicado frente a la cámara).")]
    [FormerlySerializedAs("uziPrefab")]
    [SerializeField] private GameObject uziDerecha;
    [Tooltip("Opcional: si se asigna, reemplaza el controller que ya tenga Hands-Uzi.")]
    [SerializeField] private RuntimeAnimatorController uziAnimator;
    [SerializeField] private string estadoDisparoUzi = "Armature|Shoot";
    [SerializeField] private string estadoRecargaUzi = "Armature|Recharge";
    [Tooltip("Hueso del modelo de la Uzi al que se mueve el muzzle flash.")]
    [SerializeField] private string huesoCanonUzi = "Uzi";
    [Tooltip("La Uzi de la ulti es una copia de la derecha reflejada al lado izquierdo de la cámara.")]
    [SerializeField] private bool espejarUziIzquierda = true;
    private Transform manosDerecha, manosIzquierda;
    private Vector3 posBaseManosDer, posBaseManosIzq;
    private Quaternion rotBaseManosDer, rotBaseManosIzq;
    private float duracionClipDisparo, duracionClipRecarga;

    private float danioBaseRevolver;
    private float recargaBaseRevolver;

    void Start()
    {
        balasActuales = balasMaximas;
        danioBaseRevolver = damage;
        recargaBaseRevolver = tiempoRecarga;
        if (gunAnim == null) gunAnim = GetComponentInChildren<Animator>();

        // Los Animator del revólver están en la raíz de Hands / LeftHands.
        if (gunAnim != null) { manosDerecha = gunAnim.transform; posBaseManosDer = manosDerecha.localPosition; rotBaseManosDer = manosDerecha.localRotation; }
        if (gunAnimIzquierda != null) { manosIzquierda = gunAnimIzquierda.transform; posBaseManosIzq = manosIzquierda.localPosition; rotBaseManosIzq = manosIzquierda.localRotation; }

        if (sincronizarCadenciaConClip)
        {
            SincronizarCadenciaConAnimacion();
        }

        if (AdministradorDeProgreso.Instancia != null)
        {
            damage *= AdministradorDeProgreso.Instancia.multiplicadorDaño;
            tiempoRecarga *= AdministradorDeProgreso.Instancia.multiplicadorRecarga;
            tieneFuego = AdministradorDeProgreso.Instancia.balasDeFuego;
            balasPenetrantes = AdministradorDeProgreso.Instancia.balasPenetrantes;
            disparoTriple = AdministradorDeProgreso.Instancia.disparoTriple;
        }
    }

    private void SincronizarCadenciaConAnimacion()
    {
        if (gunAnim != null && gunAnim.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in gunAnim.runtimeAnimatorController.animationClips)
            {
                if (clip.name.IndexOf("Shoot", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    float speed = gunAnim.speed > 0 ? gunAnim.speed : 1f;
                    fireRate = clip.length / speed; 
                    break;
                }
            }
        }
    }

    private void BuscarCrosshairFeedback()
    {
        if (crosshairFeedback == null)
        {
            crosshairFeedback = FindAnyObjectByType<CrosshairFeedbackManager>();
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        // Polling en vez de OnSubioDeNivel: ExperienceManager vive en UIScene (aditiva) y puede cargar después que el jugador.
        // Se espera a que termine la ulti/recarga para no pisar el daño que PlayerAbilities restaura al final de la ulti.
        if (!usandoUzi && !isUltActive && !recargando && DebeUsarUzi())
        {
            ActivarUzi();
        }

        if (usandoUzi)
        {
            dispersionActual = Mathf.MoveTowards(dispersionActual, dispersionBase, recuperacionDispersion * Time.deltaTime);
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        bool r2Presionado = false;
        bool r2RecienPresionado = false;

        if (Gamepad.current != null)
        {
            r2Presionado = Gamepad.current.rightTrigger.ReadValue() > 0.5f;
            r2RecienPresionado = r2Presionado && !r2EstabaPresionado;
            r2EstabaPresionado = r2Presionado;
        }

        if (recargando) return;

        bool gatilloRecienPresionado = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || r2RecienPresionado;
        bool gatilloMantenido = (Mouse.current != null && Mouse.current.leftButton.isPressed) || r2Presionado;
        bool quiereDisparar = usandoUzi ? gatilloMantenido : gatilloRecienPresionado;

        if (quiereDisparar && Time.time >= proximoTiempoDisparo)
        {
            if (balasActuales > 0 || isUltActive)
            {
                Disparar();
                float cadenciaActual = isUltActive ? fireRate * 0.75f : fireRate;
                proximoTiempoDisparo = Time.time + cadenciaActual;
            }
            else if (balasReserva > 0)
            {
                StartCoroutine(RutinaRecarga());
            }
            else if (gatilloRecienPresionado)
            {
                ReproducirNoBullet();
                MusicManager.Instance.PlayOutOfAmmo();
                proximoTiempoDisparo = Time.time + fireRate;
            }
        }

        if (
            (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame ||
            Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            &&
            balasActuales < balasMaximas &&
            balasReserva > 0 &&
            !recargando
        )
        {
            StartCoroutine(RutinaRecarga());
        }
    }

    void LateUpdate()
    {
        if (!usandoUzi) return;

        float t = recuperacionRetroceso * Time.deltaTime;
        retrocesoCamara = Vector2.Lerp(retrocesoCamara, Vector2.zero, t);
        retrocesoManos = Mathf.Lerp(retrocesoManos, 0f, t);

        if (cam != null) cam.localRotation = rotacionBaseCam * Quaternion.Euler(-retrocesoCamara.x, retrocesoCamara.y, 0f);

        Vector3 desplazamiento = Vector3.back * (retrocesoManosDistancia * retrocesoManos);
        Quaternion giro = Quaternion.Euler(-retrocesoManosAngulo * retrocesoManos, 0f, 0f);
        if (manosDerecha != null)
        {
            manosDerecha.localPosition = posBaseManosDer + rotBaseManosDer * desplazamiento;
            manosDerecha.localRotation = rotBaseManosDer * giro;
        }
        if (manosIzquierda != null)
        {
            manosIzquierda.localPosition = posBaseManosIzq + rotBaseManosIzq * desplazamiento;
            manosIzquierda.localRotation = rotBaseManosIzq * giro;
        }
    }

    private bool DebeUsarUzi()
    {
        if (AdministradorDeProgreso.Instancia != null && AdministradorDeProgreso.Instancia.tieneUzi) return true;
        return ExperienceManager.Instancia != null && ExperienceManager.Instancia.nivelActual >= nivelDesbloqueoUzi;
    }

    public void ActivarUzi()
    {
        usandoUzi = true;

        // Se conservan las mejoras ya aplicadas al revólver (multiplicador de progreso, power-ups de daño/recarga).
        damage = uziDamage * (damage / danioBaseRevolver);
        tiempoRecarga = uziTiempoRecarga * (tiempoRecarga / recargaBaseRevolver);

        fireRate = uziFireRate;
        multiplicadorHeadshot = uziMultiplicadorHeadshot;
        distanciaDanioMaximo = uziDistanciaDanioMaximo;
        distanciaDanioMinimo = uziDistanciaDanioMinimo;
        multiplicadorDanioLargoAlcance = uziMultiplicadorDanioLargoAlcance;

        escalaMunicion = uziEscalaMunicion;
        balasMaximas = uziBalasMaximas;
        balasActuales = balasMaximas;
        balasReserva = Mathf.RoundToInt(balasReserva * escalaMunicion);

        dispersionActual = dispersionBase;
        if (cam != null) rotacionBaseCam = cam.localRotation;

        if (uziDerecha == null)
        {
            Debug.LogWarning("[Uzi] WeaponSystem sin Hands-Uzi asignado: se usa la Uzi con el modelo del revólver.");
        }
        else
        {
            // Si se asignó el FBX (asset) en vez del objeto del prefab, se instancia al lado de Hands.
            GameObject uzi = uziDerecha.scene.IsValid() ? uziDerecha : Instantiate(uziDerecha, manosDerecha != null ? manosDerecha.parent : null, false);

            // La izquierda (ulti) es una copia espejada de la derecha, creada antes de montar la derecha.
            GameObject uziIzq = null;
            if (manosIzquierda != null)
            {
                uziIzq = Instantiate(uzi, uzi.transform.parent, false);
                if (espejarUziIzquierda) Espejar(uziIzq.transform);
            }

            gunAnim = MontarUzi(uzi, manosDerecha, gunAnim, muzzleFlash);
            gunAnimIzquierda = MontarUzi(uziIzq, manosIzquierda, gunAnimIzquierda, muzzleFlashIzquierda);
        }

        if (AdministradorDeProgreso.Instancia != null) AdministradorDeProgreso.Instancia.tieneUzi = true;
    }

    // Refleja el transform respecto del plano X del padre (Camera): la Uzi derecha pasa a la izquierda.
    private static void Espejar(Transform t)
    {
        Vector3 p = t.localPosition;
        Quaternion q = t.localRotation;
        Vector3 s = t.localScale;
        t.localPosition = new Vector3(-p.x, p.y, p.z);
        t.localRotation = new Quaternion(q.x, -q.y, -q.z, q.w);
        t.localScale = new Vector3(-s.x, s.y, s.z);
    }

    // Oculta el revólver de esas manos, activa la Uzi y la cuelga de ellas (así el dash/ulti que prenden y apagan Hands/LeftHands la afectan igual).
    private Animator MontarUzi(GameObject uzi, Transform manos, Animator animRevolver, ParticleSystem muzzle)
    {
        if (uzi == null || manos == null) return animRevolver;

        foreach (Renderer r in manos.GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is ParticleSystemRenderer)) r.enabled = false;
        }
        if (animRevolver != null) animRevolver.enabled = false;

        uzi.transform.SetParent(manos, true);
        uzi.SetActive(true);
        foreach (Renderer r in uzi.GetComponentsInChildren<Renderer>(true)) r.enabled = true;

        Animator anim = uzi.GetComponentInChildren<Animator>(true);
        if (anim == null) anim = uzi.AddComponent<Animator>();
        if (uziAnimator != null) anim.runtimeAnimatorController = uziAnimator;

        if (anim.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
            {
                if (clip.name.IndexOf("Shoot", System.StringComparison.OrdinalIgnoreCase) >= 0) duracionClipDisparo = clip.length;
                if (clip.name.IndexOf("Recharge", System.StringComparison.OrdinalIgnoreCase) >= 0) duracionClipRecarga = clip.length;
            }
        }
        // El default state del controller es el disparo: se salta al final (pose de reposo) para que no dispare al aparecer.
        anim.Play(estadoDisparoUzi, 0, 1f);

        if (muzzle != null)
        {
            foreach (Transform t in uzi.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == huesoCanonUzi) { muzzle.transform.SetParent(t, true); break; }
            }
        }

        return anim;
    }

    // Con la Uzi el estado se reinicia con Play (ráfagas rápidas) y la velocidad ajusta el clip a la cadencia o a tiempoRecarga.
    private void AnimarDisparo(Animator anim)
    {
        if (!usandoUzi) { EjecutarAnimacion(anim, "Shoot"); return; }
        if (anim == null) return;
        float cadencia = isUltActive ? fireRate * 0.75f : fireRate;
        anim.speed = duracionClipDisparo > cadencia ? duracionClipDisparo / cadencia : 1f;
        anim.Play(estadoDisparoUzi, 0, 0f);
    }

    private void AnimarRecarga(Animator anim)
    {
        if (!usandoUzi) { EjecutarAnimacion(anim, "Recharge"); return; }
        if (anim == null) return;
        anim.speed = duracionClipRecarga > 0f && tiempoRecarga > 0f ? duracionClipRecarga / tiempoRecarga : 1f;
        anim.Play(estadoRecargaUzi, 0, 0f);
    }

    private Vector3 AplicarDispersion(Vector3 direccion)
    {
        Vector2 desvio = Random.insideUnitCircle * dispersionActual;
        dispersionActual = Mathf.Min(dispersionMaxima, dispersionActual + dispersionPorDisparo);
        return Quaternion.AngleAxis(desvio.x, cam.up) * Quaternion.AngleAxis(desvio.y, cam.right) * direccion;
    }

    private void AplicarRetroceso()
    {
        retrocesoCamara.x = Mathf.Min(retrocesoCamaraMaximo, retrocesoCamara.x + retrocesoCamaraVertical);
        retrocesoCamara.y += Random.Range(-retrocesoCamaraHorizontal, retrocesoCamaraHorizontal);
        retrocesoManos = 1f;
    }

    void Disparar()
    {
        if (!isUltActive)
            balasActuales--;

        MusicManager.Instance.PlayShoot();

        Transform origenTracer = null;

        if (isUltActive && gunAnimIzquierda != null)
        {
            if (dispararDerecha)
            {
                AnimarDisparo(gunAnim);
                if (muzzleFlash != null) muzzleFlash.Play();
                if (muzzleFlash != null) origenTracer = muzzleFlash.transform;
            }
            else
            {
                AnimarDisparo(gunAnimIzquierda);
                if (muzzleFlashIzquierda != null) muzzleFlashIzquierda.Play();
                if (muzzleFlashIzquierda != null) origenTracer = muzzleFlashIzquierda.transform;
            }

            dispararDerecha = !dispararDerecha;
        }
        else
        {
            AnimarDisparo(gunAnim);
            if (muzzleFlash != null) muzzleFlash.Play();
            if (muzzleFlash != null) origenTracer = muzzleFlash.transform;
        }

        if (cam == null) return;

        Vector3 direccionBase = cam.forward;
        if (usandoUzi)
        {
            direccionBase = AplicarDispersion(direccionBase);
            AplicarRetroceso();
        }

        Vector3[] direcciones;
        if (disparoTriple)
        {
            direcciones = new Vector3[3];
            direcciones[0] = direccionBase;
            direcciones[1] = Quaternion.AngleAxis(-anguloDisparoTriple, cam.up) * direccionBase;
            direcciones[2] = Quaternion.AngleAxis(anguloDisparoTriple, cam.up) * direccionBase;
        }
        else
        {
            direcciones = new Vector3[1];
            direcciones[0] = direccionBase;
        }

        foreach (Vector3 direccion in direcciones)
        {
            ProcesarDisparo(cam.position, direccion, origenTracer);
        }

        if (balasActuales <= 0 && !isUltActive && balasReserva > 0)
        {
            StartCoroutine(RutinaRecarga());
        }
    }

    void ReproducirNoBullet()
    {
        EjecutarAnimacion(gunAnim, "NoBullet");
        if (isUltActive && gunAnimIzquierda != null)
        {
            EjecutarAnimacion(gunAnimIzquierda, "NoBullet");
        }
    }

    public void EjecutarAnimacion(Animator anim, string triggerName)
    {
        if (anim != null)
        {
            anim.ResetTrigger(triggerName);
            anim.SetTrigger(triggerName);
        }
    }

    IEnumerator RutinaRecarga()
    {
        recargando = true;
        MusicManager.Instance.PlayReload();        
        AnimarRecarga(gunAnim);
        if (isUltActive && gunAnimIzquierda != null) AnimarRecarga(gunAnimIzquierda);

        yield return new WaitForSeconds(tiempoRecarga);

        int balasFaltantes = balasMaximas - balasActuales;
        int balasARecargar = Mathf.Min(balasFaltantes, balasReserva);

        balasActuales += balasARecargar;
        balasReserva -= balasARecargar;

        BuscarCrosshairFeedback();
        if (crosshairFeedback != null && balasARecargar > 0)
        {
            if (balasReserva <= limitePocasBalas * escalaMunicion) 
            {
                crosshairFeedback.ShowWarning(CrosshairFeedbackManager.WarningType.LowAmmo);
            }
            else 
            {
                crosshairFeedback.ShowWarning(CrosshairFeedbackManager.WarningType.MinusMagazine);
            }
        }

        recargando = false;
    }

    private void CrearTracer(Vector3 origen, Vector3 destino)
    {
        if (!mostrarTracer)
            return;

        GameObject tracer = new GameObject("BulletTracer");

        LineRenderer line = tracer.AddComponent<LineRenderer>();

        line.positionCount = 2;
        line.useWorldSpace = true;

        line.startWidth = grosorTracer;
        line.endWidth = grosorTracer * 0.35f;

        Color colorTracer = tieneFuego ? Color.red : Color.white;

        if (materialTracer != null)
        {
            line.material = materialTracer;
            line.startColor = colorTracer;
            line.endColor = colorTracer;
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = colorTracer;
                line.material = material;
            }
        }

        StartCoroutine(MoverTracer(line, origen, destino, duracionTracer, tracer));
    }

    private IEnumerator MoverTracer(
        LineRenderer line,
        Vector3 origen,
        Vector3 destino,
        float duracion,
        GameObject tracerObject)
    {
        float tiempo = 0f;

        line.SetPosition(0, origen);
        line.SetPosition(1, origen);

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);

            Vector3 posicionActual = Vector3.Lerp(origen, destino, t);

            line.SetPosition(0, origen);
            line.SetPosition(1, posicionActual);

            yield return null;
        }

        line.SetPosition(0, origen);
        line.SetPosition(1, destino);

        Destroy(tracerObject, 0.02f);
    }

    void ProcesarDisparo(Vector3 origen,Vector3 direccion,Transform origenTracer)
    {
        if (!balasPenetrantes)
        {
            Ray ray = new Ray(origen, direccion);
            RaycastHit hit;
            Vector3 puntoImpacto = origen + direccion * range;

            if (Physics.Raycast(ray, out hit, range))
            {
                puntoImpacto = hit.point;

                if (hit.collider.CompareTag("Player"))
                    return;

                ProcesarImpacto(hit);
            }

            if (origenTracer != null)
            {
                CrearTracer(
                    origenTracer.position,
                    puntoImpacto
                );
            }

            return;
        }

        Ray rayPenetrante = new Ray(origen, direccion);
        RaycastHit[] impactos = Physics.RaycastAll(rayPenetrante, range);

        System.Array.Sort(
            impactos,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        Vector3 puntoImpactoPenetrante = origen + direccion * range;
        System.Collections.Generic.HashSet<GameObject> objetivosGolpeados = new System.Collections.Generic.HashSet<GameObject>();

        foreach (RaycastHit hit in impactos)
        {
            if (hit.collider.CompareTag("Player"))
                continue;

            puntoImpactoPenetrante = hit.point;

            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            BossHealth bossHealth = hit.collider.GetComponentInParent<BossHealth>();
            Boss oldBoss = hit.collider.GetComponentInParent<Boss>();
            MiniCube minion = hit.collider.GetComponentInParent<MiniCube>();

            GameObject objetivo = null;

            if (enemy != null)
                objetivo = enemy.gameObject;
            else if (bossHealth != null)
                objetivo = bossHealth.gameObject;
            else if (oldBoss != null)
                objetivo = oldBoss.gameObject;
            else if (minion != null)
                objetivo = minion.gameObject;

            if (objetivo != null)
            {
                if (objetivosGolpeados.Contains(objetivo))
                    continue;

                objetivosGolpeados.Add(objetivo);
                ProcesarImpacto(hit);
                continue;
            }

            ProcesarImpacto(hit);
            break;
        }

        if (origenTracer != null)
        {
            CrearTracer(
                origenTracer.position,
                puntoImpactoPenetrante
            );
        }
    }

    void ProcesarImpacto(RaycastHit hit)
    {
        Dynamite dinamita = hit.collider.GetComponentInParent<Dynamite>();
        if (dinamita != null)
        {
            BuscarCrosshairFeedback();
            if (crosshairFeedback != null) crosshairFeedback.OnTargetHit(true);

            dinamita.RecibirDisparo();
            return;
        }
        bool esHeadshot = hit.collider.CompareTag("Head");
        float distancia = Vector3.Distance(cam.position, hit.point);

float danoBaseDistancia = CalcularDanioPorDistancia(distancia);

float danoFinal = esHeadshot
    ? danoBaseDistancia * multiplicadorHeadshot
    : danoBaseDistancia;

        if (esHeadshot)
        {
            if (!usandoUzi) ReembolsarBala();
        }

        if (hit.collider.CompareTag("Enemigo") ||
            hit.collider.CompareTag("MinionBoss") ||
            esHeadshot)
        {
            BuscarCrosshairFeedback();

            if (crosshairFeedback != null)
            {
                crosshairFeedback.OnTargetHit(esHeadshot);
            }

            if (prefabImpactoRobot != null)
            {
                GameObject chispas = Instantiate(
                    prefabImpactoRobot,
                    hit.point,
                    Quaternion.LookRotation(hit.normal)
                );

                Destroy(chispas, 1.5f);
            }
        }
        else
        {
            if (prefabImpactoEntorno != null)
            {
                GameObject polvo = Instantiate(
                    prefabImpactoEntorno,
                    hit.point,
                    Quaternion.LookRotation(hit.normal)
                );

                Destroy(polvo, 1.5f);
            }
        }
        if (balasExplosivas)
        {
            GenerarMicroExplosion(hit.point, danoFinal * porcentajeDanoExplosion);
        }

        EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(danoFinal, esHeadshot);

            if (tieneFuego)
            {
                enemy.AplicarQuemadura(20f, 2f);
            }
        }
        BossHealth bossHealth = hit.collider.GetComponentInParent<BossHealth>();
        if (bossHealth != null)
        {
            bossHealth.TakeDamage(danoFinal);

            if (tieneFuego && Random.value <= 0.25f)
            {
                bossHealth.Quemar();
            }
        }
        else
        {

            Boss oldBoss = hit.collider.GetComponentInParent<Boss>();
            if (oldBoss != null)
            {
                oldBoss.TakeDamage(danoFinal);
                if (tieneFuego && Random.value <= 0.25f) oldBoss.Quemar();
            }
        }

        MiniCube minion = hit.collider.GetComponentInParent<MiniCube>();
        if (minion != null)
        {
            minion.TakeDamage(danoFinal);

            if (tieneFuego && Random.value <= 0.25f)
            {
                minion.Quemar();
            }
        }
    }

    void GenerarMicroExplosion(Vector3 punto, float danoArea)
    {
        if (prefabMicroExplosion != null)
        {
            GameObject fx = Instantiate(prefabMicroExplosion, punto, Quaternion.identity);
            Destroy(fx, 1.5f);
        }

        Collider[] afectados = Physics.OverlapSphere(punto, radioExplosionBala);
        System.Collections.Generic.HashSet<GameObject> golpeados = new System.Collections.Generic.HashSet<GameObject>();

        foreach (Collider col in afectados)
        {
            if (col.CompareTag("Enemigo") || (col.transform.root != null && col.transform.root.CompareTag("Enemigo")))
            {
                EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
                if (enemy != null && !golpeados.Contains(enemy.gameObject))
                {
                    golpeados.Add(enemy.gameObject);
                    enemy.TakeDamage(danoArea, false);

                    if (tieneFuego)
                    {
                        enemy.AplicarQuemadura(20f, 2f);
                    }
                }

                Boss boss = col.GetComponentInParent<Boss>();
                if (boss != null && !golpeados.Contains(boss.gameObject))
                {
                    golpeados.Add(boss.gameObject);
                    boss.TakeDamage(danoArea);
                }
            }
        }
    }

    public void ReembolsarBala()
    {
        int cantidad = Mathf.RoundToInt(escalaMunicion);
        int alCargador = Mathf.Min(cantidad, balasMaximas - balasActuales);
        balasActuales += alCargador;
        balasReserva += cantidad - alCargador;

        BuscarCrosshairFeedback();
        if (crosshairFeedback != null)
        {
            crosshairFeedback.ShowReward(CrosshairFeedbackManager.RewardType.Bullets);
        }
    }

    public void AddAmmo(int amount)
    {
        balasReserva += Mathf.RoundToInt(amount * escalaMunicion);

        BuscarCrosshairFeedback();
        if (crosshairFeedback != null) crosshairFeedback.ShowReward(CrosshairFeedbackManager.RewardType.Bullets);

        if (balasActuales <= 0 && !recargando && gameObject.activeInHierarchy)
        {
            StartCoroutine(RutinaRecarga());
        }
    }
    private float CalcularDanioPorDistancia(float distancia)
{
    if (distancia <= distanciaDanioMaximo)
        return damage;

    if (distancia >= distanciaDanioMinimo)
        return damage * multiplicadorDanioLargoAlcance;

    float t = Mathf.InverseLerp(
        distanciaDanioMaximo,
        distanciaDanioMinimo,
        distancia
    );

    return Mathf.Lerp(
        damage,
        damage * multiplicadorDanioLargoAlcance,
        t
    );
}
}