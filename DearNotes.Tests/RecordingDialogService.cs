using System.Collections.Generic;
using DearNotes.Services;

namespace DearNotes.Tests
{
    public sealed class RecordingDialogService : IDialogService
    {
        public List<(string Message, string Title)> Information { get; } = new();

        public List<(string Message, string Title)> Warnings { get; } = new();

        public List<(string Message, string Title)> Errors { get; } = new();

        public List<(string Message, string Title)> Confirmations { get; } = new();

        public bool ConfirmResult { get; set; } = true;

        public bool Confirm(string message, string title)
        {
            Confirmations.Add((message, title));

            return ConfirmResult;
        }

        public void ShowInformation(string message, string title)
        {
            Information.Add((message, title));
        }

        public void ShowWarning(string message, string title)
        {
            Warnings.Add((message, title));
        }

        public void ShowError(string message, string title)
        {
            Errors.Add((message, title));
        }
    }
}
