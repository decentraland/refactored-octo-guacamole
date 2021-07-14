using UnityEngine;
using UnityEngine.UI;
using TMPro;

internal class SceneInfoView : MonoBehaviour
{
    [SerializeField] float idleTime;
    [SerializeField] RawImageFillParent thumbnail;
    [SerializeField] TextMeshProUGUI sceneName;
    [SerializeField] TextMeshProUGUI coordinates;
    [SerializeField] TextMeshProUGUI creatorName;
    [SerializeField] TextMeshProUGUI description;
    [SerializeField] Button_OnPointerDown jumpIn;
    [SerializeField] ShowHideAnimator showHideAnimator;
    [SerializeField] UIHoverCallback hoverArea;
    [SerializeField] GameObject loadingSpinner;

    private float timer;
    private RectTransform thisRT;
    private RectTransform parentRT;
    private HotSceneCellView baseSceneView;

    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);

            AudioScriptableObjects.dialogOpen.Play(true);
        }
        showHideAnimator.Show();
        this.enabled = false;
    }

    public void Show(Vector2 position)
    {
        thisRT.anchoredPosition = position;
        Show();
    }

    public void Hide() { Hide(false); }

    public void Hide(bool instant)
    {
        if (instant)
        {
            showHideAnimator.Hide(true);

            AudioScriptableObjects.dialogClose.Play(true);
        }
        else
        {
            timer = idleTime;
            this.enabled = true;
        }
    }

    void SetSceneView(HotSceneCellView sceneView)
    {
        if (baseSceneView)
        {
            baseSceneView.OnThumbnailSet -= SetThumbnail;
        }

        baseSceneView = sceneView;

        SetMapInfoData(sceneView.mapInfoHandler);

        thumbnail.texture = sceneView.thumbnailHandler.texture;
        bool hasThumbnail = thumbnail.texture != null;
        loadingSpinner.SetActive(!hasThumbnail);
        if (!hasThumbnail)
        {
            sceneView.OnThumbnailSet += SetThumbnail;
        }
    }

    void SetMapInfoData(IMapDataView mapInfoView)
    {
        sceneName.text = mapInfoView.name;
        coordinates.text = $"{mapInfoView.baseCoord.x},{mapInfoView.baseCoord.y}";
        creatorName.text = mapInfoView.creator;
        description.text = mapInfoView.description;
    }

    void SetThumbnail(Texture2D thumbnailTexture)
    {
        thumbnail.texture = thumbnailTexture;
        loadingSpinner.SetActive(thumbnailTexture != null);
    }

    void Awake()
    {
        thisRT = (RectTransform)transform;
        parentRT = (RectTransform)transform.parent;

        this.enabled = false;
        gameObject.SetActive(false);

        jumpIn.onPointerDown += () =>
        {
            if (baseSceneView)
            {
                baseSceneView.JumpInPressed();
            }
        };

        hoverArea.OnPointerEnter += OnPointerEnter;
        hoverArea.OnPointerExit += OnPointerExit;

        HotSceneCellView.OnInfoButtonPointerDown += OnInfoButtonPointerDown;
        HotSceneCellView.OnInfoButtonPointerExit += OnInfoButtonPointerExit;
        HotSceneCellView.OnJumpIn += OnJumpIn;

        showHideAnimator.OnWillFinishHide += OnHidden;
    }

    void OnDestroy()
    {
        hoverArea.OnPointerEnter -= OnPointerEnter;
        hoverArea.OnPointerExit -= OnPointerExit;

        HotSceneCellView.OnInfoButtonPointerDown -= OnInfoButtonPointerDown;
        HotSceneCellView.OnInfoButtonPointerExit -= OnInfoButtonPointerExit;
        HotSceneCellView.OnJumpIn -= OnJumpIn;

        showHideAnimator.OnWillFinishHide -= OnHidden;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            showHideAnimator.Hide();
            this.enabled = false;

            AudioScriptableObjects.dialogClose.Play(true);
        }
    }

    void OnHidden(ShowHideAnimator animator) { baseSceneView = null; }

    void OnInfoButtonPointerDown(HotSceneCellView sceneView)
    {
        if (sceneView == baseSceneView)
            return;


        SetSceneView(sceneView);

        Vector2 localpoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, Input.mousePosition, null, out localpoint))
        {
            Show(localpoint);
        }
    }

    void OnInfoButtonPointerExit() { Hide(); }

    void OnPointerEnter() { Show(); }

    void OnPointerExit() { Hide(); }

    void OnJumpIn(Vector2Int coords, string serverName, string layerName) { gameObject.SetActive(false); }
}