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

    [Header("Melee")]
    public float meleeDamage = 35f;
    public float meleeRange = 2f;
    public float meleeCooldown = 0.5f;
    public bool canMelee = true;

    [Header("Configuración Habilidades")]
    public GameObject dynamitePrefab;
    public Transform muzzle;
    public float throwForce = 15f;
    public float ultDuration = 10f;
    public bool isUltActive = false;

    [Header("Ajustes de Forma Espectral (Reaper)")]
    public float dashDuration = 4f;
    [Tooltip("Por cuánto se multiplica tu velocidad actual. 2 = el doble de rápido.")]
    public float ghostSpeedMultiplier = 1.3f;

    [Header("Configuración Akimbo (Q)")]
    public GameObject revolverIzquierdo;
    public GameObject auraDerecha;
    public GameObject auraIzquierda;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = FindAnyObjectByType<PlayerCamera>();

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
    GameObject dyn = Instantiate(dynamitePrefab, muzzle.position, Quaternion.identity);

    Rigidbody rb = dyn.GetComponent<Rigidbody>();

    if (rb != null)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 dir = (cam.forward + Vector3.up * 0.2f).normalized;

Vector3 inheritedVelocity = playerCharacter != null ? playerCharacter.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero : Vector3.zero;

rb.linearVelocity = inheritedVelocity;

rb.AddForce(cam.forward * throwForce, ForceMode.Impulse);
        // 🔥 CLAVE: limitar rotación inicial
        rb.maxAngularVelocity = 2f;
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

        if (revolverIzquierdo != null) revolverIzquierdo.SetActive(true);
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

        if (playerCamera != null)
            playerCamera.SetDashFOV(10f);

        moveScript.isGhostMode = true;

        if (weaponScript != null)
            weaponScript.enabled = false;

        if (playerCharacter != null)
        {
            float velocidadNormal = playerCharacter.walkSpeed;
            playerCharacter.walkSpeed = velocidadNormal * ghostSpeedMultiplier;

            yield return new WaitForSeconds(dashDuration);

            if (playerCamera != null)
                playerCamera.ResetFOV();

            playerCharacter.walkSpeed = velocidadNormal;
        }
        else
        {
            yield return new WaitForSeconds(dashDuration);

            if (playerCamera != null)
                playerCamera.ResetFOV();
        }

        moveScript.isGhostMode = false;

        if (weaponScript != null)
            weaponScript.enabled = true;

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

        Ray ray = new Ray(cam.position, cam.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, meleeRange))
        {
            EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();
            if (enemy != null)
                enemy.TakeDamage(meleeDamage);

            Boss boss = hit.transform.GetComponent<Boss>();
            if (boss != null)
                boss.TakeDamage(meleeDamage);

            MiniCube minion = hit.transform.GetComponent<MiniCube>();
            if (minion != null)
                minion.TakeDamage(meleeDamage);
        }

        yield return new WaitForSeconds(meleeCooldown);

        canMelee = true;
    }
}