using FMODUnity;
using UnityEngine;

public sealed class NPCheckpoint : NPInteractable
{
    [SerializeField] private NPShiftProgress shiftProgress;
    [SerializeField] private string checkpointId = "P01";
    [SerializeField] private EventReference inspectSound;

    public override string Prompt => $"E - Inspect {checkpointId}";
    public override bool IsAvailable => shiftProgress && shiftProgress.CanInspectCheckpoint(checkpointId);

    public override void Interact()
    {
        if (IsAvailable && shiftProgress.InspectCheckpoint(checkpointId) && !inspectSound.IsNull)
            RuntimeManager.PlayOneShot(inspectSound, transform.position);
    }
}
