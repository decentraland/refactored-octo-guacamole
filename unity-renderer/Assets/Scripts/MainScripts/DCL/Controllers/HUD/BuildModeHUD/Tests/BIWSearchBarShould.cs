using System.Collections;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

public class BIWSearchBarShould : MonoBehaviour
{
    private SceneCatalogController sceneCatalogController;
    private BIWSearchBarController biwSearchBarController;
    private GameObject gameObjectToUse;

    [SetUp]
    public void SetUp()
    {
        BIWCatalogManager.Init();
        sceneCatalogController = new SceneCatalogController();
        sceneCatalogController.Initialize(
            Substitute.For<ISceneCatalogView>(),
            Substitute.For<IQuickBarController>());
        biwSearchBarController = sceneCatalogController.biwSearchBarController;
        gameObjectToUse = new GameObject("_TestObject");
        gameObjectToUse.AddComponent<AssetCatalogBridge>();
    }

    [TearDown]
    public void TearDown()
    {
        AssetCatalogBridge.i.ClearCatalog();
        BIWCatalogManager.ClearCatalog();
        BIWCatalogManager.Dispose();
        sceneCatalogController.Dispose();
        if (gameObjectToUse != null)
            GameObject.Destroy(gameObjectToUse);
    }

    [Test]
    public void AddNewSceneObjectCategoryToFilterCorrectly()
    {
        // Arrange
        string testCategory = "testCategory";

        CatalogItem testCatalogItem = new CatalogItem { category = testCategory };
        biwSearchBarController.filterObjects.Clear();

        // Act
        biwSearchBarController.AddNewSceneObjectCategoryToFilter(testCatalogItem);

        // Assert
        Assert.AreEqual(1, biwSearchBarController.filterObjects.Count, "The number of filter objects does not match!");
        Assert.IsTrue(biwSearchBarController.filterObjects[0].ContainsKey(testCategory), "The test category has not been found in the filter objects!");
    }

    [Test]
    public void FilterByName()
    {
        // Arrange
        string nameToFilter = "dirt";
        BIWTestHelper.CreateTestCatalogLocalSingleObject();

        // Act
        biwSearchBarController.FilterAssets(nameToFilter);

        // Assert
        Assert.AreEqual(1, biwSearchBarController.filterObjects.Count, "The number of filter objects does not match!");
    }

    [Test]
    public void FilterByNameNoResult()
    {
        // Arrange
        string nameToFilter = "sand";
        BIWTestHelper.CreateTestCatalogLocalSingleObject();

        // Act
        biwSearchBarController.FilterAssets(nameToFilter);

        // Assert
        Assert.AreEqual(0, biwSearchBarController.filterObjects.Count, "The number of filter objects does not match!");
    }

    [Test]
    public void FilterByCategory()
    {
        // Arrange
        string nameToFilter = "decorations";
        BIWTestHelper.CreateTestCatalogLocalSingleObject();

        // Act
        biwSearchBarController.FilterAssets(nameToFilter);

        // Assert
        Assert.AreEqual(1, biwSearchBarController.filterObjects.Count, "The number of filter objects does not match!");
    }

    [Test]
    public void FilterByCategoryNoResult()
    {
        // Arrange
        string nameToFilter = "structure";
        BIWTestHelper.CreateTestCatalogLocalSingleObject();

        // Act
        biwSearchBarController.FilterAssets(nameToFilter);

        // Assert
        Assert.AreEqual(0, biwSearchBarController.filterObjects.Count, "The number of filter objects does not match!");
    }

    [Test]
    public void FilterByTag()
    {
        // Arrange
        string nameToFilter = "fantasy";
        BIWTestHelper.CreateTestCatalogLocalSingleObject();

        // Act
        biwSearchBarController.FilterAssets(nameToFilter);

        // Assert
        Assert.AreEqual(1, biwSearchBarController.filterObjects.Count, "The number of filter objects does not match!");
    }

    [Test]
    public void FilterByTagNoResult()
    {
        // Arrange
        string nameToFilter = "wood";
        BIWTestHelper.CreateTestCatalogLocalSingleObject();

        // Act
        biwSearchBarController.FilterAssets(nameToFilter);

        // Assert
        Assert.AreEqual(0, biwSearchBarController.filterObjects.Count, "The number of filter objects does not match!");
    }

    [Test]
    public void FilterBySmartItem()
    {
        // Arrange
        BIWTestHelper.CreateTestSmartItemCatalogLocalSingleObject();

        // Act
        biwSearchBarController.ChangeSmartItemFilter();

        //TODO: SmartItems implement again the test after kernel implement smart items
        // Assert
        //Assert.AreEqual(1, biwSearchBarController.filterObjects.Count, "The number of filter objects does not match!");
    }

    [Test]
    public void FilterBySmartItemNoResult()
    {
        // Arrange
        BIWTestHelper.CreateTestCatalogLocalSingleObject();

        // Act
        biwSearchBarController.ChangeSmartItemFilter();

        // Assert
        Assert.AreEqual(0, biwSearchBarController.filterObjects.Count, "The number of filter objects does not match!");
    }
}