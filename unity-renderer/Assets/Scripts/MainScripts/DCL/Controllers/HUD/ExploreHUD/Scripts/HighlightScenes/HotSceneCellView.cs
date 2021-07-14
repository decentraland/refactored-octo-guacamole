using System;
using System.Collections.Generic;
using DCL.Helpers;
using UnityEngine;
using TMPro;
using System.Linq;
using DCL;
using static DCL.Interface.WebInterface;
using DCL.Interface;

internal class HotSceneCellView : MonoBehaviour
{
    const int THMBL_MARKETPLACE_WIDTH = 196;
    const int THMBL_MARKETPLACE_HEIGHT = 143;
    const int THMBL_MARKETPLACE_SIZEFACTOR = 50;

    [Header("Animators")]
    [SerializeField] Animator viewAnimator;
    [SerializeField] ShowHideAnimator jumpInButtonAnimator;

    [Header("Crowd")]
    [SerializeField] GameObject crowdCountContainer;
    [SerializeField] TextMeshProUGUI crowdCount;

    [Header("Events")]
    [SerializeField] GameObject eventsContainer;

    [Header("Friends")]
    [SerializeField] ExploreFriendsView friendsView;
    [SerializeField] GameObject friendsContainer;

    [Header("Scene")]
    [SerializeField] TextMeshProUGUI sceneName;
    [SerializeField] internal RawImageFillParent thumbnailImage;
    [SerializeField] UIHoverCallback sceneInfoButton;

    [Header("UI")]
    [SerializeField] UIHoverCallback jumpInHoverArea;
    [SerializeField] Button_OnPointerDown jumpIn;
    [SerializeField] Sprite errorThumbnail;

    public delegate void JumpInDelegate(Vector2Int coords, string serverName, string layerName);
    public static event JumpInDelegate OnJumpIn;

    public static event Action<HotSceneCellView> OnInfoButtonPointerDown;
    public static event Action OnInfoButtonPointerExit;

    public event Action<Texture2D> OnThumbnailSet;

    public MapInfoHandler mapInfoHandler { private set; get; }
    public FriendsHandler friendsHandler { private set; get; }
    public ThumbnailHandler thumbnailHandler { private set; get; }
    public AnimationHandler animationHandler { private set; get; }

    private ViewPool<ExploreFriendsView> friendPool;
    private Dictionary<string, ExploreFriendsView> friendViewById;
    private bool isLoaded = false;
    private bool isInitialized = false;
    private Queue<HotScenesController.HotSceneInfo.Realm> nextMostPopulatedRealms = new Queue<HotScenesController.HotSceneInfo.Realm>();
    private JumpInPayload lastJumpInTried = new JumpInPayload();

    HotScenesController.HotSceneInfo hotSceneInfo;

    protected void Awake()
    {
        crowdCountContainer.SetActive(false);
        eventsContainer.SetActive(false);

        jumpInHoverArea.OnPointerEnter += () =>
        {
            jumpInButtonAnimator.gameObject.SetActive(true);
            jumpInButtonAnimator.Show();
        };
        jumpInHoverArea.OnPointerExit += () => jumpInButtonAnimator.Hide();
        sceneInfoButton.OnPointerDown += () => jumpInButtonAnimator.Hide(true);

        // NOTE: we don't use the pointer down callback to avoid being mistakenly pressed while dragging
        jumpIn.onClick.AddListener(JumpInPressed);

        sceneInfoButton.OnPointerDown += () => OnInfoButtonPointerDown?.Invoke(this);
        sceneInfoButton.OnPointerExit += () => OnInfoButtonPointerExit?.Invoke();

        Initialize();
    }

    public void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;

        friendPool = new ViewPool<ExploreFriendsView>(friendsView, 0);
        friendViewById = new Dictionary<string, ExploreFriendsView>();

        mapInfoHandler = new MapInfoHandler();

        friendsHandler = new FriendsHandler(mapInfoHandler);
        friendsHandler.onFriendAdded += OnFriendAdded;
        friendsHandler.onFriendRemoved += OnFriendRemoved;

