using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DearNotes.Services
{
    /// <summary>
    /// Ajusta a moldura personalizada da janela: cantos arredondados do Windows 11
    /// e limites de maximização dentro da área útil do monitor atual.
    /// </summary>
    public sealed class WindowFrameService
    {
        private const int WmGetMinMaxInfo = 0x0024;

        private const int DwmWindowCornerPreference = 33;

        private const int DwmWindowCornerRound = 2;

        private const uint MonitorDefaultToNearest = 2;

        private Window? window;

        private HwndSource? source;

        public void Attach(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);

            this.window = window;
            window.SourceInitialized += OnSourceInitialized;
            window.Closed += OnClosed;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            if (window is null) return;

            IntPtr handle = new WindowInteropHelper(window).Handle;

            if (handle == IntPtr.Zero) return;

            ApplyRoundedCorners(handle);

            source = HwndSource.FromHwnd(handle);
            source?.AddHook(WindowProc);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (window is not null)
            {
                window.SourceInitialized -= OnSourceInitialized;
                window.Closed -= OnClosed;
            }

            source?.RemoveHook(WindowProc);

            source = null;
            window = null;
        }

        private static void ApplyRoundedCorners(IntPtr handle)
        {
            int preference = DwmWindowCornerRound;

            // Sistemas anteriores ao Windows 11 recusam o atributo e mantêm os cantos retos.
            _ = DwmSetWindowAttribute(
                handle,
                DwmWindowCornerPreference,
                ref preference,
                sizeof(int));
        }

        private static IntPtr WindowProc(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (message != WmGetMinMaxInfo)
            {
                return IntPtr.Zero;
            }

            AdjustMaximizedBounds(hwnd, lParam);

            handled = true;

            return IntPtr.Zero;
        }

        private static void AdjustMaximizedBounds(
            IntPtr hwnd,
            IntPtr lParam)
        {
            IntPtr monitor = MonitorFromWindow(
                hwnd,
                MonitorDefaultToNearest);

            if (monitor == IntPtr.Zero) return;

            var monitorInfo = new MonitorInfo
            {
                Size = Marshal.SizeOf<MonitorInfo>()
            };

            if (!GetMonitorInfo(monitor, ref monitorInfo)) return;

            var minMaxInfo =
                Marshal.PtrToStructure<MinMaxInfo>(lParam);

            minMaxInfo.MaxPosition.X =
                monitorInfo.WorkArea.Left -
                monitorInfo.MonitorArea.Left;

            minMaxInfo.MaxPosition.Y =
                monitorInfo.WorkArea.Top -
                monitorInfo.MonitorArea.Top;

            minMaxInfo.MaxSize.X =
                monitorInfo.WorkArea.Right -
                monitorInfo.WorkArea.Left;

            minMaxInfo.MaxSize.Y =
                monitorInfo.WorkArea.Bottom -
                monitorInfo.WorkArea.Top;

            Marshal.StructureToPtr(
                minMaxInfo,
                lParam,
                fDeleteOld: false);
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

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect MonitorArea;
            public NativeRect WorkArea;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
        {
            public NativePoint Reserved;
            public NativePoint MaxSize;
            public NativePoint MaxPosition;
            public NativePoint MinTrackSize;
            public NativePoint MaxTrackSize;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attribute,
            ref int value,
            int size);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(
            IntPtr hwnd,
            uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetMonitorInfo(
            IntPtr monitor,
            ref MonitorInfo info);
    }
}
