using DCL.Configuration;
using DCL.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public enum BuildModeCatalogSection
{
    CATEGORIES,
    ASSET_PACKS,
    FAVOURITES
}

public interface ISceneCatalogController
{
    event Action OnHideCatalogClicked;
    event Action<CatalogItem> OnCatalogItemSelected;
    event Action OnResumeInput;
    event Action OnStopInput;
    event Action<PointerEventData, CatalogItemAdapter> OnPointerEnterInCatalogItemAdapter;
    event Action<PointerEventData, CatalogItemAdapter> OnPointerExitInCatalogItemAdapter;
    void Initialize(ISceneCatalogView view, IQuickBarController quickBarController);
    void Dispose();
    void AssetsPackFilter(bool isOn);
    void CategoryFilter(bool isOn);
    void FavoritesFilter(bool isOn);
    void ToggleCatalogExpanse();
    void QuickBarInput(int quickBarSlot);
    void ShowFavorites();
    void CatalogItemSelected(CatalogItem catalogItem);
    void OnCatalogItemPackSelected(CatalogItemPack catalogItemPack);
    void SceneCatalogBack();
    bool IsCatalogOpen();
    bool IsCatalogExpanded();
    void ShowCategories();
    void ShowAssetsPacks();
    void ShowCatalogContent();
    void OpenCatalog();
    void CloseCatalog();
    void RefreshAssetPack();
    void RefreshCatalog();
    CatalogItemAdapter GetLastCatalogItemDragged();
    void SetActive(bool isActive);
    BuildModeCatalogSection GetCurrentSection();
}

public class SceneCatalogController : ISceneCatalogController
{
    internal const string FAVORITE_NAME = "Favorites";

    public event Action OnHideCatalogClicked;
    public event Action<CatalogItem> OnCatalogItemSelected;
    public event Action OnResumeInput;
    public event Action OnStopInput;
    public event Action<PointerEventData, CatalogItemAdapter> OnPointerEnterInCatalogItemAdapter;
    public event Action<PointerEventData, CatalogItemAdapter> OnPointerExitInCatalogItemAdapter;

    internal ISceneCatalogView sceneCatalogView;
    internal IQuickBarController quickBarController;
    internal FavoritesController favoritesController;
    internal BIWSearchBarController biwSearchBarController;
    internal bool isShowingAssetPacks = false;
    internal bool isFilterByAssetPacks = true;
    internal BuildModeCatalogSection currentSection = BuildModeCatalogSection.ASSET_PACKS;

    public void Initialize(ISceneCatalogView sceneCatalogView, IQuickBarController quickBarController)
    {
        this.sceneCatalogView = sceneCatalogView;
        this.quickBarController = quickBarController;
        favoritesController = new FavoritesController(sceneCatalogView.catalogGroupList);
        biwSearchBarController = new BIWSearchBarController();
        biwSearchBarController.Initialize(sceneCatalogView);

        sceneCatalogView.OnHideCatalogClicked += HideCatalogClicked;

        if (sceneCatalogView.catalogAssetPackList != null)
            sceneCatalogView.catalogAssetPackList.OnCatalogPackClick += OnCatalogItemPackSelected;

        if (sceneCatalogView.catalogGroupList != null)
        {
            sceneCatalogView.catalogGroupList.OnCatalogItemClicked += CatalogItemSelected;
            sceneCatalogView.catalogGroupList.OnResumeInput += ResumeInput;
            sceneCatalogView.catalogGroupList.OnStopInput += StopInput;
            sceneCatalogView.catalogGroupList.OnPointerEnterInAdapter += OnPointerEnter;
            sceneCatalogView.catalogGroupList.OnPointerExitInAdapter += OnPointerExit;
        }

        if (sceneCatalogView.category != null)
            sceneCatalogView.category.onValueChanged.AddListener(CategoryFilter);

        if (sceneCatalogView.favorites != null)
            sceneCatalogView.favorites.onValueChanged.AddListener(FavoritesFilter);

        if (sceneCatalogView.assetPack != null)
            sceneCatalogView.assetPack.onValueChanged.AddListener(AssetsPackFilter);

        sceneCatalogView.OnSceneCatalogBack += SceneCatalogBack;
        quickBarController.OnQuickBarShortcutSelected += QuickBarInput;
        quickBarController.OnCatalogItemSelected += CatalogItemSelected;

        biwSearchBarController.OnFilterChange += AssetsFiltered;
        biwSearchBarController.OnFilterRemove += FilterRemoved;
    }

