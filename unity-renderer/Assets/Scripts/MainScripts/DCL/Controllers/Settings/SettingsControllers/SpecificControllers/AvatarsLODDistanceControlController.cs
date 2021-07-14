using UnityEngine;

namespace DCL.SettingsControls
{
    [CreateAssetMenu(menuName = "Settings/Controllers/Controls/Avatars LOD Distance", fileName = "AvatarsLODDistanceControlController")]
    public class AvatarsLODDistanceControlController : SliderSettingsControlController
    {
        public override void Initialize()
        {
            base.Initialize();

            KernelConfig.i.EnsureConfigInitialized()
                        .Then(config =>
                        {
                            Resources.Load<BooleanVariable>("ScriptableObjects/AvatarLODsDisabled").Set(!config.features.enableAvatarLODs);
                        });

            UpdateSetting(currentGeneralSettings.avatarsLODDistance);
        }

        public override object GetStoredValue() { return currentGeneralSettings.avatarsLODDistance; }

        public override void UpdateSetting(object newValue) { DataStore.i.avatarsLOD.LODDistance.Set((float)newValue); }
    }
}