using System.Collections;
using NightProtocol.Mechanics;
using UnityEngine;

public sealed class NPShiftBed : NPInteractable
{
    [SerializeField] private NPShiftProgress shiftProgress;
    [SerializeField] private GreyboxFirstPerson playerController;
    [SerializeField] private CanvasGroup completionScreen;
    [SerializeField, Min(0.1f)] private float fadeSeconds = 1f;

    public override string Prompt => "E - Sleep";
    public override bool IsAvailable => shiftProgress && shiftProgress.CanSleep && playerController && completionScreen;

    public override void Interact()
    {
        if (!IsAvailable || !shiftProgress.FinishShift())
            return;

        playerController.SetGameplayInput(false);
        Cursor.visible = false;
        completionScreen.alpha = 0f;
        completionScreen.gameObject.SetActive(true);
        StartCoroutine(FadeToBlack());
    }

    private IEnumerator FadeToBlack()
    {
        for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            completionScreen.alpha = elapsed / fadeSeconds;
            yield return null;
        }

        completionScreen.alpha = 1f;
    }
}
