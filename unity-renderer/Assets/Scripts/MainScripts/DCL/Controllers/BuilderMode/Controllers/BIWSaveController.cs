using DCL;
using DCL.Controllers;
using UnityEngine;

public class BIWSaveController : BIWController
{
    [SerializeField]
    private float msBetweenSaves = 5000f;

    [Header("Prefab reference")]
    public BuilderInWorldBridge builderInWorldBridge;

    public int numberOfSaves { get; private set; } = 0;

    private float nextTimeToSave;
    private bool canActivateSave = true;

    public override void Init()
    {
        base.Init();

        builderInWorldBridge = InitialSceneReferences.i.builderInWorldBridge;
        if (builderInWorldBridge != null)
            builderInWorldBridge.OnKernelUpdated += TryToSave;

        if (HUDController.i.builderInWorldMainHud != null)
        {
            HUDController.i.builderInWorldMainHud.OnSaveSceneInfoAction += SaveSceneInfo;
            HUDController.i.builderInWorldMainHud.OnConfirmPublishAction += ConfirmPublishScene;
        }
    }

    private void OnDestroy()
    {
        if (builderInWorldBridge != null)
            builderInWorldBridge.OnKernelUpdated -= TryToSave;

        if (HUDController.i.builderInWorldMainHud != null)
        {
            HUDController.i.builderInWorldMainHud.OnSaveSceneInfoAction -= SaveSceneInfo;
            HUDController.i.builderInWorldMainHud.OnConfirmPublishAction -= ConfirmPublishScene;
        }
    }

    public void ResetSaveTime() { nextTimeToSave = 0; }

    public void ResetNumberOfSaves() { numberOfSaves = 0; }

    public override void EnterEditMode(ParcelScene scene)
    {
        base.EnterEditMode(scene);
        nextTimeToSave = DCLTime.realtimeSinceStartup + msBetweenSaves / 1000f;
        ResetNumberOfSaves();
    }

    public void SetSaveActivation(bool isActive, bool tryToSave = false)
    {
        canActivateSave = isActive;
        if (tryToSave)
            TryToSave();
    }

    public bool CanSave() { return DCLTime.realtimeSinceStartup >= nextTimeToSave && isEditModeActive && canActivateSave; }

    public void TryToSave()
    {
        if (CanSave())
            ForceSave();
    }

    public void ForceSave()
    {
        if (!isEditModeActive || !canActivateSave)
            return;

        builderInWorldBridge.SaveSceneState(sceneToEdit);
        nextTimeToSave = DCLTime.realtimeSinceStartup + msBetweenSaves / 1000f;
        HUDController.i.builderInWorldMainHud?.SceneSaved();
        numberOfSaves++;
    }

    public void SaveSceneInfo(string sceneName, string sceneDescription, string sceneScreenshot) { builderInWorldBridge.SaveSceneInfo(sceneToEdit, sceneName, sceneDescription, sceneScreenshot); }

    void ConfirmPublishScene(string sceneName, string sceneDescription, string sceneScreenshot) { ResetNumberOfSaves(); }
}