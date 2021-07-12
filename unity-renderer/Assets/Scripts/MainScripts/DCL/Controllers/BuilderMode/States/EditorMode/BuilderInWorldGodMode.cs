using Builder.Gizmos;
using DCL;
using DCL.Configuration;
using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using System;
using System.Collections.Generic;
using DCL.Camera;
using UnityEngine;
using Environment = DCL.Environment;

public class BuilderInWorldGodMode : BuilderInWorldMode
{
    private float initialEagleCameraHeight = 10f;
    private float initialEagleCameraDistance = 10f;
    private float initialEagleCameraLookAtHeight = 0f;

    private float snapDragFactor = 5f;
    private FreeCameraMovement freeCameraController;

    private CameraController cameraController;
    private Transform lookAtT;
    private MouseCatcher mouseCatcher;
    private PlayerAvatarController avatarRenderer;
    private DCLBuilderGizmoManager gizmoManager;
    private IBIWOutlinerController outlinerController;

    private InputAction_Trigger focusOnSelectedEntitiesInputAction;
    private InputAction_Hold multiSelectionInputAction;

    private ParcelScene sceneToEdit;

    private bool isPlacingNewObject = false;
    private bool mouseMainBtnPressed = false;
    private bool mouseSecondaryBtnPressed = false;
    private bool isSquareMultiSelectionInputActive = false;
    private bool isMouseDragging = false;
    private bool changeSnapTemporaryButtonPressed = false;

    private bool wasGizmosActive = false;
    private bool isDraggingStarted = false;
    private bool canDragSelectedEntities = false;

    private bool activateCamera = true;
    private CameraMode.ModeId avatarCameraModeBeforeEditing;

    private Vector3 lastMousePosition;
    private Vector3 dragStartedPoint;
    private GameObject builderGO;

    public const float RAYCAST_MAX_DISTANCE = 10000f;

