using DCL;
using DCL.Configuration;
using DCL.Controllers;
using System;
using UnityEngine;
using UnityEngine.UI;

public interface ISceneLimitsController
{
    void Initialize(ISceneLimitsView sceneLimitsView);
    void Dispose();
    void SetParcelScene(ParcelScene parcelScene);
    void ToggleSceneLimitsInfo();
    void Enable();
    void Disable();
    void UpdateInfo();
}

public class SceneLimitsController : ISceneLimitsController
{
    internal ISceneLimitsView sceneLimitsView;
    internal ParcelScene currentParcelScene;

    public void Initialize(ISceneLimitsView sceneLimitsView)
    {
        this.sceneLimitsView = sceneLimitsView;

        sceneLimitsView.OnToggleSceneLimitsInfo += ToggleSceneLimitsInfo;

        sceneLimitsView.SetUpdateCallback(UpdateInfo);
    }

    public void Dispose() { sceneLimitsView.OnToggleSceneLimitsInfo -= ToggleSceneLimitsInfo; }

    public void SetParcelScene(ParcelScene parcelScene)
    {
        currentParcelScene = parcelScene;
        UpdateInfo();
    }

    public void ToggleSceneLimitsInfo()
    {
        if (!sceneLimitsView.isBodyActived)
            Enable();
        else
            Disable();
    }

    public void Enable()
    {
        sceneLimitsView.SetBodyActive(true);
        sceneLimitsView.SetDetailsToggleAsOpen();
        UpdateInfo();
    }

    public void Disable()
    {
        sceneLimitsView.SetBodyActive(false);
        sceneLimitsView.SetDetailsToggleAsClose();
    }

    public void UpdateInfo()
    {
        if (currentParcelScene == null)
            return;

        if (IsParcelSceneSquare(currentParcelScene))
        {
            int size = (int)Math.Sqrt(currentParcelScene.sceneData.parcels.Length);
            int meters = size * 16;
            sceneLimitsView.SetTitleText($"{size}x{size} LAND <color=#959696>{meters}x{meters}m");
        }
        else
        {
            sceneLimitsView.SetTitleText(BIWSettings.CUSTOM_LAND);
        }

        SceneMetricsModel limits = currentParcelScene.metricsController.GetLimits();
        SceneMetricsModel usage = currentParcelScene.metricsController.GetModel();

        string leftDesc = AppendUsageAndLimit("ENTITIES", usage.entities, limits.entities);
        leftDesc += "\n" + AppendUsageAndLimit("BODIES", usage.bodies, limits.bodies);
        leftDesc += "\n" + AppendUsageAndLimit("TRIS", usage.triangles, limits.triangles);
        string rightDesc = AppendUsageAndLimit("TEXTURES", usage.textures, limits.textures);
        rightDesc += "\n" + AppendUsageAndLimit("MATERIALS", usage.materials, limits.materials);
        rightDesc += "\n" + AppendUsageAndLimit("GEOMETRIES", usage.meshes, limits.meshes);

        sceneLimitsView.SetLeftDescText(leftDesc);
        sceneLimitsView.SetRightDescText(rightDesc);

        SetFillInfo();
    }

    internal void SetFillInfo()
    {
        float percentAmount = GetHigherLimitPercentInfo();
        Color colorToUse = sceneLimitsView.lfColor;

        if (percentAmount > 66)
            colorToUse = sceneLimitsView.mfColor;

        if (percentAmount > 85)
            colorToUse = sceneLimitsView.hfColor;

        foreach (Image img in sceneLimitsView.limitUsageFillsImages)
        {
            if (img == null)
                continue;

            img.fillAmount = percentAmount / 100f;
            img.color = colorToUse;
        }
    }

    internal string AppendUsageAndLimit(string name, int usage, int limit)
    {
        string currentString = $"{name}:   {usage} / <color=#959696>{limit}</color>";

        if (usage > limit)
            currentString = "<color=red>" + currentString + "</color>";

        return currentString;
    }

    internal float GetHigherLimitPercentInfo()
    {
        SceneMetricsModel limits = currentParcelScene.metricsController.GetLimits();
        SceneMetricsModel usage = currentParcelScene.metricsController.GetModel();

        float percentEntities = usage.entities * 100 / limits.entities;
        float percentBodies = usage.bodies * 100 / limits.bodies;
        float percentTris = usage.triangles * 100 / limits.triangles;
        float percentTexture = usage.textures * 100 / limits.textures;
        float percentmats = usage.materials * 100 / limits.materials;
        float percentMeshes = usage.meshes * 100 / limits.meshes;

        float result = percentEntities;
        if (percentBodies > result)
            result = percentBodies;
        if (percentTris > result)
            result = percentTris;
        if (percentTexture > result)
            result = percentTexture;
        if (percentmats > result)
            result = percentmats;
        if (percentMeshes > result)
            result = percentMeshes;

        return result;
    }

    internal bool IsParcelSceneSquare(ParcelScene scene)
    {
        Vector2Int[] parcelsPoints = scene.sceneData.parcels;
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (Vector2Int vector in parcelsPoints)
        {
            if (vector.x < minX)
                minX = vector.x;
            if (vector.y < minY)
                minY = vector.y;
            if (vector.x > maxX)
                maxX = vector.x;
            if (vector.y > maxY)
                maxY = vector.y;
        }

        if (maxX - minX != maxY - minY)
            return false;

        int lateralLengh = Math.Abs((maxX - minX) + 1);

        if (parcelsPoints.Length != lateralLengh * lateralLengh)
            return false;

        return true;
    }
}