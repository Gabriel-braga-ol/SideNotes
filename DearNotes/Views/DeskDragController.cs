using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DearNotes.Services;
using DearNotes.ViewModels;

namespace DearNotes.Views
{
    /// <summary>
    /// Conduz o arraste da pílula com o botão direito, incluindo a captura
    /// do mouse, a troca de monitor e a recuperação de falhas do Win32.
    /// </summary>
    internal sealed class DeskDragController
    {
        private readonly Window window;

        private readonly DeskViewModel viewModel;

        private readonly IScreenService screenService;

        private readonly DeskLayoutService layout;

        private readonly FrameworkElement pillHandle;

        private readonly Action stopCollapseTimer;

        private readonly Action collapseFan;

        private Point dragAnchor;

        public DeskDragController(
            Window window,
            DeskViewModel viewModel,
            IScreenService screenService,
            DeskLayoutService layout,
            FrameworkElement pillHandle,
            Action stopCollapseTimer,
            Action collapseFan)
        {
            this.window = window;
            this.viewModel = viewModel;
            this.screenService = screenService;
            this.layout = layout;
            this.pillHandle = pillHandle;
            this.stopCollapseTimer = stopCollapseTimer;
            this.collapseFan = collapseFan;
        }

        public bool IsDragging { get; private set; }

        public void HandleMouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (IsDragging) return;

            stopCollapseTimer();

            IsDragging = true;

            viewModel.SetExpanded(false);
            collapseFan();

            if (!TryUpdateDragAnchor())
            {
                IsDragging = false;
                return;
            }

            if (!pillHandle.CaptureMouse())
            {
                IsDragging = false;
            }
        }

        public void HandleMouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!IsDragging) return;

            if (e.RightButton != MouseButtonState.Pressed)
            {
                FinishDrag();
                return;
            }

            if (!MoveWithMouse())
            {
                FinishDrag();
                return;
            }

            e.Handled = true;
        }

        public void HandleMouseRightButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!IsDragging) return;

            e.Handled = true;
            FinishDrag();
        }

        public void HandleLostMouseCapture(
            object sender,
            MouseEventArgs e)
        {
            if (IsDragging && !pillHandle.IsMouseCaptured) FinishDrag();
        }

        public bool MoveWithMouse()
        {
            if (!TryGetMousePosition(out Point mouse)) return false;

            try
            {
                Rect bounds = screenService.GetWindowBounds(window);

                Point position = DeskGeometry.ComputeDragPosition(
                    mouse,
                    bounds,
                    dragAnchor);

                return screenService.TryMoveWindow(
                    window,
                    position.X,
                    position.Y);
            }
            catch (Exception ex) when (
                ex is Win32Exception ||
                ex is InvalidOperationException)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Desk: não foi possível mover o Desk: " +
                    ex.Message);

                return false;
            }
        }

        public void Cancel()
        {
            IsDragging = false;

            if (pillHandle.IsMouseCaptured) pillHandle.ReleaseMouseCapture();
        }

        private void FinishDrag()
        {
            if (!IsDragging) return;

            IsDragging = false;

            if (pillHandle.IsMouseCaptured) pillHandle.ReleaseMouseCapture();

            if (!TryGetMousePosition(out Point mouse)) return;

            DeskScreen? nextScreen = DeskLayoutService.TryResolveScreen(
                () => screenService.FromPoint(mouse));

            if (nextScreen is null) return;

            layout.SetCurrentScreen(nextScreen);

            viewModel.SetSide(
                DeskGeometry.IsOnRightSide(
                    nextScreen.WorkArea,
                    mouse.X));

            layout.UpdateSideLayout(viewModel.IsOnRight);
            layout.SnapToEdge(viewModel.IsOnRight);
            layout.SavePosition(viewModel.IsOnRight);
        }

        private bool TryUpdateDragAnchor()
        {
            if (!TryGetMousePosition(out Point mouse)) return false;

            try
            {
                Rect bounds = screenService.GetWindowBounds(window);

                dragAnchor = new Point(
                    Math.Clamp(
                        (mouse.X - bounds.Left) /
                        Math.Max(1, bounds.Width),
                        0,
                        1),
                    Math.Clamp(
                        (mouse.Y - bounds.Top) /
                        Math.Max(1, bounds.Height),
                        0,
                        1));

                return true;
            }
            catch (Exception ex) when (
                ex is Win32Exception ||
                ex is InvalidOperationException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Desk: não foi possível iniciar o arraste: {ex.Message}");

                return false;
            }
        }

        private bool TryGetMousePosition(out Point mouse)
        {
            try
            {
                mouse = screenService.GetMousePosition();
                return true;
            }
            catch (Win32Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Desk: não foi possível obter a posição do cursor: " +
                    ex.Message);

                mouse = default;
                return false;
            }
        }
    }
}
