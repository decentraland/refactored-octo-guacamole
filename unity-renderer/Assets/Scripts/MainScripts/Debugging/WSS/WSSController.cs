using DCL.Components;
using DCL.Interface;
using System.Collections.Generic;
using DCL.Camera;
using UnityEditor;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace DCL
{
    public class DCLWebSocketService : WebSocketBehavior
    {
        public static bool enterAsAGuest = false; // TODO(Mateo): Refactor https://github.com/decentraland/unity-renderer/issues/585
        static bool VERBOSE = false;

        private void SendMessageToWeb(string type, string message)
        {
#if (UNITY_EDITOR || UNITY_STANDALONE)
            var x = new Message()
            {
                type = type,
                payload = message
            };
            Send(Newtonsoft.Json.JsonConvert.SerializeObject(x));
#endif
        }

        public class Message
        {
            public string type;
            public string payload;

            public override string ToString() { return string.Format("type = {0}... payload = {1}...", type, payload); }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);

            lock (WSSController.queuedMessages)
            {
                Message finalMessage;
                finalMessage = JsonUtility.FromJson<Message>(e.Data);

                WSSController.queuedMessages.Enqueue(finalMessage);
                WSSController.queuedMessagesDirty = true;
            }
        }

        protected override void OnError(ErrorEventArgs e) { base.OnError(e); }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            WebInterface.OnMessageFromEngine -= SendMessageToWeb;
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            WebInterface.OnMessageFromEngine += SendMessageToWeb;
            Send("{\"welcome\": true}");
            if (enterAsAGuest)
                WebInterface.SendAuthentication(WebInterface.RendererAuthenticationType.Guest);
        }
    }

    public class WSSController : MonoBehaviour
    {
        public enum DebugPanel
        {
            Off,
            Scene,
            Engine
        }

        public enum BaseUrl
        {
            LOCAL_HOST,
            CUSTOM,
        }

        public enum Environment
        {
            USE_DEFAULT_FROM_URL,
            LOCAL,
            ZONE,
            TODAY,
            ORG
        }

        private const string ENGINE_DEBUG_PANEL = "ENGINE_DEBUG_PANEL";
        private const string SCENE_DEBUG_PANEL = "SCENE_DEBUG_PANEL";
        public static WSSController i { get; private set; }
        static bool VERBOSE = false;
        WebSocketServer ws;
        public RenderingController renderingController;
        public DCLCharacterController characterController;
        private Builder.DCLBuilderBridge builderBridge = null;
        private BuilderInWorldBridge builderInWorldBridge = null;
        private HUDBridge hudBridge = null;
        public CameraController cameraController;
        public GameObject bridgesGameObject;

        [System.NonSerialized]
        public static Queue<DCLWebSocketService.Message> queuedMessages = new Queue<DCLWebSocketService.Message>();

        [System.NonSerialized]
        public static volatile bool queuedMessagesDirty;

        public bool isServerReady { get { return ws.IsListening; } }

        public string wssServerUrl = "ws://localhost:5000/";
        public bool openBrowserWhenStart;

        [Header("Kernel General Settings")]
        public bool useCustomContentServer = false;

        public string customContentServerUrl = "http://localhost:1338/";

        [Space(10)]
        public BaseUrl baseUrlMode;

        public string baseUrlCustom;

        [Space(10)]
        public Environment environment;

        public Vector2 startInCoords = new Vector2(-99, 109);

        [Header("Kernel Misc Settings")]
        public bool forceLocalComms = true;

        public bool allWearables = false;
        public bool testWearables = false;
        public bool enableTutorial = false;
        public bool builderInWorld = false;
        public bool soloScene = true;
        public DebugPanel debugPanelMode = DebugPanel.Off;

        private void Awake() { i = this; }

        private void Start()
        {
            if (useCustomContentServer)
            {
                RendereableAssetLoadHelper.useCustomContentServerUrl = true;
                RendereableAssetLoadHelper.customContentServerUrl = customContentServerUrl;
            }

#if (UNITY_EDITOR || UNITY_STANDALONE)
            DCL.DataStore.i.debugConfig.isWssDebugMode = true;

            string wssUrl = this.wssServerUrl;
            string wssServiceId = "dcl";
            ws = new WebSocketServer(wssUrl);
            ws.AddWebSocketService<DCLWebSocketService>("/" + wssServiceId);
            ws.Start();

            if (openBrowserWhenStart)
            {
                string baseUrl = "";
                string debugString = "";

                if (baseUrlMode == BaseUrl.CUSTOM)
                    baseUrl = baseUrlCustom;
                else
                    baseUrl = "http://localhost:3000/?";

                switch (environment)
                {
                    case Environment.USE_DEFAULT_FROM_URL:
                        break;
                    case Environment.LOCAL:
                        debugString = "DEBUG_MODE&";
                        break;
                    case Environment.ZONE:
                        debugString = "ENV=zone&";
                        break;
                    case Environment.TODAY:
                        debugString = "ENV=today&";
                        break;
                    case Environment.ORG:
                        debugString = "ENV=org&";
                        break;
                }

                if (forceLocalComms)
                {
                    debugString += "LOCAL_COMMS&";
                }

                if (allWearables)
                {
                    debugString += "ALL_WEARABLES&";
                }

                if (testWearables)
                {
                    debugString += "TEST_WEARABLES&";
                }

                if (enableTutorial)
                {
                    debugString += "RESET_TUTORIAL&";
                }

                if (soloScene)
                {
                    debugString += "LOS=0&";
                }

                if (builderInWorld)
                {
                    debugString += "ENABLE_BUILDER_IN_WORLD&";
                }

                string debugPanelString = "";

                if (debugPanelMode == DebugPanel.Engine)
                {
                    debugPanelString = ENGINE_DEBUG_PANEL + "&";
                }
                else if (debugPanelMode == DebugPanel.Scene)
                {
                    debugPanelString = SCENE_DEBUG_PANEL + "&";
                }

                Application.OpenURL(
                    $"{baseUrl}{debugString}{debugPanelString}position={startInCoords.x}%2C{startInCoords.y}&ws=" + wssUrl + wssServiceId);
            }
#endif
        }

        private void OnDestroy()
        {
#if (UNITY_EDITOR || UNITY_STANDALONE)
            if (ws != null)
            {
                ws.Stop();
                ws = null;
            }
#endif
        }

        void Update()
        {
#if (UNITY_EDITOR || UNITY_STANDALONE)
            lock (WSSController.queuedMessages)
            {
                if (queuedMessagesDirty)
                {
                    while (queuedMessages.Count > 0)
                    {
                        DCLWebSocketService.Message msg = queuedMessages.Dequeue();

                        if (VERBOSE)
                        {
                            Debug.Log(
                                "<b><color=#0000FF>WSSController</color></b> >>> Got it! passing message of type " +
                                msg.type);
                        }

                        switch (msg.type)
                        {
                            case "SetDebug":
                                DCL.Environment.i.platform.debugController.SetDebug();
                                break;
                            case "SetSceneDebugPanel":
                                DCL.Environment.i.platform.debugController.SetSceneDebugPanel();
                                break;
                            case "ShowFPSPanel":
                                DCL.Environment.i.platform.debugController.ShowFPSPanel();
                                break;
                            case "HideFPSPanel":
                                DCL.Environment.i.platform.debugController.HideFPSPanel();
                                break;
                            case "SetEngineDebugPanel":
                                DCL.Environment.i.platform.debugController.SetEngineDebugPanel();
                                break;
                            case "SendSceneMessage":
                                DCL.Environment.i.world.sceneController.SendSceneMessage(msg.payload);
                                break;
                            case "LoadParcelScenes":
                                DCL.Environment.i.world.sceneController.LoadParcelScenes(msg.payload);
                                break;
                            case "UnloadScene":
                                DCL.Environment.i.world.sceneController.UnloadScene(msg.payload);
                                break;
                            case "Reset":
                                DCL.Environment.i.world.sceneController.UnloadAllScenesQueued();
                                break;
                            case "CreateGlobalScene":
                                DCL.Environment.i.world.sceneController.CreateGlobalScene(msg.payload);
                                break;
                            case "BuilderReady":
                                Main.i.BuilderReady();
                                break;
                            case "UpdateParcelScenes":
                                DCL.Environment.i.world.sceneController.UpdateParcelScenes(msg.payload);
                                break;
                            case "Teleport":
                                characterController.Teleport(msg.payload);
                                break;
                            case "SetRotation":
                                cameraController.SetRotation(msg.payload);
                                break;
                            case "LoadProfile":
                                UserProfileController.i?.LoadProfile(msg.payload);
                                break;
                            case "AddUserProfileToCatalog":
                                UserProfileController.i.AddUserProfileToCatalog(msg.payload);
                                break;
                            case "AddUserProfilesToCatalog":
                                UserProfileController.i.AddUserProfilesToCatalog(msg.payload);
                                break;
                            case "RemoveUserProfilesFromCatalog":
                                UserProfileController.i.RemoveUserProfilesFromCatalog(msg.payload);
                                break;
                            case "ActivateRendering":
                                renderingController.ActivateRendering();
                                break;
                            case "DeactivateRendering":
                                renderingController.DeactivateRendering();
                                break;
                            case "ReportFocusOn":
                                bridgesGameObject.SendMessage(msg.type, msg.payload);
                                break;
                            case "ReportFocusOff":
                                bridgesGameObject.SendMessage(msg.type, msg.payload);
                                break;
                            case "ForceActivateRendering":
                                renderingController.ForceActivateRendering();
                                break;
                            case "ShowNotificationFromJson":
                                NotificationsController.i.ShowNotificationFromJson(msg.payload);
                                break;
                            case "GetMousePosition":
                                GetBuilderBridge()?.GetMousePosition(msg.payload);
                                break;
                            case "SelectGizmo":
                                GetBuilderBridge()?.SelectGizmo(msg.payload);
                                break;
                            case "ResetObject":
                                GetBuilderBridge()?.ResetObject();
                                break;
                            case "ZoomDelta":
                                GetBuilderBridge()?.ZoomDelta(msg.payload);
                                break;
                            case "SetPlayMode":
                                GetBuilderBridge()?.SetPlayMode(msg.payload);
                                break;
                            case "TakeScreenshot":
                                GetBuilderBridge()?.TakeScreenshot(msg.payload);
                                break;
                            case "ResetBuilderScene":
                                GetBuilderBridge()?.ResetBuilderScene();
                                break;
                            case "SetBuilderCameraPosition":
                                GetBuilderBridge()?.SetBuilderCameraPosition(msg.payload);
                                break;
                            case "SetBuilderCameraRotation":
                                GetBuilderBridge()?.SetBuilderCameraRotation(msg.payload);
                                break;
                            case "ResetBuilderCameraZoom":
                                GetBuilderBridge()?.ResetBuilderCameraZoom();
                                break;
                            case "SetGridResolution":
                                GetBuilderBridge()?.SetGridResolution(msg.payload);
                                break;
                            case "OnBuilderKeyDown":
                                GetBuilderBridge()?.OnBuilderKeyDown(msg.payload);
                                break;
                            case "UnloadBuilderScene":
                                GetBuilderBridge()?.UnloadBuilderScene(msg.payload);
                                break;
                            case "SetSelectedEntities":
                                GetBuilderBridge()?.SetSelectedEntities(msg.payload);
                                break;
                            case "GetCameraTargetBuilder":
                                GetBuilderBridge()?.GetCameraTargetBuilder(msg.payload);
                                break;
                            case "PreloadFile":
                                GetBuilderBridge()?.PreloadFile(msg.payload);
                                break;
                            case "SetBuilderConfiguration":
                                GetBuilderBridge()?.SetBuilderConfiguration(msg.payload);
                                break;
                            case "AddWearablesToCatalog":
                                CatalogController.i?.AddWearablesToCatalog(msg.payload);
                                break;
                            case "WearablesRequestFailed":
                                CatalogController.i?.WearablesRequestFailed(msg.payload);
                                break;
                            case "RemoveWearablesFromCatalog":
                                CatalogController.i?.RemoveWearablesFromCatalog(msg.payload);
                                break;
                            case "ClearWearableCatalog":
                                CatalogController.i?.ClearWearableCatalog();
                                break;
                            case "ConfigureHUDElement":
                                GetHUDBridge()?.ConfigureHUDElement(msg.payload);
                                break;
                            case "InitializeFriends":
                                FriendsController.i?.InitializeFriends(msg.payload);
                                break;
                            case "UpdateFriendshipStatus":
                                FriendsController.i?.UpdateFriendshipStatus(msg.payload);
                                break;
                            case "UpdateUserPresence":
                                FriendsController.i?.UpdateUserPresence(msg.payload);
                                break;
                            case "FriendNotFound":
                                FriendsController.i?.FriendNotFound(msg.payload);
                                break;
                            case "AddMessageToChatWindow":
                                ChatController.i?.AddMessageToChatWindow(msg.payload);
                                break;
                            case "UpdateMinimapSceneInformation":
                                MinimapMetadataController.i?.UpdateMinimapSceneInformation(msg.payload);
                                break;
                            case "SetTutorialEnabled":
                                DCL.Tutorial.TutorialController.i?.SetTutorialEnabled(msg.payload);
                                break;
                            case "SetTutorialEnabledForUsersThatAlreadyDidTheTutorial":
                                DCL.Tutorial.TutorialController.i?.SetTutorialEnabledForUsersThatAlreadyDidTheTutorial(msg.payload);
                                break;
                            case "TriggerSelfUserExpression":
                                GetHUDBridge().TriggerSelfUserExpression(msg.payload);
                                break;
                            case "AirdroppingRequest":
                                GetHUDBridge().AirdroppingRequest(msg.payload);
                                break;
                            case "ShowWelcomeNotification":
                                NotificationsController.i.ShowWelcomeNotification();
                                break;
                            case "ShowTermsOfServices":
                                GetHUDBridge().ShowTermsOfServices(msg.payload);
                                break;
                            case "RequestTeleport":
                                GetHUDBridge().RequestTeleport(msg.payload);
                                break;
                            case "UpdateHotScenesList":
                                HotScenesController.i.UpdateHotScenesList(msg.payload);
                                break;
                            case "UpdateBalanceOfMANA":
                                GetHUDBridge().UpdateBalanceOfMANA(msg.payload);
                                break;
                            case "SetPlayerTalking":
                                GetHUDBridge().SetPlayerTalking(msg.payload);
                                break;
                            case "SetVoiceChatEnabledByScene":
                                if (int.TryParse(msg.payload, out int value))
                                {
                                    GetHUDBridge().SetVoiceChatEnabledByScene(value);
                                }

                                break;
                            case "SetRenderProfile":
                                RenderProfileBridge.i.SetRenderProfile(msg.payload);
                                break;
                            case "ShowAvatarEditorInSignUp":
                                GetHUDBridge().ShowAvatarEditorInSignUp();
                                break;
                            case "SetUserTalking":
                                GetHUDBridge().SetUserTalking(msg.payload);
                                break;
                            case "SetUsersMuted":
                                GetHUDBridge().SetUsersMuted(msg.payload);
                                break;
                            case "SetKernelConfiguration":
                            case "UpdateRealmsInfo":
                            case "ConnectionToRealmSuccess":
                            case "ConnectionToRealmFailed":
                            case "InitializeQuests":
                            case "UpdateQuestProgress":
                            case "SetENSOwnerQueryResult":
                            case "UnpublishSceneResult":
                            case "SetLoadingScreen":
                                bridgesGameObject.SendMessage(msg.type, msg.payload);
                                break;
                            case "CrashPayloadRequest":
                            case "SetDisableAssetBundles":
                            case "DumpRendererLockersInfo":
                                //TODO(Brian): Move this to bridges
                                Main.i.SendMessage(msg.type);
                                break;
                            case "PublishSceneResult":
                                GetBuilderInWorldBridge()?.PublishSceneResult(msg.payload);
                                break;
                            case "BuilderProjectInfo":
                                GetBuilderInWorldBridge()?.BuilderProjectInfo(msg.payload);
                                break;
                            case "BuilderInWorldCatalogHeaders":
                                GetBuilderInWorldBridge()?.BuilderInWorldCatalogHeaders(msg.payload);
                                break;
                            case "RunPerformanceMeterTool":
                                if (float.TryParse(msg.payload, out float durationInSeconds))
                                {
                                    DCL.Environment.i.platform.debugController.RunPerformanceMeterTool(durationInSeconds);
                                }

                                break;
                            case "InstantiateBotsAtWorldPos":
                                DCL.Environment.i.platform.debugController.InstantiateBotsAtWorldPos(msg.payload);
                                break;
                            case "InstantiateBotsAtCoords":
                                DCL.Environment.i.platform.debugController.InstantiateBotsAtCoords(msg.payload);
                                break;
                            case "RemoveBot":
                                DCL.Environment.i.platform.debugController.RemoveBot(msg.payload);
                                break;
                            case "ClearBots":
                                DCL.Environment.i.platform.debugController.ClearBots();
                                break;
                            default:
                                Debug.Log(
                                    "<b><color=#FF0000>WSSController:</color></b> received an unknown message from kernel to renderer: " +
                                    msg.type);
                                break;
                        }
                    }

                    queuedMessagesDirty = false;
                }
            }
#endif
        }

        private Builder.DCLBuilderBridge GetBuilderBridge()
        {
            if (builderBridge == null)
            {
                builderBridge = FindObjectOfType<Builder.DCLBuilderBridge>();
            }

            return builderBridge;
        }

        private BuilderInWorldBridge GetBuilderInWorldBridge()
        {
            if (builderInWorldBridge == null)
            {
                builderInWorldBridge = FindObjectOfType<BuilderInWorldBridge>();
            }

            return builderInWorldBridge;
        }

        private HUDBridge GetHUDBridge()
        {
            if (hudBridge == null)
            {
                hudBridge = FindObjectOfType<HUDBridge>();
            }

            return hudBridge;
        }
    }
}