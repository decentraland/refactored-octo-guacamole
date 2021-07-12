using DCL;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mapping for Trigger actions
/// </summary>
public enum DCLAction_Trigger
{
    //Remember to explicitly assign the value to each entry so we minimize issues with serialization + conflicts
    CameraChange = 100,

    ToggleNavMap = 110,
    ToggleFriends = 120,
    CloseWindow = 121,
    ToggleWorldChat = 122,
    ToggleUIVisibility = 123,
    ToggleControlsHud = 124,
    ToggleSettings = 125,
    ToggleExploreHud = 126,
    ToggleVoiceChatRecording = 127,
    ToggleAvatarEditorHud = 128,
    ToggleQuestsPanelHud = 129,

    OpenExpressions = 200,
    Expression_Wave = 201,
    Expression_FistPump = 202,
    Expression_Robot = 203,
    Expression_RaiseHand = 204,
    Expression_Clap = 205,
    Expression_ThrowMoney = 206,
    Expression_SendKiss = 207,

    //Builder In World 4xx
    BuildEditModeChange = 408,
    BuildEditModeToggleUI = 409,
    BuildEditModeToggleEntityList = 410,
    BuildEditModeToggleCatalog = 411,
    BuildEditModeToggleSceneInfo = 412,
    BuildEditModeToggleChangeCamera = 413,
    BuildEditModeToggleControls = 414,
    BuildEditModeToggleSnapMode = 415,
    BuildEditModeUndoAction = 417,
    BuildEditModeRedoAction = 418,
    BuildEditModeQuickBar1 = 419,
    BuildEditModeQuickBar2 = 420,
    BuildEditModeQuickBar3 = 421,
    BuildEditModeQuickBar4 = 422,
    BuildEditModeQuickBar5 = 423,
    BuildEditModeQuickBar6 = 424,
    BuildEditModeQuickBar7 = 425,
    BuildEditModeQuickBar8 = 426,
    BuildEditModeQuickBar9 = 427,
    BuildEditModeDuplicate = 428,
    BuildEditModeTranslate = 429,
    BuildEditModeRotate = 430,
    BuildEditModeScale = 431,
    BuildEditModeDelete = 434,
    BuildEditModeFocusSelectedEntities = 435,
    BuildEditModeReset = 443,
    BuildEditHideSelectedEntities = 444,
    BuildEditShowAllEntities = 445,
    BuildEditModeResetCamera = 446,
    BuildEditModeZoomIn = 447,
    BuildEditModeZoomOut = 448
}

/// <summary>
/// Mapping for hold actions
/// </summary>
public enum DCLAction_Hold
{
    //Remember to explicitly assign the value to each entry so we minimize issues with serialization + conflicts
    Sprint = 1,
    Jump = 2,
    FreeCameraMode = 101,
    VoiceChatRecording = 102,
    DefaultConfirmAction = 300,
    DefaultCancelAction = 301,
    BuildEditModeMultiSelection = 432,
    BuildEditModeSquareMultiSelection = 433,
    BuildEditModeFirstPersonRotation = 436,
    BuildEditModeCameraAdvanceFoward = 437,
    BuildEditModeCameraAdvanceBack = 438,
    BuildEditModeCameraAdvanceLeft = 439,
    BuildEditModeCameraAdvanceRight = 440,
    BuildEditModeCameraAdvanceUp = 441,
    BuildEditModeCameraAdvanceDown = 442,
    BuildEditModeCameraPan = 446
}

/// <summary>
/// Mapping for measurable actions
/// </summary>
public enum DCLAction_Measurable
{
    //Remember to explicitly assign the value to each entry so we minimize issues with serialization + conflicts
    CharacterXAxis = 1,
    CharacterYAxis = 2,
    CameraXAxis = 3,
    CameraYAxis = 4,
}

/// <summary>
/// Group of actions currently actived
/// </summary>
public enum InputTypeMode
{
    OFF,
    GENERAL,
    BUILD_MODE_LOADING,
    BUILD_MODE
}

