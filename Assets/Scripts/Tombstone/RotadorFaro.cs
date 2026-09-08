using UnityEngine;

public class RotadorFaro : MonoBehaviour
{
    [SerializeField] private float velocidadRotacion = 45f; 

    void Update()
    {
        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime, Space.Self);
    }
}