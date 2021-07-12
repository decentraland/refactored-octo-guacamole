using UnityEngine;
using UnityEngine.UI;
using DCL.Helpers;

namespace Builder
{
    [RequireComponent(typeof(Camera))]
    public class DCLBuilderOutline : MonoBehaviour
    {
        [SerializeField] Material OutlineMaterial = null;

        private Camera builderCamera;

        private Camera outlineCamera;
        private Canvas outlineCanvas;
        private RawImage outlineRawImage;

        private int lastScreenWidth = 0;
        private int lastScreenHeight = 0;

        private void Awake()
        {
            builderCamera = GetComponent<Camera>();

            outlineCamera = new GameObject("BuilderOutlineCamera").AddComponent<Camera>();
            outlineCamera.CopyFrom(builderCamera);
            outlineCamera.clearFlags = CameraClearFlags.SolidColor;
            outlineCamera.backgroundColor = Color.clear;
            outlineCamera.cullingMask = LayerMask.GetMask(DCLBuilderRaycast.LAYER_SELECTION);
            outlineCamera.depth = builderCamera.depth - 1;
            outlineCamera.transform.SetParent(builderCamera.transform);

            outlineCanvas = new GameObject("BuilderOutlineCanvas").AddComponent<Canvas>();
            outlineCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            outlineCanvas.worldCamera = builderCamera;
            outlineCanvas.planeDistance = 1;

            outlineRawImage = new GameObject("BuilderOutlineRawImage").AddComponent<RawImage>();
            outlineRawImage.transform.SetParent(outlineCanvas.transform);
            outlineRawImage.transform.ResetLocalTRS();

            outlineRawImage.rectTransform.sizeDelta = new Vector2(outlineCanvas.pixelRect.width, outlineCanvas.pixelRect.height);
            outlineRawImage.raycastTarget = false;
            outlineRawImage.material = OutlineMaterial;

            DCLBuilderBridge.OnPreviewModeChanged += OnPreviewModeChanged;
        }

        private void OnDestroy() { Dispose(); }

        public void Dispose()
        {
            if (outlineCanvas != null)
                Destroy(outlineCanvas.gameObject);
            if (outlineRawImage != null)
                Destroy(outlineRawImage.gameObject);
            if (outlineCamera != null)
                Destroy(outlineCamera.gameObject);
            DCLBuilderBridge.OnPreviewModeChanged -= OnPreviewModeChanged;
        }

        public void Activate()
        {
            outlineCamera.gameObject.SetActive(true);
            outlineCanvas.gameObject.SetActive(true);
            outlineRawImage.gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            outlineCamera.gameObject.SetActive(false);
            outlineCanvas.gameObject.SetActive(false);
            outlineRawImage.gameObject.SetActive(false);
        }

        public void SetOutlineMaterial(Material mat)
        {
            OutlineMaterial = mat;
            outlineRawImage.material = OutlineMaterial;
        }

        private void OnResize()
        {
            outlineRawImage.gameObject.SetActive(false); // Hack: force Unity to refresh the texture

            RenderTexture currentRenderTexture = outlineCamera.targetTexture;
            if (currentRenderTexture != null)
            {
                currentRenderTexture.Release();
                Object.Destroy(currentRenderTexture);
            }

            RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            renderTexture.antiAliasing = 4;
            renderTexture.depth = 16;
            renderTexture.autoGenerateMips = true;
            renderTexture.useMipMap = true;

            outlineCamera.targetTexture = renderTexture;
            OutlineMaterial.mainTexture = outlineCamera.targetTexture;
            outlineRawImage.rectTransform.sizeDelta = new Vector2(outlineCanvas.pixelRect.width, outlineCanvas.pixelRect.height);
            outlineRawImage.gameObject.SetActive(true);
        }

        private void OnPreviewModeChanged(bool isPreview) { outlineCanvas.gameObject.SetActive(!isPreview); }

        private void Update()
        {
            if ((Screen.width > 0 && Screen.height > 0) && (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height))
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                OnResize();
            }
        }
    }
}