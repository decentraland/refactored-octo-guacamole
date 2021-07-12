using Builder;
using DCL.Configuration;
using DCL.Controllers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public interface  IBIWOutlinerController
{
    public void OutlineEntity(DCLBuilderInWorldEntity entity);
    public void CancelEntityOutline(DCLBuilderInWorldEntity entityToQuitOutline);
    public void OutlineEntities(List<DCLBuilderInWorldEntity> entitiesToEdit);
    public void CheckOutline();
    public void CancelUnselectedOutlines();
    public void CancelAllOutlines();
    public void SetOutlineCheckActive(bool isActive);
}

public class BIWOutlinerController : BIWController, IBIWOutlinerController
{
    private const int BUILDER_RENDERER_INDEX = 1;

    private Material cameraOutlinerMaterial;

    private IBIWEntityHandler biwEntityHandler;

    private List<DCLBuilderInWorldEntity> entitiesOutlined = new List<DCLBuilderInWorldEntity>();
    private int outlinerOptimizationCounter = 0;
    private bool isOutlineCheckActive = true;

    public override void Init(BIWReferencesController referencesController)
    {
        base.Init(referencesController);
        cameraOutlinerMaterial = referencesController.projectReferences.cameraOutlinerMaterial;

        biwEntityHandler = referencesController.biwEntityHandler;
    }

    public override void EnterEditMode(ParcelScene scene)
    {
        base.EnterEditMode(scene);
        ActivateBuilderInWorldCamera();
    }

    public override void ExitEditMode()
    {
        base.ExitEditMode();
        DeactivateBuilderInWorldCamera();
    }

    public override void Dispose()
    {
        base.Dispose();
        RemoveBuilderInWorldCamera();
    }

    public void SetOutlineCheckActive(bool isActive) { isOutlineCheckActive = isActive; }

    public void CheckOutline()
    {
        if (outlinerOptimizationCounter >= 10 && isOutlineCheckActive)
        {
            if (!BuilderInWorldUtils.IsPointerOverUIElement() && !BuilderInWorldUtils.IsPointerOverMaskElement(BIWSettings.GIZMOS_LAYER))
            {
                DCLBuilderInWorldEntity entity = biwEntityHandler.GetEntityOnPointer();
                RemoveEntitiesOutlineOutsidePointerOrUnselected();

                if (entity != null && !entity.IsSelected)
                    OutlineEntity(entity);
            }
            else
            {
                CancelUnselectedOutlines();
            }

            outlinerOptimizationCounter = 0;
        }
        else
            outlinerOptimizationCounter++;
    }

    public bool IsEntityOutlined(DCLBuilderInWorldEntity entity) { return entitiesOutlined.Contains(entity); }

    public void OutlineEntities(List<DCLBuilderInWorldEntity> entitiesToEdit)
    {
        foreach (DCLBuilderInWorldEntity entityToEdit in entitiesToEdit)
        {
            OutlineEntity(entityToEdit);
        }
    }

    public void OutlineEntity(DCLBuilderInWorldEntity entity)
    {
        if (entity.rootEntity.meshRootGameObject == null)
            return;

        if (!entity.rootEntity.meshRootGameObject && entity.rootEntity.renderers.Length <= 0)
            return;

        if (entitiesOutlined.Contains(entity))
            return;

        if (entity.IsLocked)
            return;

        entitiesOutlined.Add(entity);

        for (int i = 0; i < entity.rootEntity.meshesInfo.renderers.Length; i++)
        {
            if ( entity.rootEntity.meshesInfo.renderers[i] == null)
                continue;
            entity.rootEntity.meshesInfo.renderers[i].gameObject.layer = BIWSettings.SELECTION_LAYER;
        }
    }

    public void CancelUnselectedOutlines()
    {
        for (int i = 0; i < entitiesOutlined.Count; i++)
        {
            if (!entitiesOutlined[i].IsSelected)
            {
                CancelEntityOutline(entitiesOutlined[i]);
            }
        }
    }

    public void RemoveEntitiesOutlineOutsidePointerOrUnselected()
    {
        var entity = biwEntityHandler.GetEntityOnPointer();
        for (int i = 0; i < entitiesOutlined.Count; i++)
        {
            if (!entitiesOutlined[i].IsSelected || entity != entitiesOutlined[i])
                CancelEntityOutline(entitiesOutlined[i]);
        }
    }

    public void CancelAllOutlines()
    {
        for (int i = 0; i < entitiesOutlined.Count; i++)
        {
            CancelEntityOutline(entitiesOutlined[i]);
        }
    }

    public void CancelEntityOutline(DCLBuilderInWorldEntity entityToQuitOutline)
    {
        if (!entitiesOutlined.Contains(entityToQuitOutline))
            return;

        if (entityToQuitOutline.rootEntity.meshRootGameObject && entityToQuitOutline.rootEntity.meshesInfo.renderers.Length > 0)
        {
            for (int x = 0; x < entityToQuitOutline.rootEntity.meshesInfo.renderers.Length; x++)
            {
                if ( entityToQuitOutline.rootEntity.meshesInfo.renderers[x] == null)
                    continue;
                entityToQuitOutline.rootEntity.meshesInfo.renderers[x].gameObject.layer = BIWSettings.DEFAULT_LAYER;
            }
        }

        entitiesOutlined.Remove(entityToQuitOutline);
    }

    private void ActivateBuilderInWorldCamera()
    {
        Camera camera = Camera.main;
        DCLBuilderOutline outliner = camera.GetComponent<DCLBuilderOutline>();

        if (outliner == null)
        {
            outliner = camera.gameObject.AddComponent(typeof(DCLBuilderOutline)) as DCLBuilderOutline;
            outliner.SetOutlineMaterial(cameraOutlinerMaterial);
        }
        else
        {
            outliner.enabled = true;
        }

        outliner.Activate();

        UniversalAdditionalCameraData additionalCameraData = camera.transform.GetComponent<UniversalAdditionalCameraData>();
        additionalCameraData.SetRenderer(BUILDER_RENDERER_INDEX);
    }

    private void DeactivateBuilderInWorldCamera()
    {
        Camera camera = Camera.main;

        if (camera == null)
            return;

        DCLBuilderOutline outliner = camera.GetComponent<DCLBuilderOutline>();
        if (outliner != null)
        {
            outliner.enabled = false;
            outliner.Deactivate();
        }

        UniversalAdditionalCameraData additionalCameraData = camera.transform.GetComponent<UniversalAdditionalCameraData>();
        additionalCameraData.SetRenderer(0);
    }

    private void RemoveBuilderInWorldCamera()
    {
        Camera camera = Camera.main;

        if (camera == null)
            return;

        DCLBuilderOutline outliner = camera.GetComponent<DCLBuilderOutline>();
        outliner.Dispose();
        GameObject.Destroy(outliner);
    }
}