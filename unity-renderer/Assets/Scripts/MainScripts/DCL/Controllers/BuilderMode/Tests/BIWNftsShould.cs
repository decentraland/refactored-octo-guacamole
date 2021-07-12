using DCL;
using DCL.Components;
using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Tests;
using UnityEngine;
using UnityEngine.TestTools;
using Environment = DCL.Environment;

public class BIWNftsShould : IntegrationTestSuite
{
    private ParcelScene scene;
    private const string ENTITY_ID = "1";

    protected override WorldRuntimeContext CreateRuntimeContext()
    {
        return DCL.Tests.WorldRuntimeContextFactory.CreateWithCustomMocks
        (
            sceneController: new SceneController(),
            state: new WorldState(),
            componentFactory: new RuntimeComponentFactory(),
            sceneBoundsChecker: new SceneBoundsChecker()
        );
    }

    [UnitySetUp]
    protected override IEnumerator SetUp()
    {
        yield return base.SetUp();
        scene = (ParcelScene) Environment.i.world.sceneController.CreateTestScene();
        TestHelpers.CreateSceneEntity(scene, ENTITY_ID);
        BIWCatalogManager.Init();
        BIWTestHelper.CreateNFT();
    }

    [Test]
    public void NftsUsage()
    {
        string idToTest = BuilderInWorldNFTController.i.GetNfts()[0].assetContract.address;

        Assert.IsFalse(BuilderInWorldNFTController.i.IsNFTInUse(idToTest));

        BuilderInWorldNFTController.i.UseNFT(idToTest);
        Assert.IsTrue(BuilderInWorldNFTController.i.IsNFTInUse(idToTest));

        BuilderInWorldNFTController.i.StopUsingNFT(idToTest);
        Assert.IsFalse(BuilderInWorldNFTController.i.IsNFTInUse(idToTest));
    }

    [Test]
    public void NftComponent()
    {
        CatalogItem catalogItem = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];


        DCLBuilderInWorldEntity biwEntity = Utils.GetOrCreateComponent<DCLBuilderInWorldEntity>(scene.entities[ENTITY_ID].gameObject);
        biwEntity.Init(scene.entities[ENTITY_ID], null);

        NFTShape nftShape = (NFTShape) scene.SharedComponentCreate(catalogItem.id, Convert.ToInt32(CLASS_ID.NFT_SHAPE));
        nftShape.model = new NFTShape.Model();
        nftShape.model.color = new Color(0.6404918f, 0.611472f, 0.8584906f);
        nftShape.model.src = catalogItem.model;
        nftShape.model.assetId = catalogItem.id;

        scene.SharedComponentAttach(biwEntity.rootEntity.entityId, nftShape.id);

        Assert.IsTrue(biwEntity.IsEntityNFT());

        CatalogItem associatedCatalogItem = biwEntity.GetCatalogItemAssociated();
        Assert.IsTrue(associatedCatalogItem.IsNFT());
        Assert.AreEqual(associatedCatalogItem, catalogItem);
    }

    protected override IEnumerator TearDown()
    {
        BIWCatalogManager.ClearCatalog();
        BuilderInWorldNFTController.i.ClearNFTs();
        yield return base.TearDown();
        PoolManager.i.Cleanup();
    }
}