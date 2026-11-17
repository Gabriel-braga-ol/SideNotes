using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DearNotes.Services
{
    public static class AppLogger
    {
        private const long MaximumLogSizeBytes = 1_000_000;

        private static readonly object writeLock = new();

        public static void LogError(
            string context,
            Exception exception)
        {
            Write("ERRO", context, exception.ToString());
        }

        public static void LogWarning(string message)
        {
            Write("AVISO", message, details: null);
        }

        private static void Write(
            string level,
            string message,
            string? details)
        {
            string entry = FormatEntry(level, message, details);

            lock (writeLock)
            {
                try
                {
                    RotateIfNeeded();
                    Directory.CreateDirectory(AppPaths.LogsDirectory);
                    File.AppendAllText(
                        AppPaths.LogFilePath,
                        entry,
                        Encoding.UTF8);
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is UnauthorizedAccessException)
                {
                    // Sem acesso ao arquivo, a saída de depuração ainda preserva o diagnóstico.
                    Debug.WriteLine(
                        $"Falha ao gravar o log: {exception.Message}");
                    Debug.WriteLine(entry);
                }
            }
        }

        private static string FormatEntry(
            string level,
            string message,
            string? details)
        {
            var builder = new StringBuilder();
            builder.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
            builder.Append(" [").Append(level).Append("] ");
            builder.AppendLine(message);

            if (details is not null)
            {
                builder.AppendLine(details);
            }

            builder.AppendLine(new string('-', 60));

            return builder.ToString();
        }

        private static void RotateIfNeeded()
        {
            string logPath = AppPaths.LogFilePath;

            if (!File.Exists(logPath) ||
                new FileInfo(logPath).Length < MaximumLogSizeBytes)
            {
                return;
            }

            File.Move(
                logPath,
                logPath + ".1",
                overwrite: true);
        }
    }
}
