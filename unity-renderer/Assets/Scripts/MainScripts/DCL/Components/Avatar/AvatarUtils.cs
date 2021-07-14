using System;
using System.Collections.Generic;
using DCL;
using DCL.Helpers;
using UnityEngine;

public static class AvatarUtils
{
    public static int _BaseColor = Shader.PropertyToID("_BaseColor");
    public static int _EmissionColor = Shader.PropertyToID("_EmissionColor");
    public static int _BaseMap = Shader.PropertyToID("_BaseMap");
    public static int _EyesTexture = Shader.PropertyToID("_EyesTexture");
    public static int _EyeTint = Shader.PropertyToID("_EyeTint");
    public static int _IrisMask = Shader.PropertyToID("_IrisMask");
    public static int _TintMask = Shader.PropertyToID("_TintMask");

    /// <summary>
    /// This will search all the transform hierachy for sharedMaterials filtered by name, and call a map function on them.
    /// This means each material will be replaced with the function return value.
    /// </summary>
    public static void MapSharedMaterialsRecursively(Transform transformRoot,
        Func<Material, Material> mapFunction,
        string materialsContainingThisName = null)
    {
        Renderer[] renderers = transformRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            Material[] sharedMats = r.sharedMaterials;

            for (int i1 = 0; i1 < sharedMats.Length; i1++)
            {
                Material m = sharedMats[i1];

                if (m == null)
                    continue;

                bool materialNameIsCorrect = true;

                if ( !string.IsNullOrEmpty(materialsContainingThisName) )
                {
                    string materialName = m.name.ToLower();
                    materialNameIsCorrect = materialName.Contains(materialsContainingThisName.ToLower());
                }

                if (!materialNameIsCorrect)
                    continue;

                string newMatName = sharedMats[i1].name;
                Material newMat = mapFunction.Invoke(sharedMats[i1]);
                newMat.name = newMatName;
                sharedMats[i1] = newMat;
            }

            r.sharedMaterials = sharedMats;
        }
    }

    /// <summary>
    /// This will search all the transform hierachy, and change _Color on all materials containing the proper name.
    /// </summary>
    /// <param name="transformRoot">Transform where to start</param>
    /// <param name="materialsContainingThisName">name to filter in materials</param>
    /// <param name="colorToChange">color to change in the renderers</param>
    public static void SetColorInHierarchy(Transform transformRoot,
        string materialsContainingThisName,
        Color colorToChange,
        string shaderId = "_BaseColor")
    {
        int _Color = Shader.PropertyToID(shaderId);

        MapSharedMaterialsRecursively(
            transformRoot,
            (mat) =>
            {
                mat.SetColor(_Color, colorToChange);
                return mat;
            },
            materialsContainingThisName);
    }

    /// <summary>
    /// This will search all the transform hierachy for all renderers,
    /// and replace all of its materials containing the specified name by the new one.
    /// </summary>
    /// <param name="transformRoot">Transform where to start the traversal</param>
    /// <param name="replaceThemWith">material to replace them</param>
    /// <param name="materialsContainingThisName">name to filter in materials</param>
    public static List<Material> ReplaceMaterialsWithCopiesOf(Transform transformRoot,
        Material replaceThemWith,
        string materialsContainingThisName = null)
    {
        List<Material> result = new List<Material>();

        MapSharedMaterialsRecursively(
            transformRoot,
            (mat) =>
            {
                Material copy = new Material(replaceThemWith);

                Texture _MatCap = null;
                Texture _GMatCap = null;
                Texture _FMatCap = null;

                if (replaceThemWith.HasProperty(ShaderUtils.MatCap))
                    _MatCap = replaceThemWith.GetTexture(ShaderUtils.MatCap);

                if (replaceThemWith.HasProperty(ShaderUtils.GlossMatCap))
                    _GMatCap = replaceThemWith.GetTexture(ShaderUtils.GlossMatCap);

                if (replaceThemWith.HasProperty(ShaderUtils.FresnelMatCap))
                    _FMatCap = replaceThemWith.GetTexture(ShaderUtils.FresnelMatCap);

                //NOTE(Brian): This method has a bug, if the material being copied lacks a property of the source material,
                //             the source material property will get erased. It can't be added back and even the material inspector crashes.
                //             Check the comment in Lit.shader.
                copy.CopyPropertiesFromMaterial(mat);

                if (_GMatCap != null)
                    copy.SetTexture(ShaderUtils.GlossMatCap, _GMatCap);

                if (_FMatCap != null)
                    copy.SetTexture(ShaderUtils.FresnelMatCap, _FMatCap);

                if (_MatCap != null)
                    copy.SetTexture(ShaderUtils.MatCap, _MatCap);

                //copy = MaterialCachingHelper.ProcessSingleMaterial(copy);
                SRPBatchingHelper.OptimizeMaterial(copy);

                result.Add(copy);
                return copy;
            },
            materialsContainingThisName);

        return result;
    }
}