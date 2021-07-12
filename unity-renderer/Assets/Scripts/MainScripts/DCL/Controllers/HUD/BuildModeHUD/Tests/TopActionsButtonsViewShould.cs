using DCL.Configuration;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Tests.BuildModeHUDViews
{
    public class TopActionsButtonsViewShould
    {
        private TopActionsButtonsView topActionsButtonsView;

        [SetUp]
        public void SetUp() { topActionsButtonsView = TopActionsButtonsView.Create(); }

        [TearDown]
        public void TearDown() { Object.Destroy(topActionsButtonsView.gameObject); }

        [Test]
        public void ConfigureExtraActionsCorrectly()
        {
            // Arrange
            IExtraActionsController mockedExtraActionsController = Substitute.For<IExtraActionsController>();
            topActionsButtonsView.extraActionsController = null;


            // Act
            topActionsButtonsView.ConfigureExtraActions(mockedExtraActionsController);

            // Assert
            Assert.AreEqual(mockedExtraActionsController, topActionsButtonsView.extraActionsController, "The extra actions controller does not match!");
            mockedExtraActionsController.Received(1).Initialize(topActionsButtonsView.extraActionsView);
        }

        [Test]
        public void ClickOnChangeModeCorrectly()
        {
            // Arrange
            bool modeIsChanged = false;
            topActionsButtonsView.OnChangeModeClicked += () => modeIsChanged = true;

            // Act
            topActionsButtonsView.OnChangeModeClick(new DCLAction_Trigger());

            // Assert
            Assert.IsTrue(modeIsChanged, "modeIsChanged is false!");
        }

        [Test]
        public void ClickOnExtraCorrectly()
        {
            // Arrange
            bool extraClicked = false;
            topActionsButtonsView.OnExtraClicked += () => extraClicked = true;

            // Act
            topActionsButtonsView.OnExtraClick(new DCLAction_Trigger());

            // Assert
            Assert.IsTrue(extraClicked, "extraClicked is false!");
        }

        [Test]
        public void ClickOnTranslateCorrectly()
        {
            // Arrange
            bool translateClicked = false;
            topActionsButtonsView.OnTranslateClicked += () => translateClicked = true;

            // Act
            topActionsButtonsView.OnTranslateClick(new DCLAction_Trigger());

            // Assert
            Assert.IsTrue(translateClicked, "translateClicked is false!");
        }

        [Test]
        public void ClickOnRotateCorrectly()
        {
            // Arrange
            bool rotateClicked = false;
            topActionsButtonsView.OnRotateClicked += () => rotateClicked = true;

            // Act
            topActionsButtonsView.OnRotateClick(new DCLAction_Trigger());

            // Assert
            Assert.IsTrue(rotateClicked, "rotateClicked is false!");
        }

        [Test]
        public void ClickOnScaleCorrectly()
        {
            // Arrange
            bool scaleClicked = false;
            topActionsButtonsView.OnScaleClicked += () => scaleClicked = true;

            // Act
            topActionsButtonsView.OnScaleClick(new DCLAction_Trigger());

            // Assert
            Assert.IsTrue(scaleClicked, "scaleClicked is false!");
        }

        [Test]
        public void ClickOnUndoCorrectly()
        {
            // Arrange
            bool undoClicked = false;
            topActionsButtonsView.OnUndoClicked += () => undoClicked = true;

            // Act
            topActionsButtonsView.OnUndoClick();

            // Assert
            Assert.IsTrue(undoClicked, "undoClicked is false!");
        }

        [Test]
        public void ClickOnRedoCorrectly()
        {
            // Arrange
            bool redoClicked = false;
            topActionsButtonsView.OnRedoClicked += () => redoClicked = true;

            // Act
            topActionsButtonsView.OnRedoClick();

            // Assert
            Assert.IsTrue(redoClicked, "undoClicked is false!");
        }

        [Test]
        public void ClickOnDuplicateCorrectly()
        {
            // Arrange
            bool duplicateClicked = false;
            topActionsButtonsView.OnDuplicateClicked += () => duplicateClicked = true;

            // Act
            topActionsButtonsView.OnDuplicateClick(new DCLAction_Trigger());

            // Assert
            Assert.IsTrue(duplicateClicked, "duplicateClicked is false!");
        }

        [Test]
        public void ClickOnDeleteCorrectly()
        {
            // Arrange
            bool deleteClicked = false;
            topActionsButtonsView.OnDeleteClicked += () => deleteClicked = true;

            // Act
            topActionsButtonsView.OnDeleteClick(new DCLAction_Trigger());

            // Assert
            Assert.IsTrue(deleteClicked, "deleteClicked is false!");
        }

        [Test]
        public void ClickOnLogoutCorrectly()
        {
            // Arrange
            bool logoutClicked = false;
            topActionsButtonsView.OnLogOutClicked += () => logoutClicked = true;

            // Act
            topActionsButtonsView.OnLogOutClick(new DCLAction_Trigger());

            // Assert
            Assert.IsTrue(logoutClicked, "logoutClicked is false!");
        }

        [Test]
        public void SetTranslateActiveGizmosCorrectly()
        {
            //Arrange
            string gizmosActive = BIWSettings.TRANSLATE_GIZMO_NAME;

            // Act
            topActionsButtonsView.SetGizmosActive(gizmosActive);

            // Assert
            Assert.AreEqual(topActionsButtonsView.translateGizmosBtnImg.color , topActionsButtonsView.selectedBtnImgColor);
            Assert.AreEqual(topActionsButtonsView.rotateGizmosBtnImg.color , topActionsButtonsView.normalBtnImgColor);
            Assert.AreEqual(topActionsButtonsView.scaleGizmosBtnImg.color , topActionsButtonsView.normalBtnImgColor);
        }

        [Test]
        public void SetRotateActiveGizmosCorrectly()
        {
            //Arrange
            string gizmosActive = BIWSettings.ROTATE_GIZMO_NAME;

            // Act
            topActionsButtonsView.SetGizmosActive(gizmosActive);

            // Assert
            Assert.AreEqual(topActionsButtonsView.translateGizmosBtnImg.color , topActionsButtonsView.normalBtnImgColor);
            Assert.AreEqual(topActionsButtonsView.rotateGizmosBtnImg.color , topActionsButtonsView.selectedBtnImgColor);
            Assert.AreEqual(topActionsButtonsView.scaleGizmosBtnImg.color , topActionsButtonsView.normalBtnImgColor);
        }

        [Test]
        public void SetScaleActiveGizmosCorrectly()
        {
            //Arrange
            string gizmosActive = BIWSettings.SCALE_GIZMO_NAME;

            // Act
            topActionsButtonsView.SetGizmosActive(gizmosActive);

            // Assert
            Assert.AreEqual(topActionsButtonsView.translateGizmosBtnImg.color , topActionsButtonsView.normalBtnImgColor);
            Assert.AreEqual(topActionsButtonsView.rotateGizmosBtnImg.color , topActionsButtonsView.normalBtnImgColor);
            Assert.AreEqual(topActionsButtonsView.scaleGizmosBtnImg.color , topActionsButtonsView.selectedBtnImgColor);
        }

        [Test]
        public void SetEmptyGizmosCorrectly()
        {
            //Arrange
            string gizmosActive = BIWSettings.EMPTY_GIZMO_NAME;

            // Act
            topActionsButtonsView.SetGizmosActive(gizmosActive);

            // Assert
            Assert.AreEqual(topActionsButtonsView.translateGizmosBtnImg.color , topActionsButtonsView.normalBtnImgColor);
            Assert.AreEqual(topActionsButtonsView.rotateGizmosBtnImg.color , topActionsButtonsView.normalBtnImgColor);
            Assert.AreEqual(topActionsButtonsView.scaleGizmosBtnImg.color , topActionsButtonsView.normalBtnImgColor);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void SetActionsInteractable(bool isActive)
        {
            //Act
            topActionsButtonsView.SetActionsInteractable(isActive);

            Assert.AreEqual(isActive, topActionsButtonsView.duplicateBtn.IsInteractable());
            Assert.AreEqual(isActive, topActionsButtonsView.deleteBtn.IsInteractable());
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void SetSnapMode(bool isActive)
        {
            //Arrange
            Color correctColor = isActive ? topActionsButtonsView.selectedBtnImgColor : topActionsButtonsView.normalBtnImgColor;

            //Act
            topActionsButtonsView.SetSnapActive(isActive);

            //Assert
            Assert.AreEqual(topActionsButtonsView.snapModeBtnImg.color , correctColor);
        }
    }
}