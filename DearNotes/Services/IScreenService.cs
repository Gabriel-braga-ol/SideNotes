using System.Windows;

namespace DearNotes.Services
{
    public interface IScreenService
    {
        DeskScreen FindOrPrimary(string? deviceName);

        DeskScreen FromPoint(Point point);

        Point GetMousePosition();

        Rect GetWindowBounds(Window window);

        void MoveWindow(Window window, double x, double y);

        void SetWindowBounds(
            Window window,
            double x,
            double y,
            double width,
            double height);
    }
}
