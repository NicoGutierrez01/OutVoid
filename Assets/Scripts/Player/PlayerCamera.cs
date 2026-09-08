using UnityEngine;
using UnityEngine.InputSystem;

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{
    [Header("Sensibilidades")]
    public float sensMouse = 1f;
    public float sensGamepad = 5f;

    [Header("Debug")]
    [Tooltip("Activalo en el Editor para probar la velocidad de WebGL sin compilar.")]
    public bool simularWebGLEnEditor = false;

    [Header("Límites de Ángulo Vertical")]
    public float pitchMin = -85f;
    public float pitchMax = 85f;

    [Header("FOV Settings (Dash / Habilidades)")]
    public Camera mainCam;
    public float defaultFOV = 60f;
    public float fovTransitionSpeed = 10f;

    private float _pitch;
    private float _yaw;
    private float _multiplicadorPlataformaGamepad = 1f;
    private float _targetFOV;

    void Awake()
    {
        if (mainCam == null) mainCam = GetComponentInChildren<Camera>();
        if (mainCam == null) mainCam = Camera.main;

        if (mainCam != null) defaultFOV = mainCam.fieldOfView;
        _targetFOV = defaultFOV;

        ActualizarSensibilidad();
    }

    void Update()
    {
        ProcesarFOV();
    }

    public void Initialize(Transform cameraTarget)
    {
        if (cameraTarget != null)
        {
            transform.position = cameraTarget.position;
            transform.rotation = cameraTarget.rotation;
        }

        Vector3 euler = transform.eulerAngles;
        _pitch = euler.x;
        _yaw = euler.y;

        if (_pitch > 180f) _pitch -= 360f;

        ActualizarSensibilidad();
    }

    public void UpdatePosition(Transform cameraTarget)
    {
        if (cameraTarget != null)
        {
            transform.position = cameraTarget.position;
        }
    }

    public void UpdateRotation(CameraInput input)
    {
        bool esGamepad = Gamepad.current != null && (Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.001f);

        Vector2 lookDelta;

        if (esGamepad)
        {
            lookDelta = input.Look * (sensGamepad * 45f * _multiplicadorPlataformaGamepad * Time.unscaledDeltaTime);
        }
        else
        {
            lookDelta = input.Look * (sensMouse * 0.1f);
        }

        _yaw += lookDelta.x;
        _pitch -= lookDelta.y;

        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    public void ActualizarSensibilidad()
    {
        sensMouse = PlayerPrefs.GetFloat("SensibilidadMouse", 1f);
        sensGamepad = PlayerPrefs.GetFloat("SensibilidadGamepad", 5f);

        #if UNITY_WEBGL
            _multiplicadorPlataformaGamepad = 4.0f;
        #else
            _multiplicadorPlataformaGamepad = simularWebGLEnEditor ? 4.0f : 1.0f;
        #endif
    }

    private void ProcesarFOV()
    {
        if (mainCam != null && Mathf.Abs(mainCam.fieldOfView - _targetFOV) > 0.05f)
        {
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, _targetFOV, fovTransitionSpeed * Time.deltaTime);
        }
    }

    public void SetDashFOV(float fovBonus)
    {
        _targetFOV = defaultFOV + fovBonus;
    }

    public void ResetFOV()
    {
        _targetFOV = defaultFOV;
    }
}