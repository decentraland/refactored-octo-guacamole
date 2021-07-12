using DCL;
using DCL.Components;
using DCL.Configuration;
using DCL.Controllers;
using DCL.Helpers;
using DCL.Interface;
using DCL.Models;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Environment = DCL.Environment;

public interface IBIWEntityHandler
{
    public event Action<DCLBuilderInWorldEntity> OnEntityDeselected;
    public event Action OnEntitySelected;
    public event Action<List<DCLBuilderInWorldEntity>> OnDeleteSelectedEntities;
    public event Action<DCLBuilderInWorldEntity> OnEntityDeleted;
    public DCLBuilderInWorldEntity GetConvertedEntity(string entityId);
    public void DeleteEntity(DCLBuilderInWorldEntity entityToDelete);
    public void DeleteEntity(string entityId);
    public void DeleteFloorEntities();
    public void DeleteSelectedEntities();
    public IDCLEntity CreateEntityFromJSON(string entityJson);
    public DCLBuilderInWorldEntity CreateEmptyEntity(ParcelScene parcelScene, Vector3 entryPoint, Vector3 editionGOPosition, bool notifyEntityList = true);
    public List<DCLBuilderInWorldEntity> GetAllEntitiesFromCurrentScene();
    public void DeselectEntities();
    public List<DCLBuilderInWorldEntity> GetSelectedEntityList();
    public DCLBuilderInWorldEntity GetEntityOnPointer();
    public bool IsAnyEntitySelected();
    public void SetActiveMode(BuilderInWorldMode buildMode);
    public void SetMultiSelectionActive(bool isActive);
    public void ChangeLockStateSelectedEntities();
    public void DeleteEntitiesOutsideSceneBoundaries();
    public bool AreAllSelectedEntitiesInsideBoundaries();
    public bool AreAllEntitiesInsideBoundaries();
    public void EntityListChanged();
    public void NotifyEntityIsCreated(IDCLEntity entity);
    public void SetEntityName(DCLBuilderInWorldEntity entityToApply, string newName, bool sendUpdateToKernel = true);
    public void EntityClicked(DCLBuilderInWorldEntity entityToSelect);
    public void ReportTransform(bool forceReport = false);
    public void CancelSelection();
    public bool IsPointerInSelectedEntity();
    public void DestroyLastCreatedEntities();
    public void Select(IDCLEntity entity);
    public bool SelectEntity(DCLBuilderInWorldEntity entityEditable, bool selectedFromCatalog = false);
    public void DeselectEntity(DCLBuilderInWorldEntity entity);
}

public class BIWEntityHandler : BIWController, IBIWEntityHandler
{
    private const float DUPLICATE_OFFSET = 2f;

    private IBIWOutlinerController outlinerController;

    private IBIWModeController biwModeController;
    private IBIActionController biActionController;
    private IBIWCreatorController biwCreatorController;

    private BuilderInWorldBridge builderInWorldBridge;
    private Material editMaterial;

    private InputAction_Trigger hideSelectedEntitiesAction;

    private InputAction_Trigger showAllEntitiesAction;

    private readonly Dictionary<string, DCLBuilderInWorldEntity> convertedEntities = new Dictionary<string, DCLBuilderInWorldEntity>();
    private readonly List<DCLBuilderInWorldEntity> selectedEntities = new List<DCLBuilderInWorldEntity>();

    private BuilderInWorldMode currentActiveMode;
    private bool isMultiSelectionActive = false;
    private bool isSecondayClickPressed = false;

    private float lastTransformReportTime;

    private List<string> entityNameList = new List<string>();

    private InputAction_Trigger.Triggered hideSelectedEntitiesDelegate;
    private InputAction_Trigger.Triggered showAllEntitiesDelegate;

    private BuildModeHUDController hudController;

    public event Action<DCLBuilderInWorldEntity> OnEntityDeselected;
    public event Action OnEntitySelected;
    public event Action<List<DCLBuilderInWorldEntity>> OnDeleteSelectedEntities;
    public event Action<DCLBuilderInWorldEntity> OnEntityDeleted;

    private DCLBuilderInWorldEntity lastClickedEntity;
    private float lastTimeEntityClicked;

