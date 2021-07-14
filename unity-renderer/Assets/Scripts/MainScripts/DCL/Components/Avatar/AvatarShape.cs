using System;
using DCL.Components;
using DCL.Interface;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DCL.Helpers;
using DCL.Models;
using UnityEngine;

namespace DCL
{
    public class AvatarShape : BaseComponent
    {
        private const string CURRENT_PLAYER_ID = "CurrentPlayerInfoCardId";

        public static event Action<IDCLEntity, AvatarShape> OnAvatarShapeUpdated;

        public AvatarName avatarName;
        public AvatarRenderer avatarRenderer;
        public Collider avatarCollider;
        public AvatarMovementController avatarMovementController;

        [SerializeField]
        private AvatarOnPointerDown onPointerDown;

        private StringVariable currentPlayerInfoCardId;

        private AvatarModel oldModel = new AvatarModel();

        public bool everythingIsLoaded;

        private Vector3? lastAvatarPosition = null;
        private MinimapMetadata.MinimapUserInfo avatarUserInfo = new MinimapMetadata.MinimapUserInfo();
        bool initializedPosition = false;

        private void Awake()
        {
            model = new AvatarModel();
            currentPlayerInfoCardId = Resources.Load<StringVariable>(CURRENT_PLAYER_ID);
        }

        private void PlayerClicked()
        {
            if (model == null)
                return;
            currentPlayerInfoCardId.Set(((AvatarModel) model).id);
        }

        public void OnDestroy()
        {
            Cleanup();

            if (poolableObject != null && poolableObject.isInsidePool)
                poolableObject.RemoveFromPool();
        }

        public override IEnumerator ApplyChanges(BaseModel newModel)
        {
            DisablePassport();

            var model = (AvatarModel) newModel;

            everythingIsLoaded = false;

            bool avatarDone = false;
            bool avatarFailed = false;

            yield return null; //NOTE(Brian): just in case we have a Object.Destroy waiting to be resolved.

            avatarRenderer.ApplyModel(model, () =>
            {
                if (avatarRenderer.lodRenderer != null)
                    Environment.i.platform.avatarsLODController.RegisterAvatar(avatarRenderer);

                avatarDone = true;
            }, () => avatarFailed = true);

            yield return new WaitUntil(() => avatarDone || avatarFailed);

            onPointerDown.Initialize(
                new OnPointerDown.Model()
                {
                    type = OnPointerDown.NAME,
                    button = WebInterface.ACTION_BUTTON.POINTER.ToString(),
                    hoverText = "view profile"
                },
                entity
            );

            CommonScriptableObjects.worldOffset.OnChange -= OnWorldReposition;
            CommonScriptableObjects.worldOffset.OnChange += OnWorldReposition;

            entity.OnTransformChange -= avatarMovementController.OnTransformChanged;
            entity.OnTransformChange += avatarMovementController.OnTransformChanged;

            entity.OnTransformChange -= OnEntityTransformChanged;
            entity.OnTransformChange += OnEntityTransformChanged;

            onPointerDown.OnPointerDownReport -= PlayerClicked;
            onPointerDown.OnPointerDownReport += PlayerClicked;

            // To deal with the cases in which the entity transform was configured before the AvatarShape
            if (!initializedPosition && entity.components.ContainsKey(DCL.Models.CLASS_ID_COMPONENT.TRANSFORM))
            {
                initializedPosition = true;

                avatarMovementController.MoveTo(
                    entity.gameObject.transform.localPosition - Vector3.up * DCLCharacterController.i.characterController.height / 2,
                    entity.gameObject.transform.localRotation, true);
            }

            avatarUserInfo.userId = model.id;
            avatarUserInfo.userName = model.name;
            avatarUserInfo.worldPosition = lastAvatarPosition != null ? lastAvatarPosition.Value : entity.gameObject.transform.localPosition;
            MinimapMetadataController.i?.UpdateMinimapUserInformation(avatarUserInfo);

            avatarName.SetName(model.name);
            avatarName.SetTalking(model.talking);

            avatarCollider.gameObject.SetActive(true);

            everythingIsLoaded = true;
            OnAvatarShapeUpdated?.Invoke(entity, this);

            EnablePassport();
        }

        public void DisablePassport()
        {
            if (onPointerDown.collider == null)
                return;

            onPointerDown.collider.enabled = false;
        }

        public void EnablePassport()
        {
            if (onPointerDown.collider == null)
                return;

            onPointerDown.collider.enabled = true;
        }

        private void OnWorldReposition(Vector3 current, Vector3 previous)
        {
            avatarUserInfo.worldPosition = entity.gameObject.transform.position;
            MinimapMetadataController.i?.UpdateMinimapUserInformation(avatarUserInfo);
        }

        private void OnEntityTransformChanged(object newModel)
        {
            DCLTransform.Model newTransformModel = (DCLTransform.Model)newModel;
            lastAvatarPosition = newTransformModel.position;

            var model = (AvatarModel) this.model;
            avatarUserInfo.userId = model.id;
            avatarUserInfo.userName = model.name;
            avatarUserInfo.worldPosition = newTransformModel.position;
            MinimapMetadataController.i?.UpdateMinimapUserInformation(avatarUserInfo);
        }

        public override void OnPoolGet()
        {
            base.OnPoolGet();

            everythingIsLoaded = false;
            initializedPosition = false;
            oldModel = new AvatarModel();
            model = new AvatarModel();
            lastAvatarPosition = null;
            avatarName.SetName(String.Empty);
        }

        public override void Cleanup()
        {
            base.Cleanup();

            Environment.i.platform.avatarsLODController.RemoveAvatar(avatarRenderer);

            avatarRenderer.CleanupAvatar();

            if (poolableObject != null)
            {
                poolableObject.OnRelease -= Cleanup;
            }

            onPointerDown.OnPointerDownReport -= PlayerClicked;
            CommonScriptableObjects.worldOffset.OnChange -= OnWorldReposition;

            if (entity != null)
            {
                entity.OnTransformChange = null;
                entity = null;
            }

            var model = (AvatarModel) this.model;
            if (model != null)
                avatarUserInfo.userId = model.id;
            MinimapMetadataController.i?.UpdateMinimapUserInformation(avatarUserInfo, true);
        }

        public override int GetClassId() { return (int) CLASS_ID_COMPONENT.AVATAR_SHAPE; }
    }
}