    public override void Init(BIWReferencesController biwReferencesController)
    {
        base.Init(biwReferencesController);

        lookAtT = new GameObject("BIWGodModeTransform").transform;
        maxDistanceToSelectEntitiesValue = biwReferencesController.godModeDynamicVariables.maxDistanceToSelectEntities;

        snapFactor = biwReferencesController.godModeDynamicVariables.snapFactor;
        snapRotationDegresFactor = biwReferencesController.godModeDynamicVariables.snapRotationDegresFactor;
        snapScaleFactor =  biwReferencesController.godModeDynamicVariables.snapScaleFactor;
        snapDistanceToActivateMovement =  biwReferencesController.godModeDynamicVariables.snapDistanceToActivateMovement;

        initialEagleCameraHeight = biwReferencesController.godModeDynamicVariables.initialEagleCameraHeight;
        initialEagleCameraDistance = biwReferencesController.godModeDynamicVariables.initialEagleCameraDistance;
        initialEagleCameraLookAtHeight = biwReferencesController.godModeDynamicVariables.initialEagleCameraHeight;

        snapDragFactor = biwReferencesController.godModeDynamicVariables.snapDragFactor;

        outlinerController = biwReferencesController.biwOutlinerController;

        if (HUDController.i.builderInWorldMainHud != null)
        {
            HUDController.i.builderInWorldMainHud.OnTranslateSelectedAction += TranslateMode;
            HUDController.i.builderInWorldMainHud.OnRotateSelectedAction += RotateMode;
            HUDController.i.builderInWorldMainHud.OnScaleSelectedAction += ScaleMode;
            HUDController.i.builderInWorldMainHud.OnSelectedObjectPositionChange += UpdateSelectionPosition;
            HUDController.i.builderInWorldMainHud.OnSelectedObjectRotationChange += UpdateSelectionRotation;
            HUDController.i.builderInWorldMainHud.OnSelectedObjectScaleChange += UpdateSelectionScale;
            HUDController.i.builderInWorldMainHud.OnResetCameraAction += ResetCamera;
            HUDController.i.builderInWorldMainHud.OnPublishAction += TakeSceneScreenshotForPublish;
        }

        if (InitialSceneReferences.i.cameraController.TryGetCameraStateByType<FreeCameraMovement>(out CameraStateBase cameraState))
            freeCameraController = (FreeCameraMovement) cameraState;
        mouseCatcher = InitialSceneReferences.i.mouseCatcher;
        avatarRenderer = InitialSceneReferences.i.playerAvatarController;
        cameraController = InitialSceneReferences.i.cameraController;

        DCLBuilderGizmoManager.OnGizmoTransformObjectEnd += OnGizmosTransformEnd;
        DCLBuilderGizmoManager.OnGizmoTransformObjectStart += OnGizmosTransformStart;

        BIWInputWrapper.OnMouseDown += OnInputMouseDown;
        BIWInputWrapper.OnMouseUp += OnInputMouseUp;
        BIWInputWrapper.OnMouseUpOnUI += OnInputMouseUpOnUi;
        BIWInputWrapper.OnMouseDrag += OnInputMouseDrag;

        focusOnSelectedEntitiesInputAction = biwReferencesController.inputsReferences.focusOnSelectedEntitiesInputAction;
        multiSelectionInputAction = biwReferencesController.inputsReferences.multiSelectionInputAction;

        focusOnSelectedEntitiesInputAction.OnTriggered += (o) => FocusOnSelectedEntitiesInput();

        multiSelectionInputAction.OnStarted += (o) => ChangeSnapTemporaryActivated();
        multiSelectionInputAction.OnFinished += (o) => ChangeSnapTemporaryDeactivated();

        builderGO = GameObject.Instantiate(biwReferencesController.projectReferences.godModeBuilderPrefab, biwReferencesController.projectReferences.godModeBuilderPrefab.transform.position, biwReferencesController.projectReferences.godModeBuilderPrefab.transform.rotation);
        gizmoManager = builderGO.GetComponentInChildren<DCLBuilderGizmoManager>();

        gizmoManager.OnChangeTransformValue += EntitiesTransfromByGizmos;
    }

    public override void Dispose()
    {
        base.Dispose();
        DCLBuilderGizmoManager.OnGizmoTransformObjectEnd -= OnGizmosTransformEnd;
        DCLBuilderGizmoManager.OnGizmoTransformObjectStart -= OnGizmosTransformStart;

        BIWInputWrapper.OnMouseDown -= OnInputMouseDown;
        BIWInputWrapper.OnMouseUp -= OnInputMouseUp;
        BIWInputWrapper.OnMouseUpOnUI -= OnInputMouseUpOnUi;
        BIWInputWrapper.OnMouseDrag -= OnInputMouseDrag;

        gizmoManager.OnChangeTransformValue -= EntitiesTransfromByGizmos;

        GameObject.Destroy(lookAtT.gameObject);
        GameObject.Destroy(builderGO);

        if (HUDController.i.builderInWorldMainHud == null)
            return;

        HUDController.i.builderInWorldMainHud.OnSelectedObjectPositionChange -= UpdateSelectionPosition;
        HUDController.i.builderInWorldMainHud.OnSelectedObjectRotationChange -= UpdateSelectionRotation;
        HUDController.i.builderInWorldMainHud.OnSelectedObjectScaleChange -= UpdateSelectionScale;
        HUDController.i.builderInWorldMainHud.OnTranslateSelectedAction -= TranslateMode;
        HUDController.i.builderInWorldMainHud.OnRotateSelectedAction -= RotateMode;
        HUDController.i.builderInWorldMainHud.OnScaleSelectedAction -= ScaleMode;
        HUDController.i.builderInWorldMainHud.OnResetCameraAction -= ResetCamera;
        HUDController.i.builderInWorldMainHud.OnPublishAction -= TakeSceneScreenshotForPublish;
    }

