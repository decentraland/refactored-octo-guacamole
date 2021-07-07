using System;
using System.Collections;
using System.Collections.Generic;
using DCL;
using UnityEngine;

public class FeatureController
{
    private GameObject builderInWorldFeaturePrefab;

    private List<Feature> activeFeatures = new List<Feature>();

    private GameObject builderInWorld;
    private KernelConfigModel currentConfig;

    public void SetBuilderInWorldPrefab(GameObject biwPrefab) { builderInWorldFeaturePrefab = biwPrefab; }

    public KernelConfigModel GetCurrentConfig() { return currentConfig; }

    public void Start()
    {
        KernelConfig.i.EnsureConfigInitialized().Then(ApplyFeaturesConfig);
        KernelConfig.i.OnChange += OnKernelConfigChanged;
    }

    // Update is called once per frame
    public void Update()
    {
        foreach (Feature feature in activeFeatures)
        {
            feature.Update();
        }
    }

    public void OnKernelConfigChanged(KernelConfigModel current, KernelConfigModel previous) { ApplyFeaturesConfig(current); }

    public void ApplyFeaturesConfig(KernelConfigModel config)
    {
        HandleBuilderInWorld(config.features.enableBuilderInWorld);
        currentConfig = config;
    }

    private void HandleBuilderInWorld(bool isActive)
    {
        if (isActive)
        {
            if (builderInWorld != null)
                return;
            builderInWorld = GameObject.Instantiate(builderInWorldFeaturePrefab);

        }
        else
        {
            if (builderInWorld != null)
                GameObject.Destroy(builderInWorld);
        }
    }

}