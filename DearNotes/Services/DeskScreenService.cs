using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DearNotes.Services
{
    public sealed record DeskScreen(
        string DeviceName,
        Rect WorkArea,
        bool IsPrimary,
        DpiScale Dpi);

    public sealed class DeskScreenService : IScreenService
    {
        private const uint MonitorDefaultToNearest = 2;

        private const uint MonitorInfoPrimary = 1;

        private const uint MonitorDpiEffective = 0;

        private const double ReferenceDpi = 96;

        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;

        public DeskScreen FindOrPrimary(string? deviceName)
        {
            List<DeskScreen> screens = EnumerateScreens();

            return screens.Find(screen =>
                       string.Equals(
                           screen.DeviceName,
                           deviceName,
                           StringComparison.OrdinalIgnoreCase))
                   ?? screens.Find(screen => screen.IsPrimary)
                   ?? throw new InvalidOperationException(
                       "Nenhum monitor disponível.");
        }

        public DeskScreen FromPoint(Point point)
        {
            var nativePoint = new NativePoint
            {
                X = (int)Math.Round(point.X),
                Y = (int)Math.Round(point.Y)
            };

            IntPtr monitor = MonitorFromPoint(
                nativePoint,
                MonitorDefaultToNearest);

            if (monitor == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Nenhum monitor foi encontrado para o ponto informado.");
            }

            return ReadScreen(monitor);
        }

        public Point GetMousePosition()
        {
            if (!GetCursorPos(out NativePoint point))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Não foi possível obter a posição do cursor.");
            }

            return new Point(point.X, point.Y);
        }

        public Rect GetWindowBounds(Window window)
        {
            IntPtr handle = GetWindowHandle(window);

            if (!GetWindowRect(handle, out NativeRect rect))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Não foi possível obter os limites da janela.");
            }

            return ToRect(rect);
        }

        public bool TryMoveWindow(Window window, double x, double y)
        {
            IntPtr handle = GetWindowHandle(window);

            bool succeeded = SetWindowPos(
                handle,
                IntPtr.Zero,
                (int)Math.Round(x),
                (int)Math.Round(y),
                0,
                0,
                SwpNoSize | SwpNoZOrder | SwpNoActivate);

            if (!succeeded)
            {
                LogWindowPositionFailure(
                    "mover",
                    Marshal.GetLastWin32Error());
            }

            return succeeded;
        }

        public bool TrySetWindowBounds(
            Window window,
            double x,
            double y,
            double width,
            double height)
        {
            IntPtr handle = GetWindowHandle(window);

            bool succeeded = SetWindowPos(
                handle,
                IntPtr.Zero,
                (int)Math.Round(x),
                (int)Math.Round(y),
                Math.Max(1, (int)Math.Round(width)),
                Math.Max(1, (int)Math.Round(height)),
                SwpNoZOrder | SwpNoActivate);

            if (!succeeded)
            {
                LogWindowPositionFailure(
                    "redimensionar",
                    Marshal.GetLastWin32Error());
            }

            return succeeded;
        }

        private static List<DeskScreen> EnumerateScreens()
        {
            var monitors = new List<IntPtr>();

            MonitorEnumProc callback =
                (monitor, hdc, ref rect, data) =>
                {
                    monitors.Add(monitor);
                    return true;
                };

            if (!EnumDisplayMonitors(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    callback,
                    IntPtr.Zero))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Não foi possível enumerar os monitores.");
            }

            var screens = new List<DeskScreen>(monitors.Count);

            foreach (IntPtr monitor in monitors)
            {
                screens.Add(ReadScreen(monitor));
            }

            return screens;
        }

        private static DeskScreen ReadScreen(IntPtr monitor)
        {
            if (monitor == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "O monitor informado é inválido.");
            }

            var info = new MonitorInfo
            {
                Size = Marshal.SizeOf<MonitorInfo>(),
                DeviceName = string.Empty
            };

            if (!GetMonitorInfo(monitor, ref info))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Não foi possível ler as informações do monitor.");
            }

            return new DeskScreen(
                info.DeviceName,
                ToRect(info.WorkArea),
                (info.Flags & MonitorInfoPrimary) != 0,
                ReadDpi(monitor));
        }

        private static DpiScale ReadDpi(IntPtr monitor)
        {
            int result = GetDpiForMonitor(
                monitor,
                MonitorDpiEffective,
                out uint dpiX,
                out uint dpiY);

            if (result != 0 || dpiX == 0 || dpiY == 0)
            {
                Debug.WriteLine(
                    "Não foi possível obter o DPI do monitor " +
                    $"(HRESULT 0x{result:X8}); usando 96 DPI.");

                return new DpiScale(1, 1);
            }

            return new DpiScale(
                dpiX / ReferenceDpi,
                dpiY / ReferenceDpi);
        }

        private static IntPtr GetWindowHandle(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);

            IntPtr handle = new WindowInteropHelper(window).Handle;

            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "A janela ainda não possui um identificador nativo.");
            }

            return handle;
        }

        private static void LogWindowPositionFailure(
            string operation,
            int errorCode)
        {
            Debug.WriteLine(
                $"Não foi possível {operation} a janela: " +
                new Win32Exception(errorCode).Message);
        }

        private static Rect ToRect(NativeRect rect)
        {
            return new Rect(
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Bounds;
            public NativeRect WorkArea;
            public uint Flags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
        }

        private delegate bool MonitorEnumProc(
            IntPtr monitor,
            IntPtr hdc,
            ref NativeRect rect,
            IntPtr data);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumDisplayMonitors(
            IntPtr hdc,
            IntPtr clip,
            MonitorEnumProc callback,
            IntPtr data);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(
            NativePoint point,
            uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetMonitorInfo(
            IntPtr monitor,
            ref MonitorInfo info);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(
            IntPtr monitor,
            uint dpiType,
            out uint dpiX,
            out uint dpiY);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetCursorPos(out NativePoint point);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(
            IntPtr window,
            out NativeRect rect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr window,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);
    }
}