    public override void Init(BIWReferencesController referencesController)
    {
        base.Init(referencesController);
        if (HUDController.i.builderInWorldMainHud != null)
        {
            hudController = HUDController.i.builderInWorldMainHud;
            hudController.OnEntityDelete += DeleteSingleEntity;
            hudController.OnDuplicateSelectedAction += DuplicateSelectedEntitiesInput;
            hudController.OnDeleteSelectedAction += DeleteSelectedEntitiesInput;
            hudController.OnEntityClick += ChangeEntitySelectionFromList;
            hudController.OnEntityLock += ChangeEntityLockStatus;
            hudController.OnEntityChangeVisibility += ChangeEntityVisibilityStatus;
            hudController.OnEntityRename += SetEntityName;
            hudController.OnEntitySmartItemComponentUpdate += UpdateSmartItemComponentInKernel;
        }


        BIWInputWrapper.OnMouseDown += OnInputMouseDown;
        BIWInputWrapper.OnMouseUp += OnInputMouseUp;

        DCL.Environment.i.world.sceneBoundsChecker.OnEntityBoundsCheckerStatusChanged += ChangeEntityBoundsCheckerStatus;

        builderInWorldBridge = InitialSceneReferences.i.builderInWorldBridge;

        outlinerController = referencesController.biwOutlinerController;

        biwModeController = referencesController.biwModeController;
        biActionController = referencesController.BiwBiActionController;
        biwCreatorController = referencesController.biwCreatorController;

        editMaterial = referencesController.projectReferences.editMaterial;

        hideSelectedEntitiesAction = referencesController.inputsReferences.hideSelectedEntitiesAction;
        showAllEntitiesAction = referencesController.inputsReferences.showAllEntitiesAction;

        hideSelectedEntitiesDelegate = (action) => ChangeShowStateSelectedEntities();
        showAllEntitiesDelegate = (action) => ShowAllEntities();

        hideSelectedEntitiesAction.OnTriggered += hideSelectedEntitiesDelegate;
        showAllEntitiesAction.OnTriggered += showAllEntitiesDelegate;

        biActionController.OnRedo += ReSelectEntities;
        biActionController.OnUndo += ReSelectEntities;
    }

    private void OnInputMouseDown(int buttonId, Vector3 mousePosition)
    {
        if (buttonId == 1)
            isSecondayClickPressed = true;
    }

    private void OnInputMouseUp(int buttonId, Vector3 mousePosition)
    {
        if (buttonId == 1)
            isSecondayClickPressed = false;
    }

    public override void Dispose()
    {
        DestroyCollidersForAllEntities();

        biActionController.OnRedo -= ReSelectEntities;
        biActionController.OnUndo -= ReSelectEntities;

        hideSelectedEntitiesAction.OnTriggered -= hideSelectedEntitiesDelegate;
        showAllEntitiesAction.OnTriggered -= showAllEntitiesDelegate;

        DCL.Environment.i.world.sceneBoundsChecker.OnEntityBoundsCheckerStatusChanged -= ChangeEntityBoundsCheckerStatus;

        BIWInputWrapper.OnMouseDown -= OnInputMouseDown;
        BIWInputWrapper.OnMouseUp -= OnInputMouseUp;

        if (hudController != null)
        {
            hudController.OnEntityDelete -= DeleteSingleEntity;
            hudController.OnDuplicateSelectedAction -= DuplicateSelectedEntitiesInput;
            hudController.OnDeleteSelectedAction -= DeleteSelectedEntitiesInput;
            hudController.OnEntityClick -= ChangeEntitySelectionFromList;
            hudController.OnEntityLock -= ChangeEntityLockStatus;
            hudController.OnEntityChangeVisibility -= ChangeEntityVisibilityStatus;
            hudController.OnEntityChangeVisibility -= ChangeEntityVisibilityStatus;
            hudController.OnEntityRename -= SetEntityName;
            hudController.OnEntitySmartItemComponentUpdate -= UpdateSmartItemComponentInKernel;
        }
    }

    public override void Update()
    {
        base.Update();

        if (selectedEntities.Count == 0)
            return;
        if ((DCLTime.realtimeSinceStartup - lastTransformReportTime) <= BIWSettings.ENTITY_POSITION_REPORTING_DELAY)
            return;

        ReportTransform();
    }

