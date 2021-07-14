using DCL.Rendering;
using NSubstitute;

namespace DCL.Tests
{
    public static class PlatformContextFactory
    {
        public static PlatformContext CreateMocked() { return CreateWithCustomMocks(); }

        public static PlatformContext CreateWithGenericMocks(params object[] mocks)
        {
            IMemoryManager memoryManager = Substitute.For<IMemoryManager>();
            ICullingController cullingController = Substitute.For<ICullingController>();
            IParcelScenesCleaner parcelScenesCleaner = Substitute.For<IParcelScenesCleaner>();
            IClipboard clipboard = Substitute.For<IClipboard>();
            IPhysicsSyncController physicsSyncController = Substitute.For<IPhysicsSyncController>();
            IDebugController debugController = Substitute.For<IDebugController>();
            IWebRequestController webRequest = Substitute.For<IWebRequestController>();
            IServiceProviders serviceProviders = Substitute.For<IServiceProviders>();
            IIdleChecker idleChecker = Substitute.For<IIdleChecker>();
            IAvatarsLODController avatarsLODController = Substitute.For<IAvatarsLODController>();

            foreach ( var mock in mocks)
            {
                switch ( mock )
                {
                    case ICullingController cc:
                        cullingController = cc;
                        break;
                    case IMemoryManager mm:
                        memoryManager = mm;
                        break;
                    case IClipboard c:
                        clipboard = c;
                        break;
                    case IPhysicsSyncController psc:
                        physicsSyncController = psc;
                        break;
                    case IParcelScenesCleaner pscc:
                        parcelScenesCleaner = pscc;
                        break;
                    case IDebugController dc:
                        debugController = dc;
                        break;
                    case IWebRequestController wrc:
                        webRequest = wrc;
                        break;
                    case IServiceProviders sp:
                        serviceProviders = sp;
                        break;
                    case IIdleChecker idl:
                        idleChecker = idl;
                        break;
                    case IAvatarsLODController alodc:
                        avatarsLODController = alodc;
                        break;
                }
            }

            return new PlatformContext(memoryManager,
                cullingController,
                clipboard,
                physicsSyncController,
                parcelScenesCleaner,
                debugController,
                webRequest,
                serviceProviders,
                idleChecker,
                avatarsLODController);
        }

        public static PlatformContext CreateWithCustomMocks(
            IMemoryManager memoryManager = null,
            ICullingController cullingController = null,
            IClipboard clipboard = null,
            IPhysicsSyncController physicsSyncController = null,
            IParcelScenesCleaner parcelScenesCleaner = null,
            IDebugController debugController = null,
            IWebRequestController webRequestController = null,
            IServiceProviders serviceProviders = null,
            IIdleChecker idleChecker = null,
            IAvatarsLODController avatarsLODController = null)
        {
            return new PlatformContext(
                memoryManager: memoryManager ?? Substitute.For<IMemoryManager>(),
                cullingController: cullingController ?? Substitute.For<ICullingController>(),
                clipboard: clipboard ?? Substitute.For<IClipboard>(),
                physicsSyncController: physicsSyncController ?? Substitute.For<IPhysicsSyncController>(),
                parcelScenesCleaner: parcelScenesCleaner ?? Substitute.For<IParcelScenesCleaner>(),
                debugController: debugController ?? Substitute.For<IDebugController>(),
                webRequest: webRequestController ?? GetWebRequestControllerMock(),
                serviceProviders: serviceProviders ?? GetServiceProvidersMock(),
                idleChecker: idleChecker ?? Substitute.For<IIdleChecker>(),
                avatarsLODController: avatarsLODController ?? Substitute.For<IAvatarsLODController>());
        }

        private static IWebRequestController GetWebRequestControllerMock()
        {
            IWebRequestController webRequestControllerMock = Substitute.For<IWebRequestController>();
            webRequestControllerMock.Initialize(
                genericWebRequest: Substitute.For<IWebRequest>(),
                assetBundleWebRequest: Substitute.For<IWebRequestAssetBundle>(),
                textureWebRequest: Substitute.For<IWebRequest>(),
                audioWebRequest: Substitute.For<IWebRequestAudio>());

            return webRequestControllerMock;
        }

        private static IServiceProviders GetServiceProvidersMock()
        {
            IServiceProviders serviceProviders = Substitute.For<IServiceProviders>();

            ITheGraph theGraph = Substitute.For<ITheGraph>();
            serviceProviders.theGraph.Returns(theGraph);

            ICatalyst catalyst = Substitute.For<ICatalyst>();
            serviceProviders.catalyst.Returns(catalyst);

            return serviceProviders;
        }
    }
}