    public void Dispose()
    {
        sceneCatalogView.OnHideCatalogClicked -= HideCatalogClicked;

        if (sceneCatalogView.catalogAssetPackList != null)
            sceneCatalogView.catalogAssetPackList.OnCatalogPackClick -= OnCatalogItemPackSelected;

        if (sceneCatalogView.catalogGroupList != null)
        {
            sceneCatalogView.catalogGroupList.OnCatalogItemClicked -= CatalogItemSelected;
            sceneCatalogView.catalogGroupList.OnResumeInput -= ResumeInput;
            sceneCatalogView.catalogGroupList.OnStopInput -= StopInput;
            sceneCatalogView.catalogGroupList.OnPointerEnterInAdapter -= OnPointerEnter;
            sceneCatalogView.catalogGroupList.OnPointerExitInAdapter -= OnPointerExit;
        }

        if (sceneCatalogView.category != null)
            sceneCatalogView.category.onValueChanged.RemoveListener(CategoryFilter);

        if (sceneCatalogView.favorites != null)
            sceneCatalogView.favorites.onValueChanged.RemoveListener(FavoritesFilter);

        if (sceneCatalogView.assetPack != null)
            sceneCatalogView.assetPack.onValueChanged.RemoveListener(AssetsPackFilter);

        sceneCatalogView.OnSceneCatalogBack -= SceneCatalogBack;

        quickBarController.OnQuickBarShortcutSelected -= QuickBarInput;
        quickBarController.OnCatalogItemSelected -= CatalogItemSelected;

        biwSearchBarController.OnFilterChange -= AssetsFiltered;
        biwSearchBarController.OnFilterRemove -= FilterRemoved;

        favoritesController.Dispose();
        biwSearchBarController.Dispose();
    }

    public BuildModeCatalogSection GetCurrentSection() { return currentSection; }

    public void AssetsFiltered(List<Dictionary<string, List<CatalogItem>>> filterObjects)
    {
        ShowCatalogContent();
        if (sceneCatalogView.catalogGroupList != null)
            sceneCatalogView.catalogGroupList.SetContent(filterObjects);
    }

    public void FilterRemoved() { ShowLastSelectedSection(); }

    public void AssetsPackFilter(bool isOn)
    {
        if (!isOn)
            return;

        isFilterByAssetPacks = true;
        ShowAssetsPacks();
    }

    public void CategoryFilter(bool isOn)
    {
        if (!isOn)
            return;

        isFilterByAssetPacks = false;
        ShowCategories();
    }

    public void FavoritesFilter(bool isOn)
    {
        if (!isOn)
            return;

        ShowFavorites();
    }

    public void ToggleCatalogExpanse() { sceneCatalogView.ToggleCatalogExpanse(); }

    public void QuickBarInput(int quickBarSlot) { quickBarController.QuickBarObjectSelected(quickBarSlot); }

    public void ShowFavorites()
    {
        currentSection = BuildModeCatalogSection.FAVOURITES;
        biwSearchBarController.ReleaseFilters();

        sceneCatalogView.SetCatalogTitle(FAVORITE_NAME);
        ShowCatalogContent();

        List<Dictionary<string, List<CatalogItem>>> favorites = new List<Dictionary<string, List<CatalogItem>>>();
        Dictionary<string, List<CatalogItem>> groupedCategoryItems = new Dictionary<string, List<CatalogItem>>();
        groupedCategoryItems.Add(FAVORITE_NAME, favoritesController.GetFavorites());
        favorites.Add(groupedCategoryItems);

        sceneCatalogView.catalogGroupList.SetContent(favorites);
        sceneCatalogView.ShowBackButton(false);
    }

    public void CatalogItemSelected(CatalogItem catalogItem) { OnCatalogItemSelected?.Invoke(catalogItem); }

    public void ResumeInput() { OnResumeInput?.Invoke(); }

    public void StopInput() { OnStopInput?.Invoke(); }

    private void OnPointerEnter(PointerEventData eventData, CatalogItemAdapter adapter) { OnPointerEnterInCatalogItemAdapter?.Invoke(eventData, adapter); }

    private void OnPointerExit(PointerEventData eventData, CatalogItemAdapter adapter) { OnPointerExitInCatalogItemAdapter?.Invoke(eventData, adapter); }

    public void HideCatalogClicked() { OnHideCatalogClicked?.Invoke(); }

    public void OnCatalogItemPackSelected(CatalogItemPack catalogItemPack)
    {
        ShowCatalogContent();
        SetCatalogAssetPackInListView(catalogItemPack);
        sceneCatalogView.ShowBackButton(true);
    }

    internal void SetCatalogAssetPackInListView(CatalogItemPack catalogItemPack)
    {
        sceneCatalogView.SetCatalogTitle(catalogItemPack.title);
        Dictionary<string, List<CatalogItem>> groupedCatalogItem = new Dictionary<string, List<CatalogItem>>();

        foreach (CatalogItem sceneObject in catalogItemPack.assets)
        {
            string titleToUse = sceneObject.categoryName;

            if (!groupedCatalogItem.ContainsKey(titleToUse))
            {
                groupedCatalogItem.Add(titleToUse, GetAssetsListByCategory(titleToUse, catalogItemPack));
            }
        }

        List<Dictionary<string, List<CatalogItem>>> contentList = new List<Dictionary<string, List<CatalogItem>>>
        {
            groupedCatalogItem
        };

        sceneCatalogView.catalogGroupList.SetContent(contentList);
    }

