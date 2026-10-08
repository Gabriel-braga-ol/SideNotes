using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using SideNotes.Models;
using SideNotes.ViewModels;

namespace SideNotes.Views
{
    
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel;


        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;

            viewModel.SaveFailed += ViewModel_SaveFailed;
            Closed += MainWindow_Closed;
            
            DataContext = viewModel;

            Loaded += MainWindow_Loaded;
        }
        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            Note note = viewModel.CreateNote();
            
            NotesList.ScrollIntoView(note);

            TitleTextBox.Focus();
        }     

        private void  Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                viewModel.SaveCurrentNote();
                e.Handled = true;
                return;
            }


            if (e.Key == Key.Delete && NotesList.IsKeyboardFocusWithin && viewModel.SelectedNote is not null)
            {
                viewModel.DeleteSelectedNote();
            }
        }              
        
        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (!viewModel.TrySaveNotes(out _)) //out _ descarta a mensagem devolvida pelo método
            {
                MessageBoxResult answer = MessageBox.Show(
                    this,
                    "Não foi possível salvar as notas. \n\n" +
                    "Deseja fechar mesmo assim? " +
                    "As alterações não gravadas serão perdidas.", "Falha ao salvar", 
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                e.Cancel = answer != MessageBoxResult.Yes;
            }
        }

        private void ViewModel_SaveFailed(string message)
        {
            MessageBox.Show(
                this,
                message,
                "Falha ao salvar",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            viewModel.StopAutoSave();
            viewModel.SaveFailed -= ViewModel_SaveFailed;
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (viewModel.LoadWarningMessage is not null)
            {
                MessageBox.Show(
                    this,
                    viewModel.LoadWarningMessage,
                    "SideNotes");
            }
        }

        private void ToggleDeskPin_Click(object sender, RoutedEventArgs e)
        {
            if (!viewModel.TryToggleSelectedNoteDeskPin(out string? message))
            {
                MessageBox.Show(
                    this,
                    message ?? "Não foi possível alterar a fixação da nota.",
                    "SideNotes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}