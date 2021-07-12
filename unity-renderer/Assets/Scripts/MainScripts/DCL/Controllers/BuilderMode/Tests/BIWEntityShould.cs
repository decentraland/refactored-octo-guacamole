using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DCL.Components;
using DCL.Helpers;
using DCL.Models;
using NUnit.Framework;
using UnityEngine;

public class BIWEntityShould : IntegrationTestSuite_Legacy
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
    public void LockValue()
    {
        //Arrange
        bool isLocked =  true;

        //Act
        entity.SetIsLockedValue(isLocked);

        //Assert
        Assert.AreEqual(entity.IsLocked , isLocked);
    }

    [Test]
    public void DescriptiveName()
    {
        //Arrange
        string newName = "testingName";

        //Act
        entity.SetDescriptiveName(newName);

        //Assert
        Assert.AreEqual(entity.GetDescriptiveName() , newName);
    }

    [Test]
    public void SetRotation()
    {
        //Arrange
        Vector3 startRotation = Vector3.right * 180;

        //Act
        entity.SetRotation(startRotation);

        //Assert
        Assert.AreEqual(startRotation, entity.GetEulerRotation());
    }

    [Test]
    public void AddRotation()
    {
        //Arrange
        Vector3 startRotation = Vector3.right * 180;
        Vector3 addRotation = Vector3.right * 90;
        entity.SetRotation(startRotation);

        //Act
        entity.AddRotation(addRotation);

        //Assert
        Assert.AreEqual(startRotation + addRotation, entity.GetEulerRotation());
    }

    [Test]
    public void SmartItemComponent()
    {
        SmartItemComponent.Model model = new SmartItemComponent.Model();

        string testFloatKey = "TestFloat";
        float testFloat = 20f;

        string intKey = "Speed";
        int testInt = 10;

        string stringKey = "TextExample";
        string testString = "unit test example";

        string onClickKey = "OnClick";


        Dictionary<object, object> onClickDict = new Dictionary<object, object>();
        onClickDict.Add(testFloatKey, testFloat);

        model.values = new Dictionary<object, object>();
        model.values.Add(intKey, testInt);
        model.values.Add(testFloatKey, testFloat);
        model.values.Add(stringKey, testString);
        model.values.Add(onClickKey, onClickDict);

        SmartItemComponent smartItemComponent = null;

        scene.EntityComponentCreateOrUpdateWithModel(ENTITY_ID, CLASS_ID_COMPONENT.SMART_ITEM, model);

        if (scene.entities[ENTITY_ID].TryGetBaseComponent(CLASS_ID_COMPONENT.SMART_ITEM, out IEntityComponent baseComponent))
        {
            //Note (Adrian): We can't wait to set the component 1 frame in production, so we set it like production
            smartItemComponent = ((SmartItemComponent) baseComponent);
            smartItemComponent.UpdateFromModel(model);
        }
        else
        {
            Assert.Fail("Smart Component not found");
        }

        Assert.AreEqual(testInt, smartItemComponent.GetValues()[intKey]);
        Assert.AreEqual(testFloat, smartItemComponent.GetValues()[testFloatKey]);
        Assert.AreEqual(testString, smartItemComponent.GetValues()[stringKey]);

        Dictionary<object, object> onClickDictFromComponent = (Dictionary<object, object>) smartItemComponent.GetValues()[onClickKey];
        Assert.AreEqual(testFloat, onClickDictFromComponent[testFloatKey]);
    }
}