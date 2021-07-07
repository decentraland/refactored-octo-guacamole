using DCL.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using DCL;
using UnityEngine;

public class BIWModeController : BIWController
{
    public enum EditModeState
    {
        Inactive = 0,
        FirstPerson = 1,
        GodMode = 2
    }

    [Header("Scene References")]
    public GameObject cursorGO;

    [Header("References")]
    public ActionController actionController;
    public BuilderInWorldEntityHandler builderInWorldEntityHandler;

    [Header("Build Modes")]
    public BuilderInWorldFirstPersonMode firstPersonMode;
    public BuilderInWorldGodMode editorMode;

    [SerializeField]
    internal InputAction_Trigger toggleSnapModeInputAction;

    public Action OnInputDone;
    public event Action<EditModeState, EditModeState> OnChangedEditModeState;

    private EditModeState currentEditModeState = EditModeState.Inactive;

    private BuilderInWorldMode currentActiveMode;

    private bool isSnapActive = true;

    private InputAction_Trigger.Triggered snapModeDelegate;
    private GameObject editionGO;
    private GameObject undoGO;

    private void Start()
    {
        snapModeDelegate = (action) => ChangeSnapMode();
        toggleSnapModeInputAction.OnTriggered += snapModeDelegate;
    }

    private void OnDestroy()
    {
        toggleSnapModeInputAction.OnTriggered -= snapModeDelegate;

        firstPersonMode.OnInputDone -= InputDone;
        editorMode.OnInputDone -= InputDone;

        firstPersonMode.OnActionGenerated -= actionController.AddAction;
        editorMode.OnActionGenerated -= actionController.AddAction;

        if (HUDController.i.builderInWorldMainHud != null)
        {
            HUDController.i.builderInWorldMainHud.OnChangeModeAction -= ChangeAdvanceMode;
            HUDController.i.builderInWorldMainHud.OnResetAction -= ResetScaleAndRotation;
        }
    }

    public override void Init()
    {
        base.Init();
        cursorGO = InitialSceneReferences.i.cursorCanvas;

        firstPersonMode.Init();
        editorMode.Init();

        firstPersonMode.OnInputDone += InputDone;
        editorMode.OnInputDone += InputDone;

        firstPersonMode.OnActionGenerated += actionController.AddAction;
        editorMode.OnActionGenerated += actionController.AddAction;

        if (HUDController.i.builderInWorldMainHud != null)
        {
            HUDController.i.builderInWorldMainHud.OnChangeModeAction += ChangeAdvanceMode;
            HUDController.i.builderInWorldMainHud.OnResetAction += ResetScaleAndRotation;
            HUDController.i.builderInWorldMainHud.OnChangeSnapModeAction += ChangeSnapMode;
        }
    }

    public void SetEditorGameObjects(GameObject editionGO, GameObject undoGO, GameObject snapGO, GameObject freeMovementGO)
    {
        this.editionGO = editionGO;
        this.undoGO = undoGO;

        editorMode.SetEditorReferences(editionGO, undoGO, snapGO, freeMovementGO, builderInWorldEntityHandler.GetSelectedEntityList());
        firstPersonMode.SetEditorReferences(editionGO, undoGO, snapGO, freeMovementGO, builderInWorldEntityHandler.GetSelectedEntityList());
    }

    public bool IsGodModeActive() { return currentEditModeState == EditModeState.GodMode; }

    public Vector3 GetCurrentEditionPosition()
    {
        if (editionGO != null)
            return editionGO.transform.position;
        else
            return Vector3.zero;
    }

    public void UndoEditionGOLastStep()
    {
        if (undoGO == null || editionGO == null)
            return;

        BuilderInWorldUtils.CopyGameObjectStatus(undoGO, editionGO, false, false);
    }

    public override void EnterEditMode(ParcelScene scene)
    {
        base.EnterEditMode(scene);
        if (currentActiveMode == null)
            SetBuildMode(EditModeState.GodMode);
    }

    public override void ExitEditMode()
    {
        base.ExitEditMode();
        SetBuildMode(EditModeState.Inactive);
    }

    public BuilderInWorldMode GetCurrentMode() => currentActiveMode;

    public EditModeState GetCurrentStateMode() => currentEditModeState;

    private void InputDone() { OnInputDone?.Invoke(); }

    public void StartMultiSelection() { currentActiveMode.StartMultiSelection(); }

    public void EndMultiSelection() { currentActiveMode.EndMultiSelection(); }

    public void ResetScaleAndRotation() { currentActiveMode.ResetScaleAndRotation(); }

    public void CheckInput() { currentActiveMode?.CheckInput(); }

    public void CheckInputSelectedEntities() { currentActiveMode.CheckInputSelectedEntities(); }

    public bool ShouldCancelUndoAction() { return currentActiveMode.ShouldCancelUndoAction(); }

    public void CreatedEntity(DCLBuilderInWorldEntity entity) { currentActiveMode?.CreatedEntity(entity); }

    public float GetMaxDistanceToSelectEntities() { return currentActiveMode.maxDistanceToSelectEntities; }

    public void EntityDoubleClick(DCLBuilderInWorldEntity entity) {  currentActiveMode.EntityDoubleClick(entity); }

    public Vector3 GetMousePosition() { return currentActiveMode.GetPointerPosition(); }

    public Vector3 GetModeCreationEntryPoint()
    {
        if (currentActiveMode != null)
            return currentActiveMode.GetCreatedEntityPoint();
        return Vector3.zero;
    }

    public virtual void MouseClickDetected() { currentActiveMode?.MouseClickDetected(); }

    private void ChangeSnapMode()
    {
        SetSnapActive(!isSnapActive);
        InputDone();

        if (isSnapActive)
            AudioScriptableObjects.enable.Play();
        else
            AudioScriptableObjects.disable.Play();
    }

    public void SetSnapActive(bool isActive)
    {
        isSnapActive = isActive;
        currentActiveMode.SetSnapActive(isActive);
    }

    public void ChangeAdvanceMode()
    {
        if (currentEditModeState == EditModeState.GodMode)
        {
            SetBuildMode(EditModeState.FirstPerson);
        }
        else
        {
            SetBuildMode(EditModeState.GodMode);
        }
        InputDone();
    }

    public void SetBuildMode(EditModeState state)
    {
        EditModeState previousState = currentEditModeState;

        if (currentActiveMode != null)
            currentActiveMode.Deactivate();

        currentActiveMode = null;
        switch (state)
        {
            case EditModeState.Inactive:
                break;
            case EditModeState.FirstPerson:
                currentActiveMode = firstPersonMode;

                if (cursorGO != null)
                    cursorGO.SetActive(true);
                break;
            case EditModeState.GodMode:
                if (cursorGO != null)
                    cursorGO.SetActive(false);
                currentActiveMode = editorMode;
                break;
        }

        currentEditModeState = state;

        if (currentActiveMode != null)
        {
            currentActiveMode.Activate(sceneToEdit);
            currentActiveMode.SetSnapActive(isSnapActive);
            builderInWorldEntityHandler.SetActiveMode(currentActiveMode);
        }

        OnChangedEditModeState?.Invoke(previousState, state);
    }
}