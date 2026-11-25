using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DearNotes.Models;
using DearNotes.Services;
using DearNotes.ViewModels;

namespace DearNotes.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel;

        private bool isClosing;

        private bool closeConfirmed;

        public MainWindow(
            MainViewModel viewModel,
            WindowFrameService windowFrameService)
        {
            ArgumentNullException.ThrowIfNull(windowFrameService);

            InitializeComponent();

            this.viewModel = viewModel;

            viewModel.NoteCreated += ViewModel_NoteCreated;

            Closed += MainWindow_Closed;
            Loaded += MainWindow_Loaded;

            windowFrameService.Attach(this);

            DataContext = viewModel;
        }

        private void MinimizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                viewModel.SaveCurrentNote();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete &&
                NotesList.IsKeyboardFocusWithin &&
                viewModel.SelectedNote is not null)
            {
                viewModel.DeleteSelectedNote();
            }
        }

        private async void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (closeConfirmed) return;

            e.Cancel = true;

            if (isClosing) return;

            isClosing = true;

            if (!await viewModel.ConfirmCloseAsync())
            {
                isClosing = false;
                return;
            }

            closeConfirmed = true;
            Close();
        }

        private void ViewModel_NoteCreated(Note note)
        {
            NotesList.ScrollIntoView(note);
            TitleTextBox.Focus();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.ShowLoadWarningIfAny();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            viewModel.NoteCreated -= ViewModel_NoteCreated;

            viewModel.Dispose();
        }
    }
}
