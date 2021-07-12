using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DCL;
using DCL.Components;
using DCL.Helpers;
using DCL.Models;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BIWCreatorShould : IntegrationTestSuite_Legacy
{
    private BIWEntityHandler entityHandler;
    private BIWCreatorController biwCreatorController;

    protected override IEnumerator SetUp()
    {
        yield return base.SetUp();

        biwCreatorController = new BIWCreatorController();
        entityHandler = new BIWEntityHandler();
        var referencesController = BIWTestHelper.CreateReferencesControllerWithGenericMocks(
            entityHandler,
            biwCreatorController
        );

        biwCreatorController.Init(referencesController);
        entityHandler.Init(referencesController);

        entityHandler.EnterEditMode(scene);
        biwCreatorController.EnterEditMode(scene);
    }

    [Test]
    public void CreateItem()
    {
        //Arrange
        BIWCatalogManager.Init();
        BIWTestHelper.CreateTestCatalogLocalSingleObject();
        CatalogItem item = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];

        //Act
        biwCreatorController.CreateCatalogItem(item);

        //Assert
        foreach (DCLBuilderInWorldEntity entity in entityHandler.GetAllEntitiesFromCurrentScene())
        {
            Assert.IsTrue(entity.GetCatalogItemAssociated().id == item.id);
            Assert.AreEqual(Vector3.zero, entity.GetEulerRotation());
        }
    }

    [Test]
    public void CreateLastItem()
    {
        //Arrange
        BIWCatalogManager.Init();
        BIWTestHelper.CreateTestCatalogLocalSingleObject();
        CatalogItem item = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];

        //Act
        biwCreatorController.CreateCatalogItem(item);
        biwCreatorController.CreateLastCatalogItem();

        //Assert
        int cont = 0;
        foreach (DCLBuilderInWorldEntity entity in entityHandler.GetAllEntitiesFromCurrentScene())
        {
            if (entity.GetCatalogItemAssociated().id == item.id)
                cont++;
        }
        Assert.AreEqual(cont, 2);
    }

    [Test]
    public void LoadingGameObjectCreation()
    {
        //Arrange
        BIWCatalogManager.Init();
        BIWTestHelper.CreateTestCatalogLocalSingleObject();
        CatalogItem item = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];

        //Act
        biwCreatorController.CreateCatalogItem(item);

        //Assert
        DCLBuilderInWorldEntity entity = entityHandler.GetAllEntitiesFromCurrentScene().FirstOrDefault();
        Assert.IsTrue(biwCreatorController.ExistsLoadingGameObjectForEntity(entity.rootEntity.entityId));
    }

    [Test]
    public void LoadingGameObjectDestruction()
    {
        //Arrange
        BIWCatalogManager.Init();
        BIWTestHelper.CreateTestCatalogLocalSingleObject();
        CatalogItem item = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];

        //Act
        biwCreatorController.CreateCatalogItem(item);
        DCLBuilderInWorldEntity entity = entityHandler.GetAllEntitiesFromCurrentScene().FirstOrDefault();
        biwCreatorController.RemoveLoadingObject(entity.rootEntity.entityId);

        //Assert
        Assert.IsFalse(biwCreatorController.ExistsLoadingGameObjectForEntity(entity.rootEntity.entityId));
    }

    [Test]
    public void ErrorGameObjectCreation()
    {
        //Arrange
        BIWCatalogManager.Init();
        BIWTestHelper.CreateTestCatalogLocalSingleObject();
        CatalogItem item = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];
        biwCreatorController.CreateCatalogItem(item);
        DCLBuilderInWorldEntity entity = entityHandler.GetAllEntitiesFromCurrentScene().FirstOrDefault();

        //Act
        biwCreatorController.CreateErrorOnEntity(entity);

        //Assert
        Assert.IsTrue(biwCreatorController.IsAnyErrorOnEntities());
    }

    [Test]
    public void ErrorGameObjectDestruction()
    {
        //Arrange
        BIWCatalogManager.Init();
        BIWTestHelper.CreateTestCatalogLocalSingleObject();
        CatalogItem item = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];
        biwCreatorController.CreateCatalogItem(item);
        DCLBuilderInWorldEntity entity = entityHandler.GetAllEntitiesFromCurrentScene().FirstOrDefault();
        biwCreatorController.CreateErrorOnEntity(entity);

        //Act
        biwCreatorController.DeleteErrorOnEntity(entity);

        //Assert
        Assert.IsFalse(biwCreatorController.IsAnyErrorOnEntities());
    }

    [Test]
    public void CatalogItemAddMapings()
    {
        //Arrange
        BIWCatalogManager.Init();
        BIWTestHelper.CreateTestCatalogLocalSingleObject();
        CatalogItem item = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];

        //Act
        biwCreatorController.CreateCatalogItem(item);

        //Assert
        LoadParcelScenesMessage.UnityParcelScene data = scene.sceneData;
        foreach (KeyValuePair<string, string> content in item.contents)
        {
            ContentServerUtils.MappingPair mappingPair = new ContentServerUtils.MappingPair();
            mappingPair.file = content.Key;
            mappingPair.hash = content.Value;
            bool found = false;
            foreach (ContentServerUtils.MappingPair mappingPairToCheck in data.contents)
            {
                if (mappingPairToCheck.file == mappingPair.file)
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found);
        }
    }

    protected override IEnumerator TearDown()
    {
        BIWCatalogManager.ClearCatalog();
        BuilderInWorldNFTController.i.ClearNFTs();
        foreach (var placeHolder in GameObject.FindObjectsOfType<BIWLoadingPlaceHolder>())
        {
            placeHolder.Dispose();
        }
        biwCreatorController.Clean();

        yield return base.TearDown();
    }
}