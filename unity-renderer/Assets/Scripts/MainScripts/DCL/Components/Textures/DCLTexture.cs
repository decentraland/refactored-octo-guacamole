using DCL.Components;
using DCL.Controllers;
using DCL.Models;
using System;
using System.Collections;
using DCL.Helpers;
using UnityEngine;
using System.Collections.Generic;

namespace DCL
{
    public class DCLTexture : BaseDisposable
    {
        [System.Serializable]
        public class Model : BaseModel
        {
            public string src;
            public BabylonWrapMode wrap = BabylonWrapMode.CLAMP;
            public FilterMode samplingMode = FilterMode.Bilinear;
            public bool hasAlpha = false;

            public override BaseModel GetDataFromJSON(string json) { return Utils.SafeFromJson<Model>(json); }
        }

        public enum BabylonWrapMode
        {
            CLAMP,
            WRAP,
            MIRROR
        }

        public AssetPromise_Texture texturePromise { private set; get; } = null;

        public TextureWrapMode unityWrap;
        public FilterMode unitySamplingMode;
        public Texture2D texture;
        protected bool isDisposed;

        public override int GetClassId() { return (int) CLASS_ID.TEXTURE; }

        public DCLTexture() { model = new Model(); }

        public static IEnumerator FetchFromComponent(IParcelScene scene, string componentId,
            System.Action<Texture2D> OnFinish)
        {
            yield return FetchTextureComponent(scene, componentId, (dclTexture) => { OnFinish?.Invoke(dclTexture.texture); });
        }

        public static IEnumerator FetchTextureComponent(IParcelScene scene, string componentId,
            System.Action<DCLTexture> OnFinish)
        {
            if (!scene.disposableComponents.ContainsKey(componentId))
            {
                Debug.Log($"couldn't fetch texture, the DCLTexture component with id {componentId} doesn't exist");
                yield break;
            }

            DCLTexture textureComponent = scene.disposableComponents[componentId] as DCLTexture;

            if (textureComponent == null)
            {
                Debug.Log($"couldn't fetch texture, the shared component with id {componentId} is NOT a DCLTexture");
                yield break;
            }

            yield return new WaitUntil(() => textureComponent.texture != null);

            OnFinish.Invoke(textureComponent);
        }

        public override IEnumerator ApplyChanges(BaseModel newModel)
        {
            yield return new WaitUntil(() => CommonScriptableObjects.rendererState.Get());

            //If the scene creates and destroy the component before our renderer has been turned on bad things happen!
            //TODO: Analyze if we can catch this upstream and stop the IEnumerator
            if (isDisposed)
                yield break;

            Model model = (Model) newModel;

            unitySamplingMode = model.samplingMode;

            switch (model.wrap)
            {
                case BabylonWrapMode.CLAMP:
                    unityWrap = TextureWrapMode.Clamp;
                    break;
                case BabylonWrapMode.WRAP:
                    unityWrap = TextureWrapMode.Repeat;
                    break;
                case BabylonWrapMode.MIRROR:
                    unityWrap = TextureWrapMode.Mirror;
                    break;
            }

            if (texture == null && !string.IsNullOrEmpty(model.src))
            {
                bool isBase64 = model.src.Contains("image/png;base64");

                if (isBase64)
                {
                    string base64Data = model.src.Substring(model.src.IndexOf(',') + 1);

                    // The used texture variable can't be null for the ImageConversion.LoadImage to work
                    if (texture == null)
                    {
                        texture = new Texture2D(1, 1);
                    }

                    if (!ImageConversion.LoadImage(texture, Convert.FromBase64String(base64Data)))
                    {
                        Debug.LogError($"DCLTexture with id {id} couldn't parse its base64 image data.");
                    }

                    if (texture != null)
                    {
                        texture.wrapMode = unityWrap;
                        texture.filterMode = unitySamplingMode;
                        texture.Compress(false);
                        texture.Apply(unitySamplingMode != FilterMode.Point, true);
                    }
                }
                else
                {
                    string contentsUrl = string.Empty;
                    bool isExternalURL = model.src.Contains("http://") || model.src.Contains("https://");

                    if (isExternalURL)
                        contentsUrl = model.src;
                    else
                        scene.contentProvider.TryGetContentsUrl(model.src, out contentsUrl);

                    if (!string.IsNullOrEmpty(contentsUrl))
                    {
                        if (texturePromise != null)
                            AssetPromiseKeeper_Texture.i.Forget(texturePromise);

                        texturePromise = new AssetPromise_Texture(contentsUrl, new AssetPromise_Texture_Settings()
                        {
                            wrapMode = unityWrap,
                            filterMode = unitySamplingMode,
                            storeDefaultTextureInAdvance = true,
                            storeTexAsNonReadable = true,
                            limitTextureSize = true
                        });
                        texturePromise.OnSuccessEvent += (x) => texture = x.texture;
                        texturePromise.OnFailEvent += (x) => { texture = null; };

                        AssetPromiseKeeper_Texture.i.Keep(texturePromise);
                        yield return texturePromise;
                    }
                }
            }
        }

        private int refCount;

        public virtual void AttachTo(PBRMaterial material) { AddRefCount(); }

        public virtual void AttachTo(BasicMaterial material) { AddRefCount(); }

        public virtual void AttachTo(UIImage image) { AddRefCount(); }

        public virtual void DetachFrom(PBRMaterial material) { RemoveRefCount(); }

        public virtual void DetachFrom(BasicMaterial material) { RemoveRefCount(); }

        public virtual void DetachFrom(UIImage image) { RemoveRefCount(); }

        public void AddRefCount() { refCount++; }

        public void RemoveRefCount()
        {
            if (refCount == 0)
                Dispose();
        }

        public override void Dispose()
        {
            isDisposed = true;
            if (texturePromise != null)
            {
                AssetPromiseKeeper_Texture.i.Forget(texturePromise);
                texturePromise = null;
            }

            base.Dispose();
        }
    }
}