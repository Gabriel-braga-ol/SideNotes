using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DearNotes.Services;
using DearNotes.ViewModels;

namespace DearNotes.Views
{
    public partial class NoteEditorWindow : Window
    {
        private const double EditorMargin = 12;

        private readonly string monitorDeviceName;

        private readonly IScreenService screenService;

        private bool initialPositionApplied;

        public NoteEditorWindow(
            DeskViewModel viewModel,
            DeskScreen screen,
            IScreenService screenService)
        {
            InitializeComponent();

            DataContext = viewModel;
            monitorDeviceName = screen.DeviceName;
            this.screenService = screenService;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Coloca a janela no monitor correto antes de exibi-la.
            DeskScreen screen =
                screenService.FindOrPrimary(monitorDeviceName);

            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            Rect bounds = screenService.GetWindowBounds(this);

            Rect target = DeskGeometry.ComputeEditorBounds(
                screen.WorkArea,
                new Size(bounds.Width, bounds.Height),
                dpi,
                marginInDips: 0);

            screenService.MoveWindow(this, target.Left, target.Top);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (initialPositionApplied) return;

            initialPositionApplied = true;

            DeskScreen screen =
                screenService.FindOrPrimary(monitorDeviceName);

            DpiScale dpi = VisualTreeHelper.GetDpi(this);

            Rect target = DeskGeometry.ComputeEditorBounds(
                screen.WorkArea,
                new Size(Width, Height),
                dpi,
                EditorMargin);

            screenService.SetWindowBounds(
                this,
                target.Left,
                target.Top,
                target.Width,
                target.Height);

            Opacity = 1;
        }

        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            e.Handled = true;
            DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}