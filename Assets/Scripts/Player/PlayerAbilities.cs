using UnityEngine;
using System.Collections;

public class PlayerAbilities : MonoBehaviour
{
    [HideInInspector] public float dashCooldownTimer;
    [HideInInspector] public float dynamiteCooldownTimer;
    [HideInInspector] public float ultCooldownTimer;

    [Header("Referencias")]
    public Transform cam;
    public PlayerCharacter playerCharacter;
    public PlayerCamera playerCamera;

    private PlayerStats moveScript;
    private WeaponSystem weaponScript;

    [Header("Cooldowns")]
    public float dynamiteCooldown = 7f;
    public bool canUseE = true;
    public float ultCooldown = 120f;
    public bool canUseQ = true;
    public float dashCooldown = 7f;
    public bool canDash = true;

    [Header("Mejora Dinamita Colosal")]
    public bool dinamitaPotenciada = false;
    public float multiplicadorAreaDinamita = 1.6f;
    public float multiplicadorDanoDinamita = 1.5f;

    [Header("Melee")]
    public float meleeDamage = 35f;
    public float meleeRange = 2f;
    public float meleeCooldown = 0.5f;
    public bool canMelee = true;
    private bool usarHit2 = false;

    [Header("Configuración Habilidades")]
    public GameObject dynamitePrefab;
    public Transform muzzle;
    public float throwForceFrontal = 35f; 
    public float throwForceArriba = 8f;    
    public float ultDuration = 10f;
    public bool isUltActive = false;

    [Header("Ajustes de Forma Espectral (Reaper)")]
    public float dashDuration = 4f;
    [Tooltip("Por cuánto se multiplica tu velocidad actual. 2 = el doble de rápido.")]
    public float ghostSpeedMultiplier = 1.3f;

    [Header("Configuración Akimbo (Q)")]
    [Tooltip("Arrastrá acá el GameObject completo de la mano izquierda de tu compañero")]
    public GameObject revolverIzquierdo; 
    public GameObject auraDerecha;
    public GameObject auraIzquierda;

    void Start()
    {
        if (playerCamera == null) playerCamera = FindAnyObjectByType<PlayerCamera>();

        moveScript = GetComponentInParent<PlayerStats>();
        if (moveScript == null) moveScript = FindAnyObjectByType<PlayerStats>();

        weaponScript = GetComponentInParent<WeaponSystem>();
        if (weaponScript == null) weaponScript = FindAnyObjectByType<WeaponSystem>();

        if (playerCharacter == null) playerCharacter = GetComponentInParent<PlayerCharacter>();
        if (playerCharacter == null) playerCharacter = FindAnyObjectByType<PlayerCharacter>();

        if (cam == null) cam = Camera.main.transform;

        if (AdministradorDeProgreso.Instancia != null)
        {
            dashCooldown *= AdministradorDeProgreso.Instancia.multiplicadorDashCooldown;
            dynamiteCooldown *= AdministradorDeProgreso.Instancia.multiplicadorDinamitaCooldown;
        }
    }

    public void PotenciarDinamita(float factor)
    {
        dinamitaPotenciada = true;
        dynamiteCooldown = Mathf.Max(2f, dynamiteCooldown * 0.5f);
    }

    public void UpdateInput(CharacterInput input)
    {
        if (input.AbilityE && canUseE) StartCoroutine(UseDynamite());
        if (input.Ultimate && canUseQ && !isUltActive) StartCoroutine(HandleUltimate());
        if (input.Dash && canDash) StartCoroutine(GhostDash());
        if (input.Melee && canMelee) StartCoroutine(UseMelee());
    }

    IEnumerator UseDynamite()
    {
        canUseE = false;
        ThrowDynamite();
        dynamiteCooldownTimer = dynamiteCooldown;

        while (dynamiteCooldownTimer > 0)
        {
            dynamiteCooldownTimer -= Time.deltaTime;
            yield return null;
        }
        canUseE = true;
    }

