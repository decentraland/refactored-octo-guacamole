using System;
using DCL.Configuration;
using DCL.Controllers;
using DCL.Models;
using System.Collections;
using System.Collections.Generic;
using DCL.Camera;
using UnityEngine;

public class VoxelController
{
    [Header("References")]
    public VoxelPrefab voxelPrefab;

    public BuilderInWorldController buildModeController;
    public BIWOutlinerController outlinerController;
    public BIWEntityHandler biwEntityHandler;
    public FreeCameraMovement freeCameraMovement;
    public BIWActionController biwActionController;

    public LayerMask groundLayer;

    DCLBuilderInWorldEntity lastVoxelCreated;

    GameObject editionGO;
    bool mousePressed = false, isVoxelModelActivated = false, isCreatingMultipleVoxels = false;
    Vector3Int lastVoxelPositionPressed;
    Vector3 lastMousePosition;
    Dictionary<Vector3Int, VoxelPrefab> createdVoxels = new Dictionary<Vector3Int, VoxelPrefab>();
    ParcelScene currentScene;

    const float RAYCAST_MAX_DISTANCE = 10000f;
    const float VOXEL_BOUND_ERROR = 0.05f;

    private void Start()
    {
        BIWInputWrapper.OnMouseDown += MouseDown;
        BIWInputWrapper.OnMouseUp += MouseUp;
    }

    private void OnDestroy()
    {
        BIWInputWrapper.OnMouseDown -= MouseDown;
        BIWInputWrapper.OnMouseUp -= MouseUp;
    }

