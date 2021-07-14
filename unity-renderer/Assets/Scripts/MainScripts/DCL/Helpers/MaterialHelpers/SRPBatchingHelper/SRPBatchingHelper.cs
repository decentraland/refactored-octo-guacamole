using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DCL.Helpers
{
    public static class SRPBatchingHelper
    {
        public static System.Action<Material> OnMaterialProcess;
        static Dictionary<int, int> crcToQueue = new Dictionary<int, int>();

        public static void OptimizeMaterial(Material material)
        {
#if !UNITY_EDITOR
            material.name = "Material";
#endif
            //NOTE(Brian): Just enable these keywords so the SRP batcher batches more stuff.
            material.EnableKeyword("_EMISSION");
            material.EnableKeyword("_NORMALMAP");

            if (!material.IsKeywordEnabled("_ALPHATEST_ON"))
                material.SetFloat(ShaderUtils.Cutoff, 0);

            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.DisableKeyword("VERTEX_COLOR_ON");

            material.enableInstancing = false;

            if (material.HasProperty(ShaderUtils.ZWrite))
            {
                int zWrite = (int) material.GetFloat(ShaderUtils.ZWrite);

                //NOTE(Brian): for transparent meshes skip further variant optimization.
                //             Transparency needs clip space z sorting to be displayed correctly.
                if (zWrite == 0)
                {
                    material.SetInt("_Surface", 1);
                    material.DisableKeyword("_SCREEN_SPACE_OCCLUSION");
                    material.SetShaderPassEnabled("DepthNormals", false);
                    material.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent;
                    OnMaterialProcess?.Invoke(material);
                    return;
                }
            }

            material.SetInt("_Surface", 0);

            int cullMode = (int) UnityEngine.Rendering.CullMode.Off;

            if (material.HasProperty(ShaderUtils.Cull))
            {
                cullMode = 2 - (int) material.GetFloat(ShaderUtils.Cull);
            }

            int baseQueue;

            if (material.renderQueue == (int) UnityEngine.Rendering.RenderQueue.AlphaTest)
                baseQueue = (int) UnityEngine.Rendering.RenderQueue.Geometry + 600;
            else
                baseQueue = (int) UnityEngine.Rendering.RenderQueue.Geometry;

            //NOTE(Brian): This guarantees grouping calls by same shader keywords. Needed to take advantage of SRP batching.
            string appendedKeywords = material.shader.name + string.Join("", material.shaderKeywords);
            int crc = Shader.PropertyToID(appendedKeywords);

            if (!crcToQueue.ContainsKey(crc))
                crcToQueue.Add(crc, crcToQueue.Count + 1);

            //NOTE(Brian): we use 0, 100, 200 to group calls by culling mode (must group them or batches will break).
            int queueOffset = (cullMode + 1) * 150;
            material.renderQueue = baseQueue + crcToQueue[crc] + queueOffset;
            OnMaterialProcess?.Invoke(material);
        }
    }
}