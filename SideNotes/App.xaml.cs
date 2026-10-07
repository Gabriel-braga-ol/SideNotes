using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.IO;
using System.ComponentModel;
using System.Windows.Interop;

namespace SideNotes
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainViewModel viewModel;

            try
            {
                viewModel = new MainViewModel();
            }
            catch (Exception ex) when (
                ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Não foi possível carregar as notas ou preservar uma cópia " +
                    "do arquivo inválido.\n\n" +
                    "O aplicativo será encerrado sem salvar alterações.\n\n" +
                    "Detalhes: " + ex.Message,
                    "SideNotes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Shutdown();
                return;
            }
            
            MainWindow window = new MainWindow(viewModel);
            window.Show();

            window.Closed += (_, _) =>
            {
                deskSource?.Dispose();
                deskSource = null;
            };

            try
            {
                IntPtr mainWindowHandle =
                    new WindowInteropHelper(window).Handle;

                deskSource =
                    DesktopIntegration.CreateDeskPrototype(mainWindowHandle);
            }
            catch (Exception ex) when (
                ex is Win32Exception ||
                ex is InvalidOperationException)
            {
                MessageBox.Show(
                    window,
                    "Não foi possível criar o protótipo Desk.\n\n" +
                    ex.Message,
                    "SideNotes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private HwndSource? deskSource;
    }

}
