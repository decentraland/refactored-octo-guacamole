using DCL;
using DCL.Components;
using DCL.Configuration;
using DCL.Controllers;
using DCL.Helpers;
using DCL.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DCLBuilderInWorldEntity : EditableEntity
{
    public string entityUniqueId;

    public event Action<DCLBuilderInWorldEntity> OnShapeFinishLoading;
    public event Action<DCLBuilderInWorldEntity> OnStatusUpdate;
    public event Action<DCLBuilderInWorldEntity> OnDelete;
    public event Action<DCLBuilderInWorldEntity> OnErrorStatusChange;

    public bool IsLocked
    {
        get { return GetIsLockedValue(); }
        set
        {
            SetIsLockedValue(value);
            OnStatusUpdate?.Invoke(this);
        }
    }

    private bool isSelectedValue = false;

    public bool IsSelected
    {
        get { return isSelectedValue; }
        set
        {
            isSelectedValue = value;
            OnStatusUpdate?.Invoke(this);
        }
    }

    private bool isNewValue = false;

    public bool IsNew
    {
        get { return isNewValue; }
        set
        {
            isNewValue = value;
            OnStatusUpdate?.Invoke(this);
        }
    }

    private bool isVisibleValue = true;

    public bool IsVisible
    {
        get { return isVisibleValue; }
        set
        {
            isVisibleValue = value;
            OnStatusUpdate?.Invoke(this);
        }
    }

    public bool isVoxel { get; set; } = false;

    CatalogItem associatedCatalogItem;

    public bool isFloor { get; set; } = false;
    public bool isNFT { get; private set; } = false;

    private bool isShapeComponentSet = false;

    private Animation[] meshAnimations;

    private Vector3 currentRotation;
    private Transform originalParent;

    private Material[] originalMaterials;

    private Material editMaterial;

    private Dictionary<string, List<GameObject>> collidersGameObjectDictionary = new Dictionary<string, List<GameObject>>();

    private Vector3 lastPositionReported;
    private Vector3 lastScaleReported;
    private Quaternion lastRotationReported;

    #region Error Handler definition

    public bool hasError  { get; private set; } = false;

    public bool hasMissingCatalogItemError { get; private set; } = false;
    public bool isInsideBoundariesError { get; private set; } = false;

    #endregion

    public void Init(IDCLEntity entity, Material editMaterial)
    {
        rootEntity = entity;
        rootEntity.OnShapeUpdated += OnShapeUpdate;
        rootEntity.OnNameChange += OnNameUpdate;

        this.editMaterial = editMaterial;
        isVoxel = false;


        entityUniqueId = rootEntity.scene.sceneData.id + rootEntity.entityId;
        IsVisible = rootEntity.gameObject.activeSelf;

        isShapeComponentSet = false;
        InitRotation();

        if (rootEntity.meshRootGameObject && rootEntity.meshesInfo.renderers.Length > 0)
        {
            ShapeInit();
        }
    }

    public CatalogItem GetCatalogItemAssociated()
    {
        if (associatedCatalogItem != null)
            return associatedCatalogItem;

        if (rootEntity == null)
            return null;

        IAssetCatalogReferenceHolder catalogHolder = rootEntity.TryGetComponent<IAssetCatalogReferenceHolder>();
        if (catalogHolder == null)
            return null;

        string assetId = catalogHolder.GetAssetId();

        if (!string.IsNullOrEmpty(assetId) && DataStore.i.builderInWorld.catalogItemDict.TryGetValue(assetId, out associatedCatalogItem))
            return associatedCatalogItem;

        return null;
    }

    public bool HasShape() { return isShapeComponentSet; }

    public bool HasMovedSinceLastReport() { return Vector3.Distance(lastPositionReported, transform.position) >= BIWSettings.ENTITY_POSITION_REPORTING_THRESHOLD; }

    public bool HasScaledSinceLastReport() { return Math.Abs(lastScaleReported.magnitude - transform.lossyScale.magnitude) >= BIWSettings.ENTITY_SCALE_REPORTING_THRESHOLD; }

    public bool HasRotatedSinceLastReport() { return Quaternion.Angle(lastRotationReported, transform.rotation) >= BIWSettings.ENTITY_ROTATION_REPORTING_THRESHOLD; }

    public void PositionReported() { lastPositionReported = transform.position; }

    public void ScaleReported() { lastScaleReported = transform.lossyScale; }

    public void RotationReported() { lastRotationReported = transform.rotation; }

    #region Error Handling

    public void CheckErrors()
    {
        bool isCurrentlyWithError = false;

        //If the entity doesn't have a catalog item associated, we can be sure that the item is deleted
        if (GetCatalogItemAssociated() == null)
        {
            hasMissingCatalogItemError = true;
            isCurrentlyWithError = true;
        }

        //If entity is not inside boundaries it has an error
        if (isInsideBoundariesError)
            isCurrentlyWithError = true;

        bool hasErrorPreviously = hasError;
        hasError = isCurrentlyWithError;

        if (isCurrentlyWithError != hasErrorPreviously)
            OnErrorStatusChange?.Invoke(this);
    }

    public void SetEntityBoundariesError(bool isInsideBoundaries)
    {
        isInsideBoundariesError = !isInsideBoundaries;
        CheckErrors();
    }

    #endregion

    public void Select()
    {
        IsSelected = true;
        originalParent = rootEntity.gameObject.transform.parent;
        SetEditMaterial();
        lastPositionReported = transform.position;
        lastScaleReported = transform.lossyScale;
        lastRotationReported = transform.rotation;
    }

    public void Deselect()
    {
        if (!IsSelected)
            return;

        IsNew = false;
        IsSelected = false;
        if (rootEntity.gameObject != null)
            rootEntity.gameObject.transform.SetParent(originalParent);

        SetOriginalMaterials();
    }

    public void ToggleShowStatus()
    {
        rootEntity.gameObject.SetActive(!gameObject.activeSelf);
        IsVisible = gameObject.activeSelf;
        OnStatusUpdate?.Invoke(this);
    }

    public void ToggleLockStatus() { IsLocked = !IsLocked; }

    public void ShapeLoadFinish(ISharedComponent component) { OnShapeFinishLoading?.Invoke(this); }

    public void Delete()
    {
        Deselect();
        Dispose();
        OnDelete?.Invoke(this);
    }

    public void Dispose()
    {
        if (rootEntity != null)
        {
            rootEntity.OnShapeUpdated -= OnShapeUpdate;
            rootEntity.OnNameChange -= OnNameUpdate;

            if (isNFT)
            {
                foreach (KeyValuePair<Type, ISharedComponent> kvp in rootEntity.sharedComponents)
                {
                    if (kvp.Value.GetClassId() == (int) CLASS_ID.NFT_SHAPE)
                    {
                        BuilderInWorldNFTController.i.StopUsingNFT(((NFTShape.Model) kvp.Value.GetModel()).assetId);
                        break;
                    }
                }
            }

            DCL.Environment.i.world.sceneBoundsChecker?.EvaluateEntityPosition(rootEntity);
            DCL.Environment.i.world.sceneBoundsChecker?.RemoveEntityToBeChecked(rootEntity);
        }

        DestroyColliders();

        associatedCatalogItem = null;
    }

    public void DestroyColliders()
    {
        foreach (List<GameObject> entityColliderGameObject in collidersGameObjectDictionary.Values)
        {
            for (int i = entityColliderGameObject.Count - 1; i > 0; i--)
            {
                Destroy(entityColliderGameObject[i]);
            }
        }

        collidersGameObjectDictionary.Clear();
    }

    #region Components

    #region Transfrom

    public void AddRotation(Vector3 newRotation) { currentRotation += newRotation; }

    public void SetRotation(Vector3 newRotation) { currentRotation = newRotation; }

    public Vector3 GetEulerRotation() { return currentRotation; }

    public void InitRotation()
    {
        //TODO : We need to implement the initial rotation from the transform component instead of getting the current rotation
        currentRotation = rootEntity.gameObject.transform.eulerAngles;
    }

    #endregion

    #region SmartItem

    public bool HasSmartItemComponent()
    {
        if (rootEntity == null)
            return false;

        return rootEntity.components.ContainsKey(CLASS_ID_COMPONENT.SMART_ITEM);
    }

    public bool HasSmartItemActions() { return GetCatalogItemAssociated().HasActions(); }

    public SmartItemParameter[] GetSmartItemParameters() { return GetCatalogItemAssociated().parameters; }

    public SmartItemAction[] GetSmartItemActions() { return GetCatalogItemAssociated().actions; }

    #endregion

    #region Locked

    public bool GetIsLockedValue()
    {
        foreach (KeyValuePair<Type, ISharedComponent> kvp in rootEntity.sharedComponents)
        {
            if (kvp.Value.GetClassId() == (int) CLASS_ID.LOCKED_ON_EDIT)
            {
                return ((DCLLockedOnEdit.Model) kvp.Value.GetModel()).isLocked;
            }
        }

        return isFloor;
    }

    public void SetIsLockedValue(bool isLocked)
    {
        bool foundComponent = false;

        foreach (KeyValuePair<Type, ISharedComponent> kvp in rootEntity.sharedComponents)
        {
            if (kvp.Value.GetClassId() == (int) CLASS_ID.LOCKED_ON_EDIT)
            {
                ((DCLLockedOnEdit) kvp.Value).SetIsLocked(isLocked);
                foundComponent = true;
            }
        }

        if (!foundComponent)
        {
            ParcelScene scene = rootEntity.scene as ParcelScene;
            DCLLockedOnEdit entityLocked = (DCLLockedOnEdit) scene.SharedComponentCreate(Guid.NewGuid().ToString(), Convert.ToInt32(CLASS_ID.LOCKED_ON_EDIT));
            entityLocked.SetIsLocked(isLocked);
            scene.SharedComponentAttach(rootEntity.entityId, entityLocked.id);
        }
    }

    #endregion

    #region DescriptiveName

    public void SetDescriptiveName(string newName)
    {
        if (rootEntity.TryGetSharedComponent(CLASS_ID.NAME, out ISharedComponent nameComponent))
        {
            ((DCLName) nameComponent).SetNewName(newName);
        }
        else
        {
            ParcelScene scene = rootEntity.scene as ParcelScene;
            DCLName name = (DCLName) scene.SharedComponentCreate(Guid.NewGuid().ToString(), Convert.ToInt32(CLASS_ID.NAME));
            name.SetNewName(newName);
            scene.SharedComponentAttach(rootEntity.entityId, name.id);
        }

        OnStatusUpdate?.Invoke(this);
    }

    public string GetDescriptiveName()
    {
        if (rootEntity != null && rootEntity.TryGetSharedComponent(CLASS_ID.NAME, out ISharedComponent nameComponent))
        {
            return ((DCLName.Model) nameComponent.GetModel()).value;
        }

        return "";
    }

    #endregion

    #endregion

    public void ResetTransfrom()
    {
        currentRotation = Vector3.zero;
        rootEntity.gameObject.transform.eulerAngles = currentRotation;

        OnStatusUpdate?.Invoke(this);
    }

    void ShapeInit()
    {
        isShapeComponentSet = true;

        isFloor = IsEntityAFloor();
        isNFT = IsEntityNFT();

        CreateCollidersForEntity(rootEntity);

        if (isFloor)
            IsLocked = true;

        if (IsEntityAVoxel())
            SetEntityAsVoxel();

        HandleAnimation();

        if (isNFT)
        {
            foreach (KeyValuePair<Type, ISharedComponent> keyValuePairBaseDisposable in rootEntity.sharedComponents)
            {
                if (keyValuePairBaseDisposable.Value.GetClassId() == (int) CLASS_ID.NFT_SHAPE)
                {
                    BuilderInWorldNFTController.i.UseNFT(((NFTShape.Model) keyValuePairBaseDisposable.Value.GetModel()).assetId);
                    break;
                }
            }
        }

        SaveOriginalMaterial();

        DCL.Environment.i.world.sceneBoundsChecker.AddPersistent(rootEntity);
        SetEntityBoundariesError(DCL.Environment.i.world.sceneBoundsChecker.IsEntityInsideSceneBoundaries(rootEntity));
    }

    private void HandleAnimation()
    {
        // We don't want animation to be running on editor
        meshAnimations = rootEntity.gameObject.GetComponentsInChildren<Animation>();
        if (HasSmartItemComponent())
        {
            DefaultAnimationStop();
        }
        else
        {
            DefaultAnimationSample(0);
        }
    }

    private void DefaultAnimationStop()
    {
        if (meshAnimations == null)
            return;

        for (int i = 0; i < meshAnimations.Length; i++)
        {
            meshAnimations[i].Stop();
        }
    }

    private void DefaultAnimationSample(float time)
    {
        if (meshAnimations == null || meshAnimations.Length == 0)
            return;

        for (int i = 0; i < meshAnimations.Length; i++)
        {
            meshAnimations[i].Stop();
            meshAnimations[i].clip?.SampleAnimation(meshAnimations[i].gameObject, time);
        }
    }

    void SetOriginalMaterials()
    {
        if (rootEntity.meshesInfo.renderers == null)
            return;
        if (isNFT)
            return;

        int matCont = 0;
        foreach (Renderer renderer in rootEntity.meshesInfo.renderers)
        {
            if (renderer == null)
                continue;
            Material[] materials = new Material[renderer.sharedMaterials.Length];

            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (isNFT && matCont == 0)
                {
                    materials[i] = renderer.sharedMaterials[i];
                    matCont++;
                    continue;
                }

                materials[i] = originalMaterials[matCont];
                matCont++;
            }

            renderer.sharedMaterials = materials;
        }
    }

    void SetEntityAsVoxel()
    {
        isVoxel = true;
        gameObject.tag = BIWSettings.VOXEL_TAG;
    }

    void SaveOriginalMaterial()
    {
        if (rootEntity.meshesInfo == null ||
            rootEntity.meshesInfo.renderers == null ||
            rootEntity.meshesInfo.renderers.Length == 0)
            return;

        if (isNFT)
            return;

        int totalMaterials = 0;
        foreach (Renderer renderer in rootEntity.meshesInfo.renderers)
        {
            if (renderer == null)
                continue;
            totalMaterials += renderer.sharedMaterials.Length;
        }

        if (!isNFT || (isNFT && originalMaterials == null))
            originalMaterials = new Material[totalMaterials];

        int matCont = 0;
        foreach (Renderer renderer in rootEntity.meshesInfo.renderers)
        {
            if (renderer == null)
                continue;

            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (isNFT && matCont == 0)
                {
                    matCont++;
                    continue;
                }

                originalMaterials[matCont] = renderer.sharedMaterials[i];
                matCont++;
            }
        }
    }

    void SetEditMaterial()
    {
        if (rootEntity.meshesInfo == null ||
            rootEntity.meshesInfo.renderers == null ||
            rootEntity.meshesInfo.renderers.Length < 1)
            return;

        if (isNFT)
            return;

        int matCont = 0;
        foreach (Renderer renderer in rootEntity.meshesInfo.renderers)
        {
            if (renderer == null)
                continue;

            Material[] materials = new Material[renderer.sharedMaterials.Length];

            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (isNFT && matCont == 0)
                {
                    materials[i] = renderer.sharedMaterials[i];
                    matCont++;
                    continue;
                }

                materials[i] = editMaterial;
                matCont++;
            }

            renderer.sharedMaterials = materials;
        }
    }

    void OnNameUpdate(object model) { OnStatusUpdate?.Invoke(this); }

    void OnShapeUpdate(IDCLEntity entity)
    {
        ShapeInit();

        if (IsSelected)
            SetEditMaterial();
    }

    void CreateCollidersForEntity(IDCLEntity entity)
    {
        MeshesInfo meshInfo = entity.meshesInfo;
        if (meshInfo == null ||
            meshInfo.currentShape == null ||
            !meshInfo.currentShape.IsVisible())
            return;

        if (collidersGameObjectDictionary.ContainsKey(entity.scene.sceneData.id + entity.entityId) && !isNFT)
            return;

        if (entity.children.Count > 0)
        {
            using (var iterator = entity.children.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    CreateCollidersForEntity(iterator.Current.Value);
                }
            }
        }

        //Note: When we are duplicating the GLTF and NFT component, their colliders are duplicated too
        //So we eliminate any previous collider to ensure that only 1 collider remain active
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.gameObject.layer ==  BIWSettings.COLLIDER_SELECTION_LAYER)
            {
                Destroy(child.gameObject);
            }
        }

        List<GameObject> colliderList = new List<GameObject>();

        for (int i = 0; i < meshInfo.renderers.Length; i++)
        {
            if (meshInfo.renderers[i] == null)
                continue;
            GameObject entityColliderChildren = new GameObject(entity.entityId);
            entityColliderChildren.layer = BIWSettings.COLLIDER_SELECTION_LAYER;

            Transform t = entityColliderChildren.transform;
            t.SetParent(meshInfo.renderers[i].transform);
            t.ResetLocalTRS();

            var meshCollider = entityColliderChildren.AddComponent<MeshCollider>();

            if (meshInfo.renderers[i] is SkinnedMeshRenderer)
            {
                Mesh meshColliderForSkinnedMesh = new Mesh();
                (meshInfo.renderers[i] as SkinnedMeshRenderer).BakeMesh(meshColliderForSkinnedMesh);
                meshCollider.sharedMesh = meshColliderForSkinnedMesh;
                t.localScale = new Vector3(1 / meshInfo.renderers[i].gameObject.transform.lossyScale.x, 1 / meshInfo.renderers[i].gameObject.transform.lossyScale.y, 1 / meshInfo.renderers[i].gameObject.transform.lossyScale.z);
            }
            else
            {
                meshCollider.sharedMesh = meshInfo.renderers[i].GetComponent<MeshFilter>().sharedMesh;
            }

            meshCollider.enabled = meshInfo.renderers[i].enabled;
            colliderList.Add(entityColliderChildren);

            if (isNFT)
            {
                if (collidersGameObjectDictionary.ContainsKey(entity.scene.sceneData.id + entity.entityId))
                    collidersGameObjectDictionary.Remove(entity.scene.sceneData.id + entity.entityId);


                collidersGameObjectDictionary.Add(entity.scene.sceneData.id + entity.entityId, colliderList);

                colliderList = new List<GameObject>();
            }
        }

        if (!isNFT)
            collidersGameObjectDictionary.Add(entity.scene.sceneData.id + entity.entityId, colliderList);
    }

    public bool IsEntityNFT()
    {
        foreach (KeyValuePair<Type, ISharedComponent> keyValuePairBaseDisposable in rootEntity.sharedComponents)
        {
            if (keyValuePairBaseDisposable.Value.GetClassId() == (int) CLASS_ID.NFT_SHAPE)
                return true;
        }

        return false;
    }

    bool IsEntityAFloor() { return GetCatalogItemAssociated()?.category == BIWSettings.FLOOR_CATEGORY; }

    bool IsEntityAVoxel()
    {
        if (rootEntity.meshesInfo?.currentShape == null)
            return false;
        if (rootEntity.meshesInfo.renderers?.Length <= 0)
            return false;
        if (rootEntity.meshesInfo.mergedBounds.size != Vector3.one)
            return false;
        return true;
    }
}