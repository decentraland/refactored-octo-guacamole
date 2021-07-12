using UnityEngine;
using System.Collections.Generic;
using System;

namespace Builder.Gizmos
{
    public class DCLBuilderGizmoManager : MonoBehaviour
    {
        public delegate void GizmoTransformDelegate(string gizmoType);

        public static event GizmoTransformDelegate OnGizmoTransformObjectStart;
        public static event GizmoTransformDelegate OnGizmoTransformObject;
        public static event GizmoTransformDelegate OnGizmoTransformObjectEnd;

        public event Action<Vector3> OnChangeTransformValue;

        public DCLBuilderRaycast builderRaycast;

        [SerializeField] private DCLBuilderGizmo[] gizmos = null;

        public bool isTransformingObject { private set; get; }
        public DCLBuilderGizmo activeGizmo { private set; get; }

        private SnapInfo snapInfo = new SnapInfo();

        private bool isGameObjectActive = false;
        private bool isGizmosInitialized = false;

        private DCLBuilderGizmoAxis hoveredAxis = null;

        private Transform selectedEntitiesParent;
        private List<EditableEntity> selectedEntities;

        public string GetSelectedGizmo()
        {
            if (IsGizmoActive())
            {
                return activeGizmo.GetGizmoType();
            }
            return DCL.Components.DCLGizmos.Gizmo.NONE;
        }

        public void SetSnapFactor(float position, float rotation, float scale)
        {
            snapInfo.position = position;
            snapInfo.rotation = rotation;
            snapInfo.scale = scale;

            if (activeGizmo != null)
            {
                activeGizmo.SetSnapFactor(snapInfo);
            }
        }

        private void OnBeginDrag(DCLBuilderGizmoAxis hittedAxis)
        {
            isTransformingObject = true;
            activeGizmo = hittedAxis.GetGizmo();
            activeGizmo.OnBeginDrag(hittedAxis, selectedEntitiesParent);

            OnGizmoTransformObjectStart?.Invoke(activeGizmo.GetGizmoType());
        }

        private void OnDrag(Vector3 hitPoint, Vector2 mousePosition)
        {
            float value = activeGizmo.OnDrag(hitPoint, mousePosition);
            OnGizmoTransformObject?.Invoke(activeGizmo.GetGizmoType());
            OnChangeTransformValue?.Invoke(value * activeGizmo.GetActiveAxisVector());
        }

        private void OnEndDrag()
        {
            activeGizmo.OnEndDrag();
            OnGizmoTransformObjectEnd?.Invoke(activeGizmo.GetGizmoType());
            isTransformingObject = false;
        }

        public bool HasAxisHover() { return hoveredAxis != null; }

        private void SetAxisHover(DCLBuilderGizmoAxis axis)
        {
            if (hoveredAxis != null && hoveredAxis != axis)
            {
                hoveredAxis.SetColorDefault();
            }
            else if (axis != null)
            {
                axis.SetColorHighlight();
            }
            hoveredAxis = axis;
        }

        public void ForceRelativeScaleRatio()
        {
            for (int i = 0; i < gizmos.Length; i++)
            {
                gizmos[i].ForceRelativeScaleRatio();
            }
        }

        public void ShowGizmo()
        {
            if (activeGizmo != null)
            {
                activeGizmo.SetTargetTransform(selectedEntitiesParent);
                activeGizmo.gameObject.SetActive(true);
            }
        }

        public void HideGizmo(bool setInactiveGizmos = false)
        {
            if (activeGizmo != null)
            {
                activeGizmo.gameObject.SetActive(false);
            }
            if (setInactiveGizmos)
            {
                SetGizmoType(DCL.Components.DCLGizmos.Gizmo.NONE);
            }
        }

        private bool IsGizmoActive() { return activeGizmo != null; }

        private bool RaycastHit(Ray ray, out Vector3 hitPoint)
        {
            if (activeGizmo != null)
            {
                return activeGizmo.RaycastHit(ray, out hitPoint);
            }
            hitPoint = Vector3.zero;
            return false;
        }

        private void OnEnable()
        {
            if (!isGameObjectActive)
            {
                DCLBuilderBridge.OnSelectGizmo += SetGizmoType;
                DCLBuilderBridge.OnSetGridResolution += OnSetGridResolution;
                DCLBuilderCamera.OnCameraZoomChanged += OnCameraZoomChanged;
                DCLBuilderObjectSelector.OnSelectedObjectListChanged += SetSelectedEntities;
                DCLBuilderObjectSelector.OnGizmosAxisPressed += OnGizmosAxisPressed;
                DCLBuilderInput.OnMouseUp += OnMouseUp;
                DCLBuilderInput.OnMouseDrag += OnMouseDrag;
                isGameObjectActive = true;
            }
        }

