using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace SideNotes
{
    public partial class DeskWindow : Window
    {
        private string? initializationError;

        public DeskWindow()
        {
            InitializeComponent();

            SourceInitialized += DeskWindow_SourceInitialized;
            Loaded += DeskWindow_Loaded;
        }

        private void DeskWindow_SourceInitialized(
            object? sender,
            EventArgs e)
        {
            try
            {
                IntPtr handle =
                    new WindowInteropHelper(this).Handle;

                HwndSource? source = HwndSource.FromHwnd(handle);

                if (source?.CompositionTarget is HwndTarget target)
                {
                    target.RenderMode = RenderMode.SoftwareOnly;
                }

                //DesktopIntegration.AttachToDesktop(handle);
            }
            catch (Exception ex) when (
                ex is Win32Exception ||
                ex is InvalidOperationException)
            {
                initializationError = ex.Message;
            }
        }

        private void DeskWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= DeskWindow_Loaded;

            if (initializationError is null)
            {
                return;
            }

            Close();

            MessageBox.Show(
                "Não foi possível conectar o protótipo " +
                "à área de trabalho.\n\n" + initializationError,
                "SideNotes — Desk",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void TestClick_Click(
            object sender,
            RoutedEventArgs e)
        {
            ((Button)sender).Content = "Clique recebido!";
        }
    }
}