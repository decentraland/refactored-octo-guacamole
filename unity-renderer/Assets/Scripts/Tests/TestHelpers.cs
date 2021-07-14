using DCL.Components;
using DCL.Configuration;
using DCL.Controllers;
using DCL.Interface;
using DCL.Models;
using Google.Protobuf;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Color = UnityEngine.Color;
using Object = System.Object;

namespace DCL.Helpers
{
    public class WaitForAllMessagesProcessed : CustomYieldInstruction
    {
        public override bool keepWaiting => Environment.i.messaging.manager.hasPendingMessages;
    }

    // NOTE(Brian): Attribute used to determine if tests are visual. Those tests will be run to generate the baseline images.
    [AttributeUsage(AttributeTargets.Method)]
    public class VisualTestAttribute : Attribute { }

    public static class TestHelpers
    {
        public static int testSceneIteration;
        public const string testingSceneName = "DCL_Testing_";

        public static string CreateSceneMessage(string sceneId, string tag, string method, string payload) { return $"{sceneId}\t{method}\t{payload}\t{tag}\n"; }

        static int entityCounter = 123;
        static int disposableIdCounter = 123;

        public static PB_Transform GetPBTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            PB_Transform pbTranf = new PB_Transform();
            pbTranf.Position = new PB_Vector3();
            pbTranf.Position.X = position.x;
            pbTranf.Position.Y = position.y;
            pbTranf.Position.Z = position.z;
            pbTranf.Rotation = new PB_Quaternion();
            pbTranf.Rotation.X = rotation.x;
            pbTranf.Rotation.Y = rotation.y;
            pbTranf.Rotation.Z = rotation.z;
            pbTranf.Rotation.W = rotation.w;
            pbTranf.Scale = new PB_Vector3();
            pbTranf.Scale.X = scale.x;
            pbTranf.Scale.Y = scale.y;
            pbTranf.Scale.Z = scale.z;
            return pbTranf;
        }

        public static PB_Transform GetPBTransformFromModelJson(string json)
        {
            DCLTransform.Model transfModel = JsonUtility.FromJson<DCLTransform.Model>(json);
            PB_Transform pbTranf = GetPBTransform(transfModel.position, transfModel.rotation, transfModel.scale);
            return pbTranf;
        }

        public static IDCLEntity CreateSceneEntity(ParcelScene scene)
        {
            Assert.IsNotNull(scene, "Can't create entity for null scene!");

            entityCounter++;
            string id = $"{entityCounter}";
            return scene.CreateEntity(id);
        }

        public static IDCLEntity CreateSceneEntity(ParcelScene scene, string id) { return scene.CreateEntity(id); }

        public static void RemoveSceneEntity(ParcelScene scene, string id) { scene.RemoveEntity(id); }

        public static void RemoveSceneEntity(ParcelScene scene, IDCLEntity entity) { scene.RemoveEntity(entity.entityId); }

        public static T EntityComponentCreate<T, K>(ParcelScene scene, IDCLEntity entity, K model,
            CLASS_ID_COMPONENT classId = CLASS_ID_COMPONENT.NONE)
            where T : BaseComponent
            where K : new()
        {
            var factory = Environment.i.world.componentFactory as RuntimeComponentFactory;
            IPoolableComponentFactory poolableFactory = factory.poolableComponentFactory;
            int inferredId = (int) poolableFactory.GetIdForType<T>();

            int componentClassId = classId == CLASS_ID_COMPONENT.NONE
                ? (int) inferredId
                : (int) classId;

            string data;

            if (classId == CLASS_ID_COMPONENT.TRANSFORM)
            {
                PB_Transform transf = GetPBTransformFromModelJson(JsonUtility.ToJson(model));
                data = System.Convert.ToBase64String(transf.ToByteArray());
            }
            else
            {
                data = JsonUtility.ToJson(model);
            }

            return scene.EntityComponentCreateOrUpdate(
                entity.entityId,
                (CLASS_ID_COMPONENT) componentClassId,
                data) as T;
        }

        public static Coroutine EntityComponentUpdate<T, K>(T component, K model = null)
            where T : BaseComponent
            where K : class, new()
        {
            if (model == null)
            {
                model = new K();
            }

            var factory = Environment.i.world.componentFactory as RuntimeComponentFactory;
            IPoolableComponentFactory poolableFactory = factory.poolableComponentFactory;
            int inferredId = (int) poolableFactory.GetIdForType<T>();

            CLASS_ID_COMPONENT classId = (CLASS_ID_COMPONENT) inferredId;

            ParcelScene scene = component.scene as ParcelScene;
            scene.EntityComponentUpdate(component.entity, classId, JsonUtility.ToJson(model));

            return component.routine;
        }

        public static void SetEntityParent(ParcelScene scene, IDCLEntity child, IDCLEntity parent) { scene.SetEntityParent(child.entityId, parent.entityId); }

        public static void SetEntityParent(ParcelScene scene, string childEntityId, string parentEntityId) { scene.SetEntityParent(childEntityId, parentEntityId); }

        public static DCLTexture CreateDCLTexture(ParcelScene scene,
            string url,
            DCLTexture.BabylonWrapMode wrapMode = DCLTexture.BabylonWrapMode.CLAMP,
            FilterMode filterMode = FilterMode.Bilinear)
        {
            return SharedComponentCreate<DCLTexture, DCLTexture.Model>
            (
                scene,
                DCL.Models.CLASS_ID.TEXTURE,
                new DCLTexture.Model
                {
                    src = url,
                    wrap = wrapMode,
                    samplingMode = filterMode,
                }
            );
        }

