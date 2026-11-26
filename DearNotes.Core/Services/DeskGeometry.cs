using System;
using System.Windows;

namespace DearNotes.Services
{
    public static class DeskGeometry
    {
        public const double CollapsedWidth = 36;

        public const double ExpandedWidth = 400;

        public const double ScreenMargin = 8;

        public const double FanCardWidth = 340;

        public const double FanCardHeight = 188;

        public const double FanStep = 70;

        public const double FanEdgeMargin = 10;

        public const double AddButtonReserve = 64;

        public static Rect ComputeSnapBounds(
            Rect workArea,
            DpiScale dpi,
            double logicalWidth,
            double logicalHeight,
            bool isOnRight,
            double? verticalPosition,
            double currentTopPixels)
        {
            double marginX = Math.Round(ScreenMargin * dpi.DpiScaleX);
            double marginY = Math.Round(ScreenMargin * dpi.DpiScaleY);

            double maxWidth = Math.Max(
                1,
                workArea.Width - marginX * 2);

            double widthPixels = Math.Max(
                1,
                Math.Min(
                    Math.Round(logicalWidth * dpi.DpiScaleX),
                    maxWidth));

            double heightPixels = Math.Max(
                1,
                Math.Min(
                    Math.Round(logicalHeight * dpi.DpiScaleY),
                    workArea.Height - marginY * 2));

            double availableHeight = Math.Max(
                0,
                workArea.Height - heightPixels - marginY * 2);

            double minTop = workArea.Top + marginY;
            double maxTop = Math.Max(
                workArea.Top,
                workArea.Bottom - heightPixels - marginY);

            double top = verticalPosition.HasValue
                ? minTop + availableHeight *
                  Math.Clamp(verticalPosition.Value, 0, 1)
                : Math.Clamp(
                    currentTopPixels,
                    minTop,
                    minTop + availableHeight);

            top = Math.Clamp(
                Math.Round(top),
                Math.Min(minTop, maxTop),
                maxTop);

            double left = isOnRight
                ? workArea.Right - widthPixels - marginX
                : workArea.Left + marginX;

            left = Math.Clamp(
                left,
                workArea.Left,
                Math.Max(workArea.Left, workArea.Right - widthPixels));

            return new Rect(left, top, widthPixels, heightPixels);
        }

        public static double ComputeFanStep(
            int count,
            double cardHeight,
            double availableHeight,
            double preferredStep)
        {
            if (count < 2) return 0;

            double maxStep = Math.Max(
                0,
                (availableHeight - cardHeight) / (count - 1));

            return Math.Clamp(preferredStep, 0, maxStep);
        }

        public static double ComputeFanTop(
            int count,
            double cardHeight,
            double availableHeight,
            double preferredStep)
        {
            if (count <= 0) return 0;

            double step = ComputeFanStep(
                count,
                cardHeight,
                availableHeight,
                preferredStep);

            double stackHeight = cardHeight + (count - 1) * step;

            return Math.Max(0, (availableHeight - stackHeight) / 2);
        }

        public static double ComputeNormalizedTop(
            Rect workArea,
            Rect windowBounds,
            DpiScale dpi,
            double fallback)
        {
            double marginY = Math.Round(ScreenMargin * dpi.DpiScaleY);

            double availableHeight = Math.Max(
                0,
                workArea.Height - windowBounds.Height - marginY * 2);

            if (availableHeight <= 0)
            {
                return Math.Clamp(fallback, 0, 1);
            }

            double normalizedTop =
                (windowBounds.Top - workArea.Top - marginY) /
                availableHeight;

            return Math.Clamp(normalizedTop, 0, 1);
        }

        public static bool IsOnRightSide(
            Rect workArea,
            double mouseX)
        {
            double screenCenter = workArea.Left + workArea.Width / 2;

            return mouseX >= screenCenter;
        }

        public static Rect ComputeEditorBounds(
            Rect workArea,
            Size windowSize,
            DpiScale dpi,
            double marginInDips)
        {
            double marginX = marginInDips * dpi.DpiScaleX;
            double marginY = marginInDips * dpi.DpiScaleY;

            double widthPixels = Math.Max(
                1,
                Math.Min(
                    windowSize.Width * dpi.DpiScaleX,
                    workArea.Width - marginX * 2));

            double heightPixels = Math.Max(
                1,
                Math.Min(
                    windowSize.Height * dpi.DpiScaleY,
                    workArea.Height - marginY * 2));

            return new Rect(
                workArea.Left + (workArea.Width - widthPixels) / 2,
                workArea.Top + (workArea.Height - heightPixels) / 2,
                widthPixels,
                heightPixels);
        }

        public static Point ComputeDragPosition(
            Point mousePosition,
            Rect windowBounds,
            Point dragAnchor)
        {
            return new Point(
                mousePosition.X - windowBounds.Width * dragAnchor.X,
                mousePosition.Y - windowBounds.Height * dragAnchor.Y);
        }
    }
}
