using DCL.Helpers;
using System.Collections;
using UnityEngine;
using UnityGLTF;

public class GLTFBenchmark : MonoBehaviour
{
    public string url = "/GLB/TrunkSeparatedTextures/Trunk.glb";

    int count = 0;
    public int sampleCount = 100;
    float minTime = float.MaxValue;
    float maxTime = float.MinValue;

    private IEnumerator Start()
    {
        GLTFSceneImporter.PROFILING_ENABLED = true;
        GLTFSceneImporter.budgetPerFrameInMilliseconds = float.MaxValue;
        GLTFSceneImporter.OnPerformanceFinish += OnPerformanceFinish;
        yield return new WaitForSeconds(1.0f);
        RunTest();
    }

    private void OnPerformanceFinish(float obj)
    {
        if (count > 1)
        {
            minTime = Mathf.Min(obj, minTime);
            maxTime = Mathf.Max(obj, maxTime);
        }
    }

    GameObject lastGameObjectCreated;

    void RunTest()
    {
        if (count > sampleCount)
        {
            Debug.Log($"Url = {url} ... Min time = {minTime * 1000}... Max time = {maxTime * 1000}");
            return;
        }

        count++;
        GameObject gameObject = new GameObject("Test");
        lastGameObjectCreated = gameObject;
        GLTFComponent gltfComponent = gameObject.AddComponent<GLTFComponent>();
        gltfComponent.Initialize(DCL.Environment.i.platform.webRequest);
        GLTFComponent.Settings tmpSettings = new GLTFComponent.Settings()
        {
            useVisualFeedback = false,
            initialVisibility = true,
        };

        gltfComponent.OnFinishedLoadingAsset += RunTest;
        gltfComponent.LoadAsset(TestAssetsUtils.GetPath(), url, url, false, tmpSettings);
    }
}