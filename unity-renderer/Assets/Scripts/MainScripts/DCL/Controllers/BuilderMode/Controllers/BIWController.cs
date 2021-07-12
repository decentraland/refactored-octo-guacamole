using DCL.Controllers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BIWController
{
    protected ParcelScene sceneToEdit;

    protected bool isEditModeActive = false;

    public virtual void Init(BIWReferencesController referencesController) { isEditModeActive = false; }

    public virtual void EnterEditMode(ParcelScene scene)
    {
        this.sceneToEdit = scene;
        isEditModeActive = true;
    }

    public virtual void ExitEditMode()
    {
        isEditModeActive = false;
        sceneToEdit = null;
    }

    public virtual void OnGUI() { }

    public virtual void LateUpdate() { }

    public virtual void Update()
    {
        if (!isEditModeActive)
            return;
    }

    public virtual void Dispose() { }
}