        private void OnDisable()
        {
            DCLBuilderBridge.OnSelectGizmo -= SetGizmoType;
            DCLBuilderBridge.OnSetGridResolution -= OnSetGridResolution;
            DCLBuilderCamera.OnCameraZoomChanged -= OnCameraZoomChanged;
            DCLBuilderObjectSelector.OnSelectedObjectListChanged -= SetSelectedEntities;
            DCLBuilderObjectSelector.OnGizmosAxisPressed -= OnGizmosAxisPressed;
            DCLBuilderInput.OnMouseUp -= OnMouseUp;
            DCLBuilderInput.OnMouseDrag -= OnMouseDrag;
            isGameObjectActive = false;
        }

        private void Update()
        {
            if (!isTransformingObject)
            {
                CheckGizmoHover(Input.mousePosition);
            }
        }

        public void SetGizmoType(string gizmoType)
        {
            HideGizmo();

            if (gizmoType != DCL.Components.DCLGizmos.Gizmo.NONE)
            {
                bool wasGizmoActive = IsGizmoActive();

                for (int i = 0; i < gizmos.Length; i++)
                {
                    if (gizmos[i].GetGizmoType() == gizmoType)
                    {
                        activeGizmo = gizmos[i];
                        activeGizmo.SetSnapFactor(snapInfo);
                        break;
                    }
                }

                bool areEntitiesSelected = selectedEntities != null && selectedEntities.Count > 0;
                if (wasGizmoActive && areEntitiesSelected)
                {
                    ShowGizmo();
                }
                else
                {
                    GizmoStatusUpdate();
                }
            }
            else
            {
                activeGizmo = null;
            }
        }

        public void InitializeGizmos(Camera camera) { InitializeGizmos(camera, camera.transform); }

        public void InitializeGizmos(Camera camera, Transform cameraTransform)
        {
            if (!isGizmosInitialized)
            {
                for (int i = 0; i < gizmos.Length; i++)
                {
                    if (!gizmos[i].initialized)
                    {
                        gizmos[i].Initialize(camera, cameraTransform);
                    }
                }
                isGizmosInitialized = true;
            }
        }

        private void OnCameraZoomChanged(Camera camera, float zoom) { InitializeGizmos(camera); }

        private void OnSetGridResolution(float position, float rotation, float scale) { SetSnapFactor(position, rotation, scale); }

        public void SetSelectedEntities(Transform selectionParent, List<EditableEntity> entities)
        {
            selectedEntities = entities;
            selectedEntitiesParent = selectionParent;
            GizmoStatusUpdate();
        }
        private void OnGizmosAxisPressed(DCLBuilderGizmoAxis pressedAxis) { OnBeginDrag(pressedAxis); }

        private void OnMouseUp(int buttonId, Vector3 mousePosition)
        {
            if (!isTransformingObject)
            {
                return;
            }

            if (buttonId == 0)
            {
                OnEndDrag();
            }
        }

        private void OnMouseDrag(int buttonId, Vector3 mousePosition, float axisX, float axisY)
        {
            if (buttonId == 0)
            {
                bool hasMouseMoved = (axisX != 0 || axisY != 0);
                if (isTransformingObject && hasMouseMoved)
                {
                    Vector3 hit;
                    if (RaycastHit(builderRaycast.GetMouseRay(mousePosition), out hit))
                    {
                        OnDrag(hit, mousePosition);
                    }
                }
            }
        }

        private void CheckGizmoHover(Vector3 mousePosition)
        {
            if (builderRaycast.builderCamera == null)
                return;
            RaycastHit hit;
            if (builderRaycast.RaycastToGizmos(mousePosition, out hit))
            {
                DCLBuilderGizmoAxis gizmoAxis = hit.collider.gameObject.GetComponent<DCLBuilderGizmoAxis>();
                SetAxisHover(gizmoAxis);
            }
            else
            {
                SetAxisHover(null);
            }
        }

        private void GizmoStatusUpdate()
        {
            if (IsGizmoActive())
            {
                if (selectedEntities == null || selectedEntities.Count == 0)
                {
                    HideGizmo();
                }
                else
                {
                    ShowGizmo();
                }
            }
        }

        public class SnapInfo
        {
            public float position = 0;
            public float rotation = 0;
            public float scale = 0;
        }
    }
}