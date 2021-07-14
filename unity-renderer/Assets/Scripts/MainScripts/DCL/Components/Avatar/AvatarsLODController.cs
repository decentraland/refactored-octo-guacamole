using System.Collections.Generic;
using UnityEngine;

namespace DCL
{
    public class AvatarsLODController : IAvatarsLODController
    {
        private const float LODS_LOCAL_Y_POS = 1.8f;
        private const float LODS_VERTICAL_MOVEMENT = 0.1f;
        private const float LODS_VERTICAL_MOVEMENT_DELAY = 1f;

        private List<IAvatarRenderer> avatarsList = new List<IAvatarRenderer>();
        private float lastLODsVerticalMovementTime = -1;

        public AvatarsLODController()
        {
            KernelConfig.i.EnsureConfigInitialized()
                        .Then(config =>
                        {
                            DataStore.i.avatarsLOD.LODEnabled.Set(config.features.enableAvatarLODs);
                        });
        }

        public void Update()
        {
            if (!DataStore.i.avatarsLOD.LODEnabled.Get())
                return;

            UpdateAllLODs();

            UpdateLODsVerticalMovementAndBillboard();
        }

        public void RegisterAvatar(IAvatarRenderer newAvatar)
        {
            if (!DataStore.i.avatarsLOD.LODEnabled.Get() || avatarsList.Contains(newAvatar))
                return;

            avatarsList.Add(newAvatar);
        }

        public void RemoveAvatar(IAvatarRenderer targetAvatar)
        {
            if (!DataStore.i.avatarsLOD.LODEnabled.Get() || !avatarsList.Contains(targetAvatar))
                return;

            int listCount = avatarsList.Count;
            for (int i = 0; i < listCount; i++)
            {
                if (avatarsList[i] == targetAvatar)
                {
                    avatarsList.RemoveAt(i);

                    return;
                }
            }
        }

        private void UpdateLODsVerticalMovementAndBillboard()
        {
            int listCount = avatarsList.Count;
            bool applyVerticalMovement = Time.timeSinceLevelLoad - lastLODsVerticalMovementTime > LODS_VERTICAL_MOVEMENT_DELAY;
            GameObject lodGO;
            for (int i = 0; i < listCount; i++)
            {
                lodGO = avatarsList[i].GetLODRenderer().gameObject;
                if (!lodGO.activeSelf)
                    continue;

                if (applyVerticalMovement)
                    lodGO.transform.localPosition = new Vector3(lodGO.transform.localPosition.x, (lodGO.transform.localPosition.y > LODS_LOCAL_Y_POS ? LODS_LOCAL_Y_POS : LODS_LOCAL_Y_POS + LODS_VERTICAL_MOVEMENT), lodGO.transform.localPosition.z);

                Vector3 previousForward = lodGO.transform.forward;
                Vector3 lookAtDir = (lodGO.transform.position - CommonScriptableObjects.cameraPosition).normalized;

                lookAtDir.y = previousForward.y;

                lodGO.transform.forward = lookAtDir;
            }

            if (applyVerticalMovement)
                lastLODsVerticalMovementTime = Time.timeSinceLevelLoad;
        }

        public void Dispose() { }

        private void UpdateAllLODs()
        {
            if (!DataStore.i.avatarsLOD.LODEnabled.Get())
                return;

            SortedList<float, IAvatarRenderer> closeDistanceAvatars = new SortedList<float, IAvatarRenderer>();
            foreach (IAvatarRenderer avatar in avatarsList)
            {
                float distanceToPlayer = Vector3.Distance(CommonScriptableObjects.playerUnityPosition.Get(), avatar.GetTransform().position);
                bool isInLODDistance = distanceToPlayer >= DataStore.i.avatarsLOD.LODDistance.Get();

                if (isInLODDistance)
                    ToggleLOD(avatar, true);
                else
                    closeDistanceAvatars.Add(distanceToPlayer, avatar);
            }

            int closeDistanceAvatarsCount = closeDistanceAvatars.Count;
            IAvatarRenderer currentAvatar;
            for (var i = 0; i < closeDistanceAvatarsCount; i++)
            {
                currentAvatar = closeDistanceAvatars.Values[i];
                bool isLOD = i >= DataStore.i.avatarsLOD.maxNonLODAvatars.Get();

                ToggleLOD(currentAvatar, isLOD);
            }
        }

        private void ToggleLOD(IAvatarRenderer avatarRenderer, bool enabled)
        {
            avatarRenderer.GetLODRenderer().gameObject.SetActive(enabled);
            avatarRenderer.SetVisibility(!enabled); // TODO: Resolve coping with AvatarModifierArea regarding this toggling (issue #718)
        }
    }
}