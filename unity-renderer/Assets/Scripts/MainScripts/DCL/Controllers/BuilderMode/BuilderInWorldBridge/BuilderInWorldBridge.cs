using DCL;
using DCL.Components;
using DCL.Configuration;
using DCL.Controllers;
using DCL.Interface;
using DCL.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ProtocolV2;
using Environment = DCL.Environment;

/// <summary>
/// This class will handle all the messages that will be sent to kernel.
/// </summary>
public class BuilderInWorldBridge : MonoBehaviour
{
    public BuilderProjectPayload builderProject { get => builderProjectPayload; }

    //Note Adrian: OnKernelUpdated in not called in the update of the transform, since it will give a lot of 
    //events and probably dont need to get called with that frecuency
    public event Action OnKernelUpdated;
    public event Action<bool, string> OnPublishEnd;
    public event Action<string, string> OnBuilderProjectInfo;
    public event Action<string> OnCatalogHeadersReceived;

    //This is done for optimization purposes, recreating new objects can increase garbage collection
    TransformComponent entityTransformComponentModel = new TransformComponent();

    StoreSceneStateEvent storeSceneState = new StoreSceneStateEvent();
    SaveSceneStateEvent saveSceneState = new SaveSceneStateEvent();
    SaveProjectInfoEvent saveProjectInfo = new SaveProjectInfoEvent();
    ModifyEntityComponentEvent modifyEntityComponentEvent = new ModifyEntityComponentEvent();
    EntityPayload entityPayload = new EntityPayload();
    EntitySingleComponentPayload entitySingleComponentPayload = new EntitySingleComponentPayload();
    BuilderProjectPayload builderProjectPayload = new BuilderProjectPayload();

    #region MessagesFromKernel

    public void PublishSceneResult(string payload)
    {
        PublishSceneResultPayload publishSceneResultPayload = JsonUtility.FromJson<PublishSceneResultPayload>(payload);

        if (publishSceneResultPayload.ok)
        {
            OnPublishEnd?.Invoke(true, "");

            AudioScriptableObjects.confirm.Play();
        }
        else
        {
            OnPublishEnd?.Invoke(false, publishSceneResultPayload.error);

            AudioScriptableObjects.error.Play();
        }
    }

    public void BuilderInWorldCatalogHeaders(string payload) { OnCatalogHeadersReceived?.Invoke(payload); }

    public void BuilderProjectInfo(string payload)
    {
        builderProjectPayload = JsonUtility.FromJson<BuilderProjectPayload>(payload);
        OnBuilderProjectInfo?.Invoke(builderProjectPayload.title, builderProjectPayload.description);
    }

    #endregion

    #region MessagesToKernel

    public void AskKernelForCatalogHeaders() { WebInterface.SendMessage(BuilderInWorldSettings.BIW_HEADER_REQUEST_EVENT_NAME); }

    public void UpdateSmartItemComponent(DCLBuilderInWorldEntity entity, ParcelScene scene)
    {
        SmartItemComponent smartItemComponent = entity.rootEntity.TryGetComponent<SmartItemComponent>();
        if (smartItemComponent == null)
            return;

        entitySingleComponentPayload.entityId = entity.rootEntity.entityId;
        entitySingleComponentPayload.componentId = (int) CLASS_ID_COMPONENT.SMART_ITEM;

        entitySingleComponentPayload.data = smartItemComponent.GetValues();

        ChangeEntityComponent(entitySingleComponentPayload, scene);
    }

    public void SaveSceneInfo(ParcelScene scene, string sceneName, string sceneDescription, string sceneScreenshot)
    {
        saveProjectInfo.payload.title = sceneName;
        saveProjectInfo.payload.description = sceneDescription;
        saveProjectInfo.payload.screenshot = sceneScreenshot;

        WebInterface.SendSceneEvent(scene.sceneData.id, BuilderInWorldSettings.STATE_EVENT_NAME, saveProjectInfo);
    }

    public void SaveSceneState(ParcelScene scene)
    {
        saveSceneState.payload = JsonUtility.ToJson(builderProjectPayload);
        WebInterface.SendSceneEvent(scene.sceneData.id, BuilderInWorldSettings.STATE_EVENT_NAME, saveSceneState);
    }