        public static Coroutine SharedComponentUpdate<T>(T component, BaseModel model)
            where T : BaseDisposable
        {
            ParcelScene scene = component.scene as ParcelScene;
            scene.SharedComponentUpdate(component.id, model);

            return component.routine;
        }

        public static Coroutine SharedComponentUpdate<T, K>(T component, K model = null)
            where T : ISharedComponent
            where K : class, new()
        {
            if (model == null)
            {
                model = new K();
            }

            ParcelScene scene = component.scene as ParcelScene;
            scene.SharedComponentUpdate(component.id, JsonUtility.ToJson(model));

            if (component is IDelayedComponent delayedComponent)
                return delayedComponent.routine;

            return null;
        }

        public static T SharedComponentCreate<T, K>(ParcelScene scene, CLASS_ID id, K model = null)
            where T : BaseDisposable
            where K : class, new()
        {
            if (model == null)
            {
                model = new K();
            }

            disposableIdCounter++;

            string uniqueId = GetComponentUniqueId(scene, "material", (int) id, "-shared-" + disposableIdCounter);

            T result = scene.SharedComponentCreate(uniqueId, (int) id) as T;

            Assert.IsNotNull(result, "class-id mismatch!");

            scene.SharedComponentUpdate(uniqueId, JsonUtility.ToJson(model));

            return result;
        }

        public static void SharedComponentDispose(BaseDisposable component)
        {
            ParcelScene scene = component.scene as ParcelScene;
            scene.SharedComponentDispose(component.id);
        }

        public static void SharedComponentAttach(BaseDisposable component, IDCLEntity entity)
        {
            ParcelScene scene = entity.scene as ParcelScene;
            scene.SharedComponentAttach(
                entity.entityId,
                component.id
            );
        }

        public static void SetEntityTransform(ParcelScene scene, IDCLEntity entity, DCLTransform.Model model) { SetEntityTransform(scene, entity, model.position, model.rotation, model.scale); }

        public static void SetEntityTransform(ParcelScene scene, IDCLEntity entity) { SetEntityTransform(scene, entity, Vector3.zero, Quaternion.identity, Vector3.one); }

        public static void SetEntityTransform(ParcelScene scene, IDCLEntity entity, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            PB_Transform pB_Transform = GetPBTransform(position, rotation, scale);
            scene.EntityComponentCreateOrUpdate(
                entity.entityId,
                CLASS_ID_COMPONENT.TRANSFORM,
                System.Convert.ToBase64String(pB_Transform.ToByteArray())
            );
        }

        public static TextShape InstantiateEntityWithTextShape(ParcelScene scene, Vector3 position, TextShape.Model model)
        {
            IDCLEntity entity = CreateSceneEntity(scene);
            string componentId =
                GetComponentUniqueId(scene, "textShape", (int) CLASS_ID_COMPONENT.TEXT_SHAPE, entity.entityId);

            TextShape textShape = EntityComponentCreate<TextShape, TextShape.Model>(scene, entity, model);

            SetEntityTransform(scene, entity);

            return textShape;
        }

        public static GLTFShape AttachGLTFShape(IDCLEntity entity, ParcelScene scene, Vector3 position, GLTFShape.Model model)
        {
            string componentId = GetComponentUniqueId(scene, "gltfShape", (int) CLASS_ID.GLTF_SHAPE, entity.entityId);
            GLTFShape gltfShape = SharedComponentCreate<GLTFShape, GLTFShape.Model>(scene, CLASS_ID.GLTF_SHAPE, model);

            SetEntityTransform(scene, entity, position, Quaternion.identity, Vector3.one);
            SharedComponentAttach(gltfShape, entity);
            return gltfShape;
        }

        public static GLTFShape CreateEntityWithGLTFShape(ParcelScene scene, Vector3 position, string url)
        {
            IDCLEntity entity = null;
            return CreateEntityWithGLTFShape(scene, position, new GLTFShape.Model() { src = url }, out entity);
        }

        public static GLTFShape CreateEntityWithGLTFShape(ParcelScene scene, Vector3 position, string url,
            out IDCLEntity entity)
        {
            return CreateEntityWithGLTFShape(scene, position, new GLTFShape.Model() { src = url }, out entity);
        }

        public static GLTFShape CreateEntityWithGLTFShape(ParcelScene scene, Vector3 position, GLTFShape.Model model)
        {
            IDCLEntity entity = null;
            return CreateEntityWithGLTFShape(scene, position, model, out entity);
        }

        public static GLTFShape CreateEntityWithGLTFShape(ParcelScene scene, Vector3 position, GLTFShape.Model model,
            out IDCLEntity entity)
        {
            entity = CreateSceneEntity(scene);
            GLTFShape gltfShape = AttachGLTFShape(entity, scene, position, model);
            return gltfShape;
        }

        public static BoxShape CreateEntityWithBoxShape(ParcelScene scene, Vector3 position,
            BoxShape.Model model = null)
        {
            return CreateEntityWithPrimitive<BoxShape, BoxShape.Model>(scene, position, CLASS_ID.BOX_SHAPE, model);
        }

        public static BoxShape CreateEntityWithBoxShape(ParcelScene scene, Vector3 position, bool visible) { return CreateEntityWithBoxShape(scene, position, new BoxShape.Model { visible = visible }); }

