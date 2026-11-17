using System.Windows;
using DearNotes.Services;

namespace DearNotes.Tests
{
    public sealed class DeskGeometryTests
    {
        private static readonly Rect workArea = new(0, 0, 1920, 1080);

        private static readonly DpiScale defaultDpi = new(1, 1);

        [Fact]
        public void ComputeSnapBounds_RightSide_AnchorsToRightMargin()
        {
            Rect bounds = DeskGeometry.ComputeSnapBounds(
                workArea,
                defaultDpi,
                logicalWidth: 24,
                logicalHeight: 800,
                isOnRight: true,
                verticalPosition: 0.5,
                currentTopPixels: 0);

            Assert.Equal(1888, bounds.Left);
            Assert.Equal(24, bounds.Width);
            Assert.Equal(800, bounds.Height);
            Assert.Equal(140, bounds.Top);
        }

        [Fact]
        public void ComputeSnapBounds_LeftSide_AnchorsToLeftMargin()
        {
            Rect bounds = DeskGeometry.ComputeSnapBounds(
                workArea,
                defaultDpi,
                logicalWidth: 24,
                logicalHeight: 800,
                isOnRight: false,
                verticalPosition: 0.5,
                currentTopPixels: 0);

            Assert.Equal(8, bounds.Left);
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

            Assert.Equal(1872, bounds.Left);
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

            Assert.Equal(68, bounds.Width);
            Assert.Equal(16, bounds.Left);
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

            Assert.Equal(1, bounds.Width);
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
    }
}
