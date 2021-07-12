using DCL.Helpers;
using DCL.Helpers.NFT;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using DCL.Components;
using DCL.Controllers;
using DCL.Models;
using NSubstitute;
using UnityEngine;
using UnityEngine.UI;

public static class BIWTestHelper
{
    public static BIWReferencesController CreateMockUpReferenceController()
    {
        BIWReferencesController referencesController = new BIWReferencesController();
        referencesController.Init(
            Substitute.For<IBIWOutlinerController>(),
            Substitute.For<IBIWInputHandler>(),
            Substitute.For<IBIWInputWrapper>(),
            Substitute.For<IBIWPublishController>(),
            Substitute.For<IBIWCreatorController>(),
            Substitute.For<IBIWModeController>(),
            Substitute.For<IBIWFloorHandler>(),
            Substitute.For<IBIWEntityHandler>(),
            Substitute.For<IBIActionController>(),
            Substitute.For<IBIWSaveController>()
        );
        return referencesController;
    }
    public static BIWReferencesController CreateReferencesControllerWithGenericMocks(params object[] mocks)
    {
        IBIWOutlinerController outliner = Substitute.For<IBIWOutlinerController>();
        IBIWInputHandler inputHandler = Substitute.For<IBIWInputHandler>();
        IBIWInputWrapper inputWrapper = Substitute.For<IBIWInputWrapper>();
        IBIWPublishController publishController = Substitute.For<IBIWPublishController>();
        IBIWCreatorController creatorController = Substitute.For<IBIWCreatorController>();
        IBIWModeController modeController = Substitute.For<IBIWModeController>();
        IBIWFloorHandler floorHandler = Substitute.For<IBIWFloorHandler>();
        IBIWEntityHandler entityHandler = Substitute.For<IBIWEntityHandler>();
        IBIActionController actionController = Substitute.For<IBIActionController>();
        IBIWSaveController saveController = Substitute.For<IBIWSaveController>();

        foreach ( var mock in mocks)
        {
            switch ( mock )
            {
                case IBIWOutlinerController oc:
                    outliner = oc;
                    break;
                case IBIWInputHandler ih:
                    inputHandler = ih;
                    break;
                case IBIWInputWrapper iw:
                    inputWrapper = iw;
                    break;
                case IBIWPublishController pc:
                    publishController = pc;
                    break;
                case IBIWCreatorController cc:
                    creatorController = cc;
                    break;
                case IBIWModeController mc:
                    modeController = mc;
                    break;
                case IBIWFloorHandler fh:
                    floorHandler = fh;
                    break;
                case IBIWEntityHandler eh:
                    entityHandler = eh;
                    break;
                case IBIActionController ac:
                    actionController = ac;
                    break;
                case IBIWSaveController sc:
                    saveController = sc;
                    break;
            }
        }

        BIWReferencesController referencesController = new BIWReferencesController();
        referencesController.Init(
            outliner,
            inputHandler,
            inputWrapper,
            publishController,
            creatorController,
            modeController,
            floorHandler,
            entityHandler,
            actionController,
            saveController
        );
        return referencesController;
    }

    public static DCLBuilderInWorldEntity CreateSmartItemEntity(BIWEntityHandler entityHandler, ParcelScene scene, SmartItemComponent.Model model = null)
    {
        if (model == null)
            model = new SmartItemComponent.Model();

        DCLBuilderInWorldEntity entity = entityHandler.CreateEmptyEntity(scene, Vector3.zero, Vector3.zero);

        scene.EntityComponentCreateOrUpdateWithModel(entity.rootEntity.entityId, CLASS_ID_COMPONENT.SMART_ITEM, model);

        return entity;
    }

    public static CatalogItemAdapter CreateCatalogItemAdapter(GameObject gameObject)
    {
        CatalogItemAdapter adapter = Utils.GetOrCreateComponent<CatalogItemAdapter>(gameObject);
        adapter.onFavoriteColor = Color.white;
        adapter.offFavoriteColor = Color.white;
        adapter.favImg = Utils.GetOrCreateComponent<Image>(gameObject);
        adapter.smartItemGO = gameObject;
        adapter.lockedGO = gameObject;
        adapter.canvasGroup = Utils.GetOrCreateComponent<CanvasGroup>(gameObject);

        GameObject newGameObject = new GameObject();
        newGameObject.transform.SetParent(gameObject.transform);
        adapter.thumbnailImg = Utils.GetOrCreateComponent<RawImage>(newGameObject);
        return adapter;
    }

    public static void CreateTestCatalogLocalMultipleFloorObjects()
    {
        AssetCatalogBridge.i.ClearCatalog();
        string jsonPath = TestAssetsUtils.GetPathRaw() + "/BuilderInWorldCatalog/multipleSceneObjectsCatalog.json";

        if (File.Exists(jsonPath))
        {
            string jsonValue = File.ReadAllText(jsonPath);
            AssetCatalogBridge.i.AddFullSceneObjectCatalog(jsonValue);
        }
    }

    public static void CreateTestCatalogLocalSingleObject()
    {
        AssetCatalogBridge.i.ClearCatalog();
        string jsonPath = TestAssetsUtils.GetPathRaw() + "/BuilderInWorldCatalog/sceneObjectCatalog.json";

        if (File.Exists(jsonPath))
        {
            string jsonValue = File.ReadAllText(jsonPath);
            AssetCatalogBridge.i.AddFullSceneObjectCatalog(jsonValue);
        }
    }

    public static void CreateTestSmartItemCatalogLocalSingleObject()
    {
        AssetCatalogBridge.i.ClearCatalog();
        string jsonPath = TestAssetsUtils.GetPathRaw() + "/BuilderInWorldCatalog/smartItemSceneObjectCatalog.json";

        if (File.Exists(jsonPath))
        {
            string jsonValue = File.ReadAllText(jsonPath);
            AssetCatalogBridge.i.AddFullSceneObjectCatalog(jsonValue);
        }
    }

    public static void CreateNFT()
    {
        string jsonPath = TestAssetsUtils.GetPathRaw() + "/BuilderInWorldCatalog/nftAsset.json";

        if (File.Exists(jsonPath))
        {
            string jsonValue = File.ReadAllText(jsonPath);
            NFTOwner owner = NFTOwner.defaultNFTOwner;
            owner.assets.Add(JsonUtility.FromJson<NFTInfo>(jsonValue));
            BuilderInWorldNFTController.i.NftsFeteched(owner);
        }
    }
}