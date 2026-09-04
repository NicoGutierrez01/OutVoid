using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CofreRecompensa : MonoBehaviour
{
    public Animator animCofre;
    [Tooltip("Tiempo en segundos que tarda la animación en abrirse")]
    public float tiempoAnimacion = 1.2f; 
    public float radioInteraccion = 3.5f;

    [Header("UI Flotante")]
    public GameObject canvasFlotante; 
    private Transform camaraJugador; 

    private bool jugadorCerca = false;
    private bool yaAbierto = false;

    void Start()
    {
        if (canvasFlotante != null) canvasFlotante.SetActive(false);
        if (Camera.main != null) camaraJugador = Camera.main.transform;
    }

    void Update()
    {
        if (yaAbierto) return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, radioInteraccion);
        bool detectado = false;

        foreach (var hit in colliders)
        {
            if (hit.CompareTag("Player"))
            {
                detectado = true;
                break;
            }
        }

        jugadorCerca = detectado;

        if (canvasFlotante != null)
        {
            if (canvasFlotante.activeSelf != jugadorCerca)
            {
                canvasFlotante.SetActive(jugadorCerca);
            }

            if (canvasFlotante.activeSelf && camaraJugador != null)
            {
                canvasFlotante.transform.LookAt(canvasFlotante.transform.position + camaraJugador.forward);
            }
        }

        if (jugadorCerca && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            StartCoroutine(RutinaAbrirCofre());
        }
    }

    private IEnumerator RutinaAbrirCofre()
    {
        yaAbierto = true;

        if (canvasFlotante != null) canvasFlotante.SetActive(false);
        
        if (animCofre != null) animCofre.SetTrigger("Abrir");

        yield return new WaitForSeconds(tiempoAnimacion);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (PowerUpUIManager.Instancia != null)
        {
           PowerUpUIManager.Instancia.MostrarOpciones(this.gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioInteraccion);
    }
}