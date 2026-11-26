using System;
using System.Collections.Generic;
using FMODUnity;
using NightProtocol.Mechanics;
using TMPro;
using UnityEngine;

public sealed class NPShiftProgress : MonoBehaviour
{
    public enum Step
    {
        StartShift,
        ElectricalPanel,
        CCTV,
        Checkpoints,
        Generator,
        SubmitReport,
        Sleep,
        Finished
    }

    public enum TaskStatus { Inactive, Active, Completed }

    private const int CameraCount = 3;
    private const int RequiredSwitches = 3;
    private const int RequiredCheckpoints = 2;

    [SerializeField] private MechanicalInteractable[] panelSwitches;
    [SerializeField] private MechanicalInteractable generatorLever;
    [SerializeField] private StudioEventEmitter generatorLoop;
    [SerializeField] private EventReference generatorStartSound;
    [SerializeField] private EventReference lateShiftSting;
    [SerializeField] private TMP_Text objectiveLabel;

    private readonly HashSet<string> inspectedCheckpoints = new HashSet<string>();
    private readonly HashSet<int> viewedCameras = new HashSet<int>();

    public Step CurrentStep { get; private set; }
    public bool CanSubmitReport => CurrentStep == Step.SubmitReport;
    public bool CanSleep => CurrentStep == Step.Sleep;
    public bool CanUseCCTV => CurrentStep >= Step.CCTV && CurrentStep < Step.Finished;

    public TaskStatus GetStatus(Step step)
    {
        if (step < CurrentStep)
            return TaskStatus.Completed;

        return step == CurrentStep && step != Step.Finished ? TaskStatus.Active : TaskStatus.Inactive;
    }

    public string Objective => CurrentStep switch
    {
        Step.StartShift => "Start your shift at the operator terminal",
        Step.ElectricalPanel => $"Set panel switches 1, 2 and 3 to ON ({PoweredSwitchCount()}/{RequiredSwitches})",
        Step.CCTV => $"View all CCTV feeds at the monitor ({viewedCameras.Count}/{CameraCount})",
        Step.Checkpoints => $"Inspect P01 and P02 ({inspectedCheckpoints.Count}/{RequiredCheckpoints})",
        Step.Generator => "Activate the test lever in the generator building",
        Step.SubmitReport => "Return to the operator terminal and submit the report",
        Step.Sleep => "Report submitted. Go to the bed to end your shift",
        Step.Finished => "Day 1 complete",
        _ => string.Empty
    };

    private void Awake()
    {
        if (panelSwitches != null)
            foreach (var panelSwitch in panelSwitches)
                if (panelSwitch)
                    panelSwitch.onStateChanged.AddListener(OnPanelSwitchChanged);

        if (generatorLever)
            generatorLever.onStateChanged.AddListener(OnGeneratorChanged);

        RefreshObjective();
    }

    private void OnDestroy()
    {
        if (panelSwitches != null)
            foreach (var panelSwitch in panelSwitches)
                if (panelSwitch)
                    panelSwitch.onStateChanged.RemoveListener(OnPanelSwitchChanged);

        if (generatorLever)
            generatorLever.onStateChanged.RemoveListener(OnGeneratorChanged);
    }

    public void StartShift()
    {
        if (CurrentStep == Step.StartShift)
            AdvanceTo(Step.ElectricalPanel);
    }

    public string MechanicalBlockReason(MechanicalInteractable target)
    {
        if (target == generatorLever)
        {
            if (CurrentStep < Step.Generator)
                return "Generator test is not active yet";
            if (CurrentStep > Step.Generator)
                return "Generator test complete";
        }

        if (CurrentStep == Step.StartShift && panelSwitches != null && Array.IndexOf(panelSwitches, target) >= 0)
            return "Start the shift before checking the panel";

        return null;
    }

    private void OnPanelSwitchChanged(bool _)
    {
        if (CurrentStep != Step.ElectricalPanel)
            return;

        if (PoweredSwitchCount() == RequiredSwitches)
            AdvanceTo(Step.CCTV);
        else
            RefreshObjective();
    }

    private int PoweredSwitchCount()
    {
        if (panelSwitches == null || panelSwitches.Length != RequiredSwitches)
            return 0;

        int powered = 0;
        for (int i = 0; i < panelSwitches.Length; i++)
        {
            var panelSwitch = panelSwitches[i];
            if (panelSwitch && panelSwitch.IsOn && Array.IndexOf(panelSwitches, panelSwitch) == i)
                powered++;
        }

        return powered;
    }

    public void ViewCamera(int index)
    {
        if (CurrentStep != Step.CCTV || index < 0 || index >= CameraCount)
            return;

        if (!viewedCameras.Add(index))
            return;

        if (viewedCameras.Count == CameraCount)
            AdvanceTo(Step.Checkpoints);
        else
            RefreshObjective();
    }

    public bool CanInspectCheckpoint(string checkpointId)
    {
        return CurrentStep == Step.Checkpoints
            && (checkpointId == "P01" || checkpointId == "P02")
            && !inspectedCheckpoints.Contains(checkpointId);
    }

    public bool InspectCheckpoint(string checkpointId)
    {
        if (!CanInspectCheckpoint(checkpointId))
            return false;

        inspectedCheckpoints.Add(checkpointId);
        if (inspectedCheckpoints.Count == RequiredCheckpoints)
            AdvanceTo(Step.Generator);
        else
            RefreshObjective();

        return true;
    }

    private void OnGeneratorChanged(bool isOn)
    {
        if (CurrentStep != Step.Generator || !isOn)
            return;

        if (generatorLoop && !generatorLoop.EventReference.IsNull && !generatorLoop.IsPlaying())
            generatorLoop.Play();

        if (!generatorStartSound.IsNull)
            RuntimeManager.PlayOneShot(generatorStartSound, generatorLever.transform.position);

        AdvanceTo(Step.SubmitReport);
        if (!lateShiftSting.IsNull)
            Invoke(nameof(PlayLateShiftSting), 1.25f);
    }

    private void OnDisable() => CancelInvoke(nameof(PlayLateShiftSting));

    private void PlayLateShiftSting()
    {
        if (CurrentStep is Step.SubmitReport or Step.Sleep && !lateShiftSting.IsNull)
            RuntimeManager.PlayOneShot(lateShiftSting);
    }

    public bool SubmitReport()
    {
        if (!CanSubmitReport)
            return false;

        AdvanceTo(Step.Sleep);
        return true;
    }

    public bool FinishShift()
    {
        if (!CanSleep)
            return false;

        if (generatorLoop && !generatorLoop.EventReference.IsNull)
            generatorLoop.Stop();

        AdvanceTo(Step.Finished);
        return true;
    }

    private void AdvanceTo(Step nextStep)
    {
        CurrentStep = nextStep;
        RefreshObjective();
    }

    private void RefreshObjective()
    {
        if (objectiveLabel)
            objectiveLabel.text = Objective;
    }
}
