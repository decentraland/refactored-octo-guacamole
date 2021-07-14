using System;
using System.Collections.Generic;
using DCL.Helpers;
using DCL.Models;
using UnityEngine;
using Ray = UnityEngine.Ray;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DCL.Interface
{
    /**
     * This class contains the outgoing interface of Decentraland.
     * You must call those functions to interact with the WebInterface.
     *
     * The messages comming from the WebInterface instead, are reported directly to
     * the handler GameObject by name.
     */
    public static class WebInterface
    {
        public static bool VERBOSE = false;
        public static System.Action<string, string> OnMessageFromEngine;

        [System.Serializable]
        private class ReportPositionPayload
        {
            /** Camera position, world space */
            public Vector3 position;

            /** Camera rotation */
            public Quaternion rotation;

            /** Camera height, relative to the feet of the avatar or ground */
            public float playerHeight;

            public Vector3 mousePosition;

            public string id;
        }

        [System.Serializable]
        public abstract class ControlEvent
        {
            public string eventType;
        }

        public abstract class ControlEvent<T> : ControlEvent
        {
            public T payload;

            protected ControlEvent(string eventType, T payload)
            {
                this.eventType = eventType;
                this.payload = payload;
            }
        }

        [System.Serializable]
        public class StartStatefulMode : ControlEvent<StartStatefulMode.Payload>
        {
            [System.Serializable]
            public class Payload
            {
                public string sceneId;
            }

            public StartStatefulMode(string sceneId) : base("StartStatefulMode", new Payload() { sceneId = sceneId }) { }
        }

        [System.Serializable]
        public class StopStatefulMode : ControlEvent<StopStatefulMode.Payload>
        {
            [System.Serializable]
            public class Payload
            {
                public string sceneId;
            }

            public StopStatefulMode(string sceneId) : base("StopStatefulMode", new Payload() { sceneId = sceneId }) { }
        }

        [System.Serializable]
        public class SceneReady : ControlEvent<SceneReady.Payload>
        {
            [System.Serializable]
            public class Payload
            {
                public string sceneId;
            }

            public SceneReady(string sceneId) : base("SceneReady", new Payload() { sceneId = sceneId }) { }
        }

        [System.Serializable]
        public class ActivateRenderingACK : ControlEvent<object>
        {
            public ActivateRenderingACK() : base("ActivateRenderingACK", null) { }
        }

        [System.Serializable]
        public class SceneEvent<T>
        {
            public string sceneId;
            public string eventType;
            public T payload;
        }

        [System.Serializable]
        public class AllScenesEvent<T>
        {
            public string eventType;
            public T payload;
        }

        [System.Serializable]
        public class UUIDEvent<TPayload>
            where TPayload : class, new()
        {
            public string uuid;
            public TPayload payload = new TPayload();
        }

        public enum ACTION_BUTTON
        {
            POINTER,
            PRIMARY,
            SECONDARY,
            ANY
        }

        [System.Serializable]
        public class OnClickEvent : UUIDEvent<OnClickEventPayload> { };

        [System.Serializable]
        public class CameraModePayload
        {
            public CameraMode.ModeId cameraMode;
        };

        [System.Serializable]
        public class IdleStateChangedPayload
        {
            public bool isIdle;
        };

        [System.Serializable]
        public class OnPointerDownEvent : UUIDEvent<OnPointerEventPayload> { };

        [System.Serializable]
        public class OnGlobalPointerEvent
        {
            public OnGlobalPointerEventPayload payload = new OnGlobalPointerEventPayload();
        };

        [System.Serializable]
        public class OnPointerUpEvent : UUIDEvent<OnPointerEventPayload> { };

        [System.Serializable]
        private class OnTextSubmitEvent : UUIDEvent<OnTextSubmitEventPayload> { };

        [System.Serializable]
        private class OnTextInputChangeEvent : UUIDEvent<OnTextInputChangeEventPayload> { };

        [System.Serializable]
        private class OnScrollChangeEvent : UUIDEvent<OnScrollChangeEventPayload> { };

        [System.Serializable]
        private class OnFocusEvent : UUIDEvent<EmptyPayload> { };

        [System.Serializable]
        private class OnBlurEvent : UUIDEvent<EmptyPayload> { };

        [System.Serializable]
        public class OnEnterEvent : UUIDEvent<OnEnterEventPayload> { };

        [System.Serializable]
        public class OnClickEventPayload
        {
            public ACTION_BUTTON buttonId = ACTION_BUTTON.POINTER;
        }

        [System.Serializable]
        public class SendChatMessageEvent
        {
            public ChatMessage message;
        }

        [System.Serializable]
        public class RemoveEntityComponentsPayLoad
        {
            public string entityId;
            public string componentId;
        };

        [System.Serializable]
        public class StoreSceneStateEvent
        {
            public string type = "StoreSceneState";
            public string payload = "";
        };

        [System.Serializable]
        public class OnPointerEventPayload
        {
            [System.Serializable]
            public class Hit
            {
                public Vector3 origin;
                public float length;
                public Vector3 hitPoint;
                public Vector3 normal;
                public Vector3 worldNormal;
                public string meshName;
                public string entityId;
            }

            public ACTION_BUTTON buttonId;
            public Vector3 origin;
            public Vector3 direction;
            public Hit hit;
        }

        [System.Serializable]
        public class OnGlobalPointerEventPayload : OnPointerEventPayload
        {
            public enum InputEventType
            {
                DOWN,
                UP
            }

            public InputEventType type;
        }

        [System.Serializable]
        public class OnTextSubmitEventPayload
        {
            public string id;
            public string text;
        }

        [System.Serializable]
        public class OnTextInputChangeEventPayload
        {
            public string value;
        }

        [System.Serializable]
        public class OnScrollChangeEventPayload
        {
            public Vector2 value;
            public int pointerId;
        }

        [System.Serializable]
        public class EmptyPayload { }

        [System.Serializable]
        public class MetricsModel
        {
            public int meshes;
            public int bodies;
            public int materials;
            public int textures;
            public int triangles;
            public int entities;

            public static MetricsModel operator + (MetricsModel lhs, MetricsModel rhs)
            {
                return new MetricsModel()
                {
                    meshes = lhs.meshes + rhs.meshes,
                    bodies = lhs.bodies + rhs.bodies,
                    materials = lhs.materials + rhs.materials,
                    textures = lhs.textures + rhs.textures,
                    triangles = lhs.triangles + rhs.triangles,
                    entities = lhs.entities + rhs.entities
                };
            }
        }

        [System.Serializable]
        private class OnMetricsUpdate
        {
            public MetricsModel given = new MetricsModel();
            public MetricsModel limit = new MetricsModel();
        }

        [System.Serializable]
        public class OnEnterEventPayload { }

        [System.Serializable]
        public class TransformPayload
        {
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;
        }

        public class OnSendScreenshot
        {
            public string id;
            public string encodedTexture;
        };

        [System.Serializable]
        public class GotoEvent
        {
            public int x;
            public int y;
        };

        [System.Serializable]
        public class BaseResolution
        {
            public int baseResolution;
        };

        //-----------------------------------------------------
        // Raycast
        [System.Serializable]
        public class RayInfo
        {
            public Vector3 origin;
            public Vector3 direction;
            public float distance;
        }

        [System.Serializable]
        public class RaycastHitInfo
        {
            public bool didHit;
            public RayInfo ray;

            public Vector3 hitPoint;
            public Vector3 hitNormal;
        }

        [System.Serializable]
        public class HitEntityInfo
        {
            public string entityId;
            public string meshName;
        }

        [System.Serializable]
        public class RaycastHitEntity : RaycastHitInfo
        {
            public HitEntityInfo entity;
        }

        [System.Serializable]
        public class RaycastHitEntities : RaycastHitInfo
        {
            public RaycastHitEntity[] entities;
        }

        [System.Serializable]
        public class RaycastResponse<T> where T : RaycastHitInfo
        {
            public string queryId;
            public string queryType;
            public T payload;
        }

        // Note (Zak): We need to explicitly define this classes for the JsonUtility to
        // be able to serialize them
        [System.Serializable]
        public class RaycastHitFirstResponse : RaycastResponse<RaycastHitEntity> { }

        [System.Serializable]
        public class RaycastHitAllResponse : RaycastResponse<RaycastHitEntities> { }

        [System.Serializable]
        public class SendExpressionPayload
        {
            public string id;
            public long timestamp;
        }

        [System.Serializable]
        public class UserAcceptedCollectiblesPayload
        {
            public string id;
        }

        [System.Serializable]
        public class SendBlockPlayerPayload
        {
            public string userId;
        }

        [System.Serializable]
        public class SendUnblockPlayerPayload
        {
            public string userId;
        }

        [System.Serializable]
        public class TutorialStepPayload
        {
            public int tutorialStep;
        }

        [System.Serializable]
        public class PerformanceReportPayload
        {
            public string samples;
            public bool fpsIsCapped;
            public int hiccupsInThousandFrames;
            public float hiccupsTime;
            public float totalTime;
        }

        [System.Serializable]
        public class SystemInfoReportPayload
        {
            public string graphicsDeviceName = SystemInfo.graphicsDeviceName;
            public string graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion;
            public int graphicsMemorySize = SystemInfo.graphicsMemorySize;
            public string processorType = SystemInfo.processorType;
            public int processorCount = SystemInfo.processorCount;
            public int systemMemorySize = SystemInfo.systemMemorySize;
        }

        [System.Serializable]
        public class GenericAnalyticPayload
        {
            public string eventName;
            public Dictionary<object, object> data;
        }

        [System.Serializable]
        public class PerformanceHiccupPayload
        {
            public int hiccupsInThousandFrames;
            public float hiccupsTime;
            public float totalTime;
        }

        [System.Serializable]
        public class TermsOfServiceResponsePayload
        {
            public string sceneId;
            public bool dontShowAgain;
            public bool accepted;
        }

        [System.Serializable]
        public class OpenURLPayload
        {
            public string url;
        }

        [System.Serializable]
        public class SendUserEmailPayload
        {
            public string userEmail;
        }

        [System.Serializable]
        public class GIFSetupPayload
        {
            public string imageSource;
            public string id;
            public bool isWebGL1;
        }

        [System.Serializable]
        public class RequestScenesInfoAroundParcelPayload
        {
            public Vector2 parcel;
            public int scenesAround;
        }

        [System.Serializable]
        public class AudioStreamingPayload
        {
            public string url;
            public bool play;
            public float volume;
        }

        [System.Serializable]
        public class SetScenesLoadRadiusPayload
        {
            public float newRadius;
        }

        [System.Serializable]
        public class SetVoiceChatRecordingPayload
        {
            public bool recording;
        }

        [System.Serializable]
        public class ApplySettingsPayload
        {
            public float voiceChatVolume;
            public int voiceChatAllowCategory;
        }

        [System.Serializable]
        public class JumpInPayload
        {
            public FriendsController.UserStatus.Realm realm = new FriendsController.UserStatus.Realm();
            public Vector2 gridPosition;
        }

        [System.Serializable]
        public class LoadingFeedbackMessage
        {
            public string message;
            public int loadPercentage;
        }

        [System.Serializable]
        public class AnalyticsPayload
        {
            [System.Serializable]
            public class Property
            {
                public string key;
                public string value;

                public Property(string key, string value)
                {
                    this.key = key;
                    this.value = value;
                }
            }

            public string name;
            public Property[] properties;
        }

        [System.Serializable]
        public class DelightedSurveyEnabledPayload
        {
            public bool enabled;
        }

        [System.Serializable]
        public class ExternalActionSceneEventPayload
        {
            public string type;
            public string payload;
        }

        [System.Serializable]
        public class MuteUserPayload
        {
            public string[] usersId;
            public bool mute;
        }

        [System.Serializable]
        public class CloseUserAvatarPayload
        {
            public bool isSignUpFlow;
        }

        [System.Serializable]
        public class StringPayload
        {
            public string value;
        }

        [System.Serializable]
        public class KillPortableExperiencePayload
        {
            public string portableExperienceId;
        }

        [System.Serializable]
        public class WearablesRequestFiltersPayload
        {
            public string ownedByUser;
            public string[] wearableIds;
            public string[] collectionIds;
        }

        [System.Serializable]
        public class RequestWearablesPayload
        {
            public WearablesRequestFiltersPayload filters;
            public string context;
        }

        [System.Serializable]
        public class SearchENSOwnerPayload
        {
            public string name;
            public int maxResults;
        }

        [System.Serializable]
        public class UnpublishScenePayload
        {
            public string coordinates;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
    /**
     * This method is called after the first render. It marks the loading of the
     * rest of the JS client.
     */
    [DllImport("__Internal")] public static extern void StartDecentraland();
    [DllImport("__Internal")] public static extern void MessageFromEngine(string type, string message);
    [DllImport("__Internal")] public static extern string GetGraphicCard();
#else
        public static void StartDecentraland() { }

        public static void MessageFromEngine(string type, string message)
        {
            if (OnMessageFromEngine != null)
            {
                OnMessageFromEngine.Invoke(type, message);
            }

            if (VERBOSE)
            {
                Debug.Log("MessageFromEngine called with: " + type + ", " + message);
            }
        }

        public static string GetGraphicCard() => "In Editor Graphic Card";
#endif

        public static void SendMessage(string type)
        {
            // sending an empty JSON object to be compatible with other messages
            MessageFromEngine(type, "{}");
        }

        public static void SendMessage<T>(string type, T message)
        {
            string messageJson = JsonUtility.ToJson(message);

            if (VERBOSE)
            {
                Debug.Log($"Sending message: " + messageJson);
            }

            MessageFromEngine(type, messageJson);
        }

        private static ReportPositionPayload positionPayload = new ReportPositionPayload();
        private static CameraModePayload cameraModePayload = new CameraModePayload();
        private static IdleStateChangedPayload idleStateChangedPayload = new IdleStateChangedPayload();
        private static OnMetricsUpdate onMetricsUpdate = new OnMetricsUpdate();
        private static OnClickEvent onClickEvent = new OnClickEvent();
        private static OnPointerDownEvent onPointerDownEvent = new OnPointerDownEvent();
        private static OnPointerUpEvent onPointerUpEvent = new OnPointerUpEvent();
        private static OnTextSubmitEvent onTextSubmitEvent = new OnTextSubmitEvent();
        private static OnTextInputChangeEvent onTextInputChangeEvent = new OnTextInputChangeEvent();
        private static OnScrollChangeEvent onScrollChangeEvent = new OnScrollChangeEvent();
        private static OnFocusEvent onFocusEvent = new OnFocusEvent();
        private static OnBlurEvent onBlurEvent = new OnBlurEvent();
        private static OnEnterEvent onEnterEvent = new OnEnterEvent();
        private static OnSendScreenshot onSendScreenshot = new OnSendScreenshot();
        private static OnPointerEventPayload onPointerEventPayload = new OnPointerEventPayload();
        private static OnGlobalPointerEventPayload onGlobalPointerEventPayload = new OnGlobalPointerEventPayload();
        private static OnGlobalPointerEvent onGlobalPointerEvent = new OnGlobalPointerEvent();
        private static AudioStreamingPayload onAudioStreamingEvent = new AudioStreamingPayload();
        private static SetVoiceChatRecordingPayload setVoiceChatRecordingPayload = new SetVoiceChatRecordingPayload();
        private static SetScenesLoadRadiusPayload setScenesLoadRadiusPayload = new SetScenesLoadRadiusPayload();
        private static ApplySettingsPayload applySettingsPayload = new ApplySettingsPayload();
        private static GIFSetupPayload gifSetupPayload = new GIFSetupPayload();
        private static JumpInPayload jumpInPayload = new JumpInPayload();
        private static GotoEvent gotoEvent = new GotoEvent();
        private static SendChatMessageEvent sendChatMessageEvent = new SendChatMessageEvent();
        private static BaseResolution baseResEvent = new BaseResolution();
        private static AnalyticsPayload analyticsEvent = new AnalyticsPayload();
        private static DelightedSurveyEnabledPayload delightedSurveyEnabled = new DelightedSurveyEnabledPayload();
        private static ExternalActionSceneEventPayload sceneExternalActionEvent = new ExternalActionSceneEventPayload();
        private static MuteUserPayload muteUserEvent = new MuteUserPayload();
        private static StoreSceneStateEvent storeSceneState = new StoreSceneStateEvent();
        private static CloseUserAvatarPayload closeUserAvatarPayload = new CloseUserAvatarPayload();
        private static StringPayload stringPayload = new StringPayload();
        private static KillPortableExperiencePayload killPortableExperiencePayload = new KillPortableExperiencePayload();
        private static RequestWearablesPayload requestWearablesPayload = new RequestWearablesPayload();
        private static SearchENSOwnerPayload searchEnsOwnerPayload = new SearchENSOwnerPayload();

        public static void SendSceneEvent<T>(string sceneId, string eventType, T payload)
        {
            SceneEvent<T> sceneEvent = new SceneEvent<T>();
            sceneEvent.sceneId = sceneId;
            sceneEvent.eventType = eventType;
            sceneEvent.payload = payload;

            SendMessage("SceneEvent", sceneEvent);
        }

        private static void SendAllScenesEvent<T>(string eventType, T payload)
        {
            AllScenesEvent<T> allScenesEvent = new AllScenesEvent<T>();
            allScenesEvent.eventType = eventType;
            allScenesEvent.payload = payload;

            SendMessage("AllScenesEvent", allScenesEvent);
        }

        public static void ReportPosition(Vector3 position, Quaternion rotation, float playerHeight)
        {
            positionPayload.position = position;
            positionPayload.rotation = rotation;
            positionPayload.playerHeight = playerHeight;

            SendMessage("ReportPosition", positionPayload);
        }

        public static void ReportCameraChanged(CameraMode.ModeId cameraMode)
        {
            cameraModePayload.cameraMode = cameraMode;
            SendAllScenesEvent("cameraModeChanged", cameraModePayload);
        }

        public static void ReportIdleStateChanged(bool isIdle)
        {
            idleStateChangedPayload.isIdle = isIdle;
            SendAllScenesEvent("idleStateChanged", idleStateChangedPayload);
        }

        public static void ReportControlEvent<T>(T controlEvent) where T : ControlEvent { SendMessage("ControlEvent", controlEvent); }

        public static void BuilderInWorldMessage(string type, string message) { MessageFromEngine(type, message); }

        public static void ReportOnClickEvent(string sceneId, string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            onClickEvent.uuid = uuid;

            SendSceneEvent(sceneId, "uuidEvent", onClickEvent);
        }

        private static void ReportRaycastResult<T, P>(string sceneId, string queryId, string queryType, P payload) where T : RaycastResponse<P>, new() where P : RaycastHitInfo
        {
            T response = new T();
            response.queryId = queryId;
            response.queryType = queryType;
            response.payload = payload;

            SendSceneEvent<T>(sceneId, "raycastResponse", response);
        }

        public static void ReportRaycastHitFirstResult(string sceneId, string queryId, RaycastType raycastType, RaycastHitEntity payload) { ReportRaycastResult<RaycastHitFirstResponse, RaycastHitEntity>(sceneId, queryId, Protocol.RaycastTypeToLiteral(raycastType), payload); }

        public static void ReportRaycastHitAllResult(string sceneId, string queryId, RaycastType raycastType, RaycastHitEntities payload) { ReportRaycastResult<RaycastHitAllResponse, RaycastHitEntities>(sceneId, queryId, Protocol.RaycastTypeToLiteral(raycastType), payload); }

        private static OnPointerEventPayload.Hit CreateHitObject(string entityId, string meshName, Vector3 point, Vector3 normal, float distance)
        {
            OnPointerEventPayload.Hit hit = new OnPointerEventPayload.Hit();

            hit.hitPoint = point;
            hit.length = distance;
            hit.normal = normal;
            hit.worldNormal = normal;
            hit.meshName = meshName;
            hit.entityId = entityId;

            return hit;
        }

        private static void SetPointerEventPayload(OnPointerEventPayload pointerEventPayload, ACTION_BUTTON buttonId, string entityId, string meshName, Ray ray, Vector3 point, Vector3 normal, float distance, bool isHitInfoValid)
        {
            pointerEventPayload.origin = ray.origin;
            pointerEventPayload.direction = ray.direction;
            pointerEventPayload.buttonId = buttonId;

            if (isHitInfoValid)
                pointerEventPayload.hit = CreateHitObject(entityId, meshName, point, normal, distance);
            else
                pointerEventPayload.hit = null;
        }

        public static void ReportGlobalPointerDownEvent(ACTION_BUTTON buttonId, Ray ray, Vector3 point, Vector3 normal, float distance, string sceneId, string entityId = null, string meshName = null, bool isHitInfoValid = false)
        {
            SetPointerEventPayload((OnPointerEventPayload) onGlobalPointerEventPayload, buttonId, entityId, meshName, ray, point, normal, distance, isHitInfoValid);
            onGlobalPointerEventPayload.type = OnGlobalPointerEventPayload.InputEventType.DOWN;

            onGlobalPointerEvent.payload = onGlobalPointerEventPayload;

            SendSceneEvent(sceneId, "pointerEvent", onGlobalPointerEvent);
        }

        public static void ReportGlobalPointerUpEvent(ACTION_BUTTON buttonId, Ray ray, Vector3 point, Vector3 normal, float distance, string sceneId, string entityId = null, string meshName = null, bool isHitInfoValid = false)
        {
            SetPointerEventPayload((OnPointerEventPayload) onGlobalPointerEventPayload, buttonId, entityId, meshName, ray, point, normal, distance, isHitInfoValid);
            onGlobalPointerEventPayload.type = OnGlobalPointerEventPayload.InputEventType.UP;

            onGlobalPointerEvent.payload = onGlobalPointerEventPayload;

            SendSceneEvent(sceneId, "pointerEvent", onGlobalPointerEvent);
        }

        public static void ReportOnPointerDownEvent(ACTION_BUTTON buttonId, string sceneId, string uuid, string entityId, string meshName, Ray ray, Vector3 point, Vector3 normal, float distance)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            onPointerDownEvent.uuid = uuid;
            SetPointerEventPayload(onPointerEventPayload, buttonId, entityId, meshName, ray, point, normal, distance, isHitInfoValid: true);
            onPointerDownEvent.payload = onPointerEventPayload;

            SendSceneEvent(sceneId, "uuidEvent", onPointerDownEvent);
        }

        public static void ReportOnPointerUpEvent(ACTION_BUTTON buttonId, string sceneId, string uuid, string entityId, string meshName, Ray ray, Vector3 point, Vector3 normal, float distance)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            onPointerUpEvent.uuid = uuid;
            SetPointerEventPayload(onPointerEventPayload, buttonId, entityId, meshName, ray, point, normal, distance, isHitInfoValid: true);
            onPointerUpEvent.payload = onPointerEventPayload;

            SendSceneEvent(sceneId, "uuidEvent", onPointerUpEvent);
        }

        public static void ReportOnTextSubmitEvent(string sceneId, string uuid, string text)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            onTextSubmitEvent.uuid = uuid;
            onTextSubmitEvent.payload.text = text;

            SendSceneEvent(sceneId, "uuidEvent", onTextSubmitEvent);
        }

        public static void ReportOnTextInputChangedEvent(string sceneId, string uuid, string text)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            onTextInputChangeEvent.uuid = uuid;
            onTextInputChangeEvent.payload.value = text;

            SendSceneEvent(sceneId, "uuidEvent", onTextInputChangeEvent);
        }

        public static void ReportOnFocusEvent(string sceneId, string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            onFocusEvent.uuid = uuid;
            SendSceneEvent(sceneId, "uuidEvent", onFocusEvent);
        }

        public static void ReportOnBlurEvent(string sceneId, string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            onBlurEvent.uuid = uuid;
            SendSceneEvent(sceneId, "uuidEvent", onBlurEvent);
        }

        public static void ReportOnScrollChange(string sceneId, string uuid, Vector2 value, int pointerId)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            onScrollChangeEvent.uuid = uuid;
            onScrollChangeEvent.payload.value = value;
            onScrollChangeEvent.payload.pointerId = pointerId;

            SendSceneEvent(sceneId, "uuidEvent", onScrollChangeEvent);
        }

        public static void ReportEvent<T>(string sceneId, T @event) { SendSceneEvent(sceneId, "uuidEvent", @event); }

        public static void ReportOnMetricsUpdate(string sceneId, MetricsModel current,
            MetricsModel limit)
        {
            onMetricsUpdate.given = current;
            onMetricsUpdate.limit = limit;

            SendSceneEvent(sceneId, "metricsUpdate", onMetricsUpdate);
        }

        public static void ReportOnEnterEvent(string sceneId, string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
                return;

            onEnterEvent.uuid = uuid;

            SendSceneEvent(sceneId, "uuidEvent", onEnterEvent);
        }

        public static void LogOut() { SendMessage("LogOut"); }

        public static void RedirectToSignUp() { SendMessage("RedirectToSignUp"); }

        public static void PreloadFinished(string sceneId) { SendMessage("PreloadFinished", sceneId); }

        public static void ReportMousePosition(Vector3 mousePosition, string id)
        {
            positionPayload.mousePosition = mousePosition;
            positionPayload.id = id;
            SendMessage("ReportMousePosition", positionPayload);
        }

        public static void SendScreenshot(string encodedTexture, string id)
        {
            onSendScreenshot.encodedTexture = encodedTexture;
            onSendScreenshot.id = id;
            SendMessage("SendScreenshot", onSendScreenshot);
        }

        public static void SetDelightedSurveyEnabled(bool enabled)
        {
            delightedSurveyEnabled.enabled = enabled;
            SendMessage("SetDelightedSurveyEnabled", delightedSurveyEnabled);
        }

        public static void SetScenesLoadRadius(float newRadius)
        {
            setScenesLoadRadiusPayload.newRadius = newRadius;
            SendMessage("SetScenesLoadRadius", setScenesLoadRadiusPayload);
        }

        [System.Serializable]
        public class SaveAvatarPayload
        {
            public string face;
            public string face128;
            public string face256;
            public string body;
            public bool isSignUpFlow;
            public AvatarModel avatar;
        }

        public static class RendererAuthenticationType
        {
            public static string Guest => "guest";
            public static string WalletConnect => "wallet_connect";
        }

        [System.Serializable]
        public class SendAuthenticationPayload
        {
            public string rendererAuthenticationType;
        }

        [System.Serializable]
        public class SendPassportPayload
        {
            public string name;
            public string email;
        }

        [System.Serializable]
        public class SendSaveUserUnverifiedNamePayload
        {
            public string newUnverifiedName;
        }

        public static void RequestOwnProfileUpdate() { SendMessage("RequestOwnProfileUpdate"); }

        public static void SendSaveAvatar(AvatarModel avatar, Texture2D faceSnapshot, Texture2D face128Snapshot, Texture2D face256Snapshot, Texture2D bodySnapshot, bool isSignUpFlow = false)
        {
            var payload = new SaveAvatarPayload()
            {
                avatar = avatar,
                face = System.Convert.ToBase64String(faceSnapshot.EncodeToPNG()),
                face128 = System.Convert.ToBase64String(face128Snapshot.EncodeToPNG()),
                face256 = System.Convert.ToBase64String(face256Snapshot.EncodeToPNG()),
                body = System.Convert.ToBase64String(bodySnapshot.EncodeToPNG()),
                isSignUpFlow = isSignUpFlow
            };
            SendMessage("SaveUserAvatar", payload);
        }

        public static void SendAuthentication(string rendererAuthenticationType) { SendMessage("SendAuthentication", new SendAuthenticationPayload { rendererAuthenticationType = rendererAuthenticationType }); }

        public static void SendPassport(string name, string email) { SendMessage("SendPassport", new SendPassportPayload { name = name, email = email }); }

        public static void SendSaveUserUnverifiedName(string newName)
        {
            var payload = new SendSaveUserUnverifiedNamePayload()
            {
                newUnverifiedName = newName
            };

            SendMessage("SaveUserUnverifiedName", payload);
        }

        public static void SendUserAcceptedCollectibles(string airdropId) { SendMessage("UserAcceptedCollectibles", new UserAcceptedCollectiblesPayload { id = airdropId }); }

        public static void SaveUserTutorialStep(int newTutorialStep) { SendMessage("SaveUserTutorialStep", new TutorialStepPayload() { tutorialStep = newTutorialStep }); }

        public static void SendPerformanceReport(string encodedFrameTimesInMS, bool usingFPSCap, int hiccupsInThousandFrames, float hiccupsTime, float totalTime)
        {
            SendMessage("PerformanceReport", new PerformanceReportPayload()
            {
                samples = encodedFrameTimesInMS,
                fpsIsCapped = usingFPSCap,
                hiccupsInThousandFrames = hiccupsInThousandFrames,
                hiccupsTime = hiccupsTime,
                totalTime = totalTime
            });
        }

        public static void SendSystemInfoReport() { SendMessage("SystemInfoReport", new SystemInfoReportPayload()); }

        public static void SendTermsOfServiceResponse(string sceneId, bool accepted, bool dontShowAgain)
        {
            var payload = new TermsOfServiceResponsePayload()
            {
                sceneId = sceneId,
                accepted = accepted,
                dontShowAgain = dontShowAgain
            };
            SendMessage("TermsOfServiceResponse", payload);
        }

        public static void SendExpression(string expressionID, long timestamp)
        {
            SendMessage("TriggerExpression", new SendExpressionPayload()
            {
                id = expressionID,
                timestamp = timestamp
            });
        }

        public static void ReportMotdClicked() { SendMessage("MotdConfirmClicked"); }

        public static void OpenURL(string url) { SendMessage("OpenWebURL", new OpenURLPayload { url = url }); }

        public static void SendReportScene(string sceneID) { SendMessage("ReportScene", sceneID); }

        public static void SendReportPlayer(string playerName) { SendMessage("ReportPlayer", playerName); }

        public static void SendBlockPlayer(string userId)
        {
            SendMessage("BlockPlayer", new SendBlockPlayerPayload()
            {
                userId = userId
            });
        }

        public static void SendUnblockPlayer(string userId)
        {
            SendMessage("UnblockPlayer", new SendUnblockPlayerPayload()
            {
                userId = userId
            });
        }

        public static void SendUserEmail(string email)
        {
            SendMessage("ReportUserEmail", new SendUserEmailPayload()
            {
                userEmail = email
            });
        }

        public static void RequestScenesInfoAroundParcel(Vector2 parcel, int maxScenesArea)
        {
            SendMessage("RequestScenesInfoInArea", new RequestScenesInfoAroundParcelPayload()
            {
                parcel = parcel,
                scenesAround = maxScenesArea
            });
        }

        public static void SendAudioStreamEvent(string url, bool play, float volume)
        {
            onAudioStreamingEvent.url = url;
            onAudioStreamingEvent.play = play;
            onAudioStreamingEvent.volume = volume;
            SendMessage("SetAudioStream", onAudioStreamingEvent);
        }

        public static void SendSetVoiceChatRecording(bool recording)
        {
            setVoiceChatRecordingPayload.recording = recording;
            SendMessage("SetVoiceChatRecording", setVoiceChatRecordingPayload);
        }

        public static void ToggleVoiceChatRecording() { SendMessage("ToggleVoiceChatRecording"); }

        public static void ApplySettings(float voiceChatVolume, int voiceChatAllowCategory)
        {
            applySettingsPayload.voiceChatVolume = voiceChatVolume;
            applySettingsPayload.voiceChatAllowCategory = voiceChatAllowCategory;
            SendMessage("ApplySettings", applySettingsPayload);
        }

        public static void RequestGIFProcessor(string gifURL, string gifId, bool isWebGL1)
        {
            gifSetupPayload.imageSource = gifURL;
            gifSetupPayload.id = gifId;
            gifSetupPayload.isWebGL1 = isWebGL1;

            SendMessage("RequestGIFProcessor", gifSetupPayload);
        }

        public static void DeleteGIF(string id)
        {
            stringPayload.value = id;
            SendMessage("DeleteGIF", stringPayload);
        }

        public static void GoTo(int x, int y)
        {
            gotoEvent.x = x;
            gotoEvent.y = y;
            SendMessage("GoTo", gotoEvent);
        }

        public static void GoToCrowd() { SendMessage("GoToCrowd"); }

        public static void GoToMagic() { SendMessage("GoToMagic"); }

        public static void JumpIn(int x, int y, string serverName, string layerName)
        {
            jumpInPayload.realm.serverName = serverName;
            jumpInPayload.realm.layer = layerName;

            jumpInPayload.gridPosition.x = x;
            jumpInPayload.gridPosition.y = y;

            SendMessage("JumpIn", jumpInPayload);
        }

        public static void SendChatMessage(ChatMessage message)
        {
            sendChatMessageEvent.message = message;
            SendMessage("SendChatMessage", sendChatMessageEvent);
        }

        public static void UpdateFriendshipStatus(FriendsController.FriendshipUpdateStatusMessage message) { SendMessage("UpdateFriendshipStatus", message); }

        public static void ScenesLoadingFeedback(LoadingFeedbackMessage message) { SendMessage("ScenesLoadingFeedback", message); }

        public static void FetchHotScenes() { SendMessage("FetchHotScenes"); }

        public static void SetBaseResolution(int resolution)
        {
            baseResEvent.baseResolution = resolution;
            SendMessage("SetBaseResolution", baseResEvent);
        }

        public static void ReportAnalyticsEvent(string eventName) { ReportAnalyticsEvent(eventName, null); }

        public static void ReportAnalyticsEvent(string eventName, AnalyticsPayload.Property[] eventProperties)
        {
            analyticsEvent.name = eventName;
            analyticsEvent.properties = eventProperties;
            SendMessage("Track", analyticsEvent);
        }

        public static void FetchBalanceOfMANA() { SendMessage("FetchBalanceOfMANA"); }

        public static void SendSceneExternalActionEvent(string sceneId, string type, string payload)
        {
            sceneExternalActionEvent.type = type;
            sceneExternalActionEvent.payload = payload;
            SendSceneEvent(sceneId, "externalAction", sceneExternalActionEvent);
        }

        public static void SetMuteUsers(string[] usersId, bool mute)
        {
            muteUserEvent.usersId = usersId;
            muteUserEvent.mute = mute;
            SendMessage("SetMuteUsers", muteUserEvent);
        }

        public static void SendCloseUserAvatar(bool isSignUpFlow)
        {
            closeUserAvatarPayload.isSignUpFlow = isSignUpFlow;
            SendMessage("CloseUserAvatar", closeUserAvatarPayload);
        }

        public static void KillPortableExperience(string portableExperienceId)
        {
            killPortableExperiencePayload.portableExperienceId = portableExperienceId;
            SendMessage("KillPortableExperience", killPortableExperiencePayload);
        }

        public static void RequestWearables(
            string ownedByUser,
            string[] wearableIds,
            string[] collectionIds,
            string context)
        {
            requestWearablesPayload.filters = new WearablesRequestFiltersPayload
            {
                ownedByUser = ownedByUser,
                wearableIds = wearableIds,
                collectionIds = collectionIds
            };

            requestWearablesPayload.context = context;

            SendMessage("RequestWearables", requestWearablesPayload);
        }

        public static void SearchENSOwner(string name, int maxResults)
        {
            searchEnsOwnerPayload.name = name;
            searchEnsOwnerPayload.maxResults = maxResults;

            SendMessage("SearchENSOwner", searchEnsOwnerPayload);
        }

        public static void RequestUserProfile(string userId)
        {
            stringPayload.value = userId;
            SendMessage("RequestUserProfile", stringPayload);
        }

        public static void ReportAvatarFatalError() { SendMessage("ReportAvatarFatalError"); }

        public static void UnpublishScene(Vector2Int sceneCoordinates)
        {
            var payload = new UnpublishScenePayload() { coordinates = $"{sceneCoordinates.x},{sceneCoordinates.y}" };
            SendMessage("UnpublishScene", payload);
        }

        public static void NotifyStatusThroughChat(string message)
        {
            stringPayload.value = message;
            SendMessage("NotifyStatusThroughChat", stringPayload);
        }
    }
}