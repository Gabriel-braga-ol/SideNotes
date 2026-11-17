using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DearNotes.ViewModels;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Collections.Specialized;
using DearNotes.Models;
using DearNotes.Services;
using System.ComponentModel;
using System.Windows.Interop;

namespace DearNotes.Views
{
    public partial class DeskWindow : Window
    {
        private const double CollapsedWidth = DeskGeometry.CollapsedWidth;

        private const double ExpandedWidth = DeskGeometry.ExpandedWidth;

        private double currentVerticalPosition = 0.5;

        private const int WmDisplayChange = 0x007E;

        private const int WmSettingChange = 0x001A;

        private const int SpiSetWorkArea = 0x002F;

        private readonly DeskViewModel viewModel;

        private readonly IScreenService screenService;

        private readonly IDeskPositionStore positionStore;

        private bool isDragging;

        private int fanAnimationVersion;

        private NoteEditorWindow? editorWindow;

        private Point dragAnchor;

        private double requestedDeskWidth = CollapsedWidth;

        private int dpiUpdateVersion;

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

        public DeskWindow(
            DeskViewModel viewModel,
            IScreenService screenService,
            IDeskPositionStore positionStore)
        {
            InitializeComponent();

            preferredHeight = Height;

            this.viewModel = viewModel;
            this.screenService = screenService;
            this.positionStore = positionStore;
            DataContext = viewModel;

            collapseTimer.Tick += CollapseTimer_Tick;
            screenChangeTimer.Tick += ScreenChangeTimer_Tick;

            viewModel.DeskNotes.CollectionChanged += DeskNotes_CollectionChanged;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Desk: janela carregada.");

            DeskPosition position = positionStore.Load();

            currentScreen = screenService.FindOrPrimary(position.MonitorDeviceName);

            System.Diagnostics.Debug.WriteLine(
                $"Desk: monitor = {currentScreen.DeviceName}, " +
                $"escala = {currentScreen.Dpi.DpiScaleX * 100:0}%");

            Rect area = currentScreen.WorkArea;

            screenService.TryMoveWindow(
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

            NoteTabs.Margin = new Thickness(0); 

            PositionNoteCards();
        }

        private void SnapToEdge(double? verticalPosition = null, double? widthInDips = null)
        {
            if (currentScreen is null) return;

            DpiScale dpi = currentScreen.Dpi;

            Rect bounds = screenService.GetWindowBounds(this);

            Rect target = DeskGeometry.ComputeSnapBounds(
                currentScreen.WorkArea,
                dpi,
                widthInDips ?? requestedDeskWidth,
                preferredHeight,
                viewModel.IsOnRight,
                verticalPosition,
                bounds.Top);

            if (!screenService.TrySetWindowBounds(
                    this,
                    target.Left,
                    target.Top,
                    target.Width,
                    target.Height))
            {
                return;
            }

            currentVerticalPosition = DeskGeometry.ComputeNormalizedTop(
                currentScreen.WorkArea,
                target,
                dpi,
                verticalPosition ?? currentVerticalPosition);
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
            requestedDeskWidth = width;

            if (windowSource is null)
            {
                Width = width;
                return;
            }

            if (isDragging || currentScreen is null)
            {
                Rect bounds = screenService.GetWindowBounds(this);

                DpiScale dpi = currentScreen?.Dpi ??
                    VisualTreeHelper.GetDpi(this);

                double widthPixels = Math.Max(
                    1,
                    Math.Round(width * dpi.DpiScaleX));

                double left = viewModel.IsOnRight
                    ? bounds.Right - widthPixels
                    : bounds.Left;

                screenService.TrySetWindowBounds(
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

        private void ShowEditor()
        {
            if (viewModel.OpenedNote is null || currentScreen is null) return;

            if (editorWindow is null)
            {
                editorWindow = new NoteEditorWindow(
                    viewModel,
                    currentScreen,
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
            PositionNoteCards();
        }

        private void PositionNoteCards()
        {
            const double spacing = 58;
            const double cardHeight = 160;
            const double padding = 12;
            const double maxAngle = 2;
            const double maxOffset = 4;

            int count = NoteTabs.Items.Count;

            NoteTabs.Height =
                Math.Max(0, count - 1) * spacing
                + cardHeight
                + padding * 2;

            double direction = viewModel.IsOnRight ? -1 : 1;

            for (int i = 0; i < count; i++)
            {
                var container =
                    NoteTabs.ItemContainerGenerator
                        .ContainerFromIndex(i) as ContentPresenter;

                if (container is null)
                    continue;

                double progress = count <= 1
                    ? 0.5
                    : (double)i / (count - 1);

                double angle = count <= 1
                    ? 0
                    : (progress * 2 - 1) * maxAngle;

                double offset = count <= 1
                    ? 0
                    : progress * maxOffset; 

                container.Tag = angle * direction;

                Canvas.SetLeft(container, 4 + offset * direction);
                Canvas.SetTop(container, padding + i * spacing);
            }
        }

        private void SaveDeskPosition()
        {
            if (currentScreen is null) return;

            Rect bounds = screenService.GetWindowBounds(this);

            DpiScale dpi = currentScreen.Dpi;

            positionStore.Save(new DeskPosition
            {
                MonitorDeviceName = currentScreen.DeviceName,
                IsOnRight = viewModel.IsOnRight,
                VerticalPosition = DeskGeometry.ComputeNormalizedTop(
                    currentScreen.WorkArea,
                    bounds,
                    dpi,
                    fallback: 0.5)
            });
        }

        private bool MoveDeskWithMouse()
        {
            if (!TryGetMousePosition(out Point mouse)) return false;

            try
            {
                Rect bounds = screenService.GetWindowBounds(this);

                Point position = DeskGeometry.ComputeDragPosition(
                    mouse,
                    bounds,
                    dragAnchor);

                return screenService.TryMoveWindow(
                    this,
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

        private void Pill_MouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (isDragging) return;

            collapseTimer.Stop();
            isDragging = true;

            viewModel.SetExpanded(false);
            CollapseImmediately();

            if (!TryUpdateDragAnchor())
            {
                isDragging = false;
                return;
            }

            if (!PillHandle.CaptureMouse())
            {
                isDragging = false;
            }
        }

        private bool TryUpdateDragAnchor()
        {
            if (!TryGetMousePosition(out Point mouse)) return false;

            try
            {
                Rect bounds = screenService.GetWindowBounds(this);

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

        private void Pill_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            if (e.RightButton != MouseButtonState.Pressed)
            {
                FinishPillDrag();
                return;
            }

            if (!MoveDeskWithMouse())
            {
                FinishPillDrag();
                return;
            }

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

            if (!TryGetMousePosition(out Point mouse)) return;

            DeskScreen? nextScreen = TryFindScreen(
                () => screenService.FromPoint(mouse));

            if (nextScreen is null) return;

            currentScreen = nextScreen;

            viewModel.SetSide(
                DeskGeometry.IsOnRightSide(
                    currentScreen.WorkArea,
                    mouse.X));

            UpdateSideLayout();
            SnapToEdge();
            SaveDeskPosition();
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

        private DeskScreen? TryFindScreen(Func<DeskScreen> findScreen)
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

        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DeskViewModel.OpenedNote):
                    if (viewModel.OpenedNote is null)CloseEditorWindow();

                    else ShowEditor();

                    break;

                case nameof(DeskViewModel.IsExpanded):
                    if (isDragging || (!viewModel.IsExpanded && NoteTabs.Visibility == Visibility.Collapsed))
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

            DeskScreen? nextScreen = TryFindScreen(
                () => screenService.FindOrPrimary(
                    currentScreen.DeviceName));

            if (nextScreen is null) return;

            isDragging = false;

            if (PillHandle.IsMouseCaptured) PillHandle.ReleaseMouseCapture();

            collapseTimer.Stop();
            currentScreen = nextScreen;

            viewModel.CloseNote();
            viewModel.SetExpanded(false);
            CollapseImmediately();

            UpdateSideLayout();
            SnapToEdge(verticalPosition);
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
                    if (version != dpiUpdateVersion || !IsLoaded || windowSource is null || currentScreen is null) return;

                    if (isDragging)
                    {
                        MoveDeskWithMouse();
                        return;
                    }

                    DeskScreen? nextScreen = TryFindScreen(
                        () => screenService.FindOrPrimary(
                            currentScreen.DeviceName));

                    if (nextScreen is null) return;

                    currentScreen = nextScreen;

                    SnapToEdge(
                        currentVerticalPosition,
                        requestedDeskWidth);
                }));
        }
    }
}