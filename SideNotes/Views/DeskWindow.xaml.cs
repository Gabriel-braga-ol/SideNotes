using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SideNotes.ViewModels;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Collections.Specialized;
using SideNotes.Models;
using SideNotes.Services;
using System.ComponentModel;
using System.Windows.Interop;

namespace SideNotes.Views
{
    public partial class DeskWindow : Window
    {
        private const double CollapsedWidth = 24;

        private const double ExpandedWidth = 256;

        private const double EditorWidth = 576;

        private const double ScreenMargin = 8;

        private double currentVerticalPosition = 0.5;

        private const int WmDisplayChange = 0x007E;

        private const int WmSettingChange = 0x001A;

        private const int SpiSetWorkArea = 0x002F;

        private readonly DeskViewModel viewModel;

        private readonly DeskPositionStore positionStore = new();

        private bool isDragging;

        private int fanAnimationVersion;

        private int editorAnimationVersion;

        private Point dragStartMouse;

        private Point dragStartWindow;

        private DeskScreen? currentScreen;

        private HwndSource? windowSource;

        private readonly double preferredHeight;

        private readonly DispatcherTimer collapseTimer =
            new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(450)
            };

        private readonly DispatcherTimer screenChangeTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };

        public DeskWindow(DeskViewModel viewModel)
        {
            InitializeComponent();

            preferredHeight = Height;

            this.viewModel = viewModel;
            DataContext = viewModel;

            collapseTimer.Tick += CollapseTimer_Tick;
            screenChangeTimer.Tick += ScreenChangeTimer_Tick;

            viewModel.DeskNotes.CollectionChanged += DeskNotes_CollectionChanged;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DeskPosition position = positionStore.Load();

            currentScreen = DeskScreenService.FindOrPrimary(position.MonitorDeviceName);

            Rect area = currentScreen.WorkArea;

            DeskScreenService.MoveWindow(
                this,
                area.Left,
                area.Top);

            viewModel.SetSide(position.IsOnRight);

            UpdateSideLayout();
            SnapToEdge(position.VerticalPosition);
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            collapseTimer.Stop();     
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!isDragging)
            {
                collapseTimer.Stop();
                collapseTimer.Start();
            }
        }

        private void CollapseTimer_Tick(object? sender, EventArgs e)
        {
            collapseTimer.Stop();

            if (!IsMouseOver && !isDragging) viewModel.SetExpanded(false);
        }

        private void AnimateFan(bool expanded)
        {
            if (expanded)
            {
                PillHandle.Opacity = 0;
                PillHandle.IsHitTestVisible = false;

                SetDeskWidth(
                    viewModel.OpenedNote is not null
                        ? EditorWidth
                        : ExpandedWidth);
            }

            int version = ++fanAnimationVersion;

            var transform =
                (TranslateTransform)NoteTabs.RenderTransform;

            double hiddenOffset = viewModel.IsOnRight ? 36 : -36;

            if (expanded)
            {
                if (NoteTabs.Visibility != Visibility.Visible)
                {
                    NoteTabs.Opacity = 0;
                    transform.X = hiddenOffset;
                    NoteTabs.Visibility = Visibility.Visible;
                }

                NoteTabs.IsHitTestVisible = true;
                DeskRoot.Background = Brushes.Transparent;

                Animate(
                    transform,
                    TranslateTransform.XProperty,
                    0,
                    280);

                Animate(
                    NoteTabs,
                    UIElement.OpacityProperty,
                    1,
                    180);
            }
            else
            {
                NoteTabs.IsHitTestVisible = false;

                Animate(
                    transform,
                    TranslateTransform.XProperty,
                    hiddenOffset,
                    200);

                Animate(
                    NoteTabs,
                    UIElement.OpacityProperty,
                    0,
                    200,
                    () =>
                    {
                        if (version != fanAnimationVersion ||
                            viewModel.IsExpanded) return;

                        NoteTabs.Visibility = Visibility.Collapsed;
                        DeskRoot.Background = null;

                        SetDeskWidth(CollapsedWidth);

                        PillHandle.Opacity = 1;
                        PillHandle.IsHitTestVisible = true;
                    });
            }
        }

        private void Pill_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (isDragging) return;    

            collapseTimer.Stop();
            viewModel.SetExpanded(true);
        }

        private void UpdateSideLayout()
        {
            PillHandle.HorizontalAlignment = viewModel.IsOnRight
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;

            NoteTabs.HorizontalAlignment = viewModel.IsOnRight
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;

            NoteEditor.HorizontalAlignment = viewModel.IsOnRight
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

            NoteTabs.Margin = new Thickness(0);

            NoteEditor.Margin = viewModel.IsOnRight
                ? new Thickness(0, 8, ExpandedWidth, 8)
                : new Thickness(ExpandedWidth, 8, 0, 8);
        }

        private void SnapToEdge(double? verticalPosition = null, double? widthInDips = null)
        {
            if (currentScreen is null) return;

            Rect area = currentScreen.WorkArea;
            Rect bounds = DeskScreenService.GetWindowBounds(this);

            var dpi = VisualTreeHelper.GetDpi(this);

            double marginX = Math.Round(ScreenMargin * dpi.DpiScaleX);
            double marginY = Math.Round(ScreenMargin * dpi.DpiScaleY);

            double widthPixels = widthInDips.HasValue
                ? Math.Max(1, Math.Round(widthInDips.Value * dpi.DpiScaleX))
                : bounds.Width;

            double heightPixels = Math.Max(
                1,
                Math.Min(
                    Math.Round(preferredHeight * dpi.DpiScaleY),
                    area.Height - marginY * 2));

            double availableHeight = Math.Max(
                0,
                area.Height - heightPixels - marginY * 2);

            double minTop = area.Top + marginY;

            double top = verticalPosition.HasValue
                ? minTop + availableHeight *
                  Math.Clamp(verticalPosition.Value, 0, 1)
                : Math.Clamp(bounds.Top, minTop, minTop + availableHeight);

            top = Math.Round(top);

            double left = viewModel.IsOnRight
                ? area.Right - widthPixels - marginX
                : area.Left + marginX;

            DeskScreenService.SetWindowBounds(
                this,
                left,
                top,
                widthPixels,
                heightPixels);

            currentVerticalPosition = availableHeight > 0
                ? Math.Clamp((top - minTop) / availableHeight, 0, 1)
                : verticalPosition ?? currentVerticalPosition;
        }

        protected override void OnClosed(EventArgs e)
        {
            ++fanAnimationVersion;
            ++editorAnimationVersion;

            collapseTimer.Stop();
            collapseTimer.Tick -= CollapseTimer_Tick;

            viewModel.DeskNotes.CollectionChanged -= DeskNotes_CollectionChanged;
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            screenChangeTimer.Stop();
            screenChangeTimer.Tick -= ScreenChangeTimer_Tick;

            windowSource?.RemoveHook(WindowMessageHook);
            windowSource = null;

            base.OnClosed(e);
        }

        private void NoteTab_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.DataContext is not Note note) return;   

            e.Handled = true;
            collapseTimer.Stop();

            viewModel.OpenNote(note);
        }

        private void CloseNote_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CloseNote();
        }

        private void ShowEditor()
        {
            ++editorAnimationVersion;

            bool wasVisible =
                NoteEditor.Visibility == Visibility.Visible;

            NoteEditor.IsHitTestVisible = true;

            UpdateSideLayout();
            AnimateFan(true);

            var transform =
                (TranslateTransform)NoteEditor.RenderTransform;

            if (!wasVisible)
            {
                NoteEditor.Opacity = 0;
                transform.X = viewModel.IsOnRight ? 24 : -24;
            }

            NoteEditor.Visibility = Visibility.Visible;

            Animate(
                transform,
                TranslateTransform.XProperty,
                0,
                280);

            Animate(
                NoteEditor,
                UIElement.OpacityProperty,
                1,
                180);
        }

        private void AnimateEditorClose()
        {
            int version = ++editorAnimationVersion;

            // Invalida um fechamento anterior do leque.
            ++fanAnimationVersion;

            collapseTimer.Stop();

            if (NoteEditor.IsKeyboardFocusWithin) Keyboard.ClearFocus();

            NoteEditor.IsHitTestVisible = false;
            NoteTabs.IsHitTestVisible = false;

            const int duration = 180;

            var editorTransform =
                (TranslateTransform)NoteEditor.RenderTransform;

            var tabsTransform =
                (TranslateTransform)NoteTabs.RenderTransform;

            Animate(
                tabsTransform,
                TranslateTransform.XProperty,
                viewModel.IsOnRight ? 36 : -36,
                duration);

            Animate(
                NoteTabs,
                UIElement.OpacityProperty,
                0,
                duration);

            Animate(
                editorTransform,
                TranslateTransform.XProperty,
                viewModel.IsOnRight ? 24 : -24,
                duration);

            Animate(
                NoteEditor,
                UIElement.OpacityProperty,
                0,
                duration,
                () =>
                {
                    if (version != editorAnimationVersion ||
                        !viewModel.IsClosing) return;

                    NoteTabs.Visibility = Visibility.Collapsed;

                    viewModel.CompleteCloseNote();
                });
        }

        private void HideEditorImmediately()
        {
            ++editorAnimationVersion;

            if (NoteEditor.IsKeyboardFocusWithin) Keyboard.ClearFocus();

            NoteEditor.IsHitTestVisible = false;
            NoteEditor.Visibility = Visibility.Collapsed;

            NoteEditor.BeginAnimation(
                UIElement.OpacityProperty, null);

            var transform =
                (TranslateTransform)NoteEditor.RenderTransform;

            transform.BeginAnimation(
                TranslateTransform.XProperty, null);
        }

        private void CollapseImmediately()
        {
            ++fanAnimationVersion;

            NoteTabs.BeginAnimation(
                UIElement.OpacityProperty, null);

            var transform =
                (TranslateTransform)NoteTabs.RenderTransform;

            transform.BeginAnimation(
                TranslateTransform.XProperty, null);

            NoteTabs.Opacity = 0;
            NoteTabs.IsHitTestVisible = false;
            NoteTabs.Visibility = Visibility.Collapsed;

            DeskRoot.Background = null;

            SetDeskWidth(CollapsedWidth);

            PillHandle.Opacity = 1;
            PillHandle.IsHitTestVisible = true;
        }

        private void DeskNotes_CollectionChanged(
            object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(PositionNoteCards));
        }

        private static void Animate(
            DependencyObject target,
            DependencyProperty property,
            double destination,
            int milliseconds,
            Action? completed = null)
        {
            double current = (double)target.GetValue(property);

            target.SetValue(property, destination);

            var animation = new DoubleAnimation
            {
                From = current,
                To = destination,
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                FillBehavior = FillBehavior.Stop,
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            if (completed is not null)
            {
                animation.Completed += (_, _) => completed();
            }

            if (target is UIElement element)
            {
                element.BeginAnimation(
                    property,
                    animation,
                    HandoffBehavior.SnapshotAndReplace);
            }
            else if (target is Animatable animatable)
            {
                animatable.BeginAnimation(
                    property,
                    animation,
                    HandoffBehavior.SnapshotAndReplace);
            }
        }

        private void SetDeskWidth(double width)
        {
            if (windowSource is null)
            {
                Width = width;
                return;
            }

            if (isDragging || currentScreen is null)
            {
                Rect bounds = DeskScreenService.GetWindowBounds(this);

                var dpi = VisualTreeHelper.GetDpi(this);

                double widthPixels = Math.Max(
                    1,
                    Math.Round(width * dpi.DpiScaleX));

                double left = viewModel.IsOnRight
                    ? bounds.Right - widthPixels
                    : bounds.Left;

                DeskScreenService.SetWindowBounds(
                    this,
                    left,
                    bounds.Top,
                    widthPixels,
                    bounds.Height);
            }
            else
            {
                SnapToEdge(currentVerticalPosition, width);
            }

            UpdateLayout();
        }

        private void NoteContainer_Loaded(object sender, RoutedEventArgs e)
        {
            PositionNoteCards();
        }

        private void PositionNoteCards()
        {
            const double spacing = 54;
            const double cardHeight = 160;
            const double padding = 12;

            int count = NoteTabs.Items.Count;

            NoteTabs.Height =
                Math.Max(0, count - 1) * spacing
                + cardHeight
                + padding * 2;

            for (int i = 0; i < count; i++)
            {
                var container =
                    NoteTabs.ItemContainerGenerator
                        .ContainerFromIndex(i) as ContentPresenter;

                if (container is null) continue;

                Canvas.SetLeft(container, 4);
                Canvas.SetTop(container, padding + i * spacing);
            }
        }

        private void SaveDeskPosition()
        {
            if (currentScreen is null) return;

            Rect area = currentScreen.WorkArea;
            Rect bounds = DeskScreenService.GetWindowBounds(this);

            var dpi = VisualTreeHelper.GetDpi(this);

            double marginY = ScreenMargin * dpi.DpiScaleY;

            double availableHeight = Math.Max(
                0,
                area.Height - bounds.Height - marginY * 2);

            double verticalPosition = availableHeight > 0
                ? (bounds.Top - area.Top - marginY) / availableHeight
                : 0.5;

            positionStore.Save(new DeskPosition
            {
                MonitorDeviceName = currentScreen.DeviceName,
                IsOnRight = viewModel.IsOnRight,
                VerticalPosition = Math.Clamp(verticalPosition, 0, 1)
            });
        }

        private void Pill_MouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (isDragging) return;

            collapseTimer.Stop();
            isDragging = true;

            viewModel.CloseNote(immediate: true);

            HideEditorImmediately();
            CollapseImmediately();

            dragStartMouse = DeskScreenService.GetMousePosition();

            Rect bounds = DeskScreenService.GetWindowBounds(this);

            dragStartWindow = bounds.TopLeft;

            if (!PillHandle.CaptureMouse())
            {
                isDragging = false;
            }
        }

        private void Pill_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!isDragging) return;

            if (e.RightButton != MouseButtonState.Pressed)
            {
                FinishPillDrag();
                return;
            }

            Point currentMouse = DeskScreenService.GetMousePosition();

            Vector movement = currentMouse - dragStartMouse;

            DeskScreenService.MoveWindow(
                this,
                dragStartWindow.X + movement.X,
                dragStartWindow.Y + movement.Y);

            e.Handled = true;
        }

        private void Pill_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging) return;

            e.Handled = true;
            FinishPillDrag();
        }

        private void Pill_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (isDragging && !PillHandle.IsMouseCaptured) FinishPillDrag();
        }

        private void FinishPillDrag()
        {
            if (!isDragging) return;

            isDragging = false;

            if (PillHandle.IsMouseCaptured) PillHandle.ReleaseMouseCapture();

            Point mouse = DeskScreenService.GetMousePosition();

            currentScreen = DeskScreenService.FromPoint(mouse);

            Rect area = currentScreen.WorkArea;

            double screenCenter = area.Left + area.Width / 2;

            viewModel.SetSide(mouse.X >= screenCenter);

            UpdateSideLayout();
            SnapToEdge();
            SaveDeskPosition();
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DeskViewModel.OpenedNote):
                    if (viewModel.OpenedNote is null)
                    {
                        HideEditorImmediately();
                    }
                    else
                    {
                        ShowEditor();
                    }
                    break;

                case nameof(DeskViewModel.IsClosing):
                    if (viewModel.IsClosing)
                    {
                        AnimateEditorClose();
                    }
                    else if (viewModel.OpenedNote is not null)
                    {
                        // Restaura o editor se o fechamento foi cancelado.
                        ShowEditor();
                    }
                    break;

                case nameof(DeskViewModel.IsExpanded):
                    if (isDragging ||
                        (!viewModel.IsExpanded &&
                         NoteTabs.Visibility == Visibility.Collapsed))
                    {
                        CollapseImmediately();
                    }
                    else
                    {
                        AnimateFan(viewModel.IsExpanded);
                    }
                    break;

                case nameof(DeskViewModel.IsOnRight):
                    UpdateSideLayout();
                    SnapToEdge();
                    break;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            windowSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);

            windowSource?.AddHook(WindowMessageHook);
        }

        private IntPtr WindowMessageHook(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            bool displayChanged = message == WmDisplayChange;

            bool workAreaChanged =
                message == WmSettingChange &&
                wParam.ToInt64() == SpiSetWorkArea;

            if (displayChanged || workAreaChanged)
            {
                screenChangeTimer.Stop();
                screenChangeTimer.Start();
            }

            return IntPtr.Zero;
        }

        private void ScreenChangeTimer_Tick(object? sender, EventArgs e)
        {
            screenChangeTimer.Stop();

            if (currentScreen is null) return;

            double verticalPosition = currentVerticalPosition;

            DeskScreen nextScreen = DeskScreenService.FindOrPrimary(currentScreen.DeviceName);

            isDragging = false;

            if (PillHandle.IsMouseCaptured) PillHandle.ReleaseMouseCapture();

            collapseTimer.Stop();
            currentScreen = nextScreen;

            viewModel.CloseNote(immediate: true);

            HideEditorImmediately();
            CollapseImmediately();

            UpdateSideLayout();
            SnapToEdge(verticalPosition);
        }
    }
}