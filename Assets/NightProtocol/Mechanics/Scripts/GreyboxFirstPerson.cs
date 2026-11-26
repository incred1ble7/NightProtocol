using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using TMPro;

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
        [SerializeField] bool allowJumpAndSprint = true;
        [SerializeField] TMP_Text interactionPrompt;
        [SerializeField] NPShiftProgress shiftProgress;
        [SerializeField] NPInteriorAudio interiorAudio;
        [FormerlySerializedAs("footstepSound")]
        [SerializeField] EventReference outdoorFootstepSound;
        [SerializeField] EventReference indoorFootstepSound;
        [SerializeField] Transform towerStairSteps;
        [SerializeField, Min(0.1f)] float stepDistance = 1.5f;
        public bool GameplayInputEnabled { get; private set; } = true;
        CharacterController body;
        float pitch;
        float verticalVelocity;
        float distanceSinceStep;
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
        public void SetGameplayInput(bool enabled)
        {
            GameplayInputEnabled = enabled;
            if (enabled) CaptureMouse();
            else
            {
                distanceSinceStep = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (interactionPrompt) interactionPrompt.text = string.Empty;
            }
        }

        void Update()
        {
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            if (keys == null || !viewCamera) return;
            if (!GameplayInputEnabled) return;
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
            if (allowJumpAndSprint && active && body.isGrounded && keys.spaceKey.wasPressedThisFrame) verticalVelocity = Mathf.Sqrt(2f * 18f * jumpHeight);
            verticalVelocity -= 18f * Time.deltaTime;
            float speed = allowJumpAndSprint && keys.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
            Vector3 velocity = (transform.right * movement.x + transform.forward * movement.y) * speed + Vector3.up * verticalVelocity;
            Vector3 previousPosition = transform.position;
            bool groundedBeforeMove = body.isGrounded;
            body.Move(velocity * Time.deltaTime);
            Vector3 displacement = transform.position - previousPosition;
            if (transform.position.y < -25f) { body.enabled = false; transform.position = spawn; body.enabled = true; verticalVelocity = 0f; }
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            focused = null;
            NPInteractable shiftTarget = null;
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                shiftTarget = hit.collider.GetComponentInParent<NPInteractable>();
                if (!shiftTarget) focused = FindMechanicalTarget(hit.collider);
            }

            bool shiftTargetAvailable = shiftTarget && shiftTarget.isActiveAndEnabled && shiftTarget.IsAvailable;
            string blockReason = focused && shiftProgress ? shiftProgress.MechanicalBlockReason(focused) : null;
            if (interactionPrompt)
                interactionPrompt.text = active
                    ? shiftTargetAvailable ? shiftTarget.Prompt : focused ? blockReason ?? "[E] " + focused.Prompt : string.Empty
                    : "Click to look around";

            if (active && keys.eKey.wasPressedThisFrame)
            {
                if (shiftTargetAvailable) shiftTarget.Interact();
                else if (focused && blockReason == null) focused.Toggle();
            }

            if (GameplayInputEnabled && groundedBeforeMove && body.isGrounded && movement != Vector2.zero)
                PlayFootstep(displacement);
        }

        private EventReference FootstepSound => (interiorAudio && interiorAudio.IsInside) || IsOnTowerStep()
            ? indoorFootstepSound
            : outdoorFootstepSound;

        private bool IsOnTowerStep()
        {
            if (!towerStairSteps)
                return false;

            Bounds capsule = body.bounds;
            float distance = capsule.extents.y + body.stepOffset + body.skinWidth;
            return Physics.Raycast(capsule.center, Vector3.down, out var ground, distance,
                       Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                   && ground.collider.transform.IsChildOf(towerStairSteps);
        }

        private void PlayFootstep(Vector3 displacement)
        {
            displacement.y = 0;
            distanceSinceStep += displacement.magnitude;
            float stride = Mathf.Max(0.1f, stepDistance);
            if (distanceSinceStep < stride)
                return;

            distanceSinceStep %= stride;
            var sound = FootstepSound;
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, transform.position);
        }
        public static MechanicalInteractable FindTarget(Ray ray, float distance)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore)) return null;
            return FindMechanicalTarget(hit.collider);
        }
        static MechanicalInteractable FindMechanicalTarget(Collider collider)
        {
            var proxy = collider.GetComponentInParent<InteractionTarget>();
            return proxy && proxy.target ? proxy.target : collider.GetComponentInParent<MechanicalInteractable>();
        }
        void OnGUI()
        {
            if (interactionPrompt || !GameplayInputEnabled) return;
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
