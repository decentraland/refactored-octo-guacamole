using UnityEngine;

namespace DCL
{
    public class AssetPromise_Texture_Settings
    {
        public const TextureWrapMode DEFAULT_WRAP_MODE = TextureWrapMode.Clamp;
        public const FilterMode DEFAULT_FILTER_MODE = FilterMode.Bilinear;
        public const int ENFORCED_TEXTURE_MAX_SIZE = 1024;

        public TextureWrapMode wrapMode = DEFAULT_WRAP_MODE;
        public FilterMode filterMode = DEFAULT_FILTER_MODE;
        public bool storeDefaultTextureInAdvance = false;
        public bool storeTexAsNonReadable = true;
        public bool limitTextureSize = false;
    }
}