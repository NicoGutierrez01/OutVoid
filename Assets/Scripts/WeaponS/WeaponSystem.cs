using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WeaponSystem : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    public float damage = 20f;
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
            else if (!recargando && balasReserva > 0)
            {
                StartCoroutine(RutinaRecarga());
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

        if (isUltActive && gunAnimIzquierda != null)
        {
            if (dispararDerecha) 
            {
                EjecutarAnimacion(gunAnim);
                if (muzzleFlash != null) muzzleFlash.Play();
            }
            else 
            {
                EjecutarAnimacion(gunAnimIzquierda);
                if (muzzleFlashIzquierda != null) muzzleFlashIzquierda.Play();
            }
            
            dispararDerecha = !dispararDerecha;
        }
        else
        {
            EjecutarAnimacion(gunAnim);
            if (muzzleFlash != null) muzzleFlash.Play();
        }

        if (cam == null) return;

        Ray ray = new Ray(cam.position, cam.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            if (hit.transform.CompareTag("Player")) return;

            if (hit.transform.CompareTag("Enemigo") || hit.transform.CompareTag("MinionBoss")) 
            {
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

            EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            Boss boss = hit.transform.GetComponent<Boss>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
                if (tieneFuego && Random.value <= 0.25f) boss.Quemar();
            }

            MiniCube minion = hit.transform.GetComponent<MiniCube>();
            if (minion != null)
            {
                minion.TakeDamage(damage);
                if (tieneFuego && Random.value <= 0.25f) minion.Quemar();
            }
        }

        if (balasActuales <= 0 && !isUltActive && balasReserva > 0)
        {
            StartCoroutine(RutinaRecarga());
        }
    }

    void EjecutarAnimacion(Animator anim)
    {
        if (anim != null)
        {
            anim.ResetTrigger("ShootTrigger");
            anim.SetTrigger("ShootTrigger");
        }
    }

    IEnumerator RutinaRecarga()
    {
        recargando = true;
        if (gunAnim != null) gunAnim.SetTrigger("ReloadTrigger");

        yield return new WaitForSeconds(tiempoRecarga);

        int balasFaltantes = balasMaximas - balasActuales;

        int balasARecargar = Mathf.Min(balasFaltantes, balasReserva);

        balasActuales += balasARecargar;
        balasReserva -= balasARecargar;

        recargando = false;
    }

    public void AddAmmo(int amount)
    {
        balasReserva += amount;
    }
}