using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DearNotes.Models;
using DearNotes.ViewModels;

namespace DearNotes.Views
{
    /// <summary>
    /// Navegação por teclado do Desk: abertura e recolhimento do leque,
    /// abertura de notas, foco entre as abas e retorno à pílula.
    /// </summary>
    internal sealed class DeskKeyboardNavigator
    {
        private const string NoteCardName = "NoteCard";

        private readonly DeskViewModel viewModel;

        private readonly ItemsControl noteTabs;

        private readonly FrameworkElement pillHandle;

        private readonly Action<Note> openNote;

        public DeskKeyboardNavigator(
            DeskViewModel viewModel,
            ItemsControl noteTabs,
            FrameworkElement pillHandle,
            Action<Note> openNote)
        {
            this.viewModel = viewModel;
            this.noteTabs = noteTabs;
            this.pillHandle = pillHandle;
            this.openNote = openNote;
        }

        public void HandlePillKeyDown(
            object sender,
            KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Space:
                    e.Handled = true;
                    ToggleFan();
                    break;

                case Key.Escape:
                    e.Handled = true;
                    viewModel.SetExpanded(false);
                    break;
            }
        }

        public void HandleNoteKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.DataContext is not Note note) return;

            switch (e.Key)
            {
                case Key.Enter:
                case Key.Space:
                    e.Handled = true;
                    openNote(note);
                    break;

                case Key.Escape:
                    e.Handled = true;
                    CollapseAndFocusPill();
                    break;

                case Key.Left:
                case Key.Up:
                    e.Handled = true;
                    MoveFocus(note, -1);
                    break;

                case Key.Right:
                case Key.Down:
                    e.Handled = true;
                    MoveFocus(note, 1);
                    break;
            }
        }

        private void ToggleFan()
        {
            if (viewModel.IsExpanded)
            {
                viewModel.SetExpanded(false);
                return;
            }

            viewModel.SetExpanded(true);
            FocusNoteDelayed(0);
        }

        private void CollapseAndFocusPill()
        {
            viewModel.SetExpanded(false);
            pillHandle.Focus();
        }

        private void MoveFocus(Note note, int delta)
        {
            int count = noteTabs.Items.Count;

            if (count == 0) return;

            int index = noteTabs.Items.IndexOf(note);

            if (index < 0) return;

            int nextIndex = (index + delta + count) % count;

            FocusNote(nextIndex);
        }

        private void FocusNoteDelayed(int index)
        {
            noteTabs.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() => FocusNote(index)));
        }

        private void FocusNote(int index)
        {
            if (index < 0 || index >= noteTabs.Items.Count) return;

            if (noteTabs.ItemContainerGenerator
                    .ContainerFromIndex(index) is not ContentPresenter container)
            {
                return;
            }

            if (container.ContentTemplate?
                    .FindName(NoteCardName, container) is FrameworkElement card)
            {
                card.Focus();
            }
        }
    }
}
