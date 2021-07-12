using DCL;
using DCL.Controllers;
using DCL.Helpers;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Tests;
using UnityEngine;
using UnityEngine.TestTools;

public class BIWActionsShould : IntegrationTestSuite_Legacy
{
    private const string ENTITY_ID = "1";
    private BIWActionController biwActionController;
    private BIWEntityHandler entityHandler;
    private BIWFloorHandler biwFloorHandler;
    private BIWCreatorController biwCreatorController;

    protected override IEnumerator SetUp()
    {
        yield return base.SetUp();
        TestHelpers.CreateSceneEntity(scene, ENTITY_ID);
        biwActionController = new BIWActionController();
        entityHandler = new BIWEntityHandler();
        biwFloorHandler = new BIWFloorHandler();
        biwCreatorController = new BIWCreatorController();

        var referencesController = BIWTestHelper.CreateReferencesControllerWithGenericMocks(
            biwActionController,
            entityHandler,
            biwFloorHandler,
            biwCreatorController
        );

        biwActionController.Init(referencesController);
        entityHandler.Init(referencesController);
        biwFloorHandler.Init(referencesController);
        biwCreatorController.Init(referencesController);

        biwActionController.EnterEditMode(scene);
        entityHandler.EnterEditMode(scene);
        biwFloorHandler.EnterEditMode(scene);
        biwCreatorController.EnterEditMode(scene);
    }

    [Test]
    public void UndoRedoMoveAction()
    {
        BuildInWorldCompleteAction buildModeAction = new BuildInWorldCompleteAction();

        Vector3 oldPosition = scene.entities[ENTITY_ID].gameObject.transform.position;
        Vector3 newPosition = new Vector3(5, 5, 5);

        BuilderInWorldEntityAction entityAction = new BuilderInWorldEntityAction(ENTITY_ID);
        entityAction.oldValue = oldPosition;
        entityAction.newValue = newPosition;

        buildModeAction.CreateActionType(entityAction, BuildInWorldCompleteAction.ActionType.MOVE);

        scene.entities[ENTITY_ID].gameObject.transform.position = newPosition;
        biwActionController.AddAction(buildModeAction);

        biwActionController.TryToUndoAction();
        Assert.IsTrue(scene.entities[ENTITY_ID].gameObject.transform.position == oldPosition);

        biwActionController.TryToRedoAction();
        Assert.IsTrue(scene.entities[ENTITY_ID].gameObject.transform.position == newPosition);
    }

    [Test]
    public void UndoRedoRotateAction()
    {
        BuildInWorldCompleteAction buildModeAction = new BuildInWorldCompleteAction();

        Vector3 oldRotation = scene.entities[ENTITY_ID].gameObject.transform.rotation.eulerAngles;
        Vector3 newRotation = new Vector3(5, 5, 5);

        BuilderInWorldEntityAction entityAction = new BuilderInWorldEntityAction(ENTITY_ID);
        entityAction.oldValue = oldRotation;
        entityAction.newValue = newRotation;

        buildModeAction.CreateActionType(entityAction, BuildInWorldCompleteAction.ActionType.ROTATE);

        scene.entities[ENTITY_ID].gameObject.transform.rotation = Quaternion.Euler(newRotation);
        biwActionController.AddAction(buildModeAction);

        biwActionController.TryToUndoAction();
        Assert.IsTrue(scene.entities[ENTITY_ID].gameObject.transform.rotation.eulerAngles == oldRotation);

        biwActionController.TryToRedoAction();
        Assert.IsTrue(scene.entities[ENTITY_ID].gameObject.transform.rotation.eulerAngles == newRotation);
    }

