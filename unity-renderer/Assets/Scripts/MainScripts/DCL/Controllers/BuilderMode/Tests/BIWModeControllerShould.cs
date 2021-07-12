using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BIWModeControllerShould : IntegrationTestSuite_Legacy
{
    private BIWModeController biwModeController;

    protected override IEnumerator SetUp()
    {
        yield return base.SetUp();

        biwModeController = new BIWModeController();

        BIWActionController actionController = new BIWActionController();
        var referencesController = BIWTestHelper.CreateReferencesControllerWithGenericMocks(
            actionController,
            biwModeController
        );

        biwModeController.Init(referencesController);
        actionController.Init(referencesController);

        biwModeController.EnterEditMode(scene);
        actionController.EnterEditMode(scene);
    }

    [Test]
    public void SetFirstPersonMode()
    {
        //Arrange
        biwModeController.SetBuildMode(BIWModeController.EditModeState.Inactive);

        //Act
        biwModeController.SetBuildMode(BIWModeController.EditModeState.FirstPerson);

        //Assert
        Assert.IsTrue(biwModeController.GetCurrentStateMode() == BIWModeController.EditModeState.FirstPerson);
        Assert.IsTrue(biwModeController.GetCurrentMode().GetType() == typeof(BuilderInWorldFirstPersonMode));
    }

    [Test]
    public void SetGodMode()
    {
        //Arrange
        biwModeController.SetBuildMode(BIWModeController.EditModeState.Inactive);

        //Act
        biwModeController.SetBuildMode(BIWModeController.EditModeState.GodMode);

        //Assert
        Assert.IsTrue(biwModeController.GetCurrentStateMode() == BIWModeController.EditModeState.GodMode);
        Assert.IsTrue(biwModeController.GetCurrentMode().GetType() == typeof(BuilderInWorldGodMode));
    }

    [Test]
    public void InactiveMode()
    {
        //Arrange
        biwModeController.SetBuildMode(BIWModeController.EditModeState.GodMode);

        //Act
        biwModeController.SetBuildMode(BIWModeController.EditModeState.Inactive);

        //Assert
        Assert.IsTrue(biwModeController.GetCurrentStateMode() == BIWModeController.EditModeState.Inactive);
        Assert.IsTrue(biwModeController.GetCurrentMode() == null);
    }

    protected override IEnumerator TearDown()
    {
        biwModeController.Dispose();
        yield return base.TearDown();
    }
}