using DCL.Components;
using DCL.Helpers;
using UnityEngine;

namespace DCL
{
    [RequireComponent(typeof(AvatarShape))]
    public class AvatarMovementController : MonoBehaviour, IPoolLifecycleHandler
    {
        const float SPEED_SLOW = 2.0f;
        const float SPEED_FAST = 4.0f;
        const float SPEED_ULTRA_FAST = 8.0f;
        const float SPEED_GRAVITY = 8.0f;
        const float ROTATION_SPEED = 6.25f;
        const float SPEED_EPSILON = 0.0001f;

        Transform avatarTransform
        {
            get
            {
                if (avatarTransformValue == null)
                    avatarTransformValue = GetComponent<AvatarShape>().entity.gameObject.transform;

                return avatarTransformValue;
            }
        }

        Transform avatarTransformValue;

        Vector3 currentWorldPosition = Vector3.zero;
        Vector3 targetPosition;
        Quaternion targetRotation;

        float movementSpeed = SPEED_SLOW;

        public void OnPoolGet() { }

        public void OnPoolRelease()
        {
            avatarTransformValue = null;
            currentWorldPosition = Vector3.zero;
        }

        void OnEnable() { CommonScriptableObjects.worldOffset.OnChange += OnWorldReposition; }

        void OnDisable() { CommonScriptableObjects.worldOffset.OnChange -= OnWorldReposition; }

        void OnWorldReposition(Vector3 current, Vector3 previous) { avatarTransform.position = PositionUtils.WorldToUnityPosition(currentWorldPosition); }

        public void OnTransformChanged(object model)
        {
            DCLTransform.Model transformModel = (DCLTransform.Model)model;

            MoveTo(
                transformModel.position - Vector3.up * DCLCharacterController.i.characterController.height / 2, // To fix the "always flying" avatars bug, We report the chara's centered position but the body hast its pivot at its feet
                transformModel.rotation);
        }

        public void MoveTo(Vector3 position, Quaternion rotation, bool immediate = false)
        {
            if (immediate)
            {
                currentWorldPosition = position;
                avatarTransform.position = PositionUtils.WorldToUnityPosition(currentWorldPosition);
                avatarTransform.rotation = rotation;
            }

            Vector3 flatEulerRotation = rotation.eulerAngles;
            flatEulerRotation.z = flatEulerRotation.x = 0;
            rotation = Quaternion.Euler(flatEulerRotation);

            targetPosition = position;
            targetRotation = rotation;
            moveIsDirty = true;

            float distance = Vector3.Distance(targetPosition, currentWorldPosition);

            //NOTE(Brian): More distance to goal = faster movement.
            if (distance >= 50)
            {
                this.movementSpeed = float.MaxValue;
            }
            else if (distance >= 3)
            {
                this.movementSpeed = Mathf.Lerp(SPEED_SLOW, SPEED_ULTRA_FAST, (distance - 3) / 10.0f);
            }
            else
            {
                this.movementSpeed = SPEED_SLOW;
            }
        }

        private bool moveIsDirty;

        void UpdateLerp(float deltaTime)
        {
            if ( !moveIsDirty )
            {
                UpdateRotation(deltaTime, targetRotation);
                return;
            }

            moveIsDirty = Vector3.SqrMagnitude(currentWorldPosition - targetPosition) >= SPEED_EPSILON;

            //NOTE(Brian): When we update movement we don't update rotation, because the Avatar will face the movement forward vector.
            UpdateMovement(deltaTime);
        }

        private void UpdateRotation(float deltaTime, Quaternion targetRotation)
        {
            Quaternion rot = avatarTransformValue.rotation;
            rot = Quaternion.Slerp(rot, targetRotation, ROTATION_SPEED * deltaTime);
            avatarTransformValue.rotation = rot;
        }

        private void UpdateMovement(float deltaTime)
        {
            Vector3 flattenedDiff = targetPosition - currentWorldPosition;
            flattenedDiff.y = 0;

            //NOTE(Brian): Avoid Unity error when computing look rotation for 0 magnitude vectors.
            //             Note that this isn't the same as the previous distance check because this
            //             is computed with a flattened vector.
            if (flattenedDiff != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(flattenedDiff, Vector3.up);
                UpdateRotation(deltaTime, lookRotation);
            }

            Vector3 direction = (targetPosition - currentWorldPosition).normalized;
            Vector3 delta = direction * (movementSpeed * deltaTime);

            //NOTE(Brian): We need a separate value for Y movement because the gravity has to be lerped faster.
            delta.y = direction.y * SPEED_GRAVITY * deltaTime;

            //NOTE(Brian): If we overshoot targetPosition we adjust the delta value accordingly.
            if (delta.sqrMagnitude > Vector3.SqrMagnitude(targetPosition - currentWorldPosition))
            {
                delta = targetPosition - currentWorldPosition;
            }

            currentWorldPosition += delta;
        }

        void Update()
        {
            if (avatarTransformValue == null)
                return;

            UpdateLerp(Time.deltaTime);
        }
    }
}