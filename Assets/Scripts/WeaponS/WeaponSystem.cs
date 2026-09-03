using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;

public class WeaponSystem : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    public float damage = 20f;
    public float multiplicadorHeadshot = 2f; 
    private bool r2EstabaPresionado = false;
    public float range = 100f; 
    public Transform cam; 

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
    public float fireRate = 0.25f; 
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

    void Start()
    {
        balasActuales = balasMaximas;
        if (gunAnim == null) gunAnim = GetComponentInChildren<Animator>();

        if (AdministradorDeProgreso.Instancia != null)
        {
            damage *= AdministradorDeProgreso.Instancia.multiplicadorDaño;
            tiempoRecarga *= AdministradorDeProgreso.Instancia.multiplicadorRecarga;
            tieneFuego = AdministradorDeProgreso.Instancia.balasDeFuego;
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
        // 1. Bloqueo total si el juego está en pausa
        if (Time.timeScale <= 0f) return;

        // 2. Bloqueo si el clic se hace sobre elementos de UI (botones de menú, ajustes, etc.)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        bool r2RecienPresionado = false;

        if (Gamepad.current != null)
        {
            bool r2Presionado = Gamepad.current.rightTrigger.ReadValue() > 0.5f;
            r2RecienPresionado = r2Presionado && !r2EstabaPresionado;
            r2EstabaPresionado = r2Presionado;
        }

        if (recargando) return;

        if (
            (
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || r2RecienPresionado
            )
            && Time.time >= proximoTiempoDisparo
        )
        {
            if (balasActuales > 0 || isUltActive)
            {
                Disparar();
                float cadenciaActual = isUltActive ? fireRate * 0.75f : fireRate;
                proximoTiempoDisparo = Time.time + cadenciaActual;
            }
            else if (!recargando)
            {
                if (balasReserva > 0)
                {
                    StartCoroutine(RutinaRecarga());
                }
                else
                {
                    ReproducirNoBullet();
                    MusicManager.Instance.PlayOutOfAmmo();
                    proximoTiempoDisparo = Time.time + fireRate; 
                }
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

    void Disparar()
    {
        if (!isUltActive)
            balasActuales--;

        MusicManager.Instance.PlayShoot();

        // ==============================
        // ANIMACIÓN Y MUZZLE FLASH
        // ==============================

        if (isUltActive && gunAnimIzquierda != null)
        {
            if (dispararDerecha)
            {
                EjecutarAnimacion(gunAnim, "Shoot");

                if (muzzleFlash != null)
                    muzzleFlash.Play();
            }
            else
            {
                EjecutarAnimacion(gunAnimIzquierda, "Shoot");

                if (muzzleFlashIzquierda != null)
                    muzzleFlashIzquierda.Play();
            }

            dispararDerecha = !dispararDerecha;
        }
        else
        {
            EjecutarAnimacion(gunAnim, "Shoot");

            if (muzzleFlash != null)
                muzzleFlash.Play();
        }

        // ==============================
        // COMPROBAR CÁMARA
        // ==============================

        if (cam == null)
            return;

        // ==============================
        // ORIGEN DEL TRACER
        // ==============================

        Transform origenTracer = null;

        if (isUltActive && gunAnimIzquierda != null)
        {
            if (dispararDerecha && muzzleFlash != null)
            {
                origenTracer = muzzleFlash.transform;
            }
            else if (!dispararDerecha && muzzleFlashIzquierda != null)
            {
                origenTracer = muzzleFlashIzquierda.transform;
            }
        }
        else if (muzzleFlash != null)
        {
            origenTracer = muzzleFlash.transform;
        }

        // ==============================
        // DIRECCIONES DE DISPARO
        // ==============================

        Vector3[] direcciones;

        if (disparoTriple)
        {
            direcciones = new Vector3[3];

            direcciones[0] = cam.forward;
            direcciones[1] = Quaternion.AngleAxis(-anguloDisparoTriple, cam.up) * cam.forward;
            direcciones[2] = Quaternion.AngleAxis(anguloDisparoTriple, cam.up) * cam.forward;
        }
        else
        {
            direcciones = new Vector3[1];
            direcciones[0] = cam.forward;
        }

        // ==============================
        // EJECUTAR CADA DISPARO
        // ==============================

        foreach (Vector3 direccion in direcciones)
        {
            ProcesarDisparo(
                cam.position,
                direccion,
                origenTracer
            );
        }

        // ==============================
        // RECARGA AUTOMÁTICA
        // ==============================

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
        EjecutarAnimacion(gunAnim, "Recharge");
        if (isUltActive && gunAnimIzquierda != null) EjecutarAnimacion(gunAnimIzquierda, "Recharge");

        yield return new WaitForSeconds(tiempoRecarga);

        int balasFaltantes = balasMaximas - balasActuales;
        int balasARecargar = Mathf.Min(balasFaltantes, balasReserva);

        balasActuales += balasARecargar;
        balasReserva -= balasARecargar;

        BuscarCrosshairFeedback();
        if (crosshairFeedback != null && balasARecargar > 0)
        {
            if (balasReserva == 0) 
            {
                crosshairFeedback.ShowWarning(CrosshairFeedbackManager.WarningType.LastMagazine);
            }
            else if (balasReserva <= limitePocasBalas) 
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

    void ProcesarDisparo(
        Vector3 origen,
        Vector3 direccion,
        Transform origenTracer)
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
            Boss boss = hit.collider.GetComponentInParent<Boss>();
            MiniCube minion = hit.collider.GetComponentInParent<MiniCube>();

            GameObject objetivo = null;

            if (enemy != null)
                objetivo = enemy.gameObject;
            else if (boss != null)
                objetivo = boss.gameObject;
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
        bool esHeadshot = hit.collider.CompareTag("Head");
        float danoFinal = esHeadshot ? damage * multiplicadorHeadshot : damage;

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

        EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(danoFinal, esHeadshot);
        }

        Boss boss = hit.collider.GetComponentInParent<Boss>();
        if (boss != null)
        {
            boss.TakeDamage(danoFinal);

            if (tieneFuego && Random.value <= 0.25f)
            {
                boss.Quemar();
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

    public void AddAmmo(int amount)
    {
        balasReserva += amount;

        BuscarCrosshairFeedback();
        if (crosshairFeedback != null) crosshairFeedback.ShowReward(CrosshairFeedbackManager.RewardType.Bullets);
    }
}