    void ThrowDynamite()
    {
        if (dynamitePrefab == null) return;

        if (MusicManager.Instance != null) MusicManager.Instance.PlayThrowingDynamite();

        Vector3 posicionSpawn = muzzle != null ? muzzle.position : (cam.position + cam.forward * 0.8f);

        GameObject dyn = Instantiate(dynamitePrefab, posicionSpawn, Quaternion.identity);
        Dynamite dynamiteScript = dyn.GetComponent<Dynamite>();

        if (dynamiteScript != null && dinamitaPotenciada)
        {
            dynamiteScript.radioExplosion *= multiplicadorAreaDinamita;
            dynamiteScript.dañoExplosion *= multiplicadorDanoDinamita;
        }

        Collider[] playerColliders = transform.root.GetComponentsInChildren<Collider>();

        if (dynamiteScript != null)
        {
            dynamiteScript.InicializarLanzamiento(cam.forward, throwForceFrontal, throwForceArriba, playerColliders);
        }
    }

    IEnumerator HandleUltimate()
    {
        canUseQ = false;
        StartCoroutine(ActivateUlt());
        ultCooldownTimer = ultCooldown;

        while (ultCooldownTimer > 0)
        {
            ultCooldownTimer -= Time.deltaTime;
            yield return null;
        }
        canUseQ = true;
    }

    IEnumerator ActivateUlt()
    {
        isUltActive = true;
        MusicManager.Instance.PlayUltimate();
        if (revolverIzquierdo != null) 
        {
            revolverIzquierdo.SetActive(true);
            if(weaponScript != null && weaponScript.gunAnimIzquierda != null)
            {
                weaponScript.EjecutarAnimacion(weaponScript.gunAnimIzquierda, "Start");
            }
        }
        
        if (auraDerecha != null) auraDerecha.SetActive(true);
        if (auraIzquierda != null) auraIzquierda.SetActive(true);

        float originalDamage = weaponScript.damage;
        weaponScript.damage *= 1.2f;
        weaponScript.isUltActive = true;

        yield return new WaitForSeconds(ultDuration);

        weaponScript.damage = originalDamage;
        weaponScript.isUltActive = false;

        if (revolverIzquierdo != null) revolverIzquierdo.SetActive(false);
        if (auraDerecha != null) auraDerecha.SetActive(false);
        if (auraIzquierda != null) auraIzquierda.SetActive(false);

        isUltActive = false;
    }

    IEnumerator GhostDash()
    {
        canDash = false;
        MusicManager.Instance.PlayDash();
        if (playerCamera != null) playerCamera.SetDashFOV(10f);

        PlayerHUD hud = Object.FindFirstObjectByType<PlayerHUD>();
        if (hud != null)
        {
            hud.TriggerGhostOverlay(dashDuration);
        }

        moveScript.isGhostMode = true;
        if (weaponScript != null) weaponScript.enabled = false;

        if (playerCharacter != null)
        {
            float velocidadNormal = playerCharacter.walkSpeed;
            playerCharacter.walkSpeed = velocidadNormal * ghostSpeedMultiplier;

            yield return new WaitForSeconds(dashDuration);

            if (playerCamera != null) playerCamera.ResetFOV();
            playerCharacter.walkSpeed = velocidadNormal;
        }
        else
        {
            yield return new WaitForSeconds(dashDuration);
            if (playerCamera != null) playerCamera.ResetFOV();
        }

        moveScript.isGhostMode = false;
        if (weaponScript != null) weaponScript.enabled = true;

        dashCooldownTimer = dashCooldown;
        while (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
            yield return null;
        }
        canDash = true;
    }

    IEnumerator UseMelee()
    {
        canMelee = false;
        MusicManager.Instance.PlayMelee();

        if (weaponScript != null)
        {
            string triggerElegido = usarHit2 ? "Hit2" : "Hit";
            
            weaponScript.EjecutarAnimacion(weaponScript.gunAnim, triggerElegido);
            if (isUltActive && weaponScript.gunAnimIzquierda != null)
            {
                weaponScript.EjecutarAnimacion(weaponScript.gunAnimIzquierda, triggerElegido);
            }

            usarHit2 = !usarHit2; 
        }

        Ray ray = new Ray(cam.position, cam.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, meleeRange))
        {
            EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(meleeDamage);

            Boss boss = hit.transform.GetComponent<Boss>();
            if (boss != null) boss.TakeDamage(meleeDamage);

            MiniCube minion = hit.transform.GetComponent<MiniCube>();
            if (minion != null) minion.TakeDamage(meleeDamage);
        }

        yield return new WaitForSeconds(meleeCooldown);
        canMelee = true;
    }
}