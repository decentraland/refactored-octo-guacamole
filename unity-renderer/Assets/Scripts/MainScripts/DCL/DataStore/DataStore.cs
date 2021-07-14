using UnityEngine;
using Variables.RealmsInfo;

namespace DCL
{
    public enum AppMode
    {
        DEFAULT,
        BUILDER_IN_WORLD_EDITION
    }

    public class DataStore
    {
        private static DataStore instance = new DataStore();
        public static DataStore i { get => instance; }
        public static void Clear() => instance = new DataStore();

        public readonly CurrentRealmVariable playerRealm = new CurrentRealmVariable();
        public readonly RealmsVariable realmsInfo = new RealmsVariable();
        public readonly DebugConfig debugConfig = new DebugConfig();
        public readonly BaseVariable<bool> isSignUpFlow = new BaseVariable<bool>();
        public readonly BaseDictionary<string, WearableItem> wearables = new BaseDictionary<string, WearableItem>();
        public readonly BuilderInWorld builderInWorld = new BuilderInWorld();
        public readonly DataStore_Quests Quests = new DataStore_Quests();
        public readonly DataStore_HUDs HUDs = new DataStore_HUDs();
        public readonly BaseVariable<bool> isPlayerRendererLoaded = new BaseVariable<bool>();
        public readonly BaseVariable<AppMode> appMode = new BaseVariable<AppMode>();
        public readonly DataStore_Player player = new DataStore_Player();
        public readonly BaseVariable<NFTPromptModel> onOpenNFTPrompt = new BaseVariable<NFTPromptModel>();
        public readonly DataStore_AvatarsLOD avatarsLOD = new DataStore_AvatarsLOD();

        public class BuilderInWorld
        {
            public readonly BaseDictionary<string, CatalogItem> catalogItemDict = new BaseDictionary<string, CatalogItem>();
            public readonly BaseDictionary<string, CatalogItemPack> catalogItemPackDict = new BaseDictionary<string, CatalogItemPack>();
            public readonly BaseVariable<PublishSceneResultPayload> unpublishSceneResult = new BaseVariable<PublishSceneResultPayload>();
            public readonly BaseVariable<bool> showTaskBar = new BaseVariable<bool>();
        }

        public class DataStore_Quests
        {
            public readonly BaseDictionary<string, QuestModel> quests = new BaseDictionary<string, QuestModel>();
            public readonly BaseCollection<string> pinnedQuests = new BaseCollection<string>();
        }

        public class DataStore_HUDs
        {
            public readonly BaseVariable<bool> questsPanelVisible = new BaseVariable<bool>(false);
            public readonly BaseVariable<bool> builderProjectsPanelVisible = new BaseVariable<bool>(false);
            public readonly BaseVariable<bool> signupVisible = new BaseVariable<bool>(false);
            public readonly LoadingHUD loadingHUD = new LoadingHUD();

            public class LoadingHUD
            {
                public readonly BaseVariable<bool> visible = new BaseVariable<bool>(false);
                public readonly BaseVariable<string> message = new BaseVariable<string>(null);
                public readonly BaseVariable<float> percentage = new BaseVariable<float>(0);
                public readonly BaseVariable<bool> showWalletPrompt = new BaseVariable<bool>(false);
                public readonly BaseVariable<bool> showTips = new BaseVariable<bool>(false);
            }
        }

        public class DataStore_Player
        {
            // NOTE: set when character is teleported (DCLCharacterController - Teleport)
            public readonly BaseVariable<Vector3> lastTeleportPosition = new BaseVariable<Vector3>(Vector3.zero);
        }

        public class DataStore_AvatarsLOD
        {
            public readonly BaseVariable<bool> LODEnabled = new BaseVariable<bool>(false); // false because of feature flag...
            public readonly BaseVariable<float> LODDistance = new BaseVariable<float>(16f);
            public readonly BaseVariable<int> maxNonLODAvatars = new BaseVariable<int>(20);
        }
    }
}