using FMODUnity;
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
    [SerializeField] private EventReference interactionSound;

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
        if (!interactionSound.IsNull)
            RuntimeManager.PlayOneShot(interactionSound, transform.position);
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
            statusLabel.text = $"SHIFT 01\n{shiftProgress.Objective}";

        if (startShiftButton != null)
            startShiftButton.interactable = shiftProgress.CurrentStep == NPShiftProgress.Step.StartShift;

        if (submitReportButton != null)
            submitReportButton.interactable = shiftProgress.CanSubmitReport;
    }
}
