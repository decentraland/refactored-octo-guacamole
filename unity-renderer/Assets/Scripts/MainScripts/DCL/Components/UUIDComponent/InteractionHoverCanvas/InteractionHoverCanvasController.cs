using DCL.Models;
using DCL.Components;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DCL;

public class InteractionHoverCanvasController : MonoBehaviour
{
    public static InteractionHoverCanvasController i { get; private set; }
    public Canvas canvas;
    public RectTransform backgroundTransform;
    public TextMeshProUGUI text;
    public GameObject[] icons;

    bool isHovered = false;
    Camera mainCamera;
    GameObject hoverIcon;
    Vector3 meshCenteredPos;
    IDCLEntity entity;

    const string ACTION_BUTTON_POINTER = "POINTER";
    const string ACTION_BUTTON_PRIMARY = "PRIMARY";
    const string ACTION_BUTTON_SECONDARY = "SECONDARY";

    void Awake()
    {
        i = this;
        mainCamera = Camera.main;
    }

    public void Setup(string button, string feedbackText, IDCLEntity entity)
    {
        text.text = feedbackText;
        this.entity = entity;

        ConfigureIcon(button);

        canvas.enabled = enabled && isHovered;
    }

    void ConfigureIcon(string button)
    {
        if ( hoverIcon != null )
            hoverIcon.SetActive(false);

        switch (button)
        {
            case ACTION_BUTTON_POINTER:
                hoverIcon = icons[0];
                break;
            case ACTION_BUTTON_PRIMARY:
                hoverIcon = icons[1];
                break;
            case ACTION_BUTTON_SECONDARY:
                hoverIcon = icons[2];
                break;
            default: // ANY
                hoverIcon = icons[3];
                break;
        }

        hoverIcon.SetActive(true);
    }

    public void SetHoverState(bool hoverState)
    {
        if (!enabled || hoverState == isHovered)
            return;

        isHovered = hoverState;

        canvas.enabled = isHovered;
    }

    public GameObject GetCurrentHoverIcon() { return hoverIcon; }

    // This method will be used when we apply a "loose aim" for the 3rd person camera
    void CalculateMeshCenteredPos(DCLTransform.Model transformModel = null)
    {
        if (!canvas.enabled)
            return;

        if (entity.meshesInfo.renderers == null || entity.meshesInfo.renderers.Length == 0)
        {
            meshCenteredPos = transform.parent.position;
        }
        else
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < entity.meshesInfo.renderers.Length; i++)
            {
                sum += entity.meshesInfo.renderers[i].bounds.center;
            }

            meshCenteredPos = sum / entity.meshesInfo.renderers.Length;
        }
    }

    // This method will be used when we apply a "loose aim" for the 3rd person camera
    public void UpdateSizeAndPos()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector3 screenPoint = mainCamera.WorldToViewportPoint(meshCenteredPos);

        if (screenPoint.z > 0)
        {
            RectTransform canvasRect = (RectTransform) canvas.transform;
            float width = canvasRect.rect.width;
            float height = canvasRect.rect.height;
            screenPoint.Scale(new Vector3(width, height, 0));

            ((RectTransform) backgroundTransform).anchoredPosition = screenPoint;
        }
    }
}