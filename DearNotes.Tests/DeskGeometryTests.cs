using System.Windows;
using DearNotes.Services;

namespace DearNotes.Tests
{
    public sealed class DeskGeometryTests
    {
        private static readonly Rect workArea = new(0, 0, 1920, 1080);

        private static readonly DpiScale defaultDpi = new(1, 1);

        [Fact]
        public void ComputeSnapBounds_RightSide_AnchorsToRightEdge()
        {
            Rect bounds = DeskGeometry.ComputeSnapBounds(
                workArea,
                defaultDpi,
                logicalWidth: 24,
                logicalHeight: 800,
                isOnRight: true,
                verticalPosition: 0.5,
                currentTopPixels: 0);

            Assert.Equal(1896, bounds.Left);
            Assert.Equal(24, bounds.Width);
            Assert.Equal(800, bounds.Height);
            Assert.Equal(140, bounds.Top);
        }

        [Fact]
        public void ComputeSnapBounds_LeftSide_AnchorsToLeftEdge()
        {
            Rect bounds = DeskGeometry.ComputeSnapBounds(
                workArea,
                defaultDpi,
                logicalWidth: 24,
                logicalHeight: 800,
                isOnRight: false,
                verticalPosition: 0.5,
                currentTopPixels: 0);

            Assert.Equal(0, bounds.Left);
        }

        [Theory]
        [InlineData(-1, 8)]
        [InlineData(0, 8)]
        [InlineData(0.5, 140)]
        [InlineData(1, 272)]
        [InlineData(2, 272)]
        public void ComputeSnapBounds_ClampsVerticalPosition(
            double verticalPosition,
            double expectedTop)
        {
            Rect bounds = DeskGeometry.ComputeSnapBounds(
                workArea,
                defaultDpi,
                logicalWidth: 24,
                logicalHeight: 800,
                isOnRight: true,
                verticalPosition,
                currentTopPixels: 0);

            Assert.Equal(expectedTop, bounds.Top);
        }

        [Theory]
        [InlineData(-500, 8)]
        [InlineData(140, 140)]
        [InlineData(5000, 272)]
        public void ComputeSnapBounds_WithoutVerticalPosition_KeepsCurrentTopWithinRange(
            double currentTop,
            double expectedTop)
        {
            Rect bounds = DeskGeometry.ComputeSnapBounds(
                workArea,
                defaultDpi,
                logicalWidth: 24,
                logicalHeight: 800,
                isOnRight: true,
                verticalPosition: null,
                currentTopPixels: currentTop);

            Assert.Equal(expectedTop, bounds.Top);
        }

        [Fact]
        public void ComputeSnapBounds_ScalesWidthAndMarginsForDpi()
        {
            Rect bounds = DeskGeometry.ComputeSnapBounds(
                workArea,
                new DpiScale(1.5, 1.5),
                logicalWidth: 24,
                logicalHeight: 600,
                isOnRight: true,
                verticalPosition: 0.5,
                currentTopPixels: 0);

            Assert.Equal(1884, bounds.Left);
            Assert.Equal(36, bounds.Width);
            Assert.Equal(900, bounds.Height);
            Assert.Equal(90, bounds.Top);
        }

        [Fact]
        public void ComputeSnapBounds_LimitsHeightToworkArea()
        {
            Rect bounds = DeskGeometry.ComputeSnapBounds(
                workArea,
                defaultDpi,
                logicalWidth: 24,
                logicalHeight: 5000,
                isOnRight: true,
                verticalPosition: 0.5,
                currentTopPixels: 0);

            Assert.Equal(1064, bounds.Height);
            Assert.Equal(8, bounds.Top);
        }

        [Fact]
        public void ComputeSnapBounds_LimitsWidthToworkArea()
        {
            var workArea = new Rect(0, 0, 100, 300);

            Rect bounds = DeskGeometry.ComputeSnapBounds(
                workArea,
                new DpiScale(2, 2),
                logicalWidth: 256,
                logicalHeight: 200,
                isOnRight: true,
                verticalPosition: 0.5,
                currentTopPixels: 0);

            Assert.Equal(100, bounds.Width);
            Assert.Equal(0, bounds.Left);
            Assert.True(bounds.Right <= workArea.Right);
            Assert.True(bounds.Bottom <= workArea.Bottom);
        }

        [Fact]
        public void ComputeSnapBounds_WhenMarginsExceedworkArea_StaysInside()
        {
            var workArea = new Rect(0, 0, 20, 60);

            Rect bounds = DeskGeometry.ComputeSnapBounds(
                workArea,
                new DpiScale(2, 2),
                logicalWidth: 256,
                logicalHeight: 100,
                isOnRight: true,
                verticalPosition: 0.5,
                currentTopPixels: 0);

            Assert.Equal(20, bounds.Width);
            Assert.Equal(28, bounds.Height);
            Assert.True(bounds.Left >= workArea.Left);
            Assert.True(bounds.Right <= workArea.Right);
            Assert.True(bounds.Top >= workArea.Top);
            Assert.True(bounds.Bottom <= workArea.Bottom);
        }

