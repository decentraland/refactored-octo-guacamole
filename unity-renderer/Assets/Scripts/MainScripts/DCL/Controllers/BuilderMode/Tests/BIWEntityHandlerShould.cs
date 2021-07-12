using DCL.Helpers;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DCL.Models;

public class BIWEntityHandlerShould : IntegrationTestSuite_Legacy
{
    private const string ENTITY_ID = "1";
    DCLBuilderInWorldEntity entity;
    BIWEntityHandler entityHandler;

    protected override IEnumerator SetUp()
    {
        yield return base.SetUp();
        entityHandler = new BIWEntityHandler();
        entityHandler.Init(BIWTestHelper.CreateMockUpReferenceController());

        TestHelpers.CreateSceneEntity(scene, ENTITY_ID);
        entityHandler.EnterEditMode(scene);
        entity = entityHandler.GetAllEntitiesFromCurrentScene().FirstOrDefault();
    }

    [Test]
    public void EntitySelectDeselect()
    {
        Assert.IsFalse(entity.IsSelected);
        entityHandler.SelectEntity(entity);
        Assert.IsTrue(entity.IsSelected);

        Assert.AreEqual(entityHandler.GetSelectedEntityList().Count, 1);
        Assert.AreEqual(entityHandler.GetSelectedEntityList().FirstOrDefault(), entity);

        entityHandler.DeselectEntity(entity);
        Assert.IsFalse(entity.IsSelected);
    }

    [Test]
    public void EntitySelectionOperations()
    {
        DCLBuilderInWorldEntity createdEntity = entityHandler.CreateEmptyEntity(scene, Vector3.zero, Vector3.zero);

        int entityAmount = entityHandler.GetAllEntitiesFromCurrentScene().Count;
        entityHandler.SelectEntity(createdEntity);
        entityHandler.DuplicateSelectedEntities();

        Assert.Greater(entityHandler.GetAllEntitiesFromCurrentScene().Count, entityAmount);


        entityAmount = entityHandler.GetAllEntitiesFromCurrentScene().Count;
        entityHandler.DeleteSelectedEntities();

        Assert.Less(entityHandler.GetAllEntitiesFromCurrentScene().Count, entityAmount);
    }

    [Test]
    public void EntityCreationDelete()
    {
        DCLBuilderInWorldEntity createdEntity = entityHandler.CreateEmptyEntity(scene, Vector3.zero, Vector3.zero);
        Assert.IsNotNull(createdEntity);
        Assert.AreEqual(entityHandler.GetAllEntitiesFromCurrentScene().Count, 2);

        entityHandler.DeleteEntity(createdEntity.rootEntity.entityId);
        Assert.AreEqual(entityHandler.GetAllEntitiesFromCurrentScene().Count, 1);
    }

    [Test]
    public void EntityDuplicateName()
    {
        string name = "Test";

        entityHandler.SetEntityName(entity, name);
        DCLBuilderInWorldEntity createdEntity = entityHandler.CreateEmptyEntity(scene, Vector3.zero, Vector3.zero);

        entityHandler.SetEntityName(createdEntity, name);

        Assert.AreNotEqual(createdEntity.GetDescriptiveName(), name);
    }

    [Test]
    public void EntityDuplicate()
    {
        IDCLEntity duplicateEntity = entityHandler.DuplicateEntity(entity).rootEntity;
        DCLBuilderInWorldEntity convertedEntity = entityHandler.GetConvertedEntity(duplicateEntity);

        Assert.IsNotNull(convertedEntity);
    }

    protected override IEnumerator TearDown()
    {
        entityHandler.Dispose();

        yield return base.TearDown();
    }
}