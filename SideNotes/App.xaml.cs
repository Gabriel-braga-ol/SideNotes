using System;
using System.IO;
using System.Windows;
using SideNotes.Services;
using SideNotes.ViewModels;
using SideNotes.Views;

namespace SideNotes
{
    public partial class App : Application
    {
        private NotesRepository? notesRepository;

        private void Application_Startup(
            object sender,
            StartupEventArgs e)
        {
            MainViewModel viewModel;

            try
            {
                notesRepository = new NotesRepository();
                viewModel = new MainViewModel(notesRepository);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
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

            DeskWindow deskWindow = new DeskWindow(viewModel);
            deskWindow.Show();

            window.Closed += (_, _) => deskWindow.Close();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notesRepository?.Dispose();

            base.OnExit(e);
        }
    }
}