        [Theory]
        [InlineData(140, 800, 0.5)]
        [InlineData(8, 800, 0)]
        [InlineData(272, 800, 1)]
        [InlineData(-500, 800, 0)]
        [InlineData(5000, 800, 1)]
        public void ComputeNormalizedTop_ReturnsProportionalPosition(
            double top,
            double height,
            double expected)
        {
            double normalized = DeskGeometry.ComputeNormalizedTop(
                workArea,
                new Rect(0, top, 24, height),
                defaultDpi,
                fallback: 0.5);

            Assert.Equal(expected, normalized, precision: 3);
        }

        [Fact]
        public void ComputeNormalizedTop_WithoutAvailableSpace_ReturnsFallback()
        {
            double normalized = DeskGeometry.ComputeNormalizedTop(
                workArea,
                new Rect(0, 0, 24, 1080),
                defaultDpi,
                fallback: 0.3);

            Assert.Equal(0.3, normalized, precision: 3);
        }

        [Theory]
        [InlineData(960, true)]
        [InlineData(961, true)]
        [InlineData(959, false)]
        public void IsOnRightSide_UsesworkAreaCenter(
            double mouseX,
            bool expected)
        {
            Assert.Equal(
                expected,
                DeskGeometry.IsOnRightSide(workArea, mouseX));
        }

        [Fact]
        public void ComputeEditorBounds_CentersWindowWithMargin()
        {
            Rect bounds = DeskGeometry.ComputeEditorBounds(
                workArea,
                new Size(800, 600),
                defaultDpi,
                marginInDips: 12);

            Assert.Equal(560, bounds.Left);
            Assert.Equal(240, bounds.Top);
            Assert.Equal(800, bounds.Width);
            Assert.Equal(600, bounds.Height);
        }

        [Fact]
        public void ComputeEditorBounds_LimitsWindowToworkArea()
        {
            Rect bounds = DeskGeometry.ComputeEditorBounds(
                workArea,
                new Size(3000, 2000),
                defaultDpi,
                marginInDips: 12);

            Assert.Equal(12, bounds.Left);
            Assert.Equal(12, bounds.Top);
            Assert.Equal(1896, bounds.Width);
            Assert.Equal(1056, bounds.Height);
        }

        [Fact]
        public void ComputeDragPosition_AppliesRelativeAnchor()
        {
            Point position = DeskGeometry.ComputeDragPosition(
                new Point(500, 400),
                new Rect(0, 0, 200, 100),
                new Point(0.5, 0.5));

            Assert.Equal(400, position.X);
            Assert.Equal(350, position.Y);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(5, 88)]
        public void ComputeFanStep_WithRoomForAllCards_UsesPreferredStep(
            int count,
            double expectedStep)
        {
            double step = DeskGeometry.ComputeFanStep(
                count,
                DeskGeometry.FanCardHeight,
                availableHeight: 900,
                preferredStep: DeskGeometry.FanStep);

            Assert.Equal(expectedStep, step);
        }

        [Fact]
        public void ComputeFanStep_WhenHeightIsTight_LimitsStepToFitCards()
        {
            double step = DeskGeometry.ComputeFanStep(
                count: 5,
                DeskGeometry.FanCardHeight,
                availableHeight: 388,
                preferredStep: DeskGeometry.FanStep);

            Assert.Equal(53, step);
        }

        [Fact]
        public void ComputeFanStep_WhenCardsDoNotFit_ReturnsZero()
        {
            double step = DeskGeometry.ComputeFanStep(
                count: 5,
                DeskGeometry.FanCardHeight,
                availableHeight: 100,
                preferredStep: DeskGeometry.FanStep);

            Assert.Equal(0, step);
        }

        [Fact]
        public void ComputeFanTop_CentersStackInAvailableHeight()
        {
            double top = DeskGeometry.ComputeFanTop(
                count: 3,
                DeskGeometry.FanCardHeight,
                availableHeight: 900,
                preferredStep: DeskGeometry.FanStep);

            Assert.Equal(274, top);
        }

        [Fact]
        public void ComputeFanTop_WhenStackFillsHeight_StaysAtTop()
        {
            double top = DeskGeometry.ComputeFanTop(
                count: 5,
                DeskGeometry.FanCardHeight,
                availableHeight: 300,
                preferredStep: DeskGeometry.FanStep);

            Assert.Equal(0, top);
        }

        [Fact]
        public void ComputeFanTop_WithoutCards_ReturnsZero()
        {
            double top = DeskGeometry.ComputeFanTop(
                count: 0,
                DeskGeometry.FanCardHeight,
                availableHeight: 900,
                preferredStep: DeskGeometry.FanStep);

            Assert.Equal(0, top);
        }

