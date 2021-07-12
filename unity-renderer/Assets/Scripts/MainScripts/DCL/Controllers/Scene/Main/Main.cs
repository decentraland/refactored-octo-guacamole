using System;
using DCL.Components;
using DCL.Controllers;
using DCL.Helpers;
using UnityEngine;
using UnityEngine.Serialization;

namespace DCL
{
    /// <summary>
    /// This is the InitialScene entry point.
    /// Most of the application subsystems should be initialized from this class Awake() event.
    /// </summary>
    public class Main : MonoBehaviour
    {
        public static Main i { get; private set; }

        public PoolableComponentFactory componentFactory;
        public GameObject builderInWorldPrefab;

        public DebugConfig debugConfig;

        private PerformanceMetricsController performanceMetricsController;
        private EntryPoint_World worldEntryPoint;

        private FeatureController featureController;

        void Awake()
        {
            if (i != null)
            {
                Utils.SafeDestroy(this);
                return;
            }

            i = this;

            DataStore.i.debugConfig.soloScene = debugConfig.soloScene;
            DataStore.i.debugConfig.soloSceneCoords = debugConfig.soloSceneCoords;
            DataStore.i.debugConfig.ignoreGlobalScenes = debugConfig.ignoreGlobalScenes;
            DataStore.i.debugConfig.msgStepByStep = debugConfig.msgStepByStep;

            if (!Configuration.EnvironmentSettings.RUNNING_TESTS)
            {
                performanceMetricsController = new PerformanceMetricsController();
                RenderProfileManifest.i.Initialize();
                SetupEnvironment();
                featureController = new FeatureController();
                featureController.SetBuilderInWorldPrefab(builderInWorldPrefab);
            }

            DCL.Interface.WebInterface.SendSystemInfoReport();

#if !UNITY_EDITOR
            Debug.Log("DCL Unity Build Version: " + DCL.Configuration.ApplicationSettings.version);
            Debug.unityLogger.logEnabled = false;

            worldEntryPoint = new EntryPoint_World(Environment.i.world.sceneController);
#endif

            // TODO(Brian): This is a temporary fix to address elevators issue in the xmas event.
            // We should re-enable this later as produces a performance regression.
            if (!Configuration.EnvironmentSettings.RUNNING_TESTS)
                Environment.i.platform.cullingController.SetAnimationCulling(false);
        }

        protected virtual void SetupEnvironment()
        {
            Environment.SetupWithBuilders(
                messagingBuilder: MessagingContextBuilder,
                platformBuilder: PlatformContextBuilder,
                worldRuntimeBuilder: WorldRuntimeContextBuilder,
                hudBuilder: HUDContextBuilder);
        }
        protected virtual MessagingContext MessagingContextBuilder() { return MessagingContextFactory.CreateDefault(); }
        protected virtual PlatformContext PlatformContextBuilder() { return PlatformContextFactory.CreateDefault(); }
        protected virtual WorldRuntimeContext WorldRuntimeContextBuilder() { return WorldRuntimeContextFactory.CreateDefault(componentFactory); }
        protected virtual HUDContext HUDContextBuilder() { return HUDContextFactory.CreateDefault(); }
        private void Start()
        {
            Environment.i.world.sceneController.Start();
            featureController?.Start();
        }

        private void Update()
        {
            Environment.i.platform.idleChecker.Update();
            Environment.i.world.sceneController.Update();
            performanceMetricsController?.Update();
            featureController?.Update();
        }

        private void LateUpdate() { Environment.i.world.sceneController.LateUpdate(); }

        private void OnDestroy()
        {
            if (!Configuration.EnvironmentSettings.RUNNING_TESTS)
                Environment.Dispose();
            featureController?.OnDestroy();
        }

        #region RuntimeMessagingBridge

        public void LoadParcelScenes(string payload) { Environment.i.world.sceneController.LoadParcelScenes(payload); }

        public void SendSceneMessage(string payload) { Environment.i.world.sceneController.SendSceneMessage(payload); }

        public void UnloadScene(string sceneId) { Environment.i.world.sceneController.UnloadScene(sceneId); }

        public void CreateGlobalScene(string payload) { Environment.i.world.sceneController.CreateGlobalScene(payload); }

        public void UpdateParcelScenes(string payload) { Environment.i.world.sceneController.UpdateParcelScenes(payload); }

        #endregion

        public void BuilderReady() { UnityEngine.SceneManagement.SceneManager.LoadScene("BuilderScene", UnityEngine.SceneManagement.LoadSceneMode.Additive); }
    }
}