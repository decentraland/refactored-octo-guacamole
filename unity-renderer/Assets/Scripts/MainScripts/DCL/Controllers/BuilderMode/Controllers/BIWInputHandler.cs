using DCL.Controllers;
using DCL.Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IBIWInputHandler { }

public class BIWInputHandler : BIWController, IBIWInputHandler
{
    private const float MS_BETWEEN_INPUT_INTERACTION = 200;

    private IBIActionController biActionController;
    private IBIWModeController biwModeController;
    private IBIWInputWrapper builderInputWrapper;
    private IBIWOutlinerController outlinerController;
    private IBIWEntityHandler biwEntityHandler;

    private InputAction_Trigger toggleRedoActionInputAction;
    private InputAction_Trigger toggleUndoActionInputAction;
    private InputAction_Hold multiSelectionInputAction;

    private InputAction_Hold.Started multiSelectionStartDelegate;
    private InputAction_Hold.Finished multiSelectionFinishedDelegate;

    private InputAction_Trigger.Triggered redoDelegate;
    private InputAction_Trigger.Triggered undoDelegate;

    private bool isMultiSelectionActive = false;

    private float nexTimeToReceiveInput;

    public override void Init(BIWReferencesController biwReferencesController)
    {
        base.Init(biwReferencesController);

        biActionController = biwReferencesController.BiwBiActionController;
        biwModeController = biwReferencesController.biwModeController;
        builderInputWrapper = biwReferencesController.biwInputWrapper;
        outlinerController = biwReferencesController.biwOutlinerController;
        biwEntityHandler = biwReferencesController.biwEntityHandler;

        toggleRedoActionInputAction = biwReferencesController.inputsReferences.toggleRedoActionInputAction;
        toggleUndoActionInputAction = biwReferencesController.inputsReferences.toggleUndoActionInputAction;
        multiSelectionInputAction = biwReferencesController.inputsReferences.multiSelectionInputAction;

        if (HUDController.i.builderInWorldMainHud != null)
        {
            HUDController.i.builderInWorldMainHud.OnStopInput += StopInput;
            HUDController.i.builderInWorldMainHud.OnResumeInput += ResumeInput;
        }

        redoDelegate = (action) => RedoAction();
        undoDelegate = (action) => UndoAction();

        toggleRedoActionInputAction.OnTriggered += redoDelegate;
        toggleUndoActionInputAction.OnTriggered += undoDelegate;

        multiSelectionStartDelegate = (action) => StartMultiSelection();
        multiSelectionFinishedDelegate = (action) => EndMultiSelection();

        BIWInputWrapper.OnMouseClick += MouseClick;
        BIWInputWrapper.OnMouseClickOnUI += MouseClickOnUI;
        biwModeController.OnInputDone += InputDone;

        multiSelectionInputAction.OnStarted += multiSelectionStartDelegate;
        multiSelectionInputAction.OnFinished += multiSelectionFinishedDelegate;
    }

    public override void Dispose()
    {
        base.Dispose();

        toggleRedoActionInputAction.OnTriggered -= redoDelegate;
        toggleUndoActionInputAction.OnTriggered -= undoDelegate;

        multiSelectionInputAction.OnStarted -= multiSelectionStartDelegate;
        multiSelectionInputAction.OnFinished -= multiSelectionFinishedDelegate;

        BIWInputWrapper.OnMouseClick -= MouseClick;
        BIWInputWrapper.OnMouseClickOnUI -= MouseClickOnUI;
        biwModeController.OnInputDone -= InputDone;
        if (HUDController.i.builderInWorldMainHud != null)
        {
            HUDController.i.builderInWorldMainHud.OnStopInput -= StopInput;
            HUDController.i.builderInWorldMainHud.OnResumeInput -= ResumeInput;
        }
    }

    public override void Update()
    {
        base.Update();

        if (Time.timeSinceLevelLoad < nexTimeToReceiveInput)
            return;

        if (Utils.isCursorLocked || biwModeController.IsGodModeActive())
            CheckEditModeInput();
        biwModeController.CheckInput();
    }

    private void CheckEditModeInput()
    {
        outlinerController.CheckOutline();

        if (biwEntityHandler.IsAnyEntitySelected())
        {
            biwModeController.CheckInputSelectedEntities();
        }
    }

    private void StartMultiSelection()
    {
        isMultiSelectionActive = true;
        biwEntityHandler.SetMultiSelectionActive(isMultiSelectionActive);
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        biwModeController.StartMultiSelection();
    }

    private void EndMultiSelection()
    {
        isMultiSelectionActive = false;
        biwEntityHandler.SetMultiSelectionActive(isMultiSelectionActive);
        biwModeController.EndMultiSelection();
        outlinerController.CancelUnselectedOutlines();
    }

    private void MouseClick(int buttonID, Vector3 position)
    {
        if (!isEditModeActive)
            return;

        if (Time.timeSinceLevelLoad < nexTimeToReceiveInput)
            return;

        if (!BuilderInWorldUtils.IsPointerOverUIElement())
            HUDController.i.builderInWorldMainHud.HideExtraBtns();

        if (Utils.isCursorLocked || biwModeController.IsGodModeActive())
        {
            if (buttonID == 0)
            {
                MouseClickDetected();
                InputDone();
                return;
            }
            outlinerController.CheckOutline();
        }
    }

    private void MouseClickOnUI(int buttonID, Vector3 position) { HUDController.i.builderInWorldMainHud.HideExtraBtns(); }

    public bool IsMultiSelectionActive() => isMultiSelectionActive;

    private void RedoAction()
    {
        biActionController.TryToRedoAction();
        InputDone();
    }

    private void UndoAction()
    {
        InputDone();

        if (biwModeController.ShouldCancelUndoAction())
            return;

        biActionController.TryToUndoAction();
    }

    private void MouseClickDetected() { biwModeController.MouseClickDetected(); }

    private void InputDone() { nexTimeToReceiveInput = Time.timeSinceLevelLoad + MS_BETWEEN_INPUT_INTERACTION / 1000; }

    private void StopInput() { builderInputWrapper.StopInput(); }

    private void ResumeInput() { builderInputWrapper.ResumeInput(); }
}