using System.Windows;

namespace DearNotes.Services
{
    public interface IScreenService
    {
        DeskScreen FindOrPrimary(string? deviceName);

        DeskScreen FromPoint(Point point);

        Point GetMousePosition();

        Rect GetWindowBounds(Window window);

        bool TryMoveWindow(Window window, double x, double y);

        bool TrySetWindowBounds(
            Window window,
            double x,
            double y,
            double width,
            double height);
    }
}
