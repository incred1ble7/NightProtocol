using TMPro;
using NightProtocol.Mechanics;
using UnityEngine;
using UnityEngine.UI;

public sealed class NPOperatorTerminal : NPInteractable
{
    [SerializeField] private GreyboxFirstPerson playerController;
    [SerializeField] private NPShiftProgress shiftProgress;
    [SerializeField] private GameObject terminalWindow;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Button startShiftButton;
    [SerializeField] private Button submitReportButton;
    [SerializeField] private Button closeButton;

    public override string Prompt => "E - Operator terminal";

    private void Awake()
    {
        if (terminalWindow != null)
            terminalWindow.SetActive(false);

        if (startShiftButton != null)
            startShiftButton.onClick.AddListener(StartShift);

        if (submitReportButton != null)
            submitReportButton.onClick.AddListener(SubmitReport);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (startShiftButton != null)
            startShiftButton.onClick.RemoveListener(StartShift);

        if (submitReportButton != null)
            submitReportButton.onClick.RemoveListener(SubmitReport);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public override void Interact()
    {
        if (terminalWindow == null || playerController == null || shiftProgress == null)
            return;

        terminalWindow.SetActive(true);
        playerController.SetGameplayInput(false);
        Refresh();
    }

    public void Close()
    {
        if (terminalWindow != null)
            terminalWindow.SetActive(false);

        if (playerController != null)
            playerController.SetGameplayInput(true);
    }

    private void StartShift()
    {
        shiftProgress.StartShift();
        Refresh();
    }

    private void SubmitReport()
    {
        shiftProgress.SubmitReport();
        Refresh();
    }

    private void Refresh()
    {
        if (shiftProgress == null)
            return;

        if (statusLabel != null)
        {
            if (shiftProgress.ReportSubmitted)
                statusLabel.text = "SHIFT 01 COMPLETE\nReport submitted";
            else if (!shiftProgress.ShiftStarted)
                statusLabel.text = "SHIFT 01\nPress Start Shift to begin";
            else
                statusLabel.text = $"PERIMETER CHECK\n{shiftProgress.InspectedCount}/{shiftProgress.RequiredCount} checkpoints inspected";
        }

        if (startShiftButton != null)
            startShiftButton.interactable = !shiftProgress.ShiftStarted;

        if (submitReportButton != null)
            submitReportButton.interactable = shiftProgress.CanSubmitReport;
    }
}
