using System.Windows;

namespace DearNotes.Services
{
    public sealed class MessageBoxDialogService : IDialogService
    {
        public bool Confirm(string message, string title)
        {
            return Show(
                    message,
                    title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No)
                == MessageBoxResult.Yes;
        }

        public void ShowInformation(string message, string title)
        {
            Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                MessageBoxResult.OK);
        }

        public void ShowWarning(string message, string title)
        {
            Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning,
                MessageBoxResult.OK);
        }

        public void ShowError(string message, string title)
        {
            Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error,
                MessageBoxResult.OK);
        }

        private static MessageBoxResult Show(
            string message,
            string title,
            MessageBoxButton button,
            MessageBoxImage image,
            MessageBoxResult defaultResult)
        {
            Window? owner = ResolveOwner();

            // Sem janela principal (inicialização ou encerramento), o aviso aparece sem dono.
            return owner is null
                ? MessageBox.Show(
                    message,
                    title,
                    button,
                    image,
                    defaultResult)
                : MessageBox.Show(
                    owner,
                    message,
                    title,
                    button,
                    image,
                    defaultResult);
        }

        private static Window? ResolveOwner()
        {
            Window? owner = Application.Current?.MainWindow;

            return owner is { IsVisible: true } ? owner : null;
        }
    }
}
