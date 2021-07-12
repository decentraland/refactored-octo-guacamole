using System;
using System.Collections;
using System.Collections.Generic;
using DCL;
using UnityEngine;

public class FeatureController
{
    private GameObject builderInWorldFeaturePrefab;

    private List<Feature> activeFeatures = new List<Feature>();

    private KernelConfigModel currentConfig;

    public void SetBuilderInWorldPrefab(GameObject biwPrefab) { builderInWorldFeaturePrefab = biwPrefab; }

    public KernelConfigModel GetCurrentConfig() { return currentConfig; }

    public void Start()
    {
        KernelConfig.i.EnsureConfigInitialized().Then(ApplyFeaturesConfig);
        KernelConfig.i.OnChange += OnKernelConfigChanged;
    }

    public void OnGUI()
    {
        foreach (Feature feature in activeFeatures)
        {
            feature.OnGUI();
        }
    }

    public void Update()
    {
        foreach (Feature feature in activeFeatures)
        {
            feature.Update();
        }
    }

    public void LateUpdate()
    {
        foreach (Feature feature in activeFeatures)
        {
            feature.LateUpdate();
        }
    }

    public void OnDestroy()
    {
        foreach (Feature feature in activeFeatures)
        {
            feature.Dispose();
        }
    }

    public void OnKernelConfigChanged(KernelConfigModel current, KernelConfigModel previous) { ApplyFeaturesConfig(current); }

    public void ApplyFeaturesConfig(KernelConfigModel config)
    {
        HandleFeature<BuilderInWorldController>(config.features.enableBuilderInWorld);
        currentConfig = config;
    }

    private void HandleFeature<T>(bool isActive) where T : Feature
    {
        if (isActive)
            InitializeFeature<T>();
        else
            RemoveFeature<T>();
    }

    private void InitializeFeature<T>() where T : Feature
    {
        for (int i = 0; i <= activeFeatures.Count; i++)
        {
            if (activeFeatures[i].GetType() == typeof(T))
                return;
        }

        Feature feature = (Feature) Activator.CreateInstance(typeof (T));
        feature.Initialize();
        activeFeatures.Add(feature);
    }

    private void RemoveFeature<T>() where T : Feature
    {
        for (int i = 0; i <= activeFeatures.Count; i++)
        {
            if (activeFeatures[i].GetType() == typeof(T))
            {
                activeFeatures[i].Dispose();
                activeFeatures.Remove(activeFeatures[i]);
            }
        }
    }

}