/// <summary>
/// Input Controller will map inputs(keys/mouse/axis) to DCL actions, check if they can be triggered (modifiers) and raise the events
/// </summary>
public class InputController : MonoBehaviour
{
    public static bool ENABLE_THIRD_PERSON_CAMERA = true;

    [Header("General Input")]
    public InputAction_Trigger[] triggerTimeActions;

    public InputAction_Hold[] holdActions;
    public InputAction_Measurable[] measurableActions;

    [Header("BuildMode Input")]
    public InputAction_Trigger[] builderTriggerTimeActions;

    public InputAction_Hold[] builderHoldActions;
    public InputAction_Trigger[] loadingBuilderTriggerTimeActions;

    bool renderingEnabled => CommonScriptableObjects.rendererState.Get();
    bool allUIHidden => CommonScriptableObjects.allUIHidden.Get();
    public InputTypeMode inputTypeMode { get; set; } = InputTypeMode.GENERAL;

    private void Update()
    {
        if (!renderingEnabled)
        {
            Stop_Measurable(measurableActions);
            return;
        }

        switch (inputTypeMode)
        {
            case InputTypeMode.OFF:
                Stop_Measurable(measurableActions);
                return;
            case InputTypeMode.GENERAL:
                Update_Trigger(triggerTimeActions);
                Update_Hold(holdActions);
                Update_Measurable(measurableActions);
                break;
            case InputTypeMode.BUILD_MODE_LOADING:
                Update_Trigger(loadingBuilderTriggerTimeActions);
                Stop_Measurable(measurableActions);
                break;
            case InputTypeMode.BUILD_MODE:
                Update_Trigger(builderTriggerTimeActions);
                Update_Hold(builderHoldActions);
                Update_Measurable(measurableActions);
                break;
        }
    }