        public static SphereShape CreateEntityWithSphereShape(ParcelScene scene, Vector3 position,
            SphereShape.Model model = null)
        {
            return CreateEntityWithPrimitive<SphereShape, SphereShape.Model>(scene, position, CLASS_ID.SPHERE_SHAPE,
                model);
        }

        public static SphereShape CreateEntityWithSphereShape(ParcelScene scene, Vector3 position, bool visible) { return CreateEntityWithSphereShape(scene, position, new SphereShape.Model { visible = visible }); }

        public static PlaneShape CreateEntityWithPlaneShape(ParcelScene scene, Vector3 position,
            PlaneShape.Model model = null)
        {
            return CreateEntityWithPrimitive<PlaneShape, PlaneShape.Model>(scene, position, CLASS_ID.PLANE_SHAPE,
                model);
        }

        public static PlaneShape CreateEntityWithPlaneShape(ParcelScene scene, Vector3 position, bool visible) { return CreateEntityWithPlaneShape(scene, position, new PlaneShape.Model { visible = visible }); }

        public static CylinderShape CreateEntityWithCylinderShape(ParcelScene scene, Vector3 position,
            CylinderShape.Model model = null)
        {
            return CreateEntityWithPrimitive<CylinderShape, CylinderShape.Model>(scene, position,
                CLASS_ID.CYLINDER_SHAPE, model);
        }

        public static CylinderShape CreateEntityWithCylinderShape(ParcelScene scene, Vector3 position, bool visible) { return CreateEntityWithCylinderShape(scene, position, new CylinderShape.Model { visible = visible }); }

        public static ConeShape CreateEntityWithConeShape(ParcelScene scene, Vector3 position,
            ConeShape.Model model = null)
        {
            return CreateEntityWithPrimitive<ConeShape, ConeShape.Model>(scene, position, CLASS_ID.CONE_SHAPE, model);
        }

        public static ConeShape CreateEntityWithConeShape(ParcelScene scene, Vector3 position, bool visible) { return CreateEntityWithConeShape(scene, position, new ConeShape.Model { visible = visible }); }

        private static T CreateEntityWithPrimitive<T, K>(ParcelScene scene, Vector3 position, CLASS_ID classId,
            K model = null)
            where T : ParametrizedShape<K>
            where K : BaseShape.Model, new()
        {
            if (model == null)
            {
                model = new K();
            }

            IDCLEntity entity = CreateSceneEntity(scene);
            T shape = SharedComponentCreate<T, K>(scene, classId, model);
            SharedComponentAttach(shape, entity);
            SetEntityTransform(scene, entity, position, Quaternion.identity, Vector3.one);
            return shape;
        }

        public static BasicMaterial CreateEntityWithBasicMaterial(ParcelScene scene, BasicMaterial.Model model,
            out IDCLEntity entity)
        {
            return CreateEntityWithBasicMaterial(scene, model, Vector3.zero, out entity);
        }

        public static BasicMaterial CreateEntityWithBasicMaterial(ParcelScene scene, BasicMaterial.Model model, Vector3 position,
            out IDCLEntity entity)
        {
            InstantiateEntityWithShape<BoxShape, BoxShape.Model>(scene, DCL.Models.CLASS_ID.BOX_SHAPE, position,
                out entity);

            return AttachBasicMaterialToEntity(scene, entity, model);
        }

        public static BasicMaterial AttachBasicMaterialToEntity(ParcelScene scene, IDCLEntity entity, BasicMaterial.Model model)
        {
            BasicMaterial material = SharedComponentCreate<BasicMaterial, BasicMaterial.Model>(scene, CLASS_ID.BASIC_MATERIAL, model);
            SharedComponentAttach(material, entity);
            return material;
        }

        public static PBRMaterial AttachPBRMaterialToEntity(ParcelScene scene, IDCLEntity entity, PBRMaterial.Model model)
        {
            PBRMaterial material = SharedComponentCreate<PBRMaterial, PBRMaterial.Model>(scene, CLASS_ID.PBR_MATERIAL, model);
            SharedComponentAttach(material, entity);
            return material;
        }

        public static PBRMaterial CreateEntityWithPBRMaterial(ParcelScene scene, PBRMaterial.Model model,
            out IDCLEntity entity)
        {
            return CreateEntityWithPBRMaterial(scene, model, Vector3.zero, out entity);
        }

        public static PBRMaterial CreateEntityWithPBRMaterial(ParcelScene scene, PBRMaterial.Model model, Vector3 position,
            out IDCLEntity entity)
        {
            InstantiateEntityWithShape<BoxShape, BoxShape.Model>(scene, CLASS_ID.BOX_SHAPE, position,
                out entity);
            PBRMaterial material =
                SharedComponentCreate<PBRMaterial, PBRMaterial.Model>(scene, CLASS_ID.PBR_MATERIAL, model);
            SharedComponentAttach(material, entity);
            return material;
        }

        public static T InstantiateEntityWithShape<T, K>(ParcelScene scene, DCL.Models.CLASS_ID classId,
            Vector3 position, out IDCLEntity entity, K model = null)
            where T : BaseShape
            where K : class, new()
        {
            if (model == null)
            {
                model = new K();
            }

            entity = CreateSceneEntity(scene);
            string shapeId = "";

            shapeId = CreateAndSetShape(scene, entity.entityId, classId, JsonConvert.SerializeObject(model));

            T shape = scene.disposableComponents[shapeId] as T;

            SetEntityTransform(scene, entity, position, Quaternion.identity, Vector3.one);

            return shape;
        }

