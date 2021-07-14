using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DCL
{
    public class AssetBundlesLoader
    {
        private const float MAX_LOAD_BUDGET_TIME = 0.05f;
        private const int SKIPPED_FRAMES_AFTER_BUDGET_TIME_IS_REACHED_FOR_NEARBY_ASSETS = 1;
        private const int SKIPPED_FRAMES_AFTER_BUDGET_TIME_IS_REACHED_FOR_DISTANT_ASSETS = 5;
        private const float MAX_SQR_DISTANCE_FOR_QUICK_LOADING = 6000f;
        private const float TIME_BETWEEN_REPRIORITIZATIONS = 1f;

        private struct AssetBundleInfo
        {
            public Asset_AB asset;
            public AssetBundle assetBundle;
            public Transform containerTransform;
            public Action onSuccess;
            public Action onFail;

            public AssetBundleInfo(Asset_AB asset, AssetBundle assetBundle, Transform containerTransform, Action onSuccess, Action onFail)
            {
                this.asset = asset;
                this.assetBundle = assetBundle;
                this.containerTransform = containerTransform;
                this.onSuccess = onSuccess;
                this.onFail = onFail;
            }
        }

        private Coroutine assetBundlesLoadingCoroutine;
        private Queue<AssetBundleInfo> highPriorityLoadQueue = new Queue<AssetBundleInfo>();
        private Queue<AssetBundleInfo> lowPriorityLoadQueue = new Queue<AssetBundleInfo>();

        private Dictionary<string, int> loadOrderByExtension = new Dictionary<string, int>()
        {
            { "png", 0 },
            { "jpg", 1 },
            { "peg", 2 },
            { "bmp", 3 },
            { "psd", 4 },
            { "iff", 5 },
            { "mat", 6 },
            { "nim", 7 },
            { "ltf", 8 },
            { "glb", 9 }
        };

        private List<UnityEngine.Object> loadedAssetsByName = new List<UnityEngine.Object>();
        private float currentLoadBudgetTime = 0;
        private AssetBundleInfo assetBundleInfoToLoad;
        private float lastQueuesReprioritizationTime = 0;

        private bool limitTimeBudget => CommonScriptableObjects.rendererState.Get();

        public void Start()
        {
            if (assetBundlesLoadingCoroutine != null)
                return;

            assetBundlesLoadingCoroutine = CoroutineStarter.Start(LoadAssetBundlesCoroutine());
        }

        public void Stop()
        {
            if (assetBundlesLoadingCoroutine == null)
                return;

            CoroutineStarter.Stop(assetBundlesLoadingCoroutine);
            highPriorityLoadQueue.Clear();
            lowPriorityLoadQueue.Clear();
        }

        public void MarkAssetBundleForLoad(Asset_AB asset, AssetBundle assetBundle, Transform containerTransform, Action onSuccess, Action onFail)
        {
            CheckForReprioritizeAwaitingAssets();

            AssetBundleInfo assetBundleToLoad = new AssetBundleInfo(asset, assetBundle, containerTransform, onSuccess, onFail);

            float distanceFromPlayer = GetDistanceFromPlayer(containerTransform);
            if (distanceFromPlayer <= MAX_SQR_DISTANCE_FOR_QUICK_LOADING)
                highPriorityLoadQueue.Enqueue(assetBundleToLoad);
            else
                lowPriorityLoadQueue.Enqueue(assetBundleToLoad);
        }

        private IEnumerator LoadAssetBundlesCoroutine()
        {
            while (true)
            {
                while (highPriorityLoadQueue.Count > 0)
                {
                    float time = Time.realtimeSinceStartup;

                    assetBundleInfoToLoad = highPriorityLoadQueue.Dequeue();
                    yield return LoadAssetBundle(assetBundleInfoToLoad);

                    if (IsLoadBudgetTimeReached(time))
                    {
                        yield return WaitForSkippedFrames(SKIPPED_FRAMES_AFTER_BUDGET_TIME_IS_REACHED_FOR_NEARBY_ASSETS);
                        time = Time.realtimeSinceStartup;
                    }
                }

                while (lowPriorityLoadQueue.Count > 0 && highPriorityLoadQueue.Count == 0)
                {
                    float time = Time.realtimeSinceStartup;

                    assetBundleInfoToLoad = lowPriorityLoadQueue.Dequeue();
                    yield return LoadAssetBundle(assetBundleInfoToLoad);

                    if (IsLoadBudgetTimeReached(time))
                    {
                        yield return WaitForSkippedFrames(SKIPPED_FRAMES_AFTER_BUDGET_TIME_IS_REACHED_FOR_DISTANT_ASSETS);
                        time = Time.realtimeSinceStartup;
                    }
                }

                yield return null;
            }
        }

        private IEnumerator LoadAssetBundle(AssetBundleInfo assetBundleInfo)
        {
            if (assetBundleInfo.assetBundle == null)
            {
                assetBundleInfo.onFail?.Invoke();
                yield break;
            }

            AssetBundleRequest abRequest = assetBundleInfo.assetBundle.LoadAllAssetsAsync();

            while (!abRequest.isDone)
            {
                yield return null;
            }

            loadedAssetsByName = abRequest.allAssets.ToList();

            foreach (var loadedAsset in loadedAssetsByName)
            {
                string ext = "any";

                if (loadedAsset is Texture textureAsset)
                {
                    ext = "png";
                }
                else if (loadedAsset is Material)
                {
                    ext = "mat";
                }
                else if (loadedAsset is Animation || loadedAsset is AnimationClip)
                {
                    ext = "nim";
                }
                else if (loadedAsset is GameObject)
                {
                    ext = "glb";
                }

                if (assetBundleInfo.asset?.assetsByExtension == null)
                {
                    Debug.LogWarning($"Found an unexpected Null Reference: {assetBundleInfo.asset} or {assetBundleInfo.asset?.assetsByExtension}");
                }
                else
                {
                    if (!assetBundleInfo.asset.assetsByExtension.ContainsKey(ext))
                    {
                        assetBundleInfo.asset.assetsByExtension.Add(ext, new List<UnityEngine.Object>());
                    }

                    assetBundleInfo.asset.assetsByExtension[ext].Add(loadedAsset);
                }
            }

            loadedAssetsByName.Clear();
            assetBundleInfo.onSuccess?.Invoke();
        }

        private bool IsLoadBudgetTimeReached(float startTime)
        {
            if (limitTimeBudget)
            {
                currentLoadBudgetTime += Time.realtimeSinceStartup - startTime;
                if (currentLoadBudgetTime > MAX_LOAD_BUDGET_TIME)
                {
                    currentLoadBudgetTime = 0f;
                    return true;
                }
            }

            return false;
        }

        private IEnumerator WaitForSkippedFrames(int skippedFramesBetweenLoadings)
        {
            for (int i = 0; i < skippedFramesBetweenLoadings; i++)
            {
                yield return null;
            }
        }

        private void CheckForReprioritizeAwaitingAssets()
        {
            if (lowPriorityLoadQueue.Count == 0 ||
                (Time.realtimeSinceStartup - lastQueuesReprioritizationTime) < TIME_BETWEEN_REPRIORITIZATIONS)
                return;

            while (lowPriorityLoadQueue.Count > 0 && GetDistanceFromPlayer(lowPriorityLoadQueue.Peek().containerTransform) <= MAX_SQR_DISTANCE_FOR_QUICK_LOADING)
            {
                highPriorityLoadQueue.Enqueue(lowPriorityLoadQueue.Dequeue());
                lastQueuesReprioritizationTime = Time.realtimeSinceStartup;
            }
        }

        private float GetDistanceFromPlayer(Transform containerTransform)
        {
            return (containerTransform != null && limitTimeBudget) ? Vector3.SqrMagnitude(containerTransform.position - CommonScriptableObjects.playerUnityPosition.Get()) : 0f;
        }
    }
}