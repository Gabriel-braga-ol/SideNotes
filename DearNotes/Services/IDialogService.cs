namespace DearNotes.Services
{
    /// <summary>
    /// Apresenta perguntas e avisos ao usuário sem acoplar ViewModels ao WPF.
    /// </summary>
    public interface IDialogService
    {
        bool Confirm(string message, string title);

        void ShowInformation(string message, string title);

        void ShowWarning(string message, string title);

        void ShowError(string message, string title);
    }
}
