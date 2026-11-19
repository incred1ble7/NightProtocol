using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class NPInteractor : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private NPFirstPersonController playerController;
    [SerializeField] private TMP_Text promptLabel;
    [SerializeField, Min(0.5f)] private float interactionDistance = 2.5f;
    [SerializeField] private LayerMask interactionLayers = ~0;

    private void Awake()
    {
        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>();

        if (playerController == null)
            playerController = GetComponent<NPFirstPersonController>();
    }

    private void Update()
    {
        NPInteractable target = FindTarget();

        if (promptLabel != null)
            promptLabel.text = target != null ? target.Prompt : string.Empty;

        if (target != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            target.Interact();
    }

    private NPInteractable FindTarget()
    {
        if (viewCamera == null || playerController == null || !playerController.GameplayInputEnabled)
            return null;

        if (Cursor.lockState != CursorLockMode.Locked)
            return null;

        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayers, QueryTriggerInteraction.Ignore))
            return null;

        NPInteractable interactable = hit.collider.GetComponentInParent<NPInteractable>();
        return interactable != null && interactable.IsAvailable ? interactable : null;
    }
}
