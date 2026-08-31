using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CofreRecompensa : MonoBehaviour
{
    public Animator animCofre;
    [Tooltip("Tiempo en segundos que tarda la animación en abrirse")]
    public float tiempoAnimacion = 1.2f; 
    private bool jugadorCerca = false;
    private bool yaAbierto = false;

    void Update()
    {
        if (jugadorCerca && !yaAbierto && Keyboard.current.fKey.wasPressedThisFrame)
        {
            StartCoroutine(RutinaAbrirCofre());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) jugadorCerca = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) jugadorCerca = false;
    }

    private IEnumerator RutinaAbrirCofre()
    {
        yaAbierto = true;
        
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
}