        public static void InstantiateEntityWithShape(ParcelScene scene, string entityId, DCL.Models.CLASS_ID classId,
            Vector3 position, string remoteSrc = "")
        {
            CreateSceneEntity(scene, entityId);

            if (string.IsNullOrEmpty(remoteSrc))
            {
                CreateAndSetShape(scene, entityId, classId, "{}");
            }
            else
            {
                CreateAndSetShape(scene, entityId, classId, JsonConvert.SerializeObject(new
                {
                    src = remoteSrc
                }));
            }

            SetEntityTransform(scene, scene.entities[entityId], position, Quaternion.identity, Vector3.one);
        }

        public static void DetachSharedComponent(ParcelScene scene, string fromEntityId, string sharedComponentId)
        {
            if (!scene.entities.TryGetValue(fromEntityId, out IDCLEntity entity))
            {
                return;
            }

            scene.GetSharedComponent(sharedComponentId).DetachFrom(entity);
        }

        public static void InstantiateEntityWithMaterial(ParcelScene scene, string entityId, Vector3 position,
            BasicMaterial.Model basicMaterial, string materialComponentID = "a-material")
        {
            InstantiateEntityWithShape(scene, entityId, DCL.Models.CLASS_ID.BOX_SHAPE, position);

            scene.SharedComponentCreate(
                materialComponentID,
                (int) DCL.Models.CLASS_ID.BASIC_MATERIAL
            );

            scene.SharedComponentUpdate(
                materialComponentID,
                JsonUtility.ToJson(basicMaterial));

            scene.SharedComponentAttach(
                entityId,
                materialComponentID
            );
        }

        public static void InstantiateEntityWithMaterial(ParcelScene scene, string entityId, Vector3 position,
            PBRMaterial.Model pbrMaterial, string materialComponentID = "a-material")
        {
            InstantiateEntityWithShape(scene, entityId, DCL.Models.CLASS_ID.BOX_SHAPE, position);

            scene.SharedComponentCreate(
                materialComponentID,
                (int) CLASS_ID.PBR_MATERIAL
            );

            scene.SharedComponentUpdate(
                materialComponentID,
                JsonUtility.ToJson(pbrMaterial));

            scene.SharedComponentAttach(
                entityId,
                materialComponentID
            );
        }

        public static IEnumerator CreateAudioSource(ParcelScene scene, string entityId, string audioClipId, bool playing, bool loop = true)
        {
            var audioSourceModel = new DCLAudioSource.Model()
            {
                audioClipId = audioClipId,
                playing = playing,
                volume = 1.0f,
                loop = loop,
                pitch = 1.0f
            };

            DCLAudioSource audioSource =
                TestHelpers.EntityComponentCreate<DCLAudioSource, DCLAudioSource.Model>(scene, scene.entities[entityId],
                    audioSourceModel);

            yield return audioSource.routine;
        }

        public static IEnumerator LoadAudioClip(ParcelScene scene, string audioClipId, string url, bool loop, bool loading,
            float volume, bool waitForLoading = true)
        {
            DCLAudioClip.Model model = new DCLAudioClip.Model
            {
                url = url,
                loop = loop,
                shouldTryToLoad = loading,
                volume = volume
            };

            DCLAudioClip audioClip = scene.SharedComponentCreate(
                audioClipId,
                (int) CLASS_ID.AUDIO_CLIP
            ) as DCLAudioClip;

            scene.SharedComponentUpdate(audioClipId, JsonUtility.ToJson(model));

            yield return audioClip.routine;

            Assert.IsTrue(scene.disposableComponents.ContainsKey(audioClipId), "Shared component was not created correctly!");

            if (waitForLoading)
            {
                yield return new WaitUntil(
                    () =>
                    {
                        return audioClip.loadingState != DCLAudioClip.LoadState.LOADING_IN_PROGRESS &&
                               audioClip.loadingState != DCLAudioClip.LoadState.IDLE;
                    });
            }
        }

        public static IEnumerator CreateAudioSourceWithClipForEntity(IDCLEntity entity)
        {
            yield return LoadAudioClip(entity.scene as ParcelScene,
                audioClipId: "audioClipTest",
                url: TestAssetsUtils.GetPath() + "/Audio/Train.wav",
                loop: true,
                loading: true,
                volume: 1f,
                waitForLoading: true);

            yield return CreateAudioSource(entity.scene as ParcelScene,
                entityId: entity.entityId,
                audioClipId: "audioClipTest",
                playing: true);
        }

        public static string GetComponentUniqueId(ParcelScene scene, string salt, int classId, string entityId)
        {
            string baseId = salt + "-" + (int) classId + "-" + entityId;
            string finalId = baseId;

            while (scene.GetSharedComponent(finalId) != null)
            {
                finalId = baseId + UnityEngine.Random.Range(1, 10000);
            }

            return finalId;
        }

        public static string CreateAndSetShape(ParcelScene scene, string entityId, CLASS_ID classId, string model)
        {
            string componentId = GetComponentUniqueId(scene, "shape", (int) classId, entityId);

            scene.SharedComponentCreate(
                componentId,
                (int) classId
            );

            scene.SharedComponentUpdate(
                componentId,
                model);

            scene.SharedComponentAttach(
                entityId,
                componentId
            );

            return componentId;
        }

