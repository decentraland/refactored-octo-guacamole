using DCL;
using UnityEngine;

public interface IBIWPublishController { }

public class BIWPublishController : BIWController, IBIWPublishController
{
    private IBIWEntityHandler biwEntityHandler;
    private IBIWCreatorController biwCreatorController;

    private BuilderInWorldBridge builderInWorldBridge;

    private int checkerSceneLimitsOptimizationCounter = 0;

    private const int FRAMES_BEETWEN_UPDATES = 10;
    private const string FEEDBACK_MESSAGE_ENTITY_ERROR = "Some entities have errors (marked as pink cubes).";
    private const string FEEDBACK_MESSAGE_OUTSIDE_BOUNDARIES = "Some entities are outside of the Scene boundaries.";
    private const string FEEDBACK_MESSAGE_TOO_MANY_ENTITIES = "Too many entities in the scene. Check scene limits.";

    private bool reportSceneLimitsOverpassedAnalytic = true;
    private float startPublishingTimestamp = 0;

    public override void Init(BIWReferencesController biwReferencesController)
    {
        base.Init(biwReferencesController);

        biwEntityHandler = biwReferencesController.biwEntityHandler;
        biwCreatorController = biwReferencesController.biwCreatorController;

        if (HUDController.i?.builderInWorldMainHud != null)
        {
            HUDController.i.builderInWorldMainHud.OnPublishAction += StartPublishFlow;
            HUDController.i.builderInWorldMainHud.OnConfirmPublishAction += StartPublishScene;
        }

        builderInWorldBridge = InitialSceneReferences.i.builderInWorldBridge;

        if (builderInWorldBridge != null)
            builderInWorldBridge.OnPublishEnd += PublishEnd;
    }

    public override void Update()
    {
        base.Update();
        if (checkerSceneLimitsOptimizationCounter >= FRAMES_BEETWEN_UPDATES)
        {
            checkerSceneLimitsOptimizationCounter = 0;
            CheckPublishConditions();
        }
        else
        {
            checkerSceneLimitsOptimizationCounter++;
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (HUDController.i.builderInWorldMainHud != null)
        {
            HUDController.i.builderInWorldMainHud.OnPublishAction -= StartPublishFlow;
            HUDController.i.builderInWorldMainHud.OnConfirmPublishAction -= StartPublishScene;
        }
        if (builderInWorldBridge != null)
            builderInWorldBridge.OnPublishEnd -= PublishEnd;
    }

    public bool CanPublish()
    {
        if (biwCreatorController.IsAnyErrorOnEntities())
            return false;

        if (!sceneToEdit.metricsController.IsInsideTheLimits())
            return false;

        if (!biwEntityHandler.AreAllEntitiesInsideBoundaries())
            return false;

        reportSceneLimitsOverpassedAnalytic = true;
        return true;
    }

    void CheckPublishConditions()
    {
        if (HUDController.i.builderInWorldMainHud is null)
            return;

        string feedbackMessage = "";
        if (biwCreatorController.IsAnyErrorOnEntities())
        {
            feedbackMessage = FEEDBACK_MESSAGE_ENTITY_ERROR;
        }
        else if (!biwEntityHandler.AreAllEntitiesInsideBoundaries())
        {
            feedbackMessage = FEEDBACK_MESSAGE_OUTSIDE_BOUNDARIES;
        }
        else if (!sceneToEdit.metricsController.IsInsideTheLimits())
        {
            feedbackMessage = FEEDBACK_MESSAGE_TOO_MANY_ENTITIES;
            if (reportSceneLimitsOverpassedAnalytic)
            {
                BIWAnalytics.SceneLimitsOverPassed(sceneToEdit.metricsController.GetModel());
                reportSceneLimitsOverpassedAnalytic = false;
            }
        }

        HUDController.i.builderInWorldMainHud.SetPublishBtnAvailability(CanPublish(), feedbackMessage);
    }

    private void StartPublishFlow()
    {
        if (!CanPublish())
            return;

        HUDController.i.builderInWorldMainHud.PublishStart();
    }

    private void StartPublishScene(string sceneName, string sceneDescription, string sceneScreenshot)
    {
        startPublishingTimestamp = Time.realtimeSinceStartup;
        BIWAnalytics.StartScenePublish(sceneToEdit.metricsController.GetModel());
        builderInWorldBridge.PublishScene(sceneToEdit, sceneName, sceneDescription, sceneScreenshot);
    }

    private void PublishEnd(bool isOk, string message)
    {
        if (HUDController.i.builderInWorldMainHud != null)
            HUDController.i.builderInWorldMainHud.PublishEnd(isOk, message);
        string successString = isOk ? "Success" : message;
        BIWAnalytics.EndScenePublish(sceneToEdit.metricsController.GetModel(), successString, Time.realtimeSinceStartup - startPublishingTimestamp);
    }
}