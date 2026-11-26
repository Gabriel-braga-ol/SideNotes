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
    /// de <see cref="DeskGeometry"/> à janela, ao baralho e ao leque.
    /// </summary>
    internal sealed class DeskLayoutService
    {
        private readonly Window window;

        private readonly FrameworkElement deskHandle;

        private readonly ItemsControl fanCards;

        private readonly FrameworkElement addNoteButton;

        private readonly IScreenService screenService;

        private readonly IDeskPositionStore positionStore;

        private readonly double preferredHeight;

        private DeskScreen? currentScreen;

        private double requestedWidth = DeskGeometry.CollapsedWidth;

        private double verticalPosition = 0.5;

        public DeskLayoutService(
            Window window,
            FrameworkElement deskHandle,
            ItemsControl fanCards,
            FrameworkElement addNoteButton,
            IScreenService screenService,
            IDeskPositionStore positionStore,
            double preferredHeight)
        {
            this.window = window;
            this.deskHandle = deskHandle;
            this.fanCards = fanCards;
            this.addNoteButton = addNoteButton;
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

            deskHandle.HorizontalAlignment = alignment;
            addNoteButton.HorizontalAlignment = alignment;

            // Centralizar o baralho na mesma faixa do leque mantém as pílulas
            // na altura em que as cartas vão surgir.
            deskHandle.Margin = isOnRight
                ? new Thickness(
                    0, 0, DeskGeometry.HandleEdgeGap, DeskGeometry.AddButtonReserve)
                : new Thickness(
                    DeskGeometry.HandleEdgeGap, 0, 0, DeskGeometry.AddButtonReserve);

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
            int count = fanCards.Items.Count;

            double availableHeight = GetFanAvailableHeight();

            double step = DeskGeometry.ComputeFanStep(
                count,
                DeskGeometry.FanCardHeight,
                availableHeight,
                DeskGeometry.FanStep);

            double top = DeskGeometry.ComputeFanTop(
                count,
                DeskGeometry.FanCardHeight,
                availableHeight,
                DeskGeometry.FanStep);

            double left = DeskGeometry.ComputeFanCardLeft(
                isOnRight,
                DeskGeometry.ExpandedWidth);

            PositionAddButton(count, isOnRight);

            for (int i = 0; i < count; i++)
            {
                var container =
                    fanCards.ItemContainerGenerator
                        .ContainerFromIndex(i) as ContentPresenter;

                if (container is null)
                    continue;

                container.Width = DeskGeometry.FanCardWidth;
                container.Height = DeskGeometry.FanCardHeight;

                Canvas.SetLeft(container, left);
                Canvas.SetTop(container, top + i * step);
            }
        }

        private void PositionAddButton(int count, bool isOnRight)
        {
            double top = DeskGeometry.ComputeAddButtonTop(
                count,
                DeskGeometry.FanCardHeight,
                GetFanAvailableHeight(),
                DeskGeometry.FanStep);

            double sideInset = Math.Max(
                0,
                DeskGeometry.ComputeTabCenterInset() -
                DeskGeometry.AddButtonSize / 2);

            addNoteButton.Width = DeskGeometry.AddButtonSize;
            addNoteButton.Height = DeskGeometry.AddButtonSize;

            addNoteButton.Margin = isOnRight
                ? new Thickness(0, top, sideInset, 0)
                : new Thickness(sideInset, top, 0, 0);
        }

        private double GetFanAvailableHeight()
        {
            return Math.Max(
                1,
                preferredHeight - DeskGeometry.AddButtonReserve);
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