        public static void UpdateShape(ParcelScene scene, string componentId, string model) { scene.SharedComponentUpdate(componentId, model); }

        static object GetRandomValueForType(System.Type t)
        {
            if (t == typeof(float))
            {
                return 79014.5f;
            }
            else if (t == typeof(int))
            {
                return 79014;
            }
            else if (t == typeof(string))
            {
                return "test";
            }
            else if (t == typeof(Color))
            {
                return Color.magenta;
            }
            else if (t == typeof(object))
            {
                return Activator.CreateInstance(t);
            }

            return null;
        }

        public static void CompareWithDefaultedInstance<TModel, TComponent>(TComponent component)
            where TModel : class, new()
            where TComponent : IComponent
        {
            MemberInfo modelMember = null;

            modelMember = typeof(TComponent).GetRuntimeProperties().FirstOrDefault((x) => x.Name == "model");

            if (modelMember == null)
            {
                modelMember = typeof(TComponent).GetRuntimeFields().FirstOrDefault((x) => x.Name == "model");
            }

            Assert.IsTrue(modelMember != null, "model is null!!");
            TModel defaultedModel = new TModel();

            object tmpModel = null;

            //NOTE(Brian): Get model object
            if (modelMember is FieldInfo)
            {
                tmpModel = (modelMember as FieldInfo).GetValue(component);
            }
            else if (modelMember is PropertyInfo)
            {
                tmpModel = (modelMember as PropertyInfo).GetValue(component);
            }

            TModel model = tmpModel as TModel;

            Assert.IsTrue(model != null, "Model is null, there's a type mismatch between TModel type and the actual model type");

            foreach (FieldInfo f in typeof(TModel).GetFields())
            {
                System.Type t = f.GetType();

                //NOTE(Brian): Get model field
                object defaultValue = f.GetValue(defaultedModel);
                object modelValue = f.GetValue(model);
                string fieldName = f.Name;

                //NOTE(Brian): Corner case, strings are defaulted as null, but json deserialization inits them as ""
                if (modelValue is string && string.IsNullOrEmpty(modelValue as string))
                {
                    modelValue = null;
                }

                //NOTE(Brian): Corner case, arrays are defaulted as null, but json deserialization inits them as an empty array?
                if (modelValue is Array)
                {
                    modelValue = null;
                }

                Assert.AreEqual(defaultValue, modelValue, $"Checking {fieldName} failed! Is not default value! error!");
            }
        }

        public static IEnumerator TestEntityComponentDefaultsOnUpdate<TModel, TComponent>(ParcelScene scene)
            where TComponent : BaseComponent
            where TModel : class, new()
        {
            TModel generatedModel = new TModel();

            foreach (FieldInfo f in typeof(TModel).GetFields())
            {
                System.Type t = f.FieldType;
                object valueToSet = GetRandomValueForType(t);
                f.SetValue(generatedModel, valueToSet);
            }

            IDCLEntity e = CreateSceneEntity(scene);
            TComponent component = EntityComponentCreate<TComponent, TModel>(scene, e, generatedModel);

            if (component.routine != null)
            {
                yield return component.routine;
            }

            var factory = Environment.i.world.componentFactory as RuntimeComponentFactory;
            IPoolableComponentFactory poolableFactory = factory.poolableComponentFactory;
            int id = (int) poolableFactory.GetIdForType<TComponent>();

            scene.EntityComponentUpdate(e, (CLASS_ID_COMPONENT) id, "{}");

            if (component.routine != null)
            {
                yield return component.routine;
            }

            CompareWithDefaultedInstance<TModel, TComponent>(component);
            TestHelpers.RemoveSceneEntity(scene, e.entityId);
        }

        public static IEnumerator TestAttachedSharedComponentOfSameTypeIsReplaced<TModel, TComponent>(ParcelScene scene,
            CLASS_ID classId)
            where TComponent : BaseDisposable
            where TModel : class, new()
        {
            // Create scene entity and 1st component
            IDCLEntity entity = CreateSceneEntity(scene);

            var component = SharedComponentCreate<TComponent, TModel>(scene, classId);

            if (component.routine != null)
            {
                yield return component.routine;
            }

            System.Type componentType = typeof(TComponent);

            if (component is BaseShape)
            {
                componentType = typeof(BaseShape);
            }

            // Attach 1st component to entity
            TestHelpers.SharedComponentAttach(component, entity);

            Assert.IsTrue(entity.GetSharedComponent(componentType) != null);
            Assert.AreEqual(component, entity.GetSharedComponent(componentType));

            // Assign 2nd component to same entity
            var component2 = SharedComponentCreate<TComponent, TModel>(scene, classId);

            if (component2.routine != null)
            {
                yield return component2.routine;
            }

            TestHelpers.SharedComponentAttach(component2, entity);

            Assert.IsTrue(entity.GetSharedComponent(componentType) != null);
            Assert.AreEqual(component2, entity.GetSharedComponent(componentType));
            Assert.IsFalse(component.attachedEntities.Contains(entity));
        }

