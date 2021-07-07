using DCL.Controllers;
using DCL.Models;
using System;
using System.Collections.Generic;
using DCL.Configuration;
using UnityEngine;

public class BuilderInWorldMode : MonoBehaviour
{
    [Header("Design variables")]
    public float maxDistanceToSelectEntities = 50;

    [Header("Snap variables")]
    public float snapFactor = 1f;

    public float snapRotationDegresFactor = 15f;
    public float snapScaleFactor = 0.5f;

    public float snapDistanceToActivateMovement = 10f;

    [Header("Prefab references")]
    public BuilderInWorldEntityHandler builderInWorldEntityHandler;
    public BIWSaveController biwSaveController;
    public ActionController actionController;

    public event Action OnInputDone;
    public event Action<BuildInWorldCompleteAction> OnActionGenerated;

    protected GameObject editionGO, undoGO, snapGO, freeMovementGO;

    protected bool isSnapActive = false, isMultiSelectionActive = false, isModeActive = false;
    protected List<DCLBuilderInWorldEntity> selectedEntities;

    protected bool isNewObjectPlaced = false;

    protected List<BuilderInWorldEntityAction> actionList = new List<BuilderInWorldEntityAction>();

    public virtual void Init()
    {
        gameObject.SetActive(false);
        builderInWorldEntityHandler.OnEntityDeleted += OnDeleteEntity;
    }

    public virtual void SetEditorReferences(GameObject goToEdit, GameObject undoGO, GameObject snapGO, GameObject freeMovementGO, List<DCLBuilderInWorldEntity> selectedEntities)
    {
        editionGO = goToEdit;
        this.undoGO = undoGO;
        this.snapGO = snapGO;
        this.freeMovementGO = freeMovementGO;

        this.selectedEntities = selectedEntities;
    }

    private void OnDestroy() { builderInWorldEntityHandler.OnEntityDeleted -= OnDeleteEntity; }

    public virtual void Activate(ParcelScene scene)
    {
        gameObject.SetActive(true);
        isModeActive = true;
    }

    public virtual void Deactivate()
    {
        gameObject.SetActive(false);
        isModeActive = false;
        builderInWorldEntityHandler.DeselectEntities();
    }

    public virtual void SetSnapActive(bool isActive)
    {
        if (isActive && !isSnapActive)
            AudioScriptableObjects.enable.Play();
        else if (!isActive && isSnapActive)
            AudioScriptableObjects.disable.Play();

        isSnapActive = isActive;
        HUDController.i.builderInWorldMainHud?.SetSnapModeActive(isSnapActive);
    }

    public virtual void StartMultiSelection() { isMultiSelectionActive = true; }

    public virtual Vector3 GetPointerPosition() { return Input.mousePosition; }

    public virtual void EndMultiSelection() { isMultiSelectionActive = false; }

    public virtual bool ShouldCancelUndoAction() { return false; }

    public virtual void SetDuplicationOffset(float offset) { }

    public virtual void EntityDoubleClick(DCLBuilderInWorldEntity entity) { }

    public virtual void SelectedEntity(DCLBuilderInWorldEntity selectedEntity)
    {
        CenterGameObjectToEdit();

        BuilderInWorldUtils.CopyGameObjectStatus(editionGO, undoGO, false, false);
    }

    public virtual void CenterGameObjectToEdit()
    {
        if (selectedEntities.Count > 0)
        {
            foreach (DCLBuilderInWorldEntity entity in selectedEntities)
            {
                entity.rootEntity.gameObject.transform.SetParent(null);
            }

            editionGO.transform.position = GetCenterPointOfSelectedObjects();
            editionGO.transform.rotation = Quaternion.Euler(0, 0, 0);
            editionGO.transform.localScale = Vector3.one;
            foreach (DCLBuilderInWorldEntity entity in selectedEntities)
            {
                entity.rootEntity.gameObject.transform.SetParent(editionGO.transform);
            }
        }
    }

    public virtual void MouseClickDetected()
    {
        DCLBuilderInWorldEntity entityToSelect = builderInWorldEntityHandler.GetEntityOnPointer();
        if (entityToSelect != null)
        {
            builderInWorldEntityHandler.EntityClicked(entityToSelect);
        }
        else if (!isMultiSelectionActive)
        {
            builderInWorldEntityHandler.DeselectEntities();
        }
    }

    public virtual void CreatedEntity(DCLBuilderInWorldEntity createdEntity) { isNewObjectPlaced = true; }

