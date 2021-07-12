using System.Collections;
using System.Collections.Generic;
using DCL;
using NUnit.Framework;
using UnityEngine;

public class BIWSaveControllerShould : IntegrationTestSuite_Legacy
{
    public BIWSaveController biwSaveController;
    public BuilderInWorldBridge builderInWorldBridge;

    private GameObject gameObject;

    protected override IEnumerator SetUp()
    {
        yield return base.SetUp();
        gameObject = new GameObject();

        builderInWorldBridge = InitialSceneReferences.i.builderInWorldBridge;
        biwSaveController = new BIWSaveController();
        biwSaveController.Init(BIWTestHelper.CreateMockUpReferenceController());
        biwSaveController.EnterEditMode(scene);
    }

    [Test]
    public void TestSaveActivate()
    {
        //Arrange
        biwSaveController.ResetSaveTime();

        //Act
        builderInWorldBridge.RemoveEntityOnKernel("test", scene);

        //Assert
        Assert.IsFalse(biwSaveController.CanSave());
    }

    [Test]
    public void TestCanSave()
    {
        //Act
        biwSaveController.ResetSaveTime();

        //Assert
        Assert.IsTrue(biwSaveController.CanSave());
    }

    [Test]
    public void TestCantSave()
    {
        //Act
        biwSaveController.ForceSave();

        //Assert
        Assert.IsFalse(biwSaveController.CanSave());
    }

    protected override IEnumerator TearDown()
    {
        if (gameObject != null)
            Object.Destroy(gameObject);
        yield return base.TearDown();
    }
}