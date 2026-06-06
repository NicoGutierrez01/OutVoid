using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerAbilities playerAbilities;

    private PlayerInputActions _inputActions;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();

        // Auto-buscar referencias en hijos si no están asignadas
        if (playerCharacter == null) playerCharacter = GetComponentInChildren<PlayerCharacter>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<PlayerCamera>();
        if (cameraSpring == null) cameraSpring = GetComponentInChildren<CameraSpring>();
        if (cameraLean == null) cameraLean = GetComponentInChildren<CameraLean>();
        if (playerStats == null) playerStats = GetComponentInChildren<PlayerStats>();
        if (playerAbilities == null) playerAbilities = GetComponentInChildren<PlayerAbilities>();

        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());
        cameraSpring.Initialize();
        cameraLean.Initialize();
        playerStats.Initialize();
    }

    void OnDestroy()
    {
        if (_inputActions != null)
        {
            _inputActions.Gameplay.Disable();
            _inputActions.Dispose();
        }
    }

    void Update()
    {
        
        if (Time.timeScale == 0f) return;

        var input = _inputActions.Gameplay;
        var deltaTime = Time.deltaTime;

        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);

        var characterInput = new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Crouch = input.Crouch.IsPressed() ? CrouchInput.Hold : CrouchInput.None,
            Dash = input.Dash.WasPressedThisFrame(),
            Ultimate = input.Ultimate.WasPressedThisFrame(),
            AbilityE = input.AbilityE.WasPressedThisFrame(),
Melee = input.Melee.WasPressedThisFrame(),
        };

        playerCharacter.UpdateInput(characterInput);
        playerCharacter.UpdateBody(deltaTime);
        playerAbilities.UpdateInput(characterInput);

        float distanciaInteraccion = 3.5f; 
        
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hitInteract, distanciaInteraccion))
        {
            if (hitInteract.transform.CompareTag("ItemBoss"))
            {
                ItemInteractable itemMirado = hitInteract.transform.GetComponent<ItemInteractable>();
                if (itemMirado != null && itemMirado.data != null)
                {
                    Debug.Log("MIRANDO: " + itemMirado.data.nombreItem + " | " + itemMirado.data.descripcion + " | Presiona F para agarrar");

                    if (input.Interact.WasPressedThisFrame())
                    {
                        itemMirado.Recoger(gameObject);
                    }
                }
            }
        }

    #if UNITY_EDITOR
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit))
            {
                Teleport(hit.point);
            }
        }
    #endif
    }

    void LateUpdate()
    {
        // Sincronizar posición del root con el Character
        transform.position = playerCharacter.transform.position;

        var deltaTime = Time.deltaTime;
        var cameraTarget = playerCharacter.GetCameraTarget();
        var state = playerCharacter.GetState();

        playerCamera.UpdatePosition(cameraTarget);
        cameraSpring.UpdateSpring(deltaTime, cameraTarget.up);

        if (state.Stance != Stance.Slide)
            cameraLean.UpdateLean(deltaTime, state.Acceleration, cameraTarget.up);
        else
            cameraLean.transform.localRotation = Quaternion.identity;
    }

    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }
}