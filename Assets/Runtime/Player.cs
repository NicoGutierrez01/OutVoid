using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

public class Player : MonoBehaviour
{
[SerializeField] private PlayerCharacter playerCharacter;
[SerializeField] private PlayerCamera playerCamera;

[SerializeField] private CameraSpring cameraSpring;
[SerializeField] private CameraLean cameraLean;

private PlayerInputActions _inputActions;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();

        playerCharacter.Initialize();
        playerCamera.Initialize (playerCharacter.GetCameraTarget());

        cameraSpring.Initialize();
        cameraLean.Initialize();
    }


    void OnDestroy()
    {
        _inputActions.Dispose();
    }


    // Update is called once per frame
    void Update()
    {
        var input = _inputActions.Gameplay;
        var deltaTime = Time.deltaTime;
        var CameraInput = new CameraInput {Look = input.Look.ReadValue<Vector2>()};
        playerCamera.UpdateRotation(CameraInput);

        var characterInput = new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Crouch = input.Crouch.WasPressedThisFrame()
                ? CrouchInput.Toggle
                : CrouchInput.None
        };
        playerCharacter.UpdateInput(characterInput); 
        playerCharacter.UpdateBody(deltaTime);

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
        var deltaTime = Time.deltaTime;
        var cameraTarget = playerCharacter.GetCameraTarget();
        var state = playerCharacter.GetState();

        playerCamera.UpdatePosition(cameraTarget);
        cameraSpring.UpdateSpring(deltaTime, cameraTarget.up);
if (state.Stance != Stance.Slide)
{
    cameraLean.UpdateLean(deltaTime, state.Acceleration, cameraTarget.up);
}
else
{
    cameraLean.transform.localRotation = Quaternion.identity;
}    }

    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }

}
