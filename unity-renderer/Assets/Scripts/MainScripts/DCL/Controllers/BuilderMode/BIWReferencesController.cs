using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BIWReferencesController
{
    private const string PROJECT_REFERENCES_PATH = "ScriptableObjects/ProjectReferences";
    private const string GOD_MODE_DYNAMIC_VARIABLE_PATH = "ScriptableObjects/GodModeVariables";
    private const string FIRST_PERSON_DYNAMIC_VARIABLE_PATH = "ScriptableObjects/FirstPersonVariables";
    private const string INPUTS_PATH = "ScriptableObjects/InputReferences";

    //Scriptable Objects
    public BIWProjectReferences projectReferences => projectReferencesValue;
    public BIWGodModeDynamicVariables godModeDynamicVariables => godModeDynamicVariablesValue;
    public BIWFirstPersonDynamicVariables firstPersonDynamicVariables => firstPersonDynamicVariablesValue;
    public BIWInputsReferences inputsReferences => inputsReferencesValue;

    private BIWProjectReferences projectReferencesValue;
    private BIWGodModeDynamicVariables godModeDynamicVariablesValue;
    private BIWFirstPersonDynamicVariables firstPersonDynamicVariablesValue;
    private BIWInputsReferences inputsReferencesValue;

    // BIW Controllers
    public IBIWOutlinerController biwOutlinerController => biwOutlinerControllerValue;
    public IBIWInputHandler biwInputHandler => biwInputHandlerValue;
    public IBIWInputWrapper biwInputWrapper => biwInputWrapperValue;
    public IBIWPublishController biwPublishController => biwPublishControllerValue;
    public IBIWCreatorController biwCreatorController => biwCreatorControllerValue;
    public IBIWModeController biwModeController => biwModeControllerValue;
    public IBIWFloorHandler biwFloorHandler => biwFloorHandlerValue;
    public IBIWEntityHandler biwEntityHandler => biwEntityHandlerValue;
    public IBIActionController BiwBiActionController => biwBiActionControllerValue;
    public IBIWSaveController biwSaveController => biwSaveControllerValue;

    private IBIWOutlinerController biwOutlinerControllerValue;
    private IBIWInputHandler biwInputHandlerValue;
    private IBIWInputWrapper biwInputWrapperValue;
    private IBIWPublishController biwPublishControllerValue;
    private IBIWCreatorController biwCreatorControllerValue;
    private IBIWModeController biwModeControllerValue;
    private IBIWFloorHandler biwFloorHandlerValue;
    private IBIWEntityHandler biwEntityHandlerValue;
    private IBIActionController biwBiActionControllerValue;
    private IBIWSaveController biwSaveControllerValue;

    public void Init(IBIWOutlinerController outlinerController,
        IBIWInputHandler bIWInputHandler,
        IBIWInputWrapper biwInputWrapper,
        IBIWPublishController biwPublishController,
        IBIWCreatorController biwCreatorController,
        IBIWModeController biwModeController,
        IBIWFloorHandler biwFloorHandler,
        IBIWEntityHandler biwEntityHandler,
        IBIActionController biActionController,
        IBIWSaveController biwSaveController)
    {
        projectReferencesValue = Resources.Load<BIWProjectReferences>(PROJECT_REFERENCES_PATH);
        godModeDynamicVariablesValue = Resources.Load<BIWGodModeDynamicVariables>(GOD_MODE_DYNAMIC_VARIABLE_PATH);
        firstPersonDynamicVariablesValue = Resources.Load<BIWFirstPersonDynamicVariables>(FIRST_PERSON_DYNAMIC_VARIABLE_PATH);
        inputsReferencesValue = Resources.Load<BIWInputsReferences>(INPUTS_PATH);

        biwOutlinerControllerValue = outlinerController;
        biwInputHandlerValue = bIWInputHandler;
        biwInputWrapperValue = biwInputWrapper;
        biwPublishControllerValue = biwPublishController;
        biwCreatorControllerValue = biwCreatorController;
        biwModeControllerValue = biwModeController;
        biwFloorHandlerValue = biwFloorHandler;
        biwEntityHandlerValue = biwEntityHandler;
        biwBiActionControllerValue = biActionController;
        biwSaveControllerValue = biwSaveController;
    }

    public void Dispose()
    {
        projectReferencesValue = null;
        godModeDynamicVariablesValue = null;
        firstPersonDynamicVariablesValue = null;
        inputsReferencesValue = null;
    }

}