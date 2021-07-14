using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static WearableLiterals;

namespace DCL
{
    public class AvatarRenderer : MonoBehaviour, IAvatarRenderer
    {
        public enum VisualCue
        {
            CleanedUp,
            Loaded
        }

        private const int MAX_RETRIES = 5;

        public Material defaultMaterial;
        public Material eyeMaterial;
        public Material eyebrowMaterial;
        public Material mouthMaterial;
        public MeshRenderer lodRenderer;

        private AvatarModel model;

        public event Action<VisualCue> OnVisualCue;
        public event Action OnSuccessEvent;
        public event Action<bool> OnFailEvent;

        internal BodyShapeController bodyShapeController;
        internal Dictionary<WearableItem, WearableController> wearableControllers = new Dictionary<WearableItem, WearableController>();
        internal FacialFeatureController eyesController;
        internal FacialFeatureController eyebrowsController;
        internal FacialFeatureController mouthController;
        internal AvatarAnimatorLegacy animator;
        internal StickersController stickersController;

        private long lastStickerTimestamp = -1;

        internal bool isLoading = false;

        private Coroutine loadCoroutine;
        private List<string> wearablesInUse = new List<string>();

        private void Awake()
        {
            animator = GetComponent<AvatarAnimatorLegacy>();
            stickersController = GetComponent<StickersController>();
        }

        public void ApplyModel(AvatarModel model, Action onSuccess, Action onFail)
        {
            if (this.model != null && model != null && this.model.Equals(model))
            {
                onSuccess?.Invoke();
                return;
            }

            this.model = new AvatarModel();
            this.model.CopyFrom(model);

            void onSuccessWrapper()
            {
                onSuccess?.Invoke();
                this.OnSuccessEvent -= onSuccessWrapper;
            }

            this.OnSuccessEvent += onSuccessWrapper;

            void onFailWrapper(bool isFatalError)
            {
                onFail?.Invoke();
                this.OnFailEvent -= onFailWrapper;
            }

            this.OnFailEvent += onFailWrapper;

            isLoading = false;

            if (model == null)
            {
                CleanupAvatar();
                this.OnSuccessEvent?.Invoke();
                return;
            }

            StopLoadingCoroutines();
            isLoading = true;
            loadCoroutine = CoroutineStarter.Start(LoadAvatar());
        }

        void StopLoadingCoroutines()
        {
            if (loadCoroutine != null)
                CoroutineStarter.Stop(loadCoroutine);

            loadCoroutine = null;
        }

        public void CleanupAvatar()
        {
            StopLoadingCoroutines();

            eyebrowsController?.CleanUp();
            eyebrowsController = null;

            eyesController?.CleanUp();
            eyesController = null;

            mouthController?.CleanUp();
            mouthController = null;

            bodyShapeController?.CleanUp();
            bodyShapeController = null;

            using (var iterator = wearableControllers.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    iterator.Current.Value.CleanUp();
                }
            }

            wearableControllers.Clear();
            model = null;
            isLoading = false;
            OnFailEvent = null;
            OnSuccessEvent = null;

            CatalogController.RemoveWearablesInUse(wearablesInUse);
            wearablesInUse.Clear();
            OnVisualCue?.Invoke(VisualCue.CleanedUp);
        }

        void CleanUpUnusedItems()
        {
            if (model.wearables == null)
                return;

            var ids = wearableControllers.Keys.ToArray();

            for (var i = 0; i < ids.Length; i++)
            {
                var currentId = ids[i];
                var wearable = wearableControllers[currentId];

                if (!model.wearables.Contains(wearable.id) || !wearable.IsLoadedForBodyShape(model.bodyShape))
                {
                    wearable.CleanUp();
                    wearableControllers.Remove(currentId);
                }
            }
        }