    /// <summary>
    /// Map the trigger actions to inputs + modifiers and check if their events must be triggered
    /// </summary>
    public void Update_Trigger(InputAction_Trigger[] triggerTimeActions)
    {
        for (var i = 0; i < triggerTimeActions.Length; i++)
        {
            var action = triggerTimeActions[i];

            if (action.isTriggerBlocked != null && action.isTriggerBlocked.Get())
                continue;

            switch (action.GetDCLAction())
            {
                case DCLAction_Trigger.CameraChange:
                    //Disable until the fine-tuning is ready
                    if (ENABLE_THIRD_PERSON_CAMERA)
                        InputProcessor.FromKey(action, KeyCode.V,
                            modifiers: InputProcessor.Modifier.NeedsPointerLocked |
                                       InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.ToggleNavMap:
                    if (allUIHidden)
                        break;
                    InputProcessor.FromKey(action, KeyCode.M, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    InputProcessor.FromKey(action, KeyCode.Tab, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    InputProcessor.FromKey(action, KeyCode.Escape, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.ToggleFriends:
                    if (allUIHidden)
                        break;
                    InputProcessor.FromKey(action, KeyCode.L, modifiers: InputProcessor.Modifier.None);
                    break;
                case DCLAction_Trigger.ToggleWorldChat:
                    if (allUIHidden)
                        break;
                    InputProcessor.FromKey(action, KeyCode.Return, modifiers: InputProcessor.Modifier.None);
                    break;
                case DCLAction_Trigger.ToggleUIVisibility:
                    InputProcessor.FromKey(action, KeyCode.U, modifiers: InputProcessor.Modifier.None);
                    break;
                case DCLAction_Trigger.CloseWindow:
                    if (allUIHidden || DataStore.i.isSignUpFlow.Get())
                        break;
                    InputProcessor.FromKey(action, KeyCode.Escape, modifiers: InputProcessor.Modifier.None);
                    break;
                case DCLAction_Trigger.OpenExpressions:
                    if (allUIHidden)
                        break;
                    InputProcessor.FromKey(action, KeyCode.B, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    InputProcessor.FromKey(action, KeyCode.Escape, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.ToggleControlsHud:
                    InputProcessor.FromKey(action, KeyCode.C, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.ToggleSettings:
                    InputProcessor.FromKey(action, KeyCode.P, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.ToggleExploreHud:
                    if (allUIHidden)
                        break;
                    InputProcessor.FromKey(action, KeyCode.X, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.Expression_Wave:
                    InputProcessor.FromKey(action, KeyCode.Alpha1, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.Expression_FistPump:
                    InputProcessor.FromKey(action, KeyCode.Alpha2, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.Expression_Robot:
                    InputProcessor.FromKey(action, KeyCode.Alpha3, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.Expression_RaiseHand:
                    InputProcessor.FromKey(action, KeyCode.Alpha4, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.Expression_Clap:
                    InputProcessor.FromKey(action, KeyCode.Alpha5, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.Expression_ThrowMoney:
                    InputProcessor.FromKey(action, KeyCode.Alpha6, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.Expression_SendKiss:
                    InputProcessor.FromKey(action, KeyCode.Alpha7, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeChange:
                    InputProcessor.FromKey(action, KeyCode.K, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.ToggleVoiceChatRecording:
                    InputProcessor.FromKey(action, KeyCode.T, modifiers: InputProcessor.Modifier.FocusNotInInput, modifierKeys: new KeyCode[] { KeyCode.LeftAlt });
                    break;
                case DCLAction_Trigger.ToggleAvatarEditorHud:
                    InputProcessor.FromKey(action, KeyCode.I, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeToggleUI:
                    InputProcessor.FromKey(action, KeyCode.U, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeToggleChangeCamera:
                    InputProcessor.FromKey(action, KeyCode.V, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeToggleControls:
                    InputProcessor.FromKey(action, KeyCode.C, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeToggleSnapMode:
                    InputProcessor.FromKey(action, KeyCode.O, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeRedoAction:
                    InputProcessor.FromKey(action, KeyCode.Y, modifiers: InputProcessor.Modifier.FocusNotInInput, modifierKeys: new KeyCode[] { KeyCode.LeftShift });
                    break;
                case DCLAction_Trigger.BuildEditModeUndoAction:
                    InputProcessor.FromKey(action, KeyCode.Z, modifiers: InputProcessor.Modifier.FocusNotInInput, modifierKeys: new KeyCode[] { KeyCode.LeftShift });
                    break;
                case DCLAction_Trigger.BuildEditModeQuickBar1:
                    InputProcessor.FromKey(action, KeyCode.Alpha1, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeQuickBar2:
                    InputProcessor.FromKey(action, KeyCode.Alpha2, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeQuickBar3:
                    InputProcessor.FromKey(action, KeyCode.Alpha3, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeQuickBar4:
                    InputProcessor.FromKey(action, KeyCode.Alpha4, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeQuickBar5:
                    InputProcessor.FromKey(action, KeyCode.Alpha5, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeQuickBar6:
                    InputProcessor.FromKey(action, KeyCode.Alpha6, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeQuickBar7:
                    InputProcessor.FromKey(action, KeyCode.Alpha7, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeQuickBar8:
                    InputProcessor.FromKey(action, KeyCode.Alpha8, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeQuickBar9:
                    InputProcessor.FromKey(action, KeyCode.Alpha9, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeDelete:
                    InputProcessor.FromKey(action, KeyCode.Delete, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    InputProcessor.FromKey(action, KeyCode.Backspace, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeDuplicate:
                    InputProcessor.FromKey(action, KeyCode.D, modifiers: InputProcessor.Modifier.FocusNotInInput, modifierKeys: new KeyCode[] { KeyCode.LeftShift });
                    break;
                case DCLAction_Trigger.BuildEditModeTranslate:
                    InputProcessor.FromKey(action, KeyCode.G, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    InputProcessor.FromKey(action, KeyCode.M, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeRotate:
                    InputProcessor.FromKey(action, KeyCode.R, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeScale:
                    InputProcessor.FromKey(action, KeyCode.S, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeFocusSelectedEntities:
                    InputProcessor.FromKey(action, KeyCode.F, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeReset:
                    InputProcessor.FromKey(action, KeyCode.R, modifiers: InputProcessor.Modifier.FocusNotInInput, modifierKeys: new KeyCode[] { KeyCode.LeftShift });
                    break;
                case DCLAction_Trigger.BuildEditHideSelectedEntities:
                    InputProcessor.FromKey(action, KeyCode.H, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditShowAllEntities:
                    InputProcessor.FromKey(action, KeyCode.H, modifiers: InputProcessor.Modifier.FocusNotInInput, modifierKeys: new KeyCode[] { KeyCode.LeftShift });
                    break;
                case DCLAction_Trigger.BuildEditModeResetCamera:
                    InputProcessor.FromKey(action, KeyCode.C, modifiers: InputProcessor.Modifier.FocusNotInInput, modifierKeys: new KeyCode[] { KeyCode.LeftShift });
                    break;
                case DCLAction_Trigger.BuildEditModeZoomIn:
                    InputProcessor.FromKey(action, KeyCode.KeypadPlus, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.BuildEditModeZoomOut:
                    InputProcessor.FromKey(action, KeyCode.KeypadMinus, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Trigger.ToggleQuestsPanelHud:
                    InputProcessor.FromKey(action, KeyCode.J, modifiers: InputProcessor.Modifier.FocusNotInInput);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Map the hold actions to inputs + modifiers and check if their events must be triggered
    /// </summary>
    private void Update_Hold(InputAction_Hold[] holdActions)
    {
        for (var i = 0; i < holdActions.Length; i++)
        {
            var action = holdActions[i];
            switch (action.GetDCLAction())
            {
                case DCLAction_Hold.Sprint:
                    InputProcessor.FromKey(action, KeyCode.LeftShift, InputProcessor.Modifier.NeedsPointerLocked);
                    break;
                case DCLAction_Hold.Jump:
                    InputProcessor.FromKey(action, KeyCode.Space, InputProcessor.Modifier.NeedsPointerLocked);
                    break;
                case DCLAction_Hold.FreeCameraMode:
                    //Disable until the fine-tuning is ready
                    if (ENABLE_THIRD_PERSON_CAMERA)
                        InputProcessor.FromKey(action, KeyCode.Y, InputProcessor.Modifier.NeedsPointerLocked);
                    break;
                case DCLAction_Hold.VoiceChatRecording:
                    // Push to talk functionality only triggers if no modifier key is pressed
                    InputProcessor.FromKey(action, KeyCode.T, InputProcessor.Modifier.FocusNotInInput, null);
                    break;
                case DCLAction_Hold.DefaultConfirmAction:
                    InputProcessor.FromKey(action, KeyCode.E, InputProcessor.Modifier.None);
                    break;
                case DCLAction_Hold.DefaultCancelAction:
                    InputProcessor.FromKey(action, KeyCode.F, InputProcessor.Modifier.None);
                    break;
                case DCLAction_Hold.BuildEditModeMultiSelection:
                    InputProcessor.FromKey(action, KeyCode.LeftShift, InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Hold.BuildEditModeSquareMultiSelection:
                    InputProcessor.FromKey(action, KeyCode.LeftShift, InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Hold.BuildEditModeFirstPersonRotation:
                    InputProcessor.FromKey(action, KeyCode.R, InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Hold.BuildEditModeCameraAdvanceFoward:
                    InputProcessor.FromKey(action, KeyCode.UpArrow, InputProcessor.Modifier.FocusNotInInput);
                    InputProcessor.FromKey(action, KeyCode.W, InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Hold.BuildEditModeCameraAdvanceBack:
                    InputProcessor.FromKey(action, KeyCode.DownArrow, InputProcessor.Modifier.FocusNotInInput);
                    InputProcessor.FromKey(action, KeyCode.S, InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Hold.BuildEditModeCameraAdvanceLeft:
                    InputProcessor.FromKey(action, KeyCode.LeftArrow, InputProcessor.Modifier.FocusNotInInput);
                    InputProcessor.FromKey(action, KeyCode.A, InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Hold.BuildEditModeCameraAdvanceRight:
                    InputProcessor.FromKey(action, KeyCode.RightArrow, InputProcessor.Modifier.FocusNotInInput);
                    InputProcessor.FromKey(action, KeyCode.D, InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Hold.BuildEditModeCameraAdvanceUp:
                    InputProcessor.FromKey(action, KeyCode.E, InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Hold.BuildEditModeCameraAdvanceDown:
                    InputProcessor.FromKey(action, KeyCode.Q, InputProcessor.Modifier.FocusNotInInput);
                    break;
                case DCLAction_Hold.BuildEditModeCameraPan:
                    InputProcessor.FromKey(action, KeyCode.LeftShift, InputProcessor.Modifier.FocusNotInInput);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Map the measurable actions to inputs + modifiers and check if their events must be triggered
    /// </summary>
    private void Update_Measurable(InputAction_Measurable[] measurableActions)
    {
        for (var i = 0; i < measurableActions.Length; i++)
        {
            var action = measurableActions[i];
            switch (action.GetDCLAction())
            {
                case DCLAction_Measurable.CharacterXAxis:
                    InputProcessor.FromAxis(action, "Horizontal", InputProcessor.Modifier.NeedsPointerLocked);
                    break;
                case DCLAction_Measurable.CharacterYAxis:
                    InputProcessor.FromAxis(action, "Vertical", InputProcessor.Modifier.NeedsPointerLocked);
                    break;
                case DCLAction_Measurable.CameraXAxis:
                    InputProcessor.FromAxis(action, "Mouse X", InputProcessor.Modifier.NeedsPointerLocked);
                    break;
                case DCLAction_Measurable.CameraYAxis:
                    InputProcessor.FromAxis(action, "Mouse Y", InputProcessor.Modifier.NeedsPointerLocked);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void Stop_Measurable(InputAction_Measurable[] measurableActions)
    {
        for (var i = 0; i < measurableActions.Length; i++)
        {
            measurableActions[i].RaiseOnValueChanged(0);
        }
    }
}

/// <summary>
/// Helper class that wraps the processing of inputs and modifiers to trigger actions events
/// </summary>
public static class InputProcessor
{
    private static readonly KeyCode[] MODIFIER_KEYS = new[] { KeyCode.LeftControl, KeyCode.LeftAlt, KeyCode.LeftShift, KeyCode.LeftCommand };

    [Flags]
    public enum Modifier
    {
        //Set the values as bit masks
        None = 0b0000000, // No modifier needed
        NeedsPointerLocked = 0b0000001, // The pointer must be locked to the game
        FocusNotInInput = 0b0000010, // The game focus cannot be in an input field
    }

    /// <summary>
    /// Check if the modifier keys are pressed
    /// </summary>
    /// <param name="modifierKeys"> Keycodes modifiers</param>
    /// <returns></returns>
    public static Boolean PassModifierKeys(KeyCode[] modifierKeys)
    {
        for (var i = 0; i < MODIFIER_KEYS.Length; i++)
        {
            var keyCode = MODIFIER_KEYS[i];
            var pressed = Input.GetKey(keyCode);
            if (modifierKeys == null)
            {
                if (pressed)
                    return false;
            }
            else
            {
                if (modifierKeys.Contains(keyCode) != pressed)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if a miscellaneous modifiers are present. These modifiers are related to the meta-state of the application
    /// they can be anything such as mouse pointer state, where the focus is, camera mode...
    /// </summary>
    /// <param name="modifiers"></param>
    /// <returns></returns>
    public static bool PassModifiers(Modifier modifiers)
    {
        if (IsModifierSet(modifiers, Modifier.NeedsPointerLocked) && !DCL.Helpers.Utils.isCursorLocked)
            return false;

        if (IsModifierSet(modifiers, Modifier.FocusNotInInput) && FocusIsInInputField())
            return false;

        return true;
    }

    /// <summary>
    /// Process an input action mapped to a keyboard key.
    /// </summary>
    /// <param name="action">Trigger Action to perform</param>
    /// <param name="key">KeyCode mapped to this action</param>
    /// <param name="modifierKeys">KeyCodes required to perform the action</param>
    /// <param name="modifiers">Miscellaneous modifiers required for this action</param>
    public static void FromKey(InputAction_Trigger action, KeyCode key, KeyCode[] modifierKeys = null,
        Modifier modifiers = Modifier.None)
    {
        if (!PassModifiers(modifiers))
            return;

        if (!PassModifierKeys(modifierKeys))
            return;

        if (Input.GetKeyDown(key))
            action.RaiseOnTriggered();
    }

    /// <summary>
    /// Process an input action mapped to a button.
    /// </summary>
    /// <param name="action">Trigger Action to perform</param>
    /// <param name="mouseButtonIdx">Index of the mouse button mapped to this action</param>
    /// <param name="modifiers">Miscellaneous modifiers required for this action</param>
    public static void FromMouseButton(InputAction_Trigger action, int mouseButtonIdx,
        Modifier modifiers = Modifier.None)
    {
        if (!PassModifiers(modifiers))
            return;

        if (Input.GetMouseButton(mouseButtonIdx))
            action.RaiseOnTriggered();
    }

    /// <summary>
    /// Process an input action mapped to a keyboard key
    /// </summary>
    /// <param name="action">Hold Action to perform</param>
    /// <param name="key">KeyCode mapped to this action</param>
    /// <param name="modifiers">Miscellaneous modifiers required for this action</param>
    public static void FromKey(InputAction_Hold action, KeyCode key, Modifier modifiers = Modifier.None)
    {
        if (!PassModifiers(modifiers))
            return;

        if (Input.GetKeyDown(key))
            action.RaiseOnStarted();
        if (Input.GetKeyUp(key))
            action.RaiseOnFinished();
    }

    /// <summary>
    /// Process an input action mapped to a keyboard key
    /// </summary>
    /// <param name="action">Hold Action to perform</param>
    /// <param name="key">KeyCode mapped to this action</param>
    /// <param name="modifiers">Miscellaneous modifiers required for this action</param>
    /// <param name="modifierKeys">KeyCodes required to perform the action</param>
    public static void FromKey(InputAction_Hold action, KeyCode key, Modifier modifiers, KeyCode[] modifierKeys)
    {
        if (!PassModifierKeys(modifierKeys))
            return;

        FromKey(action, key, modifiers);
    }

    /// <summary>
    /// Process an input action mapped to a mouse button
    /// </summary>
    /// <param name="action">Hold Action to perform</param>
    /// <param name="mouseButtonIdx">Index of the mouse button</param>
    /// <param name="modifiers">Miscellaneous modifiers required for this action</param>
    public static void FromMouse(InputAction_Hold action, int mouseButtonIdx, Modifier modifiers = Modifier.None)
    {
        if (!PassModifiers(modifiers))
            return;

        if (Input.GetMouseButtonDown(mouseButtonIdx))
            action.RaiseOnStarted();
        if (Input.GetMouseButtonUp(mouseButtonIdx))
            action.RaiseOnFinished();
    }

    /// <summary>
    /// Process an input action mapped to an axis
    /// </summary>
    /// <param name="action">Measurable Action to perform</param>
    /// <param name="axisName">Axis name</param>
    /// <param name="modifiers">Miscellaneous modifiers required for this action</param>
    public static void FromAxis(InputAction_Measurable action, string axisName, Modifier modifiers = Modifier.None)
    {
        if (!PassModifiers(modifiers))
        {
            action.RaiseOnValueChanged(0);
            return;
        }

        action.RaiseOnValueChanged(Input.GetAxis(axisName));
    }

    /// <summary>
    /// Bitwise check for the modifiers flags
    /// </summary>
    /// <param name="modifiers">Modifier to check</param>
    /// <param name="value">Modifier mapped to a bit to check</param>
    /// <returns></returns>
    public static bool IsModifierSet(Modifier modifiers, Modifier value)
    {
        int flagsValue = (int)modifiers;
        int flagValue = (int)value;

        return (flagsValue & flagValue) != 0;
    }

    public static bool FocusIsInInputField()
    {
        if (EventSystem.current.currentSelectedGameObject != null &&
            (EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null ||
             EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>() != null))
        {
            return true;
        }

        return false;
    }
}