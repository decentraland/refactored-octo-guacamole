using DCL;
using DCL.Helpers;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

public class BIWCatalogShould
{
    private GameObject gameObjectToUse;

    [UnitySetUp]
    protected IEnumerator SetUp()
    {
        BIWCatalogManager.Init();
        gameObjectToUse = new GameObject("_TestObject");
        gameObjectToUse.AddComponent<AssetCatalogBridge>();
        yield return null;
    }

    [Test]
    public void BuilderInWorldSearch()
    {
        string nameToFilter = "Sandy";
        BIWTestHelper.CreateTestCatalogLocalMultipleFloorObjects();

        CatalogItem catalogItemToFilter = null;
        foreach (CatalogItem catalogItem in DataStore.i.builderInWorld.catalogItemDict.GetValues())
        {
            if (catalogItem.name.Contains(nameToFilter))
            {
                catalogItemToFilter = catalogItem;
                return;
            }
        }

        SceneCatalogController sceneCatalogController = new SceneCatalogController();
        List<Dictionary<string, List<CatalogItem>>>  result = sceneCatalogController.biwSearchBarController.FilterAssets(nameToFilter);

        CatalogItem filteredItem =  result[0].Values.ToList()[0][0];

        Assert.AreEqual(filteredItem, catalogItemToFilter);
    }

    [Test]
    public void BuilderInWorldQuickBar()
    {
        BIWTestHelper.CreateTestCatalogLocalSingleObject();
        CatalogItem item = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];

        CatalogItemAdapter adapter = BIWTestHelper.CreateCatalogItemAdapter(gameObjectToUse);
        adapter.SetContent(item);

        CatalogAssetGroupAdapter groupAdapter = new GameObject("_CatalogAssetGroupAdapter").AddComponent<CatalogAssetGroupAdapter>();
        groupAdapter.SubscribeToEvents(adapter);

        CatalogGroupListView catalogGroupListView = new GameObject("_CatalogGroupListView").AddComponent<CatalogGroupListView>();
        catalogGroupListView.SubscribeToEvents(groupAdapter);
        catalogGroupListView.generalCanvas = Utils.GetOrCreateComponent<Canvas>(gameObjectToUse);
        SceneCatalogView sceneCatalogView = SceneCatalogView.Create();
        sceneCatalogView.catalogGroupListView = catalogGroupListView;
        SceneCatalogController sceneCatalogController = new SceneCatalogController();

        QuickBarView quickBarView = QuickBarView.Create();

        QuickBarController quickBarController = new QuickBarController();
        sceneCatalogController.Initialize(sceneCatalogView, quickBarController);
        quickBarController.Initialize(quickBarView, sceneCatalogController);
        int slots = quickBarController.GetSlotsCount();
        quickBarView.shortcutsImgs = new QuickBarSlot[slots];

        for (int i = 0; i < slots; i++)
        {
            quickBarController.SetIndexToDrop(i);
            adapter.AdapterStartDragging(null);
            quickBarController.SetQuickBarShortcut(item, i, new Texture2D(10, 10));
            Assert.AreEqual(item, quickBarController.QuickBarObjectSelected(i));
        }
    }

    [Test]
    public void BuilderInWorldToggleFavorite()
    {
        BIWTestHelper.CreateTestCatalogLocalSingleObject();

        CatalogItem item = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];

        FavoritesController favoritesController = new FavoritesController(new GameObject("_FavoritesController").AddComponent<CatalogGroupListView>());
        favoritesController.ToggleFavoriteState(item, null);
        Assert.IsTrue(item.IsFavorite());

        favoritesController.ToggleFavoriteState(item, null);
        Assert.IsFalse(item.IsFavorite());
    }

    [Test]
    public void CatalogItemsSceneObject()
    {
        BIWTestHelper.CreateTestCatalogLocalSingleObject();

        Assert.AreEqual(DataStore.i.builderInWorld.catalogItemDict.Count(), 1);
        Assert.AreEqual(DataStore.i.builderInWorld.catalogItemPackDict.Count(), 1);
        Assert.AreEqual(BIWCatalogManager.GetCatalogItemPacksFilteredByCategories().Count, 1);
    }

    [Test]
    public void CatalogItemsNfts()
    {
        BIWTestHelper.CreateNFT();

        Assert.AreEqual(DataStore.i.builderInWorld.catalogItemDict.Count(), 1);
        Assert.AreEqual(DataStore.i.builderInWorld.catalogItemPackDict.Count(), 1);
        Assert.AreEqual(BIWCatalogManager.GetCatalogItemPacksFilteredByCategories().Count, 1);
    }

    [UnityTearDown]
    protected IEnumerator TearDown()
    {
        AssetCatalogBridge.i.ClearCatalog();
        BIWCatalogManager.ClearCatalog();
        BIWCatalogManager.Dispose();
        if (gameObjectToUse != null)
            GameObject.Destroy(gameObjectToUse);
        yield return null;
    }
}