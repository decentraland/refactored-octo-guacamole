using System;
using System.Collections.Generic;
using DCL.Helpers;
using UnityEngine;

public class HotScenesController : MonoBehaviour
{
    public static HotScenesController i { get; private set; }

    public event Action OnHotSceneListFinishUpdating;
    public event Action OnHotSceneListChunkUpdated;

    public List<HotSceneInfo> hotScenesList { get; private set; } = new List<HotSceneInfo>();
    public bool isUpdating { get; private set; }
    public float timeSinceLastUpdate { get { return Time.realtimeSinceStartup - lastUpdateTime; } }

    private float lastUpdateTime = float.MinValue * .5f;

    [Serializable]
    public class HotSceneInfo
    {
        [Serializable]
        public class Realm
        {
            public string serverName;
            public string layer;
            public int usersCount;
            public int usersMax;
            public Vector2Int[] userParcels;
        }

        public string id;
        public string name;
        public string creator;
        public string description;
        public string thumbnail;
        public Vector2Int baseCoords;
        public Vector2Int[] parcels;
        public int usersTotalCount;
        public Realm[] realms;
    }

    [Serializable]
    internal struct HotScenesUpdatePayload
    {
        public int chunkIndex;
        public int chunksCount;
        public HotSceneInfo[] scenesInfo;
    }

    void Awake() { i = this; }

    public void UpdateHotScenesList(string json)
    {
        var updatePayload = Utils.SafeFromJson<HotScenesUpdatePayload>(json);

        if (updatePayload.chunkIndex == 0)
        {
            isUpdating = true;
            hotScenesList.Clear();
        }

        hotScenesList.AddRange(updatePayload.scenesInfo);
        OnHotSceneListChunkUpdated?.Invoke();

        if (updatePayload.chunkIndex >= updatePayload.chunksCount - 1)
        {
            isUpdating = false;
            lastUpdateTime = Time.realtimeSinceStartup;
            OnHotSceneListFinishUpdating?.Invoke();
        }
    }
}