using DCL.Camera;
using DCL.Helpers;
using UnityEngine;

namespace DCL
{
    public class InitialSceneReferences : MonoBehaviour
    {
        [SerializeField] private MouseCatcher mouseCatcherReference;
        [SerializeField] private GameObject groundVisualReference;
        [SerializeField] private GameObject cameraParentReference;
        [SerializeField] private InputController inputControllerReference;
        [SerializeField] private GameObject cursorCanvasReference;
        [SerializeField] private BuilderInWorldBridge builderInWorldBridgeReference;
        [SerializeField] private PlayerAvatarController playerAvatarControllerReference;
        [SerializeField] private CameraController cameraControllerReference;
        [SerializeField] private UnityEngine.Camera mainCameraReference;

        public GameObject groundVisual { get { return groundVisualReference; } }
        public GameObject cameraParent { get { return cameraParentReference; } }
        public GameObject cursorCanvas { get { return cursorCanvasReference; } }
        public MouseCatcher mouseCatcher { get { return mouseCatcherReference; } }
        public InputController inputController { get { return inputControllerReference; } }
        public BuilderInWorldBridge builderInWorldBridge { get { return builderInWorldBridgeReference; } }
        public PlayerAvatarController playerAvatarController { get { return playerAvatarControllerReference; } }
        public CameraController cameraController { get { return cameraControllerReference; } }
        public UnityEngine.Camera mainCamera { get { return mainCameraReference; } }

        public static InitialSceneReferences i { get; private set; }

        void Awake()
        {
            if (i != null)
            {
                Utils.SafeDestroy(this);
                return;
            }

            i = this;
        }

        void OnDestroy() { i = null; }
    }
}