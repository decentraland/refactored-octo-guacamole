using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuilderInWorldProjectReferences", menuName = "BuilderInWorld/AudioReferences")]
public class BIWAudioReferences : ScriptableObject
{
    [Header("Audio Events")]
    [SerializeField]
    AudioEvent eventAssetSpawn;

    [SerializeField]
    AudioEvent eventAssetPlace;

    [SerializeField]
    AudioEvent eventAssetSelect;

    [SerializeField]
    AudioEvent eventAssetDeselect;

    [SerializeField]
    AudioEvent eventBuilderOutOfBounds;

    [SerializeField]
    AudioEvent eventBuilderOutOfBoundsPlaced;

    [SerializeField]
    AudioEvent eventAssetDelete;

    [SerializeField]
    AudioEvent eventBuilderExit;

    [SerializeField]
    AudioEvent eventBuilderMusic;

}