    public override void Update()
    {
        base.Update();
        if (isPlacingNewObject)
        {
            SetEditObjectAtMouse();
        }
        else if (isSquareMultiSelectionInputActive && isMouseDragging)
        {
            List<DCLBuilderInWorldEntity> allEntities = null;

            allEntities = biwEntityHandler.GetAllEntitiesFromCurrentScene();

            foreach (DCLBuilderInWorldEntity entity in allEntities)
            {
                if (!entity.rootEntity.meshRootGameObject || entity.rootEntity.meshesInfo.renderers.Length <= 0)
                    continue;

                if (BuilderInWorldUtils.IsWithInSelectionBounds(entity.rootEntity.meshesInfo.mergedBounds.center, lastMousePosition, Input.mousePosition))
                {
                    outlinerController.OutlineEntity(entity);
                }
                else
                {
                    outlinerController.CancelEntityOutline(entity);
                }
            }
        }
    }

    public override void OnGUI()
    {
        base.OnGUI();
        if (mouseMainBtnPressed && isSquareMultiSelectionInputActive)
        {
            var rect = BuilderInWorldUtils.GetScreenRect(lastMousePosition, Input.mousePosition);
            BuilderInWorldUtils.DrawScreenRect(rect, new Color(1f, 1f, 1f, 0.25f));
            BuilderInWorldUtils.DrawScreenRectBorder(rect, 1, Color.white);
        }
    }

    private void ChangeSnapTemporaryDeactivated()
    {
        if (changeSnapTemporaryButtonPressed)
            SetSnapActive(!isSnapActive);

        changeSnapTemporaryButtonPressed = false;
    }

    private void ChangeSnapTemporaryActivated()
    {
        if (selectedEntities.Count == 0)
            return;

        changeSnapTemporaryButtonPressed = true;

        SetSnapActive(!isSnapActive);
    }