        public static IEnumerator TestSharedComponentDefaultsOnUpdate<TModel, TComponent>(ParcelScene scene,
            CLASS_ID id)
            where TComponent : BaseDisposable
            where TModel : class, new()
        {
            TComponent component = TestHelpers.SharedComponentCreate<TComponent, TModel>(scene, id);

            if (component.routine != null)
            {
                yield return component.routine;
            }

            TModel generatedModel = new TModel();

            foreach (FieldInfo f in typeof(TModel).GetFields())
            {
                System.Type t = f.FieldType;
                object valueToSet = GetRandomValueForType(t);
                f.SetValue(generatedModel, valueToSet);
            }

            yield return SharedComponentUpdate(component, generatedModel);

            yield return TestHelpers.SharedComponentUpdate(component, new TModel());

            yield return component.routine;

            CompareWithDefaultedInstance<TModel, TComponent>(component);

            component.Dispose();
        }

        public static IEnumerator TestShapeCollision(BaseShape shapeComponent, BaseShape.Model shapeModel, IDCLEntity entity)
        {
            var scene = shapeComponent.scene;

            // make sure the shape is collidable first
            shapeModel.withCollisions = true;
            SharedComponentUpdate(shapeComponent, shapeModel);
            yield return shapeComponent.routine;

            // check every collider is enabled
            Assert.IsTrue(entity.meshesInfo.colliders.Count > 0);

            for (int i = 0; i < entity.meshesInfo.colliders.Count; i++)
            {
                Assert.IsTrue(entity.meshesInfo.colliders[i].enabled);
            }

            // update collision property with 'false'
            shapeModel.withCollisions = false;
            SharedComponentUpdate(shapeComponent, shapeModel);
            yield return shapeComponent.routine;

            // check colliders correct behaviour
            for (int i = 0; i < entity.meshesInfo.colliders.Count; i++)
            {
                Assert.IsFalse(entity.meshesInfo.colliders[i].enabled);
            }

            // update collision property with 'true' again
            shapeModel.withCollisions = true;
            SharedComponentUpdate(shapeComponent, shapeModel);
            yield return shapeComponent.routine;

            // check colliders correct behaviour
            for (int i = 0; i < entity.meshesInfo.colliders.Count; i++)
            {
                Assert.IsTrue(entity.meshesInfo.colliders[i].enabled);
            }
        }

        public static IEnumerator TestShapeVisibility(BaseShape shapeComponent, BaseShape.Model shapeModel, IDCLEntity entity)
        {
            // make sure the shape is visible first
            shapeModel.visible = true;
            yield return SharedComponentUpdate(shapeComponent, shapeModel);

            // check every mesh is shown by default
            Renderer[] renderers = entity.meshesInfo.renderers;

            Assert.IsTrue(renderers.Length > 0);

            for (int i = 0; i < renderers.Length; i++)
            {
                Assert.IsTrue(renderers[i].enabled);
            }

            yield return TestShapeOnPointerEventCollider(entity);

            // update visibility with 'false'
            shapeModel.visible = false;
            yield return SharedComponentUpdate(shapeComponent, shapeModel);

            // check renderers correct behaviour
            for (int i = 0; i < renderers.Length; i++)
            {
                Assert.IsFalse(renderers[i].enabled);
            }

            yield return TestShapeOnPointerEventCollider(entity);

            // update visibility with 'true'
            shapeModel.visible = true;
            yield return SharedComponentUpdate(shapeComponent, shapeModel);

            // check renderers correct behaviour
            for (int i = 0; i < renderers.Length; i++)
            {
                Assert.IsTrue(renderers[i].enabled);
            }

            yield return TestShapeOnPointerEventCollider(entity);
        }

        public static IEnumerator TestShapeOnPointerEventCollider(IDCLEntity entity)
        {
            Renderer[] renderers = entity.meshesInfo.renderers;

            Assert.IsTrue(renderers.Length > 0);

            var onClickComponentModel = new OnClick.Model()
            {
                type = OnClick.NAME,
                uuid = "onClick"
            };

            ParcelScene scene = entity.scene as ParcelScene;

            var onClickComponent = TestHelpers.EntityComponentCreate<OnClick, OnClick.Model>(scene, entity, onClickComponentModel, CLASS_ID_COMPONENT.UUID_CALLBACK);
            yield return onClickComponent.routine;

            Collider onPointerEventCollider;
            for (int i = 0; i < renderers.Length; i++)
            {
                Assert.IsTrue(renderers[i].transform.childCount > 0, "OnClick collider should exist as this mesh's child");

                onPointerEventCollider = renderers[i].transform.GetChild(0).GetComponent<Collider>();
                Assert.IsTrue(onPointerEventCollider != null);
                Assert.IsTrue(onPointerEventCollider.gameObject.layer == PhysicsLayers.onPointerEventLayer);

                // check the onClick collide enabled state is the same as the renderer's state
                Assert.IsTrue(onPointerEventCollider.enabled == renderers[i].enabled);
            }

            scene.EntityComponentRemove(
                entity.entityId,
                onClickComponent.name
            );
            yield return null;
        }