    public void ReportTransform(bool forceReport = false)
    {
        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            if (!entity.HasMovedSinceLastReport() &&
                !entity.HasScaledSinceLastReport() &&
                !entity.HasRotatedSinceLastReport() &&
                !forceReport)
                return;
            builderInWorldBridge.EntityTransformReport(entity.rootEntity, sceneToEdit);
            entity.PositionReported();
            entity.ScaleReported();
            entity.RotationReported();
        }

        lastTransformReportTime = DCLTime.realtimeSinceStartup;
    }

    public List<DCLBuilderInWorldEntity> GetSelectedEntityList() { return selectedEntities; }

    public bool IsAnyEntitySelected() { return selectedEntities.Count > 0; }

    public void SetActiveMode(BuilderInWorldMode buildMode)
    {
        currentActiveMode = buildMode;
        DeselectEntities();
    }

    public void SetMultiSelectionActive(bool isActive) { isMultiSelectionActive = isActive; }

    public override void EnterEditMode(ParcelScene scene)
    {
        base.EnterEditMode(scene);

        SetupAllEntities();
        EntityListChanged();
        CheckErrorOnEntities();
    }

    private void CheckErrorOnEntities()
    {
        foreach (DCLBuilderInWorldEntity entity in convertedEntities.Values)
        {
            entity.CheckErrors();
            if (entity.hasMissingCatalogItemError)
                biwCreatorController.CreateErrorOnEntity(entity);
        }
    }

    public bool IsPointerInSelectedEntity()
    {
        DCLBuilderInWorldEntity entityInPointer = GetEntityOnPointer();
        if (entityInPointer == null)
            return false;

        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            if (entityInPointer == entity)
                return true;
        }

        return false;
    }

    private void DeleteSelectedEntitiesInput()
    {
        if (selectedEntities.Count > 0)
            DeleteSelectedEntities();
    }

    private void DuplicateSelectedEntitiesInput()
    {
        if (selectedEntities.Count <= 0 || isSecondayClickPressed)
            return;

        DuplicateSelectedEntities();
    }

    public override void ExitEditMode()
    {
        base.ExitEditMode();
        DeselectEntities();

        foreach (DCLBuilderInWorldEntity entity in convertedEntities.Values)
        {
            entity.Dispose();
        }

        convertedEntities.Clear();
    }

    private void ChangeEntitySelectionFromList(DCLBuilderInWorldEntity entityToEdit)
    {
        if (!selectedEntities.Contains(entityToEdit))
            SelectFromList(entityToEdit);
        else
            DeselectEntity(entityToEdit);
    }

    private void SelectFromList(DCLBuilderInWorldEntity entityToEdit)
    {
        if (!isMultiSelectionActive)
            DeselectEntities();
        if (SelectEntity(entityToEdit))
        {
            if (!isMultiSelectionActive)
                outlinerController.OutlineEntity(entityToEdit);
            else
                outlinerController.OutlineEntities(selectedEntities);
        }
    }

    public void DeselectEntity(DCLBuilderInWorldEntity entity)
    {
        if (!selectedEntities.Contains(entity))
            return;

        entity.Deselect();

        outlinerController.CancelEntityOutline(entity);
        selectedEntities.Remove(entity);
        hudController?.UpdateEntitiesSelection(selectedEntities.Count);
        currentActiveMode?.EntityDeselected(entity);
        if (selectedEntities.Count <= 0 &&
            hudController != null)
            hudController.HideEntityInformation();

        OnEntityDeselected?.Invoke(entity);
    }

    public void DeselectEntities()
    {
        if (selectedEntities.Count <= 0)
            return;

        int amountToDeselect = selectedEntities.Count;
        for (int i = 0; i < amountToDeselect; i++)
        {
            DeselectEntity(selectedEntities[0]);
        }

        currentActiveMode?.OnDeselectedEntities();

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public DCLBuilderInWorldEntity GetEntityOnPointer()
    {
        Camera camera = Camera.main;

        if (camera == null)
            return null;

        RaycastHit hit;
        UnityEngine.Ray ray = camera.ScreenPointToRay(biwModeController.GetMousePosition());
        float distanceToSelect = biwModeController.GetMaxDistanceToSelectEntities();

        if (Physics.Raycast(ray, out hit, distanceToSelect, BIWSettings.COLLIDER_SELECTION_LAYER))
        {
            string entityID = hit.collider.gameObject.name;

            if (sceneToEdit.entities.ContainsKey(entityID))
            {
                return GetConvertedEntity(sceneToEdit.entities[entityID]);
            }
        }

        return null;
    }

    public void EntityClicked(DCLBuilderInWorldEntity entityToSelect)
    {
        if (entityToSelect != null)
        {
            if (!isMultiSelectionActive)
                DeselectEntities();

            if (!entityToSelect.IsLocked)
                ChangeEntitySelectStatus(entityToSelect);

            if (entityToSelect == lastClickedEntity && (lastTimeEntityClicked + BIWSettings.MOUSE_MS_DOUBLE_CLICK_THRESHOLD / 1000f) >= Time.realtimeSinceStartup )
                biwModeController.EntityDoubleClick(entityToSelect);

            lastClickedEntity = entityToSelect;
            lastTimeEntityClicked = Time.realtimeSinceStartup;
        }
        else if (!isMultiSelectionActive)
        {
            DeselectEntities();
        }
    }

    void ReSelectEntities()
    {
        List<DCLBuilderInWorldEntity> entitiesToReselect = new List<DCLBuilderInWorldEntity>();
        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            entitiesToReselect.Add(entity);
        }

        DeselectEntities();

        foreach (DCLBuilderInWorldEntity entity in entitiesToReselect)
        {
            SelectEntity(entity);
        }
    }

    void ChangeEntitySelectStatus(DCLBuilderInWorldEntity entityCliked)
    {
        if (entityCliked.IsSelected)
            DeselectEntity(entityCliked);
        else
            SelectEntity(entityCliked);
    }

    public void CancelSelection()
    {
        if (selectedEntities.Count == 0)
            return;

        DestroyLastCreatedEntities();
        DeselectEntities();
    }

    public void ChangeLockStateSelectedEntities()
    {
        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            entity.ToggleLockStatus();
        }

        DeselectEntities();
    }

    public void ShowAllEntities()
    {
        foreach (DCLBuilderInWorldEntity entity in convertedEntities.Values)
        {
            if (!entity.IsVisible)
                entity.ToggleShowStatus();
        }
    }

    public void ChangeShowStateSelectedEntities()
    {
        List<DCLBuilderInWorldEntity> entitiesToHide = new List<DCLBuilderInWorldEntity>(selectedEntities);

        foreach (DCLBuilderInWorldEntity entity in entitiesToHide)
        {
            if (entity.IsVisible && entity.IsSelected)
                DeselectEntity(entity);
            entity.ToggleShowStatus();
        }
    }

    public void Select(IDCLEntity entity)
    {
        DCLBuilderInWorldEntity entityEditable = GetConvertedEntity(entity);
        if (entityEditable == null)
            return;

        SelectEntity(entityEditable, true);
    }

    public bool SelectEntity(DCLBuilderInWorldEntity entityEditable, bool selectedFromCatalog = false)
    {
        if (entityEditable.IsLocked)
            return false;

        if (entityEditable.IsSelected)
            return false;

        entityEditable.Select();

        selectedEntities.Add(entityEditable);

        currentActiveMode?.SelectedEntity(entityEditable);

        if (HUDController.i.builderInWorldMainHud != null)
        {
            hudController.UpdateEntitiesSelection(selectedEntities.Count);
            hudController.ShowEntityInformation(selectedFromCatalog);
            hudController.EntityInformationSetEntity(entityEditable, sceneToEdit);
        }

        outlinerController.CancelAllOutlines();

        OnEntitySelected?.Invoke();

        return true;
    }

    public List<DCLBuilderInWorldEntity> GetAllVoxelsEntities()
    {
        List<DCLBuilderInWorldEntity> voxelEntities = new List<DCLBuilderInWorldEntity>();
        foreach (DCLBuilderInWorldEntity entity in convertedEntities.Values)
        {
            if (entity.rootEntity.scene == sceneToEdit && entity.isVoxel)
                voxelEntities.Add(entity);
        }

        return voxelEntities;
    }

    public List<DCLBuilderInWorldEntity> GetAllEntitiesFromCurrentScene()
    {
        List<DCLBuilderInWorldEntity> entities = new List<DCLBuilderInWorldEntity>();
        foreach (DCLBuilderInWorldEntity entity in convertedEntities.Values)
        {
            if (entity.rootEntity.scene == sceneToEdit)
                entities.Add(entity);
        }

        return entities;
    }

    public DCLBuilderInWorldEntity GetConvertedEntity(string entityId)
    {
        if (convertedEntities.ContainsKey(GetConvertedUniqueKeyForEntity(entityId)))
            return convertedEntities[GetConvertedUniqueKeyForEntity(entityId)];

        return null;
    }

    public DCLBuilderInWorldEntity GetConvertedEntity(IDCLEntity entity)
    {
        if (convertedEntities.ContainsKey(GetConvertedUniqueKeyForEntity(entity)))
            return convertedEntities[GetConvertedUniqueKeyForEntity(entity)];

        return null;
    }

    public void DuplicateSelectedEntities()
    {
        BuildInWorldCompleteAction buildAction = new BuildInWorldCompleteAction();
        buildAction.actionType = BuildInWorldCompleteAction.ActionType.CREATE;

        List<BuilderInWorldEntityAction> entityActionList = new List<BuilderInWorldEntityAction>();
        List<DCLBuilderInWorldEntity> entitiesToDuplicate = new List<DCLBuilderInWorldEntity>(selectedEntities);
        DeselectEntities();

        foreach (DCLBuilderInWorldEntity entityToDuplicate in entitiesToDuplicate)
        {
            if (entityToDuplicate.isNFT)
                continue;

            var entityDuplicated = DuplicateEntity(entityToDuplicate);
            BuilderInWorldEntityAction builderInWorldEntityAction = new BuilderInWorldEntityAction(entityDuplicated.rootEntity, entityDuplicated.rootEntity.entityId, BuilderInWorldUtils.ConvertEntityToJSON(entityDuplicated.rootEntity));
            entityActionList.Add(builderInWorldEntityAction);
            SelectEntity(entityDuplicated);
        }

        currentActiveMode?.SetDuplicationOffset(DUPLICATE_OFFSET);

        buildAction.CreateActionType(entityActionList, BuildInWorldCompleteAction.ActionType.CREATE);
        biActionController.AddAction(buildAction);
    }

    public DCLBuilderInWorldEntity DuplicateEntity(DCLBuilderInWorldEntity entityToDuplicate)
    {
        IDCLEntity entity = SceneUtils.DuplicateEntity(sceneToEdit, entityToDuplicate.rootEntity);
        //Note: If the entity contains the name component or DCLLockedOnEdit, we don't want to copy them 
        entity.RemoveSharedComponent(typeof(DCLName), false);
        entity.RemoveSharedComponent(typeof(DCLLockedOnEdit), false);

        BuilderInWorldUtils.CopyGameObjectStatus(entityToDuplicate.gameObject, entity.gameObject, false, false);
        DCLBuilderInWorldEntity convertedEntity = SetupEntityToEdit(entity);

        NotifyEntityIsCreated(entity);
        EntityListChanged();
        return convertedEntity;
    }

    public IDCLEntity CreateEntityFromJSON(string entityJson)
    {
        EntityData data = BuilderInWorldUtils.ConvertJSONToEntityData(entityJson);

        IDCLEntity newEntity = sceneToEdit.CreateEntity(data.entityId);


        if (data.transformComponent != null)
        {
            DCLTransform.model.position = data.transformComponent.position;
            DCLTransform.model.rotation = Quaternion.Euler(data.transformComponent.rotation);
            DCLTransform.model.scale = data.transformComponent.scale;
            sceneToEdit.EntityComponentCreateOrUpdateWithModel(newEntity.entityId, CLASS_ID_COMPONENT.TRANSFORM, DCLTransform.model);
        }

        foreach (ProtocolV2.GenericComponent component in data.components)
        {
            sceneToEdit.EntityComponentCreateOrUpdateWithModel(newEntity.entityId, (CLASS_ID_COMPONENT) component.componentId, component.data);
        }

        foreach (ProtocolV2.GenericComponent component in data.sharedComponents)
        {
            sceneToEdit.SharedComponentAttach(newEntity.entityId, component.classId);
        }

        if (data.nftComponent != null)
        {
            NFTShape nftShape = (NFTShape) sceneToEdit.SharedComponentCreate(data.nftComponent.id, Convert.ToInt32(CLASS_ID.NFT_SHAPE));
            nftShape.model = new NFTShape.Model();
            nftShape.model.color = data.nftComponent.color.ToColor();
            nftShape.model.src = data.nftComponent.src;
            nftShape.model.assetId = data.nftComponent.assetId;

            sceneToEdit.SharedComponentAttach(newEntity.entityId, nftShape.id);
        }

        var convertedEntity = SetupEntityToEdit(newEntity, true);

        if (convertedEntity.rootEntity.TryGetSharedComponent(CLASS_ID.GLTF_SHAPE, out var gltfComponent))
            gltfComponent.CallWhenReady(convertedEntity.ShapeLoadFinish);

        if (convertedEntity.rootEntity.TryGetSharedComponent(CLASS_ID.NFT_SHAPE, out var nftComponent))
            nftComponent.CallWhenReady(convertedEntity.ShapeLoadFinish);


        biwCreatorController.CreateLoadingObject(convertedEntity);
        EntityListChanged();

        return newEntity;
    }

    public DCLBuilderInWorldEntity CreateEmptyEntity(ParcelScene parcelScene, Vector3 entryPoint, Vector3 editionGOPosition, bool notifyEntityList = true)
    {
        IDCLEntity newEntity = parcelScene.CreateEntity(Guid.NewGuid().ToString());

        DCLTransform.model.position = WorldStateUtils.ConvertUnityToScenePosition(entryPoint, parcelScene);

        Vector3 pointToLookAt = Camera.main.transform.position;
        pointToLookAt.y = editionGOPosition.y;
        Quaternion lookOnLook = Quaternion.LookRotation(editionGOPosition - pointToLookAt);

        DCLTransform.model.rotation = lookOnLook;
        DCLTransform.model.scale = newEntity.gameObject.transform.lossyScale;

        parcelScene.EntityComponentCreateOrUpdateWithModel(newEntity.entityId, CLASS_ID_COMPONENT.TRANSFORM, DCLTransform.model);

        DCLBuilderInWorldEntity convertedEntity = SetupEntityToEdit(newEntity, true);
        hudController?.UpdateSceneLimitInfo();

        if (notifyEntityList)
            EntityListChanged();
        return convertedEntity;
    }

    private void SetupAllEntities()
    {
        foreach (IDCLEntity entity in sceneToEdit.entities.Values)
        {
            SetupEntityToEdit(entity);
        }
    }

    public void DestroyLastCreatedEntities()
    {
        List<DCLBuilderInWorldEntity> entitiesToRemove = new List<DCLBuilderInWorldEntity>();
        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            if (entity.IsSelected && entity.IsNew)
                entitiesToRemove.Add(entity);
        }

        if (entitiesToRemove.Count == 0)
            return;

        biwModeController.UndoEditionGOLastStep();

        foreach (DCLBuilderInWorldEntity entity in entitiesToRemove)
        {
            DeleteEntity(entity, false);
        }

        hudController.HideEntityInformation();
    }

    public void EntityListChanged()
    {
        if (HUDController.i.builderInWorldMainHud == null)
            return;
        hudController.SetEntityList(GetEntitiesInCurrentScene());
    }

    public int GetCurrentSceneEntityCount() { return GetEntitiesInCurrentScene().Count; }

    List<DCLBuilderInWorldEntity> GetEntitiesInCurrentScene()
    {
        List<DCLBuilderInWorldEntity> currentEntitiesInScene = new List<DCLBuilderInWorldEntity>();
        foreach (DCLBuilderInWorldEntity entity in convertedEntities.Values)
        {
            if (entity.rootEntity.scene == sceneToEdit)
                currentEntitiesInScene.Add(entity);
        }

        return currentEntitiesInScene;
    }

    DCLBuilderInWorldEntity SetupEntityToEdit(IDCLEntity entity, bool hasBeenCreated = false)
    {
        if (!convertedEntities.ContainsKey(GetConvertedUniqueKeyForEntity(entity)))
        {
            DCLBuilderInWorldEntity entityToEdit = entity.gameObject.AddComponent<DCLBuilderInWorldEntity>();
            entityToEdit.Init(entity, editMaterial);
            convertedEntities.Add(entityToEdit.entityUniqueId, entityToEdit);
            entity.OnRemoved += RemoveConvertedEntity;
            entityToEdit.IsNew = hasBeenCreated;

            string entityName = entityToEdit.GetDescriptiveName();
            var catalogItem = entityToEdit.GetCatalogItemAssociated();

            if ((string.IsNullOrEmpty(entityName) || entityNameList.Contains(entityName)) && catalogItem != null)
            {
                entityName = GetNewNameForEntity(catalogItem);
                SetEntityName(entityToEdit, entityName);
            }
            else if (!string.IsNullOrEmpty(entityName) && !entityNameList.Contains(entityName))
            {
                entityNameList.Add(entityName);
            }

            return entityToEdit;
        }
        else
        {
            return convertedEntities[GetConvertedUniqueKeyForEntity(entity)];
        }
    }

    private void ChangeEntityBoundsCheckerStatus(IDCLEntity entity, bool isInsideBoundaries)
    {
        var convertedEntity = GetConvertedEntity(entity);
        if (convertedEntity == null)
            return;

        convertedEntity.SetEntityBoundariesError(isInsideBoundaries);
    }

    public string GetNewNameForEntity(CatalogItem sceneObject) { return GetNewNameForEntity(sceneObject.name); }

    public string GetNewNameForEntity(string name)
    {
        int i = 1;
        if (!entityNameList.Contains(name))
            return name;

        string newName = name + " " + i;
        while (entityNameList.Contains(newName))
        {
            i++;
            newName = name + " " + i;
        }

        return newName;
    }

    public void DeleteFloorEntities()
    {
        List<DCLBuilderInWorldEntity> entitiesToDelete = new List<DCLBuilderInWorldEntity>();

        foreach (DCLBuilderInWorldEntity entity in convertedEntities.Values)
        {
            if (entity.isFloor)
            {
                entitiesToDelete.Add(entity);
            }
        }

        foreach (DCLBuilderInWorldEntity entity in entitiesToDelete)
            DeleteEntity(entity, false);
    }

    public void DeleteEntity(string entityId)
    {
        DCLBuilderInWorldEntity entity = convertedEntities[GetConvertedUniqueKeyForEntity(entityId)];
        DeleteEntity(entity, true);
    }

    public void DeleteEntity(DCLBuilderInWorldEntity entityToDelete) { DeleteEntity(entityToDelete, true); }

    public void DeleteEntity(DCLBuilderInWorldEntity entityToDelete, bool checkSelection)
    {
        if (entityToDelete.IsSelected && checkSelection)
            DeselectEntity(entityToDelete);

        if (selectedEntities.Contains(entityToDelete))
        {
            selectedEntities.Remove(entityToDelete);
            hudController?.UpdateEntitiesSelection(selectedEntities.Count);
        }

        string entityName = entityToDelete.GetDescriptiveName();

        if (entityNameList.Contains(entityName))
            entityNameList.Remove(entityName);

        RemoveConvertedEntity(entityToDelete.rootEntity);
        entityToDelete.rootEntity.OnRemoved -= RemoveConvertedEntity;
        entityToDelete.Delete();
        string idToRemove = entityToDelete.rootEntity.entityId;
        OnEntityDeleted?.Invoke(entityToDelete);
        biwCreatorController.RemoveLoadingObjectInmediate(entityToDelete.rootEntity.entityId);
        if (sceneToEdit.entities.ContainsKey(idToRemove))
            sceneToEdit.RemoveEntity(idToRemove, true);
        GameObject.Destroy(entityToDelete);
        hudController?.RefreshCatalogAssetPack();
        EntityListChanged();
        builderInWorldBridge?.RemoveEntityOnKernel(idToRemove, sceneToEdit);
    }

    public void DeleteSingleEntity(DCLBuilderInWorldEntity entityToDelete)
    {
        biActionController.CreateActionEntityDeleted(entityToDelete);
        DeleteEntity(entityToDelete, true);
    }

    public void DeleteSelectedEntities()
    {
        List<DCLBuilderInWorldEntity> entitiesToRemove = new List<DCLBuilderInWorldEntity>();

        for (int i = 0; i < selectedEntities.Count; i++)
        {
            entitiesToRemove.Add(selectedEntities[i]);
        }

        biActionController.CreateActionEntityDeleted(entitiesToRemove);

        DeselectEntities();

        foreach (DCLBuilderInWorldEntity entity in entitiesToRemove)
        {
            DeleteEntity(entity);
        }

        OnDeleteSelectedEntities?.Invoke(entitiesToRemove);
    }

    public void DeleteEntitiesOutsideSceneBoundaries()
    {
        List<DCLBuilderInWorldEntity> entitiesToRemove = new List<DCLBuilderInWorldEntity>();
        foreach (DCLBuilderInWorldEntity entity in convertedEntities.Values)
        {
            if (entity.rootEntity.scene == sceneToEdit)
            {
                if (!DCL.Environment.i.world.sceneBoundsChecker.IsEntityInsideSceneBoundaries(entity.rootEntity))
                {
                    entitiesToRemove.Add(entity);
                }
            }
        }

        foreach (DCLBuilderInWorldEntity entity in entitiesToRemove)
        {
            DeleteEntity(entity);
        }
    }

    private void DestroyCollidersForAllEntities()
    {
        foreach (DCLBuilderInWorldEntity entity in convertedEntities.Values)
        {
            entity.DestroyColliders();
        }
    }

    private void RemoveConvertedEntity(IDCLEntity entity) { convertedEntities.Remove(GetConvertedUniqueKeyForEntity(entity)); }

    public void NotifyEntityIsCreated(IDCLEntity entity) { builderInWorldBridge?.AddEntityOnKernel(entity, sceneToEdit); }

    public void UpdateSmartItemComponentInKernel(DCLBuilderInWorldEntity entityToUpdate) { builderInWorldBridge?.UpdateSmartItemComponent(entityToUpdate, sceneToEdit); }

    public void SetEntityName(DCLBuilderInWorldEntity entityToApply, string newName) { SetEntityName(entityToApply, newName, true); }

    public void SetEntityName(DCLBuilderInWorldEntity entityToApply, string newName, bool sendUpdateToKernel = true)
    {
        string currentName = entityToApply.GetDescriptiveName();

        if (currentName == newName)
            return;

        if (entityNameList.Contains(newName))
            newName = GetNewNameForEntity(newName);

        if (entityNameList.Contains(currentName))
            entityNameList.Remove(currentName);

        entityToApply.SetDescriptiveName(newName);
        entityNameList.Add(newName);

        if (sendUpdateToKernel)
            builderInWorldBridge?.ChangedEntityName(entityToApply, sceneToEdit);
    }

    private void ChangeEntityVisibilityStatus(DCLBuilderInWorldEntity entityToApply)
    {
        entityToApply.ToggleShowStatus();
        if (!entityToApply.IsVisible && selectedEntities.Contains(entityToApply))
            DeselectEntity(entityToApply);
    }

    private void ChangeEntityLockStatus(DCLBuilderInWorldEntity entityToApply)
    {
        entityToApply.ToggleLockStatus();
        if (entityToApply.IsLocked && selectedEntities.Contains(entityToApply))
            DeselectEntity(entityToApply);

        builderInWorldBridge.ChangeEntityLockStatus(entityToApply, sceneToEdit);
    }

    private string GetConvertedUniqueKeyForEntity(string entityID) { return sceneToEdit.sceneData.id + entityID; }

    private string GetConvertedUniqueKeyForEntity(IDCLEntity entity) { return entity.scene.sceneData.id + entity.entityId; }

    public bool AreAllSelectedEntitiesInsideBoundaries()
    {
        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            if (!DCL.Environment.i.world.sceneBoundsChecker.IsEntityInsideSceneBoundaries(entity.rootEntity))
            {
                return false;
            }
        }

        return true;
    }

    public bool AreAllEntitiesInsideBoundaries()
    {
        bool areAllIn = true;
        foreach (DCLBuilderInWorldEntity entity in convertedEntities.Values)
        {
            if (!DCL.Environment.i.world.sceneBoundsChecker.IsEntityInsideSceneBoundaries(entity.rootEntity))
            {
                areAllIn = false;
                break;
            }
        }

        return areAllIn;
    }
}