        [Theory]
        [InlineData(true, 280)]
        [InlineData(false, -256)]
        public void ComputeFanCardLeft_ShowsOnlyTabInsidePanel(
            bool isOnRight,
            double expectedLeft)
        {
            double left = DeskGeometry.ComputeFanCardLeft(
                isOnRight,
                DeskGeometry.ExpandedWidth);

            Assert.Equal(expectedLeft, left);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ComputeFanSlideOffset_RevealsWholeCardInsidePanel(
            bool isOnRight)
        {
            double restingLeft = DeskGeometry.ComputeFanCardLeft(
                isOnRight,
                DeskGeometry.ExpandedWidth);

            double revealedLeft =
                restingLeft + DeskGeometry.ComputeFanSlideOffset(isOnRight);

            double revealedRight = revealedLeft + DeskGeometry.FanCardWidth;

            Assert.True(revealedLeft >= 0);
            Assert.True(revealedRight <= DeskGeometry.ExpandedWidth);
        }

        // Com 5 cartas e 568 de altura: topo 20, passo 88, abas em x 280–320.
        [Theory]
        [InlineData(300, 30, 0)]
        [InlineData(300, 120, 1)]
        [InlineData(300, 380, 4)]
        [InlineData(300, 540, 4)]
        [InlineData(300, 10, -1)]
        [InlineData(300, 560, -1)]
        public void ComputeChosenCardIndex_InTabColumn_UsesTabBand(
            double x,
            double y,
            int expectedIndex)
        {
            int index = DeskGeometry.ComputeChosenCardIndex(
                new Point(x, y),
                count: 5,
                availableHeight: 568,
                isOnRight: true,
                DeskGeometry.ExpandedWidth,
                currentIndex: -1);

            Assert.Equal(expectedIndex, index);
        }

        [Fact]
        public void ComputeChosenCardIndex_InTabColumnBelowOpenCard_ChoosesNextCard()
        {
            int index = DeskGeometry.ComputeChosenCardIndex(
                new Point(300, 150),
                count: 5,
                availableHeight: 568,
                isOnRight: true,
                DeskGeometry.ExpandedWidth,
                currentIndex: 0);

            Assert.Equal(1, index);
        }

        [Fact]
        public void ComputeChosenCardIndex_OverOpenCardBody_KeepsCurrentCard()
        {
            int index = DeskGeometry.ComputeChosenCardIndex(
                new Point(100, 150),
                count: 5,
                availableHeight: 568,
                isOnRight: true,
                DeskGeometry.ExpandedWidth,
                currentIndex: 0);

            Assert.Equal(0, index);
        }

        [Fact]
        public void ComputeChosenCardIndex_OutsideOpenCard_ChoosesNothing()
        {
            int index = DeskGeometry.ComputeChosenCardIndex(
                new Point(100, 300),
                count: 5,
                availableHeight: 568,
                isOnRight: true,
                DeskGeometry.ExpandedWidth,
                currentIndex: 0);

            Assert.Equal(-1, index);
        }

        [Fact]
        public void ComputeChosenCardIndex_LeftSide_UsesMirroredTabColumn()
        {
            int index = DeskGeometry.ComputeChosenCardIndex(
                new Point(20, 120),
                count: 5,
                availableHeight: 568,
                isOnRight: false,
                DeskGeometry.ExpandedWidth,
                currentIndex: -1);

            Assert.Equal(1, index);
        }

        [Fact]
        public void ComputeChosenCardIndex_WithoutCards_ChoosesNothing()
        {
            int index = DeskGeometry.ComputeChosenCardIndex(
                new Point(300, 120),
                count: 0,
                availableHeight: 568,
                isOnRight: true,
                DeskGeometry.ExpandedWidth,
                currentIndex: -1);

            Assert.Equal(-1, index);
        }

        [Fact]
        public void ComputeTabCenterInset_CentersOnVisibleTab()
        {
            double inset = DeskGeometry.ComputeTabCenterInset();

            Assert.Equal(23, inset);
        }

        [Fact]
        public void ComputeAddButtonTop_PlacesButtonBelowLastCard()
        {
            double top = DeskGeometry.ComputeAddButtonTop(
                count: 3,
                cardHeight: 176,
                availableHeight: 900,
                preferredStep: 88);

            Assert.Equal(274 + 176 + 2 * 88 + DeskGeometry.AddButtonGap, top);
        }

        [Fact]
        public void ComputeAddButtonTop_WhenHeightIsTight_FollowsCompressedStack()
        {
            double top = DeskGeometry.ComputeAddButtonTop(
                count: 5,
                cardHeight: 176,
                availableHeight: 388,
                preferredStep: 88);

            Assert.Equal(176 + 4 * 53 + DeskGeometry.AddButtonGap, top);
        }

        [Fact]
        public void ComputeAddButtonTop_WithoutCards_CentersButton()
        {
            double top = DeskGeometry.ComputeAddButtonTop(
                count: 0,
                cardHeight: 176,
                availableHeight: 500,
                preferredStep: 88);

            Assert.Equal((500 - DeskGeometry.AddButtonSize) / 2, top);
        }
    }
}