        public static IEnumerator TestUIElementAddedCorrectlyOnInvisibleParent<TComponent, TComponentModel>(ParcelScene scene, CLASS_ID classId)
            where TComponent : UIShape
            where TComponentModel : UIShape.Model, new()
        {
            UIScreenSpace parentElement = TestHelpers.SharedComponentCreate<UIScreenSpace, UIScreenSpace.Model>(scene, CLASS_ID.UI_SCREEN_SPACE_SHAPE);
            yield return parentElement.routine;

            // make canvas invisible
            yield return SharedComponentUpdate(parentElement, new UIScreenSpace.Model { visible = false });

            TComponent targetUIElement =
                SharedComponentCreate<TComponent, TComponentModel>(scene,
                    classId,
                    new TComponentModel
                    {
                        parentComponent = parentElement.id,
                        width = new UIValue(100f),
                        height = new UIValue(100f)
                    });
            yield return targetUIElement.routine;

            RectTransform uiCanvasRectTransform = parentElement.childHookRectTransform.GetComponentInParent<RectTransform>();
            Assert.AreEqual(uiCanvasRectTransform.rect.width / 2, targetUIElement.referencesContainer.layoutElementRT.anchoredPosition.x);
            Assert.AreEqual(-uiCanvasRectTransform.rect.height / 2, targetUIElement.referencesContainer.layoutElementRT.anchoredPosition.y);

            Assert.AreEqual(100f, targetUIElement.referencesContainer.layoutElementRT.rect.width);
            Assert.AreEqual(100f, targetUIElement.referencesContainer.layoutElementRT.rect.height);
        }

        public static IEnumerator TestUIClickEventPropagation(string sceneId, UIShape.Model model, RectTransform uiObject, System.Action<bool> callback)
        {
            string srcOnClick = model.onClick;
            bool srcIsPointerBlocker = model.isPointerBlocker;

            model.isPointerBlocker = true;
            model.onClick = "UUIDFakeEventId";

            yield return TestUIClickEventPropagation(sceneId, model.onClick, uiObject, callback);

            model.isPointerBlocker = srcIsPointerBlocker;
            model.onClick = srcOnClick;

            yield return null;
        }

        public static IEnumerator TestUIClickEventPropagation(string sceneId, string eventUuid, RectTransform uiObject, System.Action<bool> callback)
        {
            // We need to populate the event data with the 'pointerPressRaycast' pointing to the 'clicked' object
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            RaycastResult raycastResult = new RaycastResult();
            raycastResult.gameObject = uiObject.gameObject;
            pointerEventData.pointerPressRaycast = raycastResult;

            string targetEventType = "SceneEvent";

            var onClickEvent = new WebInterface.OnClickEvent();
            onClickEvent.uuid = eventUuid;

            var sceneEvent = new WebInterface.SceneEvent<WebInterface.OnClickEvent>();
            sceneEvent.sceneId = sceneId;
            sceneEvent.payload = onClickEvent;
            sceneEvent.eventType = "uuidEvent";
            string eventJSON = JsonUtility.ToJson(sceneEvent);

            bool eventTriggered = false;

            yield return TestHelpers.WaitForMessageFromEngine(targetEventType, eventJSON,
                () =>
                {
                    ExecuteEvents.ExecuteHierarchy(raycastResult.gameObject, pointerEventData,
                        ExecuteEvents.pointerDownHandler);
                },
                () => { eventTriggered = true; });

            yield return null;

            // Callback!
            if (callback != null)
                callback(eventTriggered);
        }

        public static IEnumerator TestUIOnPointerDownEventPropagation(string sceneId, string eventUuid, RectTransform uiObject, System.Action<bool> callback)
        {
            // We need to populate the event data with the 'pointerPressRaycast' pointing to the 'clicked' object
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            RaycastResult raycastResult = new RaycastResult();
            raycastResult.gameObject = uiObject.gameObject;
            pointerEventData.pointerPressRaycast = raycastResult;

            string targetEventType = "SceneEvent";

            var onPointerDownEvent = new WebInterface.OnPointerDownEvent();
            onPointerDownEvent.uuid = eventUuid;

            var sceneEvent = new WebInterface.SceneEvent<WebInterface.OnPointerDownEvent>();
            sceneEvent.sceneId = sceneId;
            sceneEvent.payload = onPointerDownEvent;
            sceneEvent.eventType = "uuidEvent";
            string eventJSON = JsonUtility.ToJson(sceneEvent);

            bool eventTriggered = false;

            yield return TestHelpers.WaitForMessageFromEngine(targetEventType, eventJSON,
                () =>
                {
                    ExecuteEvents.ExecuteHierarchy(raycastResult.gameObject, pointerEventData,
                        ExecuteEvents.pointerDownHandler);
                },
                () => { eventTriggered = true; });

            yield return null;

            // Callback!
            if (callback != null)
                callback(eventTriggered);
        }

        public static IEnumerator TestUIOnPointerUpEventPropagation(string sceneId, string eventUuid, RectTransform uiObject, System.Action<bool> callback)
        {
            // We need to populate the event data with the 'pointerPressRaycast' pointing to the 'clicked' object
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            RaycastResult raycastResult = new RaycastResult();
            raycastResult.gameObject = uiObject.gameObject;
            pointerEventData.pointerPressRaycast = raycastResult;

            string targetEventType = "SceneEvent";

            var onPointerUpEvent = new WebInterface.OnPointerUpEvent();
            onPointerUpEvent.uuid = eventUuid;

            var sceneEvent = new WebInterface.SceneEvent<WebInterface.OnPointerUpEvent>();
            sceneEvent.sceneId = sceneId;
            sceneEvent.payload = onPointerUpEvent;
            sceneEvent.eventType = "uuidEvent";
            string eventJSON = JsonUtility.ToJson(sceneEvent);

            bool eventTriggered = false;

            yield return TestHelpers.WaitForMessageFromEngine(targetEventType, eventJSON,
                () =>
                {
                    ExecuteEvents.ExecuteHierarchy(raycastResult.gameObject, pointerEventData,
                        ExecuteEvents.pointerDownHandler);
                },
                () => { eventTriggered = true; });

            yield return null;

            // Callback!
            if (callback != null)
                callback(eventTriggered);
        }

