using GLTF;
using GLTF.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
#if !WINDOWS_UWP
using System.Threading;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Loader;
using Object = UnityEngine.Object;
#if !WINDOWS_UWP
using ThreadPriority = System.Threading.ThreadPriority;
#endif
using WrapMode = UnityEngine.WrapMode;

namespace UnityGLTF
{
    public struct MeshConstructionData
    {
        public MeshPrimitive Primitive { get; set; }
        public Dictionary<string, AttributeAccessor> MeshAttributes { get; set; }
    }

    public class UnityMeshData
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] Uv1;
        public Vector2[] Uv2;
        public Vector2[] Uv3;
        public Vector2[] Uv4;
        public Color[] Colors;
        public int[] Triangles;
        public Vector4[] Tangents;
        public BoneWeight[] BoneWeights;
    }

    /// <summary>
    /// Converts gltf animation data to unity
    /// </summary>
    public delegate float[] ValuesConvertion(NumericArray data, int frame);

    public class GLTFSceneImporter : IDisposable
    {
        public static bool VERBOSE = false;
        public static bool PROFILING_ENABLED = false;

        public enum ColliderType
        {
            None,
            Box,
            Mesh,
            MeshConvex
        }

        /// <summary>
        /// Maximum LOD
        /// </summary>
        public int maximumLod = 300;

        /// <summary>
        /// Timeout for certain threading operations
        /// </summary>
        public int Timeout = 8;

        /// <summary>
        /// Use Multithreading or not
        /// </summary>
        public bool isMultithreaded = false;

        /// <summary>
        /// The parent transform for the created GameObject
        /// </summary>
        public Transform SceneParent { get; set; }

        /// <summary>
        /// The last created object
        /// </summary>
        public GameObject CreatedObject { get; private set; }

        /// <summary>
        /// Adds colliders to primitive objects when created
        /// </summary>
        public ColliderType Collider { get; set; }

        /// <summary>
        /// Override for the shader to use on created materials
        /// </summary>
        public string CustomShaderName { get; set; }

        /// <summary>
        /// Material to be applied while textures are being loaded
        /// </summary>
        public Material LoadingTextureMaterial { get; set; }

        /// <summary>
        /// Initial state of the meshes
        /// </summary>
        public bool initialVisibility { get; set; }

        private static bool renderingIsDisabled => !CommonScriptableObjects.rendererState.Get();
        private static float budgetPerFrameInMillisecondsValue = 2f;

        public static float budgetPerFrameInMilliseconds { get => renderingIsDisabled ? float.MaxValue : budgetPerFrameInMillisecondsValue; set => budgetPerFrameInMillisecondsValue = value; }

        public bool KeepCPUCopyOfMesh = true;

        private bool useMaterialTransitionValue = true;

        public bool importSkeleton = true;

        public bool useMaterialTransition { get => useMaterialTransitionValue && !renderingIsDisabled; set => useMaterialTransitionValue = value; }

        public int maxTextureSize = 1024;
        private const float SAME_KEYFRAME_TIME_DELTA = 0.0001f;

        protected struct GLBStream
        {
            public Stream Stream;
            public long StartPosition;
        }

        static protected float _timeAtLastYield = 0f;
        protected AsyncCoroutineHelper _asyncCoroutineHelper;

        public bool addImagesToPersistentCaching = true;
        public bool addMaterialsToPersistentCaching = true;

        protected GameObject _lastLoadedScene;
        protected readonly GLTFMaterial DefaultMaterial = new GLTFMaterial();
        protected MaterialCacheData _defaultLoadedMaterial = null;

        protected string _gltfFileName;
        protected GLBStream _gltfStream;
        protected GLTFRoot _gltfRoot;
        protected AssetCache _assetCache;
        protected ILoader _loader;
        protected bool _isRunning = false;

        public string id;

        struct NodeId_Like
        {
            public int Id;
            public Node Value;
        }

        List<NodeId_Like> nodesWithMeshes = new List<NodeId_Like>();

        //NOTE(Brian): Using primitives in Dictionaries would produce unneeded boxing. Improve later.
        Dictionary<int, int> nodeToParent = new Dictionary<int, int>();

        //NOTE(Brian): If a material is reused between many primitives, this prevents that the ref count
        //             goes above one, as this would prevent the material to unload properly.

        //             This is because the GLTF cache cleanup setup expects ref owners to be the entire GLTF object.
        HashSet<Material> usedMaterials = new HashSet<Material>();

        /// <summary>
        /// Creates a GLTFSceneBuilder object which will be able to construct a scene based off a url
        /// </summary>
        /// <param name="gltfFileName">glTF file relative to data loader path</param>
        /// <param name="externalDataLoader">Loader to load external data references</param>
        /// <param name="asyncCoroutineHelper">Helper to load coroutines on a seperate thread</param>
        public GLTFSceneImporter(string id, string gltfFileName, ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper) : this(externalDataLoader, asyncCoroutineHelper)
        {
            _gltfFileName = gltfFileName;
            this.id = string.IsNullOrEmpty(id) ? gltfFileName : id;
        }

        public GLTFSceneImporter(string id, GLTFRoot rootNode, ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper, Stream gltfStream = null) : this(externalDataLoader, asyncCoroutineHelper)
        {
            this.id = id;
            _gltfRoot = rootNode;
            _loader = externalDataLoader;
            if (gltfStream != null)
            {
                _gltfStream = new GLBStream { Stream = gltfStream, StartPosition = gltfStream.Position };
            }
        }

        private GLTFSceneImporter(ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper)
        {
            _loader = externalDataLoader;
            _asyncCoroutineHelper = asyncCoroutineHelper;
        }

        public void Dispose()
        {
            //NOTE(Brian): If the coroutine is interrupted and the local streaming list contains something,
            //             we must clean the static list or other GLTFSceneImporter instances might get stuck.
            int streamingImagesLocalListCount = streamingImagesLocalList.Count;

            for (int i = 0; i < streamingImagesLocalListCount; i++)
            {
                string s = streamingImagesLocalList[i];

                if (streamingImagesStaticList.Contains(s))
                {
                    streamingImagesStaticList.Remove(s);
                }
            }

            if (_assetCache != null)
            {
                _assetCache.Dispose();
                _assetCache = null;
            }
        }

        public GameObject lastLoadedScene { get { return _lastLoadedScene; } }

        public static System.Action<float> OnPerformanceFinish;

        /// <summary>
        /// Loads a glTF Scene into the LastLoadedScene field
        /// </summary>
        /// <param name="sceneIndex">The scene to load, If the index isn't specified, we use the default index in the file. Failing that we load index 0.</param>
        /// <param name="showSceneObj"></param>
        /// <param name="onLoadComplete">Callback function for when load is completed</param>
        /// <returns></returns>
        public IEnumerator LoadScene(int sceneIndex = -1, bool showSceneObj = true, Action<GameObject, ExceptionDispatchInfo> onLoadComplete = null)
        {
            try
            {
                lock (this)
                {
                    if (_isRunning)
                    {
                        throw new GLTFLoadException("Cannot call LoadScene while GLTFSceneImporter is already running");
                    }

                    _isRunning = true;
                }

                if (_gltfRoot == null)
                {
                    if (VERBOSE)
                    {
                        Debug.Log("LoadScene() GLTF File Name -> " + _gltfFileName);
                    }

                    yield return LoadJsonStream(_gltfFileName);
                }

                float profiling = 0, frames = 0, jsonProfiling = 0;

                _timeAtLastYield = Time.realtimeSinceStartup;

                if (PROFILING_ENABLED)
                {
                    jsonProfiling = Time.realtimeSinceStartup;
                }

                if (_gltfRoot == null)
                {
                    yield return LoadJson();
                }

                if (PROFILING_ENABLED)
                {
                    jsonProfiling = ((Time.realtimeSinceStartup - jsonProfiling) * 1000f);
                }

                if (_assetCache == null)
                {
                    _assetCache = new AssetCache(_gltfRoot);
                }


                if (PROFILING_ENABLED)
                {
                    if (VERBOSE)
                    {
                        Debug.Log($"{_gltfFileName} >>> Load will start");
                    }

                    profiling = Time.realtimeSinceStartup;
                    frames = Time.frameCount;
                }

                yield return _LoadScene(sceneIndex, showSceneObj);

                if (PROFILING_ENABLED)
                {
                    if (VERBOSE)
                    {
                        Debug.Log($"{_gltfFileName} >>> Load finished in {((Time.realtimeSinceStartup - profiling) * 1000f)} ms... frames = {(Time.frameCount - frames)} (json = {jsonProfiling})");
                    }

                    OnPerformanceFinish?.Invoke(Time.realtimeSinceStartup - profiling);
                }

                yield return null; // NOTE(Brian): DO NOT REMOVE, we must wait a frame in order to
                //              start enqueued coroutines, if this is removed
                //              the WaitUntil below will fail.

                yield return new WaitUntil(
                    () => { return _asyncCoroutineHelper == null || _asyncCoroutineHelper.AllCoroutinesAreFinished(); });

                MaterialTransitionController[] matTransitions = CreatedObject.GetComponentsInChildren<MaterialTransitionController>(true);

                if (matTransitions != null && matTransitions.Length > 0)
                {
                    float bailout = 0;
                    //NOTE(Brian): Wait for the MaterialTransition to finish before copying the object to the library
                    yield return new WaitUntil(
                        () =>
                        {
                            bailout += Time.deltaTime;

                            if (bailout > 5.0f)
                                return true;

                            bool finishedTransition = true;

                            for (int i = 0; i < matTransitions.Length; i++)
                            {
                                if (matTransitions[i] != null)
                                {
                                    finishedTransition = false;
                                    break;
                                }
                            }

                            return finishedTransition;
                        }
                    );
                }

                if (!importSkeleton)
                {
                    foreach (var skeleton in skeletonGameObjects)
                    {
                        if (Application.isPlaying)
                            Object.Destroy(skeleton);
                        else
                            Object.DestroyImmediate(skeleton);
                    }
                }

            }
            finally
            {
                lock (this)
                {
                    _isRunning = false;
                }
            }

            onLoadComplete?.Invoke(lastLoadedScene, null);
        }

        /// <summary>
        /// Loads a node tree from a glTF file into the LastLoadedScene field
        /// </summary>
        /// <param name="nodeIndex">The node index to load from the glTF</param>
        /// <returns></returns>
        public IEnumerator LoadNodeAsync(int nodeIndex)
        {
            try
            {
                lock (this)
                {
                    if (_isRunning)
                    {
                        throw new GLTFLoadException("Cannot call LoadNode while GLTFSceneImporter is already running");
                    }

                    _isRunning = true;
                }

                if (_gltfRoot == null)
                {
                    yield return LoadJsonStream(_gltfFileName);
                    yield return LoadJson();
                }

                if (_assetCache == null)
                {
                    _assetCache = new AssetCache(_gltfRoot);
                }

                _timeAtLastYield = Time.realtimeSinceStartup;
                yield return _LoadNode(nodeIndex);

                CreatedObject = _assetCache.NodeCache[nodeIndex];
                InitializeGltfTopLevelObject();
            }
            finally
            {
                lock (this)
                {
                    _isRunning = false;
                }
            }
        }

        /// <summary>
        /// Initializes the top-level created node by adding an instantiated GLTF object component to it,
        /// so that it can cleanup after itself properly when destroyed
        /// </summary>
        private void InitializeGltfTopLevelObject()
        {
            InstantiatedGLTFObject instantiatedGltfObject = CreatedObject.AddComponent<InstantiatedGLTFObject>();
            instantiatedGltfObject.CachedData = new RefCountedCacheData
            {
                MaterialCache = _assetCache.MaterialCache,
                TextureCache = _assetCache.TextureCache,
                MeshCache = _assetCache.MeshCache
            };
        }

        private IEnumerator ConstructBufferData(Node node)
        {
            MeshId mesh = node.Mesh;
            if (mesh != null)
            {
                if (mesh.Value.Primitives != null)
                {
                    yield return ConstructMeshAttributes(mesh.Value, mesh);
                }
            }

            if (node.Children != null)
            {
                foreach (NodeId child in node.Children)
                {
                    yield return ConstructBufferData(child.Value);
                }
            }

            const string msft_LODExtName = MSFT_LODExtensionFactory.EXTENSION_NAME;
            MSFT_LODExtension lodsextension = null;

            if (_gltfRoot.ExtensionsUsed != null
                && _gltfRoot.ExtensionsUsed.Contains(msft_LODExtName)
                && node.Extensions != null
                && node.Extensions.ContainsKey(msft_LODExtName))
            {
                lodsextension = node.Extensions[msft_LODExtName] as MSFT_LODExtension;
                if (lodsextension != null && lodsextension.MeshIds.Count > 0)
                {
                    for (int i = 0; i < lodsextension.MeshIds.Count; i++)
                    {
                        int lodNodeId = lodsextension.MeshIds[i];
                        yield return ConstructBufferData(_gltfRoot.Nodes[lodNodeId]);
                    }
                }
            }
        }

        private IEnumerator ConstructMeshAttributes(GLTFMesh mesh, MeshId meshId)
        {
            int meshIdIndex = meshId.Id;

            if (_assetCache.MeshCache[meshIdIndex] == null)
            {
                _assetCache.MeshCache[meshIdIndex] = new MeshCacheData[mesh.Primitives.Count];
            }

            for (int i = 0; i < mesh.Primitives.Count; ++i)
            {
                MeshPrimitive primitive = mesh.Primitives[i];
                if (_assetCache.MeshCache[meshIdIndex][i] == null)
                {
                    _assetCache.MeshCache[meshIdIndex][i] = new MeshCacheData();
                }

                if (_assetCache.MeshCache[meshIdIndex][i].MeshAttributes.Count == 0)
                {
                    yield return ConstructMeshAttributes(primitive, meshIdIndex, i);
                    if (primitive.Material != null && !pendingImageBuffers.Contains(primitive.Material.Value))
                    {
                        pendingImageBuffers.Add(primitive.Material.Value);
                    }
                }
            }
        }

        HashSet<GLTFMaterial> pendingImageBuffers = new HashSet<GLTFMaterial>();

        static List<string> streamingImagesStaticList = new List<string>();
        List<string> streamingImagesLocalList = new List<string>();

        protected IEnumerator ConstructImageBuffer(GLTFTexture texture, int textureIndex)
        {
            int sourceId = GetTextureSourceId(texture);
            GLTFImage image = _gltfRoot.Images[sourceId];
            string imageId = GenerateImageId(image.Uri, sourceId);

            if (image.Uri != null && this.addImagesToPersistentCaching)
            {
                while (streamingImagesStaticList.Contains(image.Uri))
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            if ((image.Uri == null || !PersistentAssetCache.HasImage(imageId)) && _assetCache.ImageStreamCache[sourceId] == null)
            {
                // we only load the streams if not a base64 uri, meaning the data is in the uri
                if (image.Uri != null && !URIHelper.IsBase64Uri(image.Uri))
                {
                    streamingImagesStaticList.Add(image.Uri);
                    streamingImagesLocalList.Add(image.Uri);

                    yield return _loader.LoadStream(image.Uri);
                    _assetCache.ImageStreamCache[sourceId] = _loader.LoadedStream;

                    streamingImagesStaticList.Remove(image.Uri);
                    streamingImagesLocalList.Remove(image.Uri);
                }
                else if (image.Uri == null && image.BufferView != null && _assetCache.BufferCache[image.BufferView.Value.Buffer.Id] == null)
                {
                    int bufferIndex = image.BufferView.Value.Buffer.Id;
                    yield return ConstructBuffer(_gltfRoot.Buffers[bufferIndex], bufferIndex);
                }
            }

            _assetCache.TextureCache[textureIndex] = new TextureCacheData
            {
                TextureDefinition = texture
            };
        }

        protected IEnumerator WaitUntilEnum(WaitUntil waitUntil) { yield return waitUntil; }

        protected IEnumerator EmptyYieldEnum() { yield break; }

        private IEnumerator LoadJsonStream(string jsonUrl)
        {
#if !WINDOWS_UWP
            if (isMultithreaded && _loader.HasSyncLoadMethod)
            {
                Thread loadThread = new Thread(() => _loader.LoadStreamSync(_gltfFileName));
                loadThread.Priority = ThreadPriority.Highest;
                loadThread.Start();
                RunCoroutineSync(WaitUntilEnum(new WaitUntil(() => !loadThread.IsAlive)));
            }
            else
#endif
            {
                // HACK: Force the coroutine to run synchronously in the editor
                yield return _loader.LoadStream(_gltfFileName);
            }

            _gltfStream.Stream = _loader.LoadedStream;
            _gltfStream.StartPosition = 0;
        }

        private IEnumerator LoadJson()
        {
#if !WINDOWS_UWP
            if (isMultithreaded)
            {
                Thread parseJsonThread = new Thread(() => GLTFParser.ParseJson(_gltfStream.Stream, out _gltfRoot, _gltfStream.StartPosition));
                parseJsonThread.Priority = ThreadPriority.Highest;
                parseJsonThread.Start();
                RunCoroutineSync(WaitUntilEnum(new WaitUntil(() => !parseJsonThread.IsAlive)));
                if (_gltfRoot == null)
                {
                    throw new GLTFLoadException("Failed to parse glTF");
                }
            }
            else
#endif
            {
                GLTFParser.ParseJson(_gltfStream.Stream, out _gltfRoot, _gltfStream.StartPosition);
            }

            yield break;
        }

        public static void RunCoroutineSync(IEnumerator streamEnum)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(streamEnum);
            while (stack.Count > 0)
            {
                var enumerator = stack.Pop();
                if (enumerator.MoveNext())
                {
                    stack.Push(enumerator);
                    var subEnumerator = enumerator.Current as IEnumerator;
                    if (subEnumerator != null)
                    {
                        stack.Push(subEnumerator);
                    }
                }
            }
        }

        private IEnumerator _LoadNode(int nodeIndex, Transform parent = null)
        {
            if (nodeIndex >= _gltfRoot.Nodes.Count)
            {
                throw new ArgumentException("nodeIndex is out of range");
            }

            Node nodeToLoad = _gltfRoot.Nodes[nodeIndex];

            yield return ConstructBufferData(nodeToLoad);
            yield return ConstructNode(nodeToLoad, nodeIndex, parent);
        }

        /// <summary>
        /// Creates a scene based off loaded JSON. Includes loading in binary and image data to construct the meshes required.
        /// </summary>
        /// <param name="sceneIndex">The bufferIndex of scene in gltf file to load</param>
        /// <returns></returns>
        protected IEnumerator _LoadScene(int sceneIndex = -1, bool showSceneObj = true)
        {
            GLTFScene scene;

            if (sceneIndex >= 0 && sceneIndex < _gltfRoot.Scenes.Count)
            {
                scene = _gltfRoot.Scenes[sceneIndex];
            }
            else
            {
                scene = _gltfRoot.GetDefaultScene();
            }

            if (scene == null)
            {
                throw new GLTFLoadException("No default scene in gltf file.");
            }

            yield return ConstructScene(scene, showSceneObj);

            if (SceneParent != null)
            {
                CreatedObject.transform.SetParent(SceneParent, false);
            }

            _lastLoadedScene = CreatedObject;
        }

        protected IEnumerator ConstructBuffer(GLTFBuffer buffer, int bufferIndex)
        {
            if (buffer.Uri == null)
            {
                _assetCache.BufferCache[bufferIndex] = ConstructBufferFromGLB(bufferIndex);
            }
            else
            {
                Stream bufferDataStream = null;
                var uri = buffer.Uri;

                byte[] bufferData;
                URIHelper.TryParseBase64(uri, out bufferData);

                if (bufferData != null)
                {
                    bufferDataStream = new MemoryStream(bufferData, 0, bufferData.Length, false, true);
                }
                else
                {
                    if (PersistentAssetCache.HasBuffer(buffer.Uri, id))
                    {
                        bufferDataStream = PersistentAssetCache.GetBuffer(buffer.Uri, id).stream;
                    }
                    else
                    {
                        yield return _loader.LoadStream(buffer.Uri);
                        bufferDataStream = _loader.LoadedStream;
                        PersistentAssetCache.AddBuffer(buffer.Uri, id, bufferDataStream);
                    }
                }

                _assetCache.BufferCache[bufferIndex] = new BufferCacheData
                {
                    Stream = bufferDataStream
                };
            }
        }

        protected IEnumerator ConstructImage(GLTFImage image, int imageCacheIndex, bool markGpuOnly = false, bool linear = true)
        {
            if (_assetCache.ImageCache[imageCacheIndex] == null)
            {
                Stream stream = null;
                if (image.Uri == null)
                {
                    //NOTE(Zak): This fixes current issues of concurrent texture loading,
                    //           but it's possible that it would happen again in the future.
                    //           If that happens, we'll have implement some locking behavior for concurrent
                    //           import calls.

                    //NOTE(Brian): We can't yield between the stream creation and the stream.Read because
                    //             another coroutine can modify the stream Position in the next frame.
                    var bufferView = image.BufferView.Value;
                    var data = new byte[bufferView.ByteLength];

                    BufferCacheData bufferContents = _assetCache.BufferCache[bufferView.Buffer.Id];
                    bufferContents.Stream.Position = bufferView.ByteOffset + bufferContents.ChunkOffset;
                    bufferContents.Stream.Read(data, 0, data.Length);

                    if (ShouldYieldOnTimeout())
                    {
                        yield return YieldOnTimeout();
                    }

                    yield return ConstructUnityTexture(data, markGpuOnly, linear, imageCacheIndex);
                }
                else
                {
                    string uri = image.Uri;

                    URIHelper.TryParseBase64(uri, out byte[] bufferData);

                    if (bufferData != null)
                    {
                        stream = new MemoryStream(bufferData, 0, bufferData.Length, false, true);
                    }
                    else
                    {
                        stream = _assetCache.ImageStreamCache[imageCacheIndex];
                    }

                    if (ShouldYieldOnTimeout())
                    {
                        yield return YieldOnTimeout();
                    }

                    yield return ConstructUnityTexture(stream, markGpuOnly, linear, image, imageCacheIndex);
                }
            }
        }

        protected virtual IEnumerator ConstructUnityTexture(byte[] buffer, bool markGpuOnly, bool linear, int imageCacheIndex)
        {
            Texture2D texture = new Texture2D(0, 0, TextureFormat.ARGB32, true, linear);

            //  NOTE: the second parameter of LoadImage() marks non-readable, but we can't mark it until after we call Apply()
            texture.LoadImage(buffer, markGpuOnly);
            texture = CheckAndReduceTextureSize(texture);

#if !UNITY_EDITOR
            //NOTE(Brian): This breaks importing in editor mode
            texture.Compress(false);
#endif
            _assetCache.ImageCache[imageCacheIndex] = texture;

            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }
        }

        protected virtual IEnumerator ConstructUnityTexture(Stream stream, bool markGpuOnly, bool linear, GLTFImage image, int imageCacheIndex)
        {
            if (stream == null)
                yield break;

            if (stream is MemoryStream)
            {
                using (MemoryStream memoryStream = stream as MemoryStream)
                {
                    yield return ConstructUnityTexture(memoryStream.ToArray(), false, linear, imageCacheIndex);
                }
            }

            if (stream is FileStream fileStream)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);
                    yield return ConstructUnityTexture(memoryStream.ToArray(), false, linear, imageCacheIndex);
                }
            }
        }

        // Note that if the texture is reduced in size, the source one is destroyed
        protected Texture2D CheckAndReduceTextureSize(Texture2D source)
        {
            if (source.width > maxTextureSize || source.height > maxTextureSize)
            {
                float factor = 1.0f;
                int width = source.width;
                int height = source.height;

                if (width >= height)
                {
                    factor = (float)maxTextureSize / width;
                }
                else
                {
                    factor = (float)maxTextureSize / height;
                }

                Texture2D dstTex = TextureHelpers.Resize(source, (int) (width * factor), (int) (height * factor));

                if (Application.isPlaying)
                    Texture2D.Destroy(source);
                else
                    Texture2D.DestroyImmediate(source);

                return dstTex;
            }

            return source;
        }

        protected virtual IEnumerator ConstructMeshAttributes(MeshPrimitive primitive, int meshID, int primitiveIndex)
        {
            if (_assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes.Count == 0)
            {
                Dictionary<string, AttributeAccessor> attributeAccessors = new Dictionary<string, AttributeAccessor>(primitive.Attributes.Count + 1);
                foreach (var attributePair in primitive.Attributes)
                {
                    BufferId bufferIdPair = attributePair.Value.Value.BufferView.Value.Buffer;
                    GLTFBuffer buffer = bufferIdPair.Value;
                    int bufferId = bufferIdPair.Id;

                    // on cache miss, load the buffer
                    if (_assetCache.BufferCache[bufferId] == null)
                    {
                        yield return ConstructBuffer(buffer, bufferId);
                    }

                    AttributeAccessor attributeAccessor = new AttributeAccessor
                    {
                        AccessorId = attributePair.Value,
                        Stream = _assetCache.BufferCache[bufferId].Stream,
                        Offset = (uint) _assetCache.BufferCache[bufferId].ChunkOffset
                    };

                    attributeAccessors[attributePair.Key] = attributeAccessor;
                }

                if (primitive.Indices != null)
                {
                    int bufferId = primitive.Indices.Value.BufferView.Value.Buffer.Id;

                    if (_assetCache.BufferCache[bufferId] == null)
                    {
                        yield return ConstructBuffer(primitive.Indices.Value.BufferView.Value.Buffer.Value, bufferId);
                    }

                    AttributeAccessor indexBuilder = new AttributeAccessor
                    {
                        AccessorId = primitive.Indices,
                        Stream = _assetCache.BufferCache[bufferId].Stream,
                        Offset = (uint) _assetCache.BufferCache[bufferId].ChunkOffset
                    };

                    attributeAccessors[SemanticProperties.INDICES] = indexBuilder;
                }

                GLTFHelpers.BuildMeshAttributes(ref attributeAccessors);

                TransformAttributes(ref attributeAccessors);
                _assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes = attributeAccessors;
            }
        }

        protected void TransformAttributes(ref Dictionary<string, AttributeAccessor> attributeAccessors)
        {
            // Flip vectors and triangles to the Unity coordinate system.
            if (attributeAccessors.ContainsKey(SemanticProperties.POSITION))
            {
                AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.POSITION];
                SchemaExtensions.ConvertVector3CoordinateSpaceFast(ref attributeAccessor);
            }

            if (attributeAccessors.ContainsKey(SemanticProperties.INDICES))
            {
                AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.INDICES];
                SchemaExtensions.FlipFaces(ref attributeAccessor);
            }

            if (attributeAccessors.ContainsKey(SemanticProperties.NORMAL))
            {
                AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.NORMAL];
                SchemaExtensions.ConvertVector3CoordinateSpaceFast(ref attributeAccessor);
            }

            // TexCoord goes from 0 to 3 to match GLTFHelpers.BuildMeshAttributes
            for (int i = 0; i < 4; i++)
            {
                if (attributeAccessors.ContainsKey(SemanticProperties.TexCoord[i]))
                {
                    AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.TexCoord[i]];
                    SchemaExtensions.FlipTexCoordArrayV(ref attributeAccessor);
                }
            }

            if (attributeAccessors.ContainsKey(SemanticProperties.TANGENT))
            {
                AttributeAccessor attributeAccessor = attributeAccessors[SemanticProperties.TANGENT];
                SchemaExtensions.ConvertVector4CoordinateSpaceFast(ref attributeAccessor);
            }
        }

        #region Animation

        static string RelativePathFrom(Transform self, Transform root)
        {
            var path = new List<String>();
            for (var current = self; current != null; current = current.parent)
            {
                if (current == root)
                {
                    return String.Join("/", path.ToArray());
                }

                path.Insert(0, current.name);
            }

            throw new Exception("no RelativePath");
        }

        protected virtual void BuildAnimationSamplers(GLTFAnimation animation, int animationId)
        {
            // look up expected data types
            var typeMap = new Dictionary<int, string>();
            foreach (var channel in animation.Channels)
            {
                typeMap[channel.Sampler.Id] = channel.Target.Path.ToString();
            }

            var samplers = _assetCache.AnimationCache[animationId].Samplers;
            var samplersByType = new Dictionary<string, List<AttributeAccessor>>
            {
                { "time", new List<AttributeAccessor>(animation.Samplers.Count) }
            };

            for (var i = 0; i < animation.Samplers.Count; i++)
            {
                // no sense generating unused samplers
                if (!typeMap.ContainsKey(i))
                {
                    continue;
                }

                var samplerDef = animation.Samplers[i];

                samplers[i].Interpolation = samplerDef.Interpolation;

                // set up input accessors
                int bufferId = samplerDef.Input.Value.BufferView.Value.Buffer.Id;
                BufferCacheData bufferCacheData = _assetCache.BufferCache[bufferId];
                AttributeAccessor attributeAccessor = new AttributeAccessor
                {
                    AccessorId = samplerDef.Input,
                    Stream = bufferCacheData.Stream,
                    Offset = bufferCacheData.ChunkOffset
                };

                samplers[i].Input = attributeAccessor;
                samplersByType["time"].Add(attributeAccessor);

                // set up output accessors
                int anotherBufferId = samplerDef.Output.Value.BufferView.Value.Buffer.Id;
                bufferCacheData = _assetCache.BufferCache[anotherBufferId];

                attributeAccessor = new AttributeAccessor
                {
                    AccessorId = samplerDef.Output,
                    Stream = bufferCacheData.Stream,
                    Offset = bufferCacheData.ChunkOffset
                };

                samplers[i].Output = attributeAccessor;

                if (!samplersByType.ContainsKey(typeMap[i]))
                {
                    samplersByType[typeMap[i]] = new List<AttributeAccessor>();
                }

                samplersByType[typeMap[i]].Add(attributeAccessor);
            }

            // populate attributeAccessors with buffer data
            GLTFHelpers.BuildAnimationSamplers(ref samplersByType);
        }

        void ProcessCurves(Transform root, GameObject[] nodes, AnimationClip clip, GLTFAnimation animation, AnimationCacheData animationCache)
        {
            foreach (AnimationChannel channel in animation.Channels)
            {
                AnimationSamplerCacheData samplerCache = animationCache.Samplers[channel.Sampler.Id];
                if (channel.Target.Node == null)
                {
                    // If a channel doesn't have a target node, then just skip it.
                    // This is legal and is present in one of the asset generator models, but means that animation doesn't actually do anything.
                    // https://github.com/KhronosGroup/glTF-Asset-Generator/tree/master/Output/Positive/Animation_NodeMisc
                    // Model 08
                    continue;
                }

                var node = nodes[channel.Target.Node.Id];
                string relativePath = RelativePathFrom(node.transform, root);

                NumericArray input = samplerCache.Input.AccessorContent,
                             output = samplerCache.Output.AccessorContent;

                string[] propertyNames;
                Vector3 coordinateSpaceConversionScale = new Vector3(-1, 1, 1);

                switch (channel.Target.Path)
                {
                    case GLTFAnimationChannelPath.translation:
                        propertyNames = new string[] { "localPosition.x", "localPosition.y", "localPosition.z" };

                        SetAnimationCurve(clip, relativePath, propertyNames, input, output,
                            samplerCache.Interpolation, typeof(Transform),
                            (data, frame) =>
                            {
                                var position = data.AsVec3s[frame].ToUnityVector3Convert();

                                return new float[] { position.x, position.y, position.z };
                            });
                        break;

                    case GLTFAnimationChannelPath.rotation:
                        propertyNames = new string[] { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };

                        SetAnimationCurve(clip, relativePath, propertyNames, input, output,
                            samplerCache.Interpolation, typeof(Transform),
                            (data, frame) =>
                            {
                                var rotation = data.AsVec4s[frame];

                                var quaternion = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w).ToUnityQuaternionConvert();

                                return new float[] { quaternion.x, quaternion.y, quaternion.z, quaternion.w };
                            },
                            // NOTE(Brian): Unity makes some conversion to eulers on AnimationClip.SetCurve
                            // that breaks the keyframe optimization.
                            optimizeKeyframes: false);

                        break;

                    case GLTFAnimationChannelPath.scale:
                        propertyNames = new string[] { "localScale.x", "localScale.y", "localScale.z" };

                        SetAnimationCurve(clip, relativePath, propertyNames, input, output,
                            samplerCache.Interpolation, typeof(Transform),
                            (data, frame) =>
                            {
                                var scale = data.AsVec3s[frame];
                                return new float[] { scale.x, scale.y, scale.z };
                            });
                        break;

                    case GLTFAnimationChannelPath.weights:
                        // TODO: add support for blend shapes/morph targets
                        break;

                    default:
                        Debug.LogWarning("Cannot read GLTF animation path");
                        break;
                } // switch target type
            } // foreach channel

            // This is needed to ensure certain rotations don't get messed up
            clip.EnsureQuaternionContinuity();
        }

        protected void SetAnimationCurve(
            AnimationClip clip,
            string relativePath,
            string[] propertyNames,
            NumericArray input,
            NumericArray output,
            InterpolationType mode,
            Type curveType,
            ValuesConvertion getConvertedValues,
            bool optimizeKeyframes = true)
        {
            var channelCount = propertyNames.Length;
            var frameCount = input.AsFloats.Length;

            // copy all the key frame data to cache
            Keyframe[][] keyframes = new Keyframe[channelCount][];
            for (var ci = 0; ci < channelCount; ++ci)
            {
                keyframes[ci] = new Keyframe[frameCount];
            }

            for (var i = 0; i < frameCount; ++i)
            {
                var time = input.AsFloats[i];

                float[] values = null;
                float[] inTangents = null;
                float[] outTangents = null;
                if (mode == InterpolationType.CUBICSPLINE)
                {
                    // For cubic spline, the output will contain 3 values per keyframe; inTangent, dataPoint, and outTangent.
                    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/README.md#appendix-c-spline-interpolation

                    var cubicIndex = i * 3;
                    inTangents = getConvertedValues(output, cubicIndex);
                    values = getConvertedValues(output, cubicIndex + 1);
                    outTangents = getConvertedValues(output, cubicIndex + 2);
                }
                else
                {
                    // For other interpolation types, the output will only contain one value per keyframe
                    values = getConvertedValues(output, i);
                }

                for (var ci = 0; ci < channelCount; ++ci)
                {
                    if (mode == InterpolationType.CUBICSPLINE)
                    {
                        keyframes[ci][i] = new Keyframe(time, values[ci], inTangents[ci], outTangents[ci]);
                    }
                    else
                    {
                        keyframes[ci][i] = new Keyframe(time, values[ci]);
                    }
                }
            }

            for (var ci = 0; ci < channelCount; ++ci)
            {
                // For cubic spline interpolation, the inTangents and outTangents are already explicitly defined.
                // For the rest, set them appropriately.
                if (mode != InterpolationType.CUBICSPLINE)
                {
                    for (var i = 0; i < keyframes[ci].Length; i++)
                        SetTangentMode(keyframes[ci], i, mode);
                }

                if (!optimizeKeyframes)
                {
                    // copy all key frames data to animation curve and add it to the clip
                    AnimationCurve curve = new AnimationCurve(keyframes[ci]);
                    clip.SetCurve(relativePath, curveType, propertyNames[ci], curve);
                }
            }

            if (optimizeKeyframes)
            {
                for (var ci = 0; ci < channelCount; ++ci)
                {
                    keyframes[ci] = OptimizeKeyFrames(keyframes[ci]);
                }

                // Setting the curves by the keyframe length order fixes weird index overrun issue:
                // https://forum.unity.com/threads/animationutility-seteditorcurve-cant-add-curve-with-one-keyfram.247372/
                var orderedKeyframes = keyframes.OrderBy(x => { return x.Length; }).ToArray();

                foreach (var keyframeCollection in orderedKeyframes)
                {
                    int index = Array.IndexOf(keyframes, keyframeCollection);

                    // copy all key frames data to animation curve and add it to the clip
                    AnimationCurve curve = new AnimationCurve(keyframeCollection);
                    clip.SetCurve(relativePath, curveType, propertyNames[index], curve);
                }
            }
        }

        private static float GetDiffAngle(float a1, float a2) { return Mathf.PI - Mathf.Abs(Mathf.Abs(a1 - a2) - Mathf.PI); }

        public static Keyframe[] OptimizeKeyFrames(Keyframe[] rawKeyframes)
        {
            if (rawKeyframes.Length <= 2)
                return rawKeyframes;

            List<Keyframe> result = new List<Keyframe>(2);

            result.Add(rawKeyframes[0]);

            const float TANGENT_THRESHOLD = 0.1f;

            for (int i = 1; i < rawKeyframes.Length - 1; i++)
            {
                Keyframe nextKey = rawKeyframes[i + 1];
                Keyframe prevKey = rawKeyframes[i - 1];
                Keyframe curKey = rawKeyframes[i];

                float angCurToNext = Mathf.Atan2(nextKey.value - curKey.value, nextKey.time - curKey.time);
                float angCurToPrev = Mathf.Atan2(curKey.value - prevKey.value, curKey.time - prevKey.time);

                float curOutAngle = Mathf.Atan(curKey.outTangent);
                float curInAngle = Mathf.Atan(curKey.inTangent);

                //NOTE(Brian): Collinearity tests. Small value = more collinear.

                //NOTE(Brian): curr keyframe out tangent point against path towards the next keyframe.
                float curOutDiff = GetDiffAngle(curOutAngle, angCurToNext);

                //NOTE(Brian): curr keyframe in tangent point against path towards the prev keyframe.
                float curInDiff = GetDiffAngle(curInAngle, angCurToPrev);

                //NOTE(Brian): next keyframe in tangent point against path towards the curr keyframe.
                float nextInDiff = GetDiffAngle(Mathf.Atan(nextKey.inTangent), angCurToNext);

                //NOTE(Brian): prev keyframe out tangent point against path towards the curr keyframe.
                float prevOutDiff = GetDiffAngle(Mathf.Atan(prevKey.outTangent), angCurToPrev);

                //NOTE(Brian): test if both tangents for the current keyframe are collinear
                //             (i.e. don't cull broken curves).
                float sameDiff = GetDiffAngle(curInAngle, curOutAngle);

                float tangentDeviation = Mathf.Abs(curOutDiff + curInDiff + nextInDiff + prevOutDiff + sameDiff);

                if (tangentDeviation > TANGENT_THRESHOLD)
                    result.Add(curKey);
            }

            result.Add(rawKeyframes[rawKeyframes.Length - 1]);

            return result.ToArray();
        }

        private static void SetTangentMode(Keyframe[] keyframes, int keyframeIndex, InterpolationType interpolation)
        {
            var key = keyframes[keyframeIndex];

            switch (interpolation)
            {
                case InterpolationType.CATMULLROMSPLINE:
                    key.inTangent = 0;
                    key.outTangent = 0;
                    break;
                case InterpolationType.LINEAR:
                    key.inTangent = GetCurveKeyframeLeftLinearSlope(keyframes, keyframeIndex);
                    key.outTangent = GetCurveKeyframeLeftLinearSlope(keyframes, keyframeIndex + 1);
                    break;
                case InterpolationType.STEP:
                    key.inTangent = float.PositiveInfinity;
                    key.outTangent = float.PositiveInfinity;
                    break;
            }
        }

        private static float GetCurveKeyframeLeftLinearSlope(Keyframe[] keyframes, int keyframeIndex)
        {
            if (keyframeIndex <= 0 || keyframeIndex >= keyframes.Length)
                return 0;

            var valueDelta = keyframes[keyframeIndex].value - keyframes[keyframeIndex - 1].value;
            var timeDelta = keyframes[keyframeIndex].time - keyframes[keyframeIndex - 1].time;

            // As Unity doesn't allow us to put two keyframes in with the same time, we set the time delta to a minimum.
            if (timeDelta <= 0)
                timeDelta = SAME_KEYFRAME_TIME_DELTA;

            return valueDelta / timeDelta;
        }

        protected AnimationClip ConstructClip(Transform root, GameObject[] nodes, int animationId, out GLTFAnimation animation, out AnimationCacheData animationCache)
        {
            animation = _gltfRoot.Animations[animationId];

            animationCache = _assetCache.AnimationCache[animationId];
            if (animationCache == null)
            {
                animationCache = new AnimationCacheData(animation.Samplers.Count);
                _assetCache.AnimationCache[animationId] = animationCache;
            }
            else if (animationCache.LoadedAnimationClip != null)
            {
                return animationCache.LoadedAnimationClip;
            }

            // unpack accessors
            BuildAnimationSamplers(animation, animationId);

            // init clip
            AnimationClip clip = new AnimationClip
            {
                name = animation.Name ?? string.Format("animation:{0}", animationId)
            };
            _assetCache.AnimationCache[animationId].LoadedAnimationClip = clip;

            // needed because Animator component is unavailable at runtime
            clip.legacy = true;

            return clip;
        }

        #endregion

        protected virtual IEnumerator ConstructScene(GLTFScene scene, bool showSceneObj)
        {
            var sceneObj = new GameObject(string.IsNullOrEmpty(scene.Name) ? ("GLTFScene") : scene.Name);

            CreatedObject = sceneObj;

            sceneObj.SetActive(showSceneObj);
            sceneObj.transform.SetParent(SceneParent, false);
            sceneObj.transform.localPosition = Vector3.zero;
            sceneObj.transform.localRotation = Quaternion.identity;
            sceneObj.transform.localScale = Vector3.one;

            Transform[] nodeTransforms = new Transform[scene.Nodes.Count];

            for (int i = 0; i < scene.Nodes.Count; ++i)
            {
                NodeId node = scene.Nodes[i];

                yield return _LoadNode(node.Id, sceneObj.transform);

                GameObject nodeObj = _assetCache.NodeCache[node.Id];
                nodeTransforms[i] = nodeObj.transform;
            }

            foreach (var gltfMaterial in pendingImageBuffers)
            {
                yield return ConstructMaterialImageBuffers(gltfMaterial);
            }

            for (int i = 0; i < nodesWithMeshes.Count; i++)
            {
                NodeId_Like nodeId = nodesWithMeshes[i];
                Node node = nodeId.Value;

                yield return ConstructMesh(mesh: node.Mesh.Value,
                    parent: _assetCache.NodeCache[nodeId.Id].transform,
                    meshId: node.Mesh.Id,
                    skin: node.Skin != null ? node.Skin.Value : null);
            }

            if (_gltfRoot.Animations != null && _gltfRoot.Animations.Count > 0)
            {
                // create the AnimationClip that will contain animation data
                // NOTE (Pravs): Khronos GLTFLoader sets the animationComponent as 'enabled = false' but we don't do that so that we can find the component when needed.
                Animation animation = sceneObj.AddComponent<Animation>();
                animation.playAutomatically = true;
                animation.cullingType = AnimationCullingType.AlwaysAnimate;

                for (int i = 0; i < _gltfRoot.Animations.Count; ++i)
                {
                    GLTFAnimation gltfAnimation = null;
                    AnimationCacheData animationCache = null;

                    yield return LoadAnimationBufferData(_gltfRoot.Animations[i], i);

                    AnimationClip clip = ConstructClip(sceneObj.transform, _assetCache.NodeCache, i, out gltfAnimation, out animationCache);

                    ProcessCurves(sceneObj.transform, _assetCache.NodeCache, clip, gltfAnimation, animationCache);

                    clip.wrapMode = WrapMode.Loop;

                    animation.AddClip(clip, clip.name);

                    if (i == 0)
                    {
                        animation.clip = clip;
                    }
                }
            }

            InitializeGltfTopLevelObject();
        }

        IEnumerator LoadAnimationBufferData(GLTFAnimation animation, int animationId)
        {
            var typeMap = new Dictionary<int, string>();
            foreach (var channel in animation.Channels)
            {
                typeMap[channel.Sampler.Id] = channel.Target.Path.ToString();
            }

            for (var i = 0; i < animation.Samplers.Count; i++)
            {
                // no sense generating unused samplers
                if (!typeMap.ContainsKey(i))
                {
                    continue;
                }

                var samplerDef = animation.Samplers[i];

                // set up input accessors
                int bufferId = samplerDef.Input.Value.BufferView.Value.Buffer.Id;
                BufferCacheData bufferCacheData = _assetCache.BufferCache[bufferId];

                if (bufferCacheData == null)
                {
                    yield return ConstructBuffer(_gltfRoot.Buffers[bufferId], bufferId);
                    bufferCacheData = _assetCache.BufferCache[bufferId];

                    if (VERBOSE)
                    {
                        Debug.Log("A GLTF Animation buffer cache data is null, skipping animation sampler for " + animation.Name);
                    }
                }

                // set up output accessors
                int anotherBufferId = samplerDef.Output.Value.BufferView.Value.Buffer.Id;
                bufferCacheData = _assetCache.BufferCache[anotherBufferId];

                if (bufferCacheData == null)
                {
                    yield return ConstructBuffer(_gltfRoot.Buffers[anotherBufferId], anotherBufferId);
                    bufferCacheData = _assetCache.BufferCache[anotherBufferId];
                    if (VERBOSE)
                    {
                        Debug.Log("A GLTF Animation buffer cache data is null, skipping animation sampler for " + animation.Name);
                    }
                }
            }
        }

        protected virtual IEnumerator ConstructNode(Node node, int nodeIndex, Transform parent = null)
        {
            if (_assetCache.NodeCache[nodeIndex] != null)
            {
                yield break;
            }

            var nodeObj = new GameObject(string.IsNullOrEmpty(node.Name) ? ("GLTFNode" + nodeIndex) : node.Name);

            Vector3 position, scale;
            Quaternion rotation;

            node.GetUnityTRSProperties(out position, out rotation, out scale);

            nodeObj.transform.localPosition = position;
            nodeObj.transform.localRotation = rotation;
            nodeObj.transform.localScale = scale;

            _assetCache.NodeCache[nodeIndex] = nodeObj;

            if (node.Children != null)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    NodeId child = node.Children[i];

                    if (!nodeToParent.ContainsKey(child.Id))
                    {
                        nodeToParent.Add(child.Id, nodeIndex);
                    }

                    // todo blgross: replace with an iterartive solution
                    yield return ConstructNode(child.Value, child.Id, nodeObj.transform);
                }
            }

            Transform finalParent = parent ?? SceneParent;
            nodeObj.transform.SetParent(finalParent, false);

            const string msft_LODExtName = MSFT_LODExtensionFactory.EXTENSION_NAME;
            MSFT_LODExtension lodsextension = null;

            if (_gltfRoot.ExtensionsUsed != null
                && _gltfRoot.ExtensionsUsed.Contains(msft_LODExtName)
                && node.Extensions != null
                && node.Extensions.ContainsKey(msft_LODExtName))
            {
                lodsextension = node.Extensions[msft_LODExtName] as MSFT_LODExtension;
                if (lodsextension != null && lodsextension.MeshIds.Count > 0)
                {
                    LOD[] lods = new LOD[lodsextension.MeshIds.Count + 1];
                    List<double> lodCoverage = lodsextension.GetLODCoverage(node);

                    var lodGroupNodeObj = new GameObject(string.IsNullOrEmpty(node.Name) ? ("GLTFNode_LODGroup" + nodeIndex) : node.Name);

                    lodGroupNodeObj.SetActive(false);

                    nodeObj.transform.SetParent(lodGroupNodeObj.transform, false);

                    MeshRenderer[] childRenders = nodeObj.GetComponentsInChildren<MeshRenderer>();
                    lods[0] = new LOD(GetLodCoverage(lodCoverage, 0), childRenders);

                    LODGroup lodGroup = lodGroupNodeObj.AddComponent<LODGroup>();

                    for (int i = 0; i < lodsextension.MeshIds.Count; i++)
                    {
                        int lodNodeId = lodsextension.MeshIds[i];

                        yield return ConstructNode(_gltfRoot.Nodes[lodNodeId], lodNodeId, lodGroupNodeObj.transform);

                        int lodIndex = i + 1;
                        GameObject lodNodeObj = _assetCache.NodeCache[lodNodeId];
                        lodNodeObj.transform.SetParent(lodGroupNodeObj.transform, false);

                        childRenders = lodNodeObj.GetComponentsInChildren<MeshRenderer>();
                        lods[lodIndex] = new LOD(GetLodCoverage(lodCoverage, lodIndex), childRenders);
                    }

                    lodGroup.SetLODs(lods);
                    lodGroup.RecalculateBounds();
                    lodGroupNodeObj.SetActive(true);
                    _assetCache.NodeCache[nodeIndex] = lodGroupNodeObj;
                }
            }

            if (node.Mesh != null)
            {
                nodesWithMeshes.Add(new NodeId_Like { Id = nodeIndex, Value = node });
            }
        }

        float GetLodCoverage(List<double> lodcoverageExtras, int lodIndex)
        {
            if (lodcoverageExtras != null && lodIndex < lodcoverageExtras.Count)
            {
                return (float) lodcoverageExtras[lodIndex];
            }
            else
            {
                return 1.0f / (lodIndex + 2);
            }
        }

        private bool NeedsSkinnedMeshRenderer(MeshPrimitive primitive, Skin skin) { return HasBones(skin) || HasBlendShapes(primitive); }

        private bool HasBones(Skin skin) { return skin != null; }

        private bool HasBlendShapes(MeshPrimitive primitive) { return primitive.Targets != null; }

        IEnumerator FindSkeleton(int nodeId, System.Action<int> found)
        {
            if (nodeToParent.ContainsKey(nodeId))
            {
                yield return FindSkeleton(nodeToParent[nodeId], found);
            }

            found.Invoke(nodeId);
        }

        protected virtual IEnumerator SetupBones(Skin skin, MeshPrimitive primitive, SkinnedMeshRenderer renderer, GameObject primitiveObj, Mesh curMesh)
        {
            var boneCount = skin.Joints.Count;
            Transform[] bones = new Transform[boneCount];

            int bufferId = skin.InverseBindMatrices.Value.BufferView.Value.Buffer.Id;

            // on cache miss, load the buffer
            if (_assetCache.BufferCache[bufferId] == null)
            {
                yield return ConstructBuffer(_gltfRoot.Buffers[bufferId], bufferId);
            }

            AttributeAccessor attributeAccessor = new AttributeAccessor
            {
                AccessorId = skin.InverseBindMatrices,
                Stream = _assetCache.BufferCache[bufferId].Stream,
                Offset = _assetCache.BufferCache[bufferId].ChunkOffset
            };

            GLTFHelpers.BuildBindPoseSamplers(ref attributeAccessor);

            Matrix4x4[] gltfBindPoses = attributeAccessor.AccessorContent.AsMatrix4x4s;
            UnityEngine.Matrix4x4[] bindPoses = new UnityEngine.Matrix4x4[skin.Joints.Count];

            int skeletonId = 0;

            if (skin.Skeleton != null)
            {
                skeletonId = skin.Skeleton.Id;
            }
            else
            {
                yield return FindSkeleton(skin.Joints[0].Id, (id) => skeletonId = id);
            }

            for (int i = 0; i < boneCount; i++)
            {
                bones[i] = _assetCache.NodeCache[skin.Joints[i].Id].transform;
                bindPoses[i] = gltfBindPoses[i].ToMatrix4x4Convert();
            }

            curMesh.bindposes = bindPoses;
            renderer.bones = bones;
            renderer.rootBone = _assetCache.NodeCache[skeletonId].transform;

            if (!skeletonGameObjects.Contains(renderer.rootBone.gameObject))
                skeletonGameObjects.Add(renderer.rootBone.gameObject);
        }

        HashSet<GameObject> skeletonGameObjects = new HashSet<GameObject>();

        private BoneWeight[] CreateBoneWeightArray(Vector4[] joints, Vector4[] weights, int vertCount)
        {
            NormalizeBoneWeightArray(weights);

            BoneWeight[] boneWeights = new BoneWeight[vertCount];
            for (int i = 0; i < vertCount; i++)
            {
                boneWeights[i].boneIndex0 = (int) joints[i].x;
                boneWeights[i].boneIndex1 = (int) joints[i].y;
                boneWeights[i].boneIndex2 = (int) joints[i].z;
                boneWeights[i].boneIndex3 = (int) joints[i].w;

                boneWeights[i].weight0 = weights[i].x;
                boneWeights[i].weight1 = weights[i].y;
                boneWeights[i].weight2 = weights[i].z;
                boneWeights[i].weight3 = weights[i].w;
            }

            return boneWeights;
        }

        /// <summary>
        /// Ensures each bone weight influences applied to the vertices add up to 1
        /// </summary>
        /// <param name="weights">Bone weight array</param>
        private void NormalizeBoneWeightArray(Vector4[] weights)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                var weightSum = (weights[i].x + weights[i].y + weights[i].z + weights[i].w);

                if (weightSum > 0.001f || weightSum < -0.001f)
                {
                    weights[i] /= weightSum;
                }
            }
        }

        protected virtual IEnumerator ConstructMeshMaterials(GLTFMesh mesh, int meshId)
        {
            for (int i = 0; i < mesh.Primitives.Count; ++i)
            {
                yield return ConstructPrimitiveMaterials(mesh, meshId, i);
            }
        }

        protected virtual IEnumerator ConstructPrimitiveMaterials(GLTFMesh mesh, int meshId, int primitiveIndex)
        {
            var primitive = mesh.Primitives[primitiveIndex];
            int materialIndex = primitive.Material != null ? primitive.Material.Id : -1;

            GameObject primitiveObj = _assetCache.MeshCache[meshId][primitiveIndex].PrimitiveGO;

            Renderer renderer = primitiveObj.GetComponent<Renderer>();

            //// NOTE(Brian): Texture loading
            if (useMaterialTransition && initialVisibility)
            {
                var matController = primitiveObj.AddComponent<MaterialTransitionController>();
                var coroutine = DownloadAndConstructMaterial(primitive, materialIndex, renderer, matController);

                if (_asyncCoroutineHelper != null)
                {
                    _asyncCoroutineHelper.RunAsTask(coroutine, "matDownload");
                }
                else
                {
                    yield return coroutine;
                }
            }
            else
            {
                if (LoadingTextureMaterial != null)
                {
                    renderer.sharedMaterial = LoadingTextureMaterial;
                }

                yield return DownloadAndConstructMaterial(primitive, materialIndex, renderer, null);

                if (LoadingTextureMaterial == null)
                {
                    primitiveObj.SetActive(true);
                }
            }
        }

        protected virtual IEnumerator ConstructMesh(GLTFMesh mesh, Transform parent, int meshId, Skin skin)
        {
            bool isColliderMesh = parent.name.Contains("_collider");

            if (_assetCache.MeshCache[meshId] == null)
            {
                _assetCache.MeshCache[meshId] = new MeshCacheData[mesh.Primitives.Count];
            }

            for (int i = 0; i < mesh.Primitives.Count; ++i)
            {
                var primitive = mesh.Primitives[i];
                int materialIndex = primitive.Material != null ? primitive.Material.Id : -1;

                // NOTE(Brian): Submesh loading
                yield return ConstructMeshPrimitive(primitive, meshId, i, materialIndex);

                var primitiveObj = new GameObject("Primitive");
                primitiveObj.transform.SetParent(parent, false);
                primitiveObj.SetActive(useMaterialTransition || LoadingTextureMaterial != null);

                _assetCache.MeshCache[meshId][i].PrimitiveGO = primitiveObj;

                SkinnedMeshRenderer skinnedMeshRenderer = null;
                MeshRenderer meshRenderer = null;
                Renderer renderer = null;

                Mesh curMesh = _assetCache.MeshCache[meshId][i].LoadedMesh;
                MeshFilter meshFilter = primitiveObj.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = curMesh;

                if (!isColliderMesh)
                {
                    if (NeedsSkinnedMeshRenderer(primitive, skin))
                    {
                        skinnedMeshRenderer = primitiveObj.AddComponent<SkinnedMeshRenderer>();
                        skinnedMeshRenderer.sharedMesh = curMesh;
                        skinnedMeshRenderer.quality = SkinQuality.Auto;
                        renderer = skinnedMeshRenderer;
                        renderer.enabled = initialVisibility;

                        if (HasBones(skin))
                        {
                            yield return SetupBones(skin, primitive, skinnedMeshRenderer, primitiveObj, curMesh);
                        }
                    }
                    else
                    {
                        meshRenderer = primitiveObj.AddComponent<MeshRenderer>();
                        renderer = meshRenderer;
                        renderer.enabled = initialVisibility;
                    }

                    yield return ConstructPrimitiveMaterials(mesh, meshId, i);
                }
                else
                {
                    primitiveObj.SetActive(true);
                }

                switch (Collider)
                {
                    case ColliderType.Box:
                        var boxCollider = primitiveObj.AddComponent<BoxCollider>();
                        boxCollider.center = curMesh.bounds.center;
                        boxCollider.size = curMesh.bounds.size;
                        break;
                    case ColliderType.Mesh:
                        var meshCollider = primitiveObj.AddComponent<MeshCollider>();
                        meshCollider.sharedMesh = curMesh;
                        break;
                    case ColliderType.MeshConvex:
                        var meshConvexCollider = primitiveObj.AddComponent<MeshCollider>();
                        meshConvexCollider.sharedMesh = curMesh;
                        meshConvexCollider.convex = true;
                        break;
                }
            }
        }

        IEnumerator DownloadAndConstructMaterial(MeshPrimitive primitive, int materialIndex, Renderer renderer, MaterialTransitionController matController)
        {
            bool shouldUseDefaultMaterial = primitive.Material == null;

            GLTFMaterial materialToLoad = shouldUseDefaultMaterial ? DefaultMaterial : primitive.Material.Value;

            if ((shouldUseDefaultMaterial && _defaultLoadedMaterial == null) ||
                (!shouldUseDefaultMaterial && _assetCache.MaterialCache[materialIndex] == null))
            {
                yield return ConstructMaterial(materialToLoad, shouldUseDefaultMaterial ? -1 : materialIndex);
            }

            MaterialCacheData materialCacheData =
                materialIndex >= 0 ? _assetCache.MaterialCache[materialIndex] : _defaultLoadedMaterial;

            bool useVertexColors = primitive.Attributes.ContainsKey(SemanticProperties.COLOR_0);

            Material material = materialCacheData.GetContents(useVertexColors, false);

            bool alreadyUsedMaterial = usedMaterials.Contains(material);

            if (!alreadyUsedMaterial)
            {
                if (useVertexColors)
                    materialCacheData.CachedMaterialWithVertexColor.IncreaseRefCount();
                else
                    materialCacheData.CachedMaterial.IncreaseRefCount();

                usedMaterials.Add(material);
            }

            DCL.Helpers.SRPBatchingHelper.OptimizeMaterial(material);

            if (matController != null)
            {
                matController.OnDidFinishLoading(material);
            }
            else
            {
                renderer.sharedMaterial = material;
            }
        }

        protected virtual IEnumerator ConstructMeshPrimitive(MeshPrimitive primitive, int meshID, int primitiveIndex, int materialIndex)
        {
            if (_assetCache.MeshCache[meshID][primitiveIndex] == null)
            {
                _assetCache.MeshCache[meshID][primitiveIndex] = new MeshCacheData();
            }

            if (_assetCache.MeshCache[meshID][primitiveIndex].LoadedMesh == null)
            {
                var meshAttributes = _assetCache.MeshCache[meshID][primitiveIndex].MeshAttributes;
                var meshConstructionData = new MeshConstructionData
                {
                    Primitive = primitive,
                    MeshAttributes = meshAttributes
                };

                UnityMeshData unityMeshData = null;

                unityMeshData = ConvertAccessorsToUnityTypes(meshConstructionData);

                //yield return null;
                yield return ConstructUnityMesh(meshConstructionData, meshID, primitiveIndex, unityMeshData);
            }
        }

        static protected bool ShouldYieldOnTimeout() { return ((Time.realtimeSinceStartup - _timeAtLastYield) > (budgetPerFrameInMilliseconds / 1000f / (float) GLTFComponent.downloadingCount)); }

        static protected IEnumerator YieldOnTimeout()
        {
            yield return null;
            _timeAtLastYield = Time.realtimeSinceStartup;
        }

        protected UnityMeshData ConvertAccessorsToUnityTypes(MeshConstructionData meshConstructionData)
        {
            // todo optimize: There are multiple copies being performed to turn the buffer data into mesh data. Look into reducing them
            MeshPrimitive primitive = meshConstructionData.Primitive;
            Dictionary<string, AttributeAccessor> meshAttributes = meshConstructionData.MeshAttributes;

            int vertexCount = (int) primitive.Attributes[SemanticProperties.POSITION].Value.Count;

            return new UnityMeshData
            {
                Vertices = primitive.Attributes.ContainsKey(SemanticProperties.POSITION)
                    ? meshAttributes[SemanticProperties.POSITION].AccessorContent.AsVertices
                    : null,

                Normals = primitive.Attributes.ContainsKey(SemanticProperties.NORMAL)
                    ? meshAttributes[SemanticProperties.NORMAL].AccessorContent.AsNormals
                    : null,

                Uv1 = primitive.Attributes.ContainsKey(SemanticProperties.TEXCOORD_0)
                    ? meshAttributes[SemanticProperties.TEXCOORD_0].AccessorContent.AsTexcoords
                    : null,

                Uv2 = primitive.Attributes.ContainsKey(SemanticProperties.TEXCOORD_1)
                    ? meshAttributes[SemanticProperties.TEXCOORD_1].AccessorContent.AsTexcoords
                    : null,

                Uv3 = primitive.Attributes.ContainsKey(SemanticProperties.TEXCOORD_2)
                    ? meshAttributes[SemanticProperties.TEXCOORD_2].AccessorContent.AsTexcoords
                    : null,

                Uv4 = primitive.Attributes.ContainsKey(SemanticProperties.TEXCOORD_3)
                    ? meshAttributes[SemanticProperties.TEXCOORD_3].AccessorContent.AsTexcoords
                    : null,

                Colors = primitive.Attributes.ContainsKey(SemanticProperties.COLOR_0)
                    ? meshAttributes[SemanticProperties.COLOR_0].AccessorContent.AsColors
                    : null,

                Triangles = primitive.Indices != null
                    ? meshAttributes[SemanticProperties.INDICES].AccessorContent.AsInts
                    : MeshPrimitive.GenerateIndices(vertexCount),

                Tangents = primitive.Attributes.ContainsKey(SemanticProperties.TANGENT)
                    ? meshAttributes[SemanticProperties.TANGENT].AccessorContent.AsTangents
                    : null,

                BoneWeights = meshAttributes.ContainsKey(SemanticProperties.WEIGHTS_0) && meshAttributes.ContainsKey(SemanticProperties.JOINTS_0)
                    ? CreateBoneWeightArray(meshAttributes[SemanticProperties.JOINTS_0].AccessorContent.AsVec4s,
                        meshAttributes[SemanticProperties.WEIGHTS_0].AccessorContent.AsVec4s, vertexCount)
                    : null
            };
        }

        protected virtual IEnumerator ConstructMaterialImageBuffers(GLTFMaterial def)
        {
            var tasks = new List<AsyncCoroutineHelper.CoroutineInfo>(8);

            if (def.PbrMetallicRoughness != null)
            {
                var pbr = def.PbrMetallicRoughness;

                if (pbr.BaseColorTexture != null)
                {
                    var textureId = pbr.BaseColorTexture.Index;
                    var coroutine = ConstructImageBuffer(textureId.Value, textureId.Id);

                    if (_asyncCoroutineHelper != null)
                    {
                        tasks.Add(_asyncCoroutineHelper.RunAsTask(coroutine, ""));
                    }
                    else
                    {
                        yield return coroutine;
                    }
                }

                if (pbr.MetallicRoughnessTexture != null)
                {
                    var textureId = pbr.MetallicRoughnessTexture.Index;
                    var coroutine = ConstructImageBuffer(textureId.Value, textureId.Id);

                    if (_asyncCoroutineHelper != null)
                    {
                        tasks.Add(_asyncCoroutineHelper.RunAsTask(coroutine, ""));
                    }
                    else
                    {
                        yield return coroutine;
                    }
                }
            }

            if (def.CommonConstant != null)
            {
                if (def.CommonConstant.LightmapTexture != null)
                {
                    var textureId = def.CommonConstant.LightmapTexture.Index;
                    var coroutine = ConstructImageBuffer(textureId.Value, textureId.Id);

                    if (_asyncCoroutineHelper != null)
                    {
                        tasks.Add(_asyncCoroutineHelper.RunAsTask(coroutine, ""));
                    }
                    else
                    {
                        yield return coroutine;
                    }
                }
            }

            if (def.NormalTexture != null)
            {
                var textureId = def.NormalTexture.Index;
                var coroutine = ConstructImageBuffer(textureId.Value, textureId.Id);

                if (_asyncCoroutineHelper != null)
                {
                    tasks.Add(_asyncCoroutineHelper.RunAsTask(coroutine, ""));
                }
                else
                {
                    yield return coroutine;
                }
            }

            if (def.OcclusionTexture != null)
            {
                var textureId = def.OcclusionTexture.Index;

                if (!(def.PbrMetallicRoughness != null
                      && def.PbrMetallicRoughness.MetallicRoughnessTexture != null
                      && def.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id == textureId.Id))
                {
                    var coroutine = ConstructImageBuffer(textureId.Value, textureId.Id);

                    if (_asyncCoroutineHelper != null)
                    {
                        tasks.Add(_asyncCoroutineHelper.RunAsTask(coroutine, ""));
                    }
                    else
                    {
                        yield return coroutine;
                    }
                }
            }

            if (def.EmissiveTexture != null)
            {
                var textureId = def.EmissiveTexture.Index;
                var coroutine = ConstructImageBuffer(textureId.Value, textureId.Id);

                if (_asyncCoroutineHelper != null)
                {
                    tasks.Add(_asyncCoroutineHelper.RunAsTask(coroutine, "A"));
                }
                else
                {
                    yield return coroutine;
                }
            }

            // pbr_spec_gloss extension
            const string specGlossExtName = KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME;

            if (def.Extensions != null && def.Extensions.ContainsKey(specGlossExtName))
            {
                var specGlossDef = (KHR_materials_pbrSpecularGlossinessExtension) def.Extensions[specGlossExtName];

                if (specGlossDef.DiffuseTexture != null)
                {
                    var textureId = specGlossDef.DiffuseTexture.Index;
                    var coroutine = ConstructImageBuffer(textureId.Value, textureId.Id);

                    if (_asyncCoroutineHelper != null)
                    {
                        tasks.Add(_asyncCoroutineHelper.RunAsTask(coroutine, "2"));
                    }
                    else
                    {
                        yield return coroutine;
                    }
                }

                if (specGlossDef.SpecularGlossinessTexture != null)
                {
                    var textureId = specGlossDef.SpecularGlossinessTexture.Index;
                    var coroutine = ConstructImageBuffer(textureId.Value, textureId.Id);

                    if (_asyncCoroutineHelper != null)
                    {
                        tasks.Add(_asyncCoroutineHelper.RunAsTask(coroutine, "3"));
                    }
                    else
                    {
                        yield return coroutine;
                    }
                }
            }

            if (_asyncCoroutineHelper == null)
                yield break;

            yield return null;

            yield return new WaitUntil(
                () =>
                {
                    for (int i = 0; i < tasks.Count; i++)
                    {
                        if (!tasks[i].finished)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            );
        }

        protected IEnumerator ConstructUnityMesh(MeshConstructionData meshConstructionData, int meshId, int primitiveIndex, UnityMeshData unityMeshData)
        {
            MeshPrimitive primitive = meshConstructionData.Primitive;
            int vertexCount = (int) primitive.Attributes[SemanticProperties.POSITION].Value.Count;
            bool hasNormals = unityMeshData.Normals != null;

            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            Mesh mesh = new Mesh
            {
#if UNITY_2017_3_OR_NEWER
                indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
#endif
            };

            mesh.vertices = unityMeshData.Vertices;
            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            mesh.normals = unityMeshData.Normals;
            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            mesh.uv = unityMeshData.Uv1;
            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            mesh.uv2 = unityMeshData.Uv2;
            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            mesh.uv3 = unityMeshData.Uv3;
            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            mesh.uv4 = unityMeshData.Uv4;
            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            mesh.colors = unityMeshData.Colors;
            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            mesh.triangles = unityMeshData.Triangles;
            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            mesh.tangents = unityMeshData.Tangents;
            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            mesh.boneWeights = unityMeshData.BoneWeights;
            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

            if (!hasNormals)
            {
                mesh.RecalculateNormals();
            }

            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }

// Disable it in runtime so this optimization only takes place in
// asset bundle converter time.
#if UNITY_EDITOR
            mesh.Optimize();

            if (ShouldYieldOnTimeout())
            {
                yield return YieldOnTimeout();
            }
#endif
            if (!KeepCPUCopyOfMesh)
            {
                mesh.UploadMeshData(true);
            }

            _assetCache.MeshCache[meshId][primitiveIndex].LoadedMesh = mesh;
        }

        static Material cachedSpecGlossMat;
        static Material cachedMetalRoughMat;

        protected virtual IEnumerator ConstructMaterial(GLTFMaterial def, int materialIndex)
        {
            IUniformMap mapper;
            const string specGlossExtName = KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME;
            if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(specGlossExtName)
                                                 && def.Extensions != null && def.Extensions.ContainsKey(specGlossExtName))
            {
                if (!string.IsNullOrEmpty(CustomShaderName))
                {
                    mapper = new SpecGlossMap(CustomShaderName, maximumLod);
                }
                else
                {
                    if (cachedSpecGlossMat != null)
                    {
                        mapper = new SpecGlossMap(new Material(cachedSpecGlossMat), maximumLod);
                    }
                    else
                    {
                        mapper = new SpecGlossMap(maximumLod);
                        cachedSpecGlossMat = mapper.GetMaterialCopy();
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(CustomShaderName))
                {
                    mapper = new MetalRoughMap(CustomShaderName, maximumLod);
                }
                else
                {
                    if (cachedMetalRoughMat != null)
                    {
                        mapper = new MetalRoughMap(new Material(cachedMetalRoughMat), maximumLod);
                    }
                    else
                    {
                        mapper = new MetalRoughMap(maximumLod);
                        cachedMetalRoughMat = mapper.GetMaterialCopy();
                    }
                }
            }


            mapper.Material.name = def.Name;
            mapper.AlphaMode = def.AlphaMode;
            mapper.DoubleSided = def.DoubleSided;

            var mrMapper = mapper as IMetalRoughUniformMap;
            if (def.PbrMetallicRoughness != null && mrMapper != null)
            {
                var pbr = def.PbrMetallicRoughness;

                mrMapper.BaseColorFactor = pbr.BaseColorFactor;

                if (pbr.BaseColorTexture != null)
                {
                    TextureId textureId = pbr.BaseColorTexture.Index;
                    yield return ConstructTexture(textureId.Value, textureId.Id, false, false);
                    mrMapper.BaseColorTexture = _assetCache.TextureCache[textureId.Id].CachedTexture.Texture;
                    _assetCache.TextureCache[textureId.Id].CachedTexture.IncreaseRefCount();

                    mrMapper.BaseColorTexCoord = pbr.BaseColorTexture.TexCoord;
                    ExtTextureTransformExtension ext = GetTextureTransform(pbr.BaseColorTexture);
                    if (ext != null)
                    {
                        mrMapper.BaseColorXOffset = ext.Offset;
                        mrMapper.BaseColorXScale = ext.Scale;
                        //TODO: Implement UVs multichannel for ext.TexCoord;
                    }
                }

                mrMapper.MetallicFactor = pbr.MetallicFactor;

                if (pbr.MetallicRoughnessTexture != null)
                {
                    TextureId textureId = pbr.MetallicRoughnessTexture.Index;
                    yield return ConstructTexture(textureId.Value, textureId.Id);
                    mrMapper.MetallicRoughnessTexture = _assetCache.TextureCache[textureId.Id].CachedTexture.Texture;
                    _assetCache.TextureCache[textureId.Id].CachedTexture.IncreaseRefCount();
                    mrMapper.MetallicRoughnessTexCoord = pbr.MetallicRoughnessTexture.TexCoord;
                    mrMapper.RoughnessFactor = 0;
                }
                else
                {
                    mrMapper.RoughnessFactor = pbr.RoughnessFactor;
                }
            }

            var sgMapper = mapper as ISpecGlossUniformMap;
            if (sgMapper != null)
            {
                var specGloss = def.Extensions[specGlossExtName] as KHR_materials_pbrSpecularGlossinessExtension;

                sgMapper.DiffuseFactor = specGloss.DiffuseFactor;

                if (specGloss.DiffuseTexture != null)
                {
                    TextureId textureId = specGloss.DiffuseTexture.Index;
                    yield return ConstructTexture(textureId.Value, textureId.Id);
                    sgMapper.DiffuseTexture = _assetCache.TextureCache[textureId.Id].CachedTexture.Texture;
                    _assetCache.TextureCache[textureId.Id].CachedTexture.IncreaseRefCount();
                    sgMapper.DiffuseTexCoord = specGloss.DiffuseTexture.TexCoord;
                    ExtTextureTransformExtension ext = GetTextureTransform(specGloss.DiffuseTexture);
                    if (ext != null)
                    {
                        sgMapper.DiffuseXOffset = ext.Offset;
                        sgMapper.DiffuseXScale = ext.Scale;
                        //TODO: Implement UVs multichannel for ext.TexCoord;
                    }
                }

                sgMapper.SpecularFactor = specGloss.SpecularFactor;
                sgMapper.GlossinessFactor = specGloss.GlossinessFactor;

                if (specGloss.SpecularGlossinessTexture != null)
                {
                    TextureId textureId = specGloss.SpecularGlossinessTexture.Index;
                    yield return ConstructTexture(textureId.Value, textureId.Id);
                    sgMapper.SpecularGlossinessTexture = _assetCache.TextureCache[textureId.Id].CachedTexture.Texture;
                    _assetCache.TextureCache[textureId.Id].CachedTexture.IncreaseRefCount();
                }
            }

            if (def.NormalTexture != null)
            {
                TextureId textureId = def.NormalTexture.Index;
                yield return ConstructTexture(textureId.Value, textureId.Id);
                mapper.NormalTexture = _assetCache.TextureCache[textureId.Id].CachedTexture.Texture;
                _assetCache.TextureCache[textureId.Id].CachedTexture.IncreaseRefCount();
                mapper.NormalTexCoord = def.NormalTexture.TexCoord;
                mapper.NormalTexScale = def.NormalTexture.Scale;
            }

            if (def.OcclusionTexture != null)
            {
                mapper.OcclusionTexStrength = def.OcclusionTexture.Strength;
                TextureId textureId = def.OcclusionTexture.Index;
                yield return ConstructTexture(textureId.Value, textureId.Id);
                mapper.OcclusionTexture = _assetCache.TextureCache[textureId.Id].CachedTexture.Texture;
                _assetCache.TextureCache[textureId.Id].CachedTexture.IncreaseRefCount();
            }

            if (def.EmissiveTexture != null)
            {
                TextureId textureId = def.EmissiveTexture.Index;
                yield return ConstructTexture(textureId.Value, textureId.Id, false, false);
                mapper.EmissiveTexture = _assetCache.TextureCache[textureId.Id].CachedTexture.Texture;
                _assetCache.TextureCache[textureId.Id].CachedTexture.IncreaseRefCount();
                mapper.EmissiveTexCoord = def.EmissiveTexture.TexCoord;
            }

            mapper.EmissiveFactor = def.EmissiveFactor;

            IUniformMap vertColorMapper = mapper.Clone();
            vertColorMapper.VertexColorsEnabled = true;

            Material[] material = new Material[2];

            const int MATERIAL = 0;
            const int MATERIAL_WITH_VERTEX_COLORS = 1;

            material[MATERIAL] = mapper.Material;
            material[MATERIAL_WITH_VERTEX_COLORS] = vertColorMapper.Material;

            MaterialCacheData materialWrapper = new MaterialCacheData();

            for (int i = 0; i < 2; i++)
            {
                string materialCRC = material[i].ComputeCRC().ToString();

                if (!addMaterialsToPersistentCaching)
                {
                    switch (i)
                    {
                        case MATERIAL:
                            materialWrapper.CachedMaterial = new RefCountedMaterialData(materialCRC, material[i]);
                            //
                            // if (!usedMaterials.Contains(material[i]))
                            // {
                            //     materialWrapper.CachedMaterial.IncreaseRefCount();
                            //     usedMaterials.Add(material[i]);
                            // }
                            //
                            break;
                        case MATERIAL_WITH_VERTEX_COLORS:
                            materialWrapper.CachedMaterialWithVertexColor = new RefCountedMaterialData(materialCRC, material[i]);
                            //
                            // if (!usedMaterials.Contains(material[i]))
                            // {
                            //     materialWrapper.CachedMaterialWithVertexColor.IncreaseRefCount();
                            //     usedMaterials.Add(material[i]);
                            // }
                            //
                            break;
                    }

                    continue;
                }

                //TODO(Brian): Remove old material here if the material won't be used.
                //             (We can use Resources.UnloadUnusedAssets too, but I hate to rely on this)
                if (!PersistentAssetCache.MaterialCacheByCRC.ContainsKey(materialCRC))
                {
                    RefCountedMaterialData newRefCountedMaterial = new RefCountedMaterialData(materialCRC, material[i]);
                    PersistentAssetCache.MaterialCacheByCRC.Add(materialCRC, newRefCountedMaterial);
                }
                else if (material[i] != PersistentAssetCache.MaterialCacheByCRC[materialCRC].material)
                {
                    Material.Destroy(material[i]);
                }

                switch (i)
                {
                    case MATERIAL:
                        materialWrapper.CachedMaterial = PersistentAssetCache.MaterialCacheByCRC[materialCRC];
                        break;
                    case MATERIAL_WITH_VERTEX_COLORS:
                        materialWrapper.CachedMaterialWithVertexColor = PersistentAssetCache.MaterialCacheByCRC[materialCRC];
                        break;
                }
            }

            materialWrapper.GLTFMaterial = def;

            if (materialIndex >= 0)
            {
                _assetCache.MaterialCache[materialIndex] = materialWrapper;
            }
            else
            {
                _defaultLoadedMaterial = materialWrapper;
            }
        }

        protected virtual int GetTextureSourceId(GLTFTexture texture) { return texture.Source.Id; }

        /// <summary>
        /// Creates a texture from a glTF texture
        /// </summary>
        /// <param name="texture">The texture to load</param>
        /// <returns>The loaded unity texture</returns>
        public virtual IEnumerator LoadTextureAsync(GLTFTexture texture, int textureIndex, bool markGpuOnly = true)
        {
            try
            {
                lock (this)
                {
                    if (_isRunning)
                    {
                        throw new GLTFLoadException("Cannot CreateTexture while GLTFSceneImporter is already running");
                    }

                    _isRunning = true;
                }

                if (_gltfRoot == null)
                {
                    yield return LoadJsonStream(_gltfFileName);
                    yield return LoadJson();
                }

                if (_assetCache == null)
                {
                    _assetCache = new AssetCache(_gltfRoot);
                }

                _timeAtLastYield = Time.realtimeSinceStartup;
                yield return ConstructImageBuffer(texture, textureIndex);
                yield return ConstructTexture(texture, textureIndex, markGpuOnly);
            }
            finally
            {
                lock (this)
                {
                    _isRunning = false;
                }
            }
        }

        /// <summary>
        /// Gets texture that has been loaded from CreateTexture
        /// </summary>
        /// <param name="textureIndex">The texture to get</param>
        /// <returns>Created texture</returns>
        public virtual Texture GetTexture(int textureIndex)
        {
            if (_assetCache == null)
            {
                throw new GLTFLoadException("Asset cache needs initialized before calling GetTexture");
            }

            if (_assetCache.TextureCache[textureIndex] == null)
            {
                return null;
            }

            return _assetCache.TextureCache[textureIndex].CachedTexture.Texture;
        }

        protected virtual IEnumerator ConstructTexture(GLTFTexture texture, int textureIndex,
            bool markGpuOnly = false, bool isLinear = true)
        {
            yield return new WaitUntil(() => _assetCache.TextureCache[textureIndex] != null);

            if (_assetCache.TextureCache[textureIndex].CachedTexture != null)
                yield break;

            int sourceId = GetTextureSourceId(texture);
            GLTFImage image = _gltfRoot.Images[sourceId];
            string imageId = GenerateImageId(image.Uri, sourceId);
            RefCountedTextureData source = null;

            if (PersistentAssetCache.HasImage(imageId))
            {
                source = PersistentAssetCache.GetImage(imageId);
                _assetCache.ImageCache[sourceId] = source.Texture;

                if (_assetCache.ImageCache[sourceId] == null)
                {
                    Debug.Log($"GLTFSceneImporter - ConstructTexture - null tex detected for {sourceId} / {image.Uri} / {id}, applying invalid-tex texture...");
                    _assetCache.ImageCache[sourceId] = Texture2D.redTexture;
                }
            }
            else
            {
                yield return ConstructImage(image, sourceId, markGpuOnly, isLinear);

                if (_assetCache.ImageCache[sourceId] == null)
                {
                    Debug.Log($"GLTFSceneImporter - ConstructTexture - null tex detected for {sourceId} / {image.Uri} / {id}, applying invalid-tex texture...");
                    _assetCache.ImageCache[sourceId] = Texture2D.redTexture;
                }

                if (addImagesToPersistentCaching)
                {
                    source = PersistentAssetCache.AddImage(imageId, _assetCache.ImageCache[sourceId]);
                }
                else
                {
                    source = new RefCountedTextureData(imageId, _assetCache.ImageCache[sourceId]);
                }
            }

            var desiredFilterMode = FilterMode.Bilinear;
            var desiredWrapMode = TextureWrapMode.Repeat;

            if (texture.Sampler != null)
            {
                var sampler = texture.Sampler.Value;
                switch (sampler.MinFilter)
                {
                    case MinFilterMode.Nearest:
                    case MinFilterMode.NearestMipmapNearest:
                    case MinFilterMode.NearestMipmapLinear:
                        desiredFilterMode = FilterMode.Point;
                        break;
                    case MinFilterMode.Linear:
                    case MinFilterMode.LinearMipmapNearest:
                    case MinFilterMode.LinearMipmapLinear:
                        desiredFilterMode = FilterMode.Bilinear;
                        break;
                    default:
                        Debug.LogWarning("Unsupported Sampler.MinFilter: " + sampler.MinFilter);
                        break;
                }

                switch (sampler.WrapS)
                {
                    case GLTF.Schema.WrapMode.ClampToEdge:
                        desiredWrapMode = TextureWrapMode.Clamp;
                        break;
                    case GLTF.Schema.WrapMode.Repeat:
                    default:
                        desiredWrapMode = TextureWrapMode.Repeat;
                        break;
                }
            }

            if (markGpuOnly || (source.Texture.filterMode == desiredFilterMode && source.Texture.wrapMode == desiredWrapMode))
            {
                _assetCache.TextureCache[textureIndex].CachedTexture = source;

                if (markGpuOnly)
                {
                    Debug.LogWarning("Ignoring sampler");
                }
            }
            else
            {
                if (source.Texture.isReadable)
                {
                    var unityTexture = Object.Instantiate(source.Texture);
                    unityTexture.filterMode = desiredFilterMode;
                    unityTexture.wrapMode = desiredWrapMode;

#if !UNITY_EDITOR
                    // NOTE(Brian): This breaks importing in edit mode, so only enable it for runtime.
                    unityTexture.Apply(false, true);
#endif
                    _assetCache.TextureCache[textureIndex].CachedTexture = new RefCountedTextureData(imageId, unityTexture);
                }
                else
                {
                    Debug.LogWarning("Skipping instantiation of non-readable texture: " + image.Uri);
                    _assetCache.TextureCache[textureIndex].CachedTexture = source;
                }
            }
        }

        protected virtual void ConstructImageFromGLB(GLTFImage image, int imageCacheIndex)
        {
            var texture = new Texture2D(0, 0);
            var bufferView = image.BufferView.Value;
            var data = new byte[bufferView.ByteLength];

            var bufferContents = _assetCache.BufferCache[bufferView.Buffer.Id];
            bufferContents.Stream.Position = bufferView.ByteOffset + bufferContents.ChunkOffset;
            bufferContents.Stream.Read(data, 0, data.Length);

            texture.LoadImage(data, true);

            _assetCache.ImageCache[imageCacheIndex] = texture;
        }

        protected virtual BufferCacheData ConstructBufferFromGLB(int bufferIndex)
        {
            GLTFParser.SeekToBinaryChunk(_gltfStream.Stream, bufferIndex, _gltfStream.StartPosition); // sets stream to correct start position
            return new BufferCacheData
            {
                Stream = _gltfStream.Stream,
                ChunkOffset = (uint) _gltfStream.Stream.Position
            };
        }

        protected virtual ExtTextureTransformExtension GetTextureTransform(TextureInfo def)
        {
            IExtension extension;
            if (_gltfRoot.ExtensionsUsed != null &&
                _gltfRoot.ExtensionsUsed.Contains(ExtTextureTransformExtensionFactory.EXTENSION_NAME) &&
                def.Extensions != null &&
                def.Extensions.TryGetValue(ExtTextureTransformExtensionFactory.EXTENSION_NAME, out extension))
            {
                return (ExtTextureTransformExtension) extension;
            }

            return null;
        }

        /// <summary>
        ///  Get the absolute path to a gltf uri reference.
        /// </summary>
        /// <param name="gltfPath">The path to the gltf file</param>
        /// <returns>A path without the filename or extension</returns>
        protected static string AbsoluteUriPath(string gltfPath)
        {
            var uri = new Uri(gltfPath);
            var partialPath = uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Query.Length - uri.Segments[uri.Segments.Length - 1].Length);
            return partialPath;
        }

        /// <summary>
        /// Get the absolute path a gltf file directory
        /// </summary>
        /// <param name="gltfPath">The path to the gltf file</param>
        /// <returns>A path without the filename or extension</returns>
        protected static string AbsoluteFilePath(string gltfPath)
        {
            var fileName = Path.GetFileName(gltfPath);
            var lastIndex = gltfPath.IndexOf(fileName, StringComparison.Ordinal);
            var partialPath = gltfPath.Substring(0, lastIndex);
            return partialPath;
        }

        public static Vector2 GLTFOffsetToUnitySpace(Vector2 offset, float textureYScale) { return new Vector2(offset.x, 1 - textureYScale - offset.y); }

        private string GenerateImageId(string uri, int sourceId)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return PersistentAssetCache.GetCacheId($"embedded{sourceId}", id);
            }

            if (GetAssetContentHash(uri, out string imageHash))
            {
                return imageHash;
            }

            return PersistentAssetCache.GetCacheId(uri, id);
        }

        private bool GetAssetContentHash(string uri, out string hash)
        {
            if (_loader.assetIdConverter != null)
            {
                return _loader.assetIdConverter(uri, out hash);
            }
            hash = uri;
            return false;
        }
    }
}