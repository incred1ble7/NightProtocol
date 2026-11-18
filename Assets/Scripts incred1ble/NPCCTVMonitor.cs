using NightProtocol.Mechanics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class NPCCTVMonitor : NPInteractable
{
    [SerializeField] private GreyboxFirstPerson playerController;
    [SerializeField] private NPShiftProgress shiftProgress;
    [SerializeField] private GameObject monitorWindow;
    [SerializeField] private RawImage monitorImage;
    [SerializeField] private Camera[] cameras;

    private int selectedCamera = -1;

    public override string Prompt => "E - CCTV monitor";
    public override bool IsAvailable => shiftProgress && shiftProgress.CanUseCCTV;

    private void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnCameraRendered;
    }

    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnCameraRendered;
        Close();
    }

    public override void Interact()
    {
        if (!IsAvailable || !playerController || !monitorWindow || !monitorImage)
            return;
        if (cameras == null || cameras.Length != 3 || !cameras[0] || !cameras[0].targetTexture)
            return;

        monitorWindow.SetActive(true);
        playerController.SetGameplayInput(false);
        ShowCamera(0);
    }

    public void ShowCamera(int index)
    {
        if (!monitorWindow || !monitorWindow.activeInHierarchy || index < 0 || index >= cameras.Length)
            return;

        Camera camera = cameras[index];
        if (!camera || !camera.targetTexture)
            return;

        DisableSelectedCamera();
        selectedCamera = index;
        monitorImage.texture = camera.targetTexture;
        camera.enabled = true;
    }

    public void Close()
    {
        if (selectedCamera < 0)
            return;

        DisableSelectedCamera();
        selectedCamera = -1;
        if (monitorImage)
            monitorImage.texture = null;
        if (monitorWindow)
            monitorWindow.SetActive(false);
        if (playerController && playerController.isActiveAndEnabled)
            playerController.SetGameplayInput(true);
    }

    private void DisableSelectedCamera()
    {
        if (selectedCamera >= 0 && cameras[selectedCamera])
            cameras[selectedCamera].enabled = false;
    }

    private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
    {
        if (selectedCamera >= 0 && camera == cameras[selectedCamera] && monitorWindow && monitorWindow.activeInHierarchy && shiftProgress)
            shiftProgress.ViewCamera(selectedCamera);
    }
}
