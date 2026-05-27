using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class PlayerAbilities : MonoBehaviour
{
    [HideInInspector] public float dashCooldownTimer;
    [HideInInspector] public float dynamiteCooldownTimer;
    [HideInInspector] public float ultCooldownTimer;

    [Header("Referencias")]
    public Transform cam; 
    private PlayerMovement moveScript;
    private WeaponSystem weaponScript;

    [Header("Cooldowns")]
    public float dynamiteCooldown = 7f;
    public bool canUseE = true;
    public float ultCooldown = 120f;
    public bool canUseQ = true;
    public float dashCooldown = 7f;
    public bool canDash = true;

    [Header("Configuración Habilidades")]
    public GameObject dynamitePrefab;
    public Transform muzzle;
    public float throwForce = 15f;
    public float ultDuration = 10f;
    public bool isUltActive = false;
    public float dashDuration = 2.5f;

    [Header("Configuración Akimbo (Q)")]
    public GameObject revolverIzquierdo; 
    public GameObject auraDerecha;      
    public GameObject auraIzquierda;     

    void Start()
    {
        moveScript = GetComponent<PlayerMovement>();
        weaponScript = GetComponent<WeaponSystem>();

        if (cam == null) cam = Camera.main.transform;

        if (AdministradorDeProgreso.Instancia != null)
        {
        dashCooldown *= AdministradorDeProgreso.Instancia.multiplicadorDashCooldown;
        dynamiteCooldown *= AdministradorDeProgreso.Instancia.multiplicadorDinamitaCooldown;
        }
    }

    public void OnAbilityE(InputValue value)
    {
        if (value.isPressed && canUseE) StartCoroutine(UseDynamite());
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
        GameObject dyn = Instantiate(dynamitePrefab, muzzle.position, muzzle.rotation);
        Rigidbody rb = dyn.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(cam.forward * throwForce, ForceMode.Impulse);
        }
    }

    public void OnUltimate(InputValue value) { if (value.isPressed && canUseQ && !isUltActive) StartCoroutine(HandleUltimate()); }
    public void OnDash(InputValue value) { if (value.isPressed && canDash) StartCoroutine(GhostDash()); }

    IEnumerator HandleUltimate()
    {
        canUseQ = false;
        StartCoroutine(ActivateUlt());
        ultCooldownTimer = ultCooldown;
        while (ultCooldownTimer > 0) { ultCooldownTimer -= Time.deltaTime; yield return null; }
        canUseQ = true;
    }

    IEnumerator ActivateUlt()
    {
        isUltActive = true;
        
        if (revolverIzquierdo != null) revolverIzquierdo.SetActive(true);
        if (auraDerecha != null) auraDerecha.SetActive(true);
        if (auraIzquierda != null) auraIzquierda.SetActive(true);

        float originalDamage = weaponScript.damage;
        weaponScript.damage *= 1.5f;
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
        moveScript.isGhostMode = true;
        yield return new WaitForSeconds(dashDuration);
        moveScript.isGhostMode = false;
        dashCooldownTimer = dashCooldown;
        while (dashCooldownTimer > 0) { dashCooldownTimer -= Time.deltaTime; yield return null; }
        canDash = true;
    }
}