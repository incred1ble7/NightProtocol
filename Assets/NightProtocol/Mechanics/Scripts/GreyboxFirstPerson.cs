using UnityEngine;
using UnityEngine.InputSystem;

namespace NightProtocol.Mechanics
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GreyboxFirstPerson : MonoBehaviour
    {
        public Camera viewCamera;
        public float walkSpeed = 4f;
        public float sprintSpeed = 6.5f;
        public float mouseSensitivity = 0.1f;
        public float interactDistance = 3f;
        public float jumpHeight = 0.8f;
        CharacterController body;
        float pitch;
        float verticalVelocity;
        Vector3 spawn;
        MechanicalInteractable focused;
        GUIStyle promptStyle, hintStyle;
        void Awake()
        {
            body = GetComponent<CharacterController>();
            spawn = transform.position;
            if (!viewCamera) viewCamera = GetComponentInChildren<Camera>();
        }
        void Start() { CaptureMouse(); }
        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
        void CaptureMouse() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        void Update()
        {
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            if (keys == null || !viewCamera) return;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) CaptureMouse();
            bool active = Cursor.lockState == CursorLockMode.Locked;
            if (active && mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, delta.x, 0f);
                pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
                viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            Vector2 movement = Vector2.zero;
            if (active)
            {
                movement.x = (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
                movement.y = (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0);
                movement = Vector2.ClampMagnitude(movement, 1f);
            }
            if (body.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            if (active && body.isGrounded && keys.spaceKey.wasPressedThisFrame) verticalVelocity = Mathf.Sqrt(2f * 18f * jumpHeight);
            verticalVelocity -= 18f * Time.deltaTime;
            float speed = keys.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
            Vector3 velocity = (transform.right * movement.x + transform.forward * movement.y) * speed + Vector3.up * verticalVelocity;
            body.Move(velocity * Time.deltaTime);
            if (transform.position.y < -25f) { body.enabled = false; transform.position = spawn; body.enabled = true; verticalVelocity = 0f; }
            focused = FindTarget(viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f)), interactDistance);
            if (active && focused && keys.eKey.wasPressedThisFrame) focused.Toggle();
        }
        public static MechanicalInteractable FindTarget(Ray ray, float distance)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore)) return null;
            var proxy = hit.collider.GetComponentInParent<InteractionTarget>();
            return proxy && proxy.target ? proxy.target : hit.collider.GetComponentInParent<MechanicalInteractable>();
        }
        void OnGUI()
        {
            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 19 };
                promptStyle.normal.textColor = Color.white;
                hintStyle = new GUIStyle(promptStyle) { alignment = TextAnchor.UpperLeft, fontSize = 15 };
            }
            GUI.Label(new Rect(20, 18, 700, 28), "WASD  Move    Mouse  Look    Shift  Run    Space  Jump    E  Interact    Esc  Cursor", hintStyle);
            GUI.Label(new Rect(Screen.width / 2f - 8, Screen.height / 2f - 12, 16, 24), "+", promptStyle);
            if (focused) GUI.Label(new Rect(0, Screen.height * 0.7f, Screen.width, 36), "[E]  " + focused.Prompt, promptStyle);
            if (Cursor.lockState != CursorLockMode.Locked)
                GUI.Label(new Rect(0, Screen.height * 0.8f, Screen.width, 36), "Click to look around", promptStyle);
        }
    }
}
