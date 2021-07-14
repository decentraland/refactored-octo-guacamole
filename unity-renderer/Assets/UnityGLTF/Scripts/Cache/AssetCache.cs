using GLTF.Schema;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityGLTF.Cache
{
    /// <summary>
    /// Caches textures and counts their references to minimize memory usage
    /// </summary>
    public static class PersistentAssetCache
    {
        public static Dictionary<string, RefCountedStreamData> StreamCacheByUri = new Dictionary<string, RefCountedStreamData>();
        public static Dictionary<string, RefCountedTextureData> ImageCacheByUri = new Dictionary<string, RefCountedTextureData>();
        public static Dictionary<string, RefCountedMaterialData> MaterialCacheByCRC = new Dictionary<string, RefCountedMaterialData>();

        /// <summary>
        /// Add image to persistent cache providing the uri and idSuffix in which the image can be looked up.
        /// Both ids are used from the GLTFSceneImporter to generate an unique id among many GLTFs.
        /// </summary>
        /// <param name="uri">The relative or local uri of the image</param>
        /// <param name="idSuffix">A global identifier to prevent collisions in case the local uri between different loaded images is the same</param>
        /// <param name="texture">The texture to cached image</param>
        public static RefCountedTextureData AddImage(string uri, string idSuffix, Texture2D texture)
        {
            var key = GetCacheId(uri, idSuffix);
            return AddImage(key, texture);
        }

        /// <summary>
        /// Add image to persistent cache providing the exact id in which the image can be looked up.
        /// </summary>
        /// <param name="fullId">The relative or local uri of the image</param>
        /// <param name="texture">The texture to cached image</param>
        public static RefCountedTextureData AddImage(string fullId, Texture2D texture)
        {
            var refCountedTextureData = new RefCountedTextureData(fullId, texture);
            ImageCacheByUri[fullId] = refCountedTextureData;
            return refCountedTextureData;
        }

        /// <summary>
        /// Check if cached image exists
        /// </summary>
        /// <param name="uri">The relative or local uri of the image</param>
        /// <param name="idSuffix">A global identifier to prevent collisions in case the local uri between different loaded images is the same</param>
        /// <returns></returns>
        public static bool HasImage(string uri, string idSuffix)
        {
            string key = GetCacheId(uri, idSuffix);
            bool result = HasImage(key);
            return result;
        }

        /// <summary>
        /// Check if cached image exists
        /// </summary>
        /// <param name="fullId">id in which the image was stored using AddImage</param>
        /// <returns>True if the image exists</returns>
        public static bool HasImage(string fullId) { return ImageCacheByUri.ContainsKey(fullId); }

        /// <summary>
        /// Get image to persistent cache providing the uri and idSuffix in which the image can be looked up.
        /// Both ids are used from the GLTFSceneImporter to generate an unique id among many GLTFs.
        /// </summary>
        /// <param name="uri">The relative or local uri of the image</param>
        /// <param name="idSuffix">A global identifier to prevent collisions in case the local uri between different loaded images is the same</param>
        /// <returns></returns>
        public static RefCountedTextureData GetImage(string uri, string idSuffix) { return GetImage(GetCacheId(uri, idSuffix)); }

        public static RefCountedTextureData GetImage(string fullId) { return ImageCacheByUri[fullId]; }

        /// <summary>
        /// Remove image from persistent cache
        /// </summary>
        /// <param name="fullId">full id of the cached image</param>
        public static void RemoveImage(string fullId)
        {
            if (string.IsNullOrEmpty(fullId))
                return;

            if (HasImage(fullId))
                ImageCacheByUri.Remove(fullId);
        }

        /// <summary>
        /// Remove image from persistent cache
        /// </summary>
        /// <param name="texture">Reference to the cached image to find and remove</param>
        public static void RemoveImage(Texture texture)
        {
            string foundKey = ImageCacheByUri.Where(x => x.Value.Texture == texture).Select(x => x.Key).FirstOrDefault();

            if (foundKey == null)
                return;

            ImageCacheByUri.Remove(foundKey);
        }

        /// <summary>
        /// Add buffer to persistent cache providing the exact id in which the buffer can be looked up.
        /// </summary>
        /// <param name="uri">The relative or local uri of the buffer</param>
        /// <param name="idSuffix">A global identifier to prevent collisions in case the local uri between different loaded buffers is the same</param>
        /// <param name="refCountedStream"></param>
        public static RefCountedStreamData AddBuffer(string uri, string idSuffix, Stream refCountedStream)
        {
            var key = GetCacheId(uri, idSuffix);
            return AddBuffer(key, refCountedStream);
        }

        /// <summary>
        /// Add buffer to persistent cache providing the exact id in which the buffer can be looked up.
        /// </summary>
        /// <param name="fullId">full id of the of the buffer</param>
        public static RefCountedStreamData AddBuffer(string fullId, Stream refCountedStream)
        {
            StreamCacheByUri[fullId] = new RefCountedStreamData(fullId, refCountedStream);
            return StreamCacheByUri[fullId];
        }

        /// <summary>
        /// Check if buffer is cached
        /// </summary>
        /// <param name="uri">The relative or local uri of the buffer</param>
        /// <param name="idSuffix">A global identifier to prevent collisions in case the local uri between different loaded buffers is the same</param>
        /// <returns>True if its cached</returns>
        public static bool HasBuffer(string uri, string idSuffix)
        {
            string key = GetCacheId(uri, idSuffix);
            return HasBuffer(key);
        }

        /// <summary>
        /// Check if buffer is cached
        /// </summary>
        /// <param name="fullId">full id of the buffer in the cache</param>
        /// <returns>True if its cached</returns>
        public static bool HasBuffer(string fullId) { return StreamCacheByUri.ContainsKey(fullId); }

        /// <summary>
        /// Get any cached buffer
        /// </summary>
        /// <param name="uri">The relative or local uri of the buffer</param>
        /// <param name="idSuffix">A global identifier to prevent collisions in case the local uri between different loaded images is the same</param>
        /// <returns>The container with the cached buffer</returns>
        public static RefCountedStreamData GetBuffer(string uri, string idSuffix) { return GetBuffer(GetCacheId(uri, idSuffix)); }

        public static RefCountedStreamData GetBuffer(string fullId) { return StreamCacheByUri[fullId]; }

        /// <summary>
        /// Remove buffer from persistent cache
        /// </summary>
        /// <param name="fullId">full id of the buffer</param>
        public static void RemoveBuffer(string fullId)
        {
            if (string.IsNullOrEmpty(fullId))
                return;

            if (HasBuffer(fullId))
                StreamCacheByUri.Remove(fullId);
        }

        /// <summary>
        /// This returns a full id given two components
        /// </summary>
        /// <param name="a">first id component</param>
        /// <param name="b">second id component</param>
        /// <returns>The resulting id in the form of "a@b"</returns>
        public static string GetCacheId(string a, string b) { return $"{a}@{b}"; }
    }

    /// <summary>
    /// Caches data in order to construct a unity object
    /// </summary>
    public class AssetCache : IDisposable
    {
        /// <summary>
        /// Streams to the images to be loaded
        /// </summary>
        public Stream[] ImageStreamCache { get; private set; }

        /// <summary>
        /// Loaded raw texture data
        /// </summary>
        public Texture2D[] ImageCache { get; private set; }

        /// <summary>
        /// Textures to be used for assets. Textures from image cache with samplers applied
        /// </summary>
        public TextureCacheData[] TextureCache { get; private set; }

        /// <summary>
        /// Cache for materials to be applied to the meshes
        /// </summary>
        public MaterialCacheData[] MaterialCache { get; private set; }

        /// <summary>
        /// Byte buffers that represent the binary contents that get parsed
        /// </summary>
        public BufferCacheData[] BufferCache { get; private set; }

        /// <summary>
        /// Cache of loaded meshes
        /// </summary>
        public MeshCacheData[][] MeshCache { get; private set; }

        /// <summary>
        /// Cache of loaded animations
        /// </summary>
        public AnimationCacheData[] AnimationCache { get; private set; }

        /// <summary>
        /// Cache of loaded node objects
        /// </summary>
        public GameObject[] NodeCache { get; private set; }

        /// <summary>
        /// Creates an asset cache which caches objects used in scene
        /// </summary>
        /// <param name="root">A glTF root whose assets will eventually be cached here</param>
        public AssetCache(GLTFRoot root)
        {
            ImageCache = new Texture2D[root.Images?.Count ?? 0];
            ImageStreamCache = new Stream[ImageCache.Length];
            TextureCache = new TextureCacheData[root.Textures?.Count ?? 0];
            MaterialCache = new MaterialCacheData[root.Materials?.Count ?? 0];
            BufferCache = new BufferCacheData[root.Buffers?.Count ?? 0];
            MeshCache = new MeshCacheData[root.Meshes?.Count ?? 0][];
            for (int i = 0; i < MeshCache.Length; ++i)
            {
                MeshCache[i] = new MeshCacheData[root.Meshes?[i].Primitives.Count ?? 0];
            }

            NodeCache = new GameObject[root.Nodes?.Count ?? 0];
            AnimationCache = new AnimationCacheData[root.Animations?.Count ?? 0];
        }

        public void Dispose()
        {
            ImageCache = null;
            ImageStreamCache = null;
            TextureCache = null;
            MaterialCache = null;
            if (BufferCache != null)
            {
                foreach (BufferCacheData bufferCacheData in BufferCache)
                {
                    if (bufferCacheData != null)
                    {
                        if (bufferCacheData.Stream != null)
                        {
#if !WINDOWS_UWP
                            bufferCacheData.Stream.Close();
#else
                            bufferCacheData.Stream.Dispose();
#endif
                        }

                        bufferCacheData.Dispose();
                    }
                }

                BufferCache = null;
            }

            MeshCache = null;
            AnimationCache = null;
        }
    }
}