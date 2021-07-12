using DCL;
using DCL.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatalogAssetPackAdapter : MonoBehaviour
{
    public TextMeshProUGUI titleTxt;
    public RawImage packImg;
    public Texture collectiblesSprite;

    public event Action<CatalogItemPack> OnCatalogItemPackClick;
    CatalogItemPack catalogItemPack;

    string loadedThumbnailURL;
    AssetPromise_Texture loadedThumbnailPromise;
    public void SetContent(CatalogItemPack catalogItemPack)
    {
        this.catalogItemPack = catalogItemPack;
        titleTxt.text = this.catalogItemPack.title;

        if (catalogItemPack.id != BIWSettings.ASSETS_COLLECTIBLES)
        {
            GetThumbnail();
        }
        else
        {
            packImg.enabled = true;
            packImg.texture = collectiblesSprite;
        }
    }

    private void GetThumbnail()
    {
        var url = catalogItemPack?.GetThumbnailUrl();

        if (url == loadedThumbnailURL)
            return;

        if (catalogItemPack == null || string.IsNullOrEmpty(url))
            return;

        string newLoadedThumbnailURL = url;
        var newLoadedThumbnailPromise = new AssetPromise_Texture(url);


        newLoadedThumbnailPromise.OnSuccessEvent += SetThumbnail;
        newLoadedThumbnailPromise.OnFailEvent += x => { Debug.Log($"Error downloading: {url}"); };

        AssetPromiseKeeper_Texture.i.Keep(newLoadedThumbnailPromise);


        AssetPromiseKeeper_Texture.i.Forget(loadedThumbnailPromise);
        loadedThumbnailPromise = newLoadedThumbnailPromise;
        loadedThumbnailURL = newLoadedThumbnailURL;
    }

    public void SetThumbnail(Asset_Texture texture)
    {
        if (packImg != null)
        {
            packImg.enabled = true;
            packImg.texture = texture.texture;
        }
    }

    public void SceneAssetPackClick() { OnCatalogItemPackClick?.Invoke(catalogItemPack); }
}