    public virtual void EntityDeselected(DCLBuilderInWorldEntity entityDeselected)
    {
        CenterGameObjectToEdit();

        if (isNewObjectPlaced)
        {
            actionController.CreateActionEntityCreated(entityDeselected.rootEntity);
        }

        isNewObjectPlaced = false;
    }

    public virtual void OnDeleteEntity(DCLBuilderInWorldEntity entity) { }

    public virtual void OnDeselectedEntities() { builderInWorldEntityHandler.ReportTransform(true); }

    public virtual void CheckInput() { }

    public virtual void CheckInputSelectedEntities() { }

    public virtual void InputDone() { OnInputDone?.Invoke(); }

    public virtual void ResetScaleAndRotation()
    {
        editionGO.transform.localScale = Vector3.one;
        snapGO.transform.localScale = Vector3.one;
        freeMovementGO.transform.localScale = Vector3.one;

        Quaternion zeroAnglesQuaternion = Quaternion.Euler(Vector3.zero);

        snapGO.transform.rotation = zeroAnglesQuaternion;
        freeMovementGO.transform.rotation = zeroAnglesQuaternion;
        editionGO.transform.rotation = zeroAnglesQuaternion;

        foreach (DCLBuilderInWorldEntity decentralandEntityToEdit in selectedEntities)
        {
            decentralandEntityToEdit.ResetTransfrom();
        }

        CenterGameObjectToEdit();
    }

    public virtual Vector3 GetCreatedEntityPoint() { return Vector3.zero; }

    protected Vector3 GetCenterPointOfSelectedObjects()
    {
        float totalX = 0f;
        float totalY = 0f;
        float totalZ = 0f;
        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            totalX += entity.rootEntity.gameObject.transform.position.x;
            totalY += entity.rootEntity.gameObject.transform.position.y;
            totalZ += entity.rootEntity.gameObject.transform.position.z;
        }

        float centerX = totalX / selectedEntities.Count;
        float centerY = totalY / selectedEntities.Count;
        float centerZ = totalZ / selectedEntities.Count;
        return new Vector3(centerX, centerY, centerZ);
    }

    protected void TransformActionStarted(IDCLEntity entity, string type)
    {
        BuilderInWorldEntityAction buildModeEntityAction = new BuilderInWorldEntityAction(entity);
        switch (type)
        {
            case BuilderInWorldSettings.TRANSLATE_GIZMO_NAME:
                buildModeEntityAction.oldValue = entity.gameObject.transform.position;
                break;
            case BuilderInWorldSettings.ROTATE_GIZMO_NAME:
                buildModeEntityAction.oldValue = entity.gameObject.transform.rotation.eulerAngles;
                break;
            case BuilderInWorldSettings.SCALE_GIZMO_NAME:
                buildModeEntityAction.oldValue = entity.gameObject.transform.lossyScale;
                break;
        }

        actionList.Add(buildModeEntityAction);
    }

    protected void TransformActionEnd(IDCLEntity entity, string type)
    {
        List<BuilderInWorldEntityAction> removeList = new List<BuilderInWorldEntityAction>();
        foreach (BuilderInWorldEntityAction entityAction in actionList)
        {
            if (entityAction.entityId != entity.entityId)
                continue;

            switch (type)
            {
                case "MOVE":

                    entityAction.newValue = entity.gameObject.transform.position;
                    if (Vector3.Distance((Vector3) entityAction.oldValue, (Vector3) entityAction.newValue) <= 0.09f)
                        removeList.Add(entityAction);
                    break;
                case "ROTATE":

                    entityAction.newValue = entity.gameObject.transform.rotation.eulerAngles;
                    if (Vector3.Distance((Vector3) entityAction.oldValue, (Vector3) entityAction.newValue) <= 0.09f)
                        removeList.Add(entityAction);
                    break;
                case "SCALE":
                    entityAction.newValue = entity.gameObject.transform.lossyScale;
                    if (Vector3.Distance((Vector3) entityAction.oldValue, (Vector3) entityAction.newValue) <= 0.09f)
                        removeList.Add(entityAction);
                    break;
            }
        }

        foreach (BuilderInWorldEntityAction entityAction in removeList)
        {
            actionList.Remove(entityAction);
        }
    }

    protected void ActionFinish(BuildInWorldCompleteAction.ActionType type)
    {
        if (actionList.Count > 0 && selectedEntities.Count > 0)
        {
            BuildInWorldCompleteAction buildModeAction = new BuildInWorldCompleteAction();

            buildModeAction.actionType = type;
            buildModeAction.CreateActionType(actionList, type);
            OnActionGenerated?.Invoke(buildModeAction);

            actionList = new List<BuilderInWorldEntityAction>();
        }
    }
}