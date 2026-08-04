using UnityEngine;

public class ZonaDefensa : MonoBehaviour
{
    public static bool jugadorEnZona = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnZona = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnZona = false;
        }
    }

    private void OnDestroy()
    {
        jugadorEnZona = false;
    }
}
