using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DCL.Components
{
    public class UIImage : UIShape<UIImageReferencesContainer, UIImage.Model>
    {
        [System.Serializable]
        new public class Model : UIShape.Model
        {
            public string source;
            public float sourceLeft = 0f;
            public float sourceTop = 0f;
            public float sourceWidth = 1f;
            public float sourceHeight = 1f;
            public float paddingTop = 0f;
            public float paddingRight = 0f;
            public float paddingBottom = 0f;
            public float paddingLeft = 0f;
            public bool sizeInPixels = true;

            public override BaseModel GetDataFromJSON(string json) { return Utils.SafeFromJson<Model>(json); }
        }

        public override string referencesContainerPrefabName => "UIImage";

        DCLTexture dclTexture = null;
        private int originalWidth;
        private int originalHeight;

        public UIImage() { model = new Model(); }

        public override int GetClassId() { return (int) CLASS_ID.UI_IMAGE_SHAPE; }

        public override void AttachTo(IDCLEntity entity, System.Type overridenAttachedType = null) { Debug.LogError("Aborted UIImageShape attachment to an entity. UIShapes shouldn't be attached to entities."); }

        public override void DetachFrom(IDCLEntity entity, System.Type overridenAttachedType = null) { }

        Coroutine fetchRoutine;

        public override IEnumerator ApplyChanges(BaseModel newModel)
        {
            RectTransform parentRecTransform = referencesContainer.GetComponentInParent<RectTransform>();

            // Fetch texture
            if (!string.IsNullOrEmpty(model.source))
            {
                if (dclTexture == null || (dclTexture != null && dclTexture.id != model.source))
                {
                    if (fetchRoutine != null)
                    {
                        CoroutineStarter.Stop(fetchRoutine);
                        fetchRoutine = null;
                    }

                    IEnumerator fetchIEnum = DCLTexture.FetchTextureComponent(scene, model.source, (downloadedTexture) =>
                    {
                        referencesContainer.image.texture = downloadedTexture.texture;
                        originalWidth = downloadedTexture.texture.width;
                        originalHeight = downloadedTexture.texture.height;

                        if (downloadedTexture.texturePromise?.asset != null)
                        {
                            originalWidth = downloadedTexture.texturePromise.asset.originalWidth;
                            originalHeight = downloadedTexture.texturePromise.asset.originalHeight;
                        }

                        fetchRoutine = null;
                        dclTexture?.DetachFrom(this);
                        dclTexture = downloadedTexture;
                        dclTexture.AttachTo(this);

                        ConfigureUVRect(parentRecTransform);
                    });

                    fetchRoutine = CoroutineStarter.Start(fetchIEnum);
                }
            }
            else
            {
                referencesContainer.image.texture = null;
                dclTexture?.DetachFrom(this);
                dclTexture = null;
            }

            referencesContainer.image.enabled = model.visible;
            referencesContainer.image.color = Color.white;

            ConfigureUVRect(parentRecTransform);

            // Apply padding
            referencesContainer.paddingLayoutGroup.padding.bottom = Mathf.RoundToInt(model.paddingBottom);
            referencesContainer.paddingLayoutGroup.padding.top = Mathf.RoundToInt(model.paddingTop);
            referencesContainer.paddingLayoutGroup.padding.left = Mathf.RoundToInt(model.paddingLeft);
            referencesContainer.paddingLayoutGroup.padding.right = Mathf.RoundToInt(model.paddingRight);

            Utils.ForceRebuildLayoutImmediate(parentRecTransform);
            return null;
        }

        private void ConfigureUVRect(RectTransform parentRecTransform)
        {
            if (referencesContainer.image.texture == null)
                return;

            // Configure uv rect
            Vector2 normalizedSourceCoordinates = new Vector2(
                model.sourceLeft / originalWidth,
                -model.sourceTop / originalHeight);

            Vector2 normalizedSourceSize = new Vector2(
                model.sourceWidth * (model.sizeInPixels ? 1f : parentRecTransform.rect.width) /
                originalWidth,
                model.sourceHeight * (model.sizeInPixels ? 1f : parentRecTransform.rect.height) /
                originalHeight);

            referencesContainer.image.uvRect = new Rect(normalizedSourceCoordinates.x,
                normalizedSourceCoordinates.y + (1 - normalizedSourceSize.y),
                normalizedSourceSize.x,
                normalizedSourceSize.y);
        }

        public override void Dispose()
        {
            if (fetchRoutine != null)
            {
                CoroutineStarter.Stop(fetchRoutine);
                fetchRoutine = null;
            }

            dclTexture?.DetachFrom(this);

            if (referencesContainer != null)
                Utils.SafeDestroy(referencesContainer.gameObject);

            base.Dispose();
        }
    }
}