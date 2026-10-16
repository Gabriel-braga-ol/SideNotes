using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SideNotes.Services;
using SideNotes.ViewModels;

namespace SideNotes.Views
{
    public partial class NoteEditorWindow : Window
    {
        private readonly string monitorDeviceName;
        private bool initialPositionApplied;

        public NoteEditorWindow(
            DeskViewModel viewModel,
            DeskScreen screen)
        {
            InitializeComponent();

            DataContext = viewModel;
            monitorDeviceName = screen.DeviceName;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Coloca a janela no monitor correto antes de exibi-la.
            DeskScreen screen =
                DeskScreenService.FindOrPrimary(monitorDeviceName);

            Rect area = screen.WorkArea;
            Rect bounds = DeskScreenService.GetWindowBounds(this);

            DeskScreenService.MoveWindow(
                this,
                area.Left + (area.Width - bounds.Width) / 2,
                area.Top + (area.Height - bounds.Height) / 2);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (initialPositionApplied) return;

            initialPositionApplied = true;

            DeskScreen screen =
                DeskScreenService.FindOrPrimary(monitorDeviceName);

            Rect area = screen.WorkArea;
            DpiScale dpi = VisualTreeHelper.GetDpi(this);

            double marginX = 12 * dpi.DpiScaleX;
            double marginY = 12 * dpi.DpiScaleY;

            double widthPixels = Math.Max(
                1,
                Math.Min(Width * dpi.DpiScaleX,
                         area.Width - marginX * 2));

            double heightPixels = Math.Max(
                1,
                Math.Min(Height * dpi.DpiScaleY,
                         area.Height - marginY * 2));

            DeskScreenService.SetWindowBounds(
                this,
                area.Left + (area.Width - widthPixels) / 2,
                area.Top + (area.Height - heightPixels) / 2,
                widthPixels,
                heightPixels);

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