using UnityEngine;

public sealed class NPCheckpoint : NPInteractable
{
    [SerializeField] private NPShiftProgress shiftProgress;
    [SerializeField] private string checkpointId = "P01";

    private bool inspected;

    public override string Prompt => $"E - Inspect {checkpointId}";
    public override bool IsAvailable => shiftProgress != null && shiftProgress.ShiftStarted && !inspected;

    public override void Interact()
    {
        if (!IsAvailable)
            return;

        inspected = shiftProgress.InspectCheckpoint(checkpointId);
    }
}