    internal List<CatalogItem> GetAssetsListByCategory(string category, CatalogItemPack sceneAssetPack)
    {
        List<CatalogItem> catalogItemList = new List<CatalogItem>();

        foreach (CatalogItem catalogItem in sceneAssetPack.assets)
        {
            if (category == catalogItem.categoryName)
                catalogItemList.Add(catalogItem);
        }

        return catalogItemList;
    }

    public void SceneCatalogBack()
    {
        if (isShowingAssetPacks)
        {
            sceneCatalogView.CloseCatalog();
        }
        else
        {
            if (isFilterByAssetPacks)
                ShowAssetsPacks();
            else
                ShowCategories();

            sceneCatalogView.ShowBackButton(false);
            biwSearchBarController.ReleaseFilters();
        }
    }

    public bool IsCatalogOpen() { return sceneCatalogView.IsCatalogOpen(); }

    public bool IsCatalogExpanded() { return sceneCatalogView.IsCatalogExpanded(); }

    public void ShowCategories()
    {
        currentSection = BuildModeCatalogSection.CATEGORIES;
        biwSearchBarController.ReleaseFilters();

        if (sceneCatalogView.catalogAssetPackList != null)
        {
            sceneCatalogView.catalogAssetPackList.SetCategoryStyle();
            sceneCatalogView.catalogAssetPackList.SetContent(BIWCatalogManager.GetCatalogItemPacksFilteredByCategories());
            sceneCatalogView.catalogAssetPackList.gameObject.SetActive(true);
        }

        isShowingAssetPacks = true;
        sceneCatalogView.SetCatalogTitle(BIWSettings.CATALOG_ASSET_PACK_TITLE);

        if (sceneCatalogView.catalogGroupList != null)
            sceneCatalogView.catalogGroupList.gameObject.SetActive(false);

        sceneCatalogView.ShowBackButton(false);
    }

    public void ShowAssetsPacks()
    {
        currentSection = BuildModeCatalogSection.ASSET_PACKS;
        biwSearchBarController.ReleaseFilters();

        if (sceneCatalogView.catalogAssetPackList != null)
        {
            sceneCatalogView.catalogAssetPackList.SetAssetPackStyle();
            sceneCatalogView.catalogAssetPackList.gameObject.SetActive(true);
        }

        isShowingAssetPacks = true;
        sceneCatalogView.SetCatalogTitle(BIWSettings.CATALOG_ASSET_PACK_TITLE);
        RefreshCatalog();

        if (sceneCatalogView.catalogGroupList != null)
            sceneCatalogView.catalogGroupList.gameObject.SetActive(false);

        sceneCatalogView.ShowBackButton(false);
    }

    public void ShowCatalogContent()
    {
        isShowingAssetPacks = false;
        if (sceneCatalogView.catalogAssetPackList != null)
            sceneCatalogView.catalogAssetPackList.gameObject.SetActive(false);

        if (sceneCatalogView.catalogGroupList != null)
            sceneCatalogView.catalogGroupList.gameObject.SetActive(true);
    }

    public void OpenCatalog()
    {
        ShowLastSelectedSection();
        Utils.UnlockCursor();
        sceneCatalogView.SetActive(true);
    }

    public void CloseCatalog() { sceneCatalogView.CloseCatalog(); }

    public void RefreshAssetPack()
    {
        if (sceneCatalogView.catalogGroupList != null)
            sceneCatalogView.catalogGroupList.RefreshDisplay();
    }

    public void RefreshCatalog()
    {
        if (sceneCatalogView.catalogAssetPackList != null)
            sceneCatalogView.catalogAssetPackList.SetContent(BIWCatalogManager.GetCatalogItemPackList());
    }

    public CatalogItemAdapter GetLastCatalogItemDragged()
    {
        if (sceneCatalogView.catalogGroupList == null)
            return null;

        return sceneCatalogView.catalogGroupList.GetLastCatalogItemDragged();
    }

    private void ShowLastSelectedSection()
    {
        switch (currentSection)
        {
            case BuildModeCatalogSection.CATEGORIES:
                ShowCategories();
                break;
            case BuildModeCatalogSection.ASSET_PACKS:
                ShowAssetsPacks();
                break;
            case BuildModeCatalogSection.FAVOURITES:
                ShowFavorites();
                break;
        }
    }

    public void SetActive(bool isActive) { sceneCatalogView.SetActive(isActive); }
}