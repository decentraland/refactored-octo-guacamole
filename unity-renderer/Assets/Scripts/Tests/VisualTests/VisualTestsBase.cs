using System.Collections;
using UnityEngine;
using DCL;
using DCL.Helpers;

public class VisualTestsBase : IntegrationTestSuite_Legacy
{
    protected override string TEST_SCENE_NAME => "MainVisualTest";
    protected override bool enableSceneIntegrityChecker => false;

    protected override IEnumerator SetUp()
    {
        VisualTestHelpers.SetSSAOActive(false);
        yield break;
    }

    public IEnumerator InitVisualTestsScene(string testName)
    {
        yield return InitScene();
        yield return null;

        //TODO(Brian): This is to wait for SceneController.Awake(). We should remove this
        //             When the entry point is refactored.
        RenderProfileManifest.i.Initialize(RenderProfileManifest.i.testProfile);

        Environment.i.world.sceneBoundsChecker.Stop();
        Environment.i.world.blockersController.SetEnabled(false);

        base.SetUp_Renderer();

        VisualTestHelpers.currentTestName = testName;
        VisualTestHelpers.snapshotIndex = 0;

        DCLCharacterController.i.PauseGravity();
        DCLCharacterController.i.enabled = false;

        // Position character inside parcel (0,0)
        TestHelpers.SetCharacterPosition(new Vector3(0, 2f, 0f));

        yield return null;
    }
}