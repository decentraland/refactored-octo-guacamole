using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuilderInWorldProjectReferences", menuName = "BuilderInWorld/ProjectReferences")]
public class BIWProjectReferences : ScriptableObject
{
    [Header("Materials")]
    public Material skyBoxMaterial;
    public Material editMaterial;
    public Material cameraOutlinerMaterial;

    [Header("Prefabs")]
    public GameObject errorPrefab;
    public GameObject floorPlaceHolderPrefab;
    public GameObject floorLoadingPrefab;
    public GameObject loadingPrefab;
    public GameObject godModeBuilderPrefab;
    public GameObject audioPrefab;
}