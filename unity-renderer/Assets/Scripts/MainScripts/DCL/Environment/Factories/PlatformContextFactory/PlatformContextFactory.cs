using DCL.Bots;
using DCL.Rendering;

namespace DCL
{
    public static class PlatformContextFactory
    {
        public static PlatformContext CreateDefault()
        {
            return new PlatformContext(
                memoryManager: new MemoryManager(),
                cullingController: CullingController.Create(),
                clipboard: Clipboard.Create(),
                physicsSyncController: new PhysicsSyncController(),
                parcelScenesCleaner: new ParcelScenesCleaner(),
                debugController: new DebugController(new BotsController()),
                webRequest: WebRequestController.Create(),
                serviceProviders: new ServiceProviders(),
                idleChecker: new IdleChecker(),
                avatarsLODController: new AvatarsLODController());
        }
    }
}