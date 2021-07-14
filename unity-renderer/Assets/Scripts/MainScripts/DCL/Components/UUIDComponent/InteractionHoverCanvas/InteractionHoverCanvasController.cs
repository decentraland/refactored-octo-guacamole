using DCL.Models;
using DCL.Components;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
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

        buttonToIcon.Add( ACTION_BUTTON_POINTER, icons[0] );
        buttonToIcon.Add( ACTION_BUTTON_PRIMARY, icons[1] );
        buttonToIcon.Add( ACTION_BUTTON_SECONDARY, icons[2] );
    }

    public void Setup(string button, string feedbackText, IDCLEntity entity)
    {
        text.text = feedbackText;
        this.entity = entity;

        ConfigureIcon(button);

        canvas.enabled = enabled && isHovered;
    }

    private static Dictionary<string, GameObject> buttonToIcon = new Dictionary<string, GameObject>();
    private string currentButton;

    void ConfigureIcon(string button)
    {
        if ( currentButton == button )
            return;

        currentButton = button;

        if ( hoverIcon != null && hoverIcon.activeSelf )
            hoverIcon.SetActive(false);

        if (buttonToIcon.ContainsKey(button))
            hoverIcon = buttonToIcon[button];
        else
            hoverIcon = icons[3];

        if ( !hoverIcon.activeSelf )
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