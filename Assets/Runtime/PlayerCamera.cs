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
        transform.eulerAngles = _eulerAngles = target.eulerAngles;

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
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x) * currentSensitivity;
        transform.eulerAngles = _eulerAngles;
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