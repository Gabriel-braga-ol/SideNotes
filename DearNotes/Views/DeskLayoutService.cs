using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DearNotes.Models;
using DearNotes.Services;

namespace DearNotes.Views
{
    /// <summary>
    /// Mantém o estado de posicionamento do Desk e aplica a geometria pura
    /// de <see cref="DeskGeometry"/> à janela e às abas das notas.
    /// </summary>
    internal sealed class DeskLayoutService
    {
        private const double CardSpacing = 58;

        private const double CardHeight = 160;

        private const double CardPadding = 12;

        private const double MaxAngle = 2;

        private const double MaxOffset = 4;

        private readonly Window window;

        private readonly FrameworkElement pillHandle;

        private readonly ItemsControl noteTabs;

        private readonly IScreenService screenService;

        private readonly IDeskPositionStore positionStore;

        private readonly double preferredHeight;

        private DeskScreen? currentScreen;

        private double requestedWidth = DeskGeometry.CollapsedWidth;

        private double verticalPosition = 0.5;

        public DeskLayoutService(
            Window window,
            FrameworkElement pillHandle,
            ItemsControl noteTabs,
            IScreenService screenService,
            IDeskPositionStore positionStore,
            double preferredHeight)
        {
            this.window = window;
            this.pillHandle = pillHandle;
            this.noteTabs = noteTabs;
            this.screenService = screenService;
            this.positionStore = positionStore;
            this.preferredHeight = preferredHeight;
        }

        public DeskScreen? CurrentScreen => currentScreen;

        public double RequestedWidth => requestedWidth;

        public double VerticalPosition => verticalPosition;

        public static DeskScreen? TryResolveScreen(
            Func<DeskScreen> findScreen)
        {
            try
            {
                return findScreen();
            }
            catch (Exception ex) when (
                ex is Win32Exception ||
                ex is InvalidOperationException)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Desk: não foi possível atualizar o monitor: " +
                    ex.Message);

                return null;
            }
        }

        public DeskPosition Initialize()
        {
            DeskPosition position = positionStore.Load();

            currentScreen = screenService.FindOrPrimary(position.MonitorDeviceName);

            Rect area = currentScreen.WorkArea;

            screenService.TryMoveWindow(window, area.Left, area.Top);

            return position;
        }

        public void SetCurrentScreen(DeskScreen screen)
        {
            currentScreen = screen;
        }

        public void UpdateSideLayout(bool isOnRight)
        {
            HorizontalAlignment alignment = isOnRight
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;

            pillHandle.HorizontalAlignment = alignment;
            noteTabs.HorizontalAlignment = alignment;
            noteTabs.Margin = new Thickness(0);

            PositionNoteCards(isOnRight);
        }

        public void SnapToEdge(
            bool isOnRight,
            double? verticalPosition = null,
            double? widthInDips = null)
        {
            if (currentScreen is null) return;

            DpiScale dpi = currentScreen.Dpi;

            Rect bounds = screenService.GetWindowBounds(window);

            Rect target = DeskGeometry.ComputeSnapBounds(
                currentScreen.WorkArea,
                dpi,
                widthInDips ?? requestedWidth,
                preferredHeight,
                isOnRight,
                verticalPosition,
                bounds.Top);

            if (!screenService.TrySetWindowBounds(
                    window,
                    target.Left,
                    target.Top,
                    target.Width,
                    target.Height))
            {
                return;
            }

            this.verticalPosition = DeskGeometry.ComputeNormalizedTop(
                currentScreen.WorkArea,
                target,
                dpi,
                verticalPosition ?? this.verticalPosition);
        }

        public void SetDeskWidth(
            double width,
            bool isOnRight,
            bool isDragging,
            bool isWindowReady)
        {
            requestedWidth = width;

            if (!isWindowReady)
            {
                window.Width = width;
                return;
            }

            if (isDragging || currentScreen is null)
            {
                Rect bounds = screenService.GetWindowBounds(window);

                DpiScale dpi = currentScreen?.Dpi ??
                    VisualTreeHelper.GetDpi(window);

                double widthPixels = Math.Max(
                    1,
                    Math.Round(width * dpi.DpiScaleX));

                double left = isOnRight
                    ? bounds.Right - widthPixels
                    : bounds.Left;

                screenService.TrySetWindowBounds(
                    window,
                    left,
                    bounds.Top,
                    widthPixels,
                    bounds.Height);
            }
            else
            {
                SnapToEdge(isOnRight, verticalPosition, width);
            }

            window.UpdateLayout();
        }

        public void PositionNoteCards(bool isOnRight)
        {
            int count = noteTabs.Items.Count;

            noteTabs.Height =
                Math.Max(0, count - 1) * CardSpacing
                + CardHeight
                + CardPadding * 2;

            double direction = isOnRight ? -1 : 1;

            for (int i = 0; i < count; i++)
            {
                var container =
                    noteTabs.ItemContainerGenerator
                        .ContainerFromIndex(i) as ContentPresenter;

                if (container is null)
                    continue;

                double progress = count <= 1
                    ? 0.5
                    : (double)i / (count - 1);

                double angle = count <= 1
                    ? 0
                    : (progress * 2 - 1) * MaxAngle;

                double offset = count <= 1
                    ? 0
                    : progress * MaxOffset;

                container.Tag = angle * direction;

                Canvas.SetLeft(container, 4 + offset * direction);
                Canvas.SetTop(container, CardPadding + i * CardSpacing);
            }
        }

        public void SavePosition(bool isOnRight)
        {
            if (currentScreen is null) return;

            Rect bounds = screenService.GetWindowBounds(window);

            positionStore.Save(new DeskPosition
            {
                MonitorDeviceName = currentScreen.DeviceName,
                IsOnRight = isOnRight,
                VerticalPosition = DeskGeometry.ComputeNormalizedTop(
                    currentScreen.WorkArea,
                    bounds,
                    currentScreen.Dpi,
                    fallback: 0.5)
            });
        }
    }
}
