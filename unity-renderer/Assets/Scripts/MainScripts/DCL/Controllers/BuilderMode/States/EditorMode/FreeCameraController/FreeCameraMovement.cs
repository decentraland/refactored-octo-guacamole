using Builder.Gizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DCL.Camera
{
    public class FreeCameraMovement : CameraStateBase
    {
        private const float CAMERA_ANGLE_THRESHOLD = 0.01f;
        private const float CAMERA_PAN_THRESHOLD = 0.001f;
        private const float CAMERA_MOVEMENT_THRESHOLD = 0.001f;

        private const float CAMERA_MOVEMENT_DEACTIVATE_INPUT_MAGNITUD = 0.001f;

        private const int SCENE_SNAPSHOT_WIDTH_RES = 854;
        private const int SCENE_SNAPSHOT_HEIGHT_RES = 480;

        public float focusDistance = 5f;

        [Header("Camera Movement")]
        public float xPlaneSpeedPercentCompensantion = 0.85f;

        public float movementSpeed = 5f;
        public float lerpTime = 0.3F;

        public float lerpDeccelerationTime = 0.3F;
        public float initalAcceleration = 2f;
        public float speedClamp = 1f;

        [Header("Camera Look")]
        public float smoothLookAtSpeed = 5f;

        public float smoothCameraLookSpeed = 5f;
        public float lookSpeedH = 2f;
        public float lookSpeedV = 2f;

        [Header("Camera Pan")]
        public float smoothCameraPanAceleration = 5f;

        public float dragSpeed = 3f;

        [Header("Camera Zoom")]
        public float zoomSpeed = 2f;

        [Header("InputActions")]
        [SerializeField] internal InputAction_Hold advanceFowardInputAction;

        [SerializeField] internal InputAction_Hold advanceBackInputAction;
        [SerializeField] internal InputAction_Hold advanceLeftInputAction;
        [SerializeField] internal InputAction_Hold advanceRightInputAction;
        [SerializeField] internal InputAction_Hold advanceUpInputAction;
        [SerializeField] internal InputAction_Hold advanceDownInputAction;
        [SerializeField] internal InputAction_Hold cameraPanInputAction;
        [SerializeField] internal InputAction_Trigger zoomInFromKeyboardInputAction;
        [SerializeField] internal InputAction_Trigger zoomOutFromKeyboardInputAction;

        private Vector3 direction = Vector3.zero;

        private float yaw = 0f;
        private float pitch = 0f;

        private float panAxisX = 0f;
        private float panAxisY = 0f;

        private bool isCameraAbleToMove = true;

        private bool isAdvancingForward = false;
        private bool isAdvancingBackward = false;
        private bool isAdvancingLeft = false;
        private bool isAdvancingRight = false;
        private bool isAdvancingUp = false;
        private bool isAdvancingDown = false;

        private bool isDetectingMovement = false;
        private bool hasBeenMovement = false;

        private bool isPanCameraActive = false;
        private bool isMouseRightClickDown = false;

        private Coroutine smoothLookAtCor;
        private Coroutine smoothFocusOnTargetCor;
        private Coroutine smoothScrollCor;

        private InputAction_Hold.Started advanceForwardStartDelegate;
        private InputAction_Hold.Finished advanceForwardFinishedDelegate;

        private InputAction_Hold.Started advanceBackStartDelegate;
        private InputAction_Hold.Finished advanceBackFinishedDelegate;

        private InputAction_Hold.Started advanceLeftStartDelegate;
        private InputAction_Hold.Finished advanceLeftFinishedDelegate;

        private InputAction_Hold.Started advanceRightStartDelegate;
        private InputAction_Hold.Finished advanceRightFinishedDelegate;

        private InputAction_Hold.Started advanceDownStartDelegate;
        private InputAction_Hold.Finished advanceDownFinishedDelegate;

        private InputAction_Hold.Started advanceUpStartDelegate;
        private InputAction_Hold.Finished advanceUpFinishedDelegate;

        private InputAction_Hold.Started cameraPanStartDelegate;
        private InputAction_Hold.Finished cameraPanFinishedDelegate;

        private InputAction_Trigger.Triggered zoomInFromKeyboardDelegate;
        private InputAction_Trigger.Triggered zoomOutFromKeyboardDelegate;

        private Vector3 nextTranslation;
        private Vector3 originalCameraPosition;
        private Vector3 cameraVelocity = Vector3.zero;
        private Transform originalCameraLookAt;

        private float lastMouseWheelTime;
        private float cameraPanAdvance;
        private float cameraLookAdvance;

        public delegate void OnSnapshotsReady(Texture2D sceneSnapshot);

        private void Awake()
        {
            BuilderInWorldInputWrapper.OnMouseDrag += MouseDrag;
            BuilderInWorldInputWrapper.OnMouseDragRaw += MouseDragRaw;
            BuilderInWorldInputWrapper.OnMouseWheel += MouseWheel;

            BuilderInWorldInputWrapper.OnMouseDown += OnInputMouseDown;
            BuilderInWorldInputWrapper.OnMouseUp += OnInputMouseUp;

            DCLBuilderGizmoManager.OnGizmoTransformObjectStart += OnGizmoTransformObjectStart;
            DCLBuilderGizmoManager.OnGizmoTransformObjectEnd += OnGizmoTransformObjectEnd;

            advanceForwardStartDelegate = (action) => isAdvancingForward = true;
            advanceForwardFinishedDelegate = (action) => isAdvancingForward = false;

            advanceFowardInputAction.OnStarted += advanceForwardStartDelegate;
            advanceFowardInputAction.OnFinished += advanceForwardFinishedDelegate;

            advanceBackStartDelegate = (action) => isAdvancingBackward = true;
            advanceBackFinishedDelegate = (action) => isAdvancingBackward = false;

            advanceBackInputAction.OnStarted += advanceBackStartDelegate;
            advanceBackInputAction.OnFinished += advanceBackFinishedDelegate;

            advanceLeftStartDelegate = (action) => isAdvancingLeft = true;
            advanceLeftFinishedDelegate = (action) => isAdvancingLeft = false;

            advanceLeftInputAction.OnStarted += advanceLeftStartDelegate;
            advanceLeftInputAction.OnFinished += advanceLeftFinishedDelegate;

            advanceRightStartDelegate = (action) => isAdvancingRight = true;
            advanceRightFinishedDelegate = (action) => isAdvancingRight = false;

            advanceRightInputAction.OnStarted += advanceRightStartDelegate;
            advanceRightInputAction.OnFinished += advanceRightFinishedDelegate;

            advanceUpStartDelegate = (action) => isAdvancingUp = true;
            advanceUpFinishedDelegate = (action) => isAdvancingUp = false;

            advanceUpInputAction.OnStarted += advanceUpStartDelegate;
            advanceUpInputAction.OnFinished += advanceUpFinishedDelegate;

            advanceDownStartDelegate = (action) => isAdvancingDown = true;
            advanceDownFinishedDelegate = (action) => isAdvancingDown = false;

            advanceDownInputAction.OnStarted += advanceDownStartDelegate;
            advanceDownInputAction.OnFinished += advanceDownFinishedDelegate;

            cameraPanStartDelegate = (action) => isPanCameraActive = true;
            cameraPanFinishedDelegate = (action) => isPanCameraActive = false;

            cameraPanInputAction.OnStarted += cameraPanStartDelegate;
            cameraPanInputAction.OnFinished += cameraPanFinishedDelegate;

            zoomInFromKeyboardDelegate = (action) => MouseWheel(1f);
            zoomInFromKeyboardInputAction.OnTriggered += zoomInFromKeyboardDelegate;

            zoomOutFromKeyboardDelegate = (action) => MouseWheel(-1f);
            zoomOutFromKeyboardInputAction.OnTriggered += zoomOutFromKeyboardDelegate;
        }

        public void StartDectectingMovement()
        {
            isDetectingMovement = true;
            hasBeenMovement = false;
        }

        public bool HasBeenMovement => hasBeenMovement;

        public void StopDetectingMovement() { isDetectingMovement = false; }

        private void OnInputMouseUp(int buttonId, Vector3 mousePosition)
        {
            if (buttonId != 1)
                return;

            isMouseRightClickDown = false;
        }

        private void OnInputMouseDown(int buttonId, Vector3 mousePosition)
        {
            if (buttonId != 1)
                return;

            isMouseRightClickDown = true;
            direction = Vector3.zero;
        }

        private void OnDestroy()
        {
            BuilderInWorldInputWrapper.OnMouseDrag -= MouseDrag;
            BuilderInWorldInputWrapper.OnMouseDragRaw -= MouseDragRaw;
            BuilderInWorldInputWrapper.OnMouseWheel -= MouseWheel;

            BuilderInWorldInputWrapper.OnMouseDown -= OnInputMouseDown;
            BuilderInWorldInputWrapper.OnMouseUp -= OnInputMouseUp;

            advanceFowardInputAction.OnStarted -= advanceForwardStartDelegate;
            advanceFowardInputAction.OnFinished -= advanceForwardFinishedDelegate;

            advanceBackInputAction.OnStarted -= advanceBackStartDelegate;
            advanceBackInputAction.OnFinished -= advanceBackFinishedDelegate;

            advanceLeftInputAction.OnStarted -= advanceLeftStartDelegate;
            advanceLeftInputAction.OnFinished -= advanceLeftFinishedDelegate;

            advanceRightInputAction.OnStarted -= advanceRightStartDelegate;
            advanceRightInputAction.OnFinished -= advanceRightFinishedDelegate;

            advanceDownInputAction.OnStarted -= advanceDownStartDelegate;
            advanceDownInputAction.OnFinished -= advanceDownFinishedDelegate;

            advanceUpInputAction.OnStarted -= advanceUpStartDelegate;
            advanceUpInputAction.OnFinished -= advanceUpFinishedDelegate;

            cameraPanInputAction.OnStarted -= cameraPanStartDelegate;
            cameraPanInputAction.OnFinished -= cameraPanFinishedDelegate;

            zoomInFromKeyboardInputAction.OnTriggered -= zoomInFromKeyboardDelegate;
            zoomOutFromKeyboardInputAction.OnTriggered -= zoomOutFromKeyboardDelegate;
        }

        private void Update()
        {
            HandleCameraLook();
            HandleCameraPan();
            HandleCameraMovement();
        }

        #region CameraTransformChanges

        private void HandleCameraMovement()
        {
            HandleCameraMovementInput();
            if (direction.magnitude >= CAMERA_MOVEMENT_DEACTIVATE_INPUT_MAGNITUD)
                nextTranslation = direction;
            nextTranslation = Vector3.Lerp(nextTranslation, Vector3.zero, lerpTime * Time.deltaTime);

            if (nextTranslation.magnitude >= CAMERA_MOVEMENT_THRESHOLD)
                transform.Translate(nextTranslation * (movementSpeed * Time.deltaTime), Space.Self);
        }

        private void HandleCameraLook()
        {
            Quaternion nextIteration =  Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(pitch, yaw, 0f)), cameraLookAdvance);

            if (Mathf.Abs(nextIteration.eulerAngles.magnitude - transform.rotation.eulerAngles.magnitude) >= CAMERA_ANGLE_THRESHOLD)
                transform.rotation = nextIteration;
        }

        private void HandleCameraPan()
        {
            panAxisX = Mathf.Lerp(panAxisX, 0, cameraPanAdvance * Time.deltaTime);
            panAxisY = Mathf.Lerp(panAxisY, 0, cameraPanAdvance * Time.deltaTime);

            if (Mathf.Abs(panAxisX) >= CAMERA_PAN_THRESHOLD || Mathf.Abs(panAxisY) >= CAMERA_PAN_THRESHOLD)
                transform.Translate(panAxisX, panAxisY, 0);
        }

        #endregion

        private void HandleCameraMovementInput()
        {
            int velocityChangedCount = 0;
            if (isAdvancingForward)
            {
                cameraVelocity += GetTotalVelocity(Vector3.forward);
                velocityChangedCount++;
            }

            if (isAdvancingBackward)
            {
                cameraVelocity += GetTotalVelocity(Vector3.back);
                velocityChangedCount++;
            }

            if (!isAdvancingBackward && !isAdvancingForward)
                cameraVelocity.z = Mathf.Lerp(cameraVelocity.z, 0, lerpDeccelerationTime);

            if (isAdvancingRight)
            {
                cameraVelocity += GetTotalVelocity(Vector3.right) * xPlaneSpeedPercentCompensantion;
                velocityChangedCount++;
            }

            if (isAdvancingLeft)
            {
                cameraVelocity += GetTotalVelocity(Vector3.left) * xPlaneSpeedPercentCompensantion;
                velocityChangedCount++;
            }

            if (!isAdvancingRight && !isAdvancingLeft)
                cameraVelocity.x = Mathf.Lerp(cameraVelocity.x, 0, lerpDeccelerationTime);

            if (isAdvancingUp)
            {
                cameraVelocity += GetTotalVelocity(Vector3.up);
                velocityChangedCount++;
            }

            if (isAdvancingDown)
            {
                cameraVelocity += GetTotalVelocity(Vector3.down);
                velocityChangedCount++;
            }

            if (!isAdvancingUp && !isAdvancingDown)
                cameraVelocity.y = Mathf.Lerp(cameraVelocity.y, 0, lerpDeccelerationTime);

            if (velocityChangedCount != 0)
                cameraVelocity = Vector3.ClampMagnitude(cameraVelocity, speedClamp);

            direction = cameraVelocity;
        }

        private Vector3 GetTotalVelocity(Vector3 velocityToAdd)
        {
            if (!isMouseRightClickDown)
                return  Vector3.zero;

            if (isDetectingMovement)
                hasBeenMovement = true;
            return velocityToAdd * (initalAcceleration * Time.deltaTime);
        }

        public void SetCameraCanMove(bool canMove) { isCameraAbleToMove = canMove; }

        private void OnGizmoTransformObjectEnd(string gizmoType) { isCameraAbleToMove = true; }

        private void OnGizmoTransformObjectStart(string gizmoType) { isCameraAbleToMove = false; }

        private void MouseWheel(float axis)
        {
            if (!isCameraAbleToMove)
                return;

            if (smoothScrollCor != null)
                CoroutineStarter.Stop(smoothScrollCor);

            float delta = Time.time - lastMouseWheelTime;
            float scrollValue = axis * Mathf.Clamp01(delta);
            lastMouseWheelTime = Time.time;

            smoothScrollCor = CoroutineStarter.Start(SmoothScroll(scrollValue));
        }

        private void MouseDragRaw(int buttonId, Vector3 mousePosition, float axisX, float axisY)
        {
            if (buttonId == 1 && !isPanCameraActive)
                CameraLook(axisX, axisY);
        }

        private void MouseDrag(int buttonId, Vector3 mousePosition, float axisX, float axisY)
        {
            if (buttonId == 2 || buttonId == 1 && isPanCameraActive)
                CameraDrag(axisX, axisY);
        }

        private void CameraDrag(float axisX, float axisY)
        {
            if (!isCameraAbleToMove)
                return;

            panAxisX += -axisX * Time.deltaTime * dragSpeed;
            panAxisY += -axisY * Time.deltaTime * dragSpeed;
            cameraPanAdvance = smoothCameraPanAceleration;
        }

        private void CameraLook(float axisX, float axisY)
        {
            if (!isCameraAbleToMove || !isMouseRightClickDown)
                return;

            yaw += lookSpeedH * axisX;
            pitch -= lookSpeedV * axisY;
            cameraLookAdvance = smoothCameraLookSpeed * Time.deltaTime;
        }

        public override Vector3 OnGetRotation() { return transform.eulerAngles; }

        public void FocusOnEntities(List<DCLBuilderInWorldEntity> entitiesToFocus)
        {
            if (entitiesToFocus.Count <= 0)
                return;

            Vector3 middlePoint = FindMidPoint(entitiesToFocus);
            if (Vector3.positiveInfinity == middlePoint ||
                Vector3.negativeInfinity == middlePoint ||
                float.IsNaN(middlePoint.x) ||
                float.IsNaN(middlePoint.y) ||
                float.IsNaN(middlePoint.z))
                return;

            if (smoothFocusOnTargetCor != null)
                CoroutineStarter.Stop(smoothFocusOnTargetCor);
            smoothFocusOnTargetCor = CoroutineStarter.Start(SmoothFocusOnTarget(middlePoint));
            SmoothLookAt(middlePoint);
        }

        public void SetPosition(Vector3 position) { transform.position = position; }

        public void LookAt(Transform transformToLookAt)
        {
            transform.LookAt(transformToLookAt);
            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;
        }

        public void SmoothLookAt(Transform transformToLookAt) { SmoothLookAt(transformToLookAt.position); }

        public void SmoothLookAt(Vector3 position)
        {
            if (smoothLookAtCor != null)
                CoroutineStarter.Stop(smoothLookAtCor);
            smoothLookAtCor = CoroutineStarter.Start(SmoothLookAtCorutine(position));
        }

        Vector3 FindMidPoint(List<DCLBuilderInWorldEntity> entitiesToLook)
        {
            Vector3 finalPosition = Vector3.zero;
            int totalPoints = 0;
            foreach (DCLBuilderInWorldEntity entity in entitiesToLook)
            {
                if (entity.rootEntity.meshRootGameObject && entity.rootEntity.meshesInfo.renderers.Length > 0)
                {
                    Vector3 midPointFromEntity = Vector3.zero;
                    foreach (Renderer render in entity.rootEntity.renderers)
                    {
                        if (render == null)
                            continue;
                        midPointFromEntity += render.bounds.center;
                    }

                    midPointFromEntity /= entity.rootEntity.renderers.Length;
                    finalPosition += midPointFromEntity;
                    totalPoints++;
                }
            }

            finalPosition /= totalPoints;
            return finalPosition;
        }

        IEnumerator SmoothScroll(float axis)
        {
            float scrollMovementDestination = axis * zoomSpeed;

            Vector3 targetPosition = transform.position + transform.TransformDirection(Vector3.forward * scrollMovementDestination);

            float advance = 0;
            while (advance <= 1)
            {
                advance += smoothLookAtSpeed * Time.deltaTime;
                Vector3 result = Vector3.Lerp(transform.position, targetPosition, advance);
                transform.position = result;
                yield return null;
            }
        }

        IEnumerator SmoothFocusOnTarget(Vector3 targetPosition)
        {
            float advance = 0;
            while (advance <= 1)
            {
                advance += smoothLookAtSpeed * Time.deltaTime;
                transform.position = Vector3.Lerp(transform.position, targetPosition, advance);
                if (Vector3.Distance(transform.position, targetPosition) <= focusDistance)
                    yield break;
                yield return null;
            }
        }

        IEnumerator SmoothLookAtCorutine(Vector3 targetPosition)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
            float advance = 0;
            while (advance <= 1)
            {
                advance += smoothLookAtSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, advance);
                yield return null;
            }

            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;
        }

        public void SetResetConfiguration(Vector3 position, Transform lookAt)
        {
            originalCameraPosition = position;
            originalCameraLookAt = lookAt;
        }

        public void ResetCameraPosition()
        {
            SetPosition(originalCameraPosition);
            LookAt(originalCameraLookAt);
            direction = Vector3.zero;
        }

        public void TakeSceneScreenshot(OnSnapshotsReady onSuccess) { StartCoroutine(TakeSceneScreenshotCoroutine(onSuccess)); }

        private IEnumerator TakeSceneScreenshotCoroutine(OnSnapshotsReady callback)
        {
            var current = camera.targetTexture;
            camera.targetTexture = null;

            yield return null;

            Texture2D sceneScreenshot = ScreenshotFromCamera(SCENE_SNAPSHOT_WIDTH_RES, SCENE_SNAPSHOT_HEIGHT_RES);
            camera.targetTexture = current;
            callback?.Invoke(sceneScreenshot);
        }

        public void TakeSceneScreenshotFromResetPosition(OnSnapshotsReady onSuccess) { StartCoroutine(TakeSceneScreenshotFromResetPositionCoroutine(onSuccess)); }

        private IEnumerator TakeSceneScreenshotFromResetPositionCoroutine(OnSnapshotsReady callback)
        {
            // Store current camera position/direction
            Vector3 currentPos = transform.position;
            Vector3 currentLookAt = transform.forward;
            SetPosition(originalCameraPosition);
            transform.LookAt(originalCameraLookAt);

            var current = camera.targetTexture;
            camera.targetTexture = null;

            yield return null;

            Texture2D sceneScreenshot = ScreenshotFromCamera(SCENE_SNAPSHOT_WIDTH_RES, SCENE_SNAPSHOT_HEIGHT_RES);
            camera.targetTexture = current;
            callback?.Invoke(sceneScreenshot);

            // Restore camera position/direction after the screenshot
            SetPosition(currentPos);
            transform.forward = currentLookAt;
        }

        private Texture2D ScreenshotFromCamera(int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 32);
            camera.targetTexture = rt;
            Texture2D screenShot = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            camera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            screenShot.Apply();

            return screenShot;
        }
    }
}