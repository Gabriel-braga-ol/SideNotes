using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DearNotes.Services;
using DearNotes.ViewModels;
using DearNotes.Views;

namespace DearNotes
{
    public partial class App : Application
    {
        private const string SingleInstanceMutexName =
            @"Local\DearNotes.SingleInstance";

        private NotesRepository? notesRepository;

        private Mutex? singleInstanceMutex;

        private void Application_Startup(
            object sender,
            StartupEventArgs e)
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            if (!TryAcquireSingleInstance())
            {
                MessageBox.Show(
                    "O DearNotes já está aberto.\n\n" +
                    "Feche a outra janela antes de iniciar uma nova instância.",
                    "DearNotes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Shutdown();
                return;
            }

            if (!StorageMigration.TryMigrateIfNeeded(out string? migrationError))
            {
                MessageBox.Show(
                    migrationError ?? "Não foi possível migrar as notas.",
                    "DearNotes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
                return;
            }

            MainViewModel viewModel;
            DeskViewModel deskViewModel;

            try
            {
                var noteEncryption = new AesGcmNoteEncryption(
                    new DpapiKeyProvider(AppPaths.NotesKeyFilePath));

                notesRepository = new NotesRepository(
                    new NoteStorage(noteEncryption));

                viewModel = new MainViewModel(notesRepository);
                deskViewModel = new DeskViewModel(notesRepository);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is CryptographicException)
            {
                AppLogger.LogError("Falha ao carregar as notas", ex);

                MessageBox.Show(
                    "Não foi possível carregar ou descriptografar as notas.\n\n" +
                    "O aplicativo será encerrado sem salvar alterações.\n\n" +
                    "Detalhes: " + ex.Message,
                    "DearNotes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
                return;
            }

            MainWindow window = new MainWindow(viewModel);
            window.Show();

            var screenService = new DeskScreenService();
            var deskPositionStore = new DeskPositionStore();

            DeskWindow deskWindow = new DeskWindow(
                deskViewModel,
                screenService,
                deskPositionStore);
            deskWindow.Closed += (_, _) => deskViewModel.Dispose();
            deskWindow.Show();

            window.Closed += (_, _) => deskWindow.Close();
        }

        private bool TryAcquireSingleInstance()
        {
            try
            {
                singleInstanceMutex = new Mutex(
                    initiallyOwned: false,
                    name: SingleInstanceMutexName);

                bool acquired = singleInstanceMutex.WaitOne(
                    TimeSpan.Zero);

                if (acquired) return true;

                singleInstanceMutex.Dispose();
                singleInstanceMutex = null;

                return false;
            }
            catch (AbandonedMutexException exception)
            {
                // A instância anterior terminou sem liberar o Mutex; a posse passa para esta.
                AppLogger.LogWarning(
                    "Mutex abandonado por uma instância anterior: " +
                    exception.Message);

                return true;
            }
            catch (UnauthorizedAccessException exception)
            {
                // Sem acesso ao Mutex, o aplicativo segue sem a proteção em vez de ficar inutilizável.
                AppLogger.LogError(
                    "Falha ao verificar a instância única",
                    exception);

                singleInstanceMutex?.Dispose();
                singleInstanceMutex = null;

                return true;
            }
        }

        private void OnDispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            AppLogger.LogError(
                "Exceção não tratada na interface",
                e.Exception);

            bool notesSaved = TrySavePendingNotes();

            MessageBox.Show(
                "Ocorreu um erro inesperado e o aplicativo será encerrado.\n\n" +
                (notesSaved
                    ? "As notas foram salvas."
                    : "Não foi possível salvar as notas.") +
                "\n\nDetalhes em: " + AppPaths.LogFilePath,
                "DearNotes",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
            Shutdown(1);
        }

        private void OnAppDomainUnhandledException(
            object sender,
            UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                AppLogger.LogError(
                    "Exceção não tratada fora da interface",
                    exception);
                return;
            }

            AppLogger.LogWarning(
                "Uma exceção não tratada fora da interface foi encerrada sem detalhes.");
        }

        private void OnUnobservedTaskException(
            object? sender,
            UnobservedTaskExceptionEventArgs e)
        {
            AppLogger.LogError(
                "Exceção não observada em tarefa",
                e.Exception);

            e.SetObserved();
        }

        private bool TrySavePendingNotes()
        {
            if (notesRepository is null) return true;

            bool saved = notesRepository.TrySaveNotes(
                out string? errorMessage);

            if (!saved && errorMessage is not null)
            {
                AppLogger.LogWarning(errorMessage);
            }

            return saved;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            DispatcherUnhandledException -= OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

            notesRepository?.Dispose();

            if (singleInstanceMutex is not null)
            {
                singleInstanceMutex.ReleaseMutex();
                singleInstanceMutex.Dispose();
                singleInstanceMutex = null;
            }

            base.OnExit(e);
        }
    }
}
