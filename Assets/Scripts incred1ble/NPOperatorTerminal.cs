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
    [SerializeField] private EventReference buttonSound;
    [SerializeField] private EventReference reportCompletedSound;

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
        if (terminalWindow == null || terminalWindow.activeSelf || playerController == null || shiftProgress == null)
            return;

        terminalWindow.SetActive(true);
        playerController.SetGameplayInput(false);
        if (!interactionSound.IsNull)
            RuntimeManager.PlayOneShot(interactionSound, transform.position);
        Refresh();
    }

    public void Close()
    {
        if (terminalWindow == null || !terminalWindow.activeSelf)
            return;

        terminalWindow.SetActive(false);
        PlayButtonSound();

        if (playerController != null)
            playerController.SetGameplayInput(true);
    }

    private void StartShift()
    {
        if (!terminalWindow || !terminalWindow.activeInHierarchy || !shiftProgress
            || shiftProgress.CurrentStep != NPShiftProgress.Step.StartShift)
            return;

        shiftProgress.StartShift();
        PlayButtonSound();
        Refresh();
    }

    private void SubmitReport()
    {
        if (!terminalWindow || !terminalWindow.activeInHierarchy || !shiftProgress || !shiftProgress.SubmitReport())
            return;

        PlayButtonSound();
        if (!reportCompletedSound.IsNull)
            RuntimeManager.PlayOneShot(reportCompletedSound);
        Refresh();
    }

    private void PlayButtonSound()
    {
        if (!buttonSound.IsNull)
            RuntimeManager.PlayOneShot(buttonSound);
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
