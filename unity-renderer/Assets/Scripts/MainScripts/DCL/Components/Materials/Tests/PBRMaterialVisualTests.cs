using System.Collections;
using System.Linq;
using DCL;
using DCL.Components;
using DCL.Helpers;
using DCL.Models;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PBRMaterialVisualTests : VisualTestsBase
{
    [UnityTest, VisualTest]
    [Explicit, Category("Explicit")]
    public IEnumerator AlphaTextureShouldWork_Generate() { yield return VisualTestHelpers.GenerateBaselineForTest(AlphaTextureShouldWork()); }

    [UnityTest, VisualTest]
    [Explicit, Category("Explicit")]
    public IEnumerator AlphaTextureShouldWork()
    {
        yield return InitVisualTestsScene("PBRMaterialVisualTests_AlphaTextureShouldWork");
        DCLTexture texture = TestHelpers.CreateDCLTexture(scene, TestAssetsUtils.GetPath() + "/Images/alphaTexture.png");
        yield return texture.routine;
        Vector3 camTarget = new Vector3(5, 2, 5);
        VisualTestHelpers.RepositionVisualTestsCamera(VisualTestController.i.camera, camTarget - new Vector3(2, -1, 2), camTarget);

        PBRMaterial matPBR = TestHelpers.CreateEntityWithPBRMaterial(scene, new PBRMaterial.Model
        {
            albedoTexture = texture.id,
            transparencyMode = 2,
            albedoColor = Color.blue
        }, camTarget, out IDCLEntity entity);
        yield return matPBR.routine;

        yield return null;

        yield return VisualTestHelpers.TakeSnapshot();
    }

    [UnityTest, VisualTest]
    [Explicit, Category("Explicit")]
    public IEnumerator StandardConfigurations_Generate() { yield return VisualTestHelpers.GenerateBaselineForTest(StandardConfigurations()); }

    [UnityTest, VisualTest]
    public IEnumerator StandardConfigurations()
    {
        yield return InitVisualTestsScene("PBRMaterialVisualTests_StandardConfigurations");

        Vector3 camTarget = new Vector3(8, 3, 8);
        VisualTestHelpers.RepositionVisualTestsCamera(VisualTestController.i.camera, new Vector3(9, 4, 17), camTarget);

        GLTFShape gltfShape = TestHelpers.CreateEntityWithGLTFShape(scene, Vector3.zero,
            new LoadableShape.Model
            {
                src = TestAssetsUtils.GetPath() + "/GLB/MaterialsScene.glb"
            }, out IDCLEntity entity);
        TestHelpers.SetEntityTransform(scene, entity, new DCLTransform.Model { position = new Vector3(0, 0, 8), rotation = Quaternion.Euler(90, 180, 0) });

        LoadWrapper loader = GLTFShape.GetLoaderForEntity(entity);
        yield return new WaitUntil(() => loader.alreadyLoaded);

        yield return VisualTestHelpers.TakeSnapshot();
    }

    [UnityTest, VisualTest]
    [Explicit, Category("Explicit")]
    public IEnumerator TransparentObjectsAndSSAO_Generate() { yield return VisualTestHelpers.GenerateBaselineForTest(TransparentObjectsAndSSAO()); }

    [UnityTest, VisualTest]
    [Explicit, Category("Explicit")] //Enable the test once we properly render opaque objects with SSAO behind transparents
    public IEnumerator TransparentObjectsAndSSAO()
    {
        yield return InitVisualTestsScene("PBRMaterialVisualTests_TransparentObjectsAndSSAO");

        VisualTestHelpers.SetSSAOActive(true);

        Vector3 camTarget = new Vector3(5, 1, 5);
        VisualTestHelpers.RepositionVisualTestsCamera(VisualTestController.i.camera, new Vector3(4.6f, 1.8f, 0.6f), camTarget);

        PlaneShape plane = TestHelpers.CreateEntityWithPlaneShape(scene, new Vector3(5, 1, 5), true);
        IDCLEntity planeEntity = plane.attachedEntities.FirstOrDefault();
        TestHelpers.SetEntityTransform(scene, planeEntity, new Vector3(5, 1, 5), Quaternion.identity, Vector3.one * 3);
        PBRMaterial planeMaterial = TestHelpers.AttachPBRMaterialToEntity(scene, planeEntity, new PBRMaterial.Model { alphaTest = 1, transparencyMode = 1, albedoColor = Vector4.one });
        yield return plane.routine;
        yield return planeMaterial.routine;

        BoxShape box1 = TestHelpers.CreateEntityWithBoxShape(scene, new Vector3(4, 1, 6), true);
        PBRMaterial box1Material = TestHelpers.AttachPBRMaterialToEntity(scene, box1.attachedEntities.FirstOrDefault(), new PBRMaterial.Model { transparencyMode = 0, albedoColor = Color.blue });
        yield return box1.routine;
        yield return box1Material.routine;

        BoxShape box2 = TestHelpers.CreateEntityWithBoxShape(scene, new Vector3(5, 1, 6.5f), true);
        PBRMaterial box2Material = TestHelpers.AttachPBRMaterialToEntity(scene, box2.attachedEntities.FirstOrDefault(), new PBRMaterial.Model { transparencyMode = 0, albedoColor = Color.red });
        yield return box2.routine;
        yield return box2Material.routine;

        yield return null;

        yield return VisualTestHelpers.TakeSnapshot();
    }

    [UnityTest, VisualTest]
    [Explicit, Category("Explicit")]
    [TestCase(1, 100, ExpectedResult = null)]
    [TestCase(0.2f, 100, ExpectedResult = null)]
    [TestCase(1f, 10, ExpectedResult = null)]
    public IEnumerator Emission_AlphaTexture_AlbedoAlpha_Generate(float alpha, float emissiveIntensity) { yield return VisualTestHelpers.GenerateBaselineForTest(Emission_AlphaTexture_AlbedoAlpha(alpha, emissiveIntensity)); }

    [UnityTest, VisualTest]
    [TestCase(1, 100, ExpectedResult = null)]
    [TestCase(0.2f, 100, ExpectedResult = null)]
    [TestCase(1f, 10, ExpectedResult = null)]
    public IEnumerator Emission_AlphaTexture_AlbedoAlpha(float alpha, float emissiveIntensity)
    {
        yield return InitVisualTestsScene($"PBRMaterialVisualTests_Emission_AlphaTexture_AlbedoAlpha_{alpha}_{emissiveIntensity}");

        VisualTestHelpers.SetSSAOActive(true);

        Vector3 camTarget = new Vector3(5, 1, 5);
        VisualTestHelpers.RepositionVisualTestsCamera(VisualTestController.i.camera, new Vector3(4.6f, 1.8f, 0.6f), camTarget);

        DCLTexture texture = TestHelpers.CreateDCLTexture(scene, TestAssetsUtils.GetPath() + "/Images/avatar.png");
        DCLTexture alphaTexture = TestHelpers.CreateDCLTexture(scene, TestAssetsUtils.GetPath() + "/Images/alphaTexture.png");
        DCLTexture emissionTexture = TestHelpers.CreateDCLTexture(scene, TestAssetsUtils.GetPath() + "/Images/Gradient A4.png");
        yield return texture.routine;
        yield return alphaTexture.routine;
        yield return emissionTexture.routine;

        PlaneShape plane = TestHelpers.CreateEntityWithPlaneShape(scene, new Vector3(5, 1, 5), true);
        IDCLEntity planeEntity = plane.attachedEntities.FirstOrDefault();
        TestHelpers.SetEntityTransform(scene, planeEntity, new Vector3(5, 1, 5), Quaternion.Euler(0, 0, 180), Vector3.one * 3);
        PBRMaterial planeMaterial = TestHelpers.AttachPBRMaterialToEntity(scene, planeEntity, new PBRMaterial.Model
        {
            albedoColor = new Color(1, 1, 1, alpha),
            transparencyMode = 2,
            albedoTexture = texture.id,
            alphaTexture = alphaTexture.id,
            emissiveColor = Color.blue,
            emissiveIntensity = emissiveIntensity,
            emissiveTexture = emissionTexture.id
        });
        yield return plane.routine;
        yield return planeMaterial.routine;

        yield return null;

        yield return VisualTestHelpers.TakeSnapshot();
    }

    [UnityTest, VisualTest]
    [Explicit, Category("Explicit")]
    public IEnumerator Same_AlbedoTexture_AlphaTexture_Generate() { yield return VisualTestHelpers.GenerateBaselineForTest(Same_AlbedoTexture_AlphaTexture()); }

    [UnityTest, VisualTest]
    public IEnumerator Same_AlbedoTexture_AlphaTexture()
    {
        yield return InitVisualTestsScene($"PBRMaterialVisualTests_Same_AlbedoTexture_AlphaTexture");

        VisualTestHelpers.SetSSAOActive(true);

        Vector3 camTarget = new Vector3(5, 1, 5);
        VisualTestHelpers.RepositionVisualTestsCamera(VisualTestController.i.camera, new Vector3(4.6f, 1.8f, 0.6f), camTarget);

        DCLTexture texture = TestHelpers.CreateDCLTexture(scene, TestAssetsUtils.GetPath() + "/Images/avatar.png");
        DCLTexture alphaTexture = TestHelpers.CreateDCLTexture(scene, TestAssetsUtils.GetPath() + "/Images/avatar.png");
        yield return texture.routine;
        yield return alphaTexture.routine;

        PlaneShape plane = TestHelpers.CreateEntityWithPlaneShape(scene, new Vector3(5, 1, 5), true);
        IDCLEntity planeEntity = plane.attachedEntities.FirstOrDefault();
        TestHelpers.SetEntityTransform(scene, planeEntity, new Vector3(5, 1, 5), Quaternion.Euler(0, 0, 180), Vector3.one * 3);
        PBRMaterial planeMaterial = TestHelpers.AttachPBRMaterialToEntity(scene, planeEntity, new PBRMaterial.Model
        {
            transparencyMode = 2,
            albedoTexture = texture.id,
            alphaTexture = alphaTexture.id,
        });

        yield return plane.routine;
        yield return planeMaterial.routine;

        yield return null;

        yield return VisualTestHelpers.TakeSnapshot();
    }

    [UnityTest, VisualTest]
    [Explicit, Category("Explicit")]
    [TestCase(1, ExpectedResult = null)]
    [TestCase(0.75f, ExpectedResult = null)]
    [TestCase(0.25f, ExpectedResult = null)]
    public IEnumerator AlbedoTexture_AlbedoAlpha_Generate(float alpha) { yield return VisualTestHelpers.GenerateBaselineForTest(AlbedoTexture_AlbedoAlpha(alpha)); }

    [UnityTest, VisualTest]
    [TestCase(1, ExpectedResult = null)]
    [TestCase(0.75f, ExpectedResult = null)]
    [TestCase(0.25f, ExpectedResult = null)]
    public IEnumerator AlbedoTexture_AlbedoAlpha(float alpha)
    {
        yield return InitVisualTestsScene($"PBRMaterialVisualTests_AlbedoTexture_AlbedoAlpha_{alpha}");

        VisualTestHelpers.SetSSAOActive(true);

        Vector3 camTarget = new Vector3(5, 1, 5);
        VisualTestHelpers.RepositionVisualTestsCamera(VisualTestController.i.camera, new Vector3(4.6f, 1.8f, 0.6f), camTarget);

        DCLTexture texture = TestHelpers.CreateDCLTexture(scene, TestAssetsUtils.GetPath() + "/Images/avatar.png");
        yield return texture.routine;

        PlaneShape plane = TestHelpers.CreateEntityWithPlaneShape(scene, new Vector3(5, 1, 5), true);
        IDCLEntity planeEntity = plane.attachedEntities.FirstOrDefault();
        TestHelpers.SetEntityTransform(scene, planeEntity, new Vector3(5, 1, 5), Quaternion.Euler(0, 0, 180), Vector3.one * 3);
        PBRMaterial planeMaterial = TestHelpers.AttachPBRMaterialToEntity(scene, planeEntity, new PBRMaterial.Model
        {
            transparencyMode = 2,
            albedoTexture = texture.id,
            albedoColor = new Color(1, 1, 1, alpha)
        });
        yield return plane.routine;
        yield return planeMaterial.routine;

        yield return null;

        yield return VisualTestHelpers.TakeSnapshot();
    }
}