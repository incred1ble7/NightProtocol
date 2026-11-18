using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class NPFirstPersonController : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
    [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private float cameraPitch;
    private float verticalVelocity;

    public bool GameplayInputEnabled { get; private set; } = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>();

        if (viewCamera == null)
        {
            Debug.LogError("Player camera is missing.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        SetCursorLocked(true);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (!GameplayInputEnabled || keyboard == null || mouse == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);

            return;
        }

        Vector2 mouseDelta = mouse.delta.ReadValue() * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y, -85f, 85f);
        viewCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        transform.Rotate(Vector3.up, mouseDelta.x);

        Vector2 input = Vector2.zero;

        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 movement = transform.right * input.x + transform.forward * input.y;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        movement = movement * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(movement * Time.deltaTime);
    }

    public void SetGameplayInput(bool isEnabled)
    {
        GameplayInputEnabled = isEnabled;
        SetCursorLocked(isEnabled);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
