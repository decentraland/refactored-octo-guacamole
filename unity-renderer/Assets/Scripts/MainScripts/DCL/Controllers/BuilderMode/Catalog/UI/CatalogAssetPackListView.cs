using DCL.Configuration;
using UnityEngine;

public class CatalogAssetPackListView : ListView<CatalogItemPack>
{
    public GameObject categoryListView;
    public GameObject assetPackListView;
    public DynamicScrollSensitivity dynamicScrollSensitivity;

    public Transform categoryContentTransform;
    public CatalogAssetPackAdapter categoryAssetPackItemAdapterPrefab;
    public CatalogAssetPackAdapter catalogAssetPackItemAdapterPrefab;
    public System.Action<CatalogItemPack> OnCatalogPackClick;

    bool useAssetPackStyle = true;

    public override void AddAdapters()
    {
        base.AddAdapters();

        Transform transformToUse = contentPanelTransform;
        CatalogAssetPackAdapter prefabToUse = catalogAssetPackItemAdapterPrefab;

        if (!useAssetPackStyle)
        {
            transformToUse = categoryContentTransform;
            prefabToUse = categoryAssetPackItemAdapterPrefab;
            categoryListView.SetActive(true);
            assetPackListView.SetActive(false);
        }
        else
        {
            categoryListView.SetActive(false);
            assetPackListView.SetActive(true);
        }

        if (contentPanelTransform == null)
            return;

        foreach (CatalogItemPack catalogItemPack in contentList)
        {
            //TODO: SmartItems This quit all the smart items from the catalog
            if (catalogItemPack.id == BIWSettings.SMART_ITEM_ASSETS_PACK_ID)
                continue;

            CatalogAssetPackAdapter adapter = Instantiate(prefabToUse, transformToUse).GetComponent<CatalogAssetPackAdapter>();
            adapter.SetContent(catalogItemPack);
            adapter.OnCatalogItemPackClick += SceneAssetPackClick;
        }

        if (dynamicScrollSensitivity != null)
            dynamicScrollSensitivity.RecalculateSensitivity();

        AudioScriptableObjects.listItemAppear.ResetPitch();
    }

    public override void RemoveAdapters()
    {
        Transform transformToUse = contentPanelTransform;
        if (!useAssetPackStyle)
            transformToUse = categoryContentTransform;

        for (int i = 0; i < transformToUse.childCount; i++)
        {
            GameObject toRemove = transformToUse.GetChild(i).gameObject;
            Destroy(toRemove);
        }

        if (dynamicScrollSensitivity != null)
            dynamicScrollSensitivity.RecalculateSensitivity();
    }

    public void SetCategoryStyle() { useAssetPackStyle = false; }

    public void SetAssetPackStyle() { useAssetPackStyle = true; }

    void SceneAssetPackClick(CatalogItemPack sceneAssetPack) { OnCatalogPackClick?.Invoke(sceneAssetPack); }
}