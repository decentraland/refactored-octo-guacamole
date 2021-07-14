using System;
using DCL.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class AvatarName : MonoBehaviour
{
    const float NAME_VANISHING_POINT_DISTANCE = 20.0f;
    readonly int VOICE_CHAT_ANIMATOR_TALKING = Animator.StringToHash("Talking");

    public CanvasGroup uiContainer;
    public Transform sourceTransform;
    public TextMeshProUGUI nameText;
    public Vector3 offset;
    public Animator talkingAnimator;
    Canvas canvas;
    Camera mainCamera;
    RectTransform canvasRect;

    Vector2 res;

    public void SetName(string name)
    {
        talkingAnimator?.gameObject.SetActive(false);
        if (string.IsNullOrEmpty(name))
        {
            uiContainer.alpha = 0;
            return;
        }

        if (nameText.text != name)
        {
            nameText.text = name;
            Utils.ForceRebuildLayoutImmediate(canvasRect);
            RefreshTextPosition();
        }
    }

    public void SetTalking(bool talking)
    {
        if (!talkingAnimator)
            return;

        if (talking && !talkingAnimator.gameObject.activeSelf)
        {
            talkingAnimator.gameObject.SetActive(talking);
        }

        talkingAnimator.SetBool(VOICE_CHAT_ANIMATOR_TALKING, talking);
    }

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = (RectTransform)canvas.transform;
        talkingAnimator?.gameObject.SetActive(false);
        canvas.enabled = false;
    }

    void OnEnable()
    {
        // We initialize mainCamera here because the main camera may change while the gameobject is disabled
        mainCamera = Camera.main;
    }

    public void Update()
    {
        if ( Input.GetKeyUp(KeyCode.H) )
        {
            canvas.enabled = !canvas.enabled;

            if ( canvas.enabled )
            {
                RenderPipelineManager.beginFrameRendering += BeginFrameRendering;
            }
            else
            {
                RenderPipelineManager.beginFrameRendering -= BeginFrameRendering;
            }
        }
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginFrameRendering -= BeginFrameRendering;
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginFrameRendering -= BeginFrameRendering;
    }

    void BeginFrameRendering(ScriptableRenderContext scriptableRenderContext, Camera[] cameras)
    {
        if (string.IsNullOrEmpty(nameText.text))
            return;

        RefreshTextPosition();

        if (Screen.width != res.x || Screen.height != res.y)
            nameText.SetAllDirty();

        res = new Vector2(Screen.width, Screen.height);
    }

    private void RefreshTextPosition()
    {
        Vector3 screenPoint = mainCamera == null ? Vector3.zero : mainCamera.WorldToViewportPoint(sourceTransform.position + offset);
        uiContainer.alpha = 1.0f + (1.0f - (screenPoint.z / NAME_VANISHING_POINT_DISTANCE));

        if (screenPoint.z > 0)
        {
            if (!uiContainer.gameObject.activeSelf)
            {
                uiContainer.gameObject.SetActive(true);
            }

            float width = canvasRect.rect.width;
            float height = canvasRect.rect.height;
            screenPoint.Scale(new Vector3(width, height, 0));
            ((RectTransform)transform).anchoredPosition = screenPoint;
        }
        else
        {
            if (uiContainer.gameObject.activeSelf)
            {
                uiContainer.gameObject.SetActive(false);
            }
        }
    }
}