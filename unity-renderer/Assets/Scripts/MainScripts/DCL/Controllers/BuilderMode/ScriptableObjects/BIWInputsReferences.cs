using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuilderInWorldProjectReferences", menuName = "BuilderInWorld/InputReferences")]
public class BIWInputsReferences : ScriptableObject
{
    public InputAction_Trigger hideSelectedEntitiesAction;
    public InputAction_Trigger showAllEntitiesAction;
    public InputAction_Trigger toggleSnapModeInputAction;
    public InputAction_Trigger editModeChangeInputAction;
    public InputAction_Trigger toggleRedoActionInputAction;
    public InputAction_Trigger toggleUndoActionInputAction;
    public InputAction_Hold multiSelectionInputAction;

    [Header("First Person mode")]
    public InputAction_Hold firstPersonRotationHold;

    [Header("God mode")]
    public InputAction_Trigger focusOnSelectedEntitiesInputAction;
}