using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DCL;

public class AvatarAudioHandlerRemote : MonoBehaviour
{
    const float WALK_INTERVAL_SEC = 0.37f, RUN_INTERVAL_SEC = 0.25f;
    float nextFootstepTime = 0f;

    AudioEvent footstepJump;
    AudioEvent footstepLand;
    AudioEvent footstepWalk;
    AudioEvent footstepRun;
    AudioEvent clothesRustleShort;

    GameObject rendererContainer;
    new Renderer renderer;

    AvatarAnimatorLegacy.BlackBoard blackBoard;
    bool isGroundedPrevious = true;

    public AvatarAnimatorLegacy avatarAnimatorLegacy;
    bool globalRendererIsReady;

    private Camera mainCamera;

    private void Start()
    {
        AudioContainer ac = GetComponent<AudioContainer>();
        footstepJump = ac.GetEvent("FootstepJump");
        footstepLand = ac.GetEvent("FootstepLand");
        footstepWalk = ac.GetEvent("FootstepWalk");
        footstepRun = ac.GetEvent("FootstepRun");
        clothesRustleShort = ac.GetEvent("ClothesRustleShort");

        // Lower volume of jump/land/clothes
        footstepJump.source.volume = footstepJump.source.volume * 0.5f;
        footstepLand.source.volume = footstepLand.source.volume * 0.5f;
        clothesRustleShort.source.volume = clothesRustleShort.source.volume * 0.5f;

        if (avatarAnimatorLegacy != null)
        {
            blackBoard = avatarAnimatorLegacy.blackboard;
        }

        globalRendererIsReady = CommonScriptableObjects.rendererState.Get();
        CommonScriptableObjects.rendererState.OnChange += OnGlobalRendererStateChange;
    }

    void OnGlobalRendererStateChange(bool current, bool previous) { globalRendererIsReady = current; }

    public void Init(GameObject rendererContainer) { this.rendererContainer = rendererContainer; }

    private bool avatarIsVisible = false;

    private void Update()
    {
        if (blackBoard == null || !globalRendererIsReady)
            return;

        // Jumped
        if (!blackBoard.isGrounded && isGroundedPrevious)
        {
            if (footstepJump != null)
                footstepJump.Play(true);
        }

        // Landed
        if (blackBoard.isGrounded && !isGroundedPrevious)
        {
            if (footstepLand != null)
                footstepLand.Play(true);
        }

        isGroundedPrevious = blackBoard.isGrounded;

        if (rendererContainer == null)
        {
            SimulateFootsteps();
            return;
        }

        if (rendererContainer != null && renderer == null)
        {
            //NOTE(Mordi): The renderer takes a while to get ready, so we need to check it continually until it can be fetched
            renderer = rendererContainer.GetComponent<Renderer>();

            if ( renderer == null )
                return;
        }

        // Simulate footsteps when avatar is not visible
        if (!AvatarIsInView())
        {
            SimulateFootsteps();
        }
    }

    bool AvatarIsInView()
    {
        if (avatarIsVisible || renderer.isVisible)
        {
            avatarIsVisible = true;
            return true;
        }

        if ( mainCamera == null )
            mainCamera = Camera.main;

        if (mainCamera == null)
            return false;

        // NOTE(Mordi): In some cases, the renderer will report false even if the avatar is visible.
        // Therefore we must check whether or not the avatar is in the camera's view.

        if (mainCamera == null)
            return false;

        Vector3 point = mainCamera.WorldToViewportPoint(transform.position);

        if (point.z > 0f)
        {
            if (point.x >= 0f && point.x <= 1f)
            {
                if (point.y >= 0f && point.y <= 1f)
                {
                    return true;
                }
            }
        }

        avatarIsVisible = false;
        return false;
    }

    void SimulateFootsteps()
    {
        if ((blackBoard.movementSpeed / Time.deltaTime) > 1f && blackBoard.isGrounded)
        {
            if (Time.time >= nextFootstepTime)
            {
                if ((blackBoard.movementSpeed / Time.deltaTime) > 6f)
                {
                    if (footstepRun != null)
                        footstepRun.Play(true);

                    if (clothesRustleShort != null)
                        clothesRustleShort.Play(true);

                    nextFootstepTime = Time.time + RUN_INTERVAL_SEC;
                }
                else
                {
                    if (footstepWalk != null)
                        footstepWalk.Play(true);

                    if (clothesRustleShort != null)
                        clothesRustleShort.PlayScheduled(Random.Range(0.05f, 0.1f));

                    nextFootstepTime = Time.time + WALK_INTERVAL_SEC;
                }
            }
        }
    }
}