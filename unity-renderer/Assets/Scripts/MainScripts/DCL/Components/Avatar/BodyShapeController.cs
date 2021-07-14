using System;
using System.Collections.Generic;
using DCL.Helpers;
using UnityEngine;

public interface IBodyShapeController
{
    string bodyShapeId { get; }
    void SetupEyes(Material material, Texture texture, Texture mask, Color color);
    void SetupEyebrows(Material material, Texture texture, Color color);
    void SetupMouth(Material material, Texture texture, Texture mask, Color color);
}

public class BodyShapeController : WearableController, IBodyShapeController
{
    public string bodyShapeId => wearable.id;
    private Transform animationTarget;

    public BodyShapeController(WearableItem wearableItem) : base(wearableItem) { }

    protected BodyShapeController(BodyShapeController original) : base(original)
    {
        headRenderer = original.headRenderer;
        eyebrowsRenderer = original.eyebrowsRenderer;
        eyesRenderer = original.eyesRenderer;
        mouthRenderer = original.mouthRenderer;
        feetRenderer = original.feetRenderer;
        upperBodyRenderer = original.upperBodyRenderer;
        lowerBodyRenderer = original.lowerBodyRenderer;
    }

    public SkinnedMeshRenderer skinnedMeshRenderer { get; private set; }

    public override void Load(string bodyShapeId, Transform parent, Action<WearableController> onSuccess, Action<WearableController> onFail)
    {
        animationTarget = parent;
        base.Load(bodyShapeId, parent, onSuccess, onFail);
    }

    public void SetActiveParts(bool lowerBodyActive, bool upperBodyActive, bool feetActive)
    {
        lowerBodyRenderer.gameObject.SetActive(lowerBodyActive);
        lowerBodyRenderer.enabled = lowerBodyActive;

        upperBodyRenderer.gameObject.SetActive(upperBodyActive);
        upperBodyRenderer.enabled = upperBodyActive;

        feetRenderer.gameObject.SetActive(feetActive);
        feetRenderer.enabled = feetActive;
    }

    public void SetupEyes(Material material, Texture texture, Texture mask, Color color)
    {
        if (assetContainer?.transform == null)
        {
            Debug.LogWarning("Tried to setup eyes when the asset not ready");
            return;
        }

        AvatarUtils.MapSharedMaterialsRecursively(assetContainer.transform,
            (mat) =>
            {
                material.SetTexture(AvatarUtils._EyesTexture, texture);
                material.SetTexture(AvatarUtils._IrisMask, mask);
                material.SetColor(AvatarUtils._EyeTint, color);
                return material;
            },
            "eyes");
    }

    public void SetupEyebrows(Material material, Texture texture, Color color)
    {
        if (assetContainer?.transform == null)
        {
            Debug.LogWarning("Tried to setup eyebrows when the asset not ready");
            return;
        }

        AvatarUtils.MapSharedMaterialsRecursively(assetContainer.transform,
            (mat) =>
            {
                material.SetTexture(AvatarUtils._BaseMap, texture);

                //NOTE(Brian): This isn't an error, we must also apply hair color to this mat
                material.SetColor(AvatarUtils._BaseColor, color);

                return material;
            },
            "eyebrows");
    }

    public override void SetAssetRenderersEnabled(bool active)
    {
        base.SetAssetRenderersEnabled(active);
        if (skinnedMeshRenderer != null)
            skinnedMeshRenderer.enabled = true;
    }

    public void SetupMouth(Material material, Texture texture, Texture mask, Color color)
    {
        if (assetContainer?.transform == null)
        {
            Debug.LogWarning("Tried to setup mouth when the asset not ready");
            return;
        }

        AvatarUtils.MapSharedMaterialsRecursively(assetContainer.transform,
            (mat) =>
            {
                material.SetTexture(AvatarUtils._BaseMap, texture);
                material.SetTexture(AvatarUtils._TintMask, mask);

                //NOTE(Brian): This isn't an error, we must also apply skin color to this mat
                material.SetColor(AvatarUtils._BaseColor, color);
                return material;
            },
            "mouth");
    }

