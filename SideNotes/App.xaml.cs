using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.IO;

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
        }
    }

}
