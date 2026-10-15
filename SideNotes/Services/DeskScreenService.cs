using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SideNotes.Services
{
    public sealed record DeskScreen(
        string DeviceName,
        Rect WorkArea,
        bool IsPrimary);

    public static class DeskScreenService
    {
        private const uint MonitorDefaultToNearest = 2;

        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;

        public static DeskScreen FindOrPrimary(string? deviceName)
        {
            var screens = new List<DeskScreen>();

            MonitorEnumProc callback =
                (IntPtr monitor, IntPtr hdc, ref NativeRect rect, IntPtr data) =>
                {
                    screens.Add(ReadScreen(monitor));
                    return true;
                };

            if (!EnumDisplayMonitors(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    callback,
                    IntPtr.Zero))
            {
                throw new Win32Exception();
            }

            return screens.Find(screen =>
                       string.Equals(
                           screen.DeviceName,
                           deviceName,
                           StringComparison.OrdinalIgnoreCase))
                   ?? screens.Find(screen => screen.IsPrimary)
                   ?? throw new InvalidOperationException(
                       "Nenhum monitor disponível.");
        }

        public static DeskScreen FromPoint(Point point)
        {
            var nativePoint = new NativePoint
            {
                X = (int)Math.Round(point.X),
                Y = (int)Math.Round(point.Y)
            };

            IntPtr monitor = MonitorFromPoint(
                nativePoint,
                MonitorDefaultToNearest);

            return ReadScreen(monitor);
        }

        public static Point GetMousePosition()
        {
            if (!GetCursorPos(out NativePoint point))
            {
                throw new Win32Exception();
            }

            return new Point(point.X, point.Y);
        }

        public static Rect GetWindowBounds(Window window)
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;

            if (!GetWindowRect(handle, out NativeRect rect))
            {
                throw new Win32Exception();
            }

            return ToRect(rect);
        }

        public static void MoveWindow(Window window, double x, double y)
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;

            if (!SetWindowPos(
                    handle,
                    IntPtr.Zero,
                    (int)Math.Round(x),
                    (int)Math.Round(y),
                    0,
                    0,
                    SwpNoSize | SwpNoZOrder | SwpNoActivate))
            {
                throw new Win32Exception();
            }
        }

        public static void SetWindowBounds(Window window, double x, double y, double width, double height)
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;

            if (!SetWindowPos(
                handle,
                IntPtr.Zero,
                (int)Math.Round(x),
                (int)Math.Round(y),
                Math.Max(1, (int)Math.Round(width)),
                Math.Max(1, (int)Math.Round(height)),
                SwpNoZOrder | SwpNoActivate))
            {
                throw new Win32Exception();
            }
        }

        private static DeskScreen ReadScreen(IntPtr monitor)
        {
            var info = new MonitorInfo
            {
                Size = Marshal.SizeOf<MonitorInfo>(),
                DeviceName = string.Empty
            };

            if (!GetMonitorInfo(monitor, ref info))
            {
                throw new Win32Exception();
            }

            return new DeskScreen(
                info.DeviceName,
                ToRect(info.WorkArea),
                (info.Flags & 1) != 0);
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