    [Test]
    public void UndoRedoScaleAction()
    {
        BuildInWorldCompleteAction buildModeAction = new BuildInWorldCompleteAction();

        Vector3 oldScale = scene.entities[ENTITY_ID].gameObject.transform.localScale;
        Vector3 newScale = new Vector3(5, 5, 5);

        BuilderInWorldEntityAction entityAction = new BuilderInWorldEntityAction(ENTITY_ID);
        entityAction.oldValue = oldScale;
        entityAction.newValue = newScale;

        buildModeAction.CreateActionType(entityAction, BuildInWorldCompleteAction.ActionType.SCALE);

        scene.entities[ENTITY_ID].gameObject.transform.localScale = newScale;
        biwActionController.AddAction(buildModeAction);

        biwActionController.TryToUndoAction();
        Assert.IsTrue(scene.entities[ENTITY_ID].gameObject.transform.localScale == oldScale);

        biwActionController.TryToRedoAction();
        Assert.IsTrue(scene.entities[ENTITY_ID].gameObject.transform.localScale == newScale);
    }

    [Test]
    public void UndoRedoCreateDeleteActions()
    {
        biwActionController.CreateActionEntityCreated(scene.entities[ENTITY_ID]);
        biwActionController.TryToUndoAction();
        Assert.IsFalse(scene.entities.ContainsKey(ENTITY_ID));

        biwActionController.TryToRedoAction();
        Assert.IsTrue(scene.entities.ContainsKey(ENTITY_ID));

        DCLBuilderInWorldEntity biwEntity = Utils.GetOrCreateComponent<DCLBuilderInWorldEntity>(scene.entities[ENTITY_ID].gameObject);
        biwEntity.Init(scene.entities[ENTITY_ID], null);

        biwActionController.CreateActionEntityDeleted(biwEntity);
        biwActionController.TryToUndoAction();
        Assert.IsTrue(scene.entities.ContainsKey(ENTITY_ID));

        biwActionController.TryToRedoAction();
        Assert.IsFalse(scene.entities.ContainsKey(ENTITY_ID));
    }

    [Test]
    public void UndoRedoChangeFloorAction()
    {
        BIWCatalogManager.Init();

        BIWTestHelper.CreateTestCatalogLocalMultipleFloorObjects();

        CatalogItem oldFloor = DataStore.i.builderInWorld.catalogItemDict.GetValues()[0];
        CatalogItem newFloor = DataStore.i.builderInWorld.catalogItemDict.GetValues()[1];
        BuildInWorldCompleteAction buildModeAction = new BuildInWorldCompleteAction();

        biwCreatorController.EnterEditMode(scene);
        biwFloorHandler.EnterEditMode(scene);

        biwFloorHandler.CreateFloor(oldFloor);
        biwFloorHandler.ChangeFloor(newFloor);

        buildModeAction.CreateChangeFloorAction(oldFloor, newFloor);
        biwActionController.AddAction(buildModeAction);

        foreach (DCLBuilderInWorldEntity entity in entityHandler.GetAllEntitiesFromCurrentScene())
        {
            if (entity.isFloor)
            {
                Assert.AreEqual(entity.GetCatalogItemAssociated().id, newFloor.id);
                break;
            }
        }

        biwActionController.TryToUndoAction();

        foreach (DCLBuilderInWorldEntity entity in entityHandler.GetAllEntitiesFromCurrentScene())
        {
            if (entity.isFloor)
            {
                Assert.AreEqual(entity.GetCatalogItemAssociated().id, oldFloor.id);

                break;
            }
        }

        biwActionController.TryToRedoAction();

        foreach (DCLBuilderInWorldEntity entity in entityHandler.GetAllEntitiesFromCurrentScene())
        {
            if (entity.isFloor)
            {
                Assert.AreEqual(entity.GetCatalogItemAssociated().id, newFloor.id);
                break;
            }
        }
    }

    protected override IEnumerator TearDown()
    {
        BIWCatalogManager.ClearCatalog();
        BuilderInWorldNFTController.i.ClearNFTs();
        entityHandler.Dispose();
        biwActionController.Dispose();
        biwFloorHandler.Dispose();
        biwCreatorController.Dispose();
        yield return base.TearDown();
    }
}