    private void EntitiesTransfromByGizmos(Vector3 transformValue)
    {
        if (gizmoManager.GetSelectedGizmo() != BIWSettings.ROTATE_GIZMO_NAME)
            return;

        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            entity.AddRotation(transformValue);
        }
    }

    public void UpdateSelectionPosition(Vector3 newPosition)
    {
        if (selectedEntities.Count != 1)
            return;

        TransformActionStarted(selectedEntities[0].rootEntity, BIWSettings.TRANSLATE_GIZMO_NAME);
        editionGO.transform.position = WorldStateUtils.ConvertSceneToUnityPosition(newPosition, sceneToEdit);
        UpdateGizmosToSelectedEntities();
        TransformActionEnd(selectedEntities[0].rootEntity, BIWSettings.TRANSLATE_GIZMO_NAME);
        ActionFinish(BuildInWorldCompleteAction.ActionType.MOVE);
        biwEntityHandler.ReportTransform(true);
        biwSaveController.TryToSave();
    }

    public void UpdateSelectionRotation(Vector3 rotation)
    {
        if (selectedEntities.Count != 1)
            return;

        TransformActionStarted(selectedEntities[0].rootEntity, BIWSettings.ROTATE_GIZMO_NAME);
        selectedEntities[0].transform.rotation = Quaternion.Euler(rotation);
        TransformActionEnd(selectedEntities[0].rootEntity, BIWSettings.ROTATE_GIZMO_NAME);
        ActionFinish(BuildInWorldCompleteAction.ActionType.ROTATE);
        biwEntityHandler.ReportTransform(true);
        biwSaveController.TryToSave();
    }

    public void UpdateSelectionScale(Vector3 scale)
    {
        if (selectedEntities.Count != 1)
            return;

        var entityToUpdate = selectedEntities[0];

        TransformActionStarted(entityToUpdate.rootEntity, BIWSettings.SCALE_GIZMO_NAME);
        // Before change the scale, we unparent the entity to not to make it dependant on the editionGO and after that, reparent it
        entityToUpdate.transform.SetParent(null);
        entityToUpdate.transform.localScale = scale;
        editionGO.transform.localScale = Vector3.one;
        entityToUpdate.transform.SetParent(editionGO.transform);

        TransformActionEnd(entityToUpdate.rootEntity, BIWSettings.SCALE_GIZMO_NAME);
        ActionFinish(BuildInWorldCompleteAction.ActionType.SCALE);
        biwEntityHandler.ReportTransform(true);
        biwSaveController.TryToSave();
    }

    public void UpdateGizmosToSelectedEntities()
    {
        List<EditableEntity> editableEntities = new List<EditableEntity>();
        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            editableEntities.Add(entity);
        }

        gizmoManager.SetSelectedEntities(editionGO.transform, editableEntities);
    }

    public override void MouseClickDetected()
    {
        if (isPlacingNewObject)
        {
            biwEntityHandler.DeselectEntities();
            biwSaveController.TryToSave();
            return;
        }

        base.MouseClickDetected();
    }

    #region Mouse

    private void OnInputMouseDrag(int buttonId, Vector3 mousePosition, float axisX, float axisY)
    {
        if (Vector3.Distance(lastMousePosition, mousePosition) <= BIWSettings.MOUSE_THRESHOLD_FOR_DRAG && !isMouseDragging)
            return;

        isMouseDragging = true;
        if (buttonId != 0 ||
            selectedEntities.Count <= 0 ||
            BuilderInWorldUtils.IsPointerOverMaskElement(BIWSettings.GIZMOS_LAYER) ||
            isSquareMultiSelectionInputActive)
            return;

        if (!isDraggingStarted)
            StarDraggingSelectedEntities();

        if (canDragSelectedEntities)
        {
            Vector3 currentPoint = GetFloorPointAtMouse(mousePosition);
            Vector3 initialEntityPosition = editionGO.transform.position;

            if (isSnapActive)
            {
                currentPoint = GetPositionRoundedToSnapFactor(currentPoint);
                initialEntityPosition = GetPositionRoundedToSnapFactor(initialEntityPosition);
            }

            Vector3 move = currentPoint - dragStartedPoint;
            Vector3 destination = initialEntityPosition + move;
            editionGO.transform.position = destination;
            dragStartedPoint = currentPoint;
        }
    }

    private Vector3 GetPositionRoundedToSnapFactor(Vector3 position)
    {
        position = new Vector3(
            Mathf.Round(position.x / snapDragFactor) * snapDragFactor,
            position.y,
            Mathf.Round(position.z / snapDragFactor) * snapDragFactor);

        return position;
    }

    private void OnInputMouseUpOnUi(int buttonID, Vector3 position)
    {
        if (buttonID == 1)
        {
            mouseSecondaryBtnPressed = false;
            freeCameraController.StopDetectingMovement();
        }

        if (buttonID != 0)
            return;

        CheckEndBoundMultiselection(position);
        isMouseDragging = false;
    }

    private void OnInputMouseUp(int buttonID, Vector3 position)
    {
        if (buttonID == 1)
        {
            mouseSecondaryBtnPressed = false;
            if (CanCancelAction(position))
                biwEntityHandler.CancelSelection();

            freeCameraController.StopDetectingMovement();
        }

        if (buttonID != 0)
            return;

        EndDraggingSelectedEntities();

        CheckEndBoundMultiselection(position);

        outlinerController.SetOutlineCheckActive(true);

        isMouseDragging = false;
    }

    void OnInputMouseDown(int buttonID, Vector3 position)
    {
        lastMousePosition = position;

        if (buttonID == 1)
        {
            mouseSecondaryBtnPressed = true;
            freeCameraController.StartDectectingMovement();
        }

        if (buttonID != 0)
            return;

        dragStartedPoint = GetFloorPointAtMouse(position);

        if (isSnapActive)
        {
            dragStartedPoint.x = Mathf.Round(dragStartedPoint.x);
            dragStartedPoint.z = Mathf.Round(dragStartedPoint.z);
        }

        if (isPlacingNewObject)
            return;

        var entity = biwEntityHandler.GetEntityOnPointer();
        if ((entity == null
             || (entity != null && !entity.IsSelected))
            && !BuilderInWorldUtils.IsPointerOverMaskElement(BIWSettings.GIZMOS_LAYER))
        {
            isSquareMultiSelectionInputActive = true;
            outlinerController.SetOutlineCheckActive(false);
        }

        mouseMainBtnPressed = true;
        freeCameraController.SetCameraCanMove(false);
    }

    #endregion

    private void CheckEndBoundMultiselection(Vector3 position)
    {
        if (isSquareMultiSelectionInputActive && mouseMainBtnPressed )
        {
            if (Vector3.Distance(lastMousePosition, position) >= BIWSettings.MOUSE_THRESHOLD_FOR_DRAG)
                EndBoundMultiSelection();

            isSquareMultiSelectionInputActive = false;
            mouseMainBtnPressed = false;
        }
    }

    private bool CanCancelAction(Vector3 currentMousePosition) { return Vector3.Distance(lastMousePosition, currentMousePosition) <= BIWSettings.MOUSE_THRESHOLD_FOR_DRAG && !freeCameraController.HasBeenMovement; }

    private void StarDraggingSelectedEntities()
    {
        if (!biwEntityHandler.IsPointerInSelectedEntity() ||
            gizmoManager.HasAxisHover())
            return;

        if (gizmoManager.isActiveAndEnabled)
        {
            gizmoManager.HideGizmo();
            wasGizmosActive = true;
        }

        freeCameraController.SetCameraCanMove(false);
        isDraggingStarted = true;

        canDragSelectedEntities = true;
    }

    void EndDraggingSelectedEntities()
    {
        if (wasGizmosActive && !isPlacingNewObject)
        {
            gizmoManager.ShowGizmo();
            biwSaveController.TryToSave();
        }

        wasGizmosActive = false;

        freeCameraController.SetCameraCanMove(true);
        isDraggingStarted = false;

        canDragSelectedEntities = false;
    }

    private void EndBoundMultiSelection()
    {
        freeCameraController.SetCameraCanMove(true);
        List<DCLBuilderInWorldEntity> allEntities = null;

        allEntities = biwEntityHandler.GetAllEntitiesFromCurrentScene();

        List<DCLBuilderInWorldEntity> selectedInsideBoundsEntities = new List<DCLBuilderInWorldEntity>();
        int alreadySelectedEntities = 0;

        if (!isMultiSelectionActive)
            biwEntityHandler.DeselectEntities();

        foreach (DCLBuilderInWorldEntity entity in allEntities)
        {
            if (entity.rootEntity.meshRootGameObject && entity.rootEntity.meshesInfo.renderers.Length > 0)
            {
                if (BuilderInWorldUtils.IsWithInSelectionBounds(entity.rootEntity.meshesInfo.mergedBounds.center, lastMousePosition, Input.mousePosition)
                    && !entity.IsLocked)
                {
                    if (entity.IsSelected)
                        alreadySelectedEntities++;

                    biwEntityHandler.SelectEntity(entity);
                    selectedInsideBoundsEntities.Add(entity);
                }
            }
        }

        if (selectedInsideBoundsEntities.Count == alreadySelectedEntities && alreadySelectedEntities > 0)
        {
            foreach (DCLBuilderInWorldEntity entity in selectedInsideBoundsEntities)
            {
                biwEntityHandler.DeselectEntity(entity);
            }
        }

        outlinerController.CancelAllOutlines();
    }

    public override void Activate(ParcelScene scene)
    {
        base.Activate(scene);
        sceneToEdit = scene;

        if (activateCamera)
            ActivateCamera(scene);

        if (gizmoManager.GetSelectedGizmo() == DCL.Components.DCLGizmos.Gizmo.NONE)
            gizmoManager.SetGizmoType(BIWSettings.TRANSLATE_GIZMO_NAME);
        mouseCatcher.enabled = false;
        Environment.i.world.sceneController.IsolateScene(sceneToEdit);
        Utils.UnlockCursor();

        gizmoManager.HideGizmo();
        editionGO.transform.SetParent(null);
        avatarRenderer.SetAvatarVisibility(false);

        HUDController.i.builderInWorldMainHud?.ActivateGodModeUI();
    }

    public void ActivateCamera(ParcelScene parcelScene)
    {
        freeCameraController.gameObject.SetActive(true);
        SetLookAtObject(parcelScene);

        // NOTE(Adrian): Take into account that right now to get the relative scale of the gizmos, we set the gizmos in the player position and the camera
        Vector3 cameraPosition = GetInitialCameraPosition(parcelScene);
        freeCameraController.SetPosition(cameraPosition);
        freeCameraController.LookAt(lookAtT);
        freeCameraController.SetResetConfiguration(cameraPosition, lookAtT);

        if (cameraController.currentCameraState.cameraModeId != CameraMode.ModeId.BuildingToolGodMode)
            avatarCameraModeBeforeEditing = cameraController.currentCameraState.cameraModeId;

        cameraController.SetCameraMode(CameraMode.ModeId.BuildingToolGodMode);

        gizmoManager.InitializeGizmos(Camera.main, freeCameraController.transform);
        gizmoManager.ForceRelativeScaleRatio();
    }

    public override void OnDeleteEntity(DCLBuilderInWorldEntity entity)
    {
        base.OnDeleteEntity(entity);
        biwSaveController.TryToSave();

        if (selectedEntities.Count == 0)
            gizmoManager.HideGizmo();
    }

    public override void OnDeselectedEntities()
    {
        base.OnDeselectedEntities();
        gizmoManager.SetSelectedEntities(editionGO.transform, new List<EditableEntity>());
    }

    public override void Deactivate()
    {
        base.Deactivate();
        mouseCatcher.enabled = true;
        Utils.LockCursor();
        cameraController.SetCameraMode(avatarCameraModeBeforeEditing);

        Environment.i.world.sceneController.ReIntegrateIsolatedScene();

        gizmoManager.HideGizmo();
        RenderSettings.fog = true;
        avatarRenderer.SetAvatarVisibility(true);
    }

    public override void StartMultiSelection()
    {
        base.StartMultiSelection();

        snapGO.transform.SetParent(null);
        freeMovementGO.transform.SetParent(null);
    }

    public override void SetDuplicationOffset(float offset)
    {
        base.SetDuplicationOffset(offset);
        editionGO.transform.position += Vector3.right * offset;
    }

    public override void CreatedEntity(DCLBuilderInWorldEntity createdEntity)
    {
        base.CreatedEntity(createdEntity);

        if (!createdEntity.isFloor)
        {
            isPlacingNewObject = true;
            outlinerController.SetOutlineCheckActive(false);
        }

        gizmoManager.HideGizmo();
        if (createdEntity.isVoxel)
            createdEntity.rootEntity.gameObject.tag = BIWSettings.VOXEL_TAG;
    }

    public override Vector3 GetCreatedEntityPoint() { return GetFloorPointAtMouse(Input.mousePosition); }

    public override void SelectedEntity(DCLBuilderInWorldEntity selectedEntity)
    {
        base.SelectedEntity(selectedEntity);

        List<EditableEntity> editableEntities = new List<EditableEntity>();
        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            editableEntities.Add(entity);
        }

        gizmoManager.SetSelectedEntities(editionGO.transform, editableEntities);

        if (!isMultiSelectionActive && !selectedEntity.IsNew)
            TryLookAtEntity(selectedEntity.rootEntity);

        snapGO.transform.SetParent(null);

        UpdateActionsInteractable();
    }

    public override void EntityDeselected(DCLBuilderInWorldEntity entityDeselected)
    {
        base.EntityDeselected(entityDeselected);
        if (selectedEntities.Count <= 0)
        {
            gizmoManager.HideGizmo();
            UpdateActionsInteractable();
        }

        if (isPlacingNewObject)
            outlinerController.SetOutlineCheckActive(true);
        isPlacingNewObject = false;
    }

    public override void EntityDoubleClick(DCLBuilderInWorldEntity entity)
    {
        base.EntityDoubleClick(entity);
        if (!entity.IsLocked)
            LookAtEntity(entity.rootEntity);
    }

    private void UpdateActionsInteractable()
    {
        bool areInteratable = selectedEntities.Count > 0;
        HUDController.i.builderInWorldMainHud.SetActionsButtonsInteractable(areInteratable);
    }

    public override bool ShouldCancelUndoAction()
    {
        if (isPlacingNewObject)
        {
            biwEntityHandler.DestroyLastCreatedEntities();
            isPlacingNewObject = false;
            return true;
        }

        return false;
    }

    public override void SetSnapActive(bool isActive)
    {
        base.SetSnapActive(isActive);

        if (isSnapActive)
        {
            gizmoManager.SetSnapFactor(snapFactor, snapRotationDegresFactor, snapScaleFactor);
        }
        else
        {
            gizmoManager.SetSnapFactor(0, 0, 0);
        }
    }

    public void FocusOnSelectedEntitiesInput()
    {
        if (isModeActive)
        {
            FocusGameObject(selectedEntities);
            InputDone();
        }
    }

    public void TryLookAtEntity(IDCLEntity entity)
    {
        if (entity.meshRootGameObject == null
            || entity.meshesInfo == null
            || BuilderInWorldUtils.IsBoundInsideCamera(entity.meshesInfo.mergedBounds))
            return;

        LookAtEntity(entity);
    }

    public void LookAtEntity(IDCLEntity entity)
    {
        Vector3 pointToLook = entity.gameObject.transform.position;
        if (entity.meshesInfo.renderers.Length > 0)
        {
            Vector3 midPointFromEntityMesh = Vector3.zero;
            foreach (Renderer render in entity.renderers)
            {
                if (render == null)
                    continue;
                midPointFromEntityMesh += render.bounds.center;
            }

            midPointFromEntityMesh /= entity.renderers.Length;
            pointToLook = midPointFromEntityMesh;
        }

        freeCameraController.SmoothLookAt(pointToLook);
    }

    #region Gizmos

    public void TranslateMode() { GizmosMode( BIWSettings.TRANSLATE_GIZMO_NAME); }

    public void RotateMode() { GizmosMode( BIWSettings.ROTATE_GIZMO_NAME); }

    public void ScaleMode() { GizmosMode( BIWSettings.SCALE_GIZMO_NAME); }

    void GizmosMode(string gizmos)
    {
        if ((!isModeActive && isPlacingNewObject) || mouseSecondaryBtnPressed)
            return;
        if (gizmoManager.GetSelectedGizmo() != gizmos)
        {
            HUDController.i.builderInWorldMainHud?.SetGizmosActive(gizmos);
            gizmoManager.SetGizmoType(gizmos);
            if (selectedEntities.Count > 0 )
                gizmoManager.ShowGizmo();
        }
        //TODO: Free-Movement tool, This could be re-enabled in the future so let the code there 
        // else
        // {
        //     gizmoManager.HideGizmo(true);
        //     HUDController.i.builderInWorldMainHud?.SetGizmosActive(BIWSettings.EMPTY_GIZMO_NAME);
        // }
    }

    void OnGizmosTransformStart(string gizmoType)
    {
        outlinerController.SetOutlineCheckActive(false);
        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            TransformActionStarted(entity.rootEntity, gizmoType);
        }
    }

    void OnGizmosTransformEnd(string gizmoType)
    {
        outlinerController.SetOutlineCheckActive(true);
        foreach (DCLBuilderInWorldEntity entity in selectedEntities)
        {
            TransformActionEnd(entity.rootEntity, gizmoType);
        }

        switch (gizmoType)
        {
            case BIWSettings.TRANSLATE_GIZMO_NAME:

                ActionFinish(BuildInWorldCompleteAction.ActionType.MOVE);
                break;
            case BIWSettings.ROTATE_GIZMO_NAME:
                ActionFinish(BuildInWorldCompleteAction.ActionType.ROTATE);
                break;
            case BIWSettings.SCALE_GIZMO_NAME:
                ActionFinish(BuildInWorldCompleteAction.ActionType.SCALE);
                break;
        }

        biwSaveController.TryToSave();
    }

    #endregion

    public void FocusGameObject(List<DCLBuilderInWorldEntity> entitiesToFocus) { freeCameraController.FocusOnEntities(entitiesToFocus); }

    Vector3 GetInitialCameraPosition(ParcelScene parcelScene)
    {
        Vector3 middlePoint = BuilderInWorldUtils.CalculateUnityMiddlePoint(parcelScene);
        Vector3 direction = (parcelScene.transform.position - middlePoint).normalized;

        return parcelScene.transform.position
               + direction * initialEagleCameraDistance
               + Vector3.up * initialEagleCameraHeight;
    }

    void SetLookAtObject(ParcelScene parcelScene)
    {
        Vector3 middlePoint = BuilderInWorldUtils.CalculateUnityMiddlePoint(parcelScene);
        lookAtT.position = middlePoint + Vector3.up * initialEagleCameraLookAtHeight;
    }

    void SetEditObjectAtMouse()
    {
        RaycastHit hit;
        UnityEngine.Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, RAYCAST_MAX_DISTANCE, BIWSettings.GROUND_LAYER))
        {
            Vector3 destination = hit.point;
            if (isSnapActive)
            {
                destination.x = Mathf.Round(destination.x / snapDragFactor) * snapDragFactor;
                destination.z = Mathf.Round(destination.z / snapDragFactor) * snapDragFactor;
            }

            editionGO.transform.position = destination;

            if (selectedEntities.Count > 0 && selectedEntities[0].isNFT)
                editionGO.transform.position += Vector3.up * 2f;
        }
    }

    Vector3 GetFloorPointAtMouse(Vector3 mousePosition)
    {
        RaycastHit hit;
        UnityEngine.Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out hit, RAYCAST_MAX_DISTANCE, BIWSettings.GROUND_LAYER))
        {
            return hit.point;
        }

        return Vector3.zero;
    }

    private void ResetCamera() { freeCameraController.ResetCameraPosition(); }

    private void TakeSceneScreenshotForPublish()
    {
        biwEntityHandler.DeselectEntities();

        freeCameraController.TakeSceneScreenshot((sceneSnapshot) =>
        {
            HUDController.i.builderInWorldMainHud?.SetBuilderProjectScreenshot(sceneSnapshot);
        });
    }

    public void TakeSceneScreenshotForExit()
    {
        biwEntityHandler.DeselectEntities();

        freeCameraController.TakeSceneScreenshotFromResetPosition((sceneSnapshot) =>
        {
            HUDController.i.builderInWorldMainHud?.SetBuilderProjectScreenshot(sceneSnapshot);
        });
    }

    public void OpenNewProjectDetails()
    {
        biwEntityHandler.DeselectEntities();

        freeCameraController.TakeSceneScreenshot((sceneSnapshot) =>
        {
            HUDController.i.builderInWorldMainHud?.NewProjectStart(sceneSnapshot);
        });
    }
}