        // Simulates a mouse click by throwing a ray over a given object and checks
        // if that object is the first one that the ray pass through
        public static bool TestUIClick(Canvas canvas, RectTransform rectT)
        {
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);

            Vector2 imagePos = rectT.position;
            float scale = canvas.scaleFactor;
            Vector2 pos = imagePos;

            pos *= scale;
            pos.x *= ((RectTransform) canvas.transform).sizeDelta.x / Screen.width;
            pos.y *= ((RectTransform) canvas.transform).sizeDelta.y / Screen.height;

            pointerEventData.position = pos;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, results);

            // Check that there's at least one result
            if (results.Count == 0)
            {
                return false;
            }

            // Check that the clicked object is the one on the front
            return results[0].gameObject == rectT.gameObject;
        }

        public static IEnumerator UnloadAllUnityScenes()
        {
            if (SceneManager.sceneCount == 1)
                yield break;

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        public static IEnumerator WaitForMessageFromEngine(string targetMessageType, string targetMessageJSONPayload,
            System.Action OnIterationStart, System.Action OnSuccess)
        {
            string lastMessageFromEngineType = "";
            string lastMessageFromEnginePayload = "";

            bool awaitedConditionMet = false;

            Action<string, string> msgFromEngineCallback = (eventType, eventPayload) =>
            {
                lastMessageFromEngineType = eventType;
                lastMessageFromEnginePayload = eventPayload;

                if (lastMessageFromEngineType == targetMessageType &&
                    lastMessageFromEnginePayload == targetMessageJSONPayload)
                {
                    OnSuccess?.Invoke();
                    awaitedConditionMet = true;
                }
            };

            // Hook up to web interface engine message reporting
            WebInterface.OnMessageFromEngine += msgFromEngineCallback;

            yield return new DCL.WaitUntil(() =>
            {
                OnIterationStart?.Invoke();
                return awaitedConditionMet;
            }, 2f);

            WebInterface.OnMessageFromEngine -= msgFromEngineCallback;
        }

        /// <summary>
        /// This method intercepts a message being sent to kernel and calls OnSuccess
        /// When the message matches the targetMessageType
        ///
        /// CAUTION: Do not use this to assert a message is NOT being sent to kernel,
        /// the enumerator will yield until the timeout (2 seconds) have passed.
        /// </summary>
        /// <param name="targetMessageType">
        /// The target message type to be intercepted.
        /// </param>
        /// <param name="evt">
        /// Reference event useful for template inference.
        /// </param>
        /// <param name="OnIterationStart">
        /// This callback will kept being called until the message is intercepted or
        /// a timeout occurs. Useful for raising events.
        /// </param>
        /// <param name="OnMessageReceived">
        /// This func will be invoked with any matching message as raw json.
        /// If the func returns true, the execution will end.
        /// </param>
        /// <returns>IEnumerator to be yielded</returns>
        public static IEnumerator ExpectMessageToKernel<T>(
            string targetMessageType,
            T evt,
            Action OnIterationStart,
            Func<T, bool> OnMessageReceived = null)
        {
            bool messageWasReceived = false;

            void MsgFromEngineCallback(string eventType, string eventPayload)
            {
                string lastMessageFromEngineType = eventType;
                string lastMessageFromEnginePayload = eventPayload;

                if (string.IsNullOrEmpty(lastMessageFromEnginePayload))
                    return;

                if (lastMessageFromEngineType != targetMessageType)
                    return;

                if (OnMessageReceived == null)
                {
                    messageWasReceived = true;
                    return;
                }

                messageWasReceived = OnMessageReceived.Invoke(JsonUtility.FromJson<T>(lastMessageFromEnginePayload));
            }

            // Hook up to web interface engine message reporting
            WebInterface.OnMessageFromEngine += MsgFromEngineCallback;

            yield return new DCL.WaitUntil(() =>
            {
                OnIterationStart?.Invoke();
                return messageWasReceived;
            }, 2f);

            WebInterface.OnMessageFromEngine -= MsgFromEngineCallback;
        }

        public static void TestRectTransformMaxStretched(RectTransform rt)
        {
            Assert.AreEqual(Vector2.zero, rt.anchorMin,
                $"Rect transform {rt.name} isn't stretched out!. unexpected anchorMin value.");
            Assert.AreEqual(Vector2.zero, rt.offsetMin,
                $"Rect transform {rt.name} isn't stretched out!. unexpected offsetMin value.");
            Assert.AreEqual(Vector2.one, rt.anchorMax,
                $"Rect transform {rt.name} isn't stretched out!. unexpected anchorMax value.");
            Assert.AreEqual(Vector2.one, rt.offsetMax,
                $"Rect transform {rt.name} isn't stretched out!. unexpected offsetMax value.");
            Assert.AreEqual(Vector2.zero, rt.sizeDelta,
                $"Rect transform {rt.name} isn't stretched out!. unexpected sizeDelta value.");
        }

        public static void SetCharacterPosition(Vector3 newPosition) { DCLCharacterController.i.Teleport(JsonConvert.SerializeObject(newPosition)); }

        public static IEnumerator WaitForGLTFLoad(IDCLEntity entity)
        {
            LoadWrapper_GLTF wrapper = GLTFShape.GetLoaderForEntity(entity) as LoadWrapper_GLTF;
            return new WaitUntil(() => wrapper.alreadyLoaded);
        }
    }
}