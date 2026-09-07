using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("Ajustes de la Bala")]
    public float velocidad = 20f;
    public float dano = 10f;
    public float tiempoDeVida = 5f; 

    [HideInInspector] public Vector3 origenDisparo;

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats pm = other.GetComponentInParent<PlayerStats>();
            if (pm != null)
            {
                pm.TakeDamage(dano);
            }

            if (DirectionalIndicatorHUD.Instance != null && origenDisparo != Vector3.zero)
            {
                DirectionalIndicatorHUD.Instance.RegistrarImpacto(origenDisparo);
            }

            Destroy(gameObject);
            return; 
        }

        if (other.isTrigger) return; 

        if (other.CompareTag("Enemigo") || other.GetComponent<Kamikaze>() != null || other.GetComponent<Artillero>() != null)
        {
            return;
        }

        Destroy(gameObject);
    }
}