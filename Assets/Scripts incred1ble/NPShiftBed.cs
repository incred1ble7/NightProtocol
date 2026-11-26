using System.Collections;
using NightProtocol.Mechanics;
using TMPro;
using UnityEngine;

public sealed class NPShiftBed : NPInteractable
{
    [SerializeField] private NPShiftProgress shiftProgress;
    [SerializeField] private GreyboxFirstPerson playerController;
    [SerializeField] private CanvasGroup completionScreen;
    [SerializeField] private TMP_Text completionLabel;
    [SerializeField, Min(0.1f)] private float fadeSeconds = 1f;
    [SerializeField, Min(0.1f)] private float textFadeSeconds = 1.2f;
    [SerializeField, TextArea] private string completionText = "DAY 1 COMPLETE\n<size=45%>Your shift is over. Get some rest.</size>";

    public override string Prompt => "E - Sleep";
    public override bool IsAvailable => shiftProgress && shiftProgress.CanSleep && playerController && completionScreen;

    public override void Interact()
    {
        if (!IsAvailable || !shiftProgress.FinishShift())
            return;

        playerController.SetGameplayInput(false);
        Cursor.visible = false;
        completionScreen.alpha = 0f;
        if (completionLabel)
        {
            completionLabel.text = completionText;
            completionLabel.alpha = 0f;
        }
        completionScreen.gameObject.SetActive(true);
        StartCoroutine(FadeToBlack());
    }

    private IEnumerator FadeToBlack()
    {
        for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            completionScreen.alpha = Mathf.SmoothStep(0f, 1f, elapsed / fadeSeconds);
            yield return null;
        }

        completionScreen.alpha = 1f;

        if (!completionLabel)
            yield break;

        for (float elapsed = 0f; elapsed < textFadeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            completionLabel.alpha = Mathf.SmoothStep(0f, 1f, elapsed / textFadeSeconds);
            yield return null;
        }

        completionLabel.alpha = 1f;
    }
}
