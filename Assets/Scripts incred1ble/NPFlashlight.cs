using UnityEngine;
using UnityEngine.InputSystem;
using NightProtocol.Mechanics;

public sealed class NPFlashlight : MonoBehaviour
{
    [SerializeField] private Light flashlight;
    [SerializeField] private GreyboxFirstPerson playerController;
    [SerializeField] private bool startEnabled;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<GreyboxFirstPerson>();

        if (flashlight == null)
        {
            Debug.LogError("Flashlight is not assigned.", this);
            enabled = false;
            return;
        }

        flashlight.enabled = startEnabled;
    }

    private void Update()
    {
        if (playerController == null || !playerController.GameplayInputEnabled)
            return;

        if (Cursor.lockState != CursorLockMode.Locked || Keyboard.current == null)
            return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
            flashlight.enabled = !flashlight.enabled;
    }
}
