using System.Collections.Generic;
using System.Linq;
using DCL.Helpers;
using UnityEngine;

namespace DCL
{
    public static class AvatarMeshCombiner
    {
        private static Matrix4x4[] bindposes;

        public static GameObject Combine(SkinnedMeshRenderer bonesContainer, Transform root, System.Func<Renderer, bool> filterFunction = null)
        {
            Transform transform = root;

            var rs = root.GetComponentsInChildren<SkinnedMeshRenderer>(false);
            var tmpBones = bonesContainer.bones;

            List<CombineInstance> combineInstances = new List<CombineInstance>();
            Mesh finalMesh = new Mesh();

            Vector3 lastPos = transform.position;
            transform.position = Vector3.zero;

            List<Material> mats = new List<Material>();

            GameObject result = new GameObject("Combined Avatar");
            result.layer = root.gameObject.layer;
            result.transform.parent = null;
            result.transform.position = Vector3.zero;

            List<Mesh> bakedMeshes = new List<Mesh>();
            List<BoneWeight> boneWeights = new List<BoneWeight>();
            List<Vector3> sourceNormals = new List<Vector3>();

            SkinnedMeshRenderer bindPosesContainer = null;

            // Recipe:

            // - Re-arrange boneWeights manually
            // - Re-arrange normals manually
            //     - It seems that normals are scaled by the matrix when using CombineMeshes.
            //       This is wrong and and shouldn't happen, so we have to arrange them manually.

            // - Call BakeMesh with useScale to TRUE

            // - Doesn't matter if we have an animation or not, BUT IT HAS TO BE BAKED BEFORE THE ANIMATION STARTS
            //      - If animation already was played, we re-position all bones according to original mesh bindposes matrix
            //        matrix has to be inversed and multiplied by 0 to get position. rotation and scale can be extracted directly
            //        using Matrix4x4 methods.

            // - Set bounds using combined mesh bounds

            // - Root bone has to be correctly aligned, or the mesh is not going to be baked correctly

            for ( int ri = 0; ri < rs.Length; ri++ )
            {
                if ( rs[ri].sharedMesh == null || !rs[ri].enabled )
                    continue;

                bindPosesContainer = rs[ri];

                //Debug.Log("BindPoses name: " + bindPosesContainer.transform.parent.name);

                if ( bindposes == null )
                    bindposes = rs[ri].sharedMesh.bindposes;

                break;
            }

            if (bindPosesContainer == null)
            {
                Debug.Log("Combine failure!");
                return null;
            }

            {
                Debug.Log("Combining...");

                var bindPoses = bindPosesContainer.sharedMesh.bindposes;

                // Reset bones
                for ( int i = 0 ; i < tmpBones.Length; i++ )
                {
                    Matrix4x4 bindPose = bindPoses[i].inverse;
                    tmpBones[i].position = bindPose.MultiplyPoint3x4(Vector3.zero);
                    tmpBones[i].rotation = bindPose.rotation;

                    Vector3 bindPoseScale = bindPose.lossyScale;
                    Vector3 boneScale = tmpBones[i].lossyScale;

                    tmpBones[i].localScale = new Vector3(bindPoseScale.x / boneScale.x,
                        bindPoseScale.y / boneScale.y,
                        bindPoseScale.z / boneScale.z);
                }
            }

            for (int i = 0; i < rs.Length; i++)
            {
                var r = rs[i];

                if ( r.sharedMesh == null || !r.enabled )
                    continue;

                Mesh mesh = null;

                mesh = new Mesh();
                r.BakeMesh(mesh, true);
                bakedMeshes.Add(mesh);

                //Debug.Log("Adding weights " + r.sharedMesh.boneWeights.Length + " ... vertices = " + mesh.vertices.Length);
                //sourceNormals.AddRange(r.sharedMesh.normals);

                Transform meshTransform = r.transform;

                r.enabled = false;

                bool canMerge = true;

                if ( filterFunction != null )
                    canMerge = filterFunction.Invoke(r);

                if ( !canMerge )
                    continue;

                Transform prevParent = meshTransform.parent;

                r.transform.SetParent(null, true);

                if ( !r.sharedMaterial.name.Contains("Skin") )
                    mats.Add(r.sharedMaterial);

                var meshBoneWeights = r.sharedMesh.boneWeights;
                boneWeights.AddRange(meshBoneWeights);

                combineInstances.Add( new CombineInstance()
                {
                    mesh = mesh,
                    transform = meshTransform.localToWorldMatrix
                });

                r.transform.parent = prevParent;
            }

            transform.position = lastPos;

            finalMesh.CombineMeshes(combineInstances.ToArray(), true, true);
            //finalMesh.normals = sourceNormals.ToArray();

            foreach ( var mesh in bakedMeshes )
            {
                UnityEngine.Object.Destroy(mesh);
            }

            if (mats.Count == 0)
            {
                UnityEngine.Object.Destroy(finalMesh);
                UnityEngine.Object.Destroy(result);
                return null;
            }

            var poses = bindPosesContainer.sharedMesh.bindposes;

            List<Matrix4x4> newPoses = new List<Matrix4x4>();

            for ( int i = 0; i < poses.Length; i++ )
            {
                newPoses.Add( poses[i] );
            }

            finalMesh.bindposes = newPoses.ToArray();
            finalMesh.boneWeights = boneWeights.ToArray();
            finalMesh.Optimize();
            finalMesh.UploadMeshData(true);

            var bounds = finalMesh.bounds;
            Vector3 newCenter = bounds.center;
            newCenter.Scale(new Vector3(1, 0, 1));
            bounds.center = newCenter;

            var newSkinnedMeshRenderer = result.AddComponent<SkinnedMeshRenderer>();
            newSkinnedMeshRenderer.sharedMesh = finalMesh;
            newSkinnedMeshRenderer.bones = bonesContainer.bones;
            newSkinnedMeshRenderer.rootBone = bonesContainer.rootBone;
            newSkinnedMeshRenderer.localBounds = bonesContainer.localBounds;
            newSkinnedMeshRenderer.sharedMaterial = mats[0];
            newSkinnedMeshRenderer.quality = SkinQuality.Bone1;
            newSkinnedMeshRenderer.updateWhenOffscreen = false;
            newSkinnedMeshRenderer.skinnedMotionVectors = false;
            //result.AddComponent<MeshFilter>().sharedMesh = finalMesh;
            //result.AddComponent<MeshRenderer>().sharedMaterials = newSkinnedMeshRenderer.sharedMaterials;

            result.transform.parent = root;

            return result;
        }
    }
}