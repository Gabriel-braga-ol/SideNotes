using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DearNotes.Models;
using DearNotes.Services;
using DearNotes.ViewModels;

namespace DearNotes.Views
{
    public partial class DeskWindow : Window
    {
        private const double CollapsedWidth = DeskGeometry.CollapsedWidth;

        private const double ExpandedWidth = DeskGeometry.ExpandedWidth;

        private const int WmDisplayChange = 0x007E;

        private const int WmSettingChange = 0x001A;

        private const int SpiSetWorkArea = 0x002F;

        private readonly DeskViewModel viewModel;

        private readonly IScreenService screenService;

        private readonly DeskLayoutService layout;

        private readonly DeskDragController dragController;

        private int fanAnimationVersion;

        private NoteEditorWindow? editorWindow;

        private int dpiUpdateVersion;

        private HwndSource? windowSource;

        private readonly DispatcherTimer collapseTimer =
            new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(450)
            };

        private readonly DispatcherTimer screenChangeTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };

        public DeskWindow(
            DeskViewModel viewModel,
            IScreenService screenService,
            IDeskPositionStore positionStore)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.screenService = screenService;

            layout = new DeskLayoutService(
                this,
                PillHandle,
                NoteTabs,
                screenService,
                positionStore,
                Height);

            dragController = new DeskDragController(
                this,
                viewModel,
                screenService,
                layout,
                PillHandle,
                collapseTimer.Stop,
                CollapseImmediately);

            PillHandle.MouseRightButtonDown +=
                dragController.HandleMouseRightButtonDown;
            PillHandle.MouseRightButtonUp +=
                dragController.HandleMouseRightButtonUp;
            PillHandle.MouseMove += dragController.HandleMouseMove;
            PillHandle.LostMouseCapture +=
                dragController.HandleLostMouseCapture;

            DataContext = viewModel;

            collapseTimer.Tick += CollapseTimer_Tick;
            screenChangeTimer.Tick += ScreenChangeTimer_Tick;

            viewModel.DeskNotes.CollectionChanged += DeskNotes_CollectionChanged;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Desk: janela carregada.");

            DeskPosition position = layout.Initialize();

            if (layout.CurrentScreen is not DeskScreen screen) return;

            System.Diagnostics.Debug.WriteLine(
                $"Desk: monitor = {screen.DeviceName}, " +
                $"escala = {screen.Dpi.DpiScaleX * 100:0}%");

            viewModel.SetSide(position.IsOnRight);

            layout.UpdateSideLayout(viewModel.IsOnRight);
            layout.SnapToEdge(viewModel.IsOnRight, position.VerticalPosition);
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            collapseTimer.Stop();
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!dragController.IsDragging)
            {
                collapseTimer.Stop();
                collapseTimer.Start();
            }
        }

        private void CollapseTimer_Tick(object? sender, EventArgs e)
        {
            collapseTimer.Stop();

            if (!IsMouseOver && !dragController.IsDragging)
                viewModel.SetExpanded(false);
        }

        private void AnimateFan(bool expanded)
        {
            if (expanded)
            {
                PillHandle.Opacity = 0;
                PillHandle.IsHitTestVisible = false;

                SetDeskWidth(ExpandedWidth);
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

            if (dragController.IsDragging) return;

            collapseTimer.Stop();
            viewModel.SetExpanded(true);
        }

        protected override void OnClosed(EventArgs e)
        {
            ++dpiUpdateVersion;
            ++fanAnimationVersion;

            collapseTimer.Stop();
            collapseTimer.Tick -= CollapseTimer_Tick;

            viewModel.DeskNotes.CollectionChanged -= DeskNotes_CollectionChanged;
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            CloseEditorWindow();
            viewModel.CloseNote();

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

            bool alreadyOpened = ReferenceEquals(viewModel.OpenedNote, note);

            viewModel.OpenNote(note);

            if (alreadyOpened) ShowEditor();
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
            Dispatcher.BeginInvoke(
                new Action(() => layout.PositionNoteCards(viewModel.IsOnRight)));
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
            layout.SetDeskWidth(
                width,
                viewModel.IsOnRight,
                dragController.IsDragging,
                isWindowReady: windowSource is not null);
        }

        private void ShowEditor()
        {
            if (viewModel.OpenedNote is null ||
                layout.CurrentScreen is not DeskScreen screen) return;

            if (editorWindow is null)
            {
                editorWindow = new NoteEditorWindow(
                    viewModel,
                    screen,
                    screenService);
                editorWindow.Closed += EditorWindow_Closed;
                editorWindow.Show();
            }

            if (editorWindow.WindowState == WindowState.Minimized)
            {
                editorWindow.WindowState = WindowState.Normal;
            }

            editorWindow.Activate();
        }

        private void CloseEditorWindow()
        {
            if (editorWindow is null) return;

            NoteEditorWindow window = editorWindow;
            editorWindow = null;

            window.Closed -= EditorWindow_Closed;
            window.Close();
        }

        private void EditorWindow_Closed(object? sender, EventArgs e)
        {
            if (!ReferenceEquals(sender, editorWindow)) return;

            editorWindow!.Closed -= EditorWindow_Closed;
            editorWindow = null;

            viewModel.CloseNote();
        }

        private void NoteContainer_Loaded(object sender, RoutedEventArgs e)
        {
            layout.PositionNoteCards(viewModel.IsOnRight);
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DeskViewModel.OpenedNote):
                    if (viewModel.OpenedNote is null) CloseEditorWindow();

                    else ShowEditor();

                    break;

                case nameof(DeskViewModel.IsExpanded):
                    if (dragController.IsDragging || (!viewModel.IsExpanded && NoteTabs.Visibility == Visibility.Collapsed))
                    {
                        CollapseImmediately();
                    }
                    else
                    {
                        AnimateFan(viewModel.IsExpanded);
                    }

                    break;

                case nameof(DeskViewModel.IsOnRight):
                    layout.UpdateSideLayout(viewModel.IsOnRight);
                    layout.SnapToEdge(viewModel.IsOnRight);
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

            if (layout.CurrentScreen is not DeskScreen screen) return;

            double verticalPosition = layout.VerticalPosition;

            DeskScreen? nextScreen = DeskLayoutService.TryResolveScreen(
                () => screenService.FindOrPrimary(
                    screen.DeviceName));

            if (nextScreen is null) return;

            dragController.Cancel();

            collapseTimer.Stop();
            layout.SetCurrentScreen(nextScreen);

            viewModel.CloseNote();
            viewModel.SetExpanded(false);
            CollapseImmediately();

            layout.UpdateSideLayout(viewModel.IsOnRight);
            layout.SnapToEdge(viewModel.IsOnRight, verticalPosition);
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);

            System.Diagnostics.Debug.WriteLine(
                $"Desk DPI: " +
                $"{oldDpi.DpiScaleX * 100:0}% → " +
                $"{newDpi.DpiScaleX * 100:0}%");

            int version = ++dpiUpdateVersion;

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    if (version != dpiUpdateVersion ||
                        !IsLoaded ||
                        windowSource is null ||
                        layout.CurrentScreen is not DeskScreen screen) return;

                    if (dragController.IsDragging)
                    {
                        dragController.MoveWithMouse();
                        return;
                    }

                    DeskScreen? nextScreen = DeskLayoutService.TryResolveScreen(
                        () => screenService.FindOrPrimary(
                            screen.DeviceName));

                    if (nextScreen is null) return;

                    layout.SetCurrentScreen(nextScreen);

                    layout.SnapToEdge(
                        viewModel.IsOnRight,
                        layout.VerticalPosition,
                        layout.RequestedWidth);
                }));
        }
    }
}
