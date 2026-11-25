using System;
using System.Globalization;
using System.Windows.Data;

namespace DearNotes.Views.Converters
{
    /// <summary>
    /// Apresenta uma data como tempo relativo, como "agora" ou "há 2 h".
    /// O parâmetro "short" remove o prefixo para uso em listas compactas.
    /// </summary>
    public sealed class RelativeTimeConverter : IValueConverter
    {
        private static readonly CultureInfo portugueseCulture =
            CultureInfo.GetCultureInfo("pt-BR");

        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is not DateTimeOffset timestamp)
            {
                return string.Empty;
            }

            TimeSpan elapsed = DateTimeOffset.Now - timestamp;

            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            bool shortForm = string.Equals(
                parameter as string,
                "short",
                StringComparison.OrdinalIgnoreCase);

            if (elapsed.TotalMinutes < 1)
            {
                return "agora";
            }

            if (elapsed.TotalHours < 1)
            {
                int minutes = (int)elapsed.TotalMinutes;

                return shortForm
                    ? $"{minutes} min"
                    : $"há {minutes} min";
            }

            if (elapsed.TotalDays < 1)
            {
                int hours = (int)elapsed.TotalHours;

                return shortForm
                    ? $"{hours} h"
                    : $"há {hours} h";
            }

            if (elapsed.TotalDays < 7)
            {
                int days = (int)elapsed.TotalDays;

                return shortForm
                    ? $"{days} d"
                    : $"há {days} d";
            }

            return timestamp
                .ToLocalTime()
                .ToString("dd MMM", portugueseCulture);
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
