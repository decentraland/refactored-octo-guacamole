using DCL.Rendering;

namespace DCL
{
    /// <summary>
    /// Context related to specific platform handling.
    /// Memory, rendering, input, IO and debug systems belong here.
    /// </summary>
    public class PlatformContext : System.IDisposable
    {
        public readonly IMemoryManager memoryManager;
        public readonly ICullingController cullingController;
        public readonly IParcelScenesCleaner parcelScenesCleaner;
        public readonly IClipboard clipboard;
        public readonly IPhysicsSyncController physicsSyncController;
        public readonly IDebugController debugController;
        public readonly IWebRequestController webRequest;
        public readonly IServiceProviders serviceProviders;
        public readonly IIdleChecker idleChecker;
        public readonly IAvatarsLODController avatarsLODController;

        public PlatformContext(IMemoryManager memoryManager,
            ICullingController cullingController,
            IClipboard clipboard,
            IPhysicsSyncController physicsSyncController,
            IParcelScenesCleaner parcelScenesCleaner,
            IDebugController debugController,
            IWebRequestController webRequest,
            IServiceProviders serviceProviders,
            IIdleChecker idleChecker,
            IAvatarsLODController avatarsLODController)
        {
            this.memoryManager = memoryManager;
            this.cullingController = cullingController;
            this.clipboard = clipboard;
            this.physicsSyncController = physicsSyncController;
            this.parcelScenesCleaner = parcelScenesCleaner;
            this.debugController = debugController;
            this.webRequest = webRequest;
            this.serviceProviders = serviceProviders;
            this.idleChecker = idleChecker;
            this.avatarsLODController = avatarsLODController;
        }

        public void Update()
        {
            idleChecker.Update();
            avatarsLODController.Update();
        }

        public void Dispose()
        {
            memoryManager.Dispose();
            parcelScenesCleaner.Dispose();
            cullingController.Dispose();
            debugController.Dispose();
            webRequest.Dispose();
            serviceProviders.Dispose();
            avatarsLODController.Dispose();
        }
    }
}