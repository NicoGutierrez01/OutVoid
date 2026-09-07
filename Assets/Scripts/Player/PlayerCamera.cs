using System;
using UnityEngine;

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{
    [Header("Multiplicadores de Plataforma")]
    [Tooltip("Ajuste interno para que el slider (ej: 1 a 10) se sienta bien en Windows")]
    [SerializeField] private float multiplicadorDesktop = 0.05f;

    [Tooltip("Ajuste interno para el navegador (suele necesitar menos sensibilidad)")]
    [SerializeField] private float multiplicadorWebGL = 0.02f;

    private float currentSensitivity;
private float pitch;
private float yaw;
    private Camera cam;
    private float defaultFOV;
    private float targetFOV;

    private Vector3 _eulerAngles;

    public void Initialize(Transform target)
    {
        cam = GetComponent<Camera>();

        if (cam == null)
            cam = Camera.main;

        defaultFOV = cam.fieldOfView;
        targetFOV = defaultFOV;

     transform.position = target.position;

yaw = target.eulerAngles.y;
pitch = 0f;

transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        ActualizarSensibilidad();
    }

    public void ActualizarSensibilidad()
    {
        float sensibilidadUI = PlayerPrefs.GetFloat("SensibilidadMouse", 1f);

#if UNITY_WEBGL
        currentSensitivity = sensibilidadUI * multiplicadorWebGL;
#else
        currentSensitivity = sensibilidadUI * multiplicadorDesktop;
#endif

        Debug.Log($"[CÁMARA] Sensibilidad aplicada: {currentSensitivity}");
    }

public void UpdateRotation(CameraInput input)
{
    yaw += input.Look.x * currentSensitivity;

    pitch -= input.Look.y * currentSensitivity;

    pitch = Mathf.Clamp(pitch, -85f, 85f);

    transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
}

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;

        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView,
                targetFOV,
                8f * Time.deltaTime
            );
        }
    }

    public void SetDashFOV(float extra)
    {
        targetFOV = defaultFOV + extra;
    }

    public void ResetFOV()
    {
        targetFOV = defaultFOV;
    }
}