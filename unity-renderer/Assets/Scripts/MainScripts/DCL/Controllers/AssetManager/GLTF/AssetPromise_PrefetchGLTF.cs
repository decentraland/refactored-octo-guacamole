namespace DCL
{
    public class AssetPromise_PrefetchGLTF : AssetPromise_GLTF
    {
        public AssetPromise_PrefetchGLTF(ContentProvider provider, string url, string hash) : base(provider, url, hash) { settings.visibleFlags = AssetPromiseSettings_Rendering.VisibleFlags.INVISIBLE; }

        protected override void OnBeforeLoadOrReuse() { asset.container.name = "GLTF: " + fileName; }

        public override object GetId() { return base.GetId(); }

        protected override void OnLoad(System.Action OnSuccess, System.Action OnFail) { base.OnLoad(OnSuccess, OnFail); }

        void ParseGLTFWebRequestedFile(ref string requestedFileName) { provider.TryGetContentsUrl(assetDirectoryPath + requestedFileName, out requestedFileName); }

        protected override void OnReuse(System.Action OnSuccess) { asset.Show(false, OnSuccess); }

        protected override bool AddToLibrary()
        {
            library.Add(asset);
            return true;
        }

        internal override void Load()
        {
            if (!library.Contains(GetId()))
            {
                base.Load();
            }
            else
            {
                CallAndClearEvents(false);
            }
        }

        protected override void OnCancelLoading() { base.OnCancelLoading(); }

        protected override Asset_GLTF GetAsset(object id) { return library.Get(id); }
    }
}