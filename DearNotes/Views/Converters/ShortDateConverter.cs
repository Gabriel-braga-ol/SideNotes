using System;
using System.Globalization;
using System.Windows.Data;

namespace DearNotes.Views.Converters
{
    /// <summary>
    /// Apresenta a data local no formato curto usado no rodapé do editor.
    /// </summary>
    public sealed class ShortDateConverter : IValueConverter
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

            return timestamp
                .ToLocalTime()
                .ToString("dd MMM yyyy", portugueseCulture);
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
