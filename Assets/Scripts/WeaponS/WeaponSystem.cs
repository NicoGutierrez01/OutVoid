using UnityEngine;
using UnityEngine.InputSystem;
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

    private CrosshairFeedbackManager crosshairFeedback;

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
            (Keyboard.current.rKey.wasPressedThisFrame ||
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
        if (!isUltActive) balasActuales--;
        MusicManager.Instance.PlayShoot();

        if (isUltActive && gunAnimIzquierda != null)
        {
            if (dispararDerecha) 
            {
                EjecutarAnimacion(gunAnim, "Shoot");
                if (muzzleFlash != null) muzzleFlash.Play();
            }
            else 
            {
                EjecutarAnimacion(gunAnimIzquierda, "Shoot");
                if (muzzleFlashIzquierda != null) muzzleFlashIzquierda.Play();
            }
            
            dispararDerecha = !dispararDerecha;
        }
        else
        {
            EjecutarAnimacion(gunAnim, "Shoot");
            if (muzzleFlash != null) muzzleFlash.Play();
        }

        if (cam == null) return;

        Ray ray = new Ray(cam.position, cam.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            if (hit.collider.CompareTag("Player")) return;

            // Primero calculamos si es headshot antes de evaluar el impacto visual
            bool esHeadshot = hit.collider.CompareTag("Head");
            float danoFinal = esHeadshot ? damage * multiplicadorHeadshot : damage;

            if (hit.collider.CompareTag("Enemigo") || hit.collider.CompareTag("MinionBoss") || esHeadshot) 
            {
                BuscarCrosshairFeedback();
                if (crosshairFeedback != null) 
                {
                    // Le enviamos el estado del headshot para que pinte la mira de rojo o amarillo
                    crosshairFeedback.OnTargetHit(esHeadshot);
                }

                if (prefabImpactoRobot != null)
                {
                    GameObject chispas = Instantiate(prefabImpactoRobot, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(chispas, 1.5f); 
                }
            }
            else 
            {
                if (prefabImpactoEntorno != null)
                {
                    GameObject polvo = Instantiate(prefabImpactoEntorno, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(polvo, 1.5f);
                }
            }

            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(danoFinal, esHeadshot);

            Boss boss = hit.collider.GetComponentInParent<Boss>();
            if (boss != null)
            {
                boss.TakeDamage(danoFinal); 
                if (tieneFuego && Random.value <= 0.25f) boss.Quemar();
            }

            MiniCube minion = hit.collider.GetComponentInParent<MiniCube>();
            if (minion != null)
            {
                minion.TakeDamage(danoFinal); 
                if (tieneFuego && Random.value <= 0.25f) minion.Quemar();
            }
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

    public void AddAmmo(int amount)
    {
        balasReserva += amount;

        BuscarCrosshairFeedback();
        if (crosshairFeedback != null) crosshairFeedback.ShowReward(CrosshairFeedbackManager.RewardType.Bullets);
    }
}