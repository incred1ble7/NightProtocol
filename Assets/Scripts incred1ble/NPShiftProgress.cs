using System.Collections.Generic;
using UnityEngine;

public sealed class NPShiftProgress : MonoBehaviour
{
    [SerializeField, Min(1)] private int requiredCheckpoints = 2;

    private readonly HashSet<string> inspectedCheckpoints = new HashSet<string>();

    public bool ShiftStarted { get; private set; }
    public bool ReportSubmitted { get; private set; }
    public int InspectedCount => inspectedCheckpoints.Count;
    public int RequiredCount => requiredCheckpoints;
    public bool CanSubmitReport => ShiftStarted && !ReportSubmitted && InspectedCount >= RequiredCount;

    public void StartShift()
    {
        if (!ShiftStarted)
            ShiftStarted = true;
    }

    public bool InspectCheckpoint(string checkpointId)
    {
        if (!ShiftStarted || ReportSubmitted || string.IsNullOrWhiteSpace(checkpointId))
            return false;

        return inspectedCheckpoints.Add(checkpointId);
    }

    public bool SubmitReport()
    {
        if (!CanSubmitReport)
            return false;

        ReportSubmitted = true;
        return true;
    }
}