    public void ChangeEntityLockStatus(DCLBuilderInWorldEntity entity, ParcelScene scene)
    {
        entitySingleComponentPayload.entityId = entity.rootEntity.entityId;
        entitySingleComponentPayload.componentId = (int) CLASS_ID.LOCKED_ON_EDIT;

        foreach (KeyValuePair<Type, ISharedComponent> keyValuePairBaseDisposable in entity.rootEntity.sharedComponents)
        {
            if (keyValuePairBaseDisposable.Value.GetClassId() == (int) CLASS_ID.LOCKED_ON_EDIT)
            {
                entitySingleComponentPayload.data = ((DCLLockedOnEdit) keyValuePairBaseDisposable.Value).GetModel();
            }
        }

        ChangeEntityComponent(entitySingleComponentPayload, scene);
    }

    public void ChangedEntityName(DCLBuilderInWorldEntity entity, ParcelScene scene)
    {
        entitySingleComponentPayload.entityId = entity.rootEntity.entityId;
        entitySingleComponentPayload.componentId = (int) CLASS_ID.NAME;

        foreach (KeyValuePair<Type, ISharedComponent> keyValuePairBaseDisposable in entity.rootEntity.sharedComponents)
        {
            if (keyValuePairBaseDisposable.Value.GetClassId() == (int) CLASS_ID.NAME)
            {
                entitySingleComponentPayload.data = ((DCLName) keyValuePairBaseDisposable.Value).GetModel();
            }
        }

        ChangeEntityComponent(entitySingleComponentPayload, scene);
    }

