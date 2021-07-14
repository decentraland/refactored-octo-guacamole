using System;
using System.Collections;
using System.IO;

public delegate bool AssetIdConverter(string uri, out string id);

namespace UnityGLTF.Loader
{
    public interface ILoader
    {
        IEnumerator LoadStream(string relativeFilePath);

        void LoadStreamSync(string jsonFilePath);

        Stream LoadedStream { get; }

        bool HasSyncLoadMethod { get; }

        AssetIdConverter assetIdConverter { get; }
    }
}