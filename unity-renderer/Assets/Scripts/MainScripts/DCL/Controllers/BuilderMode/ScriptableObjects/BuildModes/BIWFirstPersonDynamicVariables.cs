using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FirstPersonModeDynamicVariable", menuName = "BuilderInWorld/DynamicVariables/FirstPerson")]
public class BIWFirstPersonDynamicVariables : ScriptableObject
{
    public float maxDistanceToSelectEntities = 50;

    [Header("Snap variables")]
    public float snapFactor = 1f;

    public float snapRotationDegresFactor = 15f;
    public float snapScaleFactor = 0.5f;

    public float snapDistanceToActivateMovement = 10f;

    [Header("Editor Design")]
    public float scaleSpeed = 0.25f;
    public float rotationSpeed = 0.5f;
    public float distanceFromCameraForNewEntitties = 5;
}