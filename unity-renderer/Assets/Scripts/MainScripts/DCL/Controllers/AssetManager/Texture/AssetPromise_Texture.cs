using System;
using DCL.Configuration;
using UnityEngine;
using UnityEngine.Networking;

namespace DCL
{
    public class AssetPromise_Texture : AssetPromise<Asset_Texture>
    {
        static readonly byte[] questionMarkPNG = { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 8, 0, 0, 0, 8, 8, 2, 0, 0, 0, 75, 109, 41, 220, 0, 0, 0, 65, 73, 68, 65, 84, 8, 29, 85, 142, 81, 10, 0, 48, 8, 66, 107, 236, 254, 87, 110, 106, 35, 172, 143, 74, 243, 65, 89, 85, 129, 202, 100, 239, 146, 115, 184, 183, 11, 109, 33, 29, 126, 114, 141, 75, 213, 65, 44, 131, 70, 24, 97, 46, 50, 34, 72, 25, 39, 181, 9, 251, 205, 14, 10, 78, 123, 43, 35, 17, 17, 228, 109, 164, 219, 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130, };

        private const string PLAIN_BASE64_PROTOCOL = "data:text/plain;base64,";

        string url;
        string idWithTexSettings;
        string idWithDefaultTexSettings;

        WebRequestAsyncOperation webRequestOp = null;

        readonly AssetPromise_Texture_Settings settings;

        public AssetPromise_Texture(string textureUrl) : this(textureUrl, new AssetPromise_Texture_Settings()) { }

        public AssetPromise_Texture(string textureUrl, bool storeTexAsNonReadable)
            : this(textureUrl, new AssetPromise_Texture_Settings() { storeTexAsNonReadable = storeTexAsNonReadable }) { }

        public AssetPromise_Texture(string textureUrl, TextureWrapMode textureWrapMode, FilterMode textureFilterMode)
            : this(textureUrl, new AssetPromise_Texture_Settings() { wrapMode = textureWrapMode, filterMode = textureFilterMode }) { }

        public AssetPromise_Texture(string textureUrl, AssetPromise_Texture_Settings settings)
        {
            url = textureUrl;
            this.settings = settings;
            idWithDefaultTexSettings = ConstructId(url, AssetPromise_Texture_Settings.DEFAULT_WRAP_MODE, AssetPromise_Texture_Settings.DEFAULT_FILTER_MODE);
            idWithTexSettings = UsesDefaultWrapAndFilterMode() ? idWithDefaultTexSettings : ConstructId(url, settings.wrapMode, settings.filterMode);
        }

        protected override void OnAfterLoadOrReuse() { }

        protected override void OnBeforeLoadOrReuse() { }

        protected override object GetLibraryAssetCheckId() { return idWithTexSettings; }

        protected override void OnCancelLoading()
        {
            if (webRequestOp != null)
                webRequestOp.Dispose();
        }

        protected override void OnLoad(Action OnSuccess, Action OnFail)
        {
            // Reuse the already-stored default texture, we duplicate it and set the needed config afterwards in AddToLibrary()
            if (library.Contains(idWithDefaultTexSettings) && !UsesDefaultWrapAndFilterMode())
            {
                OnSuccess?.Invoke();
                return;
            }

            if (!url.StartsWith(PLAIN_BASE64_PROTOCOL))
            {
                webRequestOp = DCL.Environment.i.platform.webRequest.GetTexture(
                    url: url,
                    OnSuccess: (webRequestResult) =>
                    {
                        if (asset != null)
                        {
                            var texture = DownloadHandlerTexture.GetContent(webRequestResult);
                            asset.originalWidth = texture.width;
                            asset.originalHeight = texture.height;

                            if (settings.limitTextureSize)
                            {
                                TextureHelpers.EnsureTexture2DMaxSize(ref texture, AssetPromise_Texture_Settings.ENFORCED_TEXTURE_MAX_SIZE);
                            }
                            asset.texture = texture;

                            if (IsQuestionMarkPNG(texture))
                                OnFail?.Invoke();
                            else
                                OnSuccess?.Invoke();
                        }
                        else
                        {
                            OnFail?.Invoke();
                        }
                    },
                    OnFail: (webRequestResult) =>
                    {
                        OnFail?.Invoke();
                    });
            }
            else
            {
                //For Base64 protocols we just take the bytes and create the texture
                //to avoid Unity's web request issue with large URLs
                byte[] decodedTexture = Convert.FromBase64String(url.Substring(PLAIN_BASE64_PROTOCOL.Length));
                asset.texture = new Texture2D(1, 1);
                asset.texture.LoadImage(decodedTexture);
                asset.originalWidth = asset.texture.width;
                asset.originalHeight = asset.texture.height;
                OnSuccess?.Invoke();
            }
        }

        protected override bool AddToLibrary()
        {
            if (settings.storeDefaultTextureInAdvance && !UsesDefaultWrapAndFilterMode())
            {
                if (!library.Contains(idWithDefaultTexSettings))
                {
                    // Save default texture asset
                    asset.id = idWithDefaultTexSettings;
                    asset.ConfigureTexture(AssetPromise_Texture_Settings.DEFAULT_WRAP_MODE,
                        AssetPromise_Texture_Settings.DEFAULT_FILTER_MODE, false);

                    if (!library.Add(asset))
                    {
                        Debug.Log("add to library fail?");
                        return false;
                    }
                }

                // By always using library.Get() for the default tex we have stored, we increase its references counter,
                // that will come in handy for removing that default tex when there is no one using it
                var defaultTexAsset = library.Get(idWithDefaultTexSettings);
                asset = defaultTexAsset.Clone() as Asset_Texture;
                asset.dependencyAsset = defaultTexAsset;
                asset.texture = TextureHelpers.CopyTexture(defaultTexAsset.texture);
            }

            asset.id = idWithTexSettings;
            asset.ConfigureTexture(settings.wrapMode, settings.filterMode, settings.storeTexAsNonReadable);

            if (!library.Add(asset))
            {
                Debug.Log("add to library fail?");
                return false;
            }

            asset = library.Get(asset.id);
            return true;
        }

        string ConstructId(string textureUrl, TextureWrapMode textureWrapMode, FilterMode textureFilterMode) { return ((int)textureWrapMode).ToString() + ((int)textureFilterMode).ToString() + textureUrl; }

        public override object GetId()
        {
            // We only use the id-with-settings when storing/reading from the library
            return idWithDefaultTexSettings;
        }

        public bool UsesDefaultWrapAndFilterMode()
        {
            return settings.wrapMode == AssetPromise_Texture_Settings.DEFAULT_WRAP_MODE
                   && settings.filterMode == AssetPromise_Texture_Settings.DEFAULT_FILTER_MODE;
        }

        internal bool IsQuestionMarkPNG(Texture tex)
        {
            if (!tex)
                return true;

            if (tex.width != 8 || tex.height != 8)
                return false;

            byte[] png1 = (tex as Texture2D).EncodeToPNG();
            for (int i = 0; i < questionMarkPNG.Length; i++)
                if (!png1[i].Equals(questionMarkPNG[i]))
                    return false;
            return true;
        }
    }
}