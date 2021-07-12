using System.Collections;
using System.Collections.Generic;
using Tests;
using UnityEngine;
using Tests;
using NUnit.Framework;
using UnityEngine.TestTools;

public class FeatureControllerShould : IntegrationTestSuite
{

    [Test]
    public void TestFeatureControllerApplyConfig()
    {
        //Arrange
        FeatureController featureController = new FeatureController();
        KernelConfigModel currentConfig = new KernelConfigModel();

        //Act
        featureController.ApplyFeaturesConfig(currentConfig);

        //Assert
        Assert.AreSame(featureController.GetCurrentConfig(), currentConfig);
    }

    [Test]
    public void TestFeatureControllerConfigChange()
    {
        //Arrange
        FeatureController featureController = new FeatureController();
        KernelConfigModel currentConfig = new KernelConfigModel();
        KernelConfigModel oldConfig = new KernelConfigModel();
        featureController.ApplyFeaturesConfig(oldConfig);

        //Act
        featureController.OnKernelConfigChanged(currentConfig, oldConfig);

        //Assert
        Assert.AreSame(featureController.GetCurrentConfig(), currentConfig);
    }
}