        private IEnumerator LoadAvatar()
        {
            yield return new WaitUntil(() => gameObject.activeSelf);

            bool loadSoftFailed = false;

            WearableItem resolvedBody = null;
            Helpers.Promise<WearableItem> avatarBodyPromise = null;
            if (!string.IsNullOrEmpty(model.bodyShape))
            {
                avatarBodyPromise = CatalogController.RequestWearable(model.bodyShape);
            }
            else
            {
                OnFailEvent?.Invoke(true);
                yield break;
            }

            List<WearableItem> resolvedWearables = new List<WearableItem>();
            List<Helpers.Promise<WearableItem>> avatarWearablePromises = new List<Helpers.Promise<WearableItem>>();
            if (model.wearables != null)
            {
                for (int i = 0; i < model.wearables.Count; i++)
                {
                    avatarWearablePromises.Add(CatalogController.RequestWearable(model.wearables[i]));
                }
            }

            // In this point, all the requests related to the avatar's wearables have been collected and sent to the CatalogController to be sent to kernel as a unique request.
            // From here we wait for the response of the requested wearables and process them.
            if (avatarBodyPromise != null)
            {
                yield return avatarBodyPromise;

                if (!string.IsNullOrEmpty(avatarBodyPromise.error))
                {
                    Debug.LogError(avatarBodyPromise.error);
                    loadSoftFailed = true;
                }
                else
                {
                    resolvedBody = avatarBodyPromise.value;
                    wearablesInUse.Add(avatarBodyPromise.value.id);
                }
            }

            if (resolvedBody == null)
            {
                isLoading = false;
                OnFailEvent?.Invoke(true);
                yield break;
            }

            List<Helpers.Promise<WearableItem>> replacementPromises = new List<Helpers.Promise<WearableItem>>();
            foreach (var avatarWearablePromise in avatarWearablePromises)
            {
                yield return avatarWearablePromise;

                if (!string.IsNullOrEmpty(avatarWearablePromise.error))
                {
                    Debug.LogError(avatarWearablePromise.error);
                    loadSoftFailed = true;
                }
                else
                {
                    WearableItem wearableItem = avatarWearablePromise.value;
                    wearablesInUse.Add(wearableItem.id);
                    if (wearableItem.GetRepresentation(model.bodyShape) != null)
                        resolvedWearables.Add(wearableItem);
                    else
                    {
                        model.wearables.Remove(wearableItem.id);
                        string defaultReplacement = DefaultWearables.GetDefaultWearable(model.bodyShape, wearableItem.data.category);
                        if (!string.IsNullOrEmpty(defaultReplacement))
                        {
                            model.wearables.Add(defaultReplacement);
                            replacementPromises.Add(CatalogController.RequestWearable(defaultReplacement));
                        }
                    }
                }
            }

            foreach (var wearablePromise in replacementPromises)
            {
                yield return wearablePromise;

                if (!string.IsNullOrEmpty(wearablePromise.error))
                {
                    Debug.LogError(wearablePromise.error);
                    loadSoftFailed = true;
                }
                else
                {
                    WearableItem wearableItem = wearablePromise.value;
                    wearablesInUse.Add(wearableItem.id);
                    resolvedWearables.Add(wearableItem);
                }
            }

            bool bodyIsDirty = false;
            if (bodyShapeController != null && bodyShapeController.id != model?.bodyShape)
            {
                bodyShapeController.CleanUp();
                bodyShapeController = null;
                bodyIsDirty = true;
            }

            if (bodyShapeController == null)
            {
                HideAll();
                bodyShapeController = new BodyShapeController(resolvedBody);
                eyesController = FacialFeatureController.CreateDefaultFacialFeature(bodyShapeController.bodyShapeId, Categories.EYES, eyeMaterial);
                eyebrowsController = FacialFeatureController.CreateDefaultFacialFeature(bodyShapeController.bodyShapeId, Categories.EYEBROWS, eyebrowMaterial);
                mouthController = FacialFeatureController.CreateDefaultFacialFeature(bodyShapeController.bodyShapeId, Categories.MOUTH, mouthMaterial);
            }
            else
            {
                //If bodyShape is downloading will call OnWearableLoadingSuccess (and therefore SetupDefaultMaterial) once ready
                if (bodyShapeController.isReady)
                    bodyShapeController.SetupDefaultMaterial(defaultMaterial, model.skinColor, model.hairColor);
            }

            bool wearablesIsDirty = false;
            HashSet<string> unusedCategories = new HashSet<string>(Categories.ALL);
            int wearableCount = resolvedWearables.Count;
            for (int index = 0; index < wearableCount; index++)
            {
                WearableItem wearable = resolvedWearables[index];
                if (wearable == null)
                    continue;

                unusedCategories.Remove(wearable.data.category);
                if (wearableControllers.ContainsKey(wearable))
                {
                    if (wearableControllers[wearable].IsLoadedForBodyShape(bodyShapeController.bodyShapeId))
                        UpdateWearableController(wearable);
                    else
                        wearableControllers[wearable].CleanUp();
                }
                else
                {
                    AddWearableController(wearable);
                    if (wearable.data.category != Categories.EYES && wearable.data.category != Categories.MOUTH && wearable.data.category != Categories.EYEBROWS)
                        wearablesIsDirty = true;
                }
            }

            foreach (var category in unusedCategories)
            {
                switch (category)
                {
                    case Categories.EYES:
                        eyesController = FacialFeatureController.CreateDefaultFacialFeature(bodyShapeController.bodyShapeId, Categories.EYES, eyeMaterial);
                        break;
                    case Categories.MOUTH:
                        mouthController = FacialFeatureController.CreateDefaultFacialFeature(bodyShapeController.bodyShapeId, Categories.MOUTH, mouthMaterial);
                        break;
                    case Categories.EYEBROWS:
                        eyebrowsController = FacialFeatureController.CreateDefaultFacialFeature(bodyShapeController.bodyShapeId, Categories.EYEBROWS, eyebrowMaterial);
                        break;
                }
            }


            HashSet<string> hiddenList = WearableItem.CompoundHidesList(bodyShapeController.bodyShapeId, resolvedWearables);
            if (!bodyShapeController.isReady)
            {
                bodyShapeController.Load(bodyShapeController.bodyShapeId, transform, OnWearableLoadingSuccess, OnBodyShapeLoadingFail);
            }

            foreach (WearableController wearable in wearableControllers.Values)
            {
                if (bodyIsDirty)
                    wearable.boneRetargetingDirty = true;

                wearable.Load(bodyShapeController.bodyShapeId, transform, OnWearableLoadingSuccess, x => OnWearableLoadingFail(x));
                yield return null;
            }

            yield return new WaitUntil(() => bodyShapeController.isReady && wearableControllers.Values.All(x => x.isReady));

            eyesController?.Load(bodyShapeController, model.eyeColor);
            eyebrowsController?.Load(bodyShapeController, model.hairColor);
            mouthController?.Load(bodyShapeController, model.skinColor);

            yield return new WaitUntil(() =>
                (eyebrowsController == null || (eyebrowsController != null && eyebrowsController.isReady)) &&
                (eyesController == null || (eyesController != null && eyesController.isReady)) &&
                (mouthController == null || (mouthController != null && mouthController.isReady)));

            if (bodyIsDirty || wearablesIsDirty)
            {
                OnVisualCue?.Invoke(VisualCue.Loaded);
            }

            bodyShapeController.SetActiveParts(unusedCategories.Contains(Categories.LOWER_BODY), unusedCategories.Contains(Categories.UPPER_BODY), unusedCategories.Contains(Categories.FEET));
            bodyShapeController.UpdateVisibility(hiddenList);
            foreach (WearableController wearableController in wearableControllers.Values)
            {
                wearableController.UpdateVisibility(hiddenList);
            }

            CleanUpUnusedItems();

            isLoading = false;

            SetWearableBones();
            UpdateExpressions(model.expressionTriggerId, model.expressionTriggerTimestamp);
            if (lastStickerTimestamp != model.stickerTriggerTimestamp && model.stickerTriggerId != null)
            {
                lastStickerTimestamp = model.stickerTriggerTimestamp;
                stickersController?.PlayEmote(model.stickerTriggerId);
            }

            if (loadSoftFailed)
            {
                OnFailEvent?.Invoke(false);
            }
            else
            {
                OnSuccessEvent?.Invoke();
            }
        }