    private void Update()
    {
        if (!mousePressed || !isVoxelModelActivated || !isCreatingMultipleVoxels)
            return;

        Vector3Int currentPosition = Vector3Int.zero;
        VoxelEntityHit voxelHit = buildModeController.GetCloserUnselectedVoxelEntityOnPointer();

        if (voxelHit != null && voxelHit.entityHitted.tag == BIWSettings.VOXEL_TAG && !voxelHit.entityHitted.IsSelected)
        {
            Vector3Int position = ConverPositionToVoxelPosition(voxelHit.entityHitted.rootEntity.gameObject.transform.position);
            position += voxelHit.hitVector;

            currentPosition = position;
            FillVoxels(lastVoxelPositionPressed, currentPosition);
        }
        else
        {
            RaycastHit hit;
            UnityEngine.Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, RAYCAST_MAX_DISTANCE, groundLayer))
            {
                currentPosition = ConverPositionToVoxelPosition(hit.point);
                FillVoxels(lastVoxelPositionPressed, currentPosition);
            }
        }
    }

    public void SetSceneToEdit(ParcelScene scene) { currentScene = scene; }

    public void SetEditObjectLikeVoxel()
    {
        if (mousePressed || !isVoxelModelActivated)
            return;

        VoxelEntityHit voxelHit = buildModeController.GetCloserUnselectedVoxelEntityOnPointer();

        if (voxelHit != null && voxelHit.entityHitted.IsSelected)
            return;

        if (voxelHit != null && voxelHit.entityHitted.tag == BIWSettings.VOXEL_TAG)
        {
            Vector3 position = ConverPositionToVoxelPosition(voxelHit.entityHitted.rootEntity.gameObject.transform.position);
            position += voxelHit.hitVector;
            editionGO.transform.position = position;
        }
        else
        {
            RaycastHit hit;
            UnityEngine.Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, RAYCAST_MAX_DISTANCE, groundLayer))
            {
                Vector3 position = hit.point;
                editionGO.transform.position = ConverPositionToVoxelPosition(hit.point);
            }
        }
    }

    public void SetEditionGO(GameObject editionGO) { this.editionGO = editionGO; }

    public bool IsActive() { return isVoxelModelActivated; }

    public void SetActiveMode(bool isActive) { isVoxelModelActivated = isActive; }

    public void EndMultiVoxelSelection()
    {
        List<DCLBuilderInWorldEntity> voxelEntities = biwEntityHandler.GetAllVoxelsEntities();

        foreach (DCLBuilderInWorldEntity voxelEntity in voxelEntities)
        {
            if (BuilderInWorldUtils.IsWithInSelectionBounds(voxelEntity.gameObject.transform, lastMousePosition, Input.mousePosition))
            {
                biwEntityHandler.SelectEntity(voxelEntity);
            }
        }

        outlinerController.SetOutlineCheckActive(true);
        outlinerController.CancelAllOutlines();
    }

    void FillVoxels(Vector3Int firstPosition, Vector3Int lastPosition)
    {
        int xDifference = Mathf.Abs(firstPosition.x - lastPosition.x);
        int yDifference = Mathf.Abs(firstPosition.y - lastPosition.y);
        int zDifference = Mathf.Abs(firstPosition.z - lastPosition.z);

        List<Vector3Int> mustContainVoxelList = new List<Vector3Int>();
        List<DCLBuilderInWorldEntity> voxelEntities = biwEntityHandler.GetAllVoxelsEntities();
        List<DCLBuilderInWorldEntity> allEntities = biwEntityHandler.GetAllEntitiesFromCurrentScene();

        for (int x = 0; x <= xDifference; x++)
        {
            int contX = x;
            if (firstPosition.x > lastPosition.x)
                contX = -contX;

            for (int y = 0; y <= yDifference; y++)
            {
                int contY = y;
                if (firstPosition.y > lastPosition.y)
                    contY = -contY;

                for (int z = 0; z <= zDifference; z++)
                {
                    int contZ = z;
                    if (firstPosition.z > lastPosition.z)
                        contZ = -contZ;

                    Vector3Int positionOfVoxel = new Vector3Int(firstPosition.x + contX, firstPosition.y + contY, firstPosition.z + contZ);
                    if (positionOfVoxel == firstPosition)
                        continue;
                    if (ExistVoxelAtPosition(positionOfVoxel, voxelEntities))
                        continue;
                    CreateVoxel(positionOfVoxel);
                    mustContainVoxelList.Add(positionOfVoxel);
                }
            }
        }


        List<Vector3Int> voxelToRemove = new List<Vector3Int>();
        foreach (Vector3Int position in createdVoxels.Keys)
        {
            if (!mustContainVoxelList.Contains(position))
                voxelToRemove.Add(position);
        }

        foreach (Vector3Int vector in voxelToRemove)
        {
            GameObject.Destroy(createdVoxels[vector].gameObject);
            createdVoxels.Remove(vector);
        }

        foreach (VoxelPrefab keyValuePair in createdVoxels.Values)
        {
            if (IsVoxelAtValidPoint(keyValuePair, allEntities))
                keyValuePair.SetAvailability(true);
            else
                keyValuePair.SetAvailability(false);
        }
    }

    bool ExistVoxelAtPosition(Vector3Int position, List<DCLBuilderInWorldEntity> voxelEntities)
    {
        foreach (DCLBuilderInWorldEntity voxelEntity in voxelEntities)
        {
            if (position == ConverPositionToVoxelPosition(voxelEntity.transform.position))
                return true;
        }

        return false;
    }

    VoxelPrefab CreateVoxel(Vector3Int position)
    {
        if (!createdVoxels.ContainsKey(position))
        {
            VoxelPrefab go = GameObject.Instantiate(voxelPrefab.gameObject, position, lastVoxelCreated.rootEntity.gameObject.transform.rotation).GetComponent<VoxelPrefab>();
            createdVoxels.Add(position, go);
            return go;
        }

        return null;
    }

    private void MouseUp(int buttonID, Vector3 position)
    {
        if (!mousePressed || buttonID != 0)
            return;

        if (isCreatingMultipleVoxels)
        {
            lastVoxelCreated.transform.SetParent(null);
            bool canVoxelsBeCreated = true;

            foreach (VoxelPrefab voxel in createdVoxels.Values)
            {
                if (!voxel.IsAvailable())
                {
                    canVoxelsBeCreated = false;
                    break;
                }
            }

            BuildInWorldCompleteAction buildAction = new BuildInWorldCompleteAction();
            buildAction.actionType = BuildInWorldCompleteAction.ActionType.CREATE;

            List<BuilderInWorldEntityAction> entityActionList = new List<BuilderInWorldEntityAction>();

            foreach (Vector3Int voxelPosition in createdVoxels.Keys)
            {
                if (canVoxelsBeCreated)
                {
                    IDCLEntity entity = biwEntityHandler.DuplicateEntity(lastVoxelCreated).rootEntity;
                    entity.gameObject.tag = BIWSettings.VOXEL_TAG;
                    entity.gameObject.transform.position = voxelPosition;

                    BuilderInWorldEntityAction builderInWorldEntityAction = new BuilderInWorldEntityAction(entity, entity.entityId, BuilderInWorldUtils.ConvertEntityToJSON(entity));
                    entityActionList.Add(builderInWorldEntityAction);
                }

                GameObject.Destroy(createdVoxels[voxelPosition].gameObject);
            }

            if (!canVoxelsBeCreated)
            {
                biwEntityHandler.DeleteEntity(lastVoxelCreated);
            }
            else
            {
                buildAction.CreateActionType(entityActionList, BuildInWorldCompleteAction.ActionType.CREATE);
                biwActionController.AddAction(buildAction);
            }

            createdVoxels.Clear();
            biwEntityHandler.DeselectEntities();

            lastVoxelCreated = null;
            isCreatingMultipleVoxels = false;

            mousePressed = false;
            freeCameraMovement.SetCameraCanMove(true);
        }
    }

    void MouseDown(int buttonID, Vector3 position)
    {
        if (buttonID != 0 || !isVoxelModelActivated || lastVoxelCreated == null)
            return;

        lastVoxelPositionPressed = ConverPositionToVoxelPosition(lastVoxelCreated.transform.position);
        mousePressed = true;
        freeCameraMovement.SetCameraCanMove(false);
        isCreatingMultipleVoxels = true;
    }

    public void SetVoxelSelected(DCLBuilderInWorldEntity decentralandEntityToEdit)
    {
        lastVoxelCreated = decentralandEntityToEdit;
        lastVoxelCreated.transform.localPosition = Vector3.zero;
    }

    public Vector3Int ConverPositionToVoxelPosition(Vector3 rawPosition)
    {
        Vector3Int position = Vector3Int.zero;
        position.x = Mathf.CeilToInt(rawPosition.x);
        position.y = Mathf.CeilToInt(rawPosition.y);
        position.z = Mathf.CeilToInt(rawPosition.z);
        return position;
    }

    bool IsVoxelAtValidPoint(VoxelPrefab voxelPrefab, List<DCLBuilderInWorldEntity> entitiesToCheck)
    {
        if (!currentScene.IsInsideSceneBoundaries(voxelPrefab.meshRenderer.bounds))
            return false;
        Bounds bounds = voxelPrefab.meshRenderer.bounds;
        bounds.size -= Vector3.one * VOXEL_BOUND_ERROR;
        foreach (DCLBuilderInWorldEntity entity in entitiesToCheck)
        {
            if (entity.rootEntity.meshesInfo == null || entity.rootEntity.meshesInfo.renderers == null)
                continue;
            if (bounds.Intersects(entity.rootEntity.meshesInfo.mergedBounds))
                return false;
        }

        bounds.size += Vector3.one * VOXEL_BOUND_ERROR;
        return true;
    }
}