using DCL.Configuration;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.EventSystems;

namespace Tests.BuildModeHUDControllers
{
    public class TopActionsButtonsControllerShould
    {
        private TopActionsButtonsController topActionsButtonsController;

        [SetUp]
        public void SetUp()
        {
            topActionsButtonsController = new TopActionsButtonsController();
            topActionsButtonsController.Initialize(
                Substitute.For<ITopActionsButtonsView>(),
                Substitute.For<ITooltipController>());
        }

        [TearDown]
        public void TearDown() { topActionsButtonsController.Dispose(); }

        [Test]
        public void ClickOnChangeModeCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnChangeModeClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.ChangeModeClicked();

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void ClickOnExtraCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnExtraClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.ExtraClicked();

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void ClickOnTranslateCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnTranslateClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.TranslateClicked();

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void ClickOnRotateCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnRotateClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.RotateClicked();

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void ClickOnScaleCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnScaleClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.ScaleClicked();

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void ClickOnUndoCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnUndoClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.UndoClicked();

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void ClickOnRedoCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnRedoClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.RedoClicked();

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void ClickOnDuplicateCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnDuplicateClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.DuplicateClicked();

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void ClickOnDeleteCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnDeleteClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.DeleteClicked();

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void ShowLogoutConfirmationCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnLogOutClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.LogoutClicked();

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void ConfirmLogoutCorrectly()
        {
            // Arrange
            bool clicked = false;
            topActionsButtonsController.OnLogOutClick += () => { clicked = true; };

            // Act
            topActionsButtonsController.ConfirmLogout(BuildModeModalType.EXIT);

            // Assert
            Assert.IsTrue(clicked, "The clicked is false!");
        }

        [Test]
        public void TooltipPointerEnteredCorrectly()
        {
            // Arrange
            BaseEventData testEventData = new BaseEventData(null);
            string testText = "Test text";

            // Act
            topActionsButtonsController.TooltipPointerEntered(testEventData, testText);

            // Assert
            topActionsButtonsController.tooltipController.Received(1).ShowTooltip(testEventData);
            topActionsButtonsController.tooltipController.Received(1).SetTooltipText(testText);
        }

        [Test]
        public void TooltipPointerExitedCorrectly()
        {
            // Act
            topActionsButtonsController.TooltipPointerExited();

            // Assert
            topActionsButtonsController.tooltipController.Received(1).HideTooltip();
        }

        [Test]
        public void TestSetGizmosActivetedCorrectly()
        {
            //Arrange
            string gizmosActive =  BIWSettings.TRANSLATE_GIZMO_NAME;

            // Act
            topActionsButtonsController.SetGizmosActive(gizmosActive);

            // Assert
            topActionsButtonsController.topActionsButtonsView.Received(1).SetGizmosActive(gizmosActive);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void TestSeActionsInteractable(bool isInteractable)
        {
            //Act 
            topActionsButtonsController.SetActionsInteractable(isInteractable);

            //Assert
            topActionsButtonsController.topActionsButtonsView.Received(1).SetActionsInteractable(isInteractable);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void TestSnapModeChange(bool isActive)
        {
            //Act 
            topActionsButtonsController.SetSnapActive(isActive);

            //Assert
            topActionsButtonsController.topActionsButtonsView.Received(1).SetSnapActive(isActive);
        }
    }
}