        void OnWearableLoadingSuccess(WearableController wearableController)
        {
            if (wearableController == null || model == null)
            {
                Debug.LogWarning($"WearableSuccess was called wrongly: IsWearableControllerNull=>{wearableController == null}, IsModelNull=>{model == null}");
                OnWearableLoadingFail(wearableController, 0);
                return;
            }

            wearableController.SetupDefaultMaterial(defaultMaterial, model.skinColor, model.hairColor);
        }

        void OnBodyShapeLoadingFail(WearableController wearableController)
        {
            Debug.LogError($"Avatar: {model?.name}  -  Failed loading bodyshape: {wearableController?.id}");
            CleanupAvatar();
            OnFailEvent?.Invoke(true);
        }

        void OnWearableLoadingFail(WearableController wearableController, int retriesCount = MAX_RETRIES)
        {
            if (retriesCount <= 0)
            {
                Debug.LogError($"Avatar: {model?.name}  -  Failed loading wearable: {wearableController?.id}");
                CleanupAvatar();
                OnFailEvent?.Invoke(false);
                return;
            }

            wearableController.Load(bodyShapeController.id, transform, OnWearableLoadingSuccess, x => OnWearableLoadingFail(x, retriesCount - 1));
        }

        private void SetWearableBones()
        {
            //NOTE(Brian): Set bones/rootBone of all wearables to be the same of the baseBody,
            //             so all of them are animated together.
            var mainSkinnedRenderer = bodyShapeController.skinnedMeshRenderer;

            using (var enumerator = wearableControllers.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    enumerator.Current.Value.SetAnimatorBones(mainSkinnedRenderer);
                }
            }
        }

        public void UpdateExpressions(string id, long timestamp)
        {
            model.expressionTriggerId = id;
            model.expressionTriggerTimestamp = timestamp;
            animator.SetExpressionValues(id, timestamp);
        }

        private void AddWearableController(WearableItem wearable)
        {
            if (wearable == null)
                return;
            switch (wearable.data.category)
            {
                case Categories.EYES:
                    eyesController = new FacialFeatureController(wearable, eyeMaterial);
                    break;
                case Categories.EYEBROWS:
                    eyebrowsController = new FacialFeatureController(wearable, eyebrowMaterial);
                    break;
                case Categories.MOUTH:
                    mouthController = new FacialFeatureController(wearable, mouthMaterial);
                    break;
                case Categories.BODY_SHAPE:
                    break;

                default:
                    var wearableController = new WearableController(wearable);
                    wearableControllers.Add(wearable, wearableController);
                    break;
            }
        }

        private void UpdateWearableController(WearableItem wearable)
        {
            var wearableController = wearableControllers[wearable];
            switch (wearableController.category)
            {
                case Categories.EYES:
                case Categories.EYEBROWS:
                case Categories.MOUTH:
                case Categories.BODY_SHAPE:
                    break;
                default:
                    //If wearable is downloading will call OnWearableLoadingSuccess(and therefore SetupDefaultMaterial) once ready
                    if (wearableController.isReady)
                        wearableController.SetupDefaultMaterial(defaultMaterial, model.skinColor, model.hairColor);
                    break;
            }
        }

        //TODO: Remove/replace once the class is easily mockable.
        protected void CopyFrom(AvatarRenderer original)
        {
            this.wearableControllers = original.wearableControllers;
            this.mouthController = original.mouthController;
            this.bodyShapeController = original.bodyShapeController;
            this.eyebrowsController = original.eyebrowsController;
            this.eyesController = original.eyesController;
        }

        public void SetVisibility(bool newVisibility)
        {
            //NOTE(Brian): Avatar being loaded needs the renderer.enabled as false until the loading finishes.
            //             So we can' manipulate the values because it'd show an incomplete avatar. Its easier to just deactivate the gameObject.
            if (gameObject.activeSelf != newVisibility)
                gameObject.SetActive(newVisibility);
        }

        public MeshRenderer GetLODRenderer() { return lodRenderer; }

        public Transform GetTransform() { return transform; }

        private void HideAll()
        {
            // TODO: Cache this somewhere (maybe when the LoadAvatar finishes) instead of fetching this on every call
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
        }

        protected virtual void OnDestroy() { CleanupAvatar(); }
    }
}