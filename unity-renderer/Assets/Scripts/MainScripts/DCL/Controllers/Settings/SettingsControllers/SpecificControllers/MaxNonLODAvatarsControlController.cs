using UnityEngine;

namespace DCL.SettingsControls
{
    [CreateAssetMenu(menuName = "Settings/Controllers/Controls/Max Non-LOD Avatars", fileName = "MaxNonLODAvatarsControlController")]
    public class MaxNonLODAvatarsControlController : SliderSettingsControlController
    {
        public override void Initialize()
        {
            base.Initialize();

            KernelConfig.i.EnsureConfigInitialized()
                        .Then(config =>
                        {
                            Resources.Load<BooleanVariable>("ScriptableObjects/AvatarLODsDisabled").Set(!config.features.enableAvatarLODs);
                        });

            UpdateSetting(currentGeneralSettings.maxNonLODAvatars);
        }

        public override object GetStoredValue() { return currentGeneralSettings.maxNonLODAvatars; }

        public override void UpdateSetting(object newValue) { DataStore.i.avatarsLOD.maxNonLODAvatars.Set((int)((float)newValue)); }
    }
}