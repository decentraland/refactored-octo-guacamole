using DCL.Controllers;
using DCL.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuilderInWorldFirstPersonMode : BuilderInWorldMode
{
    private float scaleSpeed = 0.25f;
    private float rotationSpeed = 0.5f;
    private float distanceFromCameraForNewEntitties = 5;

    private InputAction_Hold rotationHold;

    private Quaternion initialRotation;

    private float currentYRotationAdded;

    private bool snapObjectAlreadyMoved = false, shouldRotate = false;
    private Transform originalParentGOEdit;

    private InputAction_Hold.Started rotationHoldStartDelegate;
    private InputAction_Hold.Finished rotationHoldFinishedDelegate;

    public override void Init(BIWReferencesController biwReferencesController)
    {
        base.Init(biwReferencesController);
        maxDistanceToSelectEntitiesValue = biwReferencesController.firstPersonDynamicVariables.maxDistanceToSelectEntities;

        snapFactor = biwReferencesController.firstPersonDynamicVariables.snapFactor;
        snapRotationDegresFactor = biwReferencesController.firstPersonDynamicVariables.snapRotationDegresFactor;
        snapScaleFactor =  biwReferencesController.firstPersonDynamicVariables.snapScaleFactor;
        snapDistanceToActivateMovement =  biwReferencesController.firstPersonDynamicVariables.snapDistanceToActivateMovement;

        scaleSpeed =  biwReferencesController.firstPersonDynamicVariables.scaleSpeed;
        rotationSpeed =  biwReferencesController.firstPersonDynamicVariables.rotationSpeed;
        distanceFromCameraForNewEntitties =  biwReferencesController.firstPersonDynamicVariables.distanceFromCameraForNewEntitties;

        rotationHold = biwReferencesController.inputsReferences.firstPersonRotationHold;

        rotationHoldStartDelegate = (action) => { shouldRotate = true; };
        rotationHoldFinishedDelegate = (action) => { shouldRotate = false; };

        rotationHold.OnStarted += rotationHoldStartDelegate;
        rotationHold.OnFinished += rotationHoldFinishedDelegate;

        BIWInputWrapper.OnMouseClick += OnMouseClick;
    }

    public override void Dispose()
    {
        base.Dispose();
        rotationHold.OnStarted -= rotationHoldStartDelegate;
        rotationHold.OnFinished -= rotationHoldFinishedDelegate;

        BIWInputWrapper.OnMouseClick -= OnMouseClick;
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        if (selectedEntities.Count == 0 || isMultiSelectionActive)
            return;

        if (isSnapActive)
        {
            if (snapObjectAlreadyMoved)
            {
                Vector3 objectPosition = snapGO.transform.position;
                Vector3 eulerRotation =  snapGO.transform.rotation.eulerAngles;

                float currentSnapFactor = snapFactor;

                objectPosition.x = Mathf.RoundToInt(objectPosition.x / currentSnapFactor) * currentSnapFactor;
                objectPosition.y = Mathf.RoundToInt(objectPosition.y / currentSnapFactor) * currentSnapFactor;
                objectPosition.z = Mathf.RoundToInt(objectPosition.z / currentSnapFactor) * currentSnapFactor;
                eulerRotation.y = snapRotationDegresFactor * Mathf.FloorToInt((eulerRotation.y % snapRotationDegresFactor));

                Quaternion destinationRotation = Quaternion.AngleAxis(currentYRotationAdded, Vector3.up);
                editionGO.transform.rotation = initialRotation * destinationRotation;
                editionGO.transform.position = objectPosition;
            }
            else if (Vector3.Distance(snapGO.transform.position, editionGO.transform.position) >= snapDistanceToActivateMovement)
            {
                BuilderInWorldUtils.CopyGameObjectStatus(editionGO, snapGO, false);
                snapObjectAlreadyMoved = true;
                SetEditObjectParent();
            }
        }
        else
        {
            Vector3 pointToLookAt = Camera.main.transform.position;
            pointToLookAt.y = editionGO.transform.position.y;
            Quaternion lookOnLook = Quaternion.LookRotation(editionGO.transform.position - pointToLookAt);
            freeMovementGO.transform.rotation = lookOnLook;
        }
    }

    public override Vector3 GetPointerPosition() { return new Vector3(Screen.width / 2, Screen.height / 2, 0); }

    private void OnMouseClick(int buttonId, Vector3 mouseposition)
    {
        if (!isModeActive)
            return;
        if (buttonId != 1)
            return;
        if (selectedEntities.Count <= 0)
            return;

        UndoSelection();
    }

    public override bool ShouldCancelUndoAction()
    {
        if (biwEntityHandler.GetSelectedEntityList().Count >= 1)
        {
            UndoSelection();
            return true;
        }
        return false;
    }

    private void UndoSelection()
    {
        BuilderInWorldUtils.CopyGameObjectStatus(undoGO, editionGO, false, false);
        biwEntityHandler.DeselectEntities();
    }

    public override void SetDuplicationOffset(float offset)
    {
        base.SetDuplicationOffset(offset);
        if (isSnapActive)
            editionGO.transform.position += Vector3.right * offset;
    }

    public override void ResetScaleAndRotation()
    {
        base.ResetScaleAndRotation();
        currentYRotationAdded = 0;

        Quaternion zeroAnglesQuaternion = Quaternion.Euler(Vector3.zero);
        initialRotation = zeroAnglesQuaternion;
    }

    public override void Activate(ParcelScene scene)
    {
        base.Activate(scene);
        SetEditObjectParent();
        freeMovementGO.transform.SetParent(Camera.main.transform);
        if (HUDController.i.builderInWorldMainHud != null)
        {
            HUDController.i.builderInWorldMainHud.ActivateFirstPersonModeUI();
            HUDController.i.builderInWorldMainHud.SetVisibilityOfCatalog(false);
        }
    }

    public override void StartMultiSelection()
    {
        base.StartMultiSelection();
        originalParentGOEdit = editionGO.transform.parent;

        SetEditObjectParent();
        snapGO.transform.SetParent(null);
        freeMovementGO.transform.SetParent(null);
    }

    public override void EndMultiSelection()
    {
        base.EndMultiSelection();
        SetEditObjectParent();

        snapGO.transform.SetParent(Camera.main.transform);
        freeMovementGO.transform.SetParent(Camera.main.transform);

        SetObjectIfSnapOrNot();
    }

    public override void SelectedEntity(DCLBuilderInWorldEntity selectedEntity)
    {
        base.SelectedEntity(selectedEntity);

        initialRotation = editionGO.transform.rotation;

        SetObjectIfSnapOrNot();

        currentYRotationAdded = 0;
        BuilderInWorldUtils.CopyGameObjectStatus(editionGO, snapGO, false);
    }

    public override void CreatedEntity(DCLBuilderInWorldEntity createdEntity)
    {
        base.CreatedEntity(createdEntity);
        Utils.LockCursor();
    }

    public override void SetSnapActive(bool isActive)
    {
        base.SetSnapActive(isActive);
        if (isSnapActive)
        {
            snapObjectAlreadyMoved = false;
            snapGO.transform.SetParent(Camera.main.transform);
        }
        SetObjectIfSnapOrNot();
    }

    public override void CheckInputSelectedEntities()
    {
        base.CheckInputSelectedEntities();
        if (selectedEntities.Count == 0)
            return;

        if (isModeActive && shouldRotate)
        {
            if (isSnapActive)
            {
                RotateSelection(snapRotationDegresFactor);
                InputDone();
            }
            else
            {
                RotateSelection(rotationSpeed);
            }
        }

        if (Input.mouseScrollDelta.y > 0.5f)
        {
            if (isSnapActive)
            {
                ScaleSelection(snapScaleFactor);
                InputDone();
            }
            else
            {
                ScaleSelection(scaleSpeed);
            }
        }
        else if (Input.mouseScrollDelta.y < -0.5f)
        {
            if (isSnapActive)
            {
                ScaleSelection(-snapScaleFactor);
                InputDone();
            }
            else
            {
                ScaleSelection(-scaleSpeed);
            }
        }
    }

    public override Vector3 GetCreatedEntityPoint() { return Camera.main.transform.position + Camera.main.transform.forward * distanceFromCameraForNewEntitties; }

    void SetObjectIfSnapOrNot()
    {
        if (isMultiSelectionActive)
            return;

        if (!isSnapActive)
        {
            editionGO.transform.SetParent(null);
            freeMovementGO.transform.position = editionGO.transform.position;
            freeMovementGO.transform.rotation = editionGO.transform.rotation;
            freeMovementGO.transform.localScale = editionGO.transform.localScale;

            editionGO.transform.SetParent(null);

            Vector3 pointToLookAt = Camera.main.transform.position;
            pointToLookAt.y = editionGO.transform.position.y;
            Quaternion lookOnLook = Quaternion.LookRotation(editionGO.transform.position - pointToLookAt);

            freeMovementGO.transform.rotation = lookOnLook;
            editionGO.transform.SetParent(freeMovementGO.transform, true);
        }
        else
        {
            editionGO.transform.SetParent(null);
        }
    }

    private void SetEditObjectParent()
    {
        Transform parentToAsign = null;
        bool worldPositionStays = false;
        if (!isMultiSelectionActive)
        {
            if (isSnapActive)
            {
                if (snapObjectAlreadyMoved)
                    parentToAsign = Camera.main.transform;
            }
            else
            {
                worldPositionStays = true;
                if (freeMovementGO != null)
                    parentToAsign = freeMovementGO.transform;
            }

        }
        else
        {
            if (!isSnapActive)
                parentToAsign = originalParentGOEdit;

            worldPositionStays = true;
        }

        if (editionGO != null)
            editionGO.transform.SetParent(parentToAsign, worldPositionStays);
    }

    void RotateSelection(float angleToRotate)
    {
        currentYRotationAdded += angleToRotate;
        editionGO.transform.Rotate(Vector3.up, angleToRotate);
        snapGO.transform.Rotate(Vector3.up, angleToRotate);
        snapGO.transform.rotation = Quaternion.Euler(BuilderInWorldUtils.SnapFilterEulerAngles(snapGO.transform.rotation.eulerAngles, snapRotationDegresFactor));
    }

    void ScaleSelection(float scaleFactor)
    {
        editionGO.transform.localScale += Vector3.one * scaleFactor;
        snapGO.transform.localScale += Vector3.one * scaleFactor;
    }
}