    void ChangeEntityComponent(EntitySingleComponentPayload payload, ParcelScene scene)
    {
        modifyEntityComponentEvent.payload = payload;

        WebInterface.SceneEvent<ModifyEntityComponentEvent> sceneEvent = new WebInterface.SceneEvent<ModifyEntityComponentEvent>();
        sceneEvent.sceneId = scene.sceneData.id;
        sceneEvent.eventType = BuilderInWorldSettings.STATE_EVENT_NAME;
        sceneEvent.payload = modifyEntityComponentEvent;


        //Note (Adrian): We use Newtonsoft instead of JsonUtility because we need to deal with super classes, JsonUtility doesn't encode them
        string message = JsonConvert.SerializeObject(sceneEvent, Formatting.None, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        WebInterface.BuilderInWorldMessage(BuilderInWorldSettings.SCENE_EVENT_NAME, message);
        OnKernelUpdated?.Invoke();
    }

    public void AddEntityOnKernel(IDCLEntity entity, ParcelScene scene)
    {
        List<ComponentPayload> list = new List<ComponentPayload>();

        foreach (KeyValuePair<CLASS_ID_COMPONENT, IEntityComponent> keyValuePair in entity.components)
        {
            ComponentPayload componentPayLoad = new ComponentPayload();
            componentPayLoad.componentId = Convert.ToInt32(keyValuePair.Key);

            if (keyValuePair.Key == CLASS_ID_COMPONENT.TRANSFORM)
            {
                TransformComponent entityComponentModel = new TransformComponent();

                entityComponentModel.position = WorldStateUtils.ConvertUnityToScenePosition(entity.gameObject.transform.position, scene);
                entityComponentModel.rotation = new QuaternionRepresentation(entity.gameObject.transform.rotation);
                entityComponentModel.scale = entity.gameObject.transform.lossyScale;

                componentPayLoad.data = entityComponentModel;
            }
            else
            {
                componentPayLoad.data = keyValuePair.Value.GetModel();
            }

            list.Add(componentPayLoad);
        }

        foreach (KeyValuePair<Type, ISharedComponent> keyValuePairBaseDisposable in entity.sharedComponents)
        {
            ComponentPayload componentPayLoad = new ComponentPayload();

            componentPayLoad.componentId = keyValuePairBaseDisposable.Value.GetClassId();

            if (keyValuePairBaseDisposable.Value.GetClassId() == (int) CLASS_ID.NFT_SHAPE)
            {
                NFTComponent nftComponent = new NFTComponent();
                NFTShape.Model model = (NFTShape.Model) keyValuePairBaseDisposable.Value.GetModel();

                nftComponent.color = new ColorRepresentation(model.color);
                nftComponent.assetId = model.assetId;
                nftComponent.src = model.src;
                nftComponent.style = model.style;

                componentPayLoad.data = nftComponent;
            }
            else
            {
                componentPayLoad.data = keyValuePairBaseDisposable.Value.GetModel();
            }

            list.Add(componentPayLoad);
        }

        SendNewEntityToKernel(scene.sceneData.id, entity.entityId, list.ToArray());
    }

    public void EntityTransformReport(IDCLEntity entity, ParcelScene scene)
    {
        entitySingleComponentPayload.entityId = entity.entityId;
        entitySingleComponentPayload.componentId = (int) CLASS_ID_COMPONENT.TRANSFORM;

        entityTransformComponentModel.position = WorldStateUtils.ConvertUnityToScenePosition(entity.gameObject.transform.position, scene);
        entityTransformComponentModel.rotation = new QuaternionRepresentation(entity.gameObject.transform.rotation);
        entityTransformComponentModel.scale = entity.gameObject.transform.lossyScale;

        entitySingleComponentPayload.data = entityTransformComponentModel;

        modifyEntityComponentEvent.payload = entitySingleComponentPayload;

        WebInterface.SceneEvent<ModifyEntityComponentEvent> sceneEvent = new WebInterface.SceneEvent<ModifyEntityComponentEvent>();
        sceneEvent.sceneId = scene.sceneData.id;
        sceneEvent.eventType = BuilderInWorldSettings.STATE_EVENT_NAME;
        sceneEvent.payload = modifyEntityComponentEvent;

        //Note (Adrian): We use Newtonsoft instead of JsonUtility because we need to deal with super classes, JsonUtility doesn't encode them
        string message = JsonConvert.SerializeObject(sceneEvent);
        WebInterface.BuilderInWorldMessage(BuilderInWorldSettings.SCENE_EVENT_NAME, message);
    }

    public void RemoveEntityOnKernel(string entityId, ParcelScene scene)
    {
        RemoveEntityEvent removeEntityEvent = new RemoveEntityEvent();
        RemoveEntityPayload removeEntityPayLoad = new RemoveEntityPayload();
        removeEntityPayLoad.entityId = entityId;
        removeEntityEvent.payload = removeEntityPayLoad;

        WebInterface.SendSceneEvent(scene.sceneData.id, BuilderInWorldSettings.STATE_EVENT_NAME, removeEntityEvent);
        OnKernelUpdated?.Invoke();
    }

    public void StartKernelEditMode(ParcelScene scene) { WebInterface.ReportControlEvent(new WebInterface.StartStatefulMode(scene.sceneData.id)); }

    public void ExitKernelEditMode(ParcelScene scene) { WebInterface.ReportControlEvent(new WebInterface.StopStatefulMode(scene.sceneData.id)); }

    public void PublishScene(ParcelScene scene, string sceneName, string sceneDescription, string sceneScreenshot)
    {
        storeSceneState.payload.title = sceneName;
        storeSceneState.payload.description = sceneDescription;
        storeSceneState.payload.screenshot = sceneScreenshot;

        WebInterface.SendSceneEvent(scene.sceneData.id, BuilderInWorldSettings.STATE_EVENT_NAME, storeSceneState);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    void SendNewEntityToKernel(string sceneId, string entityId, ComponentPayload[] componentsPayload)
    {
        AddEntityEvent addEntityEvent = new AddEntityEvent();
        entityPayload.entityId = entityId;
        entityPayload.components = componentsPayload;

        addEntityEvent.payload = entityPayload;

        WebInterface.SceneEvent<AddEntityEvent> sceneEvent = new WebInterface.SceneEvent<AddEntityEvent>();
        sceneEvent.sceneId = sceneId;
        sceneEvent.eventType = BuilderInWorldSettings.STATE_EVENT_NAME;
        sceneEvent.payload = addEntityEvent;


        //Note(Adrian): We use Newtonsoft instead of JsonUtility because we need to deal with super classes, JsonUtility doesn't encode them
        string message = JsonConvert.SerializeObject(sceneEvent);
        WebInterface.BuilderInWorldMessage(BuilderInWorldSettings.SCENE_EVENT_NAME, message);
        OnKernelUpdated?.Invoke();
    }

    #endregion

}