        thumbnailHandler = new ThumbnailHandler();
        animationHandler = new AnimationHandler(viewAnimator);
    }

    public void Setup(HotScenesController.HotSceneInfo sceneInfo)
    {
        hotSceneInfo = sceneInfo;
        mapInfoHandler.SetMinimapSceneInfo(sceneInfo);

        OnCrowdInfoUpdated(sceneInfo);
        OnMapInfoUpdated(sceneInfo);
    }

    public void Clear()
    {
        thumbnailImage.texture = null;
        thumbnailHandler.Dispose();
        thumbnailImage.gameObject.SetActive(false);
        isLoaded = false;
    }

    public void JumpInPressed()
    {
        HotScenesController.HotSceneInfo.Realm realm = new HotScenesController.HotSceneInfo.Realm() { layer = null, serverName = null };
        hotSceneInfo.realms = hotSceneInfo.realms.OrderByDescending(x => x.usersCount).ToArray();
        nextMostPopulatedRealms.Clear();
        for (int i = 0; i < hotSceneInfo.realms.Length; i++)
        {
            if (hotSceneInfo.realms[i].usersCount < hotSceneInfo.realms[i].usersMax)
            {
                realm = hotSceneInfo.realms[i];
                if (i < hotSceneInfo.realms.Length - 1)
                {
                    nextMostPopulatedRealms = new Queue<HotScenesController.HotSceneInfo.Realm>(hotSceneInfo.realms.ToList().GetRange(i + 1, hotSceneInfo.realms.Length - i - 1));
                }
                break;
            }
        }

        RealmsInfoBridge.OnRealmConnectionSuccess -= OnRealmConnectionSuccess;
        RealmsInfoBridge.OnRealmConnectionSuccess += OnRealmConnectionSuccess;
        RealmsInfoBridge.OnRealmConnectionFailed -= OnRealmConnectionFailed;
        RealmsInfoBridge.OnRealmConnectionFailed += OnRealmConnectionFailed;

        lastJumpInTried.gridPosition = hotSceneInfo.baseCoords;
        lastJumpInTried.realm.serverName = realm.serverName;
        lastJumpInTried.realm.layer = realm.layer;

        OnJumpIn?.Invoke(hotSceneInfo.baseCoords, realm.serverName, realm.layer);
    }

    private void OnRealmConnectionSuccess(JumpInPayload successRealm)
    {
        if (successRealm.gridPosition != lastJumpInTried.gridPosition ||
            successRealm.realm.serverName != lastJumpInTried.realm.serverName ||
            successRealm.realm.layer != lastJumpInTried.realm.layer)
            return;

        RealmsInfoBridge.OnRealmConnectionSuccess -= OnRealmConnectionSuccess;
        RealmsInfoBridge.OnRealmConnectionFailed -= OnRealmConnectionFailed;
    }

    private void OnRealmConnectionFailed(JumpInPayload failedRealm)
    {
        if (failedRealm.gridPosition != lastJumpInTried.gridPosition ||
            failedRealm.realm.serverName != lastJumpInTried.realm.serverName ||
            failedRealm.realm.layer != lastJumpInTried.realm.layer)
            return;

        if (nextMostPopulatedRealms.Count > 0)
        {
            WebInterface.NotifyStatusThroughChat("Trying to connect to the next more populated realm...");
            HotScenesController.HotSceneInfo.Realm nextRealmToTry = nextMostPopulatedRealms.Dequeue();
            OnJumpIn?.Invoke(hotSceneInfo.baseCoords, nextRealmToTry.serverName, nextRealmToTry.layer);
        }
        else
        {
            WebInterface.NotifyStatusThroughChat("You'll stay in your current realm.");
            RealmsInfoBridge.OnRealmConnectionSuccess -= OnRealmConnectionSuccess;
            RealmsInfoBridge.OnRealmConnectionFailed -= OnRealmConnectionFailed;
            OnJumpIn?.Invoke(hotSceneInfo.baseCoords, null, null);
        }
    }

    private void OnDestroy()
    {
        friendPool.Dispose();
        thumbnailHandler.Dispose();

        friendsHandler.onFriendAdded -= OnFriendAdded;
        friendsHandler.onFriendRemoved -= OnFriendRemoved;

        RealmsInfoBridge.OnRealmConnectionFailed -= OnRealmConnectionFailed;
        RealmsInfoBridge.OnRealmConnectionSuccess -= OnRealmConnectionSuccess;
    }

    private void OnEnable()
    {
        jumpInButtonAnimator.gameObject.SetActive(false);
        jumpInHoverArea.enabled = isLoaded;
        thumbnailImage.gameObject.SetActive(isLoaded);

        if (isLoaded)
        {
            animationHandler.SetLoaded();
        }
    }

    private void OnCrowdInfoUpdated(HotScenesController.HotSceneInfo info)
    {
        crowdCount.text = info.usersTotalCount.ToString();
        crowdCountContainer.SetActive(info.usersTotalCount > 0);
    }

    private void OnFriendAdded(UserProfile profile, Color backgroundColor)
    {
        var view = friendPool.GetView();
        view.SetUserProfile(profile, backgroundColor);
        friendViewById.Add(profile.userId, view);
    }

    private void OnFriendRemoved(UserProfile profile)
    {
        if (friendViewById.TryGetValue(profile.userId, out ExploreFriendsView view))
        {
            friendPool.PoolView(view);
            friendViewById.Remove(profile.userId);
        }
    }

    private void OnMapInfoUpdated(HotScenesController.HotSceneInfo sceneInfo)
    {
        sceneName.text = sceneInfo.name;

        FetchThumbnail(sceneInfo.thumbnail,
            onFail: () => FetchThumbnail(MapUtils.GetMarketPlaceThumbnailUrl(sceneInfo.parcels, THMBL_MARKETPLACE_WIDTH, THMBL_MARKETPLACE_HEIGHT, THMBL_MARKETPLACE_SIZEFACTOR),
                onFail: () => SetThumbnail(errorThumbnail.texture)));
    }

    private void FetchThumbnail(string url, Action onFail) { thumbnailHandler.FetchThumbnail(url, SetThumbnail, onFail); }

    private void SetThumbnail(Texture2D texture)
    {
        thumbnailImage.texture = texture;
        thumbnailImage.gameObject.SetActive(true);
        OnThumbnailSet?.Invoke(texture);

        SetLoaded();
    }

    private void SetLoaded()
    {
        isLoaded = true;
        animationHandler.SetLoaded();
        jumpInHoverArea.enabled = true;
    }
}