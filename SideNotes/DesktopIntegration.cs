using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace SideNotes
{
    internal static partial class DesktopIntegration
    {
        private const int GwlStyle = -16;
        private const int WsChild = 0x40000000;
        private const int WsPopup = unchecked((int)0x80000000);


        [LibraryImport(
            "user32.dll",
            EntryPoint = "FindWindowW",
            StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr FindWindow(
            string? className,
            string? windowName);

        public static bool IsDesktopAvailable()
        {
            return FindWindow("Progman", null) != IntPtr.Zero;
        }

        [LibraryImport(
            "user32.dll",
            EntryPoint = "FindWindowExW",
            StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr FindWindowEx(
            IntPtr parent,
            IntPtr childAfter,
            string? className,
            string? windowName);

        public static string GetDesktopDiagnostic()
        {
            IntPtr progman = FindWindow("Progman", null);

            if (progman == IntPtr.Zero)
            {
                return "Explorer não encontrado";
            }

            IntPtr desktopView = FindWindowEx(
                progman,
                IntPtr.Zero,
                "SHELLDLL_DefView",
                null);

            if (desktopView != IntPtr.Zero)
            {
                return "Área dos ícones encontrada em Progman";
            }

            IntPtr worker = IntPtr.Zero;

            while (true)
            {
                worker = FindWindowEx(
                    IntPtr.Zero,
                    worker,
                    "WorkerW",
                    null);

                if (worker == IntPtr.Zero)
                {
                    break;
                }

                desktopView = FindWindowEx(
                    worker,
                    IntPtr.Zero,
                    "SHELLDLL_DefView",
                    null);

                if (desktopView != IntPtr.Zero)
                {
                    return "Área dos ícones encontrada em WorkerW";
                }
            }

            return "Explorer encontrado; área dos ícones não localizada";
        }   

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW",
            SetLastError = true)]
        private static partial int GetWindowLong(
            IntPtr window,
            int index);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW",
            SetLastError = true)]
        private static partial int SetWindowLong(
            IntPtr window,
            int index,
            int value);

        [LibraryImport("user32.dll", SetLastError = true)]
        private static partial IntPtr SetParent(
            IntPtr child,
            IntPtr newParent);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(
            IntPtr window,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        private static IntPtr FindDesktopHost()
        {
            IntPtr progman = FindWindow("Progman", null);

            if (progman != IntPtr.Zero &&
                FindWindowEx(
                    progman, IntPtr.Zero,
                    "SHELLDLL_DefView", null) != IntPtr.Zero)
            {
                return progman;
            }

            IntPtr worker = IntPtr.Zero;

            while (true)
            {
                worker = FindWindowEx(
                    IntPtr.Zero, worker, "WorkerW", null);

                if (worker == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                if (FindWindowEx(
                    worker, IntPtr.Zero,
                    "SHELLDLL_DefView", null) != IntPtr.Zero)
                {
                    return worker;
                }
            }
        }

        public static void AttachToDesktop(IntPtr window)
        {
            IntPtr host = FindDesktopHost();

            if (host == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Não foi possível localizar a janela da área de trabalho.");
            }

            int style = GetWindowLong(window, GwlStyle);
            int error = Marshal.GetLastPInvokeError();

            if (style == 0 && error != 0)
            {
                throw new Win32Exception(error);
            }

            int childStyle = (style & ~WsPopup) | WsChild;

            int previousStyle = SetWindowLong(
                window, GwlStyle, childStyle);

            error = Marshal.GetLastPInvokeError();

            if (previousStyle == 0 && error != 0)
            {
                throw new Win32Exception(error);
            }

            IntPtr previousParent = SetParent(window, host);
            error = Marshal.GetLastPInvokeError();

            if (previousParent == IntPtr.Zero && error != 0)
            {
                SetWindowLong(window, GwlStyle, style);
                throw new Win32Exception(error);
            }

            const uint SwpNoSize = 0x0001;
            const uint SwpNoActivate = 0x0010;
            const uint SwpFrameChanged = 0x0020;

            // Posição provisória dentro da área de trabalho.
            if (!SetWindowPos(
                window,
                IntPtr.Zero,
                100,
                100,
                0,
                0,
                SwpNoSize | SwpNoActivate | SwpFrameChanged))
            {
                throw new Win32Exception(
                    Marshal.GetLastPInvokeError());
            }
        }

        public static HwndSource CreateDeskPrototype(IntPtr host)
        {       
            if (host == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Não foi possível localizar a área de trabalho.");
            }

            const int WsVisible = 0x10000000;

            var parameters = new HwndSourceParameters("SideNotes Desk")
            {
                ParentWindow = host,
                WindowStyle = WsChild | WsVisible,
                PositionX = 100,
                PositionY = 100,
                Width = 280,
                Height = 360
            };

            var source = new HwndSource(parameters);

            try
            {
                source.CompositionTarget.RenderMode =
                    RenderMode.SoftwareOnly;

                var panel = new Grid
                {
                    Background = new SolidColorBrush(
                        Color.FromRgb(184, 230, 208))
                };

                panel.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });

                panel.RowDefinitions.Add(new RowDefinition
                {
                    Height = GridLength.Auto
                });

                var text = new TextBlock
                {
                    Text = "Protótipo do modo Desk",
                    FontSize = 22,
                    Foreground = new SolidColorBrush(
                        Color.FromRgb(30, 58, 52)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(24),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var button = new Button
                {
                    Content = "Testar clique",
                    Height = 36,
                    Margin = new Thickness(24)
                };

                button.Click += (_, _) =>
                {
                    button.Content = "Clique recebido!";
                };

                Grid.SetRow(button, 1);

                panel.Children.Add(text);
                panel.Children.Add(button);

                source.RootVisual = panel;

                const uint SwpNoSize = 0x0001;
                const uint SwpNoMove = 0x0002;
                const uint SwpNoActivate = 0x0010;
                const uint SwpShowWindow = 0x0040;

                if (!SetWindowPos(
                    source.Handle,
                    IntPtr.Zero,
                    0,
                    0,
                    0,
                    0,
                    SwpNoSize | SwpNoMove | SwpNoActivate | SwpShowWindow))
                {
                    throw new Win32Exception(
                        Marshal.GetLastPInvokeError());
                }

                return source;
            }
            catch
            {
                source.Dispose();
                throw;
            }
        }
    }
}