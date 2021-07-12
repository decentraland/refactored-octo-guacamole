using DCL.Controllers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuilderInWorldAudioHandler : MonoBehaviour
{
    const float MUSIC_DELAY_TIME_ON_START = 4f;
    const float MUSIC_FADE_OUT_TIME_ON_EXIT = 5f;
    const float MUSIC_FADE_OUT_TIME_ON_TUTORIAL = 3f;

    [SerializeField]
    BIWCreatorController creatorController;

    [SerializeField]
    GameObject builderInWorldModesParent;

    [SerializeField]
    BuilderInWorldController inWorldController;

    [SerializeField]
    BIWEntityHandler entityHandler;

    [SerializeField]
    BIWModeController modeController;

    [Header("Audio Events")]
    [SerializeField]
    AudioEvent eventAssetSpawn;

    [SerializeField]
    AudioEvent eventAssetPlace;

    [SerializeField]
    AudioEvent eventAssetSelect;

    [SerializeField]
    AudioEvent eventAssetDeselect;

    [SerializeField]
    AudioEvent eventBuilderOutOfBounds;

    [SerializeField]
    AudioEvent eventBuilderOutOfBoundsPlaced;

    [SerializeField]
    AudioEvent eventAssetDelete;

    [SerializeField]
    AudioEvent eventBuilderExit;

    [SerializeField]
    AudioEvent eventBuilderMusic;

    private List<string> entitiesOutOfBounds = new List<string>();
    private int entityCount;
    bool playPlacementSoundOnDeselect;
    private BIWModeController.EditModeState state = BIWModeController.EditModeState.Inactive;

    private void Start()
    {
        playPlacementSoundOnDeselect = false;

        AddListeners();
    }

    private void OnDestroy() { RemoveListeners(); }

    private void AddListeners()
    {
        creatorController.OnCatalogItemPlaced += OnAssetSpawn;
        entityHandler.OnDeleteSelectedEntities += OnAssetDelete;
        modeController.OnChangedEditModeState += OnChangedEditModeState;
        DCL.Environment.i.world.sceneBoundsChecker.OnEntityBoundsCheckerStatusChanged += OnEntityBoundsCheckerStatusChanged;

        DCL.Tutorial.TutorialController.i.OnTutorialEnabled += OnTutorialEnabled;
        DCL.Tutorial.TutorialController.i.OnTutorialDisabled += OnTutorialDisabled;

        entityHandler.OnEntityDeselected += OnAssetDeselect;
        entityHandler.OnEntitySelected += OnAssetSelect;
    }

    private void RemoveListeners()
    {
        creatorController.OnCatalogItemPlaced -= OnAssetSpawn;
        entityHandler.OnDeleteSelectedEntities -= OnAssetDelete;
        modeController.OnChangedEditModeState -= OnChangedEditModeState;
        DCL.Environment.i.world.sceneBoundsChecker.OnEntityBoundsCheckerStatusChanged -= OnEntityBoundsCheckerStatusChanged;

        DCL.Tutorial.TutorialController.i.OnTutorialEnabled -= OnTutorialEnabled;
        DCL.Tutorial.TutorialController.i.OnTutorialDisabled -= OnTutorialDisabled;

        entityHandler.OnEntityDeselected -= OnAssetDeselect;
        entityHandler.OnEntitySelected -= OnAssetSelect;
    }

    public void EnterEditMode(ParcelScene scene)
    {

        UpdateEntityCount();
        CoroutineStarter.Start(StartBuilderMusic());
        if (HUDController.i.builderInWorldMainHud != null)
            HUDController.i.builderInWorldMainHud.OnCatalogItemSelected += OnCatalogItemSelected;

        gameObject.SetActive(true);
    }

    public void ExitEditMode()
    {
        eventBuilderExit.Play();
        CoroutineStarter.Start(eventBuilderMusic.FadeOut(MUSIC_FADE_OUT_TIME_ON_EXIT));
        if (HUDController.i.builderInWorldMainHud != null)
            HUDController.i.builderInWorldMainHud.OnCatalogItemSelected -= OnCatalogItemSelected;

        gameObject.SetActive(false);
    }

    private void OnAssetSpawn() { eventAssetSpawn.Play(); }

    private void OnAssetDelete(List<DCLBuilderInWorldEntity> entities)
    {
        foreach (DCLBuilderInWorldEntity deletedEntity in entities)
        {
            if (entitiesOutOfBounds.Contains(deletedEntity.rootEntity.entityId))
            {
                entitiesOutOfBounds.Remove(deletedEntity.rootEntity.entityId);
            }
        }

        eventAssetDelete.Play();
    }

    private void OnAssetSelect() { eventAssetSelect.Play(); }

    private void OnAssetDeselect(DCLBuilderInWorldEntity entity)
    {
        if (playPlacementSoundOnDeselect)
        {
            eventAssetPlace.Play();
            playPlacementSoundOnDeselect = false;
        }
        else
            eventAssetDeselect.Play();

        UpdateEntityCount();

        if (entitiesOutOfBounds.Contains(entity.rootEntity.entityId))
        {
            eventBuilderOutOfBoundsPlaced.Play();
        }
    }

    private void OnCatalogItemSelected(CatalogItem catalogItem) { playPlacementSoundOnDeselect = true; }

    private void OnTutorialEnabled()
    {
        if (inWorldController.isBuilderInWorldActivated)
            CoroutineStarter.Start(eventBuilderMusic.FadeOut(MUSIC_FADE_OUT_TIME_ON_TUTORIAL));
    }

    private void OnTutorialDisabled()
    {
        if (inWorldController.isBuilderInWorldActivated)
            CoroutineStarter.Start(StartBuilderMusic());
    }

    private IEnumerator StartBuilderMusic()
    {
        yield return new WaitForSeconds(MUSIC_DELAY_TIME_ON_START);

        if (inWorldController.isBuilderInWorldActivated)
            eventBuilderMusic.Play();
    }

    private void OnChangedEditModeState(BIWModeController.EditModeState previous, BIWModeController.EditModeState current)
    {
        state = current;
        if (previous != BIWModeController.EditModeState.Inactive)
        {
            switch (current)
            {
                case BIWModeController.EditModeState.FirstPerson:
                    AudioScriptableObjects.cameraFadeIn.Play();
                    break;
                case BIWModeController.EditModeState.GodMode:
                    AudioScriptableObjects.cameraFadeOut.Play();
                    break;
                default:
                    break;
            }
        }
    }

    private void OnEntityBoundsCheckerStatusChanged(DCL.Models.IDCLEntity entity, bool isInsideBoundaries)
    {
        if (state == BIWModeController.EditModeState.Inactive)
            return;

        if (!isInsideBoundaries)
        {
            if (!entitiesOutOfBounds.Contains(entity.entityId))
            {
                entitiesOutOfBounds.Add(entity.entityId);
                eventBuilderOutOfBounds.Play();
            }
        }
        else
        {
            if (entitiesOutOfBounds.Contains(entity.entityId))
            {
                entitiesOutOfBounds.Remove(entity.entityId);
            }
        }
    }

    private void UpdateEntityCount() { entityCount = entityHandler.GetCurrentSceneEntityCount(); }

    private bool EntityHasBeenAddedSinceLastUpdate() { return (entityHandler.GetCurrentSceneEntityCount() > entityCount); }
}