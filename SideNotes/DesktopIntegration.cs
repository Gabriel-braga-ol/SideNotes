using System;
using System.Runtime.InteropServices;

namespace SideNotes
{
    internal static partial class DesktopIntegration
    {
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
    }
}