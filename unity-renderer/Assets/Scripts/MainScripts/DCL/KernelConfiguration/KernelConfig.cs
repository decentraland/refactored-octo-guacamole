using System.Collections.Generic;
using DCL;
using UnityEngine;
using DCL.Helpers;

public class KernelConfig
{
    public delegate void OnKernelConfigChanged(KernelConfigModel current, KernelConfigModel previous);

    public event OnKernelConfigChanged OnChange;

    public static KernelConfig i
    {
        get
        {
            if (config == null)
            {
                config = new KernelConfig();
            }
            return config;
        }
    }
    static KernelConfig config = null;

    KernelConfigModel value = new KernelConfigModel();
    List<Promise<KernelConfigModel>> initializationPromises;

    internal bool initialized = false;

    /// <summary>
    /// Set a new Kernel Configuration
    /// </summary>
    /// <param name="newValue">New Configuration</param>
    public void Set(KernelConfigModel newValue)
    {
        if (newValue == null)
        {
            return;
        }

        if (!initialized)
        {
            Initialize(newValue);
        }

        if (newValue.Equals(value))
            return;

        var previous = value;
        value = newValue;
        OnChange?.Invoke(value, previous);
    }

    /// <summary>
    /// Get Kernel Configuration
    /// </summary>
    /// <returns>Kernel Configuration</returns>
    public KernelConfigModel Get() { return value; }

    /// <summary>
    /// Get a promise that is resolved when KernelConfig is initialized.
    /// Usefeul when you need to fetch the configuration once and make sure it values were set and are not it defaults.
    /// i.e: EnsureConfigInitialized.Then(config => //do stuff)
    /// </summary>
    /// <returns>Promise with latest values</returns>
    public Promise<KernelConfigModel> EnsureConfigInitialized()
    {
        var newPromise = new Promise<KernelConfigModel>();
        if (initialized)
        {
            newPromise.Resolve(value);
        }
        else
        {
            if (initializationPromises == null)
            {
                initializationPromises = new List<Promise<KernelConfigModel>>();
            }
            initializationPromises.Add(newPromise);
        }
        return newPromise;
    }

    private void Initialize(KernelConfigModel newValue)
    {
        if (initializationPromises?.Count > 0)
        {
            for (int i = 0; i < initializationPromises.Count; i++)
            {
                initializationPromises[i].Resolve(newValue);
            }
            initializationPromises.Clear();
            initializationPromises = null;
        }

        initialized = true;
    }

    internal void Set(string json)
    {
        try
        {
            var newConfig = value.Clone();
            JsonUtility.FromJsonOverwrite(json, newConfig);
            Set(newConfig);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting KernelConfig {e.Message}");
        }
    }

    private KernelConfig() { }
}