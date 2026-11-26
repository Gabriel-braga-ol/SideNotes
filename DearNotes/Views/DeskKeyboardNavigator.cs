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
    /// abertura de notas, foco entre as cartas e retorno ao baralho.
    /// </summary>
    internal sealed class DeskKeyboardNavigator
    {
        private const string NoteCardName = "NoteCard";

        private readonly DeskViewModel viewModel;

        private readonly ItemsControl fanCards;

        private readonly FrameworkElement deskHandle;

        private readonly Action<Note> openNote;

        public DeskKeyboardNavigator(
            DeskViewModel viewModel,
            ItemsControl fanCards,
            FrameworkElement deskHandle,
            Action<Note> openNote)
        {
            this.viewModel = viewModel;
            this.fanCards = fanCards;
            this.deskHandle = deskHandle;
            this.openNote = openNote;
        }

        public void HandleDeskKeyDown(
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
                    CollapseAndFocusDesk();
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

        private void CollapseAndFocusDesk()
        {
            viewModel.SetExpanded(false);
            deskHandle.Focus();
        }

        private void MoveFocus(Note note, int delta)
        {
            int count = fanCards.Items.Count;

            if (count == 0) return;

            int index = fanCards.Items.IndexOf(note);

            if (index < 0) return;

            int nextIndex = (index + delta + count) % count;

            FocusNote(nextIndex);
        }

        private void FocusNoteDelayed(int index)
        {
            fanCards.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() => FocusNote(index)));
        }

        private void FocusNote(int index)
        {
            if (index < 0 || index >= fanCards.Items.Count) return;

            if (fanCards.ItemContainerGenerator
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
