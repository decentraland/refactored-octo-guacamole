using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityGLTF.Cache;
using Object = UnityEngine.Object;

namespace DCL
{
    public class Asset_Texture : Asset, ITexture
    {
        public Texture2D texture { get; set; }
        public Asset_Texture dependencyAsset; // to store the default tex asset and release it accordingly

        public int originalWidth { get; internal set; }
        public int originalHeight { get; internal set; }

        public event System.Action OnCleanup;

        public void ConfigureTexture(TextureWrapMode textureWrapMode, FilterMode textureFilterMode, bool makeNoLongerReadable = true)
        {
            if (texture == null)
                return;

            texture.wrapMode = textureWrapMode;
            texture.filterMode = textureFilterMode;
            texture.Compress(false);
            texture.Apply(textureFilterMode != FilterMode.Point, makeNoLongerReadable);
        }

        public override void Cleanup()
        {
            OnCleanup?.Invoke();
            PersistentAssetCache.RemoveImage(texture);
            Object.Destroy(texture);
        }

        public void Dispose() { Cleanup(); }

        public int width => texture.width;
        public int height => texture.height;
    }
}