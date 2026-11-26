using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DearNotes.Models;
using DearNotes.Services;
using DearNotes.ViewModels;

namespace DearNotes.Views
{
    public partial class DeskWindow : Window
    {
        private const int WmDisplayChange = 0x007E;

        private const int WmSettingChange = 0x001A;

        private const int SpiSetWorkArea = 0x002F;

        private readonly DeskViewModel viewModel;

        private readonly IScreenService screenService;

        private readonly DeskLayoutService layout;

        private readonly DeskDragController dragController;

        private readonly DeskFanAnimator fanAnimator;

        private readonly DeskKeyboardNavigator keyboardNavigator;

        private NoteEditorWindow? editorWindow;

        private int dpiUpdateVersion;

        private HwndSource? windowSource;

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
                TabDeck,
                FanCards,
                AddNoteButton,
                screenService,
                positionStore,
                Height);

            dragController = new DeskDragController(
                this,
                viewModel,
                screenService,
                layout,
                TabDeck,
                StopCollapseTimer,
                CollapseFan);

            fanAnimator = new DeskFanAnimator(
                viewModel,
                TabDeck,
                FanCards,
                AddNoteButton,
                SetDeskWidth,
                IsPointerInsideDesk,
                IsDragActive);

            keyboardNavigator = new DeskKeyboardNavigator(
                viewModel,
                FanCards,
                TabDeck,
                OpenNote);

            TabDeck.MouseRightButtonDown +=
                dragController.HandleMouseRightButtonDown;
            TabDeck.MouseRightButtonUp +=
                dragController.HandleMouseRightButtonUp;
            TabDeck.MouseMove += dragController.HandleMouseMove;
            TabDeck.LostMouseCapture +=
                dragController.HandleLostMouseCapture;

            MouseEnter += (_, _) => fanAnimator.HandleMouseEnter();
            MouseLeave += (_, _) => fanAnimator.HandleMouseLeave();

            DataContext = viewModel;

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

            fanAnimator.SyncAddButton();
        }

        private void DeskHandle_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (dragController.IsDragging) return;

            TabDeck.Focus();
            fanAnimator.StopCollapseTimer();
            viewModel.SetExpanded(true);
        }

        protected override void OnClosed(EventArgs e)
        {
            ++dpiUpdateVersion;

            fanAnimator.Detach();

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

        private void NoteCard_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.DataContext is not Note note) return;

            e.Handled = true;
            fanAnimator.StopCollapseTimer();

            OpenNote(note);
        }

        private void NoteCard_SlideStateChanged(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement card)
            {
                fanAnimator.UpdateCardSlide(card);
            }
        }

        private void DeskHandle_KeyDown(object sender, KeyEventArgs e)
        {
            keyboardNavigator.HandleDeskKeyDown(sender, e);
        }

        private void NoteCard_KeyDown(object sender, KeyEventArgs e)
        {
            keyboardNavigator.HandleNoteKeyDown(sender, e);
        }

        private void DeskNotes_CollectionChanged(
            object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                layout.PositionNoteCards(viewModel.IsOnRight);

                if (!viewModel.IsExpanded) fanAnimator.SyncAddButton();
            }));
        }

        private void SetDeskWidth(double width)
        {
            layout.SetDeskWidth(
                width,
                viewModel.IsOnRight,
                dragController.IsDragging,
                isWindowReady: windowSource is not null);
        }

        private void StopCollapseTimer()
        {
            fanAnimator.StopCollapseTimer();
        }

        private void CollapseFan()
        {
            fanAnimator.CollapseImmediately();
        }

        private bool IsDragActive()
        {
            return dragController.IsDragging;
        }

        private bool IsPointerInsideDesk()
        {
            try
            {
                Rect bounds = screenService.GetWindowBounds(this);
                Point mouse = screenService.GetMousePosition();

                return bounds.Contains(mouse);
            }
            catch (Exception ex) when (
                ex is Win32Exception ||
                ex is InvalidOperationException)
            {
                // A posição real do ponteiro evita recolher o leque quando a
                // janela se move sob um cursor parado, mas o WPF perde o
                // rastreamento do mouse nesse movimento.
                System.Diagnostics.Debug.WriteLine(
                    "Desk: não foi possível verificar o ponteiro: " +
                    ex.Message);

                return IsMouseOver;
            }
        }

        private void OpenNote(Note note)
        {
            bool alreadyOpened = ReferenceEquals(viewModel.OpenedNote, note);

            viewModel.OpenNote(note);

            if (alreadyOpened) ShowEditor();
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
                    screenService,
                    viewModel.IsOnRight);
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
                    fanAnimator.ApplyExpandedState();
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

            fanAnimator.StopCollapseTimer();
            layout.SetCurrentScreen(nextScreen);

            viewModel.CloseNote();
            viewModel.SetExpanded(false);
            fanAnimator.CollapseImmediately();

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
