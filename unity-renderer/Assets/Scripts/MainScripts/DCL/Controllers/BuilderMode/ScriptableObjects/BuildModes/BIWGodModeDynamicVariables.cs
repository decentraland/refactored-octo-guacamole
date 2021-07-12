using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GodModeDynamicVariable", menuName = "BuilderInWorld/DynamicVariables/GodMode")]
public class BIWGodModeDynamicVariables : ScriptableObject
{
    public float maxDistanceToSelectEntities = 50;

    [Header("Snap variables")]
    public float snapFactor = 1f;

    public float snapRotationDegresFactor = 15f;
    public float snapScaleFactor = 0.5f;

    public float snapDistanceToActivateMovement = 10f;

    [Header("Editor Design")]
    public float initialEagleCameraHeight = 10f;
    public float initialEagleCameraDistance = 10f;
    public float initialEagleCameraLookAtHeight = 0f;

    public LayerMask layerToStopClick;
    public float snapDragFactor = 5f;

}