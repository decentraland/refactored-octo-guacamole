using Builder;
using System;
using System.Collections;
using System.Collections.Generic;
using DCL.Controllers;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IBIWInputWrapper
{
    public void StopInput();

    public void ResumeInput();
}

public class BIWInputWrapper : BIWController, IBIWInputWrapper
{
    private const float MS_CLICK_THRESHOLD = 500;
    private const float MOVEMENT_CLICK_THRESHOLD = 50;

    public static event Action<int, Vector3> OnMouseClick;
    public static event Action<int, Vector3> OnMouseClickOnUI;
    public static event Action<int, Vector3> OnMouseDown;
    public static event Action<int, Vector3> OnMouseUp;
    public static event Action<int, Vector3> OnMouseUpOnUI;

    public static event Action<float> OnMouseWheel;

    public static event OnMouseDragDelegate OnMouseDrag;
    public static event OnMouseDragDelegateRaw OnMouseDragRaw;

    public delegate void OnMouseDragDelegate(int buttonId, Vector3 position, float axisX, float axisY);
    public delegate void OnMouseDragDelegateRaw(int buttonId, Vector3 position, float axisX, float axisY);

    private float lastTimeMouseDown = 0;
    private Vector3 lastMousePosition;
    private bool canInputBeMade = true;
    private bool currentClickIsOnUi = false;
    private GameObject builderInputGameObject;

    public override void Init(BIWReferencesController referencesController)
    {
        base.Init(referencesController);
        builderInputGameObject = new GameObject("BuilderInput");
        builderInputGameObject.AddComponent<DCLBuilderInput>();

        DCLBuilderInput.OnMouseDrag += MouseDrag;
        DCLBuilderInput.OnMouseRawDrag += MouseRawDrag;
        DCLBuilderInput.OnMouseWheel += MouseWheel;
        DCLBuilderInput.OnMouseDown += MouseDown;
        DCLBuilderInput.OnMouseUp += MouseUp;
    }

    public override void Dispose()
    {
        base.Dispose();
        DCLBuilderInput.OnMouseDrag -= MouseDrag;
        DCLBuilderInput.OnMouseRawDrag -= MouseRawDrag;
        DCLBuilderInput.OnMouseWheel -= MouseWheel;
        DCLBuilderInput.OnMouseDown -= MouseDown;
        DCLBuilderInput.OnMouseUp -= MouseUp;

        GameObject.Destroy(builderInputGameObject);
    }

    public override void EnterEditMode(ParcelScene scene)
    {
        base.EnterEditMode(scene);
        builderInputGameObject.SetActive(true);
    }

    public override void ExitEditMode()
    {
        base.ExitEditMode();
        builderInputGameObject.SetActive(false);
    }

    public void StopInput() { canInputBeMade = false; }

    public void ResumeInput() { canInputBeMade = true; }

    private void MouseUp(int buttonId, Vector3 mousePosition)
    {
        if (currentClickIsOnUi)
        {
            OnMouseClickOnUI?.Invoke(buttonId, mousePosition);
            currentClickIsOnUi = false;
            return;
        }

        if (!canInputBeMade)
            return;

        if (!BuilderInWorldUtils.IsPointerOverUIElement())
        {
            OnMouseUp?.Invoke(buttonId, mousePosition);
            if (Vector3.Distance(mousePosition, lastMousePosition) >= MOVEMENT_CLICK_THRESHOLD)
                return;
            if (Time.unscaledTime >= lastTimeMouseDown + MS_CLICK_THRESHOLD / 1000)
                return;
            OnMouseClick?.Invoke(buttonId, mousePosition);
        }
        else
            OnMouseUpOnUI?.Invoke(buttonId, mousePosition);
    }

    private void MouseDown(int buttonId, Vector3 mousePosition)
    {
        lastTimeMouseDown = Time.unscaledTime;
        lastMousePosition = mousePosition;
        currentClickIsOnUi = BuilderInWorldUtils.IsPointerOverUIElement();

        if (!canInputBeMade)
            return;
        if (!currentClickIsOnUi)
            OnMouseDown?.Invoke(buttonId, mousePosition);
    }

    private void MouseWheel(float axisValue)
    {
        if (!canInputBeMade)
            return;
        if (!BuilderInWorldUtils.IsPointerOverUIElement())
            OnMouseWheel?.Invoke(axisValue);
    }

    private void MouseDrag(int buttonId, Vector3 mousePosition, float axisX, float axisY)
    {
        if (!CanDrag())
            return;

        OnMouseDrag?.Invoke(buttonId, mousePosition, axisX, axisY);
    }

    private void MouseRawDrag(int buttonId, Vector3 mousePosition, float axisX, float axisY)
    {
        if (!CanDrag())
            return;

        OnMouseDragRaw?.Invoke(buttonId, mousePosition, axisX, axisY);
    }

    private bool CanDrag()
    {
        if (!canInputBeMade ||
            currentClickIsOnUi ||
            BuilderInWorldUtils.IsPointerOverUIElement())
            return false;

        return true;
    }
}