    protected override void PrepareWearable(GameObject assetContainer)
    {
        Animation animation = null;

        //NOTE(Brian): Fix to support hierarchy difference between AssetBundle and GLTF wearables.
        Utils.ForwardTransformChildTraversal<Transform>((x) =>
            {
                if (x.name.Contains("Armature"))
                {
                    var armatureGO = x.parent;
                    animation = armatureGO.gameObject.GetOrCreateComponent<Animation>();
                    armatureGO.gameObject.GetOrCreateComponent<StickerAnimationListener>();
                    return false; //NOTE(Brian): If we return false the traversal is stopped.
                }

                return true;
            },
            assetContainer.transform);

        animation.cullingType = AnimationCullingType.BasedOnRenderers;

        var allRenderers = assetContainer.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (var r in allRenderers)
        {
            string parentName = r.transform.parent.name.ToLower();

            if (parentName.Contains("ubody"))
                upperBodyRenderer = r;
            else if (parentName.Contains("lbody"))
                lowerBodyRenderer = r;
            else if (parentName.Contains("feet"))
                feetRenderer = r;
            else if (parentName.Contains("head"))
                headRenderer = r;
            else if (parentName.Contains("eyebrows"))
                eyebrowsRenderer = r;
            else if (parentName.Contains("eyes"))
                eyesRenderer = r;
            else if (parentName.Contains("mouth"))
                mouthRenderer = r;
        }

        //We create a mock SkinnedMeshRenderer to hold the bones for the animations,
        //since any of the others SkinnedMeshRenderers in the bodyshape can be disabled arbitrarily
        skinnedMeshRenderer = animation.gameObject.GetOrCreateComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.enabled = true;
        skinnedMeshRenderer.rootBone = upperBodyRenderer.rootBone;
        skinnedMeshRenderer.bones = upperBodyRenderer.bones;
        skinnedMeshRenderer.localBounds = upperBodyRenderer.localBounds;

        var animator = animationTarget.GetComponent<AvatarAnimatorLegacy>();
        animator.BindBodyShape(animation, bodyShapeId, animationTarget);

        InitializeAvatarAudioHandlers(assetContainer, animation);
    }

    public SkinnedMeshRenderer headRenderer { get; private set; }
    public SkinnedMeshRenderer eyebrowsRenderer { get; private set; }
    public SkinnedMeshRenderer eyesRenderer { get; private set; }
    public SkinnedMeshRenderer mouthRenderer { get; private set; }
    public SkinnedMeshRenderer feetRenderer { get; private set; }
    public SkinnedMeshRenderer upperBodyRenderer { get; private set; }
    public SkinnedMeshRenderer lowerBodyRenderer { get; private set; }

    public override void UpdateVisibility(HashSet<string> hiddenList)
    {
        bool headIsVisible = !hiddenList.Contains(WearableLiterals.Misc.HEAD);

        headRenderer.enabled = headIsVisible;
        eyebrowsRenderer.enabled = headIsVisible;
        eyesRenderer.enabled = headIsVisible;
        mouthRenderer.enabled = headIsVisible;

        feetRenderer.enabled = !hiddenList.Contains(WearableLiterals.Categories.FEET);
        upperBodyRenderer.enabled = !hiddenList.Contains(WearableLiterals.Categories.UPPER_BODY);
        lowerBodyRenderer.enabled = !hiddenList.Contains(WearableLiterals.Categories.LOWER_BODY);
    }

    private void InitializeAvatarAudioHandlers(GameObject container, Animation createdAnimation)
    {
        //NOTE(Mordi): Adds audio handler for animation events, and passes in the audioContainer for the avatar
        AvatarAnimationEventAudioHandler animationEventAudioHandler = createdAnimation.gameObject.AddComponent<AvatarAnimationEventAudioHandler>();
        AudioContainer audioContainer = container.transform.parent.parent.GetComponentInChildren<AudioContainer>();
        if (audioContainer != null)
        {
            animationEventAudioHandler.Init(audioContainer);

            //NOTE(Mordi): If this is a remote avatar, pass the animation component so we can keep track of whether it is culled (off-screen) or not
            AvatarAudioHandlerRemote audioHandlerRemote = audioContainer.GetComponent<AvatarAudioHandlerRemote>();
            if (audioHandlerRemote != null)
            {
                audioHandlerRemote.Init(createdAnimation.gameObject);
            }
        }
    }
}