using System;
using System.Windows;

namespace DearNotes.Services
{
    public static class DeskGeometry
    {
        public const double CollapsedWidth = 24;

        public const double ExpandedWidth = 256;

        public const double ScreenMargin = 8;

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

            double widthPixels = Math.Max(
                1,
                Math.Round(logicalWidth * dpi.DpiScaleX));

            double heightPixels = Math.Max(
                1,
                Math.Min(
                    Math.Round(logicalHeight * dpi.DpiScaleY),
                    workArea.Height - marginY * 2));

            double availableHeight = Math.Max(
                0,
                workArea.Height - heightPixels - marginY * 2);

            double minTop = workArea.Top + marginY;

            double top = verticalPosition.HasValue
                ? minTop + availableHeight *
                  Math.Clamp(verticalPosition.Value, 0, 1)
                : Math.Clamp(
                    currentTopPixels,
                    minTop,
                    minTop + availableHeight);

            top = Math.Round(top);

            double left = isOnRight
                ? workArea.Right - widthPixels - marginX
                : workArea.Left + marginX;

            return new Rect(left